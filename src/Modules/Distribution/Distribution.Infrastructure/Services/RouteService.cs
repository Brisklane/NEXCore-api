using Distribution.Application.DTOs;
using Distribution.Application.Services.Interfaces;
using Distribution.Domain.Entities;
using Distribution.Domain.Enums;
using Distribution.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;

namespace Distribution.Infrastructure.Services;

/// <summary>
/// Geography, territories, routes and the journey plan.
///
/// Two decisions worth stating:
///
/// **Route reassignment writes history.** <see cref="RouteAssignment"/> rows are appended rather
/// than overwritten, so last quarter's performance still attributes to whoever actually walked
/// the beat after it changes hands.
///
/// **The journey plan is stored, not derived.** Generating it from route frequency on every read
/// would mean plan-vs-actual silently changes whenever someone edits a route, and "we hit 94% of
/// plan" would stop being a fact about March.
/// </summary>
public class RouteService(
    DistributionDbContext db,
    IDistributionTenant tenant,
    DistributionNumbering numbering) : IRouteService
{
    // ═══ Geography ═══════════════════════════════════════════════════════════

    public async Task<List<GeoNodeDto>> GetGeoTreeAsync(Guid? rootId)
    {
        var all = await db.GeoNodes.ForCompany(tenant).OrderBy(g => g.Name).ToListAsync();

        var outletCounts = await db.Outlets.ForTenant(tenant)
            .Where(o => o.GeoNodeId != null)
            .GroupBy(o => o.GeoNodeId!.Value)
            .Select(g => new { NodeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.NodeId, x => x.Count);

        var byParent = all.ToLookup(g => g.ParentId);

        List<GeoNodeDto> Build(Guid? parentId) =>
            byParent[parentId].Select(n =>
            {
                var dto = n.ToDto();
                dto.OutletCount = outletCounts.GetValueOrDefault(n.Id);
                dto.Children = Build(n.Id);
                // A parent's count includes everything beneath it, which is what a user expects
                // when they collapse a branch.
                dto.OutletCount += dto.Children.Sum(c => c.OutletCount);
                return dto;
            }).ToList();

        return Build(rootId);
    }

    public async Task<GeoNodeDto> SaveGeoNodeAsync(Guid? nodeId, GeoNodeDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("A geography node needs a name.");

        GeoNode entity;
        if (nodeId.HasValue)
        {
            entity = await db.GeoNodes.ForCompany(tenant).FirstOrDefaultAsync(g => g.Id == nodeId)
                ?? throw new InvalidOperationException("That location no longer exists.");
            entity.StampUpdated(userId);
        }
        else
        {
            entity = new GeoNode().StampNew(tenant, userId);
            db.GeoNodes.Add(entity);
        }

        entity.Name = request.Name.Trim();
        entity.Code = request.Code;
        entity.ParentId = request.ParentId;
        entity.LevelName = string.IsNullOrWhiteSpace(request.LevelName) ? "Area" : request.LevelName;
        entity.ManagerUserId = request.ManagerUserId;
        entity.Latitude = request.Latitude;
        entity.Longitude = request.Longitude;
        entity.IsActive = request.IsActive;

        // Depth and path are denormalised so a subtree query is one indexed prefix scan rather
        // than a recursive CTE on every dashboard load.
        if (request.ParentId.HasValue)
        {
            var parent = await db.GeoNodes.ForCompany(tenant)
                .Where(g => g.Id == request.ParentId)
                .Select(g => new { g.Depth, g.Path, g.Id })
                .FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("The parent location no longer exists.");

            entity.Depth = parent.Depth + 1;
            entity.Path = string.IsNullOrEmpty(parent.Path) ? parent.Id.ToString() : $"{parent.Path}/{parent.Id}";
        }
        else
        {
            entity.Depth = 0;
            entity.Path = null;
        }

        await db.SaveChangesAsync();
        return entity.ToDto();
    }

    public async Task DeleteGeoNodeAsync(Guid nodeId, Guid userId)
    {
        var entity = await db.GeoNodes.ForCompany(tenant).FirstOrDefaultAsync(g => g.Id == nodeId)
            ?? throw new InvalidOperationException("That location no longer exists.");

        if (await db.GeoNodes.ForCompany(tenant).AnyAsync(g => g.ParentId == nodeId))
            throw new InvalidOperationException("Remove the locations underneath this one first.");

        if (await db.Outlets.ForTenant(tenant).AnyAsync(o => o.GeoNodeId == nodeId))
            throw new InvalidOperationException("Outlets are mapped to this location. Move them first.");

        entity.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    // ═══ Territories ═════════════════════════════════════════════════════════

    public async Task<List<TerritoryDto>> GetTerritoryTreeAsync(Guid? rootId)
    {
        var all = await db.Territories.ForTenant(tenant)
            .Include(t => t.GeoNode)
            .OrderBy(t => t.Name).ToListAsync();

        var routeCounts = await db.Routes.ForTenant(tenant)
            .GroupBy(r => r.TerritoryId)
            .Select(g => new { TerritoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TerritoryId, x => x.Count);

        var outletCounts = await db.Outlets.ForTenant(tenant)
            .Where(o => o.TerritoryId != null)
            .GroupBy(o => o.TerritoryId!.Value)
            .Select(g => new { TerritoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TerritoryId, x => x.Count);

        var partnerCounts = await db.Partners.ForTenant(tenant)
            .Where(p => p.TerritoryId != null)
            .GroupBy(p => p.TerritoryId!.Value)
            .Select(g => new { TerritoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TerritoryId, x => x.Count);

        var monthStart = MonthStart(DateTime.UtcNow);
        var mtd = await db.Orders.ForTenant(tenant)
            .Where(o => o.TerritoryId != null && o.OrderDate >= monthStart && IsRevenue(o.Status))
            .GroupBy(o => o.TerritoryId!.Value)
            .Select(g => new { TerritoryId = g.Key, Value = g.Sum(x => x.TotalAmount) })
            .ToDictionaryAsync(x => x.TerritoryId, x => x.Value);

        var repNames = await db.FieldReps.ForTenant(tenant)
            .ToDictionaryAsync(r => r.Id, r => r.FullName);

        var byParent = all.ToLookup(t => t.ParentId);

        List<TerritoryDto> Build(Guid? parentId) =>
            byParent[parentId].Select(t =>
            {
                var dto = t.ToDto();
                dto.ManagerName = t.ManagerFieldRepId is null ? null : repNames.GetValueOrDefault(t.ManagerFieldRepId.Value);
                dto.RouteCount = routeCounts.GetValueOrDefault(t.Id);
                dto.OutletCount = outletCounts.GetValueOrDefault(t.Id);
                dto.PartnerCount = partnerCounts.GetValueOrDefault(t.Id);
                dto.MonthToDateSales = mtd.GetValueOrDefault(t.Id);
                dto.Children = Build(t.Id);

                foreach (var child in dto.Children)
                {
                    dto.RouteCount += child.RouteCount;
                    dto.OutletCount += child.OutletCount;
                    dto.PartnerCount += child.PartnerCount;
                    dto.MonthToDateSales += child.MonthToDateSales;
                }

                return dto;
            }).ToList();

        return Build(rootId);
    }

    public async Task<PaginatedResponse<TerritoryDto>> ListTerritoriesAsync(string? search, PaginationParams pagination)
    {
        var query = db.Territories.ForTenant(tenant).Include(t => t.Parent).Include(t => t.GeoNode).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(t => EF.Functions.ILike(t.Name, term) || EF.Functions.ILike(t.Code ?? "", term));
        }

        var total = await query.CountAsync();
        var rows = await query.OrderBy(t => t.Name)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        return PaginatedResponse<TerritoryDto>.Ok(
            rows.Select(r => r.ToDto()), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<TerritoryDto?> GetTerritoryAsync(Guid territoryId)
    {
        var entity = await db.Territories.ForTenant(tenant)
            .Include(t => t.Parent).Include(t => t.GeoNode)
            .FirstOrDefaultAsync(t => t.Id == territoryId);

        if (entity is null) return null;

        var dto = entity.ToDto();
        dto.RouteCount = await db.Routes.ForTenant(tenant).CountAsync(r => r.TerritoryId == territoryId);
        dto.OutletCount = await db.Outlets.ForTenant(tenant).CountAsync(o => o.TerritoryId == territoryId);
        dto.PartnerCount = await db.Partners.ForTenant(tenant).CountAsync(p => p.TerritoryId == territoryId);
        return dto;
    }

    public async Task<TerritoryDto> SaveTerritoryAsync(Guid? territoryId, SaveTerritoryDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("A territory needs a name.");

        if (territoryId.HasValue && request.ParentId == territoryId)
            throw new InvalidOperationException("A territory cannot be its own parent.");

        DistributionTerritory entity;
        if (territoryId.HasValue)
        {
            entity = await db.Territories.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == territoryId)
                ?? throw new InvalidOperationException("That territory no longer exists.");
            entity.StampUpdated(userId);
        }
        else
        {
            entity = new DistributionTerritory().StampNew(tenant, userId);
            entity.Code = string.IsNullOrWhiteSpace(request.Code)
                ? await numbering.NextMasterCodeAsync(db.Territories, "TER")
                : request.Code;
            db.Territories.Add(entity);
        }

        if (territoryId.HasValue && !string.IsNullOrWhiteSpace(request.Code)) entity.Code = request.Code;

        entity.Name = request.Name.Trim();
        entity.ParentId = request.ParentId;
        entity.GeoNodeId = request.GeoNodeId;
        entity.ManagerFieldRepId = request.ManagerFieldRepId;
        entity.DefaultPartnerId = request.DefaultPartnerId;
        entity.DefaultWarehouseId = request.DefaultWarehouseId;
        entity.CurrencyCode = request.CurrencyCode;
        entity.IsActive = request.IsActive;
        entity.Description = request.Description;

        await db.SaveChangesAsync();
        return (await GetTerritoryAsync(entity.Id))!;
    }

    public async Task DeleteTerritoryAsync(Guid territoryId, Guid userId)
    {
        var entity = await db.Territories.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == territoryId)
            ?? throw new InvalidOperationException("That territory no longer exists.");

        if (await db.Routes.ForTenant(tenant).AnyAsync(r => r.TerritoryId == territoryId))
            throw new InvalidOperationException("Move this territory's routes before removing it.");

        entity.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    // ═══ Routes ══════════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<RouteDto>> ListRoutesAsync(
        string? search, RouteKind? kind, Guid? territoryId, Guid? fieldRepId, Guid? partnerId,
        PaginationParams pagination)
    {
        var query = db.Routes.ForTenant(tenant)
            .Include(r => r.Territory).Include(r => r.Partner).Include(r => r.FieldRep)
            .WhereIf(kind.HasValue, r => r.Kind == kind)
            .WhereIf(territoryId.HasValue, r => r.TerritoryId == territoryId)
            .WhereIf(fieldRepId.HasValue, r => r.FieldRepId == fieldRepId)
            .WhereIf(partnerId.HasValue, r => r.PartnerId == partnerId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(r => EF.Functions.ILike(r.Name, term) || EF.Functions.ILike(r.Code ?? "", term));
        }

        var total = await query.CountAsync();
        var rows = await query.OrderBy(r => r.Name)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var dtos = rows.Select(r => r.ToDto()).ToList();
        var ids = rows.Select(r => r.Id).ToList();
        var monthStart = MonthStart(DateTime.UtcNow);

        var mtd = await db.Orders.ForTenant(tenant)
            .Where(o => o.RouteId != null && ids.Contains(o.RouteId.Value)
                        && o.OrderDate >= monthStart && IsRevenue(o.Status))
            .GroupBy(o => o.RouteId!.Value)
            .Select(g => new { RouteId = g.Key, Value = g.Sum(x => x.TotalAmount) })
            .ToDictionaryAsync(x => x.RouteId, x => x.Value);

        var visitStats = await db.Visits.ForTenant(tenant)
            .Where(v => v.RouteId != null && ids.Contains(v.RouteId.Value) && v.CheckedInAt >= monthStart)
            .GroupBy(v => v.RouteId!.Value)
            .Select(g => new
            {
                RouteId = g.Key,
                Actual = g.Count(),
                Productive = g.Count(x => x.IsProductive),
                Planned = g.Count(x => x.IsPlanned),
            })
            .ToDictionaryAsync(x => x.RouteId);

        foreach (var dto in dtos)
        {
            dto.MonthToDateSales = mtd.GetValueOrDefault(dto.Id);
            if (visitStats.TryGetValue(dto.Id, out var v))
            {
                dto.CoveragePercent = DistributionMapper.Percent(v.Actual, v.Planned);
                dto.StrikeRatePercent = DistributionMapper.Percent(v.Productive, v.Actual);
            }
        }

        return PaginatedResponse<RouteDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<RouteDto?> GetRouteAsync(Guid routeId)
    {
        var entity = await db.Routes.ForTenant(tenant)
            .Include(r => r.Territory).Include(r => r.Partner).Include(r => r.FieldRep)
            .FirstOrDefaultAsync(r => r.Id == routeId);

        if (entity is null) return null;

        var dto = entity.ToDto();
        dto.Stops = (await db.RouteOutlets.ForTenant(tenant)
                .Include(r => r.Outlet)
                .Where(r => r.RouteId == routeId)
                .OrderBy(r => r.StopSequence)
                .ToListAsync())
            .Select(r => r.ToStopDto()).ToList();

        return dto;
    }

    public async Task<RouteDto> SaveRouteAsync(Guid? routeId, SaveRouteDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("A route needs a name.");
        if (request.TerritoryId == Guid.Empty)
            throw new InvalidOperationException("A route must belong to a territory.");

        SalesRoute entity;
        var previousRepId = (Guid?)null;

        if (routeId.HasValue)
        {
            entity = await db.Routes.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == routeId)
                ?? throw new InvalidOperationException("That route no longer exists.");
            previousRepId = entity.FieldRepId;
            entity.StampUpdated(userId);
        }
        else
        {
            entity = new SalesRoute().StampNew(tenant, userId);
            entity.Code = string.IsNullOrWhiteSpace(request.Code)
                ? await numbering.NextMasterCodeAsync(db.Routes, "RTE")
                : request.Code;
            db.Routes.Add(entity);
        }

        if (routeId.HasValue && !string.IsNullOrWhiteSpace(request.Code)) entity.Code = request.Code;

        entity.Name = request.Name.Trim();
        entity.Kind = request.Kind;
        entity.Frequency = request.Frequency;
        entity.TerritoryId = request.TerritoryId;
        entity.PartnerId = request.PartnerId;
        entity.FieldRepId = request.FieldRepId;
        entity.VehicleId = request.VehicleId;
        entity.VanUnitId = request.VanUnitId;
        entity.WarehouseId = request.WarehouseId;
        entity.ActiveDays = request.ActiveDays;
        entity.ActiveWeeks = request.ActiveWeeks;
        entity.StartTime = request.StartTime;
        entity.EndTime = request.EndTime;
        entity.TargetCallsPerDay = request.TargetCallsPerDay;
        entity.MinimumProductiveCalls = request.MinimumProductiveCalls;
        entity.IsActive = request.IsActive;
        entity.Description = request.Description;

        await db.SaveChangesAsync();

        // A rep change on the route record is still a reassignment, and it belongs in history.
        if (request.FieldRepId.HasValue && request.FieldRepId != previousRepId)
            await RecordAssignmentAsync(entity.Id, request.FieldRepId.Value, DateTime.UtcNow, null,
                false, "Route saved with a new owner", userId);

        return (await GetRouteAsync(entity.Id))!;
    }

    public async Task DeleteRouteAsync(Guid routeId, Guid userId)
    {
        var entity = await db.Routes.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == routeId)
            ?? throw new InvalidOperationException("That route no longer exists.");

        if (await db.FieldDays.ForTenant(tenant).AnyAsync(d => d.RouteId == routeId && d.Status == FieldDayStatus.Started))
            throw new InvalidOperationException("Someone is working this route right now. Close their day first.");

        entity.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    public async Task<RouteDto> AddOutletsAsync(AddOutletsToRouteDto request, Guid userId)
    {
        var route = await db.Routes.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == request.RouteId)
            ?? throw new InvalidOperationException("That route no longer exists.");

        var existing = await db.RouteOutlets.ForTenant(tenant)
            .Where(r => r.RouteId == request.RouteId)
            .Select(r => r.OutletId).ToListAsync();

        var nextSeq = await db.RouteOutlets.ForTenant(tenant)
            .Where(r => r.RouteId == request.RouteId)
            .Select(r => (int?)r.StopSequence).MaxAsync() ?? 0;

        foreach (var outletId in request.OutletIds.Distinct().Where(id => !existing.Contains(id)))
        {
            db.RouteOutlets.Add(new RouteOutlet
            {
                RouteId = request.RouteId,
                OutletId = outletId,
                StopSequence = ++nextSeq,
                IsMustVisit = request.MarkMustVisit,
            }.StampNew(tenant, userId));
        }

        route.OutletCount = nextSeq;
        route.StampUpdated(userId);
        await db.SaveChangesAsync();

        return (await GetRouteAsync(request.RouteId))!;
    }

    public async Task<RouteDto> RemoveOutletAsync(Guid routeId, Guid outletId, Guid userId)
    {
        var link = await db.RouteOutlets.ForTenant(tenant)
            .FirstOrDefaultAsync(r => r.RouteId == routeId && r.OutletId == outletId)
            ?? throw new InvalidOperationException("That outlet is not on this route.");

        link.StampDeleted(userId);
        await db.SaveChangesAsync();
        await ResequenceInternalAsync(routeId, userId);

        return (await GetRouteAsync(routeId))!;
    }

    public async Task<RouteDto> ResequenceAsync(ResequenceRouteDto request, Guid userId)
    {
        var links = await db.RouteOutlets.ForTenant(tenant)
            .Where(r => r.RouteId == request.RouteId).ToListAsync();

        var seq = 0;
        foreach (var id in request.OrderedRouteOutletIds)
        {
            var link = links.FirstOrDefault(l => l.Id == id);
            if (link is null) continue;
            link.StopSequence = ++seq;
            link.StampUpdated(userId);
        }

        // Anything the caller omitted keeps a stable position at the end rather than colliding on
        // sequence zero.
        foreach (var link in links.Where(l => !request.OrderedRouteOutletIds.Contains(l.Id)).OrderBy(l => l.StopSequence))
        {
            link.StopSequence = ++seq;
            link.StampUpdated(userId);
        }

        await db.SaveChangesAsync();
        return (await GetRouteAsync(request.RouteId))!;
    }

    public async Task<RouteDto> AssignAsync(AssignRouteDto request, Guid userId)
    {
        var route = await db.Routes.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == request.RouteId)
            ?? throw new InvalidOperationException("That route no longer exists.");

        var rep = await db.FieldReps.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == request.FieldRepId)
            ?? throw new InvalidOperationException("That field rep no longer exists.");

        if (!rep.IsActive)
            throw new InvalidOperationException($"{rep.FullName} is no longer active and cannot take a route.");

        await RecordAssignmentAsync(
            request.RouteId, request.FieldRepId, request.EffectiveFrom, request.EffectiveTo,
            request.IsTemporary, request.Reason, userId);

        // A temporary cover does not change who owns the beat, only who is walking it this week.
        if (!request.IsTemporary)
        {
            route.FieldRepId = request.FieldRepId;
            route.StampUpdated(userId);
        }

        await db.SaveChangesAsync();
        return (await GetRouteAsync(request.RouteId))!;
    }

    public async Task<List<RouteDto>> SplitRouteAsync(
        Guid routeId, List<Guid> outletIdsToMove, string newRouteName, Guid userId)
    {
        var source = await db.Routes.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == routeId)
            ?? throw new InvalidOperationException("That route no longer exists.");

        if (outletIdsToMove.Count == 0)
            throw new InvalidOperationException("Choose the outlets that move to the new route.");
        if (string.IsNullOrWhiteSpace(newRouteName))
            throw new InvalidOperationException("The new route needs a name.");

        var target = new SalesRoute
        {
            Name = newRouteName.Trim(),
            Kind = source.Kind,
            Frequency = source.Frequency,
            TerritoryId = source.TerritoryId,
            PartnerId = source.PartnerId,
            WarehouseId = source.WarehouseId,
            ActiveDays = source.ActiveDays,
            ActiveWeeks = source.ActiveWeeks,
            StartTime = source.StartTime,
            EndTime = source.EndTime,
            TargetCallsPerDay = source.TargetCallsPerDay,
            Description = $"Split from {source.Name}",
        }.StampNew(tenant, userId);

        target.Code = await numbering.NextMasterCodeAsync(db.Routes, "RTE");
        db.Routes.Add(target);
        await db.SaveChangesAsync();

        var links = await db.RouteOutlets.ForTenant(tenant)
            .Where(r => r.RouteId == routeId && outletIdsToMove.Contains(r.OutletId))
            .OrderBy(r => r.StopSequence).ToListAsync();

        var seq = 0;
        foreach (var link in links)
        {
            link.RouteId = target.Id;
            link.StopSequence = ++seq;
            link.StampUpdated(userId);
        }

        await db.SaveChangesAsync();
        await ResequenceInternalAsync(routeId, userId);

        target.OutletCount = links.Count;
        source.OutletCount = await db.RouteOutlets.ForTenant(tenant).CountAsync(r => r.RouteId == routeId);
        await db.SaveChangesAsync();

        return [(await GetRouteAsync(routeId))!, (await GetRouteAsync(target.Id))!];
    }

    public async Task<PaginatedResponse<OutletDto>> GetUnroutedOutletsAsync(
        Guid? territoryId, PaginationParams pagination)
    {
        var routed = db.RouteOutlets.ForTenant(tenant).Select(r => r.OutletId);

        var query = db.Outlets.ForTenant(tenant)
            .Include(o => o.Partner)
            .Where(o => !routed.Contains(o.Id))
            .Where(o => o.Status == OutletStatus.Active || o.Status == OutletStatus.Prospect)
            .WhereIf(territoryId.HasValue, o => o.TerritoryId == territoryId);

        var total = await query.CountAsync();
        var rows = await query
            // Highest-potential first: an unrouted A-grade shop is the most expensive gap.
            .OrderBy(o => o.Grade).ThenByDescending(o => o.AverageMonthlyOfftake)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        return PaginatedResponse<OutletDto>.Ok(
            rows.Select(r => r.ToDto()), total, pagination.PageNumber, pagination.PageSize);
    }

    // ═══ Journey plans ═══════════════════════════════════════════════════════

    public async Task<JourneyPlanDto?> GetJourneyPlanAsync(Guid fieldRepId, DateTime periodStart)
    {
        var start = MonthStart(periodStart);
        var entity = await db.JourneyPlans.ForTenant(tenant)
            .Include(p => p.FieldRep)
            .Include(p => p.Days.Where(d => !d.IsDeleted)).ThenInclude(d => d.Route)
            .FirstOrDefaultAsync(p => p.FieldRepId == fieldRepId && p.PeriodStart == start);

        return entity?.ToDto();
    }

    public async Task<List<JourneyPlanDto>> ListJourneyPlansAsync(Guid? territoryId, DateTime periodStart)
    {
        var start = MonthStart(periodStart);
        var plans = await db.JourneyPlans.ForTenant(tenant)
            .Include(p => p.FieldRep)
            .Include(p => p.Days.Where(d => !d.IsDeleted)).ThenInclude(d => d.Route)
            .Where(p => p.PeriodStart == start)
            .WhereIf(territoryId.HasValue, p => p.TerritoryId == territoryId)
            .OrderBy(p => p.FieldRep!.FullName)
            .ToListAsync();

        return plans.Select(p => p.ToDto()).ToList();
    }

    public async Task<JourneyPlanDto> GenerateJourneyPlanAsync(GenerateJourneyPlanDto request, Guid userId)
    {
        var rep = await db.FieldReps.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == request.FieldRepId)
            ?? throw new InvalidOperationException("That field rep no longer exists.");

        var start = MonthStart(request.PeriodStart);
        var end = request.PeriodEnd == default ? start.AddMonths(1).AddDays(-1) : request.PeriodEnd.Date;

        var plan = await db.JourneyPlans.ForTenant(tenant)
            .Include(p => p.Days)
            .FirstOrDefaultAsync(p => p.FieldRepId == request.FieldRepId && p.PeriodStart == start);

        if (plan is not null && !request.Overwrite && plan.IsPublished)
            throw new InvalidOperationException(
                "This plan is already published. Choose overwrite if you really want to rebuild it.");

        if (plan is null)
        {
            plan = new JourneyPlan
            {
                FieldRepId = request.FieldRepId,
                TerritoryId = rep.TerritoryId,
                PeriodStart = start,
                PeriodEnd = end,
                Name = $"{rep.FullName} — {start:MMMM yyyy}",
            }.StampNew(tenant, userId);
            db.JourneyPlans.Add(plan);
        }
        else
        {
            plan.PeriodEnd = end;
            plan.StampUpdated(userId);
            if (request.Overwrite)
                foreach (var day in plan.Days.Where(d => !d.IsDeleted))
                    day.StampDeleted(userId);
        }

        var routes = await db.Routes.ForTenant(tenant)
            .Where(r => r.IsActive)
            .Where(r => request.RouteIds.Count > 0
                ? request.RouteIds.Contains(r.Id)
                : r.FieldRepId == request.FieldRepId)
            .ToListAsync();

        if (routes.Count == 0)
            throw new InvalidOperationException("This rep has no routes to plan. Assign a route first.");

        var stopCounts = await db.RouteOutlets.ForTenant(tenant)
            .Where(r => routes.Select(x => x.Id).Contains(r.RouteId))
            .GroupBy(r => r.RouteId)
            .Select(g => new { RouteId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RouteId, x => x.Count);

        var holidays = request.HolidayDates.Select(d => d.Date).ToHashSet();
        var nonWorking = request.NonWorkingDays.ToHashSet();
        var existingDates = plan.Days.Where(d => !d.IsDeleted).Select(d => d.PlanDate.Date).ToHashSet();

        var plannedCalls = 0;
        var dayCount = 0;

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (nonWorking.Contains((int)date.DayOfWeek)) continue;
            if (existingDates.Contains(date.Date)) continue;

            // A holiday still produces a row, explicitly skipped. A missing row would look like a
            // rep who simply never showed up.
            var isHoliday = holidays.Contains(date.Date);

            var route = PickRouteForDate(routes, date);
            if (route is null && !isHoliday) continue;

            var stops = route is null ? 0 : stopCounts.GetValueOrDefault(route.Id);

            db.JourneyPlanDays.Add(new JourneyPlanDay
            {
                JourneyPlanId = plan.Id,
                PlanDate = date,
                RouteId = route?.Id,
                Status = isHoliday ? JourneyPlanDayStatus.Skipped : JourneyPlanDayStatus.Planned,
                SkipReason = isHoliday ? "Holiday" : null,
                PlannedCalls = isHoliday ? 0 : stops,
            }.StampNew(tenant, userId));

            if (!isHoliday)
            {
                plannedCalls += stops;
                dayCount++;
            }
        }

        plan.PlannedCalls = plannedCalls;
        await db.SaveChangesAsync();

        if (dayCount == 0)
            throw new InvalidOperationException(
                "No working days matched the route frequencies. Check the active days on the routes.");

        return (await GetJourneyPlanAsync(request.FieldRepId, start))!;
    }

    public async Task<JourneyPlanDto> PublishJourneyPlanAsync(Guid planId, Guid userId)
    {
        var plan = await db.JourneyPlans.ForTenant(tenant)
            .Include(p => p.Days)
            .FirstOrDefaultAsync(p => p.Id == planId)
            ?? throw new InvalidOperationException("That journey plan no longer exists.");

        if (plan.Days.Count(d => !d.IsDeleted) == 0)
            throw new InvalidOperationException("An empty plan cannot be published.");

        plan.IsPublished = true;
        plan.PublishedAt = DateTime.UtcNow;
        plan.PublishedByUserId = userId;
        plan.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetJourneyPlanAsync(plan.FieldRepId, plan.PeriodStart))!;
    }

    public async Task<JourneyPlanDayDto> UpdateJourneyPlanDayAsync(
        Guid dayId, UpdateJourneyPlanDayDto request, Guid userId)
    {
        var day = await db.JourneyPlanDays.ForTenant(tenant)
            .Include(d => d.Route)
            .FirstOrDefaultAsync(d => d.Id == dayId)
            ?? throw new InvalidOperationException("That plan day no longer exists.");

        if (request.Status == JourneyPlanDayStatus.Skipped && string.IsNullOrWhiteSpace(request.SkipReason))
            throw new InvalidOperationException("Skipping a planned day needs a reason.");

        if (request.RouteId.HasValue && request.RouteId != day.RouteId)
        {
            day.RouteId = request.RouteId;
            day.PlannedCalls = await db.RouteOutlets.ForTenant(tenant)
                .CountAsync(r => r.RouteId == request.RouteId);
        }

        if (request.Status.HasValue) day.Status = request.Status.Value;
        if (request.ReassignedToFieldRepId.HasValue)
        {
            day.ReassignedToFieldRepId = request.ReassignedToFieldRepId;
            day.Status = JourneyPlanDayStatus.Reassigned;
        }
        day.SkipReason = request.SkipReason ?? day.SkipReason;
        day.StampUpdated(userId);

        await db.SaveChangesAsync();

        await db.Entry(day).Reference(d => d.Route).LoadAsync();
        return day.ToDto();
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    private static DateTime MonthStart(DateTime value)
        => new(value.Year, value.Month, 1, 0, 0, 0, DateTimeKind.Utc);

    private static bool IsRevenue(DistributionOrderStatus s)
        => s != DistributionOrderStatus.Cancelled
           && s != DistributionOrderStatus.Rejected
           && s != DistributionOrderStatus.Draft;

    /// <summary>
    /// Which route runs on a given date, from the weekday mask and the week-of-month mask.
    ///
    /// When two routes both claim a day the first by name wins, deterministically — a plan that
    /// shuffles on regeneration is worse than one that is arguably wrong but stable.
    /// </summary>
    private static SalesRoute? PickRouteForDate(List<SalesRoute> routes, DateTime date)
    {
        var weekday = ((int)date.DayOfWeek).ToString();
        var weekOfMonth = ((date.Day - 1) / 7 + 1).ToString();

        return routes
            .Where(r =>
            {
                var days = string.IsNullOrWhiteSpace(r.ActiveDays)
                    ? null
                    : r.ActiveDays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (days is not null && !days.Contains(weekday)) return false;

                var weeks = string.IsNullOrWhiteSpace(r.ActiveWeeks)
                    ? null
                    : r.ActiveWeeks.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (weeks is not null && !weeks.Contains(weekOfMonth)) return false;

                return r.Frequency switch
                {
                    VisitFrequency.Daily => true,
                    VisitFrequency.AlternateDay => date.DayOfYear % 2 == 0,
                    VisitFrequency.Weekly => days is not null,
                    VisitFrequency.Fortnightly => weeks is not null || (date.Day - 1) / 7 % 2 == 0,
                    VisitFrequency.Monthly => date.Day <= 7,
                    _ => days is not null || weeks is not null,
                };
            })
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private async Task RecordAssignmentAsync(
        Guid routeId, Guid fieldRepId, DateTime from, DateTime? to,
        bool isTemporary, string? reason, Guid userId)
    {
        // Close the standing assignment rather than deleting it: the history is the point.
        if (!isTemporary)
        {
            var open = await db.RouteAssignments.ForTenant(tenant)
                .Where(a => a.RouteId == routeId && a.EffectiveTo == null && !a.IsTemporary)
                .ToListAsync();

            foreach (var a in open)
            {
                a.EffectiveTo = from.AddDays(-1);
                a.StampUpdated(userId);
            }
        }

        db.RouteAssignments.Add(new RouteAssignment
        {
            RouteId = routeId,
            FieldRepId = fieldRepId,
            EffectiveFrom = from,
            EffectiveTo = to,
            IsTemporary = isTemporary,
            Reason = reason,
        }.StampNew(tenant, userId));

        await db.SaveChangesAsync();
    }

    private async Task ResequenceInternalAsync(Guid routeId, Guid userId)
    {
        var links = await db.RouteOutlets.ForTenant(tenant)
            .Where(r => r.RouteId == routeId)
            .OrderBy(r => r.StopSequence).ToListAsync();

        var seq = 0;
        foreach (var link in links)
        {
            link.StopSequence = ++seq;
            link.StampUpdated(userId);
        }

        await db.Routes.ForTenant(tenant).Where(r => r.Id == routeId)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.OutletCount, links.Count));

        await db.SaveChangesAsync();
    }
}
