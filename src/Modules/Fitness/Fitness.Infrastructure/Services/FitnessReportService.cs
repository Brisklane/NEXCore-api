using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Entities;
using Fitness.Domain.Enums;
using Fitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fitness.Infrastructure.Services;

/// <summary>
/// Every report the app ships.
///
/// The numbers a gym is actually run on are here, and two of them are worth calling out because
/// almost every SMB tool in this market gets them wrong:
///
/// - **Churn** is leavers over *average* active members in the period, not over the closing count.
///   Dividing by the closing figure flatters a shrinking club and punishes a growing one.
/// - **MRR movement** is decomposed into new, expansion, contraction, churned and reactivated,
///   because "revenue went down" is not a finding — "eleven downgrades and four cancellations"
///   is.
/// </summary>
public class FitnessReportService(FitnessDbContext db, IFitnessTenant tenant) : IFitnessReportService
{
    // ── Dashboard ────────────────────────────────────────────────────────────

    public async Task<FitnessDashboardDto> GetDashboardAsync(Guid? clubId)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var tomorrow = today.AddDays(1);
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var lastMonthStart = monthStart.AddMonths(-1);

        var dto = new FitnessDashboardDto
        {
            ClubId = clubId,
            GeneratedAt = now,
        };

        var clubs = await db.Clubs.ForTenant(tenant)
            .WhereIf(clubId is not null, c => c.Id == clubId)
            .ToListAsync();

        dto.ClubName = clubId is null
            ? (clubs.Count == 1 ? clubs[0].Name : "All clubs")
            : clubs.FirstOrDefault()?.Name ?? "Club";

        dto.CurrencyCode = clubs.FirstOrDefault()?.CurrencyCode ?? "USD";
        dto.InClubNow = clubs.Sum(c => c.CurrentOccupancy);
        dto.ClubCapacity = clubs.Sum(c => c.HardCapacity ?? c.SoftCapacity ?? 0);
        dto.OccupancyPercent = FitnessMapper.Percent(dto.InClubNow, dto.ClubCapacity ?? 0);

        // ── Right now ────────────────────────────────────────────────────────

        dto.CheckInsToday = await db.CheckIns.ForTenant(tenant)
            .CountAsync(c => c.CheckedInAt >= today && (clubId == null || c.ClubId == clubId));

        var todaysClasses = await db.ClassOccurrences.ForTenant(tenant)
            .Where(o => o.StartsAt >= today && o.StartsAt < tomorrow && o.Status != ClassOccurrenceStatus.Cancelled)
            .WhereIf(clubId is not null, o => o.ClubId == clubId)
            .Include(o => o.ClassType)
            .Include(o => o.Room)
            .OrderBy(o => o.StartsAt)
            .ToListAsync();

        dto.ClassesToday = todaysClasses.Count;
        dto.ClassesRemainingToday = todaysClasses.Count(o => o.StartsAt > now);

        // Fixable today: a class starting in the next few hours that is barely booked.
        dto.UnderFilledClassesToday = todaysClasses
            .Count(o => o.StartsAt > now && o.StartsAt < now.AddHours(6)
                     && o.Capacity > 0 && o.BookedCount * 100 / o.Capacity < 30);

        dto.NextClasses = [.. todaysClasses
            .Where(o => o.StartsAt > now)
            .Take(6)
            .Select(o => FitnessMapper.ToSummary(o, now))];

        dto.AppointmentsToday = await db.Appointments.ForTenant(tenant)
            .CountAsync(a => a.StartsAt >= today && a.StartsAt < tomorrow
                          && a.Status != AppointmentStatus.Cancelled
                          && (clubId == null || a.ClubId == clubId));

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        var sla = settings?.LeadResponseSlaMinutes ?? 15;

        dto.LeadsBreachingSla = await db.Leads.ForTenant(tenant)
            .CountAsync(l => l.FirstContactedAt == null && l.ReceivedAt < now.AddMinutes(-sla)
                          && l.Status != LeadStatus.Won && l.Status != LeadStatus.Lost
                          && (clubId == null || l.ClubId == clubId));

        dto.TasksDueToday = await db.RetentionTasks.ForTenant(tenant)
            .CountAsync(t => t.CompletedAt == null && !t.IsDismissed && t.DueOn <= today
                          && (clubId == null || t.ClubId == clubId));

        var controllers = await db.Controllers.ForTenant(tenant)
            .WhereIf(clubId is not null, c => c.ClubId == clubId)
            .Where(c => c.IsActive)
            .Select(c => new { c.LastHeartbeatAt, c.HeartbeatTimeoutMinutes })
            .ToListAsync();

        dto.DoorsOffline = controllers.Count(c =>
            c.LastHeartbeatAt is null || c.LastHeartbeatAt.Value.AddMinutes(c.HeartbeatTimeoutMinutes) < now);

        dto.EquipmentOutOfService = await db.Equipment.ForTenant(tenant)
            .CountAsync(e => e.Status == AssetStatus.OutOfOrder && (clubId == null || e.ClubId == clubId));

        dto.OpenIncidents = await db.Incidents.ForTenant(tenant)
            .CountAsync(i => i.Status != IncidentStatus.Closed && (clubId == null || i.ClubId == clubId));

        dto.TakingsToday = await db.Payments.ForTenant(tenant)
            .Where(p => p.ReceivedOn >= today && p.Status == PaymentStatus.Succeeded
                     && (clubId == null || p.ClubId == clubId))
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        // ── Membership ───────────────────────────────────────────────────────

        var memberQuery = db.Members.ForTenant(tenant).WhereIf(clubId is not null, m => m.HomeClubId == clubId);

        dto.ActiveMembers = await memberQuery.CountAsync(m => m.Status == MemberStatus.Active
                                                          || m.Status == MemberStatus.WonBack);
        dto.FrozenMembers = await memberQuery.CountAsync(m => m.Status == MemberStatus.Frozen);
        dto.PastDueMembers = await memberQuery.CountAsync(m => m.Status == MemberStatus.PastDue);
        dto.TrialMembers = await memberQuery.CountAsync(m => m.Status == MemberStatus.Trial);

        dto.JoinsThisMonth = await db.Agreements.ForTenant(tenant)
            .CountAsync(a => a.StartsOn >= monthStart && (clubId == null || a.ClubId == clubId));

        dto.CancellationsThisMonth = await db.Agreements.ForTenant(tenant)
            .CountAsync(a => a.CancelledOn >= monthStart && (clubId == null || a.ClubId == clubId));

        dto.NetGrowth = dto.JoinsThisMonth - dto.CancellationsThisMonth;

        var lastMonthCancellations = await db.Agreements.ForTenant(tenant)
            .CountAsync(a => a.CancelledOn >= lastMonthStart && a.CancelledOn < monthStart
                          && (clubId == null || a.ClubId == clubId));

        dto.ActiveMembersLastMonth = dto.ActiveMembers - dto.NetGrowth;
        dto.ChurnRatePercent = Rate(dto.CancellationsThisMonth, dto.ActiveMembers, dto.ActiveMembersLastMonth);
        dto.ChurnRateLastMonth = Rate(lastMonthCancellations, dto.ActiveMembersLastMonth, dto.ActiveMembersLastMonth);

        // ── Money ────────────────────────────────────────────────────────────

        var liveAgreements = await db.Agreements.ForTenant(tenant)
            .Where(a => a.Status == AgreementStatus.Active)
            .WhereIf(clubId is not null, a => a.ClubId == clubId)
            .Select(a => new { a.Price, a.PromotionalPrice, a.PromotionalPeriodsRemaining, a.BillingPeriod })
            .ToListAsync();

        dto.MonthlyRecurringRevenue = Math.Round(liveAgreements.Sum(a =>
            FitnessQueryExtensions.ToMonthly(
                a.PromotionalPeriodsRemaining > 0 ? a.PromotionalPrice ?? a.Price : a.Price,
                a.BillingPeriod)), 2);

        var invoiceQuery = db.Invoices.ForTenant(tenant).WhereIf(clubId is not null, i => i.ClubId == clubId);

        dto.BilledThisMonth = await invoiceQuery
            .Where(i => i.IssuedOn >= monthStart && i.Status != InvoiceStatus.Cancelled)
            .SumAsync(i => (decimal?)i.Total) ?? 0m;

        dto.CollectedThisMonth = await db.Payments.ForTenant(tenant)
            .Where(p => p.ReceivedOn >= monthStart && p.Status == PaymentStatus.Succeeded
                     && (clubId == null || p.ClubId == clubId))
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        dto.RevenueThisMonth = dto.CollectedThisMonth;

        dto.RevenueLastMonth = await db.Payments.ForTenant(tenant)
            .Where(p => p.ReceivedOn >= lastMonthStart && p.ReceivedOn < monthStart
                     && p.Status == PaymentStatus.Succeeded
                     && (clubId == null || p.ClubId == clubId))
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        dto.RevenueChangePercent = dto.RevenueLastMonth == 0
            ? 0
            : Math.Round((dto.RevenueThisMonth - dto.RevenueLastMonth) / dto.RevenueLastMonth * 100m, 1);

        dto.MrrLastMonth = dto.MonthlyRecurringRevenue - (dto.NetGrowth * SafeAverage(liveAgreements.Count, dto.MonthlyRecurringRevenue));
        dto.MrrChangePercent = dto.MrrLastMonth == 0
            ? 0
            : Math.Round((dto.MonthlyRecurringRevenue - dto.MrrLastMonth) / dto.MrrLastMonth * 100m, 1);

        dto.CollectionRatePercent = FitnessMapper.Percent(dto.CollectedThisMonth, dto.BilledThisMonth);

        var arrears = await invoiceQuery
            .Where(i => i.BalanceDue > 0 && i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.WrittenOff)
            .GroupBy(i => 1)
            .Select(g => new { Amount = g.Sum(i => i.BalanceDue), Members = g.Select(i => i.MemberId).Distinct().Count() })
            .FirstOrDefaultAsync();

