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

public class LandedCostService : ILandedCostService
{
    private readonly ProcurementDbContext _ctx;
    private readonly ILogger<LandedCostService> _logger;

    public LandedCostService(ProcurementDbContext ctx, ILogger<LandedCostService> logger)
    {
        _ctx    = ctx;
        _logger = logger;
    }

    private IQueryable<LandedCost> BaseQuery()
        => _ctx.LandedCosts
            .AsNoTracking()
            .Include(l => l.CostLines)
            .Include(l => l.GoodsReceipts).ThenInclude(g => g.GoodsReceipt)
            .Include(l => l.Allocations).ThenInclude(a => a.GoodsReceiptLine)
            .Include(l => l.Vendor)
            .Where(l => !l.IsDeleted);

    public async Task<PaginatedResponse<LandedCostDto>> GetAllAsync(PaginationParams pagination)
    {
        var search = pagination.SearchTerm?.Trim().ToLower();

        // LandedCost has no dedicated repository — replicate GetPagedAsync inline over the
        // DbContext (AsNoTracking, soft-delete filter, predicate, ApplyOrder, Count, Skip/Take).
        // No Include: the list mapper reads scalar fields only; collection navs stay empty.
        var query = _ctx.LandedCosts
            .AsNoTracking()
            .Where(l => !l.IsDeleted)
            .Where(l =>
                (string.IsNullOrEmpty(search) ||
                    l.LandedCostNumber.ToLower().Contains(search) ||
                    (l.Description != null && l.Description.ToLower().Contains(search))) &&
                (pagination.Status == null || (int)l.Status == pagination.Status));

        var total = await query.CountAsync();

        var items = await query
            .ApplyOrderNewestFirst(pagination.SortBy,
                string.IsNullOrWhiteSpace(pagination.SortBy) ? "desc" : pagination.SortDirection, "DocumentDate")
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return PaginatedResponse<LandedCostDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<LandedCostDto?> GetByIdAsync(Guid id)
    {
        var entity = await BaseQuery().FirstOrDefaultAsync(l => l.Id == id);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<LandedCostDto> CreateAsync(CreateLandedCostDto dto)
    {
        if (dto.CostLines is null || dto.CostLines.Count == 0)
            throw new InvalidOperationException("Add at least one cost line.");
        if (dto.GoodsReceiptIds is null || dto.GoodsReceiptIds.Count == 0)
            throw new InvalidOperationException("Link at least one goods receipt.");

        var lc = new LandedCost
        {
            LandedCostNumber = $"LC-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            Description      = dto.Description,
            VendorId         = dto.VendorId,
            Status           = LandedCostStatus.Draft,
            DocumentDate     = dto.DocumentDate,
            CurrencyCode     = dto.CurrencyCode,
            ExchangeRate     = 1m,
            Notes            = dto.Notes,
        };

        var lineNumber = 1;
        decimal total = 0;
        foreach (var c in dto.CostLines)
        {
            var taxAmount = c.Amount * c.TaxPercent / 100;
            total += c.Amount + taxAmount;
            lc.CostLines.Add(new LandedCostLine
            {
                LineNumber       = lineNumber++,
                CostType         = c.CostType,
                Description      = c.Description,
                Amount           = c.Amount,
                CurrencyCode     = dto.CurrencyCode,
                AllocationMethod = c.AllocationMethod,
                LedgerAccountId  = c.LedgerAccountId,
                TaxPercent       = c.TaxPercent,
                TaxAmount        = taxAmount,
                Notes            = c.Notes,
            });
        }

        foreach (var grnId in dto.GoodsReceiptIds.Distinct())
            lc.GoodsReceipts.Add(new LandedCostGoodsReceipt { GoodsReceiptId = grnId });

        lc.TotalLandedCostAmount = total;
        _ctx.LandedCosts.Add(lc);
        await _ctx.SaveChangesAsync();
        _logger.LogInformation("Created LandedCost {Number}", lc.LandedCostNumber);
        return (await GetByIdAsync(lc.Id))!;
    }

    public async Task<LandedCostDto> PostAsync(Guid id, Guid userId)
    {
        var lc = await _ctx.LandedCosts
            .Include(l => l.CostLines)
            .Include(l => l.GoodsReceipts)
            .Include(l => l.Allocations)
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted)
            ?? throw new KeyNotFoundException("Landed cost not found");

        if (lc.Status != LandedCostStatus.Draft)
            throw new InvalidOperationException("Only a draft landed cost can be posted.");

        // Gather the GRN lines covered (loaded individually to avoid the EF array.Contains issue).
        var grnLines = new List<GoodsReceiptLine>();
        var poUnitPrice = new Dictionary<Guid, decimal>();
        foreach (var link in lc.GoodsReceipts)
        {
            var grn = await _ctx.GoodsReceipts.Include(g => g.Lines).AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == link.GoodsReceiptId);
            if (grn is null) continue;
            grnLines.AddRange(grn.Lines);
            var po = await _ctx.PurchaseOrders.Include(o => o.Lines).AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == grn.PurchaseOrderId);
            if (po is not null)
                foreach (var pl in po.Lines) poUnitPrice[pl.Id] = pl.UnitPrice;
        }

