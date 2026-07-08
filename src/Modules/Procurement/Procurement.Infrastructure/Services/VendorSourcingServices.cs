using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;
using Procurement.Domain.Entities;
using Procurement.Infrastructure.Persistence;

namespace Procurement.Infrastructure.Services;

// ── Approved Vendor List (AVL) ──────────────────────────────────────────────────

public class ApprovedVendorListService : IApprovedVendorListService
{
    private readonly ProcurementDbContext _ctx;
    private readonly ILogger<ApprovedVendorListService> _logger;

    public ApprovedVendorListService(ProcurementDbContext ctx, ILogger<ApprovedVendorListService> logger)
    {
        _ctx    = ctx;
        _logger = logger;
    }

    private IQueryable<ApprovedVendorList> BaseQuery()
        => _ctx.ApprovedVendorLists
            .AsNoTracking()
            .Include(a => a.Vendor)
            .Include(a => a.ProcurementCategory)
            .Where(a => !a.IsDeleted);

    public async Task<PaginatedResponse<ApprovedVendorListDto>> GetAllAsync(PaginationParams pagination)
    {
        var search = pagination.SearchTerm?.Trim().ToLower();

        var query = _ctx.ApprovedVendorLists
            .AsNoTracking()
            .Where(a => !a.IsDeleted
                && (string.IsNullOrEmpty(search)
                    || (a.ItemCode != null && a.ItemCode.ToLower().Contains(search))
                    || (a.ItemDescription != null && a.ItemDescription.ToLower().Contains(search))));

        var total = await query.CountAsync();

        // Eager-load Vendor + Category so the Vendor #, Vendor and Scope columns populate — count above stays join-free.
        var items = await query
            .Include(a => a.Vendor)
            .Include(a => a.ProcurementCategory)
            .ApplyOrderNewestFirst(pagination.SortBy,
                string.IsNullOrWhiteSpace(pagination.SortBy) ? "desc" : pagination.SortDirection, "CreatedAt")
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return PaginatedResponse<ApprovedVendorListDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<List<ApprovedVendorListDto>> GetByVendorAsync(Guid vendorId)
        => (await BaseQuery().Where(a => a.VendorId == vendorId).ToListAsync())
            .Select(MapToDto).ToList();

    public async Task<List<ApprovedVendorListDto>> GetByItemAsync(Guid itemId)
        => (await BaseQuery().Where(a => a.ItemId == itemId).ToListAsync())
            .Select(MapToDto).ToList();

    public async Task<List<ApprovedVendorListDto>> GetByCategoryAsync(Guid categoryId)
        => (await BaseQuery().Where(a => a.ProcurementCategoryId == categoryId).ToListAsync())
            .Select(MapToDto).ToList();

    public async Task<ApprovedVendorListDto?> GetByIdAsync(Guid id)
    {
        var entity = await BaseQuery().FirstOrDefaultAsync(a => a.Id == id);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<ApprovedVendorListDto> CreateAsync(CreateApprovedVendorListDto dto, Guid userId)
    {
        if (dto.ItemId is null && dto.ProcurementCategoryId is null)
            throw new InvalidOperationException("An AVL entry must target either an item or a procurement category.");

        var vendorExists = await _ctx.Vendors.AnyAsync(v => v.Id == dto.VendorId && !v.IsDeleted);
        if (!vendorExists)
            throw new KeyNotFoundException($"Vendor {dto.VendorId} not found");

        // Enforce single preferred vendor per scope
        if (dto.IsPreferred)
            await ClearExistingPreferredAsync(dto.ItemId, dto.ProcurementCategoryId, null);

        var entity = new ApprovedVendorList
        {
            ItemId                    = dto.ItemId,
            ItemCode                  = dto.ItemCode,
            ItemDescription           = dto.ItemDescription,
            ProcurementCategoryId     = dto.ProcurementCategoryId,
            VendorId                  = dto.VendorId,
            ValidFrom                 = dto.ValidFrom,
            ValidTo                   = dto.ValidTo,
            IsActive                  = true,
            IsPreferred               = dto.IsPreferred,
            IsExclusive               = dto.IsExclusive,
            DefaultUnitPrice          = dto.DefaultUnitPrice,
            CurrencyCode              = dto.CurrencyCode,
            LeadTimeDays              = dto.LeadTimeDays,
            MinimumOrderQuantity      = dto.MinimumOrderQuantity,
            UnitOfMeasureId           = dto.UnitOfMeasureId,
            RequiresQualityInspection = dto.RequiresQualityInspection,
            ApprovedByUserId          = userId,
            ApprovedAt                = DateTime.UtcNow,
            Notes                     = dto.Notes,
        };

        _ctx.ApprovedVendorLists.Add(entity);
        await _ctx.SaveChangesAsync();
        _logger.LogInformation("Created AVL entry {Id} for vendor {VendorId}", entity.Id, entity.VendorId);
        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<ApprovedVendorListDto> UpdateAsync(Guid id, UpdateApprovedVendorListDto dto)
    {
        var entity = await _ctx.ApprovedVendorLists
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted)
            ?? throw new KeyNotFoundException($"AVL entry {id} not found");

        if (dto.ItemCode is not null)              entity.ItemCode                  = dto.ItemCode;
        if (dto.ItemDescription is not null)       entity.ItemDescription           = dto.ItemDescription;
        if (dto.ProcurementCategoryId.HasValue)    entity.ProcurementCategoryId     = dto.ProcurementCategoryId;
        if (dto.ValidFrom.HasValue)                entity.ValidFrom                 = dto.ValidFrom.Value;
        if (dto.ValidTo.HasValue)                  entity.ValidTo                   = dto.ValidTo;
        if (dto.IsActive.HasValue)                 entity.IsActive                  = dto.IsActive.Value;
        if (dto.IsExclusive.HasValue)              entity.IsExclusive               = dto.IsExclusive.Value;
        if (dto.DefaultUnitPrice.HasValue)         entity.DefaultUnitPrice          = dto.DefaultUnitPrice;
        if (dto.CurrencyCode is not null)          entity.CurrencyCode              = dto.CurrencyCode;
        if (dto.LeadTimeDays.HasValue)             entity.LeadTimeDays              = dto.LeadTimeDays.Value;
        if (dto.MinimumOrderQuantity.HasValue)     entity.MinimumOrderQuantity      = dto.MinimumOrderQuantity;
        if (dto.RequiresQualityInspection.HasValue) entity.RequiresQualityInspection = dto.RequiresQualityInspection.Value;
        if (dto.Notes is not null)                 entity.Notes                     = dto.Notes;

        if (dto.IsPreferred.HasValue)
        {
            if (dto.IsPreferred.Value && !entity.IsPreferred)
                await ClearExistingPreferredAsync(entity.ItemId, entity.ProcurementCategoryId, entity.Id);
            entity.IsPreferred = dto.IsPreferred.Value;
        }

        _ctx.ApprovedVendorLists.Update(entity);
        await _ctx.SaveChangesAsync();
        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<ApprovedVendorListDto> BlockAsync(Guid id, BlockApprovedVendorDto dto)
    {
        var entity = await _ctx.ApprovedVendorLists
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted)
            ?? throw new KeyNotFoundException($"AVL entry {id} not found");

        entity.IsBlocked   = true;
        entity.BlockReason = dto.BlockReason;
        entity.IsPreferred = false;
        _ctx.ApprovedVendorLists.Update(entity);
        await _ctx.SaveChangesAsync();
        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<ApprovedVendorListDto> UnblockAsync(Guid id)
    {
        var entity = await _ctx.ApprovedVendorLists
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted)
            ?? throw new KeyNotFoundException($"AVL entry {id} not found");

        entity.IsBlocked   = false;
        entity.BlockReason = null;
        _ctx.ApprovedVendorLists.Update(entity);
        await _ctx.SaveChangesAsync();
        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _ctx.ApprovedVendorLists
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted)
            ?? throw new KeyNotFoundException($"AVL entry {id} not found");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        _ctx.ApprovedVendorLists.Update(entity);
        await _ctx.SaveChangesAsync();
    }

    private async Task ClearExistingPreferredAsync(Guid? itemId, Guid? categoryId, Guid? excludeId)
    {
        var existing = await _ctx.ApprovedVendorLists
            .Where(a => !a.IsDeleted && a.IsPreferred
                     && a.ItemId == itemId
                     && a.ProcurementCategoryId == categoryId
                     && (excludeId == null || a.Id != excludeId))
            .ToListAsync();
        foreach (var e in existing) e.IsPreferred = false;
    }

    private static ApprovedVendorListDto MapToDto(ApprovedVendorList a) => new()
    {
        Id                        = a.Id,
        ItemId                    = a.ItemId,
        ItemCode                  = a.ItemCode,
        ItemDescription           = a.ItemDescription,
        ProcurementCategoryId     = a.ProcurementCategoryId,
        ProcurementCategoryName   = a.ProcurementCategory?.Name,
        VendorId                  = a.VendorId,
        VendorName                = a.Vendor?.Name ?? string.Empty,
        VendorNumber              = a.Vendor?.VendorNumber ?? string.Empty,
        ValidFrom                 = a.ValidFrom,
        ValidTo                   = a.ValidTo,
        IsActive                  = a.IsActive,
        IsPreferred               = a.IsPreferred,
        IsExclusive               = a.IsExclusive,
        IsBlocked                 = a.IsBlocked,
        BlockReason               = a.BlockReason,
        DefaultUnitPrice          = a.DefaultUnitPrice,
        CurrencyCode              = a.CurrencyCode,
        LeadTimeDays              = a.LeadTimeDays,
        MinimumOrderQuantity      = a.MinimumOrderQuantity,
        UnitOfMeasureId           = a.UnitOfMeasureId,
        RequiresQualityInspection = a.RequiresQualityInspection,
        ApprovedByUserId          = a.ApprovedByUserId,
        ApprovedAt                = a.ApprovedAt,
        Notes                     = a.Notes,
    };
}

// ── Vendor Performance ──────────────────────────────────────────────────────────

public class VendorPerformanceService : IVendorPerformanceService
{
    private readonly ProcurementDbContext _ctx;
    private readonly ILogger<VendorPerformanceService> _logger;