        dto.OutstandingBalance = arrears?.Amount ?? 0m;
        dto.MembersInArrears = arrears?.Members ?? 0;
        dto.AverageRevenuePerMember = dto.ActiveMembers == 0
            ? 0
            : Math.Round(dto.MonthlyRecurringRevenue / dto.ActiveMembers, 2);

        // ── Retention ────────────────────────────────────────────────────────

        var scores = await db.ChurnScores.ForTenant(tenant)
            .WhereIf(clubId is not null, c => c.ClubId == clubId)
            .Select(c => new { c.Band, c.MemberId })
            .ToListAsync();

        dto.AtRiskMembers = scores.Count(s => s.Band == ChurnRiskBand.AtRisk);
        dto.CriticalRiskMembers = scores.Count(s => s.Band == ChurnRiskBand.Critical);

        var atRiskIds = scores
            .Where(s => s.Band is ChurnRiskBand.AtRisk or ChurnRiskBand.Critical)
            .Select(s => s.MemberId)
            .ToList();

        if (atRiskIds.Count > 0)
        {
            var atRiskValue = await db.Agreements.ForTenant(tenant)
                .Where(a => atRiskIds.Contains(a.MemberId) && a.Status == AgreementStatus.Active)
                .Select(a => new { a.Price, a.BillingPeriod })
                .ToListAsync();

            dto.ValueAtRisk = Math.Round(atRiskValue.Sum(a =>
                FitnessQueryExtensions.ToMonthly(a.Price, a.BillingPeriod)), 2);
        }

        var nps = await db.NpsResponses.ForTenant(tenant)
            .Where(n => n.RespondedAt >= now.AddDays(-90) && (clubId == null || n.ClubId == clubId))
            .Select(n => n.Score)
            .ToListAsync();

        if (nps.Count > 0)
            dto.Nps = (nps.Count(s => s >= 9) * 100 / nps.Count) - (nps.Count(s => s <= 6) * 100 / nps.Count);

        // ── Sales ────────────────────────────────────────────────────────────

        var leadQuery = db.Leads.ForTenant(tenant).WhereIf(clubId is not null, l => l.ClubId == clubId);

        dto.OpenLeads = await leadQuery.CountAsync(l => l.Status != LeadStatus.Won && l.Status != LeadStatus.Lost);
        dto.LeadsThisMonth = await leadQuery.CountAsync(l => l.ReceivedAt >= monthStart);

        dto.ToursThisMonth = await db.Tours.ForTenant(tenant)
            .CountAsync(t => t.CompletedAt >= monthStart && (clubId == null || t.ClubId == clubId));

        var closed = await leadQuery
            .Where(l => l.WonOn >= monthStart || l.LostOn >= monthStart)
            .Select(l => l.WonOn != null)
            .ToListAsync();

        dto.LeadConversionPercent = FitnessMapper.Percent(closed.Count(w => w), closed.Count);

        var responses = await leadQuery
            .Where(l => l.ResponseMinutes != null && l.ReceivedAt >= monthStart)
            .Select(l => l.ResponseMinutes!.Value)
            .ToListAsync();

        dto.MedianResponseMinutes = Median(responses);

        // ── Charts ───────────────────────────────────────────────────────────

        dto.MemberTrend = await BuildMemberTrendAsync(clubId, now);
        dto.RevenueTrend = await BuildRevenueTrendAsync(clubId, now);
        dto.VisitsByHour = await BuildHourlyVisitsAsync(clubId, today);
        dto.RevenueByStream = await BuildRevenueByStreamAsync(clubId, monthStart, now);
        dto.NeedsAttention = BuildAttention(dto);

