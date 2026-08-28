using Microsoft.EntityFrameworkCore;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// The dashboard, the attention list and the report catalogue.
///
/// A dashboard that shows everything shows nothing. This one is built around a single question —
/// what would go wrong today if nobody looked? — so the top of it is not a set of totals but a
/// ranked list of things that need a person. The totals come after, and only the ones that belong
/// to the lines of business this company actually runs: a letting agency never sees escrow, and a
/// plot developer never sees a rent roll.
/// </summary>
public partial class RealEstateReportService(
    RealEstateDbContext db,
    IRealEstateTenant tenant)
    : RealEstateServiceBase(db, tenant), IRealEstateReportService
{
    public async Task<RealEstateDashboardDto> GetDashboardAsync(Guid? officeId, Guid? projectId)
    {
        var settings = await SettingsAsync();
        var currency = await CurrencyAsync();

        var now = DateTime.UtcNow;
        var today = Today;
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var lastMonthStart = monthStart.AddMonths(-1);

        var dto = new RealEstateDashboardDto
        {
            GeneratedAt = now,
            CurrencyCode = currency,
            BrokerageEnabled = settings.BrokerageEnabled,
            DevelopmentEnabled = settings.DevelopmentEnabled,
            ContractingEnabled = settings.ContractingEnabled,
            EstateManagementEnabled = settings.EstateManagementEnabled,
        };

        if (officeId is not null)
        {
            dto.OfficeName = await Db.RealEstateOffices.ForCompany(Tenant)
                .Where(o => o.Id == officeId)
                .Select(o => o.Name)
                .FirstOrDefaultAsync();
        }

        await FillRightNowAsync(dto, officeId, projectId, now, today);

        if (settings.DevelopmentEnabled)
        {
            await FillDevelopmentAsync(dto, projectId, monthStart, lastMonthStart, today);
            await FillMoneyAsync(dto, projectId, monthStart, today);
        }

        if (settings.BrokerageEnabled) await FillBrokerageAsync(dto, officeId, monthStart, today);
        if (settings.EstateManagementEnabled) await FillEstateAsync(dto, officeId, today);
        if (settings.ContractingEnabled) await FillContractingAsync(dto, monthStart, today);

        await FillTrendsAsync(dto, projectId, today);

        dto.NeedsAttention = await GetAttentionAsync(officeId, projectId);

        return dto;
    }

    private async Task FillRightNowAsync(
        RealEstateDashboardDto dto, Guid? officeId, Guid? projectId, DateTime now, DateOnly today)
    {
        var endOfDay = today.ToDateTime(TimeOnly.MaxValue);
        var startOfDay = today.ToDateTime(TimeOnly.MinValue);

        dto.HoldsExpiringToday = await Db.UnitHolds.ForCompany(Tenant)
            .Where(h => h.Status == HoldStatus.Active && h.ExpiresAt <= endOfDay)
            .CountAsync();

        dto.ViewingsToday = await Db.Viewings.ForCompany(Tenant)
            .WhereIf(officeId.HasValue, v => v.OfficeId == officeId)
            .Where(v => v.ScheduledAt >= startOfDay && v.ScheduledAt <= endOfDay)
            .Where(v => v.Status == ViewingStatus.Scheduled || v.Status == ViewingStatus.Confirmed)
            .CountAsync();

        dto.SiteVisitsToday = await Db.SiteVisits.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, v => v.ProjectId == projectId)
            .Where(v => v.ScheduledAt >= startOfDay && v.ScheduledAt <= endOfDay)
            .Where(v => v.Status == ViewingStatus.Scheduled || v.Status == ViewingStatus.Confirmed)
            .CountAsync();

        var openStages = new[] { EnquiryStage.New, EnquiryStage.Contacted, EnquiryStage.Qualified };

        // "Unanswered" is not the same as "new". An enquiry somebody opened but never replied to is
        // the one that loses the deal, so the test is on first contact rather than on stage.
        dto.UnansweredLeads = await Db.Enquiries.ForCompany(Tenant)
            .WhereIf(officeId.HasValue, e => e.OfficeId == officeId)
            .WhereIf(projectId.HasValue, e => e.ProjectId == projectId)
            .Where(e => e.FirstContactedAt == null && e.ClosedAt == null && openStages.Contains(e.Stage))
            .CountAsync();

        dto.LeadsBreachingSla = await Db.Enquiries.ForCompany(Tenant)
            .WhereIf(officeId.HasValue, e => e.OfficeId == officeId)
            .WhereIf(projectId.HasValue, e => e.ProjectId == projectId)
            .Where(e => e.ClosedAt == null && e.FirstContactedAt == null
                     && e.ResponseDueAt != null && e.ResponseDueAt < now)
            .CountAsync();

        dto.DemandsDueToday = await Db.Demands.ForCompany(Tenant)
            .Where(d => d.DueDate == today && d.Status != DemandStatus.Settled && d.Status != DemandStatus.Cancelled)
            .CountAsync();

        var receiptsToday = await Db.Receipts.ForCompany(Tenant)
            .WhereIf(officeId.HasValue, r => r.OfficeId == officeId)
            .WhereIf(projectId.HasValue, r => r.ProjectId == projectId)
            .Where(r => r.ReceivedOn == today && r.Status == ReceiptStatus.Posted)
            .Select(r => r.Amount)
            .ToListAsync();

        dto.ReceiptsToday = receiptsToday.Count;
        dto.CollectedToday = RealEstateMapper.Money(receiptsToday.Sum());

        var openWorkOrderStates = new[]
        {
            WorkOrderStatus.Raised, WorkOrderStatus.AwaitingAuthorisation, WorkOrderStatus.Authorised,
            WorkOrderStatus.Assigned, WorkOrderStatus.AppointmentSet, WorkOrderStatus.InProgress,
            WorkOrderStatus.AwaitingParts,
        };

        dto.OpenWorkOrders = await Db.WorkOrders.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, w => w.ProjectId == projectId)
            .Where(w => openWorkOrderStates.Contains(w.Status))
            .CountAsync();

        dto.VisitorsInsideNow = await Db.VisitorPasses.ForCompany(Tenant)
            .Where(v => v.Status == GateEntryStatus.CheckedIn && !v.IsCancelled)
            .CountAsync();
    }

    private async Task FillDevelopmentAsync(
        RealEstateDashboardDto dto, Guid? projectId, DateOnly monthStart, DateOnly lastMonthStart, DateOnly today)
    {
        var byStatus = await Db.Units.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, u => u.ProjectId == projectId)
            .Where(u => u.IsSaleable)
            .GroupBy(u => u.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        dto.TotalUnits = byStatus.Sum(x => x.Count);
        dto.UnitsAvailable = byStatus.Where(x => x.Status == PropertyStatus.Available).Sum(x => x.Count);
        dto.UnitsHeld = byStatus.Where(x => x.Status is PropertyStatus.Held or PropertyStatus.Reserved).Sum(x => x.Count);
        dto.UnitsBooked = byStatus.Where(x => x.Status == PropertyStatus.Booked).Sum(x => x.Count);
        dto.UnitsSold = byStatus
            .Where(x => x.Status is PropertyStatus.Sold or PropertyStatus.Registered or PropertyStatus.Possessed)
            .Sum(x => x.Count);

        dto.AbsorptionPercent = RealEstateMapper.Percent(dto.UnitsBooked + dto.UnitsSold, dto.TotalUnits);

        dto.InventoryByStatus = byStatus
            .OrderByDescending(x => x.Count)
            .Select(x => new BreakdownSliceDto
            {
                Label = x.Status.ToString(),
                Count = x.Count,
                Value = x.Count,
                Percent = RealEstateMapper.Percent(x.Count, dto.TotalUnits),
                Tone = x.Status switch
                {
                    PropertyStatus.Available => "positive",
                    PropertyStatus.Held or PropertyStatus.Reserved => "warning",
                    PropertyStatus.Litigation or PropertyStatus.Blocked => "danger",
                    _ => "neutral",
                },
            }).ToList();

        var live = new[]
        {
            BookingStatus.Confirmed, BookingStatus.AgreementSigned, BookingStatus.Defaulting,
            BookingStatus.PossessionOffered, BookingStatus.Possessed, BookingStatus.Completed,
        };

        var bookings = await Db.Bookings.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, b => b.ProjectId == projectId)
            .Where(b => b.BookingDate >= lastMonthStart)
            .Select(b => new { b.BookingDate, b.Status, b.TotalConsideration, b.NetSalePrice })
            .ToListAsync();

        decimal Value(DateOnly from, DateOnly to) => RealEstateMapper.Money(bookings
            .Where(b => b.BookingDate >= from && b.BookingDate <= to && live.Contains(b.Status))
            .Sum(b => b.TotalConsideration > 0m ? b.TotalConsideration : b.NetSalePrice));

        dto.BookingValueThisMonth = Value(monthStart, today);
        dto.BookingValueLastMonth = Value(lastMonthStart, monthStart.AddDays(-1));

        dto.BookingsThisMonth = bookings.Count(b => b.BookingDate >= monthStart && live.Contains(b.Status));

        dto.CancellationsThisMonth = await Db.Cancellations.ForCompany(Tenant)
            .Where(c => c.CreatedAt >= monthStart.ToDateTime(TimeOnly.MinValue))
            .CountAsync();
    }

    private async Task FillMoneyAsync(
        RealEstateDashboardDto dto, Guid? projectId, DateOnly monthStart, DateOnly today)
    {
        var demanded = await Db.Demands.ForCompany(Tenant)
            .Where(d => d.IssuedOn >= monthStart && d.Status != DemandStatus.Cancelled)
            .SumAsync(d => (decimal?)d.TotalAmount) ?? 0m;

        var collected = await Db.Receipts.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, r => r.ProjectId == projectId)
            .Where(r => r.ReceivedOn >= monthStart && r.Status == ReceiptStatus.Posted)
            .SumAsync(r => (decimal?)r.Amount) ?? 0m;

        dto.DemandedThisMonth = RealEstateMapper.Money(demanded);
        dto.CollectedThisMonth = RealEstateMapper.Money(collected);

        // Efficiency against what was asked for this month, not against everything ever owed.
        // The second number would flatter a bad month and hide a good one.
        dto.CollectionEfficiencyPercent = RealEstateMapper.Percent(collected, demanded);

        var outstanding = await Db.Bookings.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, b => b.ProjectId == projectId)
            .Where(b => b.Status != BookingStatus.Cancelled)
            .Select(b => new { b.Outstanding, b.OverdueAmount, b.DaysOverdue })
            .ToListAsync();

        dto.TotalOutstanding = RealEstateMapper.Money(outstanding.Sum(b => b.Outstanding));
        dto.OverdueAmount = RealEstateMapper.Money(outstanding.Sum(b => b.OverdueAmount));
        dto.DefaulterCount = outstanding.Count(b => b.DaysOverdue > 0);

        dto.SurchargeAccrued = RealEstateMapper.Money(
            await Db.SurchargeAccruals.ForCompany(Tenant)
                .Where(s => s.AccrualDate >= monthStart)
                .SumAsync(s => (decimal?)s.Amount) ?? 0m);

        dto.EscrowBalance = RealEstateMapper.Money(
            await Db.ProjectBankAccounts.ForCompany(Tenant)
                .WhereIf(projectId.HasValue, a => a.ProjectId == projectId)
                .Where(a => a.Kind == ProjectAccountKind.Escrow)
                .SumAsync(a => (decimal?)a.Balance) ?? 0m);
    }

    private async Task FillBrokerageAsync(
        RealEstateDashboardDto dto, Guid? officeId, DateOnly monthStart, DateOnly today)
    {
        dto.LiveListings = await Db.Listings.ForCompany(Tenant)
            .WhereIf(officeId.HasValue, l => l.OfficeId == officeId)
            .Where(l => l.Status == ListingStatus.Live || l.Status == ListingStatus.UnderOffer)
            .CountAsync();

        var openStages = new[]
        {
            EnquiryStage.New, EnquiryStage.Contacted, EnquiryStage.Qualified, EnquiryStage.ViewingBooked,
            EnquiryStage.Viewed, EnquiryStage.Revisit, EnquiryStage.Negotiation, EnquiryStage.OfferMade,
        };

        dto.OpenEnquiries = await Db.Enquiries.ForCompany(Tenant)
            .WhereIf(officeId.HasValue, e => e.OfficeId == officeId)
            .Where(e => e.ClosedAt == null && openStages.Contains(e.Stage))
            .CountAsync();

        var progressing = new[] { DealStatus.Agreed, DealStatus.Progressing, DealStatus.Exchanged };

        var deals = await Db.Deals.ForCompany(Tenant)
            .WhereIf(officeId.HasValue, d => d.OfficeId == officeId)
            .Where(d => progressing.Contains(d.Status))
            .Select(d => new { d.AgreedPrice, d.GrossFee })
            .ToListAsync();

        dto.DealsInProgress = deals.Count;

        // Pipeline is the fee we stand to earn, not the value of the property. Showing the latter
        // makes a small agency look like a large one and tells nobody anything useful.
        dto.PipelineValue = RealEstateMapper.Money(deals.Sum(d => d.GrossFee));

        dto.CommissionEarnedThisMonth = RealEstateMapper.Money(
            await Db.CommissionCalculations.ForCompany(Tenant)
                .Where(c => c.CalculatedOn >= monthStart && c.Status != CommissionStatus.Cancelled
                                                        && c.Status != CommissionStatus.ClawedBack)
                .SumAsync(c => (decimal?)c.NetDistributable) ?? 0m);

        var responded = await Db.Enquiries.ForCompany(Tenant)
            .WhereIf(officeId.HasValue, e => e.OfficeId == officeId)
            .Where(e => e.FirstContactedAt != null && e.ReceivedAt >= monthStart.ToDateTime(TimeOnly.MinValue))
            .Select(e => new { e.ReceivedAt, e.FirstContactedAt })
            .ToListAsync();

        dto.AverageSpeedToLeadMinutes = responded.Count == 0
            ? 0m
            : RealEstateMapper.Money((decimal)responded
                .Average(e => (e.FirstContactedAt!.Value - e.ReceivedAt).TotalMinutes));

        var closedDeals = await Db.Deals.ForCompany(Tenant)
            .WhereIf(officeId.HasValue, d => d.OfficeId == officeId)
            .Where(d => d.AgreedOn >= monthStart.AddMonths(-12))
            .Select(d => d.Status)
            .ToListAsync();

        var completed = closedDeals.Count(s => s == DealStatus.Completed);
        var fellThrough = closedDeals.Count(s => s == DealStatus.FellThrough);

        dto.FallThroughRatePercent = RealEstateMapper.Percent(fellThrough, completed + fellThrough);
    }

    private async Task FillEstateAsync(RealEstateDashboardDto dto, Guid? officeId, DateOnly today)
    {
        var liveTenancies = new[] { TenancyStatus.Active, TenancyStatus.NoticeGiven, TenancyStatus.Expiring };

        var tenancies = await Db.Tenancies.ForCompany(Tenant)
            .WhereIf(officeId.HasValue, t => t.OfficeId == officeId)
            .Where(t => liveTenancies.Contains(t.Status))
            .Select(t => new { t.Rent, t.Frequency, t.ArrearsAmount, t.EndDate })
            .ToListAsync();

        dto.ActiveTenancies = tenancies.Count;

        // Every frequency is normalised to a month so the rent roll is one comparable number.
        // A weekly tenancy is 52 weeks over 12 months, not four weeks a month.
        dto.MonthlyRentRoll = RealEstateMapper.Money(tenancies.Sum(t => MonthlyEquivalent(t.Rent, t.Frequency)));
        dto.RentArrears = RealEstateMapper.Money(tenancies.Sum(t => t.ArrearsAmount));

        dto.TenanciesExpiringIn90Days = tenancies.Count(t => t.EndDate is not null
            && t.EndDate >= today && t.EndDate <= today.AddDays(90));

        dto.VoidUnits = await Db.VoidRecords.ForCompany(Tenant)
            .Where(v => !v.IsClosed)
            .CountAsync();

        var managed = await Db.Properties.ForCompany(Tenant)
            .Where(p => p.Status == PropertyStatus.Let || p.Occupancy == OccupancyState.Vacant)
            .CountAsync();

        dto.OccupancyPercent = RealEstateMapper.Percent(dto.ActiveTenancies, Math.Max(managed, dto.ActiveTenancies));

        dto.ComplianceCertificatesExpiring = await Db.ComplianceCertificates.ForCompany(Tenant)
            .Where(c => c.IsCurrent && c.ExpiresOn <= today.AddDays(60))
            .CountAsync();

        var openTickets = new[]
        {
            TicketStatus.Open, TicketStatus.Acknowledged, TicketStatus.Assigned,
            TicketStatus.InProgress, TicketStatus.OnHold, TicketStatus.Reopened, TicketStatus.Escalated,
        };

        var complaints = await Db.Complaints.ForCompany(Tenant)
            .Where(c => openTickets.Contains(c.Status))
            .Select(c => new { c.SlaBreached, c.SlaDueAt })
            .ToListAsync();

        dto.OpenComplaints = complaints.Count;
        dto.ComplaintsBreachingSla = complaints.Count(c => c.SlaBreached
            || (c.SlaDueAt is not null && c.SlaDueAt < DateTime.UtcNow));
    }

    internal static decimal MonthlyEquivalent(decimal amount, RentFrequency frequency) => frequency switch
    {
        RentFrequency.Weekly => amount * 52m / 12m,
        RentFrequency.Fortnightly => amount * 26m / 12m,
        RentFrequency.Monthly => amount,
        RentFrequency.Quarterly => amount / 3m,
        RentFrequency.HalfYearly => amount / 6m,
        RentFrequency.Yearly => amount / 12m,
        _ => amount,
    };

    private async Task FillContractingAsync(RealEstateDashboardDto dto, DateOnly monthStart, DateOnly today)
    {
        var activeStates = new[]
        {
            ProjectStatus.Approved, ProjectStatus.Launched, ProjectStatus.UnderConstruction, ProjectStatus.Planning,
        };

        var projects = await Db.ConstructionProjects.ForCompany(Tenant)
            .Where(p => activeStates.Contains(p.Status))
            .Select(p => new
            {
                p.PhysicalProgressPercent,
                p.RetentionHeld,
                p.PlannedCompletionDate,
                p.ForecastCompletionDate,
            })
            .ToListAsync();

        dto.ActiveConstructionProjects = projects.Count;

        dto.AveragePhysicalProgressPercent = projects.Count == 0
            ? 0m
            : RealEstateMapper.Money(projects.Average(p => p.PhysicalProgressPercent));

        dto.RetentionHeld = RealEstateMapper.Money(projects.Sum(p => p.RetentionHeld));

        dto.ProjectsAtRiskOfDelay = projects.Count(p =>
            p.ForecastCompletionDate is not null
            && p.PlannedCompletionDate is not null
            && p.ForecastCompletionDate > p.PlannedCompletionDate);

        dto.CertifiedValueThisMonth = RealEstateMapper.Money(
            await Db.InterimPaymentCertificates.ForCompany(Tenant)
                .Where(c => c.IssuedOn >= monthStart)
                .Where(c => c.Status == CertificateStatus.Certified || c.Status == CertificateStatus.Approved
                                                                    || c.Status == CertificateStatus.Paid)
                .SumAsync(c => (decimal?)c.ThisCertificateGross) ?? 0m);

        dto.OpenVariations = await Db.VariationOrders.ForCompany(Tenant)
            .Where(v => v.Status == VariationStatus.Proposed || v.Status == VariationStatus.Quoted)
            .CountAsync();
    }

    private async Task FillTrendsAsync(RealEstateDashboardDto dto, Guid? projectId, DateOnly today)
    {
        var from = new DateOnly(today.Year, today.Month, 1).AddMonths(-11);

        var receipts = await Db.Receipts.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, r => r.ProjectId == projectId)
            .Where(r => r.ReceivedOn >= from && r.Status == ReceiptStatus.Posted)
            .Select(r => new { r.ReceivedOn, r.Amount })
            .ToListAsync();

        var bookings = await Db.Bookings.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, b => b.ProjectId == projectId)
            .Where(b => b.BookingDate >= from && b.Status != BookingStatus.Cancelled)
            .Select(b => new { b.BookingDate, b.TotalConsideration, b.NetSalePrice })
            .ToListAsync();

        var months = Enumerable.Range(0, 12).Select(i => from.AddMonths(i)).ToList();

        dto.CollectionTrend = months.Select(m => new TrendPointDto
        {
            Label = m.ToString("MMM yy"),
            Date = m,
            Value = RealEstateMapper.Money(receipts
                .Where(r => r.ReceivedOn.Year == m.Year && r.ReceivedOn.Month == m.Month)
                .Sum(r => r.Amount)),
        }).ToList();

        dto.BookingTrend = months.Select(m =>
        {
            var mine = bookings
                .Where(b => b.BookingDate.Year == m.Year && b.BookingDate.Month == m.Month)
                .ToList();

            return new TrendPointDto
            {
                Label = m.ToString("MMM yy"),
                Date = m,
                Value = RealEstateMapper.Money(mine.Sum(b => b.TotalConsideration > 0m ? b.TotalConsideration : b.NetSalePrice)),
                SecondaryValue = mine.Count,
            };
        }).ToList();

        var leads = await Db.Enquiries.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, e => e.ProjectId == projectId)
            .Where(e => e.ReceivedAt >= from.ToDateTime(TimeOnly.MinValue))
            .GroupBy(e => e.Channel)
            .Select(g => new { Channel = g.Key, Count = g.Count() })
            .ToListAsync();

        var totalLeads = leads.Sum(l => l.Count);

        dto.LeadsBySource = leads
            .OrderByDescending(l => l.Count)
            .Select(l => new BreakdownSliceDto
            {
                Label = l.Channel.ToString(),
                Count = l.Count,
                Value = l.Count,
                Percent = RealEstateMapper.Percent(l.Count, totalLeads),
            }).ToList();
    }

    // ═══ The portfolio ═══════════════════════════════════════════════════════

    public async Task<List<ProjectListItemDto>> GetPortfolioAsync(ListQueryDto query)
    {
        var currency = await CurrencyAsync();

        var projects = await Db.Projects.ForCompany(Tenant)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), p => p.Name.Contains(query.Search!))
            .WhereIf(query.OfficeId.HasValue, p => p.OfficeId == query.OfficeId)
            .OrderBy(p => p.Status)
            .ThenBy(p => p.Name)
            .ToListAsync();

        if (projects.Count == 0) return [];

        var ids = projects.Select(p => p.Id).ToList();

        var units = await Db.Units.ForCompany(Tenant)
            .Where(u => ids.Contains(u.ProjectId) && u.IsSaleable)
            .GroupBy(u => new { u.ProjectId, u.Status })
            .Select(g => new { g.Key.ProjectId, g.Key.Status, Count = g.Count() })
            .ToListAsync();

        var money = await Db.Bookings.ForCompany(Tenant)
            .Where(b => ids.Contains(b.ProjectId) && b.Status != BookingStatus.Cancelled)
            .GroupBy(b => b.ProjectId)
            .Select(g => new
            {
                ProjectId = g.Key,
                Sales = g.Sum(b => b.TotalConsideration),
                Collected = g.Sum(b => b.TotalPaid),
                Outstanding = g.Sum(b => b.Outstanding),
            })
            .ToDictionaryAsync(x => x.ProjectId, x => x);

        var escrow = await Db.ProjectBankAccounts.ForCompany(Tenant)
            .Where(a => ids.Contains(a.ProjectId) && a.Kind == ProjectAccountKind.Escrow)
            .GroupBy(a => a.ProjectId)
            .Select(g => new { ProjectId = g.Key, Balance = g.Sum(a => a.Balance) })
            .ToDictionaryAsync(x => x.ProjectId, x => x.Balance);

        var progress = await MilestoneProgressAsync(ids);

        var areaNames = await Db.GeoAreas.ForCompany(Tenant)
            .Where(a => projects.Where(p => p.GeoAreaId != null).Select(p => p.GeoAreaId!.Value).Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Name);

        return projects.Select(p =>
        {
            var mine = units.Where(u => u.ProjectId == p.Id).ToList();
            var m = money.GetValueOrDefault(p.Id);

            var total = mine.Sum(u => u.Count);
            var booked = mine.Where(u => u.Status == PropertyStatus.Booked).Sum(u => u.Count);
            var sold = mine
                .Where(u => u.Status is PropertyStatus.Sold or PropertyStatus.Registered or PropertyStatus.Possessed)
                .Sum(u => u.Count);

            var slip = p.ForecastPossessionDate is not null && p.PromisedPossessionDate is not null
                ? p.ForecastPossessionDate.Value.DayNumber - p.PromisedPossessionDate.Value.DayNumber
                : (int?)null;

            return new ProjectListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code,
                Kind = p.Kind,
                Status = p.Status,
                City = p.City,
                AreaName = p.GeoAreaId is null ? null : areaNames.GetValueOrDefault(p.GeoAreaId.Value),
                HeroImageUrl = p.HeroImageUrl,
                CurrencyCode = string.IsNullOrWhiteSpace(p.CurrencyCode) ? currency : p.CurrencyCode,
                TotalUnits = total,
                UnitsAvailable = mine.Where(u => u.Status == PropertyStatus.Available).Sum(u => u.Count),
                UnitsBooked = booked,
                UnitsSold = sold,
                AbsorptionPercent = RealEstateMapper.Percent(booked + sold, total),
                TotalSalesValue = RealEstateMapper.Money(m?.Sales ?? 0m),
                TotalCollected = RealEstateMapper.Money(m?.Collected ?? 0m),
                Outstanding = RealEstateMapper.Money(m?.Outstanding ?? 0m),
                PhysicalProgressPercent = progress.GetValueOrDefault(p.Id),
                PromisedPossessionDate = p.PromisedPossessionDate,
                ForecastPossessionDate = p.ForecastPossessionDate,

                // Only a slip is shown. A project running early is not news, and reporting it as a
                // negative number invites somebody to add it to a total that then means nothing.
                SlipDays = slip is > 0 ? slip : null,
                HasJointVenture = p.HasJointVenture,
                EscrowBalance = escrow.TryGetValue(p.Id, out var bal) ? RealEstateMapper.Money(bal) : null,
            };
        }).ToList();
    }

    private async Task<Dictionary<Guid, decimal>> MilestoneProgressAsync(List<Guid> projectIds)
    {
        if (projectIds.Count == 0) return [];

        var milestones = await Db.ProjectMilestones.ForCompany(Tenant)
            .Where(m => projectIds.Contains(m.ProjectId))
            .Select(m => new { m.ProjectId, m.WeightPercent, m.Status })
            .ToListAsync();

        var result = new Dictionary<Guid, decimal>();

        foreach (var id in projectIds)
        {
            var mine = milestones.Where(m => m.ProjectId == id).ToList();

            if (mine.Count == 0) { result[id] = 0m; continue; }

            var weight = mine.Sum(m => m.WeightPercent);

            var earned = mine
                .Where(m => m.Status is MilestoneStatus.Reached or MilestoneStatus.Certified)
                .Sum(m => m.WeightPercent);

            result[id] = weight <= 0m
                ? RealEstateMapper.Percent(
                    mine.Count(m => m.Status is MilestoneStatus.Reached or MilestoneStatus.Certified), mine.Count)
                : RealEstateMapper.Money(earned / weight * 100m);
        }

        return result;
    }
}
