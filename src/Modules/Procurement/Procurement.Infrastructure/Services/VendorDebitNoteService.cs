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

public class VendorDebitNoteService : IVendorDebitNoteService
{
    private readonly ProcurementDbContext _ctx;
    private readonly IDocumentSequenceService _sequences;
    private readonly IPurchaseReturnService _returns;
    private readonly ILogger<VendorDebitNoteService> _logger;

    public VendorDebitNoteService(
        ProcurementDbContext ctx, IDocumentSequenceService sequences,
        IPurchaseReturnService returns, ILogger<VendorDebitNoteService> logger)
    {
        _ctx       = ctx;
        _sequences = sequences;
        _returns   = returns;
        _logger    = logger;
    }

    private IQueryable<VendorDebitNote> BaseQuery()
        => _ctx.VendorDebitNotes
            .AsNoTracking()
            .Include(d => d.Lines)
            .Include(d => d.Vendor)
            .Include(d => d.PurchaseReturn)
            .Include(d => d.OriginalInvoice)
            .Where(d => !d.IsDeleted);

    public async Task<PaginatedResponse<VendorDebitNoteDto>> GetAllAsync(PaginationParams pagination)
    {
        var search = pagination.SearchTerm?.Trim().ToLower();

        // List query mirrors the inherited GetPagedAsync (AsNoTracking, no Include): the
        // list cards only need scalar fields, so collection navs stay empty-initialized.
        var query = _ctx.VendorDebitNotes
            .AsNoTracking()
            .Where(d => !d.IsDeleted)
            .Where(d => (string.IsNullOrEmpty(search)
                    || d.DebitNoteNumber.ToLower().Contains(search)
                    || (d.VendorName != null && d.VendorName.ToLower().Contains(search))
                    || d.PurchaseReturn.ReturnNumber.ToLower().Contains(search))
                && (pagination.Status == null || (int)d.Status == pagination.Status));

        var total = await query.CountAsync();

        var items = await query
            .ApplyOrderNewestFirst(pagination.SortBy,
                string.IsNullOrWhiteSpace(pagination.SortBy) ? "desc" : pagination.SortDirection, "DebitNoteDate")
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return PaginatedResponse<VendorDebitNoteDto>.Ok(
            items.Select(MapToListDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<List<VendorDebitNoteDto>> GetByVendorAsync(Guid vendorId)
        => (await BaseQuery().Where(d => d.VendorId == vendorId).ToListAsync()).Select(MapToDto).ToList();

    public async Task<VendorDebitNoteDto?> GetByIdAsync(Guid id)
    {
        var entity = await BaseQuery().FirstOrDefaultAsync(d => d.Id == id);
        return entity is null ? null : MapToDto(entity);
    }

    public Task<List<PurchaseReturnDto>> GetEligibleReturnsAsync()
        => _returns.GetEligibleForDebitNoteAsync();

    public async Task<VendorDebitNoteDto> CreateAsync(CreateVendorDebitNoteDto dto)
    {
        var ret = await _ctx.PurchaseReturns
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == dto.PurchaseReturnId && !r.IsDeleted)
            ?? throw new KeyNotFoundException("Purchase return not found");

        if (ret.Status != PurchaseReturnStatus.Posted)
            throw new InvalidOperationException("A debit note can only be raised against a posted return.");

        var exists = await _ctx.VendorDebitNotes.AnyAsync(d => d.PurchaseReturnId == ret.Id && !d.IsDeleted);
        if (exists)
            throw new InvalidOperationException("A debit note already exists for this return.");

        var note = new VendorDebitNote
        {
            DebitNoteNumber   = await _sequences.GetNextNumberAsync(ProcurementDocumentType.VendorDebitNote),
            PurchaseReturnId  = ret.Id,
            VendorId          = ret.VendorId,
            OriginalInvoiceId = dto.OriginalInvoiceId,
            DebitNoteDate     = dto.DebitNoteDate,
            Status            = DebitNoteStatus.Draft,
            CurrencyCode      = ret.CurrencyCode,
            ExchangeRate      = 1m,
            Notes             = dto.Notes,
        };

        var lineNumber = 1;
        decimal sub = 0, tax = 0;
        foreach (var rl in ret.Lines)
        {
            var lineSub = rl.QuantityReturned * rl.UnitPrice;
            sub += lineSub;
            tax += rl.TaxAmount;
            note.Lines.Add(new VendorDebitNoteLine
            {
                LineNumber      = lineNumber++,
                ReturnLineId    = rl.Id,
                ItemId          = rl.ItemId,
                ItemCode        = rl.ItemCode,
                ItemDescription = rl.ItemDescription,
                Quantity        = rl.QuantityReturned,
                UnitOfMeasureId = rl.UnitOfMeasureId,
                UnitPrice       = rl.UnitPrice,
                DiscountAmount  = 0,
                TaxPercent      = rl.TaxPercent,
                TaxAmount       = rl.TaxAmount,
                SubTotal        = lineSub,
                TotalAmount     = lineSub + rl.TaxAmount,
            });
        }

        note.SubTotalAmount    = sub;
        note.TaxAmount         = tax;
        note.TotalAmount       = sub + tax;
        note.SettledAmount     = 0;
        note.OutstandingAmount = note.TotalAmount;

        _ctx.VendorDebitNotes.Add(note);
        await _ctx.SaveChangesAsync();
        _logger.LogInformation("Created VendorDebitNote {Number} from return {ReturnId}", note.DebitNoteNumber, ret.Id);
        return (await GetByIdAsync(note.Id))!;
    }

    public async Task<VendorDebitNoteDto> SendAsync(Guid id)
    {
        var n = await Tracked(id);
        if (n.Status != DebitNoteStatus.Draft) throw new InvalidOperationException("Only a draft debit note can be sent.");
        n.Status = DebitNoteStatus.Sent; n.SentAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<VendorDebitNoteDto> AcknowledgeAsync(Guid id)
    {
        var n = await Tracked(id);
        if (n.Status != DebitNoteStatus.Sent) throw new InvalidOperationException("Only a sent debit note can be acknowledged.");
        n.Status = DebitNoteStatus.Acknowledged; n.AcknowledgedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<VendorDebitNoteDto> SettleAsync(Guid id)
    {
        var n = await Tracked(id);
        if (n.Status is DebitNoteStatus.Cancelled or DebitNoteStatus.FullySettled)
            throw new InvalidOperationException("This debit note cannot be settled.");
        n.SettledAmount = n.TotalAmount;
        n.OutstandingAmount = 0;
        n.Status = DebitNoteStatus.FullySettled;
        n.SettledAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<VendorDebitNoteDto> CancelAsync(Guid id)
    {
        var n = await Tracked(id);
        if (n.Status == DebitNoteStatus.FullySettled) throw new InvalidOperationException("A settled debit note cannot be cancelled.");
        n.Status = DebitNoteStatus.Cancelled;
        await _ctx.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task DeleteAsync(Guid id)
    {
        var n = await Tracked(id);
        if (n.Status is not (DebitNoteStatus.Draft or DebitNoteStatus.Cancelled))
            throw new InvalidOperationException("Only a draft or cancelled debit note can be deleted.");
        n.IsDeleted = true; n.DeletedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();
    }

    private async Task<VendorDebitNote> Tracked(Guid id)
        => await _ctx.VendorDebitNotes.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted)
            ?? throw new KeyNotFoundException("Debit note not found");

    // List projection: no navigations loaded, so only scalar fields are mapped.
    // VendorName uses the denormalized scalar; nav-derived numbers stay null and
    // Lines stay empty (the list cards never read them).
    private static VendorDebitNoteDto MapToListDto(VendorDebitNote d) => new()
    {
        Id = d.Id, DebitNoteNumber = d.DebitNoteNumber, PurchaseReturnId = d.PurchaseReturnId,
        VendorId = d.VendorId, VendorName = d.VendorName, OriginalInvoiceId = d.OriginalInvoiceId,
        DebitNoteDate = d.DebitNoteDate, SentAt = d.SentAt, AcknowledgedAt = d.AcknowledgedAt, SettledAt = d.SettledAt,
        Status = d.Status, CurrencyCode = d.CurrencyCode, ExchangeRate = d.ExchangeRate,
        SubTotalAmount = d.SubTotalAmount, TaxAmount = d.TaxAmount, TotalAmount = d.TotalAmount,
        SettledAmount = d.SettledAmount, OutstandingAmount = d.OutstandingAmount, Notes = d.Notes,
    };

    private static VendorDebitNoteDto MapToDto(VendorDebitNote d) => new()
    {
        Id = d.Id, DebitNoteNumber = d.DebitNoteNumber, PurchaseReturnId = d.PurchaseReturnId,
        PurchaseReturnNumber = d.PurchaseReturn?.ReturnNumber, VendorId = d.VendorId, VendorName = d.Vendor?.Name,
        OriginalInvoiceId = d.OriginalInvoiceId, OriginalInvoiceNumber = d.OriginalInvoice?.InvoiceNumber,
        DebitNoteDate = d.DebitNoteDate, SentAt = d.SentAt, AcknowledgedAt = d.AcknowledgedAt, SettledAt = d.SettledAt,
        Status = d.Status, CurrencyCode = d.CurrencyCode, ExchangeRate = d.ExchangeRate,
        SubTotalAmount = d.SubTotalAmount, TaxAmount = d.TaxAmount, TotalAmount = d.TotalAmount,
        SettledAmount = d.SettledAmount, OutstandingAmount = d.OutstandingAmount, Notes = d.Notes,
        Lines = d.Lines.OrderBy(l => l.LineNumber).Select(l => new VendorDebitNoteLineDto
        {
            Id = l.Id, LineNumber = l.LineNumber, ReturnLineId = l.ReturnLineId, ItemId = l.ItemId, ItemCode = l.ItemCode,
            ItemDescription = l.ItemDescription, Quantity = l.Quantity, UnitPrice = l.UnitPrice, DiscountAmount = l.DiscountAmount,
            TaxPercent = l.TaxPercent, TaxAmount = l.TaxAmount, SubTotal = l.SubTotal, TotalAmount = l.TotalAmount, Notes = l.Notes,
        }).ToList(),
    };
}
