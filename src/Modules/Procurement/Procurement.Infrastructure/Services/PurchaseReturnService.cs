using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;
using Procurement.Domain.Entities;
using Procurement.Domain.Enums;
using Procurement.Infrastructure.Persistence;

namespace Procurement.Infrastructure.Services;

public class PurchaseReturnService : IPurchaseReturnService
{
    private readonly ProcurementDbContext _ctx;
    private readonly IDocumentSequenceService _sequences;
    private readonly ILogger<PurchaseReturnService> _logger;

    public PurchaseReturnService(ProcurementDbContext ctx, IDocumentSequenceService sequences, ILogger<PurchaseReturnService> logger)
    {
        _ctx       = ctx;
        _sequences = sequences;
        _logger    = logger;
    }

    private IQueryable<PurchaseReturn> BaseQuery()
        => _ctx.PurchaseReturns
            .AsNoTracking()
            .Include(r => r.Lines)
            .Include(r => r.Vendor)
            .Include(r => r.GoodsReceipt)
            .Include(r => r.PurchaseOrder)
            .Include(r => r.DebitNote)
            .Where(r => !r.IsDeleted);

    public async Task<PaginatedResponse<PurchaseReturnDto>> GetAllAsync(PaginationParams pagination)
    {
        var search = pagination.SearchTerm?.Trim().ToLower();
        var query = _ctx.PurchaseReturns
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .Where(r =>
                (string.IsNullOrEmpty(search) ||
                    r.ReturnNumber.ToLower().Contains(search) ||
                    (r.VendorReturnAuthorisationNumber != null && r.VendorReturnAuthorisationNumber.ToLower().Contains(search))) &&
                (pagination.Status == null || (int)r.Status == pagination.Status));

        var total = await query.CountAsync();

        // Eager-load the navigations the list columns need (GRN #, Vendor, Debit Note) — count above stays join-free.
        var items = await query
            .Include(r => r.Vendor)
            .Include(r => r.GoodsReceipt)
            .Include(r => r.PurchaseOrder)
            .Include(r => r.DebitNote)
            .ApplyOrderNewestFirst(pagination.SortBy,
                string.IsNullOrWhiteSpace(pagination.SortBy) ? "desc" : pagination.SortDirection, "ReturnDate")
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return PaginatedResponse<PurchaseReturnDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<List<PurchaseReturnDto>> GetByVendorAsync(Guid vendorId)
        => (await BaseQuery().Where(r => r.VendorId == vendorId).ToListAsync()).Select(MapToDto).ToList();

    public async Task<List<PurchaseReturnDto>> GetEligibleForDebitNoteAsync()
        => (await BaseQuery()
                .Where(r => r.Status == PurchaseReturnStatus.Posted && r.DebitNote == null)
                .ToListAsync())
            .Select(MapToDto).ToList();

    public async Task<PurchaseReturnDto?> GetByIdAsync(Guid id)
    {
        var entity = await BaseQuery().FirstOrDefaultAsync(r => r.Id == id);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<PurchaseReturnDto> CreateAsync(CreatePurchaseReturnDto dto)
    {
        var grn = await _ctx.GoodsReceipts
            .Include(g => g.Lines)
            .FirstOrDefaultAsync(g => g.Id == dto.GoodsReceiptId && !g.IsDeleted)
            ?? throw new KeyNotFoundException("Goods receipt not found");

        if (grn.Status != GoodsReceiptStatus.Posted)
            throw new InvalidOperationException("Returns can only be created against a posted goods receipt.");

        if (dto.Lines is null || dto.Lines.Count == 0)
            throw new InvalidOperationException("Add at least one line to return.");

        // Load the PO once for line pricing (avoid EF array.Contains).
        var po = await _ctx.PurchaseOrders.Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == grn.PurchaseOrderId);
        var poLines = po?.Lines.ToDictionary(l => l.Id) ?? new();

        var ret = new PurchaseReturn
        {
            ReturnNumber                   = await _sequences.GetNextNumberAsync(ProcurementDocumentType.PurchaseReturn),
            PurchaseOrderId                = grn.PurchaseOrderId,
            GoodsReceiptId                 = grn.Id,
            VendorId                       = grn.VendorId,
            ReturnDate                     = dto.ReturnDate,
            ReturnReason                   = dto.ReturnReason,
            Status                         = PurchaseReturnStatus.Draft,
            CurrencyCode                   = po?.CurrencyCode ?? "USD",
            VendorReturnAuthorisationNumber = dto.VendorReturnAuthorisationNumber,
            Description                    = dto.Description,
            Notes                          = dto.Notes,
        };

        var lineNumber = 1;
        decimal total = 0;
        foreach (var lineDto in dto.Lines.Where(l => l.QuantityReturned > 0))
        {
            var grnLine = grn.Lines.FirstOrDefault(l => l.Id == lineDto.GoodsReceiptLineId)
                ?? throw new InvalidOperationException("Goods receipt line not found on this receipt.");

            if (lineDto.QuantityReturned > grnLine.QuantityReceived)
                throw new InvalidOperationException($"Cannot return more than received for '{grnLine.ItemDescription}'.");

            poLines.TryGetValue(grnLine.PurchaseOrderLineId, out var poLine);
            var unitPrice  = poLine?.UnitPrice ?? 0;
            var taxPercent = poLine?.TaxPercent ?? 0;
            var lineSub    = lineDto.QuantityReturned * unitPrice;
            var taxAmount  = lineSub * taxPercent / 100;
            var lineTotal  = lineSub + taxAmount;
            total += lineTotal;

            ret.Lines.Add(new PurchaseReturnLine
            {
                LineNumber              = lineNumber++,
                GoodsReceiptLineId      = grnLine.Id,
                PurchaseOrderLineId     = grnLine.PurchaseOrderLineId,
                ItemId                  = grnLine.ItemId,
                ItemCode                = grnLine.ItemCode,
                ItemDescription         = grnLine.ItemDescription,
                QuantityReceived        = grnLine.QuantityReceived,
                QuantityReturned        = lineDto.QuantityReturned,
                UnitOfMeasureId         = grnLine.UnitOfMeasureId,
                UnitOfMeasureName       = grnLine.UnitOfMeasureName,
                UnitPrice               = unitPrice,
                TaxPercent              = taxPercent,
                TaxAmount               = taxAmount,
                TotalReturnAmount       = lineTotal,
                ReturnReason            = lineDto.ReturnReason,
                QualityIssueDescription = lineDto.QualityIssueDescription,
                LotNumber               = lineDto.LotNumber ?? grnLine.LotNumber,
                Notes                   = lineDto.Notes,
            });
        }

        if (ret.Lines.Count == 0)
            throw new InvalidOperationException("Enter a return quantity on at least one line.");

        ret.TotalReturnAmount = total;
        _ctx.PurchaseReturns.Add(ret);
        await _ctx.SaveChangesAsync();
        _logger.LogInformation("Created PurchaseReturn {Number}", ret.ReturnNumber);
        return (await GetByIdAsync(ret.Id))!;
    }

    public async Task<PurchaseReturnDto> ApproveAsync(Guid id, Guid userId)
    {
        var ret = await Tracked(id);
        if (ret.Status != PurchaseReturnStatus.Draft)
            throw new InvalidOperationException("Only a draft return can be approved.");
        ret.Status = PurchaseReturnStatus.Approved;
        ret.ApprovedAt = DateTime.UtcNow;
        ret.ApprovedByUserId = userId;
        await _ctx.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<PurchaseReturnDto> PostAsync(Guid id, Guid userId)
    {
        var ret = await Tracked(id);
        if (ret.Status != PurchaseReturnStatus.Approved)
            throw new InvalidOperationException("Only an approved return can be posted.");
        ret.Status = PurchaseReturnStatus.Posted;
        ret.PostedAt = DateTime.UtcNow;
        ret.PostedByUserId = userId;
        await _ctx.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<PurchaseReturnDto> CancelAsync(Guid id)
    {
        var ret = await Tracked(id);
        if (ret.Status == PurchaseReturnStatus.Posted)
            throw new InvalidOperationException("A posted return cannot be cancelled.");
        ret.Status = PurchaseReturnStatus.Cancelled;
        await _ctx.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task DeleteAsync(Guid id)
    {
        var ret = await Tracked(id);
        if (ret.Status is not (PurchaseReturnStatus.Draft or PurchaseReturnStatus.Cancelled))
            throw new InvalidOperationException("Only a draft or cancelled return can be deleted.");
        ret.IsDeleted = true;
        ret.DeletedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();
    }

    private async Task<PurchaseReturn> Tracked(Guid id)
        => await _ctx.PurchaseReturns.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted)
            ?? throw new KeyNotFoundException("Purchase return not found");

    private static PurchaseReturnDto MapToDto(PurchaseReturn r) => new()
    {
        Id                  = r.Id,
        ReturnNumber        = r.ReturnNumber,
        PurchaseOrderId     = r.PurchaseOrderId,
        PurchaseOrderNumber = r.PurchaseOrder?.OrderNumber,
        GoodsReceiptId      = r.GoodsReceiptId,
        GoodsReceiptNumber  = r.GoodsReceipt?.ReceiptNumber,
        VendorId            = r.VendorId,
        VendorName          = r.Vendor?.Name,
        ReturnDate          = r.ReturnDate,
        ApprovedAt          = r.ApprovedAt,
        PostedAt            = r.PostedAt,
        Status              = r.Status,
        ReturnReason        = r.ReturnReason,
        CurrencyCode        = r.CurrencyCode,
        TotalReturnAmount   = r.TotalReturnAmount,
        VendorReturnAuthorisationNumber = r.VendorReturnAuthorisationNumber,
        Description         = r.Description,
        Notes               = r.Notes,
        DebitNoteId         = r.DebitNote?.Id,
        DebitNoteNumber     = r.DebitNote?.DebitNoteNumber,
        Lines = r.Lines.OrderBy(l => l.LineNumber).Select(l => new PurchaseReturnLineDto
        {
            Id = l.Id, LineNumber = l.LineNumber, GoodsReceiptLineId = l.GoodsReceiptLineId,
            PurchaseOrderLineId = l.PurchaseOrderLineId, ItemId = l.ItemId, ItemCode = l.ItemCode,
            ItemDescription = l.ItemDescription, QuantityReceived = l.QuantityReceived, QuantityReturned = l.QuantityReturned,
            UnitOfMeasureId = l.UnitOfMeasureId, UnitOfMeasureName = l.UnitOfMeasureName, UnitPrice = l.UnitPrice,
            TaxPercent = l.TaxPercent, TaxAmount = l.TaxAmount, TotalReturnAmount = l.TotalReturnAmount,
            ReturnReason = l.ReturnReason, QualityIssueDescription = l.QualityIssueDescription, LotNumber = l.LotNumber, Notes = l.Notes,
        }).ToList(),
    };
}