    public VendorPerformanceService(ProcurementDbContext ctx, ILogger<VendorPerformanceService> logger)
    {
        _ctx    = ctx;
        _logger = logger;
    }

    private IQueryable<VendorPerformance> BaseQuery()
        => _ctx.VendorPerformances
            .AsNoTracking()
            .Include(p => p.Vendor)
            .Where(p => !p.IsDeleted);

    public async Task<List<VendorPerformanceDto>> GetAllAsync()
        => (await BaseQuery().OrderByDescending(p => p.PeriodTo).ToListAsync())
            .Select(MapToDto).ToList();

    public async Task<List<VendorPerformanceDto>> GetByVendorAsync(Guid vendorId)
        => (await BaseQuery().Where(p => p.VendorId == vendorId)
                .OrderByDescending(p => p.PeriodTo).ToListAsync())
            .Select(MapToDto).ToList();

    public async Task<VendorPerformanceDto?> GetByIdAsync(Guid id)
    {
        var entity = await BaseQuery().FirstOrDefaultAsync(p => p.Id == id);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<VendorPerformanceDto> CreateAsync(CreateVendorPerformanceDto dto, Guid userId)
    {
        if (dto.PeriodTo < dto.PeriodFrom)
            throw new InvalidOperationException("Period end must be on or after period start.");

        var vendorExists = await _ctx.Vendors.AnyAsync(v => v.Id == dto.VendorId && !v.IsDeleted);
        if (!vendorExists)
            throw new KeyNotFoundException($"Vendor {dto.VendorId} not found");

        var entity = new VendorPerformance
        {
            VendorId              = dto.VendorId,
            PeriodFrom            = dto.PeriodFrom,
            PeriodTo              = dto.PeriodTo,
            OnTimeDeliveryRate    = dto.OnTimeDeliveryRate,
            QualityScore          = dto.QualityScore,
            PriceComplianceRate   = dto.PriceComplianceRate,
            ResponsivenessScore   = dto.ResponsivenessScore,
            DocumentAccuracyScore = dto.DocumentAccuracyScore,
            TotalOrders           = dto.TotalOrders,
            LateDeliveries        = dto.LateDeliveries,
            QualityRejections     = dto.QualityRejections,
            InvoiceDiscrepancies  = dto.InvoiceDiscrepancies,
            TotalPurchaseValue    = dto.TotalPurchaseValue,
            EvaluatedByUserId     = userId,
            EvaluatedAt           = DateTime.UtcNow,
            Comments              = dto.Comments,
        };
        entity.OverallRating = ComputeOverall(entity);

        _ctx.VendorPerformances.Add(entity);
        await _ctx.SaveChangesAsync();

        // Roll up the latest scorecard onto the vendor master for quick reference.
        await SyncVendorSnapshotAsync(entity.VendorId);

        _logger.LogInformation("Created VendorPerformance {Id} for vendor {VendorId}", entity.Id, entity.VendorId);
        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<VendorPerformanceDto> UpdateAsync(Guid id, UpdateVendorPerformanceDto dto)
    {
        var entity = await _ctx.VendorPerformances
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted)
            ?? throw new KeyNotFoundException($"VendorPerformance {id} not found");

        if (dto.PeriodFrom.HasValue)            entity.PeriodFrom            = dto.PeriodFrom.Value;
        if (dto.PeriodTo.HasValue)              entity.PeriodTo              = dto.PeriodTo.Value;
        if (dto.OnTimeDeliveryRate.HasValue)    entity.OnTimeDeliveryRate    = dto.OnTimeDeliveryRate.Value;
        if (dto.QualityScore.HasValue)          entity.QualityScore          = dto.QualityScore.Value;
        if (dto.PriceComplianceRate.HasValue)   entity.PriceComplianceRate   = dto.PriceComplianceRate.Value;
        if (dto.ResponsivenessScore.HasValue)   entity.ResponsivenessScore   = dto.ResponsivenessScore.Value;
        if (dto.DocumentAccuracyScore.HasValue) entity.DocumentAccuracyScore = dto.DocumentAccuracyScore.Value;
        if (dto.TotalOrders.HasValue)           entity.TotalOrders           = dto.TotalOrders.Value;
        if (dto.LateDeliveries.HasValue)        entity.LateDeliveries        = dto.LateDeliveries.Value;
        if (dto.QualityRejections.HasValue)     entity.QualityRejections     = dto.QualityRejections.Value;
        if (dto.InvoiceDiscrepancies.HasValue)  entity.InvoiceDiscrepancies  = dto.InvoiceDiscrepancies.Value;
        if (dto.TotalPurchaseValue.HasValue)    entity.TotalPurchaseValue    = dto.TotalPurchaseValue.Value;
        if (dto.Comments is not null)           entity.Comments              = dto.Comments;

        if (entity.PeriodTo < entity.PeriodFrom)
            throw new InvalidOperationException("Period end must be on or after period start.");

        entity.OverallRating = ComputeOverall(entity);

        _ctx.VendorPerformances.Update(entity);
        await _ctx.SaveChangesAsync();
        await SyncVendorSnapshotAsync(entity.VendorId);
        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _ctx.VendorPerformances
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted)
            ?? throw new KeyNotFoundException($"VendorPerformance {id} not found");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        _ctx.VendorPerformances.Update(entity);
        await _ctx.SaveChangesAsync();
    }

