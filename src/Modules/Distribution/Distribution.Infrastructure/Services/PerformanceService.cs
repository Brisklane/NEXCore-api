using Distribution.Application.DTOs;
using Distribution.Application.Services.Interfaces;
using Distribution.Domain.Entities;
using Distribution.Domain.Enums;
using Distribution.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;

namespace Distribution.Infrastructure.Services;

/// <summary>
/// Targets, incentives and the field KPI set.
///
/// KPI snapshots are computed once per day and stored rather than derived on read. Coverage and
/// strike rate are ratios over a window; recomputing a quarter of them across a thousand routes
/// every time somebody opens a dashboard is how reporting screens end up taking forty seconds.
///
/// The gate on an incentive scheme is the part that matters commercially. A rep who hits value by
/// hammering three big outlets and visiting nobody else has not done the job, and a gated scheme
/// is how the business says so without arguing about it every month.
/// </summary>
public class PerformanceService(DistributionDbContext db, IDistributionTenant tenant) : IPerformanceService
{
    // ═══ Targets ═════════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<TargetDto>> ListTargetsAsync(
        TargetScope? scope, TargetMetric? metric, Guid? fieldRepId, Guid? territoryId,
        DateTime? periodStart, PaginationParams pagination)
    {
        var query = db.Targets.ForTenant(tenant)
            .Include(t => t.Lines.Where(l => !l.IsDeleted))
            .WhereIf(scope.HasValue, t => t.Scope == scope)
            .WhereIf(metric.HasValue, t => t.Metric == metric)
            .WhereIf(fieldRepId.HasValue, t => t.FieldRepId == fieldRepId)
            .WhereIf(territoryId.HasValue, t => t.TerritoryId == territoryId)
            .WhereIf(periodStart.HasValue, t => t.PeriodStart == periodStart!.Value.Date);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(t => t.PeriodStart).ThenBy(t => t.Name)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var dtos = rows.Select(r => r.ToDto()).ToList();
        await DecorateTargetsAsync(dtos);

        return PaginatedResponse<TargetDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<TargetDto?> GetTargetAsync(Guid targetId)
    {
        var entity = await db.Targets.ForTenant(tenant)
            .Include(t => t.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(t => t.Id == targetId);

        if (entity is null) return null;

        var dto = entity.ToDto();
        await DecorateTargetsAsync([dto]);
        return dto;
    }

    public async Task<TargetDto> SaveTargetAsync(Guid? targetId, SaveTargetDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("A target needs a name.");
        if (request.PeriodEnd < request.PeriodStart)
            throw new InvalidOperationException("The target's end date is before its start date.");
        if (request.TargetValue <= 0)
            throw new InvalidOperationException("A target needs a value above zero.");

        var missing = request.Scope switch
        {
            TargetScope.Territory when request.TerritoryId is null => "a territory",
            TargetScope.Route when request.RouteId is null => "a route",
            TargetScope.FieldRep when request.FieldRepId is null => "a field rep",
            TargetScope.Partner when request.PartnerId is null => "a partner",
            TargetScope.Outlet when request.OutletId is null => "an outlet",
            _ => null,
        };

        if (missing is not null)
            throw new InvalidOperationException($"A {request.Scope} target needs {missing}.");

        SalesTarget entity;
        if (targetId.HasValue)
        {
            entity = await db.Targets.ForTenant(tenant)
                .Include(t => t.Lines)
                .FirstOrDefaultAsync(t => t.Id == targetId)
                ?? throw new InvalidOperationException("That target no longer exists.");

            if (entity.IsPublished && entity.PeriodEnd < DateTime.UtcNow.Date)
                throw new InvalidOperationException(
                    "This period has closed. Editing a published target after the fact would rewrite what was paid on.");

            entity.StampUpdated(userId);
        }
        else
        {
            entity = new SalesTarget().StampNew(tenant, userId);
            db.Targets.Add(entity);
        }

        entity.Name = request.Name.Trim();
        entity.Metric = request.Metric;
        entity.Period = request.Period;
        entity.Scope = request.Scope;
        entity.PeriodStart = request.PeriodStart.Date;
        entity.PeriodEnd = request.PeriodEnd.Date;
        entity.TerritoryId = request.TerritoryId;
        entity.RouteId = request.RouteId;
        entity.FieldRepId = request.FieldRepId;
        entity.PartnerId = request.PartnerId;
        entity.OutletId = request.OutletId;
        entity.CurrencyCode = request.CurrencyCode;
        entity.TargetValue = request.TargetValue;
        entity.IsPublished = request.IsPublished;
        entity.Note = request.Note;

        if (request.IsPublished && entity.PublishedAt is null) entity.PublishedAt = DateTime.UtcNow;

        foreach (var existing in entity.Lines.Where(l => !l.IsDeleted).ToList())
            if (request.Lines.All(l => l.Id != existing.Id))
                existing.StampDeleted(userId);

        var order = 0;
        foreach (var line in request.Lines)
        {
            var target = line.Id != Guid.Empty ? entity.Lines.FirstOrDefault(l => l.Id == line.Id) : null;
            if (target is null)
            {
                target = new TargetLine { TargetId = entity.Id }.StampNew(tenant, userId);
                entity.Lines.Add(target);
            }

            target.ItemId = line.ItemId;
            target.ItemName = line.ItemName;
            target.BrandId = line.BrandId;
            target.CategoryId = line.CategoryId;
            target.WeekNumber = line.WeekNumber;
            target.PhaseStart = line.PhaseStart;
            target.PhaseEnd = line.PhaseEnd;
            target.Uom = string.IsNullOrWhiteSpace(line.Uom) ? "PCS" : line.Uom;
            target.TargetValue = line.TargetValue;
            target.DisplayOrder = order++;
        }

        // Phased lines must add up to the header, or the month's expectations quietly drift.
        var phased = entity.Lines.Where(l => !l.IsDeleted && l.WeekNumber.HasValue).ToList();
        if (phased.Count > 0)
        {
            var sum = phased.Sum(l => l.TargetValue);
            if (Math.Abs(sum - entity.TargetValue) > 0.01m)
                throw new InvalidOperationException(
                    $"The weekly phasing adds up to {sum:N2} but the target is {entity.TargetValue:N2}.");
        }

        await db.SaveChangesAsync();
        await ComputeAchievementAsync(entity);
        await db.SaveChangesAsync();

        return (await GetTargetAsync(entity.Id))!;
    }

    public async Task<TargetDto> PublishTargetAsync(Guid targetId, Guid userId)
    {
        var entity = await db.Targets.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == targetId)
            ?? throw new InvalidOperationException("That target no longer exists.");

        entity.IsPublished = true;
        entity.PublishedAt = DateTime.UtcNow;
        entity.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetTargetAsync(targetId))!;
    }

    public async Task DeleteTargetAsync(Guid targetId, Guid userId)
    {
        var entity = await db.Targets.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == targetId)
            ?? throw new InvalidOperationException("That target no longer exists.");

        var paidAgainst = await db.IncentivePayouts.ForTenant(tenant)
            .AnyAsync(p => p.FieldRepId == entity.FieldRepId
                           && p.PeriodStart == entity.PeriodStart && p.IsPaid);

        if (paidAgainst)
            throw new InvalidOperationException("An incentive has already been paid against this period.");

        entity.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    public async Task<List<TargetDto>> RecomputeTargetsAsync(DateTime periodStart, DateTime periodEnd)
    {
        var targets = await db.Targets.ForTenant(tenant)
            .Include(t => t.Lines.Where(l => !l.IsDeleted))
            .Where(t => t.PeriodStart >= periodStart.Date && t.PeriodEnd <= periodEnd.Date)
            .ToListAsync();

        foreach (var target in targets) await ComputeAchievementAsync(target);

        await db.SaveChangesAsync();

        var dtos = targets.Select(t => t.ToDto()).ToList();
        await DecorateTargetsAsync(dtos);
        return dtos;
    }

    // ═══ Incentives ══════════════════════════════════════════════════════════

    public async Task<List<IncentiveSchemeDto>> ListIncentiveSchemesAsync(bool? activeOnly)
    {
        var today = DateTime.UtcNow.Date;

        var rows = await db.IncentiveSchemes.ForTenant(tenant)
            .Include(s => s.Slabs.Where(x => !x.IsDeleted))
            .WhereIf(activeOnly == true, s => s.IsActive && s.ValidFrom <= today && s.ValidTo >= today)
            .OrderByDescending(s => s.ValidFrom)
            .ToListAsync();

        return rows.Select(MapScheme).ToList();
    }

    public async Task<IncentiveSchemeDto> SaveIncentiveSchemeAsync(
        Guid? id, IncentiveSchemeDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("An incentive scheme needs a name.");
        if (request.Basis == IncentiveBasis.Slab && request.Slabs.Count == 0)
            throw new InvalidOperationException("A slab-based incentive needs at least one slab.");
        if (request.Basis == IncentiveBasis.Gated && request.GateMetric is null)
            throw new InvalidOperationException("A gated incentive needs a gate metric.");

        IncentiveScheme entity;
        if (id.HasValue)
        {
            entity = await db.IncentiveSchemes.ForTenant(tenant)
                .Include(s => s.Slabs)
                .FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new InvalidOperationException("That incentive scheme no longer exists.");
            entity.StampUpdated(userId);
        }
        else
        {
            entity = new IncentiveScheme().StampNew(tenant, userId);
            db.IncentiveSchemes.Add(entity);
        }

        entity.Name = request.Name.Trim();
        entity.Basis = request.Basis;
        entity.Metric = request.Metric;
        entity.Period = request.Period;
        entity.ValidFrom = request.ValidFrom.Date;
        entity.ValidTo = request.ValidTo.Date;
        entity.ApplicableRoles = request.ApplicableRoles;
        entity.TerritoryId = request.TerritoryId;
        entity.GateMetric = request.GateMetric;
        entity.GateThresholdPercent = request.GateThresholdPercent;
        entity.MinimumAchievementPercent = request.MinimumAchievementPercent;
        entity.LinearRatePercent = request.LinearRatePercent;
        entity.MaxPayout = request.MaxPayout;
        entity.CurrencyCode = request.CurrencyCode;
        entity.SpiffItemId = request.SpiffItemId;
        entity.SpiffRatePerUnit = request.SpiffRatePerUnit;
        entity.IsApproved = request.IsApproved;
        entity.Terms = request.Terms;
        entity.IsActive = request.IsActive;

        if (request.IsApproved && entity.ApprovedAt is null)
        {
            entity.ApprovedAt = DateTime.UtcNow;
            entity.ApprovedByUserId = userId;
        }

        foreach (var existing in entity.Slabs.Where(s => !s.IsDeleted).ToList())
            if (request.Slabs.All(s => s.Id != existing.Id))
                existing.StampDeleted(userId);

        var number = 0;
        foreach (var slab in request.Slabs.OrderBy(s => s.FromAchievementPercent))
        {
            var target = slab.Id != Guid.Empty ? entity.Slabs.FirstOrDefault(s => s.Id == slab.Id) : null;
            if (target is null)
            {
                target = new IncentiveSlab { SchemeId = entity.Id }.StampNew(tenant, userId);
                entity.Slabs.Add(target);
            }

            target.SlabNumber = ++number;
            target.FromAchievementPercent = slab.FromAchievementPercent;
            target.ToAchievementPercent = slab.ToAchievementPercent;
            target.PayoutAmount = slab.PayoutAmount;
            target.PayoutPercent = slab.PayoutPercent;
            target.Label = slab.Label;
        }

        await db.SaveChangesAsync();

        var saved = await db.IncentiveSchemes.ForTenant(tenant)
            .Include(s => s.Slabs.Where(x => !x.IsDeleted))
            .FirstAsync(s => s.Id == entity.Id);

        return MapScheme(saved);
    }

    public async Task<List<IncentivePayoutDto>> ComputePayoutsAsync(
        Guid schemeId, DateTime periodStart, DateTime periodEnd, Guid userId)
    {
        var scheme = await db.IncentiveSchemes.ForTenant(tenant)
            .Include(s => s.Slabs.Where(x => !x.IsDeleted))
            .FirstOrDefaultAsync(s => s.Id == schemeId)
            ?? throw new InvalidOperationException("That incentive scheme no longer exists.");

        if (!scheme.IsApproved)
            throw new InvalidOperationException("Approve the scheme before computing payouts against it.");

        var roles = string.IsNullOrWhiteSpace(scheme.ApplicableRoles)
            ? null
            : scheme.ApplicableRoles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(r => Enum.TryParse<FieldRole>(r, out var parsed) ? parsed : (FieldRole?)null)
                .Where(r => r.HasValue).Select(r => r!.Value).ToList();

        var reps = await db.FieldReps.ForTenant(tenant)
            .Where(r => r.IsActive)
            .WhereIf(scheme.TerritoryId.HasValue, r => r.TerritoryId == scheme.TerritoryId)
            .ToListAsync();

        if (roles is { Count: > 0 }) reps = reps.Where(r => roles.Contains(r.Role)).ToList();

        var results = new List<IncentivePayout>();

        foreach (var rep in reps)
        {
            var target = await db.Targets.ForTenant(tenant)
                .FirstOrDefaultAsync(t => t.FieldRepId == rep.Id && t.Metric == scheme.Metric
                                          && t.PeriodStart == periodStart.Date);

            if (target is null) continue;

            var achieved = await MeasureAsync(scheme.Metric, rep.Id, periodStart, periodEnd);
            var achievementPercent = DistributionMapper.Percent(achieved, target.TargetValue);

            // The gate: nothing pays until it is cleared, and the reason is recorded so the
            // conversation at payout time is short.
            var gatePassed = true;
            string? gateFailure = null;

            if (scheme.GateMetric.HasValue && scheme.GateThresholdPercent > 0)
            {
                var gateTarget = await db.Targets.ForTenant(tenant)
                    .FirstOrDefaultAsync(t => t.FieldRepId == rep.Id && t.Metric == scheme.GateMetric
                                              && t.PeriodStart == periodStart.Date);

                var gateAchieved = await MeasureAsync(scheme.GateMetric.Value, rep.Id, periodStart, periodEnd);
                var gatePercent = gateTarget is null
                    ? gateAchieved
                    : DistributionMapper.Percent(gateAchieved, gateTarget.TargetValue);

                gatePassed = gatePercent >= scheme.GateThresholdPercent;
                if (!gatePassed)
                    gateFailure = $"{scheme.GateMetric} at {gatePercent:N1}% against a {scheme.GateThresholdPercent:N0}% gate";
            }

            if (achievementPercent < scheme.MinimumAchievementPercent)
            {
                gatePassed = false;
                gateFailure ??= $"Achievement {achievementPercent:N1}% is below the {scheme.MinimumAchievementPercent:N0}% floor";
            }

            IncentiveSlab? slab = null;
            decimal payout = 0;

            if (gatePassed)
            {
                switch (scheme.Basis)
                {
                    case IncentiveBasis.Slab:
                    case IncentiveBasis.Gated:
                        slab = scheme.Slabs.OrderBy(s => s.SlabNumber).LastOrDefault(s =>
                            achievementPercent >= s.FromAchievementPercent
                            && (s.ToAchievementPercent is null || achievementPercent <= s.ToAchievementPercent));

                        payout = slab is null ? 0
                            : slab.PayoutAmount > 0 ? slab.PayoutAmount
                            : Math.Round(achieved * slab.PayoutPercent / 100m, 2);
                        break;

                    case IncentiveBasis.Linear:
                        payout = Math.Round(achieved * scheme.LinearRatePercent / 100m, 2);
                        break;

                    case IncentiveBasis.Team:
                        payout = Math.Round(achieved * scheme.LinearRatePercent / 100m, 2);
                        break;

                    case IncentiveBasis.Spiff:
                        payout = 0;
                        break;
                }

                if (scheme.MaxPayout > 0) payout = Math.Min(payout, scheme.MaxPayout);
            }

            var spiff = scheme.SpiffItemId.HasValue && scheme.SpiffRatePerUnit > 0
                ? await ComputeSpiffAsync(scheme, rep.Id, periodStart, periodEnd)
                : 0;

            var existing = await db.IncentivePayouts.ForTenant(tenant)
                .FirstOrDefaultAsync(p => p.SchemeId == schemeId && p.FieldRepId == rep.Id
                                          && p.PeriodStart == periodStart.Date);

            if (existing?.IsPaid == true) continue;

            var entity = existing;
            if (entity is null)
            {
                entity = new IncentivePayout
                {
                    SchemeId = schemeId,
                    FieldRepId = rep.Id,
                    PeriodStart = periodStart.Date,
                    PeriodEnd = periodEnd.Date,
                }.StampNew(tenant, userId);
                db.IncentivePayouts.Add(entity);
            }
            else
            {
                entity.StampUpdated(userId);
            }

            entity.TargetValue = target.TargetValue;
            entity.AchievedValue = achieved;
            entity.AchievementPercent = achievementPercent;
            entity.GatePassed = gatePassed;
            entity.GateFailureReason = gateFailure;
            entity.SlabNumber = slab?.SlabNumber;
            entity.PayoutAmount = payout;
            entity.SpiffAmount = spiff;
            entity.NetPayout = payout + spiff - entity.DeductionAmount;
            entity.CurrencyCode = scheme.CurrencyCode;

            results.Add(entity);
        }

        await db.SaveChangesAsync();

        var names = await db.FieldReps.ForTenant(tenant).ToDictionaryAsync(r => r.Id, r => r.FullName);

        return results.Select(p => MapPayout(p, scheme.Name, names)).ToList();
    }

    public async Task<IncentivePayoutDto> ApprovePayoutAsync(
        Guid payoutId, bool isApproved, string? note, Guid userId)
    {
        var entity = await db.IncentivePayouts.ForTenant(tenant)
            .Include(p => p.FieldRep).Include(p => p.Scheme)
            .FirstOrDefaultAsync(p => p.Id == payoutId)
            ?? throw new InvalidOperationException("That payout no longer exists.");

        if (entity.IsPaid)
            throw new InvalidOperationException("This payout has already gone to payroll.");

        if (!isApproved && string.IsNullOrWhiteSpace(note))
            throw new InvalidOperationException("Rejecting a payout needs a reason.");

        entity.IsApproved = isApproved;
        entity.ApprovedAt = DateTime.UtcNow;
        entity.ApprovedByUserId = userId;
        entity.Note = note;
        entity.StampUpdated(userId);

        await db.SaveChangesAsync();

        return MapPayout(entity, entity.Scheme?.Name,
            new Dictionary<Guid, string> { [entity.FieldRepId ?? Guid.Empty] = entity.FieldRep?.FullName ?? "" });
    }

    public async Task<PaginatedResponse<IncentivePayoutDto>> ListPayoutsAsync(
        Guid? schemeId, Guid? fieldRepId, DateTime? periodStart, PaginationParams pagination)
    {
        var query = db.IncentivePayouts.ForTenant(tenant)
            .Include(p => p.FieldRep).Include(p => p.Scheme)
            .WhereIf(schemeId.HasValue, p => p.SchemeId == schemeId)
            .WhereIf(fieldRepId.HasValue, p => p.FieldRepId == fieldRepId)
            .WhereIf(periodStart.HasValue, p => p.PeriodStart == periodStart!.Value.Date);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(p => p.PeriodStart).ThenByDescending(p => p.NetPayout)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var names = rows.Where(r => r.FieldRepId.HasValue)
            .ToDictionary(r => r.FieldRepId!.Value, r => r.FieldRep?.FullName ?? "");

        return PaginatedResponse<IncentivePayoutDto>.Ok(
            rows.Select(r => MapPayout(r, r.Scheme?.Name, names)),
            total, pagination.PageNumber, pagination.PageSize);
    }

    // ═══ KPIs ════════════════════════════════════════════════════════════════

    public async Task<int> ComputeKpiSnapshotsAsync(DateTime date)
    {
        var day = date.Date;
        var next = day.AddDays(1);

        var days = await db.FieldDays.ForTenant(tenant)
            .Include(d => d.Route)
            .Where(d => d.WorkDate == day)
            .ToListAsync();

        if (days.Count == 0) return 0;

        var dayIds = days.Select(d => d.Id).ToList();

        var visitStats = await db.Visits.ForTenant(tenant)
            .Where(v => dayIds.Contains(v.FieldDayId))
            .GroupBy(v => v.FieldDayId)
            .Select(g => new
            {
                DayId = g.Key,
                Actual = g.Count(),
                Productive = g.Count(x => x.IsProductive),
                Unplanned = g.Count(x => !x.IsPlanned),
                MustSellSold = g.Sum(x => x.MustSellSoldCount),
                MustSellTarget = g.Sum(x => x.MustSellTargetCount),
                Minutes = g.Sum(x => x.DurationMinutes ?? 0),
                FirstAt = g.Min(x => x.CheckedInAt),
                LastAt = g.Max(x => x.CheckedOutAt),
            })
            .ToDictionaryAsync(x => x.DayId);

        var orderStats = await db.Orders.ForTenant(tenant)
            .Where(o => o.FieldDayId != null && dayIds.Contains(o.FieldDayId.Value) && IsRevenue(o.Status))
            .GroupBy(o => o.FieldDayId!.Value)
            .Select(g => new
            {
                DayId = g.Key,
                Value = g.Sum(x => x.TotalAmount),
                Bills = g.Count(),
                Lines = g.Sum(x => x.LineCount),
            })
            .ToDictionaryAsync(x => x.DayId);

        var collectionStats = await db.Collections.ForTenant(tenant)
            .Where(c => c.FieldDayId != null && dayIds.Contains(c.FieldDayId.Value) && !c.IsReversed)
            .GroupBy(c => c.FieldDayId!.Value)
            .Select(g => new { DayId = g.Key, Value = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.DayId, x => x.Value);

        var written = 0;

        foreach (var fieldDay in days)
        {
            var snapshot = await db.KpiSnapshots.ForTenant(tenant)
                .FirstOrDefaultAsync(k => k.SnapshotDate == day && k.Scope == TargetScope.FieldRep
                                          && k.FieldRepId == fieldDay.FieldRepId);

            if (snapshot is null)
            {
                snapshot = new KpiSnapshot
                {
                    SnapshotDate = day,
                    Scope = TargetScope.FieldRep,
                    FieldRepId = fieldDay.FieldRepId,
                    RouteId = fieldDay.RouteId,
                }.StampNew(tenant, Guid.Empty);
                db.KpiSnapshots.Add(snapshot);
            }

            visitStats.TryGetValue(fieldDay.Id, out var visits);
            orderStats.TryGetValue(fieldDay.Id, out var orders);
            var collected = collectionStats.GetValueOrDefault(fieldDay.Id);

            snapshot.PlannedCalls = fieldDay.PlannedCalls;
            snapshot.ActualCalls = visits?.Actual ?? 0;
            snapshot.ProductiveCalls = visits?.Productive ?? 0;
            snapshot.UnplannedCalls = visits?.Unplanned ?? 0;
            snapshot.MissedCalls = Math.Max(0, fieldDay.PlannedCalls - snapshot.ActualCalls);

            snapshot.CoveragePercent = DistributionMapper.Percent(snapshot.ActualCalls, snapshot.PlannedCalls);
            snapshot.StrikeRatePercent = DistributionMapper.Percent(snapshot.ProductiveCalls, snapshot.ActualCalls);

            snapshot.BillCount = orders?.Bills ?? 0;
            snapshot.SalesValue = orders?.Value ?? 0;
            snapshot.LinesPerCall = snapshot.ProductiveCalls == 0
                ? 0 : Math.Round((decimal)(orders?.Lines ?? 0) / snapshot.ProductiveCalls, 2);
            snapshot.AverageBillValue = snapshot.BillCount == 0
                ? 0 : Math.Round(snapshot.SalesValue / snapshot.BillCount, 2);
            snapshot.DropSize = snapshot.ProductiveCalls == 0
                ? 0 : Math.Round(snapshot.SalesValue / snapshot.ProductiveCalls, 2);

            snapshot.CollectionValue = collected;
            snapshot.NewOutletsAdded = fieldDay.NewOutletsAdded;
            snapshot.MustSellCompliancePercent =
                DistributionMapper.Percent(visits?.MustSellSold ?? 0, visits?.MustSellTarget ?? 0);

            snapshot.TimeInMarketMinutes = visits?.Minutes ?? 0;
            snapshot.AverageTimePerCallMinutes = snapshot.ActualCalls == 0
                ? 0 : Math.Round((decimal)snapshot.TimeInMarketMinutes / snapshot.ActualCalls, 1);
            snapshot.FirstCallAt = visits?.FirstAt?.TimeOfDay;
            snapshot.LastCallAt = visits?.LastAt?.TimeOfDay;
            snapshot.DistanceCoveredKm = fieldDay.DistanceCoveredKm;

            snapshot.TotalOutlets = fieldDay.RouteId is null
                ? 0
                : await db.RouteOutlets.ForTenant(tenant).CountAsync(r => r.RouteId == fieldDay.RouteId);

            snapshot.ActiveOutlets = await db.Orders.ForTenant(tenant)
                .Where(o => o.FieldRepId == fieldDay.FieldRepId && o.OrderDate >= day.AddDays(-30)
                            && o.OrderDate < next && o.OutletId != null)
                .Select(o => o.OutletId).Distinct().CountAsync();

            snapshot.ComputedAt = DateTime.UtcNow;
            written++;
        }

        await db.SaveChangesAsync();
        return written;
    }

    public async Task<List<KpiDto>> GetKpisAsync(
        TargetScope scope, DateTime from, DateTime to, Guid? territoryId, Guid? fieldRepId, Guid? routeId)
    {
        var rows = await db.KpiSnapshots.ForTenant(tenant)
            .Where(k => k.SnapshotDate >= from.Date && k.SnapshotDate <= to.Date && k.Scope == scope)
            .WhereIf(territoryId.HasValue, k => k.TerritoryId == territoryId)
            .WhereIf(fieldRepId.HasValue, k => k.FieldRepId == fieldRepId)
            .WhereIf(routeId.HasValue, k => k.RouteId == routeId)
            .ToListAsync();

        // Roll the daily snapshots into one row per subject over the window. Ratios are
        // recomputed from the summed numerators, never averaged — averaging ratios is the
        // classic way to get a coverage number nobody can reproduce.
        var grouped = rows
            .GroupBy(k => scope switch
            {
                TargetScope.FieldRep => k.FieldRepId,
                TargetScope.Route => k.RouteId,
                TargetScope.Territory => k.TerritoryId,
                TargetScope.Partner => k.PartnerId,
                _ => null,
            })
            .Select(g =>
            {
                var planned = g.Sum(x => x.PlannedCalls);
                var actual = g.Sum(x => x.ActualCalls);
                var productive = g.Sum(x => x.ProductiveCalls);
                var bills = g.Sum(x => x.BillCount);
                var sales = g.Sum(x => x.SalesValue);

                return new KpiDto
                {
                    SnapshotDate = to.Date,
                    Scope = scope,
                    FieldRepId = scope == TargetScope.FieldRep ? g.Key : null,
                    RouteId = scope == TargetScope.Route ? g.Key : null,
                    TerritoryId = scope == TargetScope.Territory ? g.Key : null,
                    PartnerId = scope == TargetScope.Partner ? g.Key : null,
                    PlannedCalls = planned,
                    ActualCalls = actual,
                    ProductiveCalls = productive,
                    UnplannedCalls = g.Sum(x => x.UnplannedCalls),
                    MissedCalls = g.Sum(x => x.MissedCalls),
                    CoveragePercent = DistributionMapper.Percent(actual, planned),
                    StrikeRatePercent = DistributionMapper.Percent(productive, actual),
                    LinesPerCall = productive == 0 ? 0 : Math.Round(g.Sum(x => x.LinesPerCall * x.ProductiveCalls) / productive, 2),
                    AverageBillValue = bills == 0 ? 0 : Math.Round(sales / bills, 2),
                    DropSize = productive == 0 ? 0 : Math.Round(sales / productive, 2),
                    BillCount = bills,
                    NewOutletsAdded = g.Sum(x => x.NewOutletsAdded),
                    ActiveOutlets = g.Max(x => x.ActiveOutlets),
                    TotalOutlets = g.Max(x => x.TotalOutlets),
                    MustSellCompliancePercent = Math.Round(g.Average(x => x.MustSellCompliancePercent), 2),
                    TimeInMarketMinutes = g.Sum(x => x.TimeInMarketMinutes),
                    AverageTimePerCallMinutes = actual == 0
                        ? 0 : Math.Round((decimal)g.Sum(x => x.TimeInMarketMinutes) / actual, 1),
                    DistanceCoveredKm = g.Sum(x => x.DistanceCoveredKm),
                    SalesValue = sales,
                    CollectionValue = g.Sum(x => x.CollectionValue),
                    ReturnValue = g.Sum(x => x.ReturnValue),
                    OverdueAmount = g.Max(x => x.OverdueAmount),
                    CollectionEfficiencyPercent = DistributionMapper.Percent(g.Sum(x => x.CollectionValue), sales),
                };
            })
            .ToList();

        await DecorateKpisAsync(grouped);
        return grouped.OrderByDescending(k => k.SalesValue).ToList();
    }

    public async Task<List<RankedRowDto>> GetLeaderboardAsync(
        TargetScope scope, TargetMetric metric, DateTime from, DateTime to)
    {
        var kpis = await GetKpisAsync(scope, from, to, null, null, null);

        var rows = kpis.Select(k => new RankedRowDto
        {
            Id = k.FieldRepId ?? k.RouteId ?? k.TerritoryId ?? k.PartnerId,
            Name = k.FieldRepName ?? k.RouteName ?? k.TerritoryName ?? k.PartnerName ?? "Unknown",
            Value = metric switch
            {
                TargetMetric.SalesValue => k.SalesValue,
                TargetMetric.Collection => k.CollectionValue,
                TargetMetric.Coverage => k.CoveragePercent,
                TargetMetric.ProductiveCalls => k.ProductiveCalls,
                TargetMetric.NewOutlets => k.NewOutletsAdded,
                TargetMetric.MustSellCompliance => k.MustSellCompliancePercent,
                TargetMetric.LinesPerCall => k.LinesPerCall,
                _ => k.SalesValue,
            },
            Quantity = k.ProductiveCalls,
            SubLabel = $"{k.StrikeRatePercent:N0}% strike · {k.CoveragePercent:N0}% coverage",
        })
        .OrderByDescending(r => r.Value)
        .ToList();

        var total = rows.Sum(r => r.Value);
        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].Rank = i + 1;
            rows[i].SharePercent = DistributionMapper.Percent(rows[i].Value, total);
        }

        return rows;
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    private static bool IsRevenue(DistributionOrderStatus s)
        => s != DistributionOrderStatus.Cancelled
           && s != DistributionOrderStatus.Rejected
           && s != DistributionOrderStatus.Draft;

    private async Task ComputeAchievementAsync(SalesTarget target)
    {
        var achieved = target.Scope switch
        {
            TargetScope.FieldRep when target.FieldRepId.HasValue =>
                await MeasureAsync(target.Metric, target.FieldRepId.Value, target.PeriodStart, target.PeriodEnd),
            _ => await MeasureScopeAsync(target),
        };

        target.AchievedValue = achieved;
        target.AchievementPercent = DistributionMapper.Percent(achieved, target.TargetValue);

        // Pro-rata is elapsed working days over the period, so "behind" means something on the
        // fifteenth rather than only on the last day.
        var totalDays = Math.Max(1, (target.PeriodEnd - target.PeriodStart).Days + 1);
        var elapsed = Math.Clamp((DateTime.UtcNow.Date - target.PeriodStart).Days + 1, 0, totalDays);

        target.ProRataTarget = Math.Round(target.TargetValue * elapsed / totalDays, 2);
        target.ProjectedValue = elapsed == 0 ? 0 : Math.Round(achieved / elapsed * totalDays, 2);
        target.LastComputedAt = DateTime.UtcNow;

        foreach (var line in target.Lines.Where(l => !l.IsDeleted && l.ItemId.HasValue))
        {
            line.AchievedValue = await db.OrderLines.ForTenant(tenant)
                .Where(l => l.ItemId == line.ItemId
                            && db.Orders.ForTenant(tenant).Any(o => o.Id == l.OrderId
                                && o.OrderDate >= target.PeriodStart && o.OrderDate <= target.PeriodEnd
                                && (target.FieldRepId == null || o.FieldRepId == target.FieldRepId)
                                && IsRevenue(o.Status)))
                .SumAsync(l => (decimal?)(target.Metric == TargetMetric.SalesVolume ? l.BaseQuantity : l.LineTotal)) ?? 0;

            line.AchievementPercent = DistributionMapper.Percent(line.AchievedValue, line.TargetValue);
        }
    }

    private async Task<decimal> MeasureScopeAsync(SalesTarget target)
    {
        var orders = db.Orders.ForTenant(tenant)
            .Where(o => o.OrderDate >= target.PeriodStart && o.OrderDate <= target.PeriodEnd && IsRevenue(o.Status))
            .WhereIf(target.TerritoryId.HasValue, o => o.TerritoryId == target.TerritoryId)
            .WhereIf(target.RouteId.HasValue, o => o.RouteId == target.RouteId)
            .WhereIf(target.PartnerId.HasValue, o => o.PartnerId == target.PartnerId)
            .WhereIf(target.OutletId.HasValue, o => o.OutletId == target.OutletId);

        return target.Metric switch
        {
            TargetMetric.SalesValue => await orders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0,
            TargetMetric.SalesVolume => await orders.SumAsync(o => (decimal?)o.TotalQuantity) ?? 0,
            _ => await orders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0,
        };
    }

    private async Task<decimal> MeasureAsync(
        TargetMetric metric, Guid fieldRepId, DateTime from, DateTime to)
    {
        var start = from.Date;
        var end = to.Date.AddDays(1);

        return metric switch
        {
            TargetMetric.SalesValue => await db.Orders.ForTenant(tenant)
                .Where(o => o.FieldRepId == fieldRepId && o.OrderDate >= start && o.OrderDate < end && IsRevenue(o.Status))
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0,

            TargetMetric.SalesVolume => await db.Orders.ForTenant(tenant)
                .Where(o => o.FieldRepId == fieldRepId && o.OrderDate >= start && o.OrderDate < end && IsRevenue(o.Status))
                .SumAsync(o => (decimal?)o.TotalQuantity) ?? 0,

            TargetMetric.Collection => await db.Collections.ForTenant(tenant)
                .Where(c => c.FieldRepId == fieldRepId && c.CollectedAt >= start && c.CollectedAt < end && !c.IsReversed)
                .SumAsync(c => (decimal?)c.Amount) ?? 0,

            TargetMetric.ProductiveCalls => await db.Visits.ForTenant(tenant)
                .CountAsync(v => v.FieldRepId == fieldRepId && v.CheckedInAt >= start
                                 && v.CheckedInAt < end && v.IsProductive),

            TargetMetric.NewOutlets => await db.FieldDays.ForTenant(tenant)
                .Where(d => d.FieldRepId == fieldRepId && d.WorkDate >= start && d.WorkDate < end)
                .SumAsync(d => (int?)d.NewOutletsAdded) ?? 0,

            TargetMetric.Coverage => await ComputeCoverageAsync(fieldRepId, start, end),

            TargetMetric.LinesPerCall => await ComputeLinesPerCallAsync(fieldRepId, start, end),

            TargetMetric.MustSellCompliance => await db.Visits.ForTenant(tenant)
                .Where(v => v.FieldRepId == fieldRepId && v.CheckedInAt >= start && v.CheckedInAt < end
                            && v.MustSellTargetCount > 0)
                .Select(v => (decimal?)v.MustSellSoldCount).AverageAsync() ?? 0,

            TargetMetric.RangeSelling => await db.OrderLines.ForTenant(tenant)
                .Where(l => db.Orders.ForTenant(tenant).Any(o => o.Id == l.OrderId
                    && o.FieldRepId == fieldRepId && o.OrderDate >= start && o.OrderDate < end))
                .Select(l => l.ItemId).Distinct().CountAsync(),

            _ => 0,
        };
    }

    private async Task<decimal> ComputeCoverageAsync(Guid fieldRepId, DateTime start, DateTime end)
    {
        var stats = await db.FieldDays.ForTenant(tenant)
            .Where(d => d.FieldRepId == fieldRepId && d.WorkDate >= start && d.WorkDate < end)
            .GroupBy(_ => 1)
            .Select(g => new { Planned = g.Sum(x => x.PlannedCalls), Actual = g.Sum(x => x.ActualCalls) })
            .FirstOrDefaultAsync();

        return stats is null ? 0 : DistributionMapper.Percent(stats.Actual, stats.Planned);
    }

    private async Task<decimal> ComputeLinesPerCallAsync(Guid fieldRepId, DateTime start, DateTime end)
    {
        var stats = await db.Visits.ForTenant(tenant)
            .Where(v => v.FieldRepId == fieldRepId && v.CheckedInAt >= start && v.CheckedInAt < end && v.IsProductive)
            .GroupBy(_ => 1)
            .Select(g => new { Calls = g.Count(), Lines = g.Sum(x => x.LinesSold) })
            .FirstOrDefaultAsync();

        return stats is null || stats.Calls == 0 ? 0 : Math.Round((decimal)stats.Lines / stats.Calls, 2);
    }

    private async Task<decimal> ComputeSpiffAsync(
        IncentiveScheme scheme, Guid fieldRepId, DateTime from, DateTime to)
    {
        var quantity = await db.OrderLines.ForTenant(tenant)
            .Where(l => l.ItemId == scheme.SpiffItemId && !l.IsFreeGoods
                        && db.Orders.ForTenant(tenant).Any(o => o.Id == l.OrderId
                            && o.FieldRepId == fieldRepId
                            && o.OrderDate >= from && o.OrderDate <= to && IsRevenue(o.Status)))
            .SumAsync(l => (decimal?)l.BaseQuantity) ?? 0;

        return Math.Round(quantity * scheme.SpiffRatePerUnit, 2);
    }

    private async Task DecorateTargetsAsync(List<TargetDto> dtos)
    {
        if (dtos.Count == 0) return;

        var repIds = dtos.Where(d => d.FieldRepId.HasValue).Select(d => d.FieldRepId!.Value).Distinct().ToList();
        var territoryIds = dtos.Where(d => d.TerritoryId.HasValue).Select(d => d.TerritoryId!.Value).Distinct().ToList();
        var routeIds = dtos.Where(d => d.RouteId.HasValue).Select(d => d.RouteId!.Value).Distinct().ToList();
        var partnerIds = dtos.Where(d => d.PartnerId.HasValue).Select(d => d.PartnerId!.Value).Distinct().ToList();

        var reps = await db.FieldReps.ForTenant(tenant)
            .Where(r => repIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.FullName);
        var territories = await db.Territories.ForTenant(tenant)
            .Where(t => territoryIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Name);
        var routes = await db.Routes.ForTenant(tenant)
            .Where(r => routeIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.Name);
        var partners = await db.Partners.ForTenant(tenant)
            .Where(p => partnerIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name);

        foreach (var dto in dtos)
        {
            if (dto.FieldRepId.HasValue) dto.FieldRepName = reps.GetValueOrDefault(dto.FieldRepId.Value);
            if (dto.TerritoryId.HasValue) dto.TerritoryName = territories.GetValueOrDefault(dto.TerritoryId.Value);
            if (dto.RouteId.HasValue) dto.RouteName = routes.GetValueOrDefault(dto.RouteId.Value);
            if (dto.PartnerId.HasValue) dto.PartnerName = partners.GetValueOrDefault(dto.PartnerId.Value);
        }
    }

    private async Task DecorateKpisAsync(List<KpiDto> rows)
    {
        if (rows.Count == 0) return;

        var repIds = rows.Where(r => r.FieldRepId.HasValue).Select(r => r.FieldRepId!.Value).Distinct().ToList();
        var routeIds = rows.Where(r => r.RouteId.HasValue).Select(r => r.RouteId!.Value).Distinct().ToList();
        var territoryIds = rows.Where(r => r.TerritoryId.HasValue).Select(r => r.TerritoryId!.Value).Distinct().ToList();
        var partnerIds = rows.Where(r => r.PartnerId.HasValue).Select(r => r.PartnerId!.Value).Distinct().ToList();

        var reps = await db.FieldReps.ForTenant(tenant)
            .Where(r => repIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.FullName);
        var routes = await db.Routes.ForTenant(tenant)
            .Where(r => routeIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.Name);
        var territories = await db.Territories.ForTenant(tenant)
            .Where(t => territoryIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Name);
        var partners = await db.Partners.ForTenant(tenant)
            .Where(p => partnerIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name);

        foreach (var row in rows)
        {
            if (row.FieldRepId.HasValue) row.FieldRepName = reps.GetValueOrDefault(row.FieldRepId.Value);
            if (row.RouteId.HasValue) row.RouteName = routes.GetValueOrDefault(row.RouteId.Value);
            if (row.TerritoryId.HasValue) row.TerritoryName = territories.GetValueOrDefault(row.TerritoryId.Value);
            if (row.PartnerId.HasValue) row.PartnerName = partners.GetValueOrDefault(row.PartnerId.Value);
        }
    }

    private static IncentiveSchemeDto MapScheme(IncentiveScheme e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Basis = e.Basis,
        Metric = e.Metric,
        Period = e.Period,
        ValidFrom = e.ValidFrom,
        ValidTo = e.ValidTo,
        ApplicableRoles = e.ApplicableRoles,
        TerritoryId = e.TerritoryId,
        GateMetric = e.GateMetric,
        GateThresholdPercent = e.GateThresholdPercent,
        MinimumAchievementPercent = e.MinimumAchievementPercent,
        LinearRatePercent = e.LinearRatePercent,
        MaxPayout = e.MaxPayout,
        CurrencyCode = e.CurrencyCode,
        SpiffItemId = e.SpiffItemId,
        SpiffRatePerUnit = e.SpiffRatePerUnit,
        IsApproved = e.IsApproved,
        Terms = e.Terms,
        IsActive = e.IsActive,
        Slabs = e.Slabs.OrderBy(s => s.SlabNumber).Select(s => new IncentiveSlabDto
        {
            Id = s.Id,
            SlabNumber = s.SlabNumber,
            FromAchievementPercent = s.FromAchievementPercent,
            ToAchievementPercent = s.ToAchievementPercent,
            PayoutAmount = s.PayoutAmount,
            PayoutPercent = s.PayoutPercent,
            Label = s.Label,
        }).ToList(),
    };

    private static IncentivePayoutDto MapPayout(
        IncentivePayout e, string? schemeName, Dictionary<Guid, string> names) => new()
    {
        Id = e.Id,
        SchemeId = e.SchemeId,
        SchemeName = schemeName,
        FieldRepId = e.FieldRepId,
        FieldRepName = e.FieldRepId is null ? null : names.GetValueOrDefault(e.FieldRepId.Value),
        PartnerId = e.PartnerId,
        PeriodStart = e.PeriodStart,
        PeriodEnd = e.PeriodEnd,
        TargetValue = e.TargetValue,
        AchievedValue = e.AchievedValue,
        AchievementPercent = e.AchievementPercent,
        GatePassed = e.GatePassed,
        GateFailureReason = e.GateFailureReason,
        SlabNumber = e.SlabNumber,
        PayoutAmount = e.PayoutAmount,
        SpiffAmount = e.SpiffAmount,
        DeductionAmount = e.DeductionAmount,
        NetPayout = e.NetPayout,
        CurrencyCode = e.CurrencyCode,
        IsApproved = e.IsApproved,
        IsPaid = e.IsPaid,
        PayrollReference = e.PayrollReference,
        Note = e.Note,
    };
}