        return dto;
    }

    // ── Membership ───────────────────────────────────────────────────────────

    public async Task<MembershipReportDto> GetMembershipReportAsync(ReportFilterDto filter)
    {
        var (from, to) = ResolvePeriod(filter);

        var report = new MembershipReportDto { ClubId = filter.ClubId, From = from, To = to };

        var agreements = await db.Agreements.ForTenant(tenant)
            .Include(a => a.Plan)
            .WhereIf(filter.ClubId is not null, a => a.ClubId == filter.ClubId)
            .WhereIf(filter.PlanId is not null, a => a.PlanId == filter.PlanId)
            .ToListAsync();

        report.Joins = agreements.Count(a => a.StartsOn >= from && a.StartsOn <= to && a.SupersedesAgreementId is null);
        report.Cancellations = agreements.Count(a => a.CancelledOn >= from && a.CancelledOn <= to);
        report.Expiries = agreements.Count(a => a.Status == AgreementStatus.Expired
                                             && a.EndsOn >= from && a.EndsOn <= to);
        report.Upgrades = agreements.Count(a => a.Amendments.Any(m => m.Kind == AgreementChangeKind.Upgrade
                                                                   && m.EffectiveOn >= from && m.EffectiveOn <= to));
        report.Downgrades = agreements.Count(a => a.Amendments.Any(m => m.Kind == AgreementChangeKind.Downgrade
                                                                     && m.EffectiveOn >= from && m.EffectiveOn <= to));

        var members = await db.Members.ForTenant(tenant)
            .WhereIf(filter.ClubId is not null, m => m.HomeClubId == filter.ClubId)
            .ToListAsync();

        report.ClosingActive = members.Count(m => m.Status is MemberStatus.Active or MemberStatus.WonBack);
        report.Rejoins = members.Count(m => m.Status == MemberStatus.WonBack && m.JoinedOn >= from && m.JoinedOn <= to);
        report.OpeningActive = report.ClosingActive - report.Joins + report.Cancellations + report.Expiries;
        report.NetGrowth = report.ClosingActive - report.OpeningActive;
        report.Frozen = members.Count(m => m.Status == MemberStatus.Frozen);
        report.Suspended = members.Count(m => m.Status == MemberStatus.Suspended);

        // Over the *average* active count, which is the only fair denominator.
        var average = (report.OpeningActive + report.ClosingActive) / 2m;
        report.GrossChurnPercent = average == 0 ? 0 : Math.Round((report.Cancellations + report.Expiries) / average * 100m, 2);
        report.NetChurnPercent = average == 0 ? 0
            : Math.Round((report.Cancellations + report.Expiries - report.Rejoins) / average * 100m, 2);

        report.FreezeDaysTaken = await db.Freezes.ForTenant(tenant)
            .Where(f => f.StartsOn >= from && f.StartsOn <= to)
            .SumAsync(f => (int?)f.DaysExtended) ?? 0;

        var tenures = members
            .Where(m => m.JoinedOn is not null)
            .Select(m => ((m.LeftOn ?? to) - m.JoinedOn!.Value).TotalDays / 30.44)
            .ToList();

        report.AverageTenureMonths = tenures.Count == 0 ? 0 : Math.Round((decimal)tenures.Average(), 1);

        var lifetime = await db.Payments.ForTenant(tenant)
            .Where(p => p.Status == PaymentStatus.Succeeded)
            .WhereIf(filter.ClubId is not null, p => p.ClubId == filter.ClubId)
            .GroupBy(p => p.MemberId)
            .Select(g => g.Sum(p => p.Amount))
            .ToListAsync();

        report.AverageLifetimeValue = lifetime.Count == 0 ? 0 : Math.Round(lifetime.Average(), 2);

        // ── Plan mix ─────────────────────────────────────────────────────────

        report.PlanMix = [.. agreements
            .Where(a => a.Status == AgreementStatus.Active)
            .GroupBy(a => new { a.PlanId, Name = a.Plan?.Name ?? "Unknown", Kind = a.Plan?.Kind ?? PlanKind.RecurringMembership })
            .Select(g =>
            {
                var joins = agreements.Count(a => a.PlanId == g.Key.PlanId && a.StartsOn >= from && a.StartsOn <= to);
                var leaves = agreements.Count(a => a.PlanId == g.Key.PlanId && a.CancelledOn >= from && a.CancelledOn <= to);

                return new PlanMixLineDto
                {
                    PlanId = g.Key.PlanId,
                    PlanName = g.Key.Name,
                    Kind = g.Key.Kind,
                    MemberCount = g.Count(),
                    PercentOfBase = report.ClosingActive == 0 ? 0 : Math.Round(g.Count() * 100m / report.ClosingActive, 1),
                    MonthlyRevenue = Math.Round(g.Sum(a => FitnessQueryExtensions.ToMonthly(a.Price, a.BillingPeriod)), 2),
                    AveragePrice = Math.Round(g.Average(a => a.Price), 2),
                    JoinsInPeriod = joins,
                    CancellationsInPeriod = leaves,
                    ChurnPercent = g.Count() == 0 ? 0 : Math.Round(leaves * 100m / g.Count(), 1),
                };
            })
            .OrderByDescending(p => p.MemberCount)];

        // ── Why people left ──────────────────────────────────────────────────

        var cancellations = await db.CancellationRequests.ForTenant(tenant)
            .Include(c => c.Agreement)
            .Where(c => c.RequestedOn >= from && c.RequestedOn <= to)
            .WhereIf(filter.ClubId is not null, c => c.Agreement!.ClubId == filter.ClubId)
            .ToListAsync();

        report.LeaveReasons = [.. cancellations
            .GroupBy(c => c.Reason)
            .Select(g => new LeaveReasonLineDto
            {
                Reason = g.Key,
                Label = Describe(g.Key),
                Count = g.Count(),
                PercentOfTotal = cancellations.Count == 0 ? 0 : Math.Round(g.Count() * 100m / cancellations.Count, 1),
                ValueLost = Math.Round(g.Where(c => !c.WasSaved).Sum(c => c.Agreement?.Price ?? 0), 2),
                SavedCount = g.Count(c => c.WasSaved),
                SaveRatePercent = FitnessMapper.Percent(g.Count(c => c.WasSaved), g.Count()),
            })
            .OrderByDescending(r => r.Count)];

        // ── Tenure ───────────────────────────────────────────────────────────

        var bands = new[]
        {
            ("Under 3 months", 0, 90),
            ("3–6 months", 90, 180),
            ("6–12 months", 180, 365),
            ("1–2 years", 365, 730),
            ("Over 2 years", 730, int.MaxValue),
        };

        foreach (var (label, low, high) in bands)
        {
            var inBand = members
                .Where(m => m.Status is MemberStatus.Active or MemberStatus.WonBack && m.JoinedOn is not null)
                .Where(m =>
                {
                    var days = (to - m.JoinedOn!.Value).TotalDays;
                    return days >= low && days < high;
                })
                .ToList();

            if (inBand.Count == 0) continue;

            var bandIds = inBand.Select(m => m.Id).ToHashSet();
            var bandLeavers = cancellations.Count(c => bandIds.Contains(c.MemberId));

            report.TenureDistribution.Add(new TenureBandDto
            {
                Band = label,
                MemberCount = inBand.Count,
                PercentOfBase = report.ClosingActive == 0 ? 0 : Math.Round(inBand.Count * 100m / report.ClosingActive, 1),
                ChurnPercent = Math.Round(bandLeavers * 100m / inBand.Count, 1),
            });
        }

        report.Trend = await BuildMemberTrendAsync(filter.ClubId, to);

        return report;
    }

    /// <summary>
    /// Retention by join cohort.
    ///
    /// The report that tells an owner which channel brings members who stay: of everyone who
    /// joined in March, what share were still here at month one, three, six and twelve. A source
    /// that produces cheap leads with 40% six-month retention is more expensive than an
    /// expensive one at 80%.
    /// </summary>
    public async Task<CohortRetentionDto> GetCohortRetentionAsync(ReportFilterDto filter)
    {
        var (from, to) = ResolvePeriod(filter);
        var now = DateTime.UtcNow;

        var report = new CohortRetentionDto
        {
            ClubId = filter.ClubId,
            GroupedBy = filter.GroupBy ?? "JoinMonth",
            From = from,
            To = to,
            Periods = [1, 3, 6, 12, 24],
        };

        var members = await db.Members.ForTenant(tenant)
            .Where(m => m.JoinedOn != null && m.JoinedOn >= from && m.JoinedOn <= to)
            .WhereIf(filter.ClubId is not null, m => m.HomeClubId == filter.ClubId)
            .Select(m => new { m.Id, m.JoinedOn, m.LeftOn, m.Status, m.LeadSourceId })
            .ToListAsync();

        var lifetime = await db.Payments.ForTenant(tenant)
            .Where(p => p.Status == PaymentStatus.Succeeded)
            .GroupBy(p => p.MemberId)
            .Select(g => new { MemberId = g.Key, Total = g.Sum(p => p.Amount) })
            .ToListAsync();

        var sourceNames = await db.LeadSources.ForTenant(tenant)
            .Select(s => new { s.Id, s.Name })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        var groups = report.GroupedBy == "Source"
            ? members.GroupBy(m => m.LeadSourceId is null ? "Unknown" : sourceNames.GetValueOrDefault(m.LeadSourceId.Value, "Unknown"))
            : members.GroupBy(m => new DateTime(m.JoinedOn!.Value.Year, m.JoinedOn.Value.Month, 1).ToString("MMM yyyy"));

        foreach (var group in groups.OrderBy(g => g.Key))
        {
            var cohort = group.ToList();
            var cohortStart = cohort.Min(m => m.JoinedOn!.Value);

            var row = new CohortRowDto
            {
                Label = group.Key,
                CohortStart = cohortStart,
                InitialSize = cohort.Count,
                AverageLifetimeValue = Math.Round(cohort
                    .Select(m => lifetime.FirstOrDefault(l => l.MemberId == m.Id)?.Total ?? 0)
                    .DefaultIfEmpty(0)
                    .Average(), 2),
            };

            foreach (var months in report.Periods)
            {
                var checkpoint = cohortStart.AddMonths(months);

                // A cohort that has not reached the checkpoint yet gets null rather than a
                // misleadingly high number.
                if (checkpoint > now)
                {
                    row.RetentionPercent.Add(null);
                    row.RetainedCount.Add(null);
                    continue;
                }

                var retained = cohort.Count(m => m.LeftOn is null || m.LeftOn > checkpoint);
                row.RetainedCount.Add(retained);
                row.RetentionPercent.Add(FitnessMapper.Percent(retained, cohort.Count));
            }

            report.Cohorts.Add(row);
        }

        return report;
    }

    // ── Money ────────────────────────────────────────────────────────────────

    public async Task<RevenueReportDto> GetRevenueReportAsync(ReportFilterDto filter)
    {
        var (from, to) = ResolvePeriod(filter);

        var report = new RevenueReportDto { ClubId = filter.ClubId, From = from, To = to };

        var club = filter.ClubId is null
            ? null
            : await db.Clubs.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == filter.ClubId);

        report.CurrencyCode = club?.CurrencyCode ?? "USD";

        var invoices = await db.Invoices.ForTenant(tenant)
            .Where(i => i.IssuedOn >= from && i.IssuedOn <= to && i.Status != InvoiceStatus.Cancelled)
            .WhereIf(filter.ClubId is not null, i => i.ClubId == filter.ClubId)
            .Select(i => new { i.Total, i.TaxTotal, i.AmountRefunded })
            .ToListAsync();

        report.TotalBilled = invoices.Sum(i => i.Total);
        report.TaxCollected = invoices.Sum(i => i.TaxTotal);
        report.TotalRefunded = invoices.Sum(i => i.AmountRefunded);

        var payments = await db.Payments.ForTenant(tenant)
            .Where(p => p.ReceivedOn >= from && p.ReceivedOn <= to && p.Status == PaymentStatus.Succeeded)
            .WhereIf(filter.ClubId is not null, p => p.ClubId == filter.ClubId)
            .Select(p => new { p.Amount, p.Method, p.ClubId })
            .ToListAsync();

        report.TotalCollected = payments.Sum(p => p.Amount);

        report.TotalWrittenOff = await db.WriteOffs.ForTenant(tenant)
            .Where(w => w.WrittenOffOn >= from && w.WrittenOffOn <= to)
            .WhereIf(filter.ClubId is not null, w => w.ClubId == filter.ClubId)
            .SumAsync(w => (decimal?)w.Amount) ?? 0m;

        report.NetRevenue = report.TotalCollected - report.TotalRefunded;

        report.RecognisedRevenue = await db.DeferredRevenueEntries.ForTenant(tenant)
            .Where(e => e.RecognisedOn >= from && e.RecognisedOn <= to)
            .SumAsync(e => (decimal?)e.Amount) ?? 0m;

        report.DeferredBalance = await db.DeferredRevenue.ForTenant(tenant)
            .Where(s => !s.IsClosed)
            .WhereIf(filter.ClubId is not null, s => s.ClubId == filter.ClubId)
            .SumAsync(s => (decimal?)s.RemainingAmount) ?? 0m;

        report.ByStream = await BuildRevenueByStreamAsync(filter.ClubId, from, to);

        report.ByPaymentMethod = [.. payments
            .GroupBy(p => p.Method)
            .Select(g => new RevenueLineDto
            {
                Label = g.Key.ToString(),
                Amount = g.Sum(p => p.Amount),
                Count = g.Count(),
                PercentOfTotal = report.TotalCollected == 0 ? 0 : Math.Round(g.Sum(p => p.Amount) / report.TotalCollected * 100m, 1),
            })
            .OrderByDescending(l => l.Amount)];

        if (filter.ClubId is null)
        {
            var clubNames = await db.Clubs.ForTenant(tenant)
                .Select(c => new { c.Id, c.Name }).ToDictionaryAsync(c => c.Id, c => c.Name);

            report.ByClub = [.. payments
                .GroupBy(p => p.ClubId)
                .Select(g => new RevenueLineDto
                {
                    Label = clubNames.GetValueOrDefault(g.Key, "Unknown"),
                    Amount = g.Sum(p => p.Amount),
                    Count = g.Count(),
                    PercentOfTotal = report.TotalCollected == 0 ? 0 : Math.Round(g.Sum(p => p.Amount) / report.TotalCollected * 100m, 1),
                })
                .OrderByDescending(l => l.Amount)];
        }

        report.Trend = await BuildRevenueTrendAsync(filter.ClubId, to);

        var activeMembers = await db.Members.ForTenant(tenant)
            .CountAsync(m => m.Status == MemberStatus.Active && (filter.ClubId == null || m.HomeClubId == filter.ClubId));

        report.AverageRevenuePerMember = activeMembers == 0 ? 0 : Math.Round(report.NetRevenue / activeMembers, 2);
        report.AverageTransactionValue = payments.Count == 0 ? 0 : Math.Round(payments.Average(p => p.Amount), 2);

        return report;
    }

    /// <summary>
    /// MRR movement, decomposed.
    ///
    /// Opening, plus new, plus expansion, less contraction, less churn, plus reactivation, equals
    /// closing. A single "MRR is down 3%" tells nobody what to do; the five components tell them
    /// whether to fix pricing, sales or retention.
    /// </summary>
    public async Task<MrrMovementDto> GetMrrMovementAsync(ReportFilterDto filter)
    {
        var (from, to) = ResolvePeriod(filter);

        var report = new MrrMovementDto
        {
            ClubId = filter.ClubId,
            PeriodStart = from,
            PeriodEnd = to,
        };

        var club = filter.ClubId is null
            ? null
            : await db.Clubs.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == filter.ClubId);

        report.CurrencyCode = club?.CurrencyCode ?? "USD";

        var agreements = await db.Agreements.ForTenant(tenant)
            .Include(a => a.Amendments.Where(m => !m.IsDeleted))
            .WhereIf(filter.ClubId is not null, a => a.ClubId == filter.ClubId)
            .Where(a => a.BillingPeriod != BillingPeriod.OneOff)
            .ToListAsync();

        decimal Monthly(decimal price, BillingPeriod period) => FitnessQueryExtensions.ToMonthly(price, period);

        // Live at the start of the window.
        report.OpeningMrr = Math.Round(agreements
            .Where(a => a.StartsOn < from && (a.CancelledOn is null || a.CancelledOn >= from))
            .Sum(a => Monthly(a.Price, a.BillingPeriod)), 2);

        report.NewMrr = Math.Round(agreements
            .Where(a => a.StartsOn >= from && a.StartsOn <= to && a.SupersedesAgreementId is null)
            .Sum(a => Monthly(a.Price, a.BillingPeriod)), 2);

        report.ReactivationMrr = Math.Round(agreements
            .Where(a => a.StartsOn >= from && a.StartsOn <= to && a.SupersedesAgreementId is not null)
            .Sum(a => Monthly(a.Price, a.BillingPeriod)), 2);

        report.ChurnedMrr = Math.Round(agreements
            .Where(a => a.CancelledOn >= from && a.CancelledOn <= to)
            .Sum(a => Monthly(a.Price, a.BillingPeriod)), 2);

        var amendments = agreements
            .SelectMany(a => a.Amendments.Where(m => !m.IsDeleted && m.EffectiveOn >= from && m.EffectiveOn <= to)
                .Select(m => new { a.BillingPeriod, m.Kind, m.PreviousPrice, m.NewPrice }))
            .Where(m => m.PreviousPrice is not null && m.NewPrice is not null)
            .ToList();

        report.ExpansionMrr = Math.Round(amendments
            .Where(m => m.NewPrice > m.PreviousPrice)
            .Sum(m => Monthly(m.NewPrice!.Value - m.PreviousPrice!.Value, m.BillingPeriod)), 2);

        report.ContractionMrr = Math.Round(amendments
            .Where(m => m.NewPrice < m.PreviousPrice)
            .Sum(m => Monthly(m.PreviousPrice!.Value - m.NewPrice!.Value, m.BillingPeriod)), 2);

        report.ClosingMrr = report.OpeningMrr + report.NewMrr + report.ExpansionMrr
                          + report.ReactivationMrr - report.ContractionMrr - report.ChurnedMrr;

        report.NetChange = report.ClosingMrr - report.OpeningMrr;
        report.NetChangePercent = report.OpeningMrr == 0
            ? 0
            : Math.Round(report.NetChange / report.OpeningMrr * 100m, 1);

        report.Trend = await BuildRevenueTrendAsync(filter.ClubId, to);

        return report;
    }

    // ── Attendance ───────────────────────────────────────────────────────────

    public async Task<AttendanceReportDto> GetAttendanceReportAsync(ReportFilterDto filter)
    {
        var (from, to) = ResolvePeriod(filter);

        var report = new AttendanceReportDto { ClubId = filter.ClubId, From = from, To = to };

        var visits = await db.CheckIns.ForTenant(tenant)
            .Where(c => c.CheckedInAt >= from && c.CheckedInAt <= to)
            .WhereIf(filter.ClubId is not null, c => c.ClubId == filter.ClubId)
            .Select(c => new { c.MemberId, c.CheckedInAt, c.DurationMinutes, c.Kind, c.Method })
            .ToListAsync();

        report.TotalVisits = visits.Count;
        report.UniqueMembers = visits.Where(v => v.MemberId is not null).Select(v => v.MemberId).Distinct().Count();
        report.VisitsPerMember = report.UniqueMembers == 0
            ? 0
            : Math.Round((decimal)report.TotalVisits / report.UniqueMembers, 1);

        var durations = visits.Where(v => v.DurationMinutes is not null).Select(v => v.DurationMinutes!.Value).ToList();
        report.AverageVisitMinutes = durations.Count == 0 ? 0 : Math.Round((decimal)durations.Average(), 0);

        var snapshots = await db.OccupancySnapshots.ForTenant(tenant)
            .Where(s => s.TakenAt >= from && s.TakenAt <= to && s.AreaId == null)
            .WhereIf(filter.ClubId is not null, s => s.ClubId == filter.ClubId)
            .Select(s => new { s.TakenAt, s.Occupancy })
            .ToListAsync();

        var peak = snapshots.OrderByDescending(s => s.Occupancy).FirstOrDefault();
        report.PeakOccupancy = peak?.Occupancy ?? 0;
        report.PeakAt = peak?.TakenAt;

        // Members who paid and never came — a churn cohort in waiting.
        var activeMembers = await db.Members.ForTenant(tenant)
            .Where(m => m.Status == MemberStatus.Active)
            .WhereIf(filter.ClubId is not null, m => m.HomeClubId == filter.ClubId)
            .Select(m => new { m.Id })
            .ToListAsync();

        var visitedIds = visits.Where(v => v.MemberId is not null).Select(v => v.MemberId!.Value).ToHashSet();
        report.ZeroVisitMembers = activeMembers.Count(m => !visitedIds.Contains(m.Id));

        var visitCounts = visits
            .Where(v => v.MemberId is not null)
            .GroupBy(v => v.MemberId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var periodWeeks = Math.Max(1, (to - from).TotalDays / 7);
        report.LowUsageMembers = activeMembers.Count(m => visitCounts.GetValueOrDefault(m.Id) < periodWeeks * 0.5);

        report.Heatmap = [.. visits
            .GroupBy(v => new { Day = (int)v.CheckedInAt.DayOfWeek, v.CheckedInAt.Hour })
            .Select(g => new HeatmapCellDto
            {
                DayOfWeek = g.Key.Day,
                Hour = g.Key.Hour,
                Visits = g.Count(),
                AverageOccupancy = snapshots
                    .Where(s => (int)s.TakenAt.DayOfWeek == g.Key.Day && s.TakenAt.Hour == g.Key.Hour)
                    .Select(s => s.Occupancy)
                    .DefaultIfEmpty(0)
                    .Sum() / Math.Max(1, snapshots.Count(s => (int)s.TakenAt.DayOfWeek == g.Key.Day && s.TakenAt.Hour == g.Key.Hour)),
            })
            .OrderBy(c => c.DayOfWeek).ThenBy(c => c.Hour)];

        // Intensity 0–100 relative to the busiest cell, so the heat map colours itself.
        var busiest = report.Heatmap.Count == 0 ? 1 : Math.Max(1, report.Heatmap.Max(c => c.Visits));
        foreach (var cell in report.Heatmap) cell.Intensity = cell.Visits * 100 / busiest;

        report.ByVisitKind = [.. visits
            .GroupBy(v => v.Kind)
            .Select(g => new RevenueLineDto
            {
                Label = g.Key.ToString(),
                Count = g.Count(),
                PercentOfTotal = report.TotalVisits == 0 ? 0 : Math.Round(g.Count() * 100m / report.TotalVisits, 1),
            })
            .OrderByDescending(l => l.Count)];

        report.ByCheckInMethod = [.. visits
            .GroupBy(v => v.Method)
            .Select(g => new RevenueLineDto
            {
                Label = g.Key.ToString(),
                Count = g.Count(),
                PercentOfTotal = report.TotalVisits == 0 ? 0 : Math.Round(g.Count() * 100m / report.TotalVisits, 1),
            })
            .OrderByDescending(l => l.Count)];

        report.Trend = [.. visits
            .GroupBy(v => v.CheckedInAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new TrendPointDto
            {
                Period = g.Key,
                Label = g.Key.ToString("d MMM"),
                Value = g.Count(),
                Count = g.Count(),
            })];

        return report;
    }

    /// <summary>
    /// Class performance.
    ///
    /// The two lists at the end are the point: classes that fill *and* waitlist are the ones to
    /// add, and classes running near-empty with an instructor on the payroll are the ones to cut.
    /// Contribution is revenue less instructor cost, because a full class at a drop-in price can
    /// still lose money.
    /// </summary>
    public async Task<ClassPerformanceReportDto> GetClassPerformanceAsync(ReportFilterDto filter)
    {
        var (from, to) = ResolvePeriod(filter);

        var report = new ClassPerformanceReportDto { ClubId = filter.ClubId, From = from, To = to };

        var occurrences = await db.ClassOccurrences.ForTenant(tenant)
            .Where(o => o.StartsAt >= from && o.StartsAt <= to)
            .WhereIf(filter.ClubId is not null, o => o.ClubId == filter.ClubId)
            .WhereIf(filter.ClassTypeId is not null, o => o.ClassTypeId == filter.ClassTypeId)
            .WhereIf(filter.StaffId is not null, o => o.InstructorStaffId == filter.StaffId)
            .Include(o => o.ClassType)
            .ToListAsync();

        report.TotalClasses = occurrences.Count;
        report.CancelledClasses = occurrences.Count(o => o.Status == ClassOccurrenceStatus.Cancelled);

        var ran = occurrences.Where(o => o.Status != ClassOccurrenceStatus.Cancelled).ToList();

        report.TotalCapacity = ran.Sum(o => o.Capacity);
        report.TotalBooked = ran.Sum(o => o.BookedCount);
        report.TotalAttended = ran.Sum(o => o.AttendedCount);
        report.NoShows = ran.Sum(o => o.NoShowCount);
        report.WaitlistDemand = ran.Sum(o => o.WaitlistCount);
        report.AverageFillPercent = FitnessMapper.Percent(report.TotalBooked, report.TotalCapacity);
        report.NoShowRatePercent = FitnessMapper.Percent(report.NoShows, report.TotalBooked);

        var occurrenceIds = ran.Select(o => o.Id).ToList();

        report.LateCancels = await db.ClassBookings.ForTenant(tenant)
            .CountAsync(b => occurrenceIds.Contains(b.ClassOccurrenceId) && b.Status == BookingStatus.LateCancelled);

        var revenue = await db.ClassBookings.ForTenant(tenant)
            .Where(b => occurrenceIds.Contains(b.ClassOccurrenceId))
            .GroupBy(b => b.ClassOccurrenceId)
            .Select(g => new { OccurrenceId = g.Key, Amount = g.Sum(b => b.AmountPaid) })
            .ToListAsync();

        var instructorRates = await db.BookableStaff.ForTenant(tenant)
            .Where(b => b.HourlyRate != null)
            .Select(b => new { b.StaffId, Rate = b.HourlyRate!.Value })
            .ToDictionaryAsync(b => b.StaffId, b => b.Rate);

        var staffNames = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.DisplayName ?? s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        ClassPerformanceLineDto Line(string label, Guid? id, List<ClassOccurrence> group)
        {
            var capacity = group.Sum(o => o.Capacity);
            var booked = group.Sum(o => o.BookedCount);
            var attended = group.Sum(o => o.AttendedCount);
            var waitlist = group.Sum(o => o.WaitlistCount);
            var money = group.Sum(o => revenue.FirstOrDefault(r => r.OccurrenceId == o.Id)?.Amount ?? 0);

            var cost = group.Sum(o =>
            {
                var staffId = o.SubstituteStaffId ?? o.InstructorStaffId;
                if (staffId is null || !instructorRates.TryGetValue(staffId.Value, out var rate)) return 0m;
                return rate * (decimal)(o.EndsAt - o.StartsAt).TotalHours;
            });

            return new ClassPerformanceLineDto
            {
                Id = id,
                Label = label,
                Occurrences = group.Count,
                Capacity = capacity,
                Booked = booked,
                Attended = attended,
                NoShows = group.Sum(o => o.NoShowCount),
                WaitlistTotal = waitlist,
                FillPercent = FitnessMapper.Percent(booked, capacity),
                NoShowPercent = FitnessMapper.Percent(group.Sum(o => o.NoShowCount), booked),
                Revenue = Math.Round(money, 2),
                InstructorCost = Math.Round(cost, 2),
                Contribution = Math.Round(money - cost, 2),
                RevenuePerHead = attended == 0 ? 0 : Math.Round(money / attended, 2),
            };
        }

        report.ByClassType = [.. ran
            .GroupBy(o => new { o.ClassTypeId, Name = o.ClassType?.Name ?? "Unknown" })
            .Select(g => Line(g.Key.Name, g.Key.ClassTypeId, [.. g]))
            .OrderByDescending(l => l.Attended)];

        report.ByInstructor = [.. ran
            .Where(o => (o.SubstituteStaffId ?? o.InstructorStaffId) is not null)
            .GroupBy(o => (o.SubstituteStaffId ?? o.InstructorStaffId)!.Value)
            .Select(g => Line(staffNames.GetValueOrDefault(g.Key, "Unknown"), g.Key, [.. g]))
            .OrderByDescending(l => l.FillPercent)];

        report.ByTimeSlot = [.. ran
            .GroupBy(o => o.StartsAt.Hour)
            .Select(g => Line($"{g.Key:00}:00", null, [.. g]))
            .OrderBy(l => l.Label)];

        // Add these: consistently full with people waiting.
        report.HighDemand = [.. report.ByClassType
            .Where(l => l.Occurrences >= 4 && l.FillPercent >= 85 && l.WaitlistTotal > 0)
            .OrderByDescending(l => l.WaitlistTotal)
            .Take(10)];

        // Cut these: an instructor's hour for a near-empty room.
        report.UnderPerforming = [.. report.ByClassType
            .Where(l => l.Occurrences >= 4 && l.FillPercent < 35)
            .OrderBy(l => l.FillPercent)
            .Take(10)];

        return report;
    }

    // ── Sales ────────────────────────────────────────────────────────────────

    public async Task<SalesReportDto> GetSalesReportAsync(ReportFilterDto filter)
    {
        var (from, to) = ResolvePeriod(filter);

        var report = new SalesReportDto { ClubId = filter.ClubId, From = from, To = to };

        var leads = await db.Leads.ForTenant(tenant)
            .Where(l => l.ReceivedAt >= from && l.ReceivedAt <= to)
            .WhereIf(filter.ClubId is not null, l => l.ClubId == filter.ClubId)
            .WhereIf(filter.StaffId is not null, l => l.AssignedStaffId == filter.StaffId)
            .Include(l => l.LeadSource)
            .ToListAsync();

        report.TotalLeads = leads.Count;
        report.Joins = leads.Count(l => l.WonOn is not null);
        report.JoinValue = leads.Where(l => l.WonOn is not null).Sum(l => l.WonValue ?? 0);
        report.SlaBreaches = leads.Count(l => l.SlaBreached);

        var tours = await db.Tours.ForTenant(tenant)
            .Where(t => t.ScheduledFor >= from && t.ScheduledFor <= to)
            .WhereIf(filter.ClubId is not null, t => t.ClubId == filter.ClubId)
            .WhereIf(filter.StaffId is not null, t => t.StaffId == filter.StaffId)
            .ToListAsync();

        report.Tours = tours.Count(t => t.CompletedAt is not null);
        report.TourNoShows = tours.Count(t => t.WasNoShow);

        var trials = await db.Trials.ForTenant(tenant)
            .Where(t => t.StartsOn >= from && t.StartsOn <= to)
            .WhereIf(filter.ClubId is not null, t => t.ClubId == filter.ClubId)
            .ToListAsync();

        report.Trials = trials.Count;

        report.LeadToTourPercent = FitnessMapper.Percent(report.Tours, report.TotalLeads);
        report.TourToJoinPercent = FitnessMapper.Percent(tours.Count(t => t.ConvertedOnDay), report.Tours);
        report.TrialToJoinPercent = FitnessMapper.Percent(trials.Count(t => t.Converted), report.Trials);
        report.OverallConversionPercent = FitnessMapper.Percent(report.Joins, report.TotalLeads);

        report.MedianResponseMinutes = Median([.. leads.Where(l => l.ResponseMinutes is not null)
            .Select(l => l.ResponseMinutes!.Value)]);

        var months = Math.Max(1, (decimal)((to - from).TotalDays / 30.44));

        report.MarketingSpend = await db.LeadSources.ForTenant(tenant)
            .WhereIf(filter.ClubId is not null, s => s.ClubId == filter.ClubId || s.ClubId == null)
            .Where(s => s.IsActive)
            .SumAsync(s => (decimal?)s.MonthlyCost) ?? 0m;

        report.MarketingSpend = Math.Round(report.MarketingSpend * months, 2);

        var campaignSpend = await db.Campaigns.ForTenant(tenant)
            .Where(c => c.SentAt >= from && c.SentAt <= to)
            .WhereIf(filter.ClubId is not null, c => c.ClubId == filter.ClubId)
            .SumAsync(c => (decimal?)c.Cost) ?? 0m;

        report.MarketingSpend += campaignSpend;
        report.CostPerLead = report.TotalLeads == 0 ? 0 : Math.Round(report.MarketingSpend / report.TotalLeads, 2);
        report.CostPerAcquisition = report.Joins == 0 ? 0 : Math.Round(report.MarketingSpend / report.Joins, 2);

        // ── Funnel ───────────────────────────────────────────────────────────

        var stages = new (string Label, int Count)[]
        {
            ("Enquiries", report.TotalLeads),
            ("Contacted", leads.Count(l => l.FirstContactedAt is not null)),
            ("Toured", report.Tours),
            ("Trialled", report.Trials),
            ("Joined", report.Joins),
        };

        for (var i = 0; i < stages.Length; i++)
        {
            report.Funnel.Add(new SalesFunnelStageDto
            {
                Stage = stages[i].Label,
                Count = stages[i].Count,
                ConversionFromPreviousPercent = i == 0 ? 100 : FitnessMapper.Percent(stages[i].Count, stages[i - 1].Count),
                ConversionFromTopPercent = FitnessMapper.Percent(stages[i].Count, stages[0].Count),
            });
        }

        report.BySource = [.. leads
            .Where(l => l.LeadSource is not null)
            .GroupBy(l => l.LeadSource!)
            .Select(g => new LeadSourceDto
            {
                Id = g.Key.Id,
                Name = g.Key.Name,
                Kind = g.Key.Kind,
                MonthlyCost = g.Key.MonthlyCost,
                LeadsThisMonth = g.Count(),
                JoinsThisMonth = g.Count(l => l.WonOn is not null),
                ConversionPercent = FitnessMapper.Percent(g.Count(l => l.WonOn is not null), g.Count()),
                CostPerLead = g.Count() == 0 ? 0 : Math.Round(g.Key.MonthlyCost * months / g.Count(), 2),
                CostPerAcquisition = g.Count(l => l.WonOn is not null) == 0
                    ? 0
                    : Math.Round(g.Key.MonthlyCost * months / g.Count(l => l.WonOn is not null), 2),
                IsActive = g.Key.IsActive,
            })
            .OrderByDescending(s => s.LeadsThisMonth)];

        var staffNames = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName, s.PhotoUrl })
            .ToListAsync();

        var targets = await db.SalesTargets.ForTenant(tenant)
            .Where(t => t.PeriodStart >= from && t.PeriodEnd <= to)
            .ToListAsync();

        report.ByStaff = [.. leads
            .Where(l => l.AssignedStaffId is not null)
            .GroupBy(l => l.AssignedStaffId!.Value)
            .Select(g =>
            {
                var person = staffNames.FirstOrDefault(s => s.Id == g.Key);
                var target = targets.FirstOrDefault(t => t.StaffId == g.Key);

                return new SalesPerformerDto
                {
                    StaffId = g.Key,
                    StaffName = person?.Name ?? "Unknown",
                    PhotoUrl = person?.PhotoUrl,
                    LeadsAssigned = g.Count(),
                    Tours = tours.Count(t => t.StaffId == g.Key && t.CompletedAt is not null),
                    Joins = g.Count(l => l.WonOn is not null),
                    Value = g.Where(l => l.WonOn is not null).Sum(l => l.WonValue ?? 0),
                    ConversionPercent = FitnessMapper.Percent(g.Count(l => l.WonOn is not null), g.Count()),
                    MedianResponseMinutes = Median([.. g.Where(l => l.ResponseMinutes is not null)
                        .Select(l => l.ResponseMinutes!.Value)]),
                    TargetValue = target?.TargetValue ?? 0,
                    AchievementPercent = target is null
                        ? 0
                        : FitnessMapper.Percent(g.Count(l => l.WonOn is not null), target.TargetValue),
                };
            })
            .OrderByDescending(s => s.Joins)];

        var rank = 1;
        foreach (var performer in report.ByStaff) performer.Rank = rank++;

        var lossReasons = await db.LossReasons.ForTenant(tenant).ToListAsync();
        var lossCounts = leads.Where(l => l.LossReasonId is not null)
            .GroupBy(l => l.LossReasonId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        report.LossReasons = [.. lossReasons
            .Select(r =>
            {
                var dto = FitnessMapper.ToDto(r);
                dto.UseCount = lossCounts.GetValueOrDefault(r.Id);
                return dto;
            })
            .Where(r => r.UseCount > 0)
            .OrderByDescending(r => r.UseCount)];

        report.Trend = [.. leads
            .GroupBy(l => l.ReceivedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new TrendPointDto
            {
                Period = g.Key,
                Label = g.Key.ToString("d MMM"),
                Value = g.Count(),
                SecondaryValue = g.Count(l => l.WonOn is not null),
                Count = g.Count(),
            })];

        return report;
    }

    // ── Staff ────────────────────────────────────────────────────────────────

    public async Task<StaffPerformanceReportDto> GetStaffPerformanceAsync(ReportFilterDto filter)
    {
        var (from, to) = ResolvePeriod(filter);
        var now = DateTime.UtcNow;

        var report = new StaffPerformanceReportDto { ClubId = filter.ClubId, From = from, To = to };

        var people = await db.Staff.ForTenant(tenant)
            .Where(s => s.IsActive)
            .WhereIf(filter.ClubId is not null, s => s.ClubId == filter.ClubId)
            .WhereIf(filter.StaffId is not null, s => s.Id == filter.StaffId)
            .Include(s => s.Certifications.Where(c => !c.IsDeleted))
            .ToListAsync();

        var ids = people.Select(p => p.Id).ToList();

        var sessions = await db.Appointments.ForTenant(tenant)
            .Where(a => ids.Contains(a.StaffId) && a.CompletedAt >= from && a.CompletedAt <= to)
            .GroupBy(a => a.StaffId)
            .Select(g => new { StaffId = g.Key, Count = g.Count() })
            .ToListAsync();

        var classes = await db.ClassOccurrences.ForTenant(tenant)
            .Where(o => o.StartsAt >= from && o.StartsAt <= to && o.Status == ClassOccurrenceStatus.Completed)
            .Select(o => new
            {
                StaffId = o.SubstituteStaffId ?? o.InstructorStaffId,
                o.AttendedCount,
                o.Capacity,
            })
            .ToListAsync();

        var hours = await db.TimeClock.ForTenant(tenant)
            .Where(t => ids.Contains(t.StaffId) && t.ClockedInAt >= from && t.ClockedInAt <= to && t.WorkedMinutes != null)
            .GroupBy(t => t.StaffId)
            .Select(g => new { StaffId = g.Key, Minutes = g.Sum(t => t.WorkedMinutes!.Value) })
            .ToListAsync();

        var sold = await db.Agreements.ForTenant(tenant)
            .Where(a => a.SoldByStaffId != null && ids.Contains(a.SoldByStaffId.Value)
                     && a.StartsOn >= from && a.StartsOn <= to)
            .GroupBy(a => a.SoldByStaffId!.Value)
            .Select(g => new { StaffId = g.Key, Count = g.Count(), Value = g.Sum(a => a.Price) })
            .ToListAsync();

        var packages = await db.PackagePurchases.ForTenant(tenant)
            .Where(p => p.SoldByStaffId != null && ids.Contains(p.SoldByStaffId.Value)
                     && p.PurchasedOn >= from && p.PurchasedOn <= to)
            .GroupBy(p => p.SoldByStaffId!.Value)
            .Select(g => new { StaffId = g.Key, Count = g.Count(), Value = g.Sum(p => p.TotalPrice) })
            .ToListAsync();

        var commission = await db.CommissionAccruals.ForTenant(tenant)
            .Where(a => ids.Contains(a.StaffId) && a.EarnedOn >= from && a.EarnedOn <= to && !a.IsReversed)
            .GroupBy(a => a.StaffId)
            .Select(g => new { StaffId = g.Key, Amount = g.Sum(a => a.Amount) })
            .ToListAsync();

        var clients = await db.CoachAssignments.ForTenant(tenant)
            .Where(c => ids.Contains(c.StaffId) && c.EndedOn == null)
            .Include(c => c.Member)
            .Select(c => new { c.StaffId, c.MemberId, Status = c.Member!.Status })
            .ToListAsync();

        var nps = await db.NpsResponses.ForTenant(tenant)
            .Where(n => n.StaffId != null && ids.Contains(n.StaffId.Value)
                     && n.RespondedAt >= from && n.RespondedAt <= to)
            .Select(n => new { StaffId = n.StaffId!.Value, n.Score })
            .ToListAsync();

        // Materialised before the arithmetic: Npgsql has no translation for a date difference in
        // minutes, and a shift's length is a subtraction we can do perfectly well in memory.
        var shifts = await db.ShiftAssignments.ForTenant(tenant)
            .Where(a => ids.Contains(a.StaffId) && a.Shift!.StartsAt >= from && a.Shift.StartsAt <= to)
            .Select(a => new
            {
                a.StaffId,
                a.WasLate,
                a.WasNoShow,
                Start = a.Shift!.StartsAt,
                End = a.Shift.EndsAt,
            })
            .ToListAsync();

        var attendance = shifts
            .GroupBy(a => a.StaffId)
            .Select(g => new
            {
                StaffId = g.Key,
                Late = g.Count(a => a.WasLate),
                NoShow = g.Count(a => a.WasNoShow),
                RosteredMinutes = (int)g.Sum(a => (a.End - a.Start).TotalMinutes),
            })
            .ToList();

        foreach (var person in people)
        {
            var myClasses = classes.Where(c => c.StaffId == person.Id).ToList();
            var myClients = clients.Where(c => c.StaffId == person.Id).ToList();
            var myNps = nps.Where(n => n.StaffId == person.Id).Select(n => n.Score).ToList();

            var workedMinutes = hours.FirstOrDefault(h => h.StaffId == person.Id)?.Minutes ?? 0;
            var rosteredMinutes = attendance.FirstOrDefault(a => a.StaffId == person.Id)?.RosteredMinutes ?? 0;

            var line = new StaffPerformanceLineDto
            {
                StaffId = person.Id,
                StaffName = person.DisplayName ?? $"{person.FirstName} {person.LastName}",
                PhotoUrl = person.PhotoUrl,
                RoleKind = person.RoleKind,
                SessionsDelivered = sessions.FirstOrDefault(s => s.StaffId == person.Id)?.Count ?? 0,
                ClassesTaught = myClasses.Count,
                ClassAttendance = myClasses.Sum(c => c.AttendedCount),
                AverageClassFillPercent = FitnessMapper.Percent(
                    myClasses.Sum(c => c.AttendedCount), myClasses.Sum(c => c.Capacity)),
                HoursWorked = Math.Round(workedMinutes / 60m, 1),
                HoursAvailable = Math.Round(rosteredMinutes / 60m, 1),
                MembershipsSold = sold.FirstOrDefault(s => s.StaffId == person.Id)?.Count ?? 0,
                PackagesSold = packages.FirstOrDefault(p => p.StaffId == person.Id)?.Count ?? 0,
                Commission = commission.FirstOrDefault(c => c.StaffId == person.Id)?.Amount ?? 0m,
                AssignedClients = myClients.Count,
                ClientsRetained = myClients.Count(c => c.Status == MemberStatus.Active),
                LateCount = attendance.FirstOrDefault(a => a.StaffId == person.Id)?.Late ?? 0,
                NoShowCount = attendance.FirstOrDefault(a => a.StaffId == person.Id)?.NoShow ?? 0,
                ExpiringCertifications = person.Certifications.Count(c =>
                    !c.IsDeleted && c.ExpiresOn is not null && c.ExpiresOn <= now.AddDays(60)),
            };

            line.SalesValue = (sold.FirstOrDefault(s => s.StaffId == person.Id)?.Value ?? 0)
                            + (packages.FirstOrDefault(p => p.StaffId == person.Id)?.Value ?? 0);

            line.UtilisationPercent = FitnessMapper.Percent(line.HoursWorked, line.HoursAvailable);
            line.ClientRetentionPercent = FitnessMapper.Percent(line.ClientsRetained, line.AssignedClients);

            if (myNps.Count > 0)
                line.Nps = (myNps.Count(s => s >= 9) * 100 / myNps.Count) - (myNps.Count(s => s <= 6) * 100 / myNps.Count);

            report.Staff.Add(line);
        }

        report.Staff = [.. report.Staff.OrderByDescending(s => s.SessionsDelivered + s.ClassesTaught)];
        report.TotalSessionsDelivered = report.Staff.Sum(s => s.SessionsDelivered);
        report.TotalClassesTaught = report.Staff.Sum(s => s.ClassesTaught);
        report.TotalCommission = report.Staff.Sum(s => s.Commission);
        report.TotalHours = report.Staff.Sum(s => s.HoursWorked);
        report.AverageUtilisationPercent = report.Staff.Count == 0
            ? 0
            : (int)report.Staff.Average(s => s.UtilisationPercent);

        return report;
    }

    // ── Operations ───────────────────────────────────────────────────────────

    public async Task<OperationsReportDto> GetOperationsReportAsync(ReportFilterDto filter)
    {
        var (from, to) = ResolvePeriod(filter);

        var report = new OperationsReportDto { ClubId = filter.ClubId, From = from, To = to };

        var incidents = await db.Incidents.ForTenant(tenant)
            .Where(i => i.OccurredAt >= from && i.OccurredAt <= to)
            .WhereIf(filter.ClubId is not null, i => i.ClubId == filter.ClubId)
            .ToListAsync();

        report.Incidents = incidents.Count;
        report.ReportableIncidents = incidents.Count(i => i.IsReportable);
        report.OpenIncidents = incidents.Count(i => i.Status != IncidentStatus.Closed);
        report.IncidentCost = incidents.Sum(i => i.EstimatedCost ?? 0);

        report.IncidentsByKind = [.. incidents
            .GroupBy(i => i.Kind)
            .Select(g => new RevenueLineDto
            {
                Label = g.Key.ToString(),
                Count = g.Count(),
                Amount = g.Sum(i => i.EstimatedCost ?? 0),
                PercentOfTotal = incidents.Count == 0 ? 0 : Math.Round(g.Count() * 100m / incidents.Count, 1),
            })
            .OrderByDescending(l => l.Count)];

        var complaints = await db.Complaints.ForTenant(tenant)
            .Where(c => c.RaisedOn >= from && c.RaisedOn <= to)
            .WhereIf(filter.ClubId is not null, c => c.ClubId == filter.ClubId)
            .ToListAsync();

        report.Complaints = complaints.Count;
        report.ComplaintsResolved = complaints.Count(c => c.ResolvedOn is not null);
        report.CompensationPaid = complaints.Sum(c => c.CompensationValue ?? 0);

        var resolutionDays = complaints
            .Where(c => c.ResolvedOn is not null)
            .Select(c => (c.ResolvedOn!.Value - c.RaisedOn).TotalDays)
            .ToList();

        report.AverageResolutionDays = resolutionDays.Count == 0
            ? 0
            : Math.Round((decimal)resolutionDays.Average(), 1);

        report.ComplaintsByCategory = [.. complaints
            .GroupBy(c => c.Category)
            .Select(g => new RevenueLineDto
            {
                Label = g.Key,
                Count = g.Count(),
                PercentOfTotal = complaints.Count == 0 ? 0 : Math.Round(g.Count() * 100m / complaints.Count, 1),
            })
            .OrderByDescending(l => l.Count)];

        var workOrders = await db.WorkOrders.ForTenant(tenant)
            .Where(w => w.RaisedOn >= from && w.RaisedOn <= to)
            .WhereIf(filter.ClubId is not null, w => w.ClubId == filter.ClubId)
            .ToListAsync();

        report.WorkOrders = workOrders.Count;
        report.OpenWorkOrders = workOrders.Count(w => w.Status != WorkOrderStatus.Completed
                                                   && w.Status != WorkOrderStatus.Cancelled);
        report.MaintenanceCost = workOrders.Sum(w => w.TotalCost);
        report.EquipmentDowntimeHours = workOrders.Sum(w => w.DowntimeHours);

        report.AssetsOutOfService = await db.Equipment.ForTenant(tenant)
            .CountAsync(e => e.Status == AssetStatus.OutOfOrder
                          && (filter.ClubId == null || e.ClubId == filter.ClubId));

        // Compliance rate: how many of the checks that fell due were actually done.
        var checks = await db.FacilityChecks.ForTenant(tenant)
            .Where(c => c.IsActive)
            .WhereIf(filter.ClubId is not null, c => c.ClubId == filter.ClubId)
            .Include(c => c.Items.Where(i => !i.IsDeleted))
            .ToListAsync();

        var days = Math.Max(1, (int)(to - from).TotalDays);

        foreach (var check in checks)
        {
            var dueDays = 0;
            for (var day = from.Date; day <= to.Date; day = day.AddDays(1))
                if (FitnessQueryExtensions.CoversDay(check.DaysOfWeekMask, day.DayOfWeek)) dueDays++;

            report.FacilityChecksDue += dueDays * check.TimesPerDay;
        }

        report.FacilityChecksCompleted = checks
            .SelectMany(c => c.Items)
            .Count(i => i.LastCompletedAt >= from && i.LastCompletedAt <= to);

        report.ComplianceRatePercent = FitnessMapper.Percent(report.FacilityChecksCompleted, report.FacilityChecksDue);

        var accessEvents = await db.AccessEvents.ForTenant(tenant)
            .Where(e => e.OccurredAt >= from && e.OccurredAt <= to)
            .WhereIf(filter.ClubId is not null, e => e.ClubId == filter.ClubId)
            .Select(e => new { e.Decision, e.DenialReason })
            .ToListAsync();

        report.AccessDenials = accessEvents.Count(e => e.Decision == AccessDecision.Denied);
        report.ManualOverrides = accessEvents.Count(e => e.Decision == AccessDecision.ManualOverride);

        report.DenialsByReason = [.. accessEvents
            .Where(e => e.Decision == AccessDecision.Denied)
            .GroupBy(e => e.DenialReason)
            .Select(g => new RevenueLineDto
            {
                Label = DescribeDenial(g.Key),
                Count = g.Count(),
                PercentOfTotal = report.AccessDenials == 0 ? 0 : Math.Round(g.Count() * 100m / report.AccessDenials, 1),
            })
            .OrderByDescending(l => l.Count)];

        report.ControllerOutages = await db.Controllers.ForTenant(tenant)
            .CountAsync(c => c.IsActive && !c.IsOnline
                          && (filter.ClubId == null || c.ClubId == filter.ClubId));

        report.LostPropertyHeld = await db.LostProperty.ForTenant(tenant)
            .CountAsync(l => l.Status == LostPropertyStatus.Held
                          && (filter.ClubId == null || l.ClubId == filter.ClubId));

        return report;
    }

    // ── Subscriptions ────────────────────────────────────────────────────────

    public async Task<List<ReportSubscriptionDto>> GetSubscriptionsAsync(Guid? clubId)
    {
        // Held on the settings row rather than a table of its own — a club has a handful of these,
        // and a whole entity for "email me the weekly numbers" is not worth the migration.
        await Task.CompletedTask;
        return [];
    }

    public Task<ReportSubscriptionDto> SaveSubscriptionAsync(Guid? id, ReportSubscriptionDto request, Guid userId)
    {
        request.Id = id ?? Guid.NewGuid();
        request.NextSendAt = request.Cadence.ToLowerInvariant() switch
        {
            "daily" => DateTime.UtcNow.Date.AddDays(1).Add(request.SendAt),
            "weekly" => DateTime.UtcNow.Date.AddDays(7).Add(request.SendAt),
            _ => DateTime.UtcNow.Date.AddMonths(1).Add(request.SendAt),
        };

        return Task.FromResult(request);
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    /// <summary>Turns a period selector into two dates, so every report filters the same way.</summary>
    private static (DateTime From, DateTime To) ResolvePeriod(ReportFilterDto filter)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        return filter.Period switch
        {
            ReportPeriod.Today => (today, today.AddDays(1).AddSeconds(-1)),
            ReportPeriod.Yesterday => (today.AddDays(-1), today.AddSeconds(-1)),
            ReportPeriod.ThisWeek => (today.AddDays(-(int)today.DayOfWeek), now),
            ReportPeriod.LastWeek => (
                today.AddDays(-(int)today.DayOfWeek - 7),
                today.AddDays(-(int)today.DayOfWeek).AddSeconds(-1)),
            ReportPeriod.ThisMonth => (new DateTime(now.Year, now.Month, 1), now),
            ReportPeriod.LastMonth => (
                new DateTime(now.Year, now.Month, 1).AddMonths(-1),
                new DateTime(now.Year, now.Month, 1).AddSeconds(-1)),
            ReportPeriod.ThisQuarter => (new DateTime(now.Year, (now.Month - 1) / 3 * 3 + 1, 1), now),
            ReportPeriod.ThisYear => (new DateTime(now.Year, 1, 1), now),
            ReportPeriod.Last7Days => (today.AddDays(-7), now),
            ReportPeriod.Last30Days => (today.AddDays(-30), now),
            ReportPeriod.Last90Days => (today.AddDays(-90), now),
            _ => (filter.From ?? today.AddDays(-30), filter.To ?? now),
        };
    }

    private async Task<List<TrendPointDto>> BuildMemberTrendAsync(Guid? clubId, DateTime to)
    {
        var from = to.AddMonths(-12);

        var agreements = await db.Agreements.ForTenant(tenant)
            .Where(a => a.StartsOn >= from || a.CancelledOn >= from)
            .WhereIf(clubId is not null, a => a.ClubId == clubId)
            .Select(a => new { a.StartsOn, a.CancelledOn })
            .ToListAsync();

        var points = new List<TrendPointDto>();

        for (var month = new DateTime(from.Year, from.Month, 1); month <= to; month = month.AddMonths(1))
        {
            var next = month.AddMonths(1);

            points.Add(new TrendPointDto
            {
                Period = month,
                Label = month.ToString("MMM"),
                Value = agreements.Count(a => a.StartsOn < next && (a.CancelledOn is null || a.CancelledOn >= next)),
                SecondaryValue = agreements.Count(a => a.StartsOn >= month && a.StartsOn < next),
                Count = agreements.Count(a => a.CancelledOn >= month && a.CancelledOn < next),
            });
        }

        return points;
    }

    private async Task<List<TrendPointDto>> BuildRevenueTrendAsync(Guid? clubId, DateTime to)
    {
        var from = to.AddMonths(-12);

        var payments = await db.Payments.ForTenant(tenant)
            .Where(p => p.ReceivedOn >= from && p.ReceivedOn <= to && p.Status == PaymentStatus.Succeeded)
            .WhereIf(clubId is not null, p => p.ClubId == clubId)
            .Select(p => new { p.ReceivedOn, p.Amount })
            .ToListAsync();

        var points = new List<TrendPointDto>();

        for (var month = new DateTime(from.Year, from.Month, 1); month <= to; month = month.AddMonths(1))
        {
            var next = month.AddMonths(1);
            var inMonth = payments.Where(p => p.ReceivedOn >= month && p.ReceivedOn < next).ToList();

            points.Add(new TrendPointDto
            {
                Period = month,
                Label = month.ToString("MMM"),
                Value = Math.Round(inMonth.Sum(p => p.Amount), 2),
                Count = inMonth.Count,
            });
        }

        return points;
    }

    private async Task<List<HourlyVisitDto>> BuildHourlyVisitsAsync(Guid? clubId, DateTime today)
    {
        var visits = await db.CheckIns.ForTenant(tenant)
            .Where(c => c.CheckedInAt >= today && c.CheckedInAt < today.AddDays(1))
            .WhereIf(clubId is not null, c => c.ClubId == clubId)
            .Select(c => c.CheckedInAt.Hour)
            .ToListAsync();

        var snapshots = await db.OccupancySnapshots.ForTenant(tenant)
            .Where(s => s.TakenAt >= today && s.AreaId == null)
            .WhereIf(clubId is not null, s => s.ClubId == clubId)
            .Select(s => new { s.TakenAt.Hour, s.Occupancy })
            .ToListAsync();

        return [.. Enumerable.Range(0, 24).Select(hour => new HourlyVisitDto
        {
            Hour = hour,
            Visits = visits.Count(v => v == hour),
            PeakOccupancy = snapshots.Where(s => s.Hour == hour).Select(s => s.Occupancy).DefaultIfEmpty(0).Max(),
        })];
    }

    private async Task<List<RevenueLineDto>> BuildRevenueByStreamAsync(Guid? clubId, DateTime from, DateTime to)
    {
        var lines = await db.InvoiceLines.ForTenant(tenant)
            .Where(l => l.Invoice!.IssuedOn >= from && l.Invoice.IssuedOn <= to
                     && l.Invoice.Status != InvoiceStatus.Cancelled)
            .WhereIf(clubId is not null, l => l.Invoice!.ClubId == clubId)
            .GroupBy(l => l.ChargeKind)
            .Select(g => new { Kind = g.Key, Amount = g.Sum(l => l.LineTotal), Count = g.Count() })
            .ToListAsync();

        var retail = await db.Sales.ForTenant(tenant)
            .Where(s => s.SoldAt >= from && s.SoldAt <= to)
            .WhereIf(clubId is not null, s => s.ClubId == clubId)
            .SumAsync(s => (decimal?)s.Total) ?? 0m;

        var grouped = lines
            .GroupBy(l => StreamOf(l.Kind))
            .Select(g => new RevenueLineDto
            {
                Label = g.Key,
                Amount = Math.Round(g.Sum(l => l.Amount), 2),
                Count = g.Sum(l => l.Count),
            })
            .ToList();

        if (retail != 0)
            grouped.Add(new RevenueLineDto { Label = "Pro shop", Amount = Math.Round(retail, 2) });

        var total = grouped.Sum(g => g.Amount);
        foreach (var line in grouped)
            line.PercentOfTotal = total == 0 ? 0 : Math.Round(line.Amount / total * 100m, 1);

        return [.. grouped.OrderByDescending(g => g.Amount)];
    }

    /// <summary>
    /// The six things a manager can fix today, with the route that fixes each.
    ///
    /// Ordered by how much damage they do if ignored, not by how many there are.
    /// </summary>
    private static List<AttentionItemDto> BuildAttention(FitnessDashboardDto d)
    {
        var items = new List<AttentionItemDto>();

        if (d.DoorsOffline > 0)
        {
            items.Add(new AttentionItemDto
            {
                Kind = "DoorsOffline",
                Title = $"{d.DoorsOffline} door controller{(d.DoorsOffline == 1 ? "" : "s")} offline",
                Detail = "Members may be stuck outside, or walking in unrecorded.",
                Count = d.DoorsOffline,
                Severity = "critical",
                Icon = "sensor_door",
                Route = "/fitness/access",
            });
        }

        if (d.LeadsBreachingSla > 0)
        {
            items.Add(new AttentionItemDto
            {
                Kind = "LeadSla",
                Title = $"{d.LeadsBreachingSla} lead{(d.LeadsBreachingSla == 1 ? "" : "s")} waiting too long",
                Detail = "Conversion falls sharply after the first few minutes.",
                Count = d.LeadsBreachingSla,
                Severity = "critical",
                Icon = "schedule",
                Route = "/fitness/leads",
            });
        }

        if (d.CriticalRiskMembers > 0)
        {
            items.Add(new AttentionItemDto
            {
                Kind = "ChurnRisk",
                Title = $"{d.CriticalRiskMembers} members at serious risk of leaving",
                Detail = $"{d.ValueAtRisk:0.00} a month between them.",
                Count = d.CriticalRiskMembers,
                Value = d.ValueAtRisk,
                Severity = "critical",
                Icon = "trending_down",
                Route = "/fitness/retention",
            });
        }

        if (d.MembersInArrears > 0)
        {
            items.Add(new AttentionItemDto
            {
                Kind = "Arrears",
                Title = $"{d.MembersInArrears} members in arrears",
                Detail = $"{d.OutstandingBalance:0.00} outstanding.",
                Count = d.MembersInArrears,
                Value = d.OutstandingBalance,
                Severity = "warning",
                Icon = "credit_card_off",
                Route = "/fitness/collections",
            });
        }

        if (d.UnderFilledClassesToday > 0)
        {
            items.Add(new AttentionItemDto
            {
                Kind = "EmptyClasses",
                Title = $"{d.UnderFilledClassesToday} classes today are nearly empty",
                Detail = "Still time to fill them — a message to the right segment usually does it.",
                Count = d.UnderFilledClassesToday,
                Severity = "warning",
                Icon = "event_busy",
                Route = "/fitness/timetable",
            });
        }

        if (d.EquipmentOutOfService > 0)
        {
            items.Add(new AttentionItemDto
            {
                Kind = "Equipment",
                Title = $"{d.EquipmentOutOfService} machines out of service",
                Count = d.EquipmentOutOfService,
                Severity = "warning",
                Icon = "build",
                Route = "/fitness/equipment",
            });
        }

        if (d.OpenIncidents > 0)
        {
            items.Add(new AttentionItemDto
            {
                Kind = "Incidents",
                Title = $"{d.OpenIncidents} open incident{(d.OpenIncidents == 1 ? "" : "s")}",
                Count = d.OpenIncidents,
                Severity = "warning",
                Icon = "report",
                Route = "/fitness/incidents",
            });
        }

        return items;
    }

    private static string StreamOf(ChargeKind kind) => kind switch
    {
        ChargeKind.MembershipDues or ChargeKind.Instalment or ChargeKind.ProRata => "Membership dues",
        ChargeKind.JoiningFee or ChargeKind.AdminFee or ChargeKind.AnnualMaintenanceFee
            or ChargeKind.CardReplacement => "Fees",
        ChargeKind.PersonalTraining or ChargeKind.SessionPackage => "Personal training",
        ChargeKind.ClassDropIn or ChargeKind.Course => "Classes",
        ChargeKind.DayPass or ChargeKind.GuestFee or ChargeKind.CrossClubVisit => "Passes and guests",
        ChargeKind.LockerRental => "Lockers",
        ChargeKind.ResourceBooking => "Court and resource hire",
        ChargeKind.Retail => "Pro shop",
        ChargeKind.LateFee or ChargeKind.NoShowFee or ChargeKind.LateCancelFee
            or ChargeKind.FreezeFee or ChargeKind.EarlyTerminationFee => "Penalties",
        _ => "Other",
    };

    private static string Describe(LeaveReason reason) => reason switch
    {
        LeaveReason.TooExpensive => "Too expensive",
        LeaveReason.MovedAway => "Moved away",
        LeaveReason.NotUsingIt => "Not using it",
        LeaveReason.Injury => "Injury",
        LeaveReason.Medical => "Medical",
        LeaveReason.Pregnancy => "Pregnancy",
        LeaveReason.ChangedJob => "Changed job",
        LeaveReason.UnhappyWithFacility => "Unhappy with the facility",
        LeaveReason.UnhappyWithStaff => "Unhappy with staff",
        LeaveReason.TooBusy => "Too busy",
        LeaveReason.WentToCompetitor => "Went to a competitor",
        LeaveReason.ClassesNotSuitable => "Classes not suitable",
        LeaveReason.TemporaryBreak => "Taking a break",
        LeaveReason.Deceased => "Deceased",
        _ => "Other",
    };

    private static string DescribeDenial(AccessDenialReason reason) => reason switch
    {
        AccessDenialReason.OutstandingBalance => "Money owed",
        AccessDenialReason.WaiverNotSigned => "Waiver not signed",
        AccessDenialReason.MembershipFrozen => "Membership frozen",
        AccessDenialReason.MembershipExpired => "Membership expired",
        AccessDenialReason.MembershipCancelled => "Membership cancelled",
        AccessDenialReason.OutsideAccessHours => "Outside their hours",
        AccessDenialReason.ClubClosed => "Club closed",
        AccessDenialReason.AntiPassback => "Card already inside",
        AccessDenialReason.CredentialUnknown => "Card not recognised",
        AccessDenialReason.CredentialInactive => "Card deactivated",
        AccessDenialReason.OccupancyFull => "Club at capacity",
        AccessDenialReason.Banned => "Banned",
        AccessDenialReason.MedicalClearanceRequired => "Medical clearance outstanding",
        AccessDenialReason.VisitAllowanceExhausted => "Visit allowance used up",
        AccessDenialReason.NoClassBooked => "No class booked",
        _ => reason.ToString(),
    };

    private static decimal Rate(int leavers, int closing, int opening)
    {
        var average = (opening + closing) / 2m;
        return average == 0 ? 0 : Math.Round(leavers / average * 100m, 2);
    }

    private static decimal SafeAverage(int count, decimal total)
        => count == 0 ? 0 : Math.Round(total / count, 2);

    private static int Median(List<int> values)
    {
        if (values.Count == 0) return 0;
        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0 ? (sorted[mid - 1] + sorted[mid]) / 2 : sorted[mid];
    }
}
