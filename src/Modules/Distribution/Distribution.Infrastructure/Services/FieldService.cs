using BCrypt.Net;
using Distribution.Application.DTOs;
using Distribution.Application.Services.Interfaces;
using Distribution.Domain.Entities;
using Distribution.Domain.Enums;
using Distribution.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;

namespace Distribution.Infrastructure.Services;

/// <summary>
/// The field terminal: the day, the beat, the visit, and everything captured at the counter.
///
/// Three rules shape this service:
///
/// **Every write is idempotent by client key.** A rep on a bad connection will retry, and a
/// double-submitted check-in that becomes two visits quietly corrupts coverage and strike rate
/// for the whole month.
///
/// **An out-of-fence check-in is recorded, not blocked.** Refusing it just moves the lie somewhere
/// the system cannot see — the rep stands outside and taps anyway, or fakes the GPS. Recording it
/// with a mandatory reason keeps the data honest and gives the supervisor something to look at.
///
/// **A call with no order still has to be explained.** Coverage and strike rate are both ratios
/// whose denominator is the visit, so a visit that produced nothing is data, not an absence.
/// </summary>
public class FieldService(
    DistributionDbContext db,
    IDistributionTenant tenant,
    DistributionNumbering numbering) : IFieldService
{
    // ═══ Reps & devices ══════════════════════════════════════════════════════

    public async Task<PaginatedResponse<FieldRepDto>> ListRepsAsync(
        string? search, FieldRole? role, Guid? territoryId, Guid? partnerId, PaginationParams pagination)
    {
        var query = db.FieldReps.ForTenant(tenant)
            .Include(r => r.Partner)
            .WhereIf(role.HasValue, r => r.Role == role)
            .WhereIf(territoryId.HasValue, r => r.TerritoryId == territoryId)
            .WhereIf(partnerId.HasValue, r => r.PartnerId == partnerId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(r => EF.Functions.ILike(r.FullName, term)
                                     || EF.Functions.ILike(r.Code ?? "", term)
                                     || EF.Functions.ILike(r.Phone ?? "", term));
        }

        var total = await query.CountAsync();
        var rows = await query.OrderBy(r => r.FullName)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var dtos = rows.Select(r => r.ToDto()).ToList();
        var ids = rows.Select(r => r.Id).ToList();
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var routeCounts = await db.Routes.ForTenant(tenant)
            .Where(r => r.FieldRepId != null && ids.Contains(r.FieldRepId.Value))
            .GroupBy(r => r.FieldRepId!.Value)
            .Select(g => new { RepId = g.Key, Routes = g.Count(), Outlets = g.Sum(x => x.OutletCount) })
            .ToDictionaryAsync(x => x.RepId);

        var todayDays = await db.FieldDays.ForTenant(tenant)
            .Where(d => ids.Contains(d.FieldRepId) && d.WorkDate == today)
            .ToDictionaryAsync(d => d.FieldRepId, d => d.Status);

        var mtd = await db.Orders.ForTenant(tenant)
            .Where(o => o.FieldRepId != null && ids.Contains(o.FieldRepId.Value)
                        && o.OrderDate >= monthStart && IsRevenue(o.Status))
            .GroupBy(o => o.FieldRepId!.Value)
            .Select(g => new { RepId = g.Key, Value = g.Sum(x => x.TotalAmount) })
            .ToDictionaryAsync(x => x.RepId, x => x.Value);

        var visitStats = await db.Visits.ForTenant(tenant)
            .Where(v => ids.Contains(v.FieldRepId) && v.CheckedInAt >= monthStart)
            .GroupBy(v => v.FieldRepId)
            .Select(g => new
            {
                RepId = g.Key,
                Actual = g.Count(),
                Productive = g.Count(x => x.IsProductive),
                Planned = g.Count(x => x.IsPlanned),
            })
            .ToDictionaryAsync(x => x.RepId);

        foreach (var dto in dtos)
        {
            if (routeCounts.TryGetValue(dto.Id, out var r))
            {
                dto.RouteCount = r.Routes;
                dto.OutletCount = r.Outlets;
            }
            if (todayDays.TryGetValue(dto.Id, out var status)) dto.TodayStatus = status;
            dto.MonthToDateSales = mtd.GetValueOrDefault(dto.Id);
            if (visitStats.TryGetValue(dto.Id, out var v))
            {
                dto.CoveragePercent = DistributionMapper.Percent(v.Actual, v.Planned);
                dto.StrikeRatePercent = DistributionMapper.Percent(v.Productive, v.Actual);
            }
        }

        return PaginatedResponse<FieldRepDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<FieldRepDto?> GetRepAsync(Guid repId)
    {
        var entity = await db.FieldReps.ForTenant(tenant)
            .Include(r => r.Partner)
            .FirstOrDefaultAsync(r => r.Id == repId);

        if (entity is null) return null;

        var dto = entity.ToDto();
        dto.RouteCount = await db.Routes.ForTenant(tenant).CountAsync(r => r.FieldRepId == repId);

        if (entity.TerritoryId.HasValue)
            dto.TerritoryName = await db.Territories.ForTenant(tenant)
                .Where(t => t.Id == entity.TerritoryId).Select(t => t.Name).FirstOrDefaultAsync();

        if (entity.ReportsToFieldRepId.HasValue)
            dto.ReportsToName = await db.FieldReps.ForTenant(tenant)
                .Where(r => r.Id == entity.ReportsToFieldRepId).Select(r => r.FullName).FirstOrDefaultAsync();

        return dto;
    }

    public async Task<FieldRepDto> SaveRepAsync(Guid? repId, SaveFieldRepDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new InvalidOperationException("A field rep needs a name.");

        if (repId.HasValue && request.ReportsToFieldRepId == repId)
            throw new InvalidOperationException("A rep cannot report to themselves.");

        FieldRep entity;
        if (repId.HasValue)
        {
            entity = await db.FieldReps.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == repId)
                ?? throw new InvalidOperationException("That field rep no longer exists.");
            entity.StampUpdated(userId);
        }
        else
        {
            entity = new FieldRep().StampNew(tenant, userId);
            entity.Code = string.IsNullOrWhiteSpace(request.Code)
                ? await numbering.NextMasterCodeAsync(db.FieldReps, "REP")
                : request.Code;
            db.FieldReps.Add(entity);
        }

        if (repId.HasValue && !string.IsNullOrWhiteSpace(request.Code)) entity.Code = request.Code;

        entity.FullName = request.FullName.Trim();
        entity.DisplayName = request.DisplayName;
        entity.Role = request.Role;
        entity.Phone = request.Phone;
        entity.Email = request.Email;
        entity.PhotoUrl = request.PhotoUrl;
        entity.UserId = request.UserId;
        entity.EmployeeId = request.EmployeeId;
        entity.PartnerId = request.PartnerId;
        entity.TerritoryId = request.TerritoryId;
        entity.ReportsToFieldRepId = request.ReportsToFieldRepId;
        entity.DefaultVanUnitId = request.DefaultVanUnitId;
        entity.DefaultWarehouseId = request.DefaultWarehouseId;
        entity.JoinedOn = request.JoinedOn;
        entity.LeftOn = request.LeftOn;
        entity.CashHoldingLimit = request.CashHoldingLimit;
        entity.DiscountAuthorityPercent = request.DiscountAuthorityPercent;
        entity.CanOnboardOutlets = request.CanOnboardOutlets;
        entity.CanCollectPayments = request.CanCollectPayments;
        entity.CanAcceptReturns = request.CanAcceptReturns;
        entity.IsActive = request.IsActive;
        entity.Note = request.Note;

        // PINs are hashed and never round-tripped: a shared device is exactly where a plaintext
        // credential would leak.
        if (!string.IsNullOrWhiteSpace(request.Pin))
        {
            if (request.Pin.Trim().Length < 4)
                throw new InvalidOperationException("A PIN must be at least four digits.");
            entity.PinHash = BCrypt.Net.BCrypt.HashPassword(request.Pin.Trim());
        }

        await db.SaveChangesAsync();
        return (await GetRepAsync(entity.Id))!;
    }

    public async Task DeleteRepAsync(Guid repId, Guid userId)
    {
        var entity = await db.FieldReps.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == repId)
            ?? throw new InvalidOperationException("That field rep no longer exists.");

        if (await db.FieldDays.ForTenant(tenant).AnyAsync(d => d.FieldRepId == repId && d.Status == FieldDayStatus.Started))
            throw new InvalidOperationException("This rep has an open day. Close it first.");

        if (await db.Routes.ForTenant(tenant).AnyAsync(r => r.FieldRepId == repId))
            throw new InvalidOperationException("Reassign this rep's routes before removing them.");

        entity.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    public async Task<FieldRepDto?> AuthenticatePinAsync(string code, string pin)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(pin)) return null;

        var entity = await db.FieldReps.ForTenant(tenant)
            .Include(r => r.Partner)
            .FirstOrDefaultAsync(r => r.Code == code && r.IsActive);

        if (entity?.PinHash is null) return null;
        if (!BCrypt.Net.BCrypt.Verify(pin, entity.PinHash)) return null;

        return entity.ToDto();
    }

    public async Task<List<FieldDeviceDto>> ListDevicesAsync(Guid? fieldRepId, bool? staleOnly)
    {
        var staleBefore = DateTime.UtcNow.AddHours(-24);

        var rows = await db.FieldDevices.ForTenant(tenant)
            .Include(d => d.FieldRep)
            .WhereIf(fieldRepId.HasValue, d => d.FieldRepId == fieldRepId)
            .WhereIf(staleOnly == true, d => d.LastSyncAt == null || d.LastSyncAt < staleBefore
                                             || d.PendingOutboxCount > 0)
            .OrderByDescending(d => d.LastSeenAt)
            .ToListAsync();

        return rows.Select(d => d.ToDto()).ToList();
    }

    public async Task<FieldDeviceDto> RegisterDeviceAsync(FieldDeviceDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceIdentifier))
            throw new InvalidOperationException("A device needs an identifier.");

        var entity = await db.FieldDevices.ForTenant(tenant)
            .Include(d => d.FieldRep)
            .FirstOrDefaultAsync(d => d.DeviceIdentifier == request.DeviceIdentifier);

        if (entity is null)
        {
            entity = new FieldDevice
            {
                DeviceIdentifier = request.DeviceIdentifier,
                RegisteredAt = DateTime.UtcNow,
            }.StampNew(tenant, userId);
            db.FieldDevices.Add(entity);
        }
        else
        {
            entity.StampUpdated(userId);
        }

        entity.FieldRepId = request.FieldRepId ?? entity.FieldRepId;
        entity.DeviceName = request.DeviceName ?? entity.DeviceName;
        entity.Platform = request.Platform ?? entity.Platform;
        entity.OsVersion = request.OsVersion ?? entity.OsVersion;
        entity.AppVersion = request.AppVersion ?? entity.AppVersion;
        entity.LastSeenAt = DateTime.UtcNow;
        entity.LastSyncAt = DateTime.UtcNow;
        entity.PendingOutboxCount = request.PendingOutboxCount;

        // A wipe is acknowledged the moment the device comes back and reports in.
        if (entity.WipeRequested && entity.WipedAt is null) entity.WipedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return entity.ToDto();
    }

    public async Task<FieldDeviceDto> BlockDeviceAsync(Guid deviceId, bool isBlocked, string? reason, Guid userId)
    {
        if (isBlocked && string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Blocking a device needs a reason.");

        var entity = await db.FieldDevices.ForTenant(tenant)
            .Include(d => d.FieldRep)
            .FirstOrDefaultAsync(d => d.Id == deviceId)
            ?? throw new InvalidOperationException("That device is not registered.");

        entity.IsBlocked = isBlocked;
        entity.BlockReason = isBlocked ? reason : null;
        entity.StampUpdated(userId);

        await db.SaveChangesAsync();
        return entity.ToDto();
    }

    public async Task<FieldDeviceDto> RequestWipeAsync(Guid deviceId, Guid userId)
    {
        var entity = await db.FieldDevices.ForTenant(tenant)
            .Include(d => d.FieldRep)
            .FirstOrDefaultAsync(d => d.Id == deviceId)
            ?? throw new InvalidOperationException("That device is not registered.");

        entity.WipeRequested = true;
        entity.IsBlocked = true;
        entity.BlockReason ??= "Wipe requested";
        entity.WipedAt = null;
        entity.StampUpdated(userId);

        await db.SaveChangesAsync();
        return entity.ToDto();
    }

    // ═══ The day ═════════════════════════════════════════════════════════════

    public async Task<FieldDayDto> StartDayAsync(StartDayDto request, Guid userId)
    {
        var workDate = (request.WorkDate == default ? DateTime.UtcNow : request.WorkDate).Date;

        var rep = await db.FieldReps.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == request.FieldRepId)
            ?? throw new InvalidOperationException("That field rep no longer exists.");

        if (!rep.IsActive)
            throw new InvalidOperationException($"{rep.FullName} is no longer active.");

        // One day per rep per date. Two open days is how a settlement goes missing.
        var existing = await db.FieldDays.ForTenant(tenant)
            .Include(d => d.FieldRep).Include(d => d.Route)
            .FirstOrDefaultAsync(d => d.FieldRepId == request.FieldRepId && d.WorkDate == workDate);

        if (existing is not null)
        {
            if (existing.Status == FieldDayStatus.NotStarted)
            {
                existing.Status = FieldDayStatus.Started;
                existing.StartedAt = DateTime.UtcNow;
                existing.StampUpdated(userId);
                await db.SaveChangesAsync();
            }
            return existing.ToDto();
        }

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        if ((settings?.RequireStartSelfie ?? false) && string.IsNullOrWhiteSpace(request.SelfieUrl))
            throw new InvalidOperationException("A photo is required to start the day.");

        // The route comes from the published journey plan when there is one, so plan-vs-actual is
        // measured against what was actually planned rather than what the rep chose this morning.
        var routeId = request.RouteId;
        Guid? planDayId = null;

        var planDay = await db.JourneyPlanDays.ForTenant(tenant)
            .Include(d => d.JourneyPlan)
            .Where(d => d.PlanDate == workDate
                        && d.JourneyPlan != null
                        && d.JourneyPlan.FieldRepId == request.FieldRepId
                        && d.JourneyPlan.IsPublished)
            .FirstOrDefaultAsync();

        if (planDay is not null)
        {
            routeId ??= planDay.RouteId;
            planDayId = planDay.Id;
        }

        var day = new FieldDay
        {
            FieldRepId = request.FieldRepId,
            WorkDate = workDate,
            RouteId = routeId,
            JourneyPlanDayId = planDayId,
            VanUnitId = request.VanUnitId ?? rep.DefaultVanUnitId,
            Status = FieldDayStatus.Started,
            StartedAt = DateTime.UtcNow,
            StartLatitude = request.Latitude,
            StartLongitude = request.Longitude,
            StartSelfieUrl = request.SelfieUrl,
        }.StampNew(tenant, userId);

        if (routeId.HasValue)
            day.PlannedCalls = await db.RouteOutlets.ForTenant(tenant).CountAsync(r => r.RouteId == routeId);

        db.FieldDays.Add(day);

        if (planDay is not null)
        {
            planDay.Status = JourneyPlanDayStatus.InProgress;
            planDay.FieldDayId = day.Id;
            planDay.StampUpdated(userId);
        }

        if (!string.IsNullOrWhiteSpace(request.DeviceIdentifier))
            await RegisterDeviceAsync(new FieldDeviceDto
            {
                DeviceIdentifier = request.DeviceIdentifier,
                FieldRepId = request.FieldRepId,
            }, userId);

        await db.SaveChangesAsync();

        await db.Entry(day).Reference(d => d.FieldRep).LoadAsync();
        await db.Entry(day).Reference(d => d.Route).LoadAsync();
        return day.ToDto();
    }

    public async Task<FieldDayDto?> GetDayAsync(Guid fieldDayId)
    {
        var entity = await db.FieldDays.ForTenant(tenant)
            .Include(d => d.FieldRep).Include(d => d.Route)
            .FirstOrDefaultAsync(d => d.Id == fieldDayId);

        if (entity is null) return null;

        var dto = entity.ToDto();
        var settlement = await db.Settlements.ForTenant(tenant)
            .Where(s => s.FieldDayId == fieldDayId)
            .Select(s => new { s.Id, s.Status })
            .FirstOrDefaultAsync();

        if (settlement is not null)
        {
            dto.SettlementId = settlement.Id;
            dto.SettlementStatus = settlement.Status;
        }

        if (entity.VanUnitId.HasValue)
            dto.VanUnitName = await db.VanUnits.ForTenant(tenant)
                .Where(v => v.Id == entity.VanUnitId).Select(v => v.Name).FirstOrDefaultAsync();

        return dto;
    }

    public async Task<FieldDayDto?> GetTodayAsync(Guid fieldRepId, DateTime? workDate)
    {
        var date = (workDate ?? DateTime.UtcNow).Date;
        var entity = await db.FieldDays.ForTenant(tenant)
            .Where(d => d.FieldRepId == fieldRepId && d.WorkDate == date)
            .Select(d => d.Id)
            .FirstOrDefaultAsync();

        return entity == Guid.Empty ? null : await GetDayAsync(entity);
    }

    public async Task<FieldDayBoardDto?> GetDayBoardAsync(Guid fieldDayId)
    {
        var day = await db.FieldDays.ForTenant(tenant)
            .Include(d => d.FieldRep).Include(d => d.Route)
            .FirstOrDefaultAsync(d => d.Id == fieldDayId);

        if (day is null) return null;

        var board = new FieldDayBoardDto { Day = (await GetDayAsync(fieldDayId))! };

        // The beat: planned stops from the route, merged with visits already recorded today
        // (including unplanned ones the rep added themselves).
        var visits = await db.Visits.ForTenant(tenant)
            .Include(v => v.Outlet)
            .Where(v => v.FieldDayId == fieldDayId)
            .ToListAsync();

        var plannedStops = day.RouteId is null
            ? []
            : await db.RouteOutlets.ForTenant(tenant)
                .Include(r => r.Outlet)
                .Where(r => r.RouteId == day.RouteId)
                .OrderBy(r => r.StopSequence)
                .ToListAsync();

        var outletIds = plannedStops.Select(s => s.OutletId)
            .Concat(visits.Select(v => v.OutletId))
            .Distinct().ToList();

        var credit = await db.CreditProfiles.ForTenant(tenant)
            .Where(c => c.OutletId != null && outletIds.Contains(c.OutletId.Value))
            .ToDictionaryAsync(c => c.OutletId!.Value);

        var openTasks = await db.VisitTasks.ForTenant(tenant)
            .Where(t => t.OutletId != null && outletIds.Contains(t.OutletId.Value) && t.CompletedAt == null)
            .GroupBy(t => t.OutletId!.Value)
            .Select(g => new { OutletId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OutletId, x => x.Count);

        var assetCounts = await db.OutletAssets.ForTenant(tenant)
            .Where(a => outletIds.Contains(a.OutletId) && a.RetrievedOn == null)
            .GroupBy(a => a.OutletId)
            .Select(g => new { OutletId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OutletId, x => x.Count);

        var pinned = await db.OutletNotes.ForTenant(tenant)
            .Where(n => outletIds.Contains(n.OutletId) && n.IsPinned)
            .GroupBy(n => n.OutletId)
            .Select(g => new { OutletId = g.Key, Text = g.OrderByDescending(x => x.NotedAt).First().Text })
            .ToDictionaryAsync(x => x.OutletId, x => x.Text);

        var cards = new List<VisitCardDto>();

        foreach (var stop in plannedStops)
        {
            if (stop.Outlet is null) continue;
            var visit = visits.FirstOrDefault(v => v.OutletId == stop.OutletId);
            cards.Add(BuildCard(stop.Outlet, visit, stop.StopSequence, stop.IsMustVisit, true,
                credit, openTasks, assetCounts, pinned));
        }

        foreach (var visit in visits.Where(v => !v.IsPlanned && v.Outlet is not null))
            cards.Add(BuildCard(visit.Outlet!, visit, visit.StopSequence, false, false,
                credit, openTasks, assetCounts, pinned));

        board.Stops = cards.OrderBy(c => c.StopSequence).ToList();
        board.PendingStops = cards.Count(c => c.Status is VisitStatus.Pending or VisitStatus.CheckedIn);
        board.CompletedStops = cards.Count(c => c.Status is VisitStatus.CheckedOut or VisitStatus.OrderTaken
                                                or VisitStatus.NoOrder);

        board.Tasks = (await db.VisitTasks.ForTenant(tenant)
                .Where(t => t.CompletedAt == null
                            && (t.FieldRepId == day.FieldRepId || t.RouteId == day.RouteId
                                || (t.OutletId != null && outletIds.Contains(t.OutletId.Value))))
                .OrderByDescending(t => t.Priority).ThenBy(t => t.DueOn)
                .Take(50).ToListAsync())
            .Select(t => t.ToDto()).ToList();

        board.Surveys = (await db.SurveyForms.ForTenant(tenant)
                .Include(f => f.Questions.Where(q => !q.IsDeleted))
                .Where(f => f.IsActive
                            && (f.ActiveFrom == null || f.ActiveFrom <= day.WorkDate)
                            && (f.ActiveTo == null || f.ActiveTo >= day.WorkDate)
                            && (f.RouteId == null || f.RouteId == day.RouteId))
                .ToListAsync())
            .Select(f => f.ToDto()).ToList();

        board.MustSellItems = await BuildFocusItemsAsync(day.WorkDate);

        if (day.VanUnitId.HasValue)
            board.VanStock = await BuildVanSummaryAsync(day.VanUnitId.Value);

        var target = await db.Targets.ForTenant(tenant)
            .Where(t => t.FieldRepId == day.FieldRepId && t.Metric == TargetMetric.SalesValue
                        && t.PeriodStart <= day.WorkDate && t.PeriodEnd >= day.WorkDate)
            .FirstOrDefaultAsync();

        if (target is not null)
        {
            var workingDays = Math.Max(1, (target.PeriodEnd - target.PeriodStart).Days + 1);
            board.TargetValue = Math.Round(target.TargetValue / workingDays, 2);
            board.AchievedValue = day.OrderValue;
        }

        var collectionTarget = await db.Targets.ForTenant(tenant)
            .Where(t => t.FieldRepId == day.FieldRepId && t.Metric == TargetMetric.Collection
                        && t.PeriodStart <= day.WorkDate && t.PeriodEnd >= day.WorkDate)
            .Select(t => t.TargetValue).FirstOrDefaultAsync();

        if (collectionTarget > 0)
        {
            var workingDays = 26m;
            board.CollectionTarget = Math.Round(collectionTarget / workingDays, 2);
        }

        board.CollectedAmount = day.CollectedAmount;
        return board;
    }

    public async Task<FieldDayDto> CloseDayAsync(CloseDayDto request, Guid userId)
    {
        var day = await db.FieldDays.ForTenant(tenant)
            .Include(d => d.FieldRep).Include(d => d.Route)
            .FirstOrDefaultAsync(d => d.Id == request.FieldDayId)
            ?? throw new InvalidOperationException("That day no longer exists.");

        if (day.Status is FieldDayStatus.Closed or FieldDayStatus.ForceClosed)
            throw new InvalidOperationException("This day is already closed.");

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        // Open visits are ambiguous: was the rep still in the shop, or did they forget? Closing
        // them silently would inflate coverage with calls that never finished.
        var openVisits = await db.Visits.ForTenant(tenant)
            .Where(v => v.FieldDayId == day.Id && v.CheckedInAt != null && v.CheckedOutAt == null)
            .ToListAsync();

        if (openVisits.Count > 0 && !request.ForceClose)
            throw new InvalidOperationException(
                $"{openVisits.Count} visit(s) are still checked in. Check out of them before closing the day.");

        if ((settings?.BlockDayCloseWithUnsynced ?? true) && day.UnsyncedCount > 0 && !request.ForceClose)
            throw new InvalidOperationException(
                $"{day.UnsyncedCount} transaction(s) have not synced. Reconnect, or ask a supervisor to force-close.");

        if (request.ForceClose && string.IsNullOrWhiteSpace(request.ForceCloseReason))
            throw new InvalidOperationException("Force-closing a day needs a reason.");

        foreach (var visit in openVisits)
        {
            visit.CheckedOutAt = DateTime.UtcNow;
            visit.Status = visit.IsProductive ? VisitStatus.OrderTaken : VisitStatus.NoOrder;
            visit.DurationMinutes = visit.CheckedInAt is null
                ? null
                : (int)(DateTime.UtcNow - visit.CheckedInAt.Value).TotalMinutes;
            visit.Note = $"{visit.Note} Auto-closed at day end.".Trim();
            visit.StampUpdated(userId);
        }

        day.Status = request.ForceClose ? FieldDayStatus.ForceClosed : FieldDayStatus.Closed;
        day.ClosedAt = DateTime.UtcNow;
        day.CashDeclared = request.CashDeclared;
        day.DistanceCoveredKm = request.DistanceCoveredKm;
        day.EndLatitude = request.Latitude;
        day.EndLongitude = request.Longitude;
        day.Note = request.Note;
        day.ForceCloseReason = request.ForceCloseReason;
        if (request.ForceClose) day.ForceClosedByUserId = userId;
        day.StampUpdated(userId);

        await RecomputeDayCountersAsync(day);

        if (day.JourneyPlanDayId.HasValue)
        {
            var planDay = await db.JourneyPlanDays.ForTenant(tenant)
                .Include(d => d.JourneyPlan)
                .FirstOrDefaultAsync(d => d.Id == day.JourneyPlanDayId);

            if (planDay is not null)
            {
                planDay.Status = JourneyPlanDayStatus.Completed;
                planDay.ActualCalls = day.ActualCalls;
                planDay.ProductiveCalls = day.ProductiveCalls;
                planDay.StampUpdated(userId);

                if (planDay.JourneyPlan is not null)
                {
                    planDay.JourneyPlan.ActualCalls += day.ActualCalls;
                    planDay.JourneyPlan.ProductiveCalls += day.ProductiveCalls;
                    planDay.JourneyPlan.UnplannedCalls += day.UnplannedCalls;
                    planDay.JourneyPlan.MissedCalls += Math.Max(0, day.PlannedCalls - day.ActualCalls);
                }
            }
        }

        await db.SaveChangesAsync();
        return (await GetDayAsync(day.Id))!;
    }

    public async Task<PaginatedResponse<FieldDayDto>> ListDaysAsync(
        Guid? fieldRepId, Guid? routeId, DateTime? from, DateTime? to, FieldDayStatus? status,
        PaginationParams pagination)
    {
        var query = db.FieldDays.ForTenant(tenant)
            .Include(d => d.FieldRep).Include(d => d.Route)
            .WhereIf(fieldRepId.HasValue, d => d.FieldRepId == fieldRepId)
            .WhereIf(routeId.HasValue, d => d.RouteId == routeId)
            .WhereIf(from.HasValue, d => d.WorkDate >= from)
            .WhereIf(to.HasValue, d => d.WorkDate <= to)
            .WhereIf(status.HasValue, d => d.Status == status);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(d => d.WorkDate)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        return PaginatedResponse<FieldDayDto>.Ok(
            rows.Select(r => r.ToDto()), total, pagination.PageNumber, pagination.PageSize);
    }

    // ═══ Visits ══════════════════════════════════════════════════════════════

    public async Task<VisitDto> CheckInAsync(CheckInDto request, Guid userId)
    {
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var replay = await db.Visits.ForTenant(tenant)
                .Include(v => v.Outlet)
                .FirstOrDefaultAsync(v => v.IdempotencyKey == request.IdempotencyKey);
            if (replay is not null) return replay.ToDto();
        }

        var day = await db.FieldDays.ForTenant(tenant).FirstOrDefaultAsync(d => d.Id == request.FieldDayId)
            ?? throw new InvalidOperationException("That day no longer exists.");

        if (day.Status != FieldDayStatus.Started)
            throw new InvalidOperationException("The day must be started before visiting an outlet.");

        var outlet = await db.Outlets.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == request.OutletId)
            ?? throw new InvalidOperationException("That outlet no longer exists.");

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        // A rep already inside a shop cannot be inside another one.
        var openElsewhere = await db.Visits.ForTenant(tenant)
            .AnyAsync(v => v.FieldDayId == day.Id && v.CheckedInAt != null && v.CheckedOutAt == null
                           && v.OutletId != request.OutletId);

        if (openElsewhere)
            throw new InvalidOperationException("You are still checked in at another outlet.");

        var existing = await db.Visits.ForTenant(tenant)
            .Include(v => v.Outlet)
            .FirstOrDefaultAsync(v => v.FieldDayId == day.Id && v.OutletId == request.OutletId
                                      && v.CheckedOutAt == null);

        var (validation, distance) = ValidateGeo(outlet, request.Latitude, request.Longitude, settings);

        if (validation == GeoValidation.OutsideFence)
        {
            if (!(settings?.AllowOutOfFenceCheckIn ?? true))
                throw new InvalidOperationException(
                    $"You are {distance:N0}m from {outlet.Name}. Move closer to check in.");

            if (string.IsNullOrWhiteSpace(request.GeoExceptionReason))
                throw new InvalidOperationException(
                    $"You are {distance:N0}m from {outlet.Name}. Give a reason to check in from here.");
        }

        if (validation == GeoValidation.NoFix && (settings?.RequireGeoOnCheckIn ?? true)
            && string.IsNullOrWhiteSpace(request.GeoExceptionReason))
            throw new InvalidOperationException("No location fix. Give a reason to check in without one.");

        var isPlanned = !request.IsUnplanned;
        if (day.RouteId.HasValue)
            isPlanned = await db.RouteOutlets.ForTenant(tenant)
                .AnyAsync(r => r.RouteId == day.RouteId && r.OutletId == request.OutletId);

        if (!isPlanned && settings is not null && settings.MaxUnplannedVisitsPerDay > 0
            && day.UnplannedCalls >= settings.MaxUnplannedVisitsPerDay)
            throw new InvalidOperationException(
                $"You have already made {settings.MaxUnplannedVisitsPerDay} unplanned visits today.");

        var visit = existing;
        if (visit is null)
        {
            var sequence = await db.RouteOutlets.ForTenant(tenant)
                .Where(r => r.RouteId == day.RouteId && r.OutletId == request.OutletId)
                .Select(r => (int?)r.StopSequence).FirstOrDefaultAsync()
                ?? (await db.Visits.ForTenant(tenant).Where(v => v.FieldDayId == day.Id)
                        .Select(v => (int?)v.StopSequence).MaxAsync() ?? 0) + 1;

            visit = new OutletVisit
            {
                FieldDayId = day.Id,
                OutletId = request.OutletId,
                RouteId = day.RouteId,
                FieldRepId = day.FieldRepId,
                StopSequence = sequence,
                IsPlanned = isPlanned,
                IdempotencyKey = request.IdempotencyKey,
            }.StampNew(tenant, userId);

            db.Visits.Add(visit);
        }
        else
        {
            visit.StampUpdated(userId);
        }

        visit.Status = VisitStatus.CheckedIn;
        visit.CheckedInAt = DateTime.UtcNow;
        visit.CheckInLatitude = request.Latitude;
        visit.CheckInLongitude = request.Longitude;
        visit.GeoValidation = validation;
        visit.DistanceFromOutletMetres = distance;
        visit.GeoExceptionReason = request.GeoExceptionReason;

        visit.MustSellTargetCount = (await BuildFocusItemsAsync(day.WorkDate)).Count;

        outlet.LastVisitAt = DateTime.UtcNow;
        outlet.TotalVisits++;
        outlet.StampUpdated(userId);

        day.ActualCalls++;
        if (!isPlanned) day.UnplannedCalls++;
        day.StampUpdated(userId);

        await db.SaveChangesAsync();
        await db.Entry(visit).Reference(v => v.Outlet).LoadAsync();

        return visit.ToDto();
    }

    public async Task<VisitDto> CheckOutAsync(CheckOutDto request, Guid userId)
    {
        var visit = await db.Visits.ForTenant(tenant)
            .Include(v => v.Outlet)
            .FirstOrDefaultAsync(v => v.Id == request.VisitId)
            ?? throw new InvalidOperationException("That visit no longer exists.");

        if (visit.CheckedOutAt is not null)
            throw new InvalidOperationException("You have already checked out of this outlet.");

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        // A call with no order still has to be explained; the alternative is a strike rate nobody
        // can act on.
        if (!visit.IsProductive)
        {
            if ((settings?.RequireReasonOnNoOrder ?? true) && request.NoOrderReasonId is null)
                throw new InvalidOperationException("A visit with no order needs a reason.");

            if (request.NoOrderReasonId.HasValue)
            {
                var reason = await db.ReasonCodes.ForCompany(tenant)
                    .FirstOrDefaultAsync(r => r.Id == request.NoOrderReasonId);

                if (reason is null) throw new InvalidOperationException("That reason is not recognised.");
                if (reason.RequiresNote && string.IsNullOrWhiteSpace(request.NoOrderNote))
                    throw new InvalidOperationException($"'{reason.Name}' needs a note.");
            }
        }

        var mandatorySurvey = await db.SurveyForms.ForTenant(tenant)
            .AnyAsync(f => f.IsActive && f.IsMandatory
                           && (f.ActiveFrom == null || f.ActiveFrom <= DateTime.UtcNow)
                           && (f.ActiveTo == null || f.ActiveTo >= DateTime.UtcNow));

        if (mandatorySurvey && !visit.SurveyCompleted)
            throw new InvalidOperationException("A mandatory survey is outstanding for this visit.");

        visit.CheckedOutAt = DateTime.UtcNow;
        visit.CheckOutLatitude = request.Latitude;
        visit.CheckOutLongitude = request.Longitude;
        visit.NoOrderReasonId = request.NoOrderReasonId;
        visit.NoOrderNote = request.NoOrderNote;
        visit.AssetsVerified = request.AssetsVerified;
        visit.Note = request.Note;
        visit.Status = visit.IsProductive ? VisitStatus.CheckedOut : VisitStatus.NoOrder;
        visit.DurationMinutes = visit.CheckedInAt is null
            ? null
            : Math.Max(0, (int)(DateTime.UtcNow - visit.CheckedInAt.Value).TotalMinutes);
        visit.StampUpdated(userId);

        if (visit.IsProductive)
        {
            var day = await db.FieldDays.ForTenant(tenant).FirstOrDefaultAsync(d => d.Id == visit.FieldDayId);
            if (day is not null)
            {
                day.ProductiveCalls++;
                day.StampUpdated(userId);
            }

            if (visit.Outlet is not null)
            {
                visit.Outlet.ProductiveVisits++;
                visit.Outlet.StampUpdated(userId);
            }
        }

        await db.SaveChangesAsync();
        return visit.ToDto();
    }

    public async Task<VisitDto?> GetVisitAsync(Guid visitId)
    {
        var entity = await db.Visits.ForTenant(tenant)
            .Include(v => v.Outlet)
            .FirstOrDefaultAsync(v => v.Id == visitId);

        if (entity is null) return null;

        var dto = entity.ToDto();

        if (entity.NoOrderReasonId.HasValue)
            dto.NoOrderReasonName = await db.ReasonCodes.ForCompany(tenant)
                .Where(r => r.Id == entity.NoOrderReasonId).Select(r => r.Name).FirstOrDefaultAsync();

        if (entity.OrderId.HasValue)
            dto.OrderNumber = await db.Orders.ForTenant(tenant)
                .Where(o => o.Id == entity.OrderId).Select(o => o.OrderNumber).FirstOrDefaultAsync();

        dto.FieldRepName = await db.FieldReps.ForTenant(tenant)
            .Where(r => r.Id == entity.FieldRepId).Select(r => r.FullName).FirstOrDefaultAsync();

        return dto;
    }

    public async Task<PaginatedResponse<VisitSummaryDto>> ListVisitsAsync(
        Guid? fieldRepId, Guid? outletId, Guid? routeId, DateTime? from, DateTime? to,
        VisitStatus? status, bool? outOfFenceOnly, PaginationParams pagination)
    {
        var query = db.Visits.ForTenant(tenant)
            .Include(v => v.Outlet)
            .WhereIf(fieldRepId.HasValue, v => v.FieldRepId == fieldRepId)
            .WhereIf(outletId.HasValue, v => v.OutletId == outletId)
            .WhereIf(routeId.HasValue, v => v.RouteId == routeId)
            .WhereIf(from.HasValue, v => v.CheckedInAt >= from)
            .WhereIf(to.HasValue, v => v.CheckedInAt <= to)
            .WhereIf(status.HasValue, v => v.Status == status)
            .WhereIf(outOfFenceOnly == true, v => v.GeoValidation == GeoValidation.OutsideFence);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(v => v.CheckedInAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var dtos = rows.Select(r => r.ToSummary()).ToList();

        var repIds = rows.Select(r => r.FieldRepId).Distinct().ToList();
        var routeIds = rows.Where(r => r.RouteId.HasValue).Select(r => r.RouteId!.Value).Distinct().ToList();
        var reasonIds = rows.Where(r => r.NoOrderReasonId.HasValue).Select(r => r.NoOrderReasonId!.Value).Distinct().ToList();

        var reps = await db.FieldReps.ForTenant(tenant)
            .Where(r => repIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.FullName);
        var routes = await db.Routes.ForTenant(tenant)
            .Where(r => routeIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.Name);
        var reasons = await db.ReasonCodes.ForCompany(tenant)
            .Where(r => reasonIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.Name);

        for (var i = 0; i < rows.Count; i++)
        {
            dtos[i].FieldRepName = reps.GetValueOrDefault(rows[i].FieldRepId);
            if (rows[i].RouteId.HasValue) dtos[i].RouteName = routes.GetValueOrDefault(rows[i].RouteId!.Value);
            if (rows[i].NoOrderReasonId.HasValue)
                dtos[i].NoOrderReasonName = reasons.GetValueOrDefault(rows[i].NoOrderReasonId!.Value);
        }

        return PaginatedResponse<VisitSummaryDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    // ═══ Tasks ═══════════════════════════════════════════════════════════════

    public async Task<List<VisitTaskDto>> ListTasksAsync(
        Guid? fieldRepId, Guid? outletId, Guid? routeId, bool? openOnly)
        => (await db.VisitTasks.ForTenant(tenant)
                .WhereIf(fieldRepId.HasValue, t => t.FieldRepId == fieldRepId)
                .WhereIf(outletId.HasValue, t => t.OutletId == outletId)
                .WhereIf(routeId.HasValue, t => t.RouteId == routeId)
                .WhereIf(openOnly == true, t => t.CompletedAt == null)
                .OrderByDescending(t => t.Priority).ThenBy(t => t.DueOn)
                .ToListAsync())
            .Select(t => t.ToDto()).ToList();

    public async Task<VisitTaskDto> SaveTaskAsync(Guid? taskId, SaveVisitTaskDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new InvalidOperationException("A task needs a title.");

        if (request.OutletId is null && request.RouteId is null
            && request.TerritoryId is null && request.FieldRepId is null)
            throw new InvalidOperationException("A task needs somebody or somewhere to belong to.");

        VisitTask entity;
        if (taskId.HasValue)
        {
            entity = await db.VisitTasks.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == taskId)
                ?? throw new InvalidOperationException("That task no longer exists.");
            entity.StampUpdated(userId);
        }
        else
        {
            entity = new VisitTask().StampNew(tenant, userId);
            db.VisitTasks.Add(entity);
        }

        entity.Title = request.Title.Trim();
        entity.Instructions = request.Instructions;
        entity.OutletId = request.OutletId;
        entity.RouteId = request.RouteId;
        entity.TerritoryId = request.TerritoryId;
        entity.FieldRepId = request.FieldRepId;
        entity.DueOn = request.DueOn == default ? DateTime.UtcNow.Date : request.DueOn;
        entity.RequiresPhoto = request.RequiresPhoto;
        entity.Priority = request.Priority;
        entity.IsMandatory = request.IsMandatory;

        await db.SaveChangesAsync();
        return entity.ToDto();
    }

    public async Task<VisitTaskDto> CompleteTaskAsync(CompleteVisitTaskDto request, Guid userId)
    {
        var entity = await db.VisitTasks.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == request.TaskId)
            ?? throw new InvalidOperationException("That task no longer exists.");

        if (entity.RequiresPhoto && string.IsNullOrWhiteSpace(request.PhotoUrl))
            throw new InvalidOperationException("This task needs a photo to close it.");

        entity.CompletedAt = DateTime.UtcNow;
        entity.VisitId = request.VisitId;
        entity.PhotoUrl = request.PhotoUrl;
        entity.CompletionNote = request.CompletionNote;
        entity.StampUpdated(userId);

        if (request.VisitId.HasValue)
        {
            var visit = await db.Visits.ForTenant(tenant).FirstOrDefaultAsync(v => v.Id == request.VisitId);
            if (visit is not null) entity.CompletedByFieldRepId = visit.FieldRepId;
        }

        await db.SaveChangesAsync();
        return entity.ToDto();
    }

    public async Task DeleteTaskAsync(Guid taskId, Guid userId)
    {
        var entity = await db.VisitTasks.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == taskId)
            ?? throw new InvalidOperationException("That task no longer exists.");

        entity.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    // ═══ Surveys ═════════════════════════════════════════════════════════════

    public async Task<List<SurveyFormDto>> ListSurveyFormsAsync(bool? activeOnly)
    {
        var today = DateTime.UtcNow.Date;
        var rows = await db.SurveyForms.ForTenant(tenant)
            .Include(f => f.Questions.Where(q => !q.IsDeleted))
            .WhereIf(activeOnly == true, f => f.IsActive
                                              && (f.ActiveFrom == null || f.ActiveFrom <= today)
                                              && (f.ActiveTo == null || f.ActiveTo >= today))
            .OrderBy(f => f.Name).ToListAsync();

        var dtos = rows.Select(f => f.ToDto()).ToList();
        var ids = rows.Select(f => f.Id).ToList();

        var counts = await db.SurveyResponses.ForTenant(tenant)
            .Where(r => ids.Contains(r.SurveyFormId))
            .GroupBy(r => r.SurveyFormId)
            .Select(g => new { FormId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.FormId, x => x.Count);

        foreach (var dto in dtos) dto.ResponseCount = counts.GetValueOrDefault(dto.Id);
        return dtos;
    }

    public async Task<SurveyFormDto?> GetSurveyFormAsync(Guid formId)
    {
        var entity = await db.SurveyForms.ForTenant(tenant)
            .Include(f => f.Questions.Where(q => !q.IsDeleted))
            .FirstOrDefaultAsync(f => f.Id == formId);

        return entity?.ToDto();
    }

    public async Task<SurveyFormDto> SaveSurveyFormAsync(Guid? formId, SurveyFormDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("A survey needs a name.");
        if (request.Questions.Count == 0)
            throw new InvalidOperationException("A survey needs at least one question.");

        SurveyForm entity;
        if (formId.HasValue)
        {
            entity = await db.SurveyForms.ForTenant(tenant)
                .Include(f => f.Questions)
                .FirstOrDefaultAsync(f => f.Id == formId)
                ?? throw new InvalidOperationException("That survey no longer exists.");
            entity.StampUpdated(userId);
        }
        else
        {
            entity = new SurveyForm().StampNew(tenant, userId);
            db.SurveyForms.Add(entity);
        }

        entity.Name = request.Name.Trim();
        entity.Instructions = request.Instructions;
        entity.ActiveFrom = request.ActiveFrom;
        entity.ActiveTo = request.ActiveTo;
        entity.ApplicableChannels = request.ApplicableChannels;
        entity.TerritoryId = request.TerritoryId;
        entity.RouteId = request.RouteId;
        entity.IsMandatory = request.IsMandatory;
        entity.RepeatEveryVisit = request.RepeatEveryVisit;
        entity.IsActive = request.IsActive;

        foreach (var existing in entity.Questions.Where(q => !q.IsDeleted).ToList())
            if (request.Questions.All(q => q.Id != existing.Id))
                existing.StampDeleted(userId);

        var order = 0;
        foreach (var dto in request.Questions)
        {
            var question = dto.Id != Guid.Empty ? entity.Questions.FirstOrDefault(q => q.Id == dto.Id) : null;
            if (question is null)
            {
                question = new SurveyQuestion { SurveyFormId = entity.Id }.StampNew(tenant, userId);
                entity.Questions.Add(question);
            }

            question.Text = dto.Text;
            question.Kind = dto.Kind;
            question.DisplayOrder = order++;
            question.IsRequired = dto.IsRequired;
            question.Options = dto.Options;
            question.MinValue = dto.MinValue;
            question.MaxValue = dto.MaxValue;
            question.Unit = dto.Unit;
            question.Guidance = dto.Guidance;
            question.ShowWhenQuestionId = dto.ShowWhenQuestionId;
            question.ShowWhenValue = dto.ShowWhenValue;
        }

        await db.SaveChangesAsync();
        return (await GetSurveyFormAsync(entity.Id))!;
    }

    public async Task DeleteSurveyFormAsync(Guid formId, Guid userId)
    {
        var entity = await db.SurveyForms.ForTenant(tenant).FirstOrDefaultAsync(f => f.Id == formId)
            ?? throw new InvalidOperationException("That survey no longer exists.");

        entity.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    public async Task<SurveyResponseDto> SubmitSurveyAsync(SubmitSurveyDto request, Guid userId)
    {
        var form = await db.SurveyForms.ForTenant(tenant)
            .Include(f => f.Questions.Where(q => !q.IsDeleted))
            .FirstOrDefaultAsync(f => f.Id == request.SurveyFormId)
            ?? throw new InvalidOperationException("That survey no longer exists.");

        var missing = form.Questions
            .Where(q => q.IsRequired)
            .Where(q => !request.Answers.Any(a => a.QuestionId == q.Id && HasValue(a)))
            .Select(q => q.Text)
            .Take(3).ToList();

        if (missing.Count > 0)
            throw new InvalidOperationException($"These answers are required: {string.Join("; ", missing)}");

        var response = new SurveyResponse
        {
            SurveyFormId = request.SurveyFormId,
            OutletId = request.OutletId,
            VisitId = request.VisitId,
            FieldRepId = request.FieldRepId,
            SubmittedAt = DateTime.UtcNow,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
        }.StampNew(tenant, userId);

        foreach (var answer in request.Answers)
        {
            var question = form.Questions.FirstOrDefault(q => q.Id == answer.QuestionId);
            if (question is null) continue;

            if (question.Kind == SurveyQuestionKind.Numeric && answer.NumericValue.HasValue)
            {
                if (question.MinValue.HasValue && answer.NumericValue < question.MinValue)
                    throw new InvalidOperationException($"'{question.Text}' must be at least {question.MinValue}.");
                if (question.MaxValue.HasValue && answer.NumericValue > question.MaxValue)
                    throw new InvalidOperationException($"'{question.Text}' must be at most {question.MaxValue}.");
            }

            response.Answers.Add(new SurveyAnswer
            {
                ResponseId = response.Id,
                QuestionId = question.Id,
                // The question text is copied so a later edit to the form does not rewrite history.
                QuestionText = question.Text,
                Kind = question.Kind,
                TextValue = answer.TextValue,
                NumericValue = answer.NumericValue,
                BoolValue = answer.BoolValue,
                DateValue = answer.DateValue,
                PhotoUrl = answer.PhotoUrl,
            }.StampNew(tenant, userId));
        }

        db.SurveyResponses.Add(response);

        if (request.VisitId.HasValue)
            await db.Visits.ForTenant(tenant).Where(v => v.Id == request.VisitId)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.SurveyCompleted, true));

        await db.SaveChangesAsync();
        return MapResponse(response, form.Name);
    }

    public async Task<PaginatedResponse<SurveyResponseDto>> ListSurveyResponsesAsync(
        Guid? formId, Guid? outletId, DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.SurveyResponses.ForTenant(tenant)
            .Include(r => r.SurveyForm)
            .Include(r => r.Answers)
            .WhereIf(formId.HasValue, r => r.SurveyFormId == formId)
            .WhereIf(outletId.HasValue, r => r.OutletId == outletId)
            .WhereIf(from.HasValue, r => r.SubmittedAt >= from)
            .WhereIf(to.HasValue, r => r.SubmittedAt <= to);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(r => r.SubmittedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var outletIds = rows.Select(r => r.OutletId).Distinct().ToList();
        var names = await db.Outlets.ForTenant(tenant)
            .Where(o => outletIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.Name);

        var dtos = rows.Select(r =>
        {
            var dto = MapResponse(r, r.SurveyForm?.Name);
            dto.OutletName = names.GetValueOrDefault(r.OutletId);
            return dto;
        });

        return PaginatedResponse<SurveyResponseDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    // ═══ Merchandising ═══════════════════════════════════════════════════════

    public async Task<MerchandisingAuditDto> SubmitAuditAsync(SubmitAuditDto request, Guid userId)
    {
        if (request.Lines.Count == 0)
            throw new InvalidOperationException("An audit needs at least one line.");

        var outlet = await db.Outlets.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == request.OutletId)
            ?? throw new InvalidOperationException("That outlet no longer exists.");

        var audit = new MerchandisingAudit
        {
            OutletId = request.OutletId,
            VisitId = request.VisitId,
            FieldRepId = request.FieldRepId,
            Kind = request.Kind,
            AuditedAt = DateTime.UtcNow,
            BeforePhotoUrl = request.BeforePhotoUrl,
            AfterPhotoUrl = request.AfterPhotoUrl,
            Note = request.Note,
        }.StampNew(tenant, userId);

        foreach (var line in request.Lines)
            audit.Lines.Add(new MerchandisingAuditLine
            {
                AuditId = audit.Id,
                ItemId = line.ItemId,
                ItemName = line.ItemName,
                ItemCode = line.ItemCode,
                IsExpectedInAssortment = line.IsExpectedInAssortment,
                IsPresent = line.IsPresent,
                Facings = line.Facings,
                ExpectedFacings = line.ExpectedFacings,
                ShelfStock = line.ShelfStock,
                ObservedPrice = line.ObservedPrice,
                MandatedPrice = line.MandatedPrice,
                IsPriceCompliant = ComputePriceCompliance(line),
                IsCorrectPosition = line.IsCorrectPosition,
                Note = line.Note,
            }.StampNew(tenant, userId));

        ScoreAudit(audit);
        db.MerchandisingAudits.Add(audit);

        // The outlet carries the latest score so the beat list can be sorted by execution quality
        // without joining the audit table.
        outlet.PerfectStoreScore = audit.Score;
        outlet.StampUpdated(userId);

        if (request.VisitId.HasValue)
            await db.Visits.ForTenant(tenant).Where(v => v.Id == request.VisitId)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.AuditCompleted, true));

        await db.SaveChangesAsync();
        return (await GetAuditAsync(audit.Id))!;
    }

    public async Task<MerchandisingAuditDto?> GetAuditAsync(Guid auditId)
    {
        var entity = await db.MerchandisingAudits.ForTenant(tenant)
            .Include(a => a.Outlet)
            .Include(a => a.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(a => a.Id == auditId);

        if (entity is null) return null;

        var dto = entity.ToDto();
        dto.FieldRepName = await db.FieldReps.ForTenant(tenant)
            .Where(r => r.Id == entity.FieldRepId).Select(r => r.FullName).FirstOrDefaultAsync();

        return dto;
    }

    public async Task<PaginatedResponse<MerchandisingAuditDto>> ListAuditsAsync(
        Guid? outletId, Guid? fieldRepId, AuditKind? kind, DateTime? from, DateTime? to,
        PaginationParams pagination)
    {
        var query = db.MerchandisingAudits.ForTenant(tenant)
            .Include(a => a.Outlet)
            .WhereIf(outletId.HasValue, a => a.OutletId == outletId)
            .WhereIf(fieldRepId.HasValue, a => a.FieldRepId == fieldRepId)
            .WhereIf(kind.HasValue, a => a.Kind == kind)
            .WhereIf(from.HasValue, a => a.AuditedAt >= from)
            .WhereIf(to.HasValue, a => a.AuditedAt <= to);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(a => a.AuditedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var dtos = rows.Select(r =>
        {
            var dto = r.ToDto();
            dto.Lines = [];
            return dto;
        });

        return PaginatedResponse<MerchandisingAuditDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<CompetitorObservationDto> RecordCompetitorAsync(CompetitorObservationDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.CompetitorName))
            throw new InvalidOperationException("A competitor observation needs a brand name.");

        var entity = new CompetitorObservation
        {
            OutletId = request.OutletId,
            VisitId = request.VisitId,
            FieldRepId = request.FieldRepId,
            CompetitorName = request.CompetitorName.Trim(),
            ProductName = request.ProductName,
            PackSize = request.PackSize,
            ObservedPrice = request.ObservedPrice,
            ObservedMrp = request.ObservedMrp,
            SchemeDescription = request.SchemeDescription,
            VisibleStock = request.VisibleStock,
            Facings = request.Facings,
            HasDisplay = request.HasDisplay,
            HasPosm = request.HasPosm,
            PhotoUrl = request.PhotoUrl,
            ObservedAt = request.ObservedAt == default ? DateTime.UtcNow : request.ObservedAt,
            Note = request.Note,
        }.StampNew(tenant, userId);

        db.CompetitorObservations.Add(entity);
        await db.SaveChangesAsync();

        request.Id = entity.Id;
        request.ObservedAt = entity.ObservedAt;
        return request;
    }

    public async Task<PaginatedResponse<CompetitorObservationDto>> ListCompetitorObservationsAsync(
        Guid? outletId, string? competitorName, DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.CompetitorObservations.ForTenant(tenant)
            .WhereIf(outletId.HasValue, c => c.OutletId == outletId)
            .WhereIf(from.HasValue, c => c.ObservedAt >= from)
            .WhereIf(to.HasValue, c => c.ObservedAt <= to);

        if (!string.IsNullOrWhiteSpace(competitorName))
        {
            var term = $"%{competitorName.Trim()}%";
            query = query.Where(c => EF.Functions.ILike(c.CompetitorName, term));
        }

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(c => c.ObservedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var outletIds = rows.Select(r => r.OutletId).Distinct().ToList();
        var names = await db.Outlets.ForTenant(tenant)
            .Where(o => outletIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.Name);

        var dtos = rows.Select(r => new CompetitorObservationDto
        {
            Id = r.Id,
            OutletId = r.OutletId,
            OutletName = names.GetValueOrDefault(r.OutletId),
            VisitId = r.VisitId,
            FieldRepId = r.FieldRepId,
            CompetitorName = r.CompetitorName,
            ProductName = r.ProductName,
            PackSize = r.PackSize,
            ObservedPrice = r.ObservedPrice,
            ObservedMrp = r.ObservedMrp,
            SchemeDescription = r.SchemeDescription,
            VisibleStock = r.VisibleStock,
            Facings = r.Facings,
            HasDisplay = r.HasDisplay,
            HasPosm = r.HasPosm,
            PhotoUrl = r.PhotoUrl,
            ObservedAt = r.ObservedAt,
            Note = r.Note,
        });

        return PaginatedResponse<CompetitorObservationDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PosmPlacementDto> RecordPosmAsync(PosmPlacementDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.MaterialName))
            throw new InvalidOperationException("Point-of-sale material needs a name.");

        var entity = new PosmPlacement
        {
            OutletId = request.OutletId,
            VisitId = request.VisitId,
            FieldRepId = request.FieldRepId,
            ItemId = request.ItemId,
            MaterialName = request.MaterialName.Trim(),
            Quantity = request.Quantity <= 0 ? 1 : request.Quantity,
            PlacedOn = request.PlacedOn == default ? DateTime.UtcNow : request.PlacedOn,
            ExpiresOn = request.ExpiresOn,
            SchemeId = request.SchemeId,
            PhotoUrl = request.PhotoUrl,
            Position = request.Position,
            IsVerified = !string.IsNullOrWhiteSpace(request.PhotoUrl),
            LastVerifiedAt = string.IsNullOrWhiteSpace(request.PhotoUrl) ? null : DateTime.UtcNow,
            Note = request.Note,
        }.StampNew(tenant, userId);

        db.PosmPlacements.Add(entity);
        await db.SaveChangesAsync();

        request.Id = entity.Id;
        request.IsVerified = entity.IsVerified;
        return request;
    }

    public async Task<List<PosmPlacementDto>> ListPosmAsync(Guid? outletId, Guid? schemeId, bool? activeOnly)
    {
        var today = DateTime.UtcNow.Date;
        var rows = await db.PosmPlacements.ForTenant(tenant)
            .WhereIf(outletId.HasValue, p => p.OutletId == outletId)
            .WhereIf(schemeId.HasValue, p => p.SchemeId == schemeId)
            .WhereIf(activeOnly == true, p => p.RemovedOn == null && (p.ExpiresOn == null || p.ExpiresOn >= today))
            .OrderByDescending(p => p.PlacedOn)
            .ToListAsync();

        var outletIds = rows.Select(r => r.OutletId).Distinct().ToList();
        var names = await db.Outlets.ForTenant(tenant)
            .Where(o => outletIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.Name);

        return rows.Select(r => new PosmPlacementDto
        {
            Id = r.Id,
            OutletId = r.OutletId,
            OutletName = names.GetValueOrDefault(r.OutletId),
            VisitId = r.VisitId,
            FieldRepId = r.FieldRepId,
            ItemId = r.ItemId,
            MaterialName = r.MaterialName,
            Quantity = r.Quantity,
            PlacedOn = r.PlacedOn,
            ExpiresOn = r.ExpiresOn,
            RemovedOn = r.RemovedOn,
            SchemeId = r.SchemeId,
            PhotoUrl = r.PhotoUrl,
            Position = r.Position,
            IsVerified = r.IsVerified,
            LastVerifiedAt = r.LastVerifiedAt,
            Note = r.Note,
            IsExpired = r.ExpiresOn is not null && r.ExpiresOn.Value.Date < today,
        }).ToList();
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    private static bool IsRevenue(DistributionOrderStatus s)
        => s != DistributionOrderStatus.Cancelled
           && s != DistributionOrderStatus.Rejected
           && s != DistributionOrderStatus.Draft;

    private static bool HasValue(SurveyAnswerDto a)
        => !string.IsNullOrWhiteSpace(a.TextValue) || a.NumericValue.HasValue
           || a.BoolValue.HasValue || a.DateValue.HasValue || !string.IsNullOrWhiteSpace(a.PhotoUrl);

    /// <summary>
    /// Compares the check-in fix against the outlet's geofence. Returns the verdict and the
    /// distance, both of which are stored — the distance is what makes an exception reviewable
    /// rather than merely flagged.
    /// </summary>
    private static (GeoValidation Validation, decimal? Metres) ValidateGeo(
        RetailOutlet outlet, double? latitude, double? longitude, DistributionSettings? settings)
    {
        if (latitude is null || longitude is null) return (GeoValidation.NoFix, null);
        if (outlet.Latitude is null || outlet.Longitude is null) return (GeoValidation.NoOutletGeo, null);

        var metres = NetworkService.GeoDistanceMetres(
            latitude.Value, longitude.Value, outlet.Latitude.Value, outlet.Longitude.Value);

        var radius = outlet.GeofenceRadiusMetres > 0
            ? outlet.GeofenceRadiusMetres
            : settings?.DefaultGeofenceRadiusMetres ?? 150;

        return (metres <= radius ? GeoValidation.InsideFence : GeoValidation.OutsideFence, (decimal)metres);
    }

    private static bool ComputePriceCompliance(MerchandisingAuditLineDto line)
    {
        if (line.ObservedPrice is null || line.MandatedPrice is null || line.MandatedPrice == 0) return true;
        // Two percent tolerance: shelf prices are rounded, and flagging every rounding is noise.
        var deviation = Math.Abs(line.ObservedPrice.Value - line.MandatedPrice.Value) / line.MandatedPrice.Value;
        return deviation <= 0.02m;
    }

    /// <summary>
    /// Scores an audit. The weights are the conventional perfect-store split; they are computed
    /// and stored rather than recomputed on read, so a later change to the weighting does not
    /// silently rewrite the score a bonus was already paid on.
    /// </summary>
    private static void ScoreAudit(MerchandisingAudit audit)
    {
        var lines = audit.Lines.ToList();
        if (lines.Count == 0) return;

        var expected = lines.Where(l => l.IsExpectedInAssortment).ToList();
        var present = expected.Count(l => l.IsPresent);

        audit.OnShelfAvailabilityPercent = DistributionMapper.Percent(present, expected.Count);
        audit.AvailabilityScore = audit.OnShelfAvailabilityPercent;

        var ourFacings = lines.Sum(l => l.Facings);
        var expectedFacings = lines.Sum(l => l.ExpectedFacings);
        audit.PlanogramScore = expectedFacings == 0
            ? 100
            : Math.Min(100, DistributionMapper.Percent(ourFacings, expectedFacings));

        // Share of shelf uses expected facings as the denominator proxy when no competitor count
        // was captured; it is honest about being an approximation rather than inventing one.
        audit.ShareOfShelfPercent = expectedFacings == 0 ? 0 : audit.PlanogramScore;

        audit.VisibilityScore = DistributionMapper.Percent(lines.Count(l => l.IsCorrectPosition), lines.Count);

        var priced = lines.Where(l => l.ObservedPrice is not null && l.MandatedPrice is not null).ToList();
        audit.PricingScore = priced.Count == 0
            ? 100
            : DistributionMapper.Percent(priced.Count(l => l.IsPriceCompliant), priced.Count);

        audit.PosmScore = string.IsNullOrWhiteSpace(audit.AfterPhotoUrl) ? 0 : 100;

        audit.Score = Math.Round(
            audit.AvailabilityScore * 0.35m +
            audit.PlanogramScore * 0.25m +
            audit.VisibilityScore * 0.15m +
            audit.PricingScore * 0.15m +
            audit.PosmScore * 0.10m, 2);
    }

    private static VisitCardDto BuildCard(
        RetailOutlet outlet, OutletVisit? visit, int sequence, bool mustVisit, bool planned,
        Dictionary<Guid, CreditProfile> credit, Dictionary<Guid, int> tasks,
        Dictionary<Guid, int> assets, Dictionary<Guid, string> pinned)
    {
        credit.TryGetValue(outlet.Id, out var profile);

        return new VisitCardDto
        {
            VisitId = visit?.Id,
            OutletId = outlet.Id,
            OutletName = outlet.Name,
            OutletCode = outlet.Code,
            Channel = outlet.Channel,
            Grade = outlet.Grade,
            OutletStatus = outlet.Status,
            AddressLine = outlet.AddressLine,
            Landmark = outlet.Landmark,
            OwnerName = outlet.OwnerName,
            OwnerPhone = outlet.OwnerPhone,
            Latitude = outlet.Latitude,
            Longitude = outlet.Longitude,
            GeofenceRadiusMetres = outlet.GeofenceRadiusMetres,
            StopSequence = sequence,
            IsMustVisit = mustVisit,
            IsPlanned = planned,
            Status = visit?.Status ?? VisitStatus.Pending,
            CheckedInAt = visit?.CheckedInAt,
            CheckedOutAt = visit?.CheckedOutAt,
            DurationMinutes = visit?.DurationMinutes,
            IsProductive = visit?.IsProductive ?? false,
            OrderValue = visit?.OrderValue ?? 0,
            CollectedAmount = visit?.CollectedAmount ?? 0,
            OutstandingAmount = profile?.OutstandingAmount ?? outlet.OutstandingAmount,
            OverdueAmount = profile?.OverdueAmount ?? 0,
            IsCreditBlocked = profile?.IsBlocked ?? outlet.Status == OutletStatus.CreditBlocked,
            LastVisitAt = outlet.LastVisitAt,
            LastOrderAt = outlet.LastOrderAt,
            AverageMonthlyOfftake = outlet.AverageMonthlyOfftake,
            OpenTaskCount = tasks.GetValueOrDefault(outlet.Id),
            AssetCount = assets.GetValueOrDefault(outlet.Id),
            HasPinnedNote = pinned.ContainsKey(outlet.Id),
            PinnedNote = pinned.GetValueOrDefault(outlet.Id),
        };
    }

    private async Task<List<FocusItemDto>> BuildFocusItemsAsync(DateTime onDate)
    {
        // Focus SKUs are the ones carrying a live scheme with an explicit priority — trade
        // marketing signals what it wants pushed by prioritising the scheme, not by a second list
        // nobody remembers to maintain.
        var schemes = await db.Schemes.ForCompany(tenant)
            .Include(s => s.Products.Where(p => !p.IsDeleted))
            .Where(s => s.Status == SchemeStatus.Active && s.ValidFrom <= onDate && s.ValidTo >= onDate
                        && s.Priority > 0)
            .OrderByDescending(s => s.Priority)
            .Take(10)
            .ToListAsync();

        var items = new List<FocusItemDto>();

        foreach (var scheme in schemes)
            foreach (var product in scheme.Products.Where(p => p.ItemId.HasValue && p.IsQualifying && !p.IsExcluded))
                if (items.All(i => i.ItemId != product.ItemId))
                    items.Add(new FocusItemDto
                    {
                        ItemId = product.ItemId!.Value,
                        ItemName = product.ItemName ?? "Focus item",
                        TargetQuantityPerOutlet = product.RequiredQuantity > 0 ? product.RequiredQuantity : scheme.MinQuantity,
                        SchemeId = scheme.Id,
                        SchemeName = scheme.Name,
                    });

        return items;
    }

    private async Task<VanStockSummaryDto> BuildVanSummaryAsync(Guid vanUnitId)
    {
        var balances = await db.VanStockBalances.ForTenant(tenant)
            .Where(b => b.VanUnitId == vanUnitId && b.Quantity > 0)
            .ToListAsync();

        var name = await db.VanUnits.ForTenant(tenant)
            .Where(v => v.Id == vanUnitId).Select(v => v.Name).FirstOrDefaultAsync();

        var horizon = DateTime.UtcNow.Date.AddDays(30);

        return new VanStockSummaryDto
        {
            VanUnitId = vanUnitId,
            VanUnitName = name,
            SellableValue = balances.Where(b => b.Compartment == VanCompartment.Sellable).Sum(b => b.Quantity * b.UnitCost),
            ReturnValue = balances.Where(b => b.Compartment == VanCompartment.SaleableReturn).Sum(b => b.Quantity * b.UnitCost),
            DamagedValue = balances.Where(b => b.Compartment == VanCompartment.Damaged).Sum(b => b.Quantity * b.UnitCost),
            ExpiredValue = balances.Where(b => b.Compartment == VanCompartment.Expired).Sum(b => b.Quantity * b.UnitCost),
            TotalValue = balances.Sum(b => b.Quantity * b.UnitCost),
            LineCount = balances.Count,
            NearExpiryLineCount = balances.Count(b => b.ExpiryDate is not null && b.ExpiryDate <= horizon),
            Balances = balances.Select(b => b.ToDto()).ToList(),
        };
    }

    private async Task RecomputeDayCountersAsync(FieldDay day)
    {
        var stats = await db.Visits.ForTenant(tenant)
            .Where(v => v.FieldDayId == day.Id)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Actual = g.Count(),
                Productive = g.Count(x => x.IsProductive),
                Unplanned = g.Count(x => !x.IsPlanned),
                OrderValue = g.Sum(x => x.OrderValue),
                Collected = g.Sum(x => x.CollectedAmount),
                Returns = g.Sum(x => x.ReturnValue),
                Lines = g.Sum(x => x.LinesSold),
            })
            .FirstOrDefaultAsync();

        if (stats is null) return;

        day.ActualCalls = stats.Actual;
        day.ProductiveCalls = stats.Productive;
        day.UnplannedCalls = stats.Unplanned;
        day.OrderValue = stats.OrderValue;
        day.CollectedAmount = stats.Collected;
        day.ReturnValue = stats.Returns;
        day.DistinctLinesSold = stats.Lines;
    }

    private static SurveyResponseDto MapResponse(SurveyResponse response, string? formName) => new()
    {
        Id = response.Id,
        SurveyFormId = response.SurveyFormId,
        SurveyName = formName,
        OutletId = response.OutletId,
        VisitId = response.VisitId,
        FieldRepId = response.FieldRepId,
        SubmittedAt = response.SubmittedAt,
        Answers = response.Answers.Select(a => new SurveyAnswerResultDto
        {
            QuestionId = a.QuestionId,
            QuestionText = a.QuestionText,
            Kind = a.Kind,
            TextValue = a.TextValue,
            NumericValue = a.NumericValue,
            BoolValue = a.BoolValue,
            DateValue = a.DateValue,
            PhotoUrl = a.PhotoUrl,
        }).ToList(),
    };
}
