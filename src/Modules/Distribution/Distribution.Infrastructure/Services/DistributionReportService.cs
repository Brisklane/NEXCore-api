using Distribution.Application.DTOs;
using Distribution.Application.Services.Interfaces;
using Distribution.Domain.Entities;
using Distribution.Domain.Enums;
using Distribution.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Distribution.Infrastructure.Services;

/// <summary>
/// Dashboards, reports and the exception queue.
///
/// The exception dashboard is the piece that earns its place. A distribution manager does not
/// want eleven screens to remember to open; they want one queue of things that need a human
/// today, in one shape, ranked by how much it matters. Everything else here is a conventional
/// report over the same filter.
/// </summary>
public class DistributionReportService(DistributionDbContext db, IDistributionTenant tenant)
    : IDistributionReportService
{
    public async Task<DistributionDashboardDto> GetDashboardAsync(Guid? territoryId, DateTime? asOf)
    {
        var now = asOf ?? DateTime.UtcNow;
        var today = now.Date;
        var tomorrow = today.AddDays(1);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthStart = monthStart.AddMonths(-1);
        var lastYearStart = monthStart.AddYears(-1);

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        var dto = new DistributionDashboardDto
        {
            AsOf = now,
            CurrencyCode = settings?.BaseCurrencyCode ?? "USD",
        };

        var orders = db.Orders.ForTenant(tenant)
            .Where(o => IsRevenue(o.Status))
            .WhereIf(territoryId.HasValue, o => o.TerritoryId == territoryId);

        dto.TodaySales = await orders.Where(o => o.OrderDate >= today && o.OrderDate < tomorrow)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
        dto.TodayOrders = await orders.CountAsync(o => o.OrderDate >= today && o.OrderDate < tomorrow);

        dto.MonthToDateSales = await orders.Where(o => o.OrderDate >= monthStart)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
        dto.LastMonthSales = await orders.Where(o => o.OrderDate >= lastMonthStart && o.OrderDate < monthStart)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
        dto.SameMonthLastYearSales = await orders
            .Where(o => o.OrderDate >= lastYearStart && o.OrderDate < lastYearStart.AddMonths(1))
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        dto.GrowthPercent = dto.SameMonthLastYearSales == 0
            ? 0
            : Math.Round((dto.MonthToDateSales - dto.SameMonthLastYearSales) / dto.SameMonthLastYearSales * 100, 2);

        dto.MonthTarget = await db.Targets.ForTenant(tenant)
            .Where(t => t.Metric == TargetMetric.SalesValue && t.PeriodStart == monthStart && t.IsPublished)
            .WhereIf(territoryId.HasValue, t => t.TerritoryId == territoryId)
            .SumAsync(t => (decimal?)t.TargetValue) ?? 0;

        dto.MonthAchievementPercent = DistributionMapper.Percent(dto.MonthToDateSales, dto.MonthTarget);

        var elapsed = Math.Max(1, (today - monthStart).Days + 1);
        var totalDays = DateTime.DaysInMonth(now.Year, now.Month);
        dto.MonthProjectedSales = Math.Round(dto.MonthToDateSales / elapsed * totalDays, 2);

        dto.TodayCollections = await db.Collections.ForTenant(tenant)
            .Where(c => !c.IsReversed && c.CollectedAt >= today && c.CollectedAt < tomorrow)
            .SumAsync(c => (decimal?)c.Amount) ?? 0;

        var visitStats = await db.Visits.ForTenant(tenant)
            .Where(v => v.CheckedInAt >= today && v.CheckedInAt < tomorrow)
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Count(), Productive = g.Count(x => x.IsProductive) })
            .FirstOrDefaultAsync();

        dto.TodayVisits = visitStats?.Total ?? 0;
        dto.TodayProductiveVisits = visitStats?.Productive ?? 0;
        dto.TodayStrikeRatePercent = DistributionMapper.Percent(dto.TodayProductiveVisits, dto.TodayVisits);

        dto.ActiveFieldReps = await db.FieldReps.ForTenant(tenant)
            .CountAsync(r => r.IsActive && (territoryId == null || r.TerritoryId == territoryId));

        dto.RepsStartedToday = await db.FieldDays.ForTenant(tenant)
            .CountAsync(d => d.WorkDate == today && d.Status != FieldDayStatus.NotStarted);

        dto.RepsNotStarted = Math.Max(0, dto.ActiveFieldReps - dto.RepsStartedToday);

        var coverage = await db.FieldDays.ForTenant(tenant)
            .Where(d => d.WorkDate >= monthStart)
            .GroupBy(_ => 1)
            .Select(g => new { Planned = g.Sum(x => x.PlannedCalls), Actual = g.Sum(x => x.ActualCalls) })
            .FirstOrDefaultAsync();

        dto.CoveragePercent = coverage is null ? 0 : DistributionMapper.Percent(coverage.Actual, coverage.Planned);

        dto.TotalOutlets = await db.Outlets.ForTenant(tenant)
            .CountAsync(o => o.Status == OutletStatus.Active
                             && (territoryId == null || o.TerritoryId == territoryId));

        dto.NewOutletsThisMonth = await db.Outlets.ForTenant(tenant)
            .CountAsync(o => o.OnboardedAt >= monthStart);

        dto.ActiveOutlets = await orders.Where(o => o.OrderDate >= monthStart && o.OutletId != null)
            .Select(o => o.OutletId).Distinct().CountAsync();

        var open = await db.Orders.ForTenant(tenant)
            .Where(o => o.Status >= DistributionOrderStatus.Approved && o.Status < DistributionOrderStatus.Delivered)
            .WhereIf(territoryId.HasValue, o => o.TerritoryId == territoryId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Value = g.Sum(x => x.TotalAmount),
                AwaitingDispatch = g.Count(x => x.Status < DistributionOrderStatus.Dispatched),
            })
            .FirstOrDefaultAsync();

        dto.OpenOrders = open?.Count ?? 0;
        dto.OpenOrderValue = open?.Value ?? 0;
        dto.OrdersAwaitingDispatch = open?.AwaitingDispatch ?? 0;

        dto.TripsInProgress = await db.Trips.ForTenant(tenant)
            .CountAsync(t => t.TripDate == today && t.Status >= TripStatus.Departed && t.Status < TripStatus.Returned);

        var delivered = await db.Orders.ForTenant(tenant)
            .Where(o => o.DeliveredAt >= monthStart)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Fill = g.Average(x => x.FillRatePercent),
                OnTime = g.Count(x => x.PromisedDeliveryDate == null || x.DeliveredAt <= x.PromisedDeliveryDate),
                Total = g.Count(),
            })
            .FirstOrDefaultAsync();

        dto.FillRatePercent = Math.Round(delivered?.Fill ?? 0, 2);
        dto.OnTimeDeliveryPercent = delivered is null
            ? 0 : DistributionMapper.Percent(delivered.OnTime, delivered.Total);

        var credit = await db.CreditProfiles.ForTenant(tenant)
            .GroupBy(_ => 1)
            .Select(g => new { Outstanding = g.Sum(x => x.OutstandingAmount), Overdue = g.Sum(x => x.OverdueAmount) })
            .FirstOrDefaultAsync();

        dto.Receivables = credit?.Outstanding ?? 0;
        dto.Overdue = credit?.Overdue ?? 0;

        var monthCollections = await db.Collections.ForTenant(tenant)
            .Where(c => !c.IsReversed && c.CollectedAt >= monthStart)
            .SumAsync(c => (decimal?)c.Amount) ?? 0;

        dto.CollectionEfficiencyPercent = DistributionMapper.Percent(monthCollections, dto.MonthToDateSales);

        dto.UndepositedCash = await db.Collections.ForTenant(tenant)
            .Where(c => !c.IsDeposited && !c.IsReversed && c.Tender == PaymentTender.Cash)
            .SumAsync(c => (decimal?)c.Amount) ?? 0;

        dto.UnsettledRoutes = await CountUnsettledAsync(today);

        var schemes = await db.Schemes.ForCompany(tenant)
            .Where(s => s.Status == SchemeStatus.Active || s.Status == SchemeStatus.Exhausted)
            .GroupBy(_ => 1)
            .Select(g => new { Consumed = g.Sum(x => x.ConsumedAmount), Budget = g.Sum(x => x.BudgetAmount) })
            .FirstOrDefaultAsync();

        dto.SchemeSpendMonthToDate = await db.SchemeApplications.ForTenant(tenant)
            .Where(a => !a.IsReversed && a.AppliedAt >= monthStart)
            .SumAsync(a => (decimal?)a.BenefitValue) ?? 0;

        dto.SchemeBudgetRemaining = Math.Max(0, (schemes?.Budget ?? 0) - (schemes?.Consumed ?? 0));

        var slaDays = settings?.ClaimSettlementSlaDays ?? 15;

        var claims = await db.Claims.ForTenant(tenant)
            .Where(c => c.Status != ClaimStatus.Settled && c.Status != ClaimStatus.Rejected
                        && c.Status != ClaimStatus.Cancelled && c.Status != ClaimStatus.Draft)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Value = g.Sum(x => x.ClaimedAmount),
                Breaching = g.Count(x => x.AgeingDays > slaDays),
            })
            .FirstOrDefaultAsync();

        dto.OpenClaims = claims?.Count ?? 0;
        dto.OpenClaimValue = claims?.Value ?? 0;
        dto.ClaimsBreachingSla = claims?.Breaching ?? 0;

        var channelStock = await db.StockDeclarations.ForTenant(tenant)
            .Where(d => d.AsOfDate >= monthStart.AddMonths(-1))
            .GroupBy(_ => 1)
            .Select(g => new { Stock = g.Sum(x => x.TotalValue), NearExpiry = g.Sum(x => x.NearExpiryValue) })
            .FirstOrDefaultAsync();

        dto.ChannelStockValue = channelStock?.Stock ?? 0;
        dto.NearExpiryValue = channelStock?.NearExpiry ?? 0;

        var secondary = await db.SecondarySales.ForTenant(tenant)
            .Where(s => s.SaleDate >= monthStart && s.IsMapped)
            .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

        dto.SellThroughPercent = DistributionMapper.Percent(secondary, dto.MonthToDateSales);

        dto.SalesTrend = await BuildTrendAsync(monthStart.AddMonths(-2), today, "Day", territoryId);
        dto.TopTerritories = await TopByAsync("Territory", monthStart, tomorrow, territoryId, 5);
        dto.TopReps = await TopByAsync("FieldRep", monthStart, tomorrow, territoryId, 5);
        dto.TopItems = await TopByAsync("Item", monthStart, tomorrow, territoryId, 5);

        var exceptions = await GetExceptionsAsync(territoryId);
        dto.Exceptions = exceptions.Rows.Take(10).ToList();

        return dto;
    }

    public async Task<ExceptionDashboardDto> GetExceptionsAsync(Guid? territoryId)
    {
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        var today = DateTime.UtcNow.Date;

        var dto = new ExceptionDashboardDto { AsOf = DateTime.UtcNow };
        var rows = new List<ExceptionRowDto>();

        // Unsettled routes — money sitting in a market overnight.
        var unsettled = await db.FieldDays.ForTenant(tenant)
            .Include(d => d.FieldRep)
            .Where(d => d.WorkDate >= today.AddDays(-7)
                        && (d.Status == FieldDayStatus.Closed || d.Status == FieldDayStatus.ForceClosed)
                        && !db.Settlements.ForTenant(tenant).Any(s => s.FieldDayId == d.Id
                            && s.Status == SettlementStatus.Closed))
            .Take(50).ToListAsync();

        dto.UnsettledRoutes = unsettled.Count;
        rows.AddRange(unsettled.Select(d => new ExceptionRowDto
        {
            Kind = "UnsettledRoute",
            Severity = AlertSeverity.Critical,
            Title = $"{d.FieldRep?.FullName ?? "Route"} — {d.WorkDate:d} not settled",
            Detail = $"{d.CollectedAmount:N2} collected, {d.CashDeclared:N2} declared.",
            Amount = d.CollectedAmount,
            OccurredAt = d.ClosedAt,
            AgeingDays = (int)(today - d.WorkDate.Date).TotalDays,
            ReferenceId = d.Id,
            ReferenceType = "FieldDay",
            ActionRoute = "/distribution/settlement",
        }));

        // Unexplained variances — the gate that keeps settlement honest.
        var variances = await db.SettlementVariances.ForTenant(tenant)
            .Include(v => v.Settlement)
            .Where(v => v.ReasonCodeId == null)
            .Take(50).ToListAsync();

        dto.UnexplainedVariances = variances.Count;
        rows.AddRange(variances.Select(v => new ExceptionRowDto
        {
            Kind = "UnexplainedVariance",
            Severity = AlertSeverity.Critical,
            Title = $"{v.Kind} of {Math.Abs(v.VarianceAmount):N2} unexplained",
            Detail = v.ItemName ?? v.Settlement?.SettlementNumber,
            Amount = Math.Abs(v.VarianceAmount),
            ReferenceId = v.SettlementId,
            ReferenceType = "RouteSettlement",
            ActionRoute = "/distribution/settlement",
        }));

        var breaches = await db.CreditProfiles.ForTenant(tenant)
            .Include(c => c.Outlet).Include(c => c.Partner)
            .Where(c => c.OverdueAmount > 0 || c.IsBlocked)
            .OrderByDescending(c => c.OverdueAmount)
            .Take(50).ToListAsync();

        dto.CreditBreaches = breaches.Count;
        rows.AddRange(breaches.Select(c => new ExceptionRowDto
        {
            Kind = "CreditBreach",
            Severity = c.IsBlocked ? AlertSeverity.Critical : AlertSeverity.Warning,
            Title = $"{c.Outlet?.Name ?? c.Partner?.Name ?? "Account"} — {c.OverdueAmount:N2} overdue",
            Detail = c.IsBlocked ? c.BlockReason : $"Oldest invoice {c.OldestInvoiceDays} days.",
            Amount = c.OverdueAmount,
            AgeingDays = c.OldestInvoiceDays,
            ReferenceId = c.OutletId ?? c.PartnerId,
            ReferenceType = c.OutletId.HasValue ? "Outlet" : "Partner",
            ActionRoute = "/distribution/credit",
        }));

        var criticalDays = settings?.NearExpiryCriticalDays ?? 30;
        var expiryHorizon = today.AddDays(criticalDays);

        var nearExpiry = await db.VanStockBalances.ForTenant(tenant)
            .Include(b => b.VanUnit)
            .Where(b => b.Quantity > 0 && b.ExpiryDate != null && b.ExpiryDate <= expiryHorizon)
            .Take(50).ToListAsync();

        dto.NearExpiryLines = nearExpiry.Count;
        rows.AddRange(nearExpiry.Select(b => new ExceptionRowDto
        {
            Kind = "NearExpiry",
            Severity = b.ExpiryDate!.Value.Date <= today ? AlertSeverity.Critical : AlertSeverity.Warning,
            Title = $"{b.ItemName} expires {b.ExpiryDate:d}",
            Detail = $"{b.Quantity:N0} on {b.VanUnit?.Name ?? "a van"}.",
            Amount = b.Quantity * b.UnitCost,
            AgeingDays = (int)(b.ExpiryDate.Value.Date - today).TotalDays,
            ReferenceId = b.VanUnitId,
            ReferenceType = "VanUnit",
            ActionRoute = "/distribution/traceability",
        }));

        dto.UnmappedSecondaryRows = await db.SecondarySaleLines.ForTenant(tenant).CountAsync(l => !l.IsMapped);
        if (dto.UnmappedSecondaryRows > 0)
            rows.Add(new ExceptionRowDto
            {
                Kind = "UnmappedSecondary",
                Severity = AlertSeverity.Warning,
                Title = $"{dto.UnmappedSecondaryRows} secondary sales row(s) could not be mapped",
                Detail = "Resolve the exceptions so the channel picture is complete.",
                ActionRoute = "/distribution/secondary",
            });

        var slaDays = settings?.ClaimSettlementSlaDays ?? 15;
        var overdueClaims = await db.Claims.ForTenant(tenant)
            .Include(c => c.Partner)
            .Where(c => c.AgeingDays > slaDays && c.Status != ClaimStatus.Settled
                        && c.Status != ClaimStatus.Rejected && c.Status != ClaimStatus.Cancelled)
            .OrderByDescending(c => c.AgeingDays)
            .Take(50).ToListAsync();

        dto.OverdueClaims = overdueClaims.Count;
        rows.AddRange(overdueClaims.Select(c => new ExceptionRowDto
        {
            Kind = "OverdueClaim",
            Severity = AlertSeverity.Warning,
            Title = $"{c.ClaimNumber} — {c.AgeingDays} days old",
            Detail = $"{c.Partner?.Name}: {c.ClaimedAmount:N2} claimed.",
            Amount = c.ClaimedAmount,
            AgeingDays = c.AgeingDays,
            ReferenceId = c.Id,
            ReferenceType = "ChannelClaim",
            ActionRoute = "/distribution/claims",
        }));

        var licenceHorizon = today.AddDays(30);

        var expiringDocs = await db.PartnerDocuments.ForTenant(tenant)
            .Include(d => d.Partner)
            .Where(d => d.ExpiresOn != null && d.ExpiresOn <= licenceHorizon)
            .Take(30).ToListAsync();

        var expiringVehicles = await db.VehicleCompliances.ForTenant(tenant)
            .Include(c => c.Vehicle)
            .Where(c => !c.IsSuperseded && c.ExpiresOn <= licenceHorizon)
            .Take(30).ToListAsync();

        dto.ExpiringLicences = expiringDocs.Count + expiringVehicles.Count;

        rows.AddRange(expiringDocs.Select(d => new ExceptionRowDto
        {
            Kind = "ExpiringLicence",
            Severity = d.ExpiresOn!.Value.Date < today ? AlertSeverity.Critical : AlertSeverity.Warning,
            Title = $"{d.Partner?.Name}: {d.DocumentType} expires {d.ExpiresOn:d}",
            AgeingDays = (int)(d.ExpiresOn.Value.Date - today).TotalDays,
            ReferenceId = d.PartnerId,
            ReferenceType = "Partner",
            ActionRoute = "/distribution/partners",
        }));

        rows.AddRange(expiringVehicles.Select(c => new ExceptionRowDto
        {
            Kind = "ExpiringLicence",
            Severity = c.ExpiresOn.Date < today ? AlertSeverity.Critical : AlertSeverity.Warning,
            Title = $"{c.Vehicle?.RegistrationNumber}: {c.Kind} expires {c.ExpiresOn:d}",
            AgeingDays = (int)(c.ExpiresOn.Date - today).TotalDays,
            ReferenceId = c.VehicleId,
            ReferenceType = "Vehicle",
            ActionRoute = "/distribution/fleet",
        }));

        dto.OutOfFenceVisits = await db.Visits.ForTenant(tenant)
            .CountAsync(v => v.GeoValidation == GeoValidation.OutsideFence && v.CheckedInAt >= today.AddDays(-7));

        if (dto.OutOfFenceVisits > 0)
            rows.Add(new ExceptionRowDto
            {
                Kind = "OutOfFenceVisit",
                Severity = AlertSeverity.Info,
                Title = $"{dto.OutOfFenceVisits} check-in(s) outside the outlet geofence this week",
                ActionRoute = "/distribution/journey",
            });

        var failed = await db.ProofsOfDelivery.ForTenant(tenant)
            .Where(p => !p.IsClean && !p.IsExceptionResolved)
            .OrderByDescending(p => p.DeliveredAt)
            .Take(30).ToListAsync();

        dto.FailedDeliveries = failed.Count;
        rows.AddRange(failed.Select(p => new ExceptionRowDto
        {
            Kind = "DeliveryException",
            Severity = AlertSeverity.Warning,
            Title = $"{p.PodNumber} — {p.ShortValue + p.DamagedValue + p.RejectedValue:N2} not accepted",
            OccurredAt = p.DeliveredAt,
            Amount = p.ShortValue + p.DamagedValue + p.RejectedValue,
            ReferenceId = p.Id,
            ReferenceType = "ProofOfDelivery",
            ActionRoute = "/distribution/pod",
        }));

        var excursions = await db.ColdChainLogs.ForTenant(tenant)
            .Include(l => l.Checkpoint)
            .Where(l => l.IsOutOfRange && !l.IsResolved)
            .Take(30).ToListAsync();

        dto.ColdChainExcursions = excursions.Count;
        rows.AddRange(excursions.Select(l => new ExceptionRowDto
        {
            Kind = "ColdChainExcursion",
            Severity = AlertSeverity.Critical,
            Title = $"{l.Checkpoint?.Name} at {l.ReadingCelsius:N1}°C",
            Detail = "No corrective action recorded.",
            OccurredAt = l.RecordedAt,
            ReferenceId = l.CheckpointId,
            ReferenceType = "ColdChainCheckpoint",
            ActionRoute = "/distribution/traceability",
        }));

        dto.BouncedCheques = await db.Cheques.ForTenant(tenant)
            .CountAsync(c => c.Status == ChequeStatus.Bounced && c.BouncedOn >= today.AddDays(-30));

        var staleBefore = DateTime.UtcNow.AddHours(-24);
        dto.StaleDevices = await db.FieldDevices.ForTenant(tenant)
            .CountAsync(d => d.PendingOutboxCount > 0 || d.LastSyncAt == null || d.LastSyncAt < staleBefore);

        if (dto.StaleDevices > 0)
            rows.Add(new ExceptionRowDto
            {
                Kind = "StaleDevice",
                Severity = AlertSeverity.Warning,
                Title = $"{dto.StaleDevices} device(s) have not synced in 24 hours",
                Detail = "Unsynced transactions are missing from every number on this screen.",
                ActionRoute = "/distribution/team",
            });

        dto.Rows = rows
            .OrderByDescending(r => r.Severity)
            .ThenByDescending(r => r.Amount ?? 0)
            .ToList();

        dto.TotalCount = dto.Rows.Count;
        dto.CriticalCount = dto.Rows.Count(r => r.Severity == AlertSeverity.Critical);
        dto.WarningCount = dto.Rows.Count(r => r.Severity == AlertSeverity.Warning);

        return dto;
    }

    // ═══ Reports ═════════════════════════════════════════════════════════════

    public async Task<SalesReportDto> GetSalesReportAsync(DistributionReportFilter filter, string groupBy)
    {
        var (from, to) = Window(filter);
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        var orders = ApplyFilter(db.Orders.ForTenant(tenant).Where(o => IsRevenue(o.Status)), filter, from, to);

        var totals = await orders
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Value = g.Sum(x => x.TotalAmount),
                Count = g.Count(),
                Margin = g.Sum(x => x.MarginAmount),
                Quantity = g.Sum(x => x.TotalQuantity),
            })
            .FirstOrDefaultAsync();

        var dto = new SalesReportDto
        {
            Filter = filter,
            CurrencyCode = settings?.BaseCurrencyCode ?? "USD",
            PrimaryValue = totals?.Value ?? 0,
            OrderCount = totals?.Count ?? 0,
            MarginAmount = totals?.Margin ?? 0,
            Quantity = totals?.Quantity ?? 0,
        };

        dto.AverageOrderValue = dto.OrderCount == 0 ? 0 : Math.Round(dto.PrimaryValue / dto.OrderCount, 2);
        dto.MarginPercent = DistributionMapper.Percent(dto.MarginAmount, dto.PrimaryValue);

        dto.SecondaryValue = await db.SecondarySales.ForTenant(tenant)
            .Where(s => s.SaleDate >= from && s.SaleDate <= to && s.IsMapped)
            .WhereIf(filter.PartnerId.HasValue, s => s.PartnerId == filter.PartnerId)
            .WhereIf(filter.TerritoryId.HasValue, s => s.TerritoryId == filter.TerritoryId)
            .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

        dto.ReturnValue = await db.Returns.ForTenant(tenant)
            .Where(r => r.RequestedOn >= from && r.RequestedOn <= to)
            .WhereIf(filter.PartnerId.HasValue, r => r.PartnerId == filter.PartnerId)
            .WhereIf(filter.OutletId.HasValue, r => r.OutletId == filter.OutletId)
            .SumAsync(r => (decimal?)r.ApprovedValue) ?? 0;

        dto.NetValue = dto.PrimaryValue - dto.ReturnValue;
        dto.Trend = await BuildTrendAsync(from, to, filter.Granularity ?? "Day", filter.TerritoryId);
        dto.Rows = await TopByAsync(groupBy, from, to.AddDays(1), filter.TerritoryId, 50);

        return dto;
    }

    public async Task<ProductivityReportDto> GetProductivityReportAsync(DistributionReportFilter filter)
    {
        var (from, to) = Window(filter);

        var snapshots = await db.KpiSnapshots.ForTenant(tenant)
            .Where(k => k.SnapshotDate >= from && k.SnapshotDate <= to && k.Scope == TargetScope.FieldRep)
            .WhereIf(filter.TerritoryId.HasValue, k => k.TerritoryId == filter.TerritoryId)
            .WhereIf(filter.FieldRepId.HasValue, k => k.FieldRepId == filter.FieldRepId)
            .WhereIf(filter.RouteId.HasValue, k => k.RouteId == filter.RouteId)
            .ToListAsync();

        var repIds = snapshots.Where(s => s.FieldRepId.HasValue).Select(s => s.FieldRepId!.Value).Distinct().ToList();
        var reps = await db.FieldReps.ForTenant(tenant)
            .Where(r => repIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.FullName);

        var dto = new ProductivityReportDto { Filter = filter };

        dto.Rows = snapshots
            .GroupBy(s => s.FieldRepId)
            .Select(g => Roll(g.ToList(), reps.GetValueOrDefault(g.Key ?? Guid.Empty)))
            .OrderByDescending(k => k.SalesValue)
            .ToList();

        dto.Totals = Roll(snapshots, "All");

        dto.Heatmap = snapshots
            .GroupBy(s => new { s.SnapshotDate, s.FieldRepId })
            .Select(g => new CoverageCellDto
            {
                Date = g.Key.SnapshotDate,
                FieldRepId = g.Key.FieldRepId,
                FieldRepName = reps.GetValueOrDefault(g.Key.FieldRepId ?? Guid.Empty),
                PlannedCalls = g.Sum(x => x.PlannedCalls),
                ActualCalls = g.Sum(x => x.ActualCalls),
                ProductiveCalls = g.Sum(x => x.ProductiveCalls),
                CoveragePercent = DistributionMapper.Percent(g.Sum(x => x.ActualCalls), g.Sum(x => x.PlannedCalls)),
                SalesValue = g.Sum(x => x.SalesValue),
                IsHoliday = g.Sum(x => x.PlannedCalls) == 0,
            })
            .OrderBy(c => c.Date)
            .ToList();

        return dto;

        static KpiDto Roll(List<KpiSnapshot> rows, string? name)
        {
            var planned = rows.Sum(x => x.PlannedCalls);
            var actual = rows.Sum(x => x.ActualCalls);
            var productive = rows.Sum(x => x.ProductiveCalls);
            var bills = rows.Sum(x => x.BillCount);
            var sales = rows.Sum(x => x.SalesValue);

            return new KpiDto
            {
                FieldRepId = rows.FirstOrDefault()?.FieldRepId,
                FieldRepName = name,
                PlannedCalls = planned,
                ActualCalls = actual,
                ProductiveCalls = productive,
                MissedCalls = rows.Sum(x => x.MissedCalls),
                UnplannedCalls = rows.Sum(x => x.UnplannedCalls),
                CoveragePercent = DistributionMapper.Percent(actual, planned),
                StrikeRatePercent = DistributionMapper.Percent(productive, actual),
                BillCount = bills,
                SalesValue = sales,
                CollectionValue = rows.Sum(x => x.CollectionValue),
                NewOutletsAdded = rows.Sum(x => x.NewOutletsAdded),
                AverageBillValue = bills == 0 ? 0 : Math.Round(sales / bills, 2),
                DropSize = productive == 0 ? 0 : Math.Round(sales / productive, 2),
                LinesPerCall = productive == 0
                    ? 0 : Math.Round(rows.Sum(x => x.LinesPerCall * x.ProductiveCalls) / productive, 2),
                TimeInMarketMinutes = rows.Sum(x => x.TimeInMarketMinutes),
                DistanceCoveredKm = rows.Sum(x => x.DistanceCoveredKm),
                CollectionEfficiencyPercent = DistributionMapper.Percent(rows.Sum(x => x.CollectionValue), sales),
            };
        }
    }

    public async Task<OutletAnalyticsDto> GetOutletAnalyticsAsync(DistributionReportFilter filter)
    {
        var (from, to) = Window(filter);

        var outlets = db.Outlets.ForTenant(tenant)
            .WhereIf(filter.TerritoryId.HasValue, o => o.TerritoryId == filter.TerritoryId)
            .WhereIf(filter.PartnerId.HasValue, o => o.PartnerId == filter.PartnerId)
            .WhereIf(filter.Channel.HasValue, o => o.Channel == filter.Channel)
            .WhereIf(filter.Grade.HasValue, o => o.Grade == filter.Grade);

        var dto = new OutletAnalyticsDto
        {
            Filter = filter,
            TotalOutlets = await outlets.CountAsync(),
            ActiveOutlets = await outlets.CountAsync(o => o.Status == OutletStatus.Active),
            NewOutlets = await outlets.CountAsync(o => o.OnboardedAt >= from && o.OnboardedAt <= to),
        };

        var billed = await db.Orders.ForTenant(tenant)
            .Where(o => o.OutletId != null && o.OrderDate >= from && o.OrderDate <= to && IsRevenue(o.Status))
            .Select(o => o.OutletId!.Value).Distinct().ToListAsync();

        dto.BilledOutlets = billed.Count;
        dto.DormantOutlets = Math.Max(0, dto.ActiveOutlets - dto.BilledOutlets);

        // "Lost" is an outlet that bought in the previous window and not in this one — the number
        // that actually matters, and the one a simple active/inactive flag never shows.
        var span = (to - from).Days + 1;
        var priorFrom = from.AddDays(-span);

        var priorBuyers = await db.Orders.ForTenant(tenant)
            .Where(o => o.OutletId != null && o.OrderDate >= priorFrom && o.OrderDate < from && IsRevenue(o.Status))
            .Select(o => o.OutletId!.Value).Distinct().ToListAsync();

        dto.LostOutlets = priorBuyers.Count(id => !billed.Contains(id));
        dto.ChurnPercent = DistributionMapper.Percent(dto.LostOutlets, priorBuyers.Count);

        dto.AverageOfftake = dto.BilledOutlets == 0
            ? 0
            : Math.Round(await db.Orders.ForTenant(tenant)
                .Where(o => o.OutletId != null && billed.Contains(o.OutletId.Value)
                            && o.OrderDate >= from && o.OrderDate <= to && IsRevenue(o.Status))
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0 / dto.BilledOutlets, 2);

        dto.ByChannel = await outlets
            .GroupBy(o => o.Channel)
            .Select(g => new RankedRowDto { Name = g.Key.ToString(), Value = g.Count() })
            .ToListAsync();

        dto.ByGrade = await outlets
            .GroupBy(o => o.Grade)
            .Select(g => new RankedRowDto { Name = g.Key.ToString(), Value = g.Count() })
            .ToListAsync();

        dto.TopOutlets = await db.Orders.ForTenant(tenant)
            .Include(o => o.Outlet)
            .Where(o => o.OutletId != null && o.OrderDate >= from && o.OrderDate <= to && IsRevenue(o.Status))
            .GroupBy(o => new { o.OutletId, Name = o.Outlet!.Name })
            .Select(g => new RankedRowDto
            {
                Id = g.Key.OutletId,
                Name = g.Key.Name,
                Value = g.Sum(x => x.TotalAmount),
                Quantity = g.Count(),
            })
            .OrderByDescending(r => r.Value)
            .Take(25).ToListAsync();

        dto.DormantList = await outlets
            .Where(o => o.Status == OutletStatus.Active && !billed.Contains(o.Id))
            .OrderByDescending(o => o.LifetimeSales)
            .Take(25)
            .Select(o => new RankedRowDto
            {
                Id = o.Id,
                Name = o.Name,
                SubLabel = o.LastOrderAt == null ? "Never ordered" : $"Last ordered {o.LastOrderAt:d}",
                Value = o.LifetimeSales,
            })
            .ToListAsync();

        return dto;
    }

    public async Task<LogisticsReportDto> GetLogisticsReportAsync(DistributionReportFilter filter)
    {
        var (from, to) = Window(filter);

        var trips = await db.Trips.ForTenant(tenant)
            .Include(t => t.Vehicle)
            .Where(t => t.TripDate >= from && t.TripDate <= to)
            .WhereIf(filter.RouteId.HasValue, t => t.RouteId == filter.RouteId)
            .ToListAsync();

        var dto = new LogisticsReportDto
        {
            Filter = filter,
            TripCount = trips.Count,
            StopCount = trips.Sum(t => t.PlannedStops),
            CompletedStops = trips.Sum(t => t.CompletedStops),
            FailedStops = trips.Sum(t => t.FailedStops),
            TotalDistanceKm = trips.Sum(t => t.DistanceKm),
            TotalExpense = trips.Sum(t => t.TotalExpense),
            DeliveredValue = trips.Sum(t => t.DeliveredValue),
        };

        dto.FillRatePercent = DistributionMapper.Percent(dto.DeliveredValue, trips.Sum(t => t.PlannedValue));
        dto.OnTimePercent = trips.Count == 0 ? 0 : Math.Round(trips.Average(t => t.OnTimePercent), 2);
        dto.CostPerDrop = dto.CompletedStops == 0 ? 0 : Math.Round(dto.TotalExpense / dto.CompletedStops, 2);
        dto.CostPerKm = dto.TotalDistanceKm == 0 ? 0 : Math.Round(dto.TotalExpense / dto.TotalDistanceKm, 2);

        var cycles = await db.Orders.ForTenant(tenant)
            .Where(o => o.DeliveredAt != null && o.OrderDate >= from && o.OrderDate <= to)
            .Select(o => new { o.OrderDate, o.DeliveredAt })
            .ToListAsync();

        dto.AverageCycleTimeHours = cycles.Count == 0
            ? 0
            : Math.Round((decimal)cycles.Average(c => (c.DeliveredAt!.Value - c.OrderDate).TotalHours), 1);

        var reasonIds = await db.TripStops.ForTenant(tenant)
            .Where(s => s.FailureReasonCodeId != null
                        && trips.Select(t => t.Id).Contains(s.TripId))
            .GroupBy(s => s.FailureReasonCodeId!.Value)
            .Select(g => new { ReasonId = g.Key, Count = g.Count() })
            .ToListAsync();

        var reasons = await db.ReasonCodes.ForCompany(tenant)
            .Where(r => reasonIds.Select(x => x.ReasonId).Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name);

        dto.FailureReasons = reasonIds
            .Select(r => new RankedRowDto
            {
                Id = r.ReasonId,
                Name = reasons.GetValueOrDefault(r.ReasonId) ?? "Unknown",
                Value = r.Count,
            })
            .OrderByDescending(r => r.Value)
            .ToList();

        dto.ByVehicle = trips
            .Where(t => t.VehicleId.HasValue)
            .GroupBy(t => new { t.VehicleId, Reg = t.Vehicle?.RegistrationNumber ?? "Unknown" })
            .Select(g => new RankedRowDto
            {
                Id = g.Key.VehicleId,
                Name = g.Key.Reg,
                Value = g.Sum(x => x.DeliveredValue),
                Quantity = g.Sum(x => x.CompletedStops),
                SubLabel = $"{g.Sum(x => x.DistanceKm):N0} km",
            })
            .OrderByDescending(r => r.Value)
            .Take(25).ToList();

        dto.Trend = trips
            .GroupBy(t => t.TripDate)
            .Select(g => new TrendPointDto
            {
                Bucket = g.Key,
                Label = g.Key.ToString("dd MMM"),
                Value = g.Sum(x => x.DeliveredValue),
                Count = g.Count(),
            })
            .OrderBy(p => p.Bucket)
            .ToList();

        return dto;
    }

    public async Task<ReceivablesReportDto> GetReceivablesReportAsync(DistributionReportFilter filter)
    {
        var (from, to) = Window(filter);
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        var profiles = await db.CreditProfiles.ForTenant(tenant)
            .Include(c => c.Outlet).Include(c => c.Partner)
            .WhereIf(filter.PartnerId.HasValue, c => c.PartnerId == filter.PartnerId)
            .WhereIf(filter.TerritoryId.HasValue, c => c.Outlet != null && c.Outlet.TerritoryId == filter.TerritoryId)
            .ToListAsync();

        var dto = new ReceivablesReportDto
        {
            Filter = filter,
            CurrencyCode = settings?.BaseCurrencyCode ?? "USD",
            TotalOutstanding = profiles.Sum(p => p.OutstandingAmount),
            TotalOverdue = profiles.Sum(p => p.OverdueAmount),
            Bucket0To30 = profiles.Sum(p => p.Bucket0To30),
            Bucket31To60 = profiles.Sum(p => p.Bucket31To60),
            Bucket61To90 = profiles.Sum(p => p.Bucket61To90),
            Bucket90Plus = profiles.Sum(p => p.Bucket90Plus),
            BlockedOutletCount = profiles.Count(p => p.IsBlocked),
            ProvisionedAmount = profiles.Sum(p => p.ProvisionedAmount),
            Rows = profiles.OrderByDescending(p => p.OverdueAmount).Take(200).Select(p => p.ToDto()).ToList(),
        };

        dto.CollectedInPeriod = await db.Collections.ForTenant(tenant)
            .Where(c => !c.IsReversed && c.CollectedAt >= from && c.CollectedAt <= to)
            .SumAsync(c => (decimal?)c.Amount) ?? 0;

        dto.DueInPeriod = await db.Orders.ForTenant(tenant)
            .Where(o => o.OrderDate >= from && o.OrderDate <= to && IsRevenue(o.Status))
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        dto.CollectionEfficiencyPercent = DistributionMapper.Percent(dto.CollectedInPeriod, dto.DueInPeriod);
        dto.AverageCollectionDays = profiles.Count == 0 ? 0 : Math.Round((decimal)profiles.Average(p => p.OldestInvoiceDays), 1);

        var bounced = await db.Cheques.ForTenant(tenant)
            .Where(c => c.Status == ChequeStatus.Bounced && c.BouncedOn >= from && c.BouncedOn <= to)
            .GroupBy(_ => 1)
            .Select(g => new { Count = g.Count(), Value = g.Sum(x => x.Amount) })
            .FirstOrDefaultAsync();

        dto.BouncedChequeCount = bounced?.Count ?? 0;
        dto.BouncedChequeValue = bounced?.Value ?? 0;

        return dto;
    }

    public async Task<ReturnsReportDto> GetReturnsReportAsync(DistributionReportFilter filter)
    {
        var (from, to) = Window(filter);
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        var returns = await db.Returns.ForTenant(tenant)
            .Include(r => r.Outlet)
            .Include(r => r.Lines.Where(l => !l.IsDeleted))
            .Where(r => r.RequestedOn >= from && r.RequestedOn <= to)
            .WhereIf(filter.OutletId.HasValue, r => r.OutletId == filter.OutletId)
            .WhereIf(filter.PartnerId.HasValue, r => r.PartnerId == filter.PartnerId)
            .WhereIf(filter.RouteId.HasValue, r => r.RouteId == filter.RouteId)
            .ToListAsync();

        var dto = new ReturnsReportDto
        {
            Filter = filter,
            CurrencyCode = settings?.BaseCurrencyCode ?? "USD",
            ReturnCount = returns.Count,
            ReturnValue = returns.Sum(r => r.ApprovedValue > 0 ? r.ApprovedValue : r.ClaimedValue),
            SaleableValue = returns.Where(r => r.Kind == ReturnKind.SaleableMarketReturn).Sum(r => r.ClaimedValue),
            DamagedValue = returns.Where(r => r.Kind == ReturnKind.Damaged).Sum(r => r.ClaimedValue),
            ExpiredValue = returns.Where(r => r.Kind == ReturnKind.Expired).Sum(r => r.ClaimedValue),
        };

        dto.SalesValue = await db.Orders.ForTenant(tenant)
            .Where(o => o.OrderDate >= from && o.OrderDate <= to && IsRevenue(o.Status))
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        dto.ReturnRatePercent = DistributionMapper.Percent(dto.ReturnValue, dto.SalesValue);

        var receipts = await db.ReturnReceipts.ForTenant(tenant)
            .Where(r => r.ReceivedAt >= from && r.ReceivedAt <= to)
            .GroupBy(_ => 1)
            .Select(g => new { Restocked = g.Sum(x => x.RestockedValue), Scrapped = g.Sum(x => x.ScrappedValue) })
            .FirstOrDefaultAsync();

        dto.RestockedValue = receipts?.Restocked ?? 0;
        dto.ScrappedValue = receipts?.Scrapped ?? 0;

        dto.ByReason = returns
            .Where(r => r.ReasonCodeId.HasValue)
            .GroupBy(r => r.ReasonCodeId!.Value)
            .Select(g => new RankedRowDto { Id = g.Key, Name = g.Key.ToString(), Value = g.Sum(x => x.ClaimedValue) })
            .OrderByDescending(r => r.Value).Take(20).ToList();

        var reasonIds = dto.ByReason.Where(r => r.Id.HasValue).Select(r => r.Id!.Value).ToList();
        var reasonNames = await db.ReasonCodes.ForCompany(tenant)
            .Where(r => reasonIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.Name);

        foreach (var row in dto.ByReason.Where(r => r.Id.HasValue))
            row.Name = reasonNames.GetValueOrDefault(row.Id!.Value) ?? "Unknown";

        dto.ByItem = returns
            .SelectMany(r => r.Lines)
            .GroupBy(l => new { l.ItemId, l.ItemName })
            .Select(g => new RankedRowDto
            {
                Id = g.Key.ItemId,
                Name = g.Key.ItemName,
                Value = g.Sum(x => x.LineValue),
                Quantity = g.Sum(x => x.RequestedQuantity),
            })
            .OrderByDescending(r => r.Value).Take(25).ToList();

        dto.ByOutlet = returns
            .Where(r => r.OutletId.HasValue)
            .GroupBy(r => new { r.OutletId, Name = r.Outlet?.Name ?? "Unknown" })
            .Select(g => new RankedRowDto
            {
                Id = g.Key.OutletId,
                Name = g.Key.Name,
                Value = g.Sum(x => x.ClaimedValue),
                Quantity = g.Count(),
            })
            .OrderByDescending(r => r.Value).Take(25).ToList();

        dto.Trend = returns
            .GroupBy(r => r.RequestedOn.Date)
            .Select(g => new TrendPointDto
            {
                Bucket = g.Key,
                Label = g.Key.ToString("dd MMM"),
                Value = g.Sum(x => x.ClaimedValue),
                Count = g.Count(),
            })
            .OrderBy(p => p.Bucket).ToList();

        return dto;
    }

    public async Task<ClaimsReportDto> GetClaimsReportAsync(DistributionReportFilter filter)
    {
        var (from, to) = Window(filter);
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        var slaDays = settings?.ClaimSettlementSlaDays ?? 15;

        var claims = await db.Claims.ForTenant(tenant)
            .Include(c => c.Partner)
            .Where(c => c.SubmittedOn >= from && c.SubmittedOn <= to)
            .WhereIf(filter.PartnerId.HasValue, c => c.PartnerId == filter.PartnerId)
            .ToListAsync();

        var settled = claims.Where(c => c.Status == ClaimStatus.Settled).ToList();
        var open = claims.Where(c => c.Status is not (ClaimStatus.Settled or ClaimStatus.Rejected
            or ClaimStatus.Cancelled or ClaimStatus.Draft)).ToList();

        var dto = new ClaimsReportDto
        {
            Filter = filter,
            CurrencyCode = settings?.BaseCurrencyCode ?? "USD",
            TotalClaims = claims.Count,
            ClaimedValue = claims.Sum(c => c.ClaimedAmount),
            ApprovedValue = claims.Sum(c => c.ApprovedAmount),
            SettledValue = settled.Sum(c => c.SettledAmount),
            RejectedValue = claims.Where(c => c.Status == ClaimStatus.Rejected).Sum(c => c.ClaimedAmount),
            OpenClaims = open.Count,
            OpenClaimValue = open.Sum(c => c.ClaimedAmount),
            BreachingSlaCount = open.Count(c => c.AgeingDays > slaDays),
            AverageSettlementDays = settled.Count == 0
                ? 0 : Math.Round((decimal)settled.Average(c => c.SettlementDays ?? 0), 1),
        };

        dto.ApprovalRatePercent = DistributionMapper.Percent(dto.ApprovedValue, dto.ClaimedValue);

        dto.Bucket0To15 = open.Where(c => c.AgeingDays <= 15).Sum(c => c.ClaimedAmount);
        dto.Bucket16To30 = open.Where(c => c.AgeingDays is > 15 and <= 30).Sum(c => c.ClaimedAmount);
        dto.Bucket31To60 = open.Where(c => c.AgeingDays is > 30 and <= 60).Sum(c => c.ClaimedAmount);
        dto.Bucket60Plus = open.Where(c => c.AgeingDays > 60).Sum(c => c.ClaimedAmount);

        dto.ByKind = claims
            .GroupBy(c => c.Kind)
            .Select(g => new RankedRowDto
            {
                Name = g.Key.ToString(),
                Value = g.Sum(x => x.ClaimedAmount),
                Quantity = g.Count(),
            })
            .OrderByDescending(r => r.Value).ToList();

        dto.ByPartner = claims
            .Where(c => c.PartnerId.HasValue)
            .GroupBy(c => new { c.PartnerId, Name = c.Partner?.Name ?? "Unknown" })
            .Select(g => new RankedRowDto
            {
                Id = g.Key.PartnerId,
                Name = g.Key.Name,
                Value = g.Sum(x => x.ClaimedAmount),
                Quantity = g.Count(),
                SubLabel = $"{g.Average(x => x.AgeingDays):N0} days average age",
            })
            .OrderByDescending(r => r.Value).Take(25).ToList();

        dto.Trend = claims
            .GroupBy(c => c.SubmittedOn.Date)
            .Select(g => new TrendPointDto
            {
                Bucket = g.Key,
                Label = g.Key.ToString("dd MMM"),
                Value = g.Sum(x => x.ClaimedAmount),
                SecondaryValue = g.Sum(x => x.SettledAmount),
                Count = g.Count(),
            })
            .OrderBy(p => p.Bucket).ToList();

        return dto;
    }

    public async Task<StockReportDto> GetStockReportAsync(DistributionReportFilter filter)
    {
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        var vanStock = await db.VanStockBalances.ForTenant(tenant)
            .Include(b => b.VanUnit)
            .Where(b => b.Quantity > 0)
            .WhereIf(filter.ItemId.HasValue, b => b.ItemId == filter.ItemId)
            .ToListAsync();

        var declarations = await db.StockDeclarations.ForTenant(tenant)
            .Include(d => d.Partner)
            .Where(d => d.AsOfDate >= DateTime.UtcNow.Date.AddDays(-45))
            .WhereIf(filter.PartnerId.HasValue, d => d.PartnerId == filter.PartnerId)
            .ToListAsync();

        var dto = new StockReportDto
        {
            Filter = filter,
            CurrencyCode = settings?.BaseCurrencyCode ?? "USD",
            VanStockValue = vanStock.Sum(b => b.Quantity * b.UnitCost),
            ChannelStockValue = declarations.Sum(d => d.TotalValue),
            NearExpiryValue = declarations.Sum(d => d.NearExpiryValue)
                              + vanStock.Where(b => b.ExpiryDate != null
                                    && b.ExpiryDate <= DateTime.UtcNow.Date.AddDays(settings?.NearExpiryWarningDays ?? 90))
                                  .Sum(b => b.Quantity * b.UnitCost),
            ExpiredValue = declarations.Sum(d => d.ExpiredValue)
                           + vanStock.Where(b => b.Compartment == VanCompartment.Expired)
                               .Sum(b => b.Quantity * b.UnitCost),
        };

        dto.InTransitValue = await db.TransferRequests.ForTenant(tenant)
            .Where(t => t.Status == TransferRequestStatus.InTransit)
            .SumAsync(t => (decimal?)t.TotalValue) ?? 0;

        dto.TotalStockValue = dto.VanStockValue + dto.ChannelStockValue + dto.InTransitValue;
        dto.DaysOfCover = declarations.Count == 0 ? 0 : Math.Round(declarations.Average(d => d.DaysOfCover), 1);

        dto.ByLocation = vanStock
            .GroupBy(b => new { b.VanUnitId, Name = b.VanUnit?.Name ?? "Van" })
            .Select(g => new RankedRowDto
            {
                Id = g.Key.VanUnitId,
                Name = g.Key.Name,
                Value = g.Sum(x => x.Quantity * x.UnitCost),
                Quantity = g.Sum(x => x.Quantity),
            })
            .Concat(declarations
                .GroupBy(d => new { d.PartnerId, Name = d.Partner?.Name ?? "Partner" })
                .Select(g => new RankedRowDto
                {
                    Id = g.Key.PartnerId,
                    Name = g.Key.Name,
                    Value = g.Sum(x => x.TotalValue),
                }))
            .OrderByDescending(r => r.Value)
            .Take(50).ToList();

        // Dead stock: on a van, never moved in sixty days. The most reliably ignored money in
        // distribution.
        var stale = DateTime.UtcNow.AddDays(-60);
        dto.DeadStockValue = vanStock
            .Where(b => b.LastMovementAt == null || b.LastMovementAt < stale)
            .Sum(b => b.Quantity * b.UnitCost);

        return dto;
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    private static bool IsRevenue(DistributionOrderStatus s)
        => s != DistributionOrderStatus.Cancelled
           && s != DistributionOrderStatus.Rejected
           && s != DistributionOrderStatus.Draft;

    private static (DateTime From, DateTime To) Window(DistributionReportFilter filter)
    {
        var to = (filter.To ?? DateTime.UtcNow).Date;
        var from = (filter.From ?? to.AddDays(-30)).Date;
        return (from, to);
    }

    private static IQueryable<DistributionOrder> ApplyFilter(
        IQueryable<DistributionOrder> query, DistributionReportFilter filter, DateTime from, DateTime to)
        => query
            .Where(o => o.OrderDate >= from && o.OrderDate <= to)
            .WhereIf(filter.TerritoryId.HasValue, o => o.TerritoryId == filter.TerritoryId)
            .WhereIf(filter.RouteId.HasValue, o => o.RouteId == filter.RouteId)
            .WhereIf(filter.PartnerId.HasValue, o => o.PartnerId == filter.PartnerId)
            .WhereIf(filter.OutletId.HasValue, o => o.OutletId == filter.OutletId)
            .WhereIf(filter.FieldRepId.HasValue, o => o.FieldRepId == filter.FieldRepId)
            .WhereIf(filter.WarehouseId.HasValue, o => o.WarehouseId == filter.WarehouseId);

    private async Task<int> CountUnsettledAsync(DateTime today)
        => await db.FieldDays.ForTenant(tenant)
            .CountAsync(d => d.WorkDate >= today.AddDays(-7)
                             && (d.Status == FieldDayStatus.Closed || d.Status == FieldDayStatus.ForceClosed)
                             && !db.Settlements.ForTenant(tenant)
                                 .Any(s => s.FieldDayId == d.Id && s.Status == SettlementStatus.Closed));

    private async Task<List<TrendPointDto>> BuildTrendAsync(
        DateTime from, DateTime to, string granularity, Guid? territoryId)
    {
        var rows = await db.Orders.ForTenant(tenant)
            .Where(o => o.OrderDate >= from && o.OrderDate <= to.AddDays(1) && IsRevenue(o.Status))
            .WhereIf(territoryId.HasValue, o => o.TerritoryId == territoryId)
            .Select(o => new { o.OrderDate, o.TotalAmount })
            .ToListAsync();

        var buckets = granularity.Equals("Month", StringComparison.OrdinalIgnoreCase)
            ? rows.GroupBy(r => new DateTime(r.OrderDate.Year, r.OrderDate.Month, 1, 0, 0, 0, DateTimeKind.Utc))
            : granularity.Equals("Week", StringComparison.OrdinalIgnoreCase)
                ? rows.GroupBy(r => r.OrderDate.Date.AddDays(-(int)r.OrderDate.DayOfWeek))
                : rows.GroupBy(r => r.OrderDate.Date);

        return buckets
            .Select(g => new TrendPointDto
            {
                Bucket = g.Key,
                Label = granularity.Equals("Month", StringComparison.OrdinalIgnoreCase)
                    ? g.Key.ToString("MMM yyyy") : g.Key.ToString("dd MMM"),
                Value = g.Sum(x => x.TotalAmount),
                Count = g.Count(),
            })
            .OrderBy(p => p.Bucket)
            .ToList();
    }

    private async Task<List<RankedRowDto>> TopByAsync(
        string groupBy, DateTime from, DateTime to, Guid? territoryId, int take)
    {
        var orders = db.Orders.ForTenant(tenant)
            .Where(o => o.OrderDate >= from && o.OrderDate < to && IsRevenue(o.Status))
            .WhereIf(territoryId.HasValue, o => o.TerritoryId == territoryId);

        List<RankedRowDto> rows = groupBy switch
        {
            "Item" => await db.OrderLines.ForTenant(tenant)
                .Where(l => orders.Any(o => o.Id == l.OrderId))
                .GroupBy(l => new { l.ItemId, l.ItemName })
                .Select(g => new RankedRowDto
                {
                    Id = g.Key.ItemId,
                    Name = g.Key.ItemName,
                    Value = g.Sum(x => x.LineTotal),
                    Quantity = g.Sum(x => x.BaseQuantity),
                })
                .OrderByDescending(r => r.Value).Take(take).ToListAsync(),

            "FieldRep" => await orders
                .Where(o => o.FieldRepId != null)
                .GroupBy(o => o.FieldRepId!.Value)
                .Select(g => new RankedRowDto { Id = g.Key, Value = g.Sum(x => x.TotalAmount), Quantity = g.Count() })
                .OrderByDescending(r => r.Value).Take(take).ToListAsync(),

            "Territory" => await orders
                .Where(o => o.TerritoryId != null)
                .GroupBy(o => o.TerritoryId!.Value)
                .Select(g => new RankedRowDto { Id = g.Key, Value = g.Sum(x => x.TotalAmount), Quantity = g.Count() })
                .OrderByDescending(r => r.Value).Take(take).ToListAsync(),

            "Partner" => await orders
                .Where(o => o.PartnerId != null)
                .GroupBy(o => o.PartnerId!.Value)
                .Select(g => new RankedRowDto { Id = g.Key, Value = g.Sum(x => x.TotalAmount), Quantity = g.Count() })
                .OrderByDescending(r => r.Value).Take(take).ToListAsync(),

            "Route" => await orders
                .Where(o => o.RouteId != null)
                .GroupBy(o => o.RouteId!.Value)
                .Select(g => new RankedRowDto { Id = g.Key, Value = g.Sum(x => x.TotalAmount), Quantity = g.Count() })
                .OrderByDescending(r => r.Value).Take(take).ToListAsync(),

            _ => await orders
                .Where(o => o.OutletId != null)
                .GroupBy(o => o.OutletId!.Value)
                .Select(g => new RankedRowDto { Id = g.Key, Value = g.Sum(x => x.TotalAmount), Quantity = g.Count() })
                .OrderByDescending(r => r.Value).Take(take).ToListAsync(),
        };

        await NameRowsAsync(rows, groupBy);

        var total = rows.Sum(r => r.Value);
        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].Rank = i + 1;
            rows[i].SharePercent = DistributionMapper.Percent(rows[i].Value, total);
        }

        return rows;
    }

    private async Task NameRowsAsync(List<RankedRowDto> rows, string groupBy)
    {
        if (rows.Count == 0 || groupBy == "Item") return;

        var ids = rows.Where(r => r.Id.HasValue).Select(r => r.Id!.Value).ToList();

        Dictionary<Guid, string> names = groupBy switch
        {
            "FieldRep" => await db.FieldReps.ForTenant(tenant)
                .Where(r => ids.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.FullName),
            "Territory" => await db.Territories.ForTenant(tenant)
                .Where(t => ids.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Name),
            "Partner" => await db.Partners.ForTenant(tenant)
                .Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name),
            "Route" => await db.Routes.ForTenant(tenant)
                .Where(r => ids.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.Name),
            _ => await db.Outlets.ForTenant(tenant)
                .Where(o => ids.Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.Name),
        };

        foreach (var row in rows.Where(r => r.Id.HasValue))
            row.Name = names.GetValueOrDefault(row.Id!.Value) ?? "Unknown";
    }
}
