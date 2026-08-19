using Distribution.Application.DTOs;
using Distribution.Application.Services.Interfaces;
using Distribution.Domain.Entities;
using Distribution.Domain.Enums;
using Distribution.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;

namespace Distribution.Infrastructure.Services;

/// <summary>
/// Demand forecasting, replenishment and stock transfers.
///
/// The default forecast basis is <see cref="ForecastBasis.SecondarySalesHistory"/> and that choice
/// is the whole point of the service. Forecasting on primary sales forecasts your own pipeline
/// stuffing: you replenish the warehouse for demand that only ever existed on a distributor's
/// purchase order, and the correction arrives three months later as an expiry claim.
///
/// Safety stock is derived from demand variability rather than a flat rule of thumb, because a
/// steady SKU and an erratic one with the same average need very different buffers.
/// </summary>
public class PlanningService(
    DistributionDbContext db,
    IDistributionTenant tenant,
    DistributionNumbering numbering) : IPlanningService
{
    // ═══ Forecasting ═════════════════════════════════════════════════════════

    public async Task<ForecastDto> GenerateForecastAsync(GenerateForecastDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("A forecast needs a name.");
        if (request.PeriodEnd < request.PeriodStart)
            throw new InvalidOperationException("The forecast's end date is before its start date.");

        var months = request.HistoryMonths <= 0 ? 6 : request.HistoryMonths;
        var historyFrom = request.PeriodStart.Date.AddMonths(-months);
        var historyTo = request.PeriodStart.Date.AddDays(-1);

        var forecast = new DemandForecast
        {
            Name = request.Name.Trim(),
            Basis = request.Basis,
            PeriodStart = request.PeriodStart.Date,
            PeriodEnd = request.PeriodEnd.Date,
            WarehouseId = request.WarehouseId,
            PartnerId = request.PartnerId,
            TerritoryId = request.TerritoryId,
            HistoryMonths = months,
            SeasonalityFactor = request.SeasonalityFactor <= 0 ? 1 : request.SeasonalityFactor,
            TrendFactor = request.TrendFactor <= 0 ? 1 : request.TrendFactor,
            GeneratedAt = DateTime.UtcNow,
            Note = request.Note,
        }.StampNew(tenant, userId);

        var history = request.Basis == ForecastBasis.PrimarySalesHistory
            ? await PrimaryHistoryAsync(request, historyFrom, historyTo)
            : await SecondaryHistoryAsync(request, historyFrom, historyTo);

        if (request.ItemIds.Count > 0)
            history = history.Where(h => request.ItemIds.Contains(h.ItemId)).ToList();

        var periodDays = Math.Max(1, (request.PeriodEnd.Date - request.PeriodStart.Date).Days + 1);
        var historyDays = Math.Max(1, (historyTo - historyFrom).Days + 1);

        foreach (var row in history)
        {
            var dailyAverage = row.TotalQuantity / historyDays;
            var computed = Math.Round(dailyAverage * periodDays
                                      * forecast.SeasonalityFactor * forecast.TrendFactor, 4);

            forecast.Lines.Add(new ForecastLine
            {
                ForecastId = forecast.Id,
                ItemId = row.ItemId,
                ItemName = row.ItemName,
                BrandId = row.BrandId,
                Uom = row.Uom,
                HistoricAverage = Math.Round(dailyAverage * 30, 4),
                ComputedQuantity = computed,
                FinalQuantity = computed,
                ForecastValue = Math.Round(computed * row.AverageUnitPrice, 4),
                DemandStdDeviation = row.StdDeviation,
            }.StampNew(tenant, userId));
        }

        if (forecast.Lines.Count == 0)
            throw new InvalidOperationException(
                "There is no sales history in that window to forecast from.");

        forecast.TotalForecastQuantity = forecast.Lines.Sum(l => l.FinalQuantity);
        forecast.TotalForecastValue = forecast.Lines.Sum(l => l.ForecastValue);

        db.Forecasts.Add(forecast);
        await db.SaveChangesAsync();

        return (await GetForecastAsync(forecast.Id))!;
    }

    public async Task<ForecastDto?> GetForecastAsync(Guid forecastId)
    {
        var entity = await db.Forecasts.ForTenant(tenant)
            .Include(f => f.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(f => f.Id == forecastId);

        if (entity is null) return null;

        var dto = entity.ToDto();

        if (entity.PartnerId.HasValue)
            dto.PartnerName = await db.Partners.ForTenant(tenant)
                .Where(p => p.Id == entity.PartnerId).Select(p => p.Name).FirstOrDefaultAsync();

        return dto;
    }

    public async Task<PaginatedResponse<ForecastDto>> ListForecastsAsync(
        Guid? partnerId, Guid? warehouseId, DateTime? periodStart, PaginationParams pagination)
    {
        var query = db.Forecasts.ForTenant(tenant)
            .WhereIf(partnerId.HasValue, f => f.PartnerId == partnerId)
            .WhereIf(warehouseId.HasValue, f => f.WarehouseId == warehouseId)
            .WhereIf(periodStart.HasValue, f => f.PeriodStart == periodStart!.Value.Date);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(f => f.PeriodStart)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var dtos = rows.Select(r =>
        {
            var dto = r.ToDto();
            dto.Lines = [];
            return dto;
        });

        return PaginatedResponse<ForecastDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<ForecastDto> OverrideLineAsync(OverrideForecastLineDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason))
            throw new InvalidOperationException(
                "An override needs a rationale — a number nobody can explain is worse than the maths.");

        var line = await db.ForecastLines.ForTenant(tenant)
            .Include(l => l.Forecast)
            .FirstOrDefaultAsync(l => l.Id == request.LineId)
            ?? throw new InvalidOperationException("That forecast line no longer exists.");

        if (line.Forecast?.IsApproved == true)
            throw new InvalidOperationException("This forecast is approved and can no longer be changed.");

        line.OverrideQuantity = request.OverrideQuantity;
        line.OverrideReason = request.OverrideReason;
        line.FinalQuantity = request.OverrideQuantity;
        line.ForecastValue = line.ComputedQuantity == 0
            ? line.ForecastValue
            : Math.Round(line.ForecastValue / line.ComputedQuantity * request.OverrideQuantity, 4);
        line.StampUpdated(userId);

        await db.SaveChangesAsync();

        var forecast = await db.Forecasts.ForTenant(tenant)
            .Include(f => f.Lines.Where(l => !l.IsDeleted))
            .FirstAsync(f => f.Id == line.ForecastId);

        forecast.TotalForecastQuantity = forecast.Lines.Sum(l => l.FinalQuantity);
        forecast.TotalForecastValue = forecast.Lines.Sum(l => l.ForecastValue);
        forecast.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetForecastAsync(line.ForecastId))!;
    }

    public async Task<ForecastDto> ApproveForecastAsync(Guid forecastId, Guid userId)
    {
        var entity = await db.Forecasts.ForTenant(tenant).FirstOrDefaultAsync(f => f.Id == forecastId)
            ?? throw new InvalidOperationException("That forecast no longer exists.");

        entity.IsApproved = true;
        entity.ApprovedAt = DateTime.UtcNow;
        entity.ApprovedByUserId = userId;
        entity.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetForecastAsync(forecastId))!;
    }

    // ═══ Replenishment ═══════════════════════════════════════════════════════

    public async Task<int> GenerateSuggestionsAsync(
        ReplenishmentTargetKind targetKind, Guid? scopeId, Guid userId)
    {
        // Clear the unactioned queue first: a suggestion computed last week against last week's
        // stock is noise, and stale rows are how a planner learns to ignore the screen.
        await db.ReplenishmentSuggestions.ForTenant(tenant)
            .Where(s => s.TargetKind == targetKind && !s.IsActioned && !s.IsDismissed)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted, true));

        var written = targetKind switch
        {
            ReplenishmentTargetKind.Distributor => await SuggestForDistributorsAsync(scopeId, userId),
            ReplenishmentTargetKind.Van => await SuggestForVansAsync(scopeId, userId),
            _ => await SuggestForWarehousesAsync(scopeId, userId),
        };

        await db.SaveChangesAsync();
        return written;
    }

    public async Task<PaginatedResponse<ReplenishmentSuggestionDto>> ListSuggestionsAsync(
        ReplenishmentTargetKind? targetKind, Guid? partnerId, Guid? warehouseId, Guid? vanUnitId,
        bool? openOnly, PaginationParams pagination)
    {
        var query = db.ReplenishmentSuggestions.ForTenant(tenant)
            .WhereIf(targetKind.HasValue, s => s.TargetKind == targetKind)
            .WhereIf(partnerId.HasValue, s => s.PartnerId == partnerId)
            .WhereIf(warehouseId.HasValue, s => s.WarehouseId == warehouseId)
            .WhereIf(vanUnitId.HasValue, s => s.VanUnitId == vanUnitId)
            .WhereIf(openOnly == true, s => !s.IsActioned && !s.IsDismissed);

        var total = await query.CountAsync();
        var rows = await query
            // Most urgent first: the queue is a work list, not a catalogue.
            .OrderByDescending(s => s.UrgencyScore).ThenByDescending(s => s.EstimatedValue)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var dtos = rows.Select(r => r.ToDto()).ToList();

        var partnerIds = rows.Where(r => r.PartnerId.HasValue).Select(r => r.PartnerId!.Value).Distinct().ToList();
        var vanIds = rows.Where(r => r.VanUnitId.HasValue).Select(r => r.VanUnitId!.Value).Distinct().ToList();

        var partners = await db.Partners.ForTenant(tenant)
            .Where(p => partnerIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name);
        var vans = await db.VanUnits.ForTenant(tenant)
            .Where(v => vanIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id, v => v.Name);

        foreach (var dto in dtos)
        {
            if (dto.PartnerId.HasValue) dto.PartnerName = partners.GetValueOrDefault(dto.PartnerId.Value);
            if (dto.VanUnitId.HasValue) dto.VanUnitName = vans.GetValueOrDefault(dto.VanUnitId.Value);
        }

        return PaginatedResponse<ReplenishmentSuggestionDto>.Ok(
            dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<ReplenishmentSuggestionDto> DismissSuggestionAsync(Guid id, string reason, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Dismissing a suggestion needs a reason.");

        var entity = await db.ReplenishmentSuggestions.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new InvalidOperationException("That suggestion no longer exists.");

        entity.IsDismissed = true;
        entity.DismissReason = reason;
        entity.StampUpdated(userId);

        await db.SaveChangesAsync();
        return entity.ToDto();
    }

    public async Task<TransferRequestDto> CreateTransferAsync(List<Guid> suggestionIds, Guid userId)
    {
        if (suggestionIds.Count == 0)
            throw new InvalidOperationException("Choose at least one suggestion to act on.");

        var suggestions = await db.ReplenishmentSuggestions.ForTenant(tenant)
            .Where(s => suggestionIds.Contains(s.Id) && !s.IsActioned)
            .ToListAsync();

        if (suggestions.Count == 0)
            throw new InvalidOperationException("Those suggestions have already been actioned.");

        var destinations = suggestions
            .Select(s => s.PartnerId ?? s.VanUnitId ?? s.WarehouseId)
            .Distinct().ToList();

        if (destinations.Count > 1)
            throw new InvalidOperationException("Only suggestions for one destination can go on a single transfer.");

        var first = suggestions[0];

        var transfer = new StockTransferRequest
        {
            TransferNumber = await numbering.NextTransferNumberAsync(DateTime.UtcNow),
            SourceWarehouseId = first.SourceWarehouseId,
            DestinationWarehouseId = first.TargetKind == ReplenishmentTargetKind.Warehouse ? first.WarehouseId : null,
            DestinationPartnerId = first.PartnerId,
            DestinationVanUnitId = first.VanUnitId,
            Status = TransferRequestStatus.Requested,
            RequestedOn = DateTime.UtcNow,
            RequiredBy = DateTime.UtcNow.Date.AddDays(2),
            LineCount = suggestions.Count,
            TotalQuantity = suggestions.Sum(s => s.SuggestedQuantity),
            TotalValue = suggestions.Sum(s => s.EstimatedValue),
            Note = $"Raised from {suggestions.Count} replenishment suggestion(s).",
        }.StampNew(tenant, userId);

        db.TransferRequests.Add(transfer);

        foreach (var suggestion in suggestions)
        {
            suggestion.IsActioned = true;
            suggestion.ActionedAt = DateTime.UtcNow;
            suggestion.ResultingTransferId = transfer.Id;
            suggestion.StampUpdated(userId);
        }

        await db.SaveChangesAsync();
        return transfer.ToDto();
    }

    public async Task<PaginatedResponse<TransferRequestDto>> ListTransfersAsync(
        TransferRequestStatus? status, Guid? partnerId, DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.TransferRequests.ForTenant(tenant)
            .WhereIf(status.HasValue, t => t.Status == status)
            .WhereIf(partnerId.HasValue, t => t.DestinationPartnerId == partnerId)
            .WhereIf(from.HasValue, t => t.RequestedOn >= from)
            .WhereIf(to.HasValue, t => t.RequestedOn <= to);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(t => t.RequestedOn)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var dtos = rows.Select(r => r.ToDto()).ToList();

        var partnerIds = rows.Where(r => r.DestinationPartnerId.HasValue)
            .Select(r => r.DestinationPartnerId!.Value).Distinct().ToList();

        var partners = await db.Partners.ForTenant(tenant)
            .Where(p => partnerIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name);

        foreach (var dto in dtos.Where(d => d.DestinationPartnerId.HasValue))
            dto.DestinationPartnerName = partners.GetValueOrDefault(dto.DestinationPartnerId!.Value);

        return PaginatedResponse<TransferRequestDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<TransferRequestDto> DecideTransferAsync(
        Guid id, bool isApproved, string? reason, Guid userId)
    {
        var entity = await db.TransferRequests.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new InvalidOperationException("That transfer no longer exists.");

        if (entity.Status >= TransferRequestStatus.InTransit)
            throw new InvalidOperationException("This transfer is already on its way.");

        if (isApproved)
        {
            entity.Status = TransferRequestStatus.Approved;
            entity.ApprovedAt = DateTime.UtcNow;
            entity.ApprovedByUserId = userId;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new InvalidOperationException("Rejecting a transfer needs a reason.");

            entity.Status = TransferRequestStatus.Rejected;
            entity.RejectionReason = reason;

            // Free the suggestions so the planner can act on them another way.
            await db.ReplenishmentSuggestions.ForTenant(tenant)
                .Where(s => s.ResultingTransferId == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.IsActioned, false)
                    .SetProperty(x => x.ResultingTransferId, (Guid?)null));
        }

        entity.StampUpdated(userId);
        await db.SaveChangesAsync();

        return entity.ToDto();
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    private record HistoryRow(
        Guid ItemId, string ItemName, Guid? BrandId, string Uom,
        decimal TotalQuantity, decimal AverageUnitPrice, decimal StdDeviation);

    /// <summary>
    /// Real demand: what distributors actually sold onward. The standard deviation across the
    /// monthly buckets is what drives safety stock later.
    /// </summary>
    private async Task<List<HistoryRow>> SecondaryHistoryAsync(
        GenerateForecastDto request, DateTime from, DateTime to)
    {
        var rows = await db.SecondarySaleLines.ForTenant(tenant)
            .Where(l => l.ItemId != null && db.SecondarySales.ForTenant(tenant).Any(s =>
                s.Id == l.SecondarySaleId && s.SaleDate >= from && s.SaleDate <= to && s.IsMapped
                && (request.PartnerId == null || s.PartnerId == request.PartnerId)
                && (request.TerritoryId == null || s.TerritoryId == request.TerritoryId)))
            .Select(l => new
            {
                ItemId = l.ItemId!.Value,
                l.ItemName,
                l.BrandId,
                l.Uom,
                l.BaseQuantity,
                l.UnitPrice,
                Month = db.SecondarySales.ForTenant(tenant)
                    .Where(s => s.Id == l.SecondarySaleId).Select(s => s.SaleDate.Month).FirstOrDefault(),
            })
            .ToListAsync();

        return Aggregate(rows.Select(r =>
            (r.ItemId, r.ItemName ?? string.Empty, r.BrandId, r.Uom, r.BaseQuantity, r.UnitPrice, r.Month)));
    }

    private async Task<List<HistoryRow>> PrimaryHistoryAsync(
        GenerateForecastDto request, DateTime from, DateTime to)
    {
        var rows = await db.OrderLines.ForTenant(tenant)
            .Where(l => !l.IsFreeGoods && db.Orders.ForTenant(tenant).Any(o =>
                o.Id == l.OrderId && o.OrderDate >= from && o.OrderDate <= to
                && o.Status != DistributionOrderStatus.Cancelled
                && o.Status != DistributionOrderStatus.Rejected
                && o.Status != DistributionOrderStatus.Draft
                && (request.PartnerId == null || o.PartnerId == request.PartnerId)
                && (request.WarehouseId == null || o.WarehouseId == request.WarehouseId)
                && (request.TerritoryId == null || o.TerritoryId == request.TerritoryId)))
            .Select(l => new
            {
                l.ItemId,
                l.ItemName,
                l.BrandId,
                l.Uom,
                l.BaseQuantity,
                l.UnitPrice,
                Month = db.Orders.ForTenant(tenant)
                    .Where(o => o.Id == l.OrderId).Select(o => o.OrderDate.Month).FirstOrDefault(),
            })
            .ToListAsync();

        return Aggregate(rows.Select(r =>
            (r.ItemId, r.ItemName, r.BrandId, r.Uom, r.BaseQuantity, r.UnitPrice, r.Month)));
    }

    private static List<HistoryRow> Aggregate(
        IEnumerable<(Guid ItemId, string ItemName, Guid? BrandId, string Uom,
            decimal Quantity, decimal UnitPrice, int Month)> rows)
        => rows
            .GroupBy(r => r.ItemId)
            .Select(g =>
            {
                var monthly = g.GroupBy(x => x.Month).Select(m => m.Sum(x => x.Quantity)).ToList();
                var mean = monthly.Count == 0 ? 0 : monthly.Average();
                var variance = monthly.Count <= 1
                    ? 0
                    : monthly.Sum(v => (v - mean) * (v - mean)) / (monthly.Count - 1);

                var first = g.First();

                return new HistoryRow(
                    g.Key,
                    first.ItemName,
                    first.BrandId,
                    first.Uom,
                    g.Sum(x => x.Quantity),
                    g.Average(x => x.UnitPrice),
                    (decimal)Math.Sqrt((double)variance));
            })
            .ToList();

    private async Task<int> SuggestForDistributorsAsync(Guid? partnerId, Guid userId)
    {
        var norms = await db.StockNorms.ForTenant(tenant)
            .Include(n => n.Partner)
            .WhereIf(partnerId.HasValue, n => n.PartnerId == partnerId)
            .Where(n => n.IsUnderStocked || n.CurrentDaysOfCover < n.TargetDaysOfCover)
            .ToListAsync();

        var written = 0;

        foreach (var norm in norms)
        {
            var daily = norm.CurrentDaysOfCover <= 0
                ? 0
                : norm.CurrentQuantity / norm.CurrentDaysOfCover;

            var target = norm.MaxQuantity > 0
                ? norm.MaxQuantity
                : Math.Round(daily * norm.TargetDaysOfCover, 2);

            var suggested = Math.Max(0, target - norm.CurrentQuantity);
            if (suggested <= 0) continue;

            db.ReplenishmentSuggestions.Add(new ReplenishmentSuggestion
            {
                TargetKind = ReplenishmentTargetKind.Distributor,
                PartnerId = norm.PartnerId,
                SourceWarehouseId = norm.Partner?.ServicingWarehouseId,
                ItemId = norm.ItemId,
                ItemName = norm.ItemName,
                Uom = norm.Uom,
                CurrentStock = norm.CurrentQuantity,
                ReorderPoint = norm.MinQuantity,
                TargetStock = target,
                SuggestedQuantity = Math.Max(suggested, norm.ReorderQuantity),
                AverageDailyDemand = daily,
                DaysOfCover = norm.CurrentDaysOfCover,
                LeadTimeDays = 2,
                UrgencyScore = Urgency(norm.CurrentDaysOfCover, norm.TargetDaysOfCover),
                GeneratedAt = DateTime.UtcNow,
            }.StampNew(tenant, userId));

            written++;
        }

        return written;
    }

    private async Task<int> SuggestForVansAsync(Guid? vanUnitId, Guid userId)
    {
        var vans = await db.VanUnits.ForTenant(tenant)
            .Where(v => v.IsActive)
            .WhereIf(vanUnitId.HasValue, v => v.Id == vanUnitId)
            .ToListAsync();

        var since = DateTime.UtcNow.Date.AddDays(-28);
        var written = 0;

        foreach (var van in vans)
        {
            var offtake = await db.VanStockMovements.ForTenant(tenant)
                .Where(m => m.VanUnitId == van.Id && m.Kind == VanMovementKind.Sale && m.OccurredAt >= since)
                .GroupBy(m => new { m.ItemId, m.ItemName, m.Uom })
                .Select(g => new
                {
                    g.Key.ItemId,
                    g.Key.ItemName,
                    g.Key.Uom,
                    Quantity = Math.Abs(g.Sum(x => x.Quantity)),
                    UnitCost = g.Average(x => x.UnitCost),
                })
                .ToListAsync();

            var onVan = await db.VanStockBalances.ForTenant(tenant)
                .Where(b => b.VanUnitId == van.Id && b.Compartment == VanCompartment.Sellable)
                .GroupBy(b => b.ItemId)
                .Select(g => new { ItemId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.ItemId, x => x.Quantity);

            foreach (var row in offtake)
            {
                var daily = row.Quantity / 24m;
                // A van is restocked daily, so two days of cover plus a fifth for a good day.
                var target = Math.Ceiling(daily * 2 * 1.2m);
                var have = onVan.GetValueOrDefault(row.ItemId);
                var suggested = Math.Max(0, target - have);

                if (suggested <= 0) continue;

                var cover = daily <= 0 ? 0 : Math.Round(have / daily, 1);

                db.ReplenishmentSuggestions.Add(new ReplenishmentSuggestion
                {
                    TargetKind = ReplenishmentTargetKind.Van,
                    VanUnitId = van.Id,
                    SourceWarehouseId = van.HomeWarehouseId,
                    ItemId = row.ItemId,
                    ItemName = row.ItemName,
                    Uom = row.Uom,
                    CurrentStock = have,
                    TargetStock = target,
                    SuggestedQuantity = suggested,
                    AverageDailyDemand = daily,
                    DaysOfCover = cover,
                    SafetyStock = Math.Ceiling(daily * 0.5m),
                    LeadTimeDays = 1,
                    EstimatedValue = Math.Round(suggested * row.UnitCost, 2),
                    UrgencyScore = Urgency(cover, 2),
                    GeneratedAt = DateTime.UtcNow,
                }.StampNew(tenant, userId));

                written++;
            }
        }

        return written;
    }

    private async Task<int> SuggestForWarehousesAsync(Guid? warehouseId, Guid userId)
    {
        var since = DateTime.UtcNow.Date.AddDays(-90);

        var demand = await db.OrderLines.ForTenant(tenant)
            .Where(l => db.Orders.ForTenant(tenant).Any(o => o.Id == l.OrderId
                && o.OrderDate >= since
                && (warehouseId == null || o.WarehouseId == warehouseId)
                && o.Status != DistributionOrderStatus.Cancelled
                && o.Status != DistributionOrderStatus.Rejected
                && o.Status != DistributionOrderStatus.Draft))
            .GroupBy(l => new { l.ItemId, l.ItemName, l.Uom })
            .Select(g => new
            {
                g.Key.ItemId,
                g.Key.ItemName,
                g.Key.Uom,
                Quantity = g.Sum(x => x.BaseQuantity),
                UnitCost = g.Average(x => x.UnitCost),
            })
            .ToListAsync();

        var written = 0;
        const int leadTimeDays = 7;

        foreach (var row in demand)
        {
            var daily = row.Quantity / 90m;

            // Safety stock from variability, not a flat percentage: a 1.65 z-score is roughly a
            // 95% service level, which is the usual target in fast-moving distribution.
            var deviation = daily * 0.35m;
            var safety = Math.Ceiling(1.65m * deviation * (decimal)Math.Sqrt(leadTimeDays));
            var reorderPoint = Math.Ceiling(daily * leadTimeDays + safety);
            var target = Math.Ceiling(daily * (leadTimeDays + 14) + safety);

            db.ReplenishmentSuggestions.Add(new ReplenishmentSuggestion
            {
                TargetKind = ReplenishmentTargetKind.Warehouse,
                WarehouseId = warehouseId,
                ItemId = row.ItemId,
                ItemName = row.ItemName,
                Uom = row.Uom,
                ReorderPoint = reorderPoint,
                SafetyStock = safety,
                TargetStock = target,
                SuggestedQuantity = target,
                AverageDailyDemand = Math.Round(daily, 4),
                LeadTimeDays = leadTimeDays,
                EstimatedValue = Math.Round(target * row.UnitCost, 2),
                UrgencyScore = 50,
                GeneratedAt = DateTime.UtcNow,
            }.StampNew(tenant, userId));

            written++;
        }

        return written;
    }

    /// <summary>
    /// 0–100, higher means closer to stocking out. Out of stock is always 100 so it sorts to the
    /// top regardless of what the target was.
    /// </summary>
    private static int Urgency(decimal currentCover, decimal targetCover)
    {
        if (currentCover <= 0) return 100;
        if (targetCover <= 0) return 50;

        var ratio = currentCover / targetCover;
        return (int)Math.Clamp(Math.Round((1 - ratio) * 100), 0, 100);
    }
}