        if (grnLines.Count == 0)
            throw new InvalidOperationException("The linked goods receipts have no lines to allocate to.");

        // Remove any prior allocations, then compute fresh ones per cost line.
        _ctx.LandedCostAllocations.RemoveRange(lc.Allocations);

        foreach (var costLine in lc.CostLines)
        {
            decimal Basis(GoodsReceiptLine l) => costLine.AllocationMethod switch
            {
                LandedCostAllocationMethod.ByQuantity => l.QuantityReceived,
                LandedCostAllocationMethod.ByValue    => l.QuantityReceived * poUnitPrice.GetValueOrDefault(l.PurchaseOrderLineId),
                LandedCostAllocationMethod.Equal      => 1m,
                _                                     => l.QuantityReceived, // ByWeight/ByVolume fallback
            };

            var totalBasis = grnLines.Sum(Basis);
            if (totalBasis <= 0) totalBasis = grnLines.Count; // avoid divide-by-zero → equal split

            foreach (var grnLine in grnLines)
            {
                var basis = costLine.AllocationMethod == LandedCostAllocationMethod.Equal ? 1m : Basis(grnLine);
                if (basis <= 0 && costLine.AllocationMethod != LandedCostAllocationMethod.Equal) continue;
                var pct = basis / totalBasis;
                var allocated = Math.Round(costLine.Amount * pct, 2);
                var perUnit = grnLine.QuantityReceived > 0 ? Math.Round(allocated / grnLine.QuantityReceived, 4) : 0;

                _ctx.LandedCostAllocations.Add(new LandedCostAllocation
                {
                    LandedCostId          = lc.Id,
                    LandedCostLineId      = costLine.Id,
                    GoodsReceiptLineId    = grnLine.Id,
                    AllocationMethod      = costLine.AllocationMethod,
                    AllocationBasisValue  = basis,
                    TotalBasisValue       = totalBasis,
                    AllocationPercent     = Math.Round(pct * 100, 2),
                    AllocatedAmount       = allocated,
                    AllocatedAmountPerUnit = perUnit,
                });
            }
        }

        lc.Status = LandedCostStatus.Posted;
        lc.PostedAt = DateTime.UtcNow;
        lc.PostedByUserId = userId;
        await _ctx.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<LandedCostDto> CancelAsync(Guid id)
    {
        var lc = await Tracked(id);
        if (lc.Status == LandedCostStatus.Posted)
            throw new InvalidOperationException("A posted landed cost cannot be cancelled.");
        lc.Status = LandedCostStatus.Cancelled;
        await _ctx.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task DeleteAsync(Guid id)
    {
        var lc = await Tracked(id);
        if (lc.Status == LandedCostStatus.Posted)
            throw new InvalidOperationException("A posted landed cost cannot be deleted.");
        lc.IsDeleted = true; lc.DeletedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();
    }

    private async Task<LandedCost> Tracked(Guid id)
        => await _ctx.LandedCosts.FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted)
            ?? throw new KeyNotFoundException("Landed cost not found");

    private static LandedCostDto MapToDto(LandedCost l) => new()
    {
        Id = l.Id, LandedCostNumber = l.LandedCostNumber, Description = l.Description,
        VendorId = l.VendorId, VendorName = l.Vendor?.Name, Status = l.Status,
        DocumentDate = l.DocumentDate, PostedAt = l.PostedAt, CurrencyCode = l.CurrencyCode,
        ExchangeRate = l.ExchangeRate, TotalLandedCostAmount = l.TotalLandedCostAmount, Notes = l.Notes,
        CostLines = l.CostLines.OrderBy(c => c.LineNumber).Select(c => new LandedCostLineDto
        {
            Id = c.Id, LineNumber = c.LineNumber, CostType = c.CostType, Description = c.Description,
            Amount = c.Amount, CurrencyCode = c.CurrencyCode, AllocationMethod = c.AllocationMethod,
            TaxPercent = c.TaxPercent, TaxAmount = c.TaxAmount, Notes = c.Notes,
        }).ToList(),
        GoodsReceipts = l.GoodsReceipts.Select(g => new LandedCostGoodsReceiptDto
        {
            Id = g.Id, GoodsReceiptId = g.GoodsReceiptId, GoodsReceiptNumber = g.GoodsReceipt?.ReceiptNumber,
        }).ToList(),
        Allocations = l.Allocations.Select(a => new LandedCostAllocationDto
        {
            Id = a.Id, LandedCostLineId = a.LandedCostLineId, GoodsReceiptLineId = a.GoodsReceiptLineId,
            ItemDescription = a.GoodsReceiptLine?.ItemDescription, AllocationMethod = a.AllocationMethod,
            AllocationBasisValue = a.AllocationBasisValue, TotalBasisValue = a.TotalBasisValue,
            AllocationPercent = a.AllocationPercent, AllocatedAmount = a.AllocatedAmount, AllocatedAmountPerUnit = a.AllocatedAmountPerUnit,
        }).ToList(),
    };
}