    /// <summary>Weighted overall score: OnTime 30%, Quality 30%, Price 20%, Responsiveness 10%, DocAccuracy 10%.</summary>
    private static decimal ComputeOverall(VendorPerformance p)
        => Math.Round(
            p.OnTimeDeliveryRate    * 0.30m +
            p.QualityScore          * 0.30m +
            p.PriceComplianceRate   * 0.20m +
            p.ResponsivenessScore   * 0.10m +
            p.DocumentAccuracyScore * 0.10m, 2);

    /// <summary>Copies the most recent scorecard's headline KPIs onto the Vendor master record.</summary>
    private async Task SyncVendorSnapshotAsync(Guid vendorId)
    {
        var latest = await _ctx.VendorPerformances
            .Where(p => p.VendorId == vendorId && !p.IsDeleted)
            .OrderByDescending(p => p.PeriodTo)
            .FirstOrDefaultAsync();
        if (latest is null) return;

        var vendor = await _ctx.Vendors.FirstOrDefaultAsync(v => v.Id == vendorId && !v.IsDeleted);
        if (vendor is null) return;

        vendor.OverallRating      = latest.OverallRating;
        vendor.OnTimeDeliveryRate = latest.OnTimeDeliveryRate;
        vendor.QualityScore       = latest.QualityScore;
        _ctx.Vendors.Update(vendor);
        await _ctx.SaveChangesAsync();
    }

    private static VendorPerformanceDto MapToDto(VendorPerformance p) => new()
    {
        Id                    = p.Id,
        VendorId              = p.VendorId,
        VendorName            = p.Vendor?.Name ?? string.Empty,
        VendorNumber          = p.Vendor?.VendorNumber ?? string.Empty,
        PeriodFrom            = p.PeriodFrom,
        PeriodTo              = p.PeriodTo,
        OnTimeDeliveryRate    = p.OnTimeDeliveryRate,
        QualityScore          = p.QualityScore,
        PriceComplianceRate   = p.PriceComplianceRate,
        ResponsivenessScore   = p.ResponsivenessScore,
        DocumentAccuracyScore = p.DocumentAccuracyScore,
        OverallRating         = p.OverallRating,
        TotalOrders           = p.TotalOrders,
        LateDeliveries        = p.LateDeliveries,
        QualityRejections     = p.QualityRejections,
        InvoiceDiscrepancies  = p.InvoiceDiscrepancies,
        TotalPurchaseValue    = p.TotalPurchaseValue,
        EvaluatedByUserId     = p.EvaluatedByUserId,
        EvaluatedAt           = p.EvaluatedAt,
        Comments              = p.Comments,
    };
}
