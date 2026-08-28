using Microsoft.EntityFrameworkCore;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>Brokerage and estate-management reports.</summary>
public partial class RealEstateReportService
{
    private async Task LeadFunnelAsync(ReportResultDto result, ReportRequestDto request, DateOnly from, DateOnly to)
    {
        result.Columns =
        [
            Text("stage", "Stage", 160),
            Number("count", "Enquiries"),
            Pct("ofTotal", "Of total"),
            Pct("conversion", "Reached from previous"),
            Number("avgDays", "Average days here", false),
        ];

        var enquiries = await Db.Enquiries.ForCompany(Tenant)
            .WhereIf(request.OfficeId.HasValue, e => e.OfficeId == request.OfficeId)
            .WhereIf(request.ProjectId.HasValue, e => e.ProjectId == request.ProjectId)
            .Where(e => e.ReceivedAt >= from.ToDateTime(TimeOnly.MinValue)
                     && e.ReceivedAt <= to.ToDateTime(TimeOnly.MaxValue))
            .Select(e => new { e.Stage, e.ReceivedAt, e.LastActivityAt, e.ClosedAt, e.Channel, e.LossReasonCodeId })
            .ToListAsync();

        // The funnel is cumulative: an enquiry that reached "Booked" also reached every stage
        // before it. Counting only the current stage makes every funnel look like a cliff.
        var ladder = new[]
        {
            EnquiryStage.New, EnquiryStage.Contacted, EnquiryStage.Qualified, EnquiryStage.ViewingBooked,
            EnquiryStage.Viewed, EnquiryStage.Negotiation, EnquiryStage.OfferMade, EnquiryStage.Agreed,
            EnquiryStage.Booked, EnquiryStage.Completed,
        };

        var total = enquiries.Count;
        var rows = new List<Dictionary<string, object?>>();
        var previous = total;

        for (var i = 0; i < ladder.Length; i++)
        {
            var stage = ladder[i];
            var reached = enquiries.Count(e => Array.IndexOf(ladder, e.Stage) >= i);

            var inStage = enquiries.Where(e => e.Stage == stage && e.LastActivityAt is not null).ToList();

            rows.Add(new Dictionary<string, object?>
            {
                ["stage"] = SplitCamel(stage.ToString()),
                ["count"] = reached,
                ["ofTotal"] = RealEstateMapper.Percent(reached, total),
                ["conversion"] = RealEstateMapper.Percent(reached, previous),
                ["avgDays"] = inStage.Count == 0
                    ? null
                    : Math.Round(inStage.Average(e => (e.LastActivityAt!.Value - e.ReceivedAt).TotalDays), 1),
            });

            previous = reached == 0 ? previous : reached;
        }

        var lost = enquiries.Count(e => e.Stage == EnquiryStage.Lost);

        rows.Add(new Dictionary<string, object?>
        {
            ["stage"] = "Lost",
            ["count"] = lost,
            ["ofTotal"] = RealEstateMapper.Percent(lost, total),
            ["conversion"] = 0m,
            ["avgDays"] = null,
        });

        result.Rows = rows;

        result.Breakdown = enquiries
            .GroupBy(e => e.Channel)
            .OrderByDescending(g => g.Count())
            .Select(g => new BreakdownSliceDto
            {
                Label = g.Key.ToString(),
                Count = g.Count(),
                Value = g.Count(),
                Percent = RealEstateMapper.Percent(g.Count(), total),
            }).ToList();

        result.Totals["__rowCount"] = rows.Count;
    }

    private static string SplitCamel(string value)
        => System.Text.RegularExpressions.Regex.Replace(value, "(?<!^)([A-Z])", " $1");

    private async Task AgentPerformanceAsync(ReportResultDto result, ReportRequestDto request, DateOnly from, DateOnly to)
    {
        result.Columns =
        [
            Text("agent", "Negotiator", 190),
            Number("leads", "Leads"),
            Number("viewings", "Viewings"),
            Number("deals", "Deals agreed"),
            Number("completed", "Completed"),
            Pct("conversion", "Lead to deal"),
            Money("fees", "Fees earned"),
            Number("speedToLead", "Speed to lead (min)", false),
        ];

        var agents = await Db.AgentProfiles.ForCompany(Tenant)
            .WhereIf(request.OfficeId.HasValue, a => a.OfficeId == request.OfficeId)
            .WhereIf(request.AgentId.HasValue, a => a.Id == request.AgentId)
            .ToListAsync();

        if (agents.Count == 0) { Total(result); return; }

        var ids = agents.Select(a => a.Id).ToList();

        var enquiries = await Db.Enquiries.ForCompany(Tenant)
            .Where(e => e.AssignedAgentId != null && ids.Contains(e.AssignedAgentId.Value))
            .Where(e => e.ReceivedAt >= from.ToDateTime(TimeOnly.MinValue)
                     && e.ReceivedAt <= to.ToDateTime(TimeOnly.MaxValue))
            .Select(e => new { e.AssignedAgentId, e.ReceivedAt, e.FirstContactedAt })
            .ToListAsync();

        var viewings = await Db.Viewings.ForCompany(Tenant)
            .Where(v => ids.Contains(v.AgentId) && v.ScheduledAt >= from.ToDateTime(TimeOnly.MinValue)
                                                && v.ScheduledAt <= to.ToDateTime(TimeOnly.MaxValue))
            .GroupBy(v => v.AgentId)
            .Select(g => new { AgentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.AgentId, x => x.Count);

        var deals = await Db.Deals.ForCompany(Tenant)
            .Where(d => d.AgreedOn >= from && d.AgreedOn <= to)
            .Select(d => new { d.ListingAgentId, d.SellingAgentId, d.Status, d.GrossFee })
            .ToListAsync();

        var splits = await Db.CommissionSplits.ForCompany(Tenant)
            .Where(s => s.AgentProfileId != null && ids.Contains(s.AgentProfileId.Value))
            .GroupBy(s => s.AgentProfileId!.Value)
            .Select(g => new { AgentId = g.Key, Net = g.Sum(s => s.NetAmount) })
            .ToDictionaryAsync(x => x.AgentId, x => x.Net);

        result.Rows = agents.Select(a =>
        {
            var mine = enquiries.Where(e => e.AssignedAgentId == a.Id).ToList();
            var responded = mine.Where(e => e.FirstContactedAt is not null).ToList();

            var myDeals = deals.Where(d => d.ListingAgentId == a.Id || d.SellingAgentId == a.Id).ToList();

            return new Dictionary<string, object?>
            {
                ["agent"] = a.DisplayName,
                ["leads"] = mine.Count,
                ["viewings"] = viewings.GetValueOrDefault(a.Id),
                ["deals"] = myDeals.Count,
                ["completed"] = myDeals.Count(d => d.Status == DealStatus.Completed),
                ["conversion"] = RealEstateMapper.Percent(myDeals.Count, mine.Count),
                ["fees"] = RealEstateMapper.Money(splits.GetValueOrDefault(a.Id)),
                ["speedToLead"] = responded.Count == 0
                    ? null
                    : Math.Round(responded.Average(e => (e.FirstContactedAt!.Value - e.ReceivedAt).TotalMinutes), 0),
            };
        })
        .OrderByDescending(r => Convert.ToDecimal(r["fees"]))
        .ToList();

        Total(result);
    }

    private async Task ListingPerformanceAsync(ReportResultDto result, ReportRequestDto request)
    {
        result.Columns =
        [
            Text("reference", "Listing", 120),
            Text("address", "Address", 260),
            Tag("status", "Status"),
            Money("asking", "Asking", false),
            Number("days", "Days on market", false),
            Number("views", "Views"),
            Number("enquiries", "Enquiries"),
            Number("viewings", "Viewings"),
            Number("offers", "Offers"),
            Pct("viewToEnquiry", "View to enquiry"),
            Date("lastEnquiry", "Last enquiry"),
        ];

        var listings = await Db.Listings.ForCompany(Tenant)
            .WhereIf(request.OfficeId.HasValue, l => l.OfficeId == request.OfficeId)
            .Where(l => l.Status == ListingStatus.Live || l.Status == ListingStatus.UnderOffer
                     || l.Status == ListingStatus.SubjectToContract)
            .Include(l => l.Property)
            .OrderByDescending(l => l.DaysOnMarket)
            .ToListAsync();

        result.Rows = listings.Select(l => new Dictionary<string, object?>
        {
            ["reference"] = l.Reference,
            ["address"] = l.Property is null ? "—" : RealEstateMapper.OneLineAddress(l.Property),
            ["status"] = l.Status.ToString(),
            ["asking"] = l.AskingPrice ?? 0m,
            ["days"] = l.DaysOnMarket ?? (l.ListedOn is null ? null : Today.DayNumber - l.ListedOn.Value.DayNumber),
            ["views"] = l.ViewCount,
            ["enquiries"] = l.EnquiryCount,
            ["viewings"] = l.ViewingCount,
            ["offers"] = l.OfferCount,
            ["viewToEnquiry"] = RealEstateMapper.Percent(l.EnquiryCount, l.ViewCount),
            ["lastEnquiry"] = l.LastEnquiryAt?.Date,
        }).ToList();

        Total(result);
    }

    private async Task DealPipelineAsync(ReportResultDto result, ReportRequestDto request)
    {
        result.Columns =
        [
            Text("reference", "Deal", 120),
            Text("address", "Property", 240),
            Tag("status", "Status"),
            Text("buyer", "Buyer", 180),
            Money("price", "Agreed price", false),
            Money("fee", "Our fee"),
            Date("agreedOn", "Agreed"),
            Number("daysInProgress", "Days running", false),
            Number("daysSinceMilestone", "Days since last move", false),
            Date("targetCompletion", "Target completion"),
            Text("chain", "Chain", 100),
        ];

        var progressing = new[] { DealStatus.Agreed, DealStatus.Progressing, DealStatus.Exchanged, DealStatus.OnHold };

        var deals = await Db.Deals.ForCompany(Tenant)
            .WhereIf(request.OfficeId.HasValue, d => d.OfficeId == request.OfficeId)
            .Where(d => progressing.Contains(d.Status))
            .OrderByDescending(d => d.DaysSinceLastMilestone ?? 0)
            .ToListAsync();

        var names = await PartyNamesAsync(deals.Select(d => d.BuyerPartyId));

        var propertyIds = deals.Select(d => d.PropertyId).Distinct().ToList();

        var properties = propertyIds.Count == 0
            ? []
            : await Db.Properties.ForCompany(Tenant).Where(p => propertyIds.Contains(p.Id)).ToListAsync();

        result.Rows = deals.Select(d => new Dictionary<string, object?>
        {
            ["reference"] = d.Reference,
            ["address"] = properties.Where(p => p.Id == d.PropertyId)
                .Select(RealEstateMapper.OneLineAddress).FirstOrDefault() ?? "—",
            ["status"] = d.Status.ToString(),
            ["buyer"] = names.GetValueOrDefault(d.BuyerPartyId, "—"),
            ["price"] = d.AgreedPrice,
            ["fee"] = d.GrossFee,
            ["agreedOn"] = d.AgreedOn,
            ["daysInProgress"] = d.DaysInProgress,
            ["daysSinceMilestone"] = d.DaysSinceLastMilestone,
            ["targetCompletion"] = d.TargetCompletionDate,
            ["chain"] = d.ChainId is null ? "No" : $"Position {d.ChainPosition ?? 0}",
        }).ToList();

        result.Breakdown = deals
            .GroupBy(d => d.Status)
            .Select(g => new BreakdownSliceDto
            {
                Label = g.Key.ToString(),
                Count = g.Count(),
                Value = RealEstateMapper.Money(g.Sum(d => d.GrossFee)),
                Percent = RealEstateMapper.Percent(g.Count(), deals.Count),
                Tone = g.Key == DealStatus.OnHold ? "warning" : "neutral",
            }).ToList();

        Total(result);
    }

    private async Task CommissionStatementAsync(ReportResultDto result, ReportRequestDto request, DateOnly from, DateOnly to)
    {
        result.Columns =
        [
            Text("reference", "Calculation", 140),
            Date("calculatedOn", "Calculated"),
            Text("earner", "Earner", 190),
            Text("role", "Role", 120),
            Money("transactionValue", "Transaction", false),
            Money("gross", "Gross"),
            Money("deductions", "Deductions"),
            Money("withholding", "Withheld"),
            Money("net", "Net"),
            Money("paid", "Paid"),
            Tag("status", "Status"),
        ];

        var calculations = await Db.CommissionCalculations.ForCompany(Tenant)
            .WhereIf(request.ProjectId.HasValue, c => c.ProjectId == request.ProjectId)
            .Where(c => c.CalculatedOn >= from && c.CalculatedOn <= to)
            .Include(c => c.Splits)
            .OrderByDescending(c => c.CalculatedOn)
            .ToListAsync();

        var splits = calculations.SelectMany(c => c.Splits)
            .Where(s => request.AgentId is null || s.AgentProfileId == request.AgentId)
            .Where(s => request.ChannelPartnerId is null || s.ChannelPartnerId == request.ChannelPartnerId)
            .ToList();

        var agentIds = splits.Where(s => s.AgentProfileId != null).Select(s => s.AgentProfileId!.Value).Distinct().ToList();
        var partnerIds = splits.Where(s => s.ChannelPartnerId != null).Select(s => s.ChannelPartnerId!.Value).Distinct().ToList();

        var agents = agentIds.Count == 0
            ? []
            : await Db.AgentProfiles.ForCompany(Tenant)
                .Where(a => agentIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, a => a.DisplayName);

        var partners = partnerIds.Count == 0
            ? []
            : await Db.ChannelPartners.ForCompany(Tenant)
                .Where(p => partnerIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name);

        result.Rows = splits.Select(s =>
        {
            var calculation = calculations.First(c => c.Id == s.CommissionCalculationId);

            return new Dictionary<string, object?>
            {
                ["reference"] = calculation.Reference,
                ["calculatedOn"] = calculation.CalculatedOn,
                ["earner"] = s.AgentProfileId is not null
                    ? agents.GetValueOrDefault(s.AgentProfileId.Value, "—")
                    : s.ChannelPartnerId is not null
                        ? partners.GetValueOrDefault(s.ChannelPartnerId.Value, "—")
                        : "House",
                ["role"] = s.Role,
                ["transactionValue"] = calculation.TransactionValue,
                ["gross"] = s.GrossAmount,
                ["deductions"] = s.DeductionTotal,
                ["withholding"] = s.WithholdingAmount,
                ["net"] = s.NetAmount,
                ["paid"] = s.PaidAmount,
                ["status"] = s.Status.ToString(),
            };
        }).ToList();

        result.Trend = MonthlyTrend(from, to,
            calculations.Select(c => (c.CalculatedOn, c.NetDistributable, 1m)));

        Total(result);
    }

    private async Task FallThroughAsync(ReportResultDto result, DateOnly from, DateOnly to)
    {
        result.Columns =
        [
            Text("deal", "Deal", 120),
            Date("occurredOn", "Fell through"),
            Tag("cause", "Cause"),
            Text("stage", "Stage reached", 160),
            Number("daysInProgress", "Days invested", false),
            Money("lostFee", "Fee lost"),
            Money("costIncurred", "Cost incurred"),
            Text("relisted", "Relisted", 100),
        ];

        var records = await Db.FallThroughRecords.ForCompany(Tenant)
            .Where(f => f.OccurredOn >= from && f.OccurredOn <= to)
            .OrderByDescending(f => f.OccurredOn)
            .ToListAsync();

        var dealIds = records.Select(f => f.DealId).Distinct().ToList();

        var deals = dealIds.Count == 0
            ? []
            : await Db.Deals.ForCompany(Tenant)
                .Where(d => dealIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.Reference);

        result.Rows = records.Select(f => new Dictionary<string, object?>
        {
            ["deal"] = deals.GetValueOrDefault(f.DealId, "—"),
            ["occurredOn"] = f.OccurredOn,
            ["cause"] = SplitCamel(f.Cause.ToString()),
            ["stage"] = f.StageReached ?? "—",
            ["daysInProgress"] = f.DaysInProgress,
            ["lostFee"] = f.LostFee,
            ["costIncurred"] = f.CostIncurred,
            ["relisted"] = f.RelistedImmediately ? "Yes" : "No",
        }).ToList();

        result.Breakdown = records
            .GroupBy(f => f.Cause)
            .OrderByDescending(g => g.Count())
            .Select(g => new BreakdownSliceDto
            {
                Label = SplitCamel(g.Key.ToString()),
                Count = g.Count(),
                Value = RealEstateMapper.Money(g.Sum(f => f.LostFee)),
                Percent = RealEstateMapper.Percent(g.Count(), records.Count),
                Tone = "danger",
            }).ToList();

        Total(result);
    }

    private async Task PartnerPerformanceAsync(ReportResultDto result)
    {
        result.Columns =
        [
            Text("name", "Partner", 220),
            Tag("status", "Status"),
            Number("leads", "Leads registered"),
            Number("visits", "Site visits"),
            Number("bookings", "Bookings"),
            Money("bookingValue", "Booking value"),
            Money("collection", "Collection contributed"),
            Pct("conversion", "Conversion"),
            Number("cancellations", "Cancellations"),
            Money("commissionEarned", "Commission earned"),
            Money("commissionPaid", "Paid"),
            Money("commissionPending", "Pending"),
            Money("advance", "Advance outstanding"),
        ];

        var partners = await Db.ChannelPartners.ForCompany(Tenant)
            .OrderByDescending(p => p.BookingValue)
            .ToListAsync();

        result.Rows = partners.Select(p => new Dictionary<string, object?>
        {
            ["name"] = p.Name,
            ["status"] = p.Status.ToString(),
            ["leads"] = p.LeadsRegistered,
            ["visits"] = p.SiteVisitsDone,
            ["bookings"] = p.BookingsMade,
            ["bookingValue"] = p.BookingValue,
            ["collection"] = p.CollectionContribution,
            ["conversion"] = p.ConversionPercent,
            ["cancellations"] = p.CancellationCount,
            ["commissionEarned"] = p.CommissionEarned,
            ["commissionPaid"] = p.CommissionPaid,
            ["commissionPending"] = p.CommissionPending,
            ["advance"] = p.AdvanceOutstanding,
        }).ToList();

        Total(result);
    }

    // ═══ Estate management ═══════════════════════════════════════════════════

    private async Task RentRollAsync(ReportResultDto result, ReportRequestDto request)
    {
        result.Columns =
        [
            Text("reference", "Tenancy", 120),
            Text("address", "Property", 250),
            Text("tenant", "Tenant", 190),
            Money("rent", "Rent", false),
            Tag("frequency", "Frequency"),
            Money("monthly", "Monthly equivalent"),
            Money("annual", "Annualised"),
            Date("startDate", "From"),
            Date("endDate", "To"),
            Tag("status", "Status"),
            Money("arrears", "Arrears"),
            Money("deposit", "Deposit", false),
        ];

        var liveStates = new[] { TenancyStatus.Active, TenancyStatus.NoticeGiven, TenancyStatus.Expiring };

        var tenancies = await Db.Tenancies.ForCompany(Tenant)
            .WhereIf(request.OfficeId.HasValue, t => t.OfficeId == request.OfficeId)
            .WhereIf(request.LandlordId.HasValue, t => t.LandlordId == request.LandlordId)
            .WhereIf(request.PropertyId.HasValue, t => t.PropertyId == request.PropertyId)
            .Where(t => liveStates.Contains(t.Status))
            .Include(t => t.Property)
            .OrderBy(t => t.Reference)
            .ToListAsync();

        var tenantNames = await TenancyTenantNamesAsync(tenancies.Select(t => t.Id));

        result.Rows = tenancies.Select(t =>
        {
            var monthly = MonthlyEquivalent(t.Rent, t.Frequency);

            return new Dictionary<string, object?>
            {
                ["reference"] = t.Reference,
                ["address"] = t.Property is null ? "—" : RealEstateMapper.OneLineAddress(t.Property),
                ["tenant"] = tenantNames.GetValueOrDefault(t.Id, "—"),
                ["rent"] = t.Rent,
                ["frequency"] = t.Frequency.ToString(),
                ["monthly"] = RealEstateMapper.Money(monthly),
                ["annual"] = RealEstateMapper.Money(monthly * 12m),
                ["startDate"] = t.StartDate,
                ["endDate"] = t.EndDate,
                ["status"] = t.Status.ToString(),
                ["arrears"] = t.ArrearsAmount,
                ["deposit"] = t.DepositAmount,
            };
        }).ToList();

        result.Breakdown = tenancies
            .GroupBy(t => t.Status)
            .Select(g => new BreakdownSliceDto
            {
                Label = g.Key.ToString(),
                Count = g.Count(),
                Value = RealEstateMapper.Money(g.Sum(t => MonthlyEquivalent(t.Rent, t.Frequency))),
                Percent = RealEstateMapper.Percent(g.Count(), tenancies.Count),
            }).ToList();

        Total(result);
    }

    private async Task<Dictionary<Guid, string>> TenancyTenantNamesAsync(IEnumerable<Guid> tenancyIds)
    {
        var ids = tenancyIds.Distinct().ToList();
        if (ids.Count == 0) return [];

        var parties = await Db.TenancyParties.ForCompany(Tenant)
            .Where(p => ids.Contains(p.TenancyId))
            .Select(p => new { p.TenancyId, p.PartyId, p.IsLeadTenant })
            .ToListAsync();

        var names = await PartyNamesAsync(parties.Select(p => p.PartyId));

        return parties
            .GroupBy(p => p.TenancyId)
            .ToDictionary(
                g => g.Key,
                g => string.Join(", ", g.OrderByDescending(p => p.IsLeadTenant)
                    .Select(p => names.GetValueOrDefault(p.PartyId, "—")).Take(2)));
    }

    private async Task ArrearsAsync(ReportResultDto result, ReportRequestDto request)
    {
        result.Columns =
        [
            Text("reference", "Case", 130),
            Text("tenant", "Tenant", 190),
            Text("address", "Property", 240),
            Money("arrears", "Arrears"),
            Money("lateFees", "Late fees"),
            Number("days", "Days", false),
            Number("months", "Months", false),
            Text("bucket", "Age", 110),
            Text("promise", "Promise to pay", 130),
            Text("notice", "Notice served", 120),
            Text("legal", "With legal", 110),
        ];

        var cases = await Db.ArrearsCases.ForCompany(Tenant)
            .Where(c => !c.IsClosed)
            .OrderByDescending(c => c.ArrearsAmount)
            .ToListAsync();

        var names = await PartyNamesAsync(cases.Select(c => c.PartyId));

        var propertyIds = cases.Where(c => c.PropertyId != null).Select(c => c.PropertyId!.Value).Distinct().ToList();

        var properties = propertyIds.Count == 0
            ? []
            : await Db.Properties.ForCompany(Tenant).Where(p => propertyIds.Contains(p.Id)).ToListAsync();

        var tenancyRefs = await Db.Tenancies.ForCompany(Tenant)
            .Where(t => cases.Select(c => c.TenancyId).Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Reference);

        result.Rows = cases.Select(c => new Dictionary<string, object?>
        {
            ["reference"] = string.IsNullOrWhiteSpace(c.Reference)
                ? tenancyRefs.GetValueOrDefault(c.TenancyId, "—")
                : c.Reference,
            ["tenant"] = names.GetValueOrDefault(c.PartyId, "—"),
            ["address"] = c.PropertyId is null
                ? "—"
                : properties.Where(p => p.Id == c.PropertyId).Select(RealEstateMapper.OneLineAddress).FirstOrDefault() ?? "—",
            ["arrears"] = c.ArrearsAmount,
            ["lateFees"] = c.LateFeeAmount,
            ["days"] = c.DaysInArrears,
            ["months"] = c.MonthsInArrears,
            ["bucket"] = RealEstateMapper.AgeingBucket(c.DaysInArrears),
            ["promise"] = c.ActivePromiseId is null ? "None" : "Active",
            ["notice"] = c.NoticeId is null ? "No" : "Yes",
            ["legal"] = c.ReferredToLegal ? "Yes" : "No",
        }).ToList();

        result.Breakdown = cases
            .GroupBy(c => RealEstateMapper.AgeingBucket(c.DaysInArrears))
            .Select(g => new BreakdownSliceDto
            {
                Label = g.Key,
                Count = g.Count(),
                Value = RealEstateMapper.Money(g.Sum(c => c.ArrearsAmount)),
                Percent = RealEstateMapper.Percent(g.Sum(c => c.ArrearsAmount), cases.Sum(c => c.ArrearsAmount)),
                Tone = g.Key is "121–180" or "180+" ? "danger" : "warning",
            }).ToList();

        Total(result);
    }

    private async Task ExpiriesAsync(ReportResultDto result, ReportRequestDto request)
    {
        result.Columns =
        [
            Text("reference", "Tenancy", 120),
            Text("address", "Property", 250),
            Text("tenant", "Tenant", 190),
            Date("endDate", "Ends"),
            Number("daysToEnd", "Days", false),
            Money("rent", "Rent", false),
            Tag("status", "Status"),
            Text("periodic", "Rolls to periodic", 150),
            Text("renewal", "Renewal started", 140),
        ];

        var liveStates = new[] { TenancyStatus.Active, TenancyStatus.NoticeGiven, TenancyStatus.Expiring };
        var horizon = Today.AddDays(180);

        var tenancies = await Db.Tenancies.ForCompany(Tenant)
            .WhereIf(request.OfficeId.HasValue, t => t.OfficeId == request.OfficeId)
            .Where(t => liveStates.Contains(t.Status) && t.EndDate != null && t.EndDate <= horizon)
            .Include(t => t.Property)
            .OrderBy(t => t.EndDate)
            .ToListAsync();

        var names = await TenancyTenantNamesAsync(tenancies.Select(t => t.Id));

        var renewals = await Db.TenancyRenewals.ForCompany(Tenant)
            .Where(r => tenancies.Select(t => t.Id).Contains(r.TenancyId))
            .Select(r => r.TenancyId)
            .ToListAsync();

        result.Rows = tenancies.Select(t => new Dictionary<string, object?>
        {
            ["reference"] = t.Reference,
            ["address"] = t.Property is null ? "—" : RealEstateMapper.OneLineAddress(t.Property),
            ["tenant"] = names.GetValueOrDefault(t.Id, "—"),
            ["endDate"] = t.EndDate,
            ["daysToEnd"] = t.EndDate!.Value.DayNumber - Today.DayNumber,
            ["rent"] = t.Rent,
            ["status"] = t.Status.ToString(),
            ["periodic"] = t.RollsToPeriodic ? "Yes" : "No",
            ["renewal"] = renewals.Contains(t.Id) ? "Yes" : "Not yet",
        }).ToList();

        Total(result);
    }

    private async Task VoidsAsync(ReportResultDto result, DateOnly from, DateOnly to)
    {
        result.Columns =
        [
            Text("address", "Property", 260),
            Date("vacantFrom", "Vacant from"),
            Date("letFrom", "Let from"),
            Number("days", "Days void", false),
            Money("askingRent", "Asking rent", false),
            Money("lostRent", "Rent lost"),
            Money("holdingCost", "Holding cost"),
            Number("enquiries", "Enquiries"),
            Number("viewings", "Viewings"),
            Text("refurb", "Refurbishment", 130),
        ];

        var voids = await Db.VoidRecords.ForCompany(Tenant)
            .Where(v => v.VacantFrom >= from && v.VacantFrom <= to)
            .OrderByDescending(v => v.DaysVoid)
            .ToListAsync();

        var propertyIds = voids.Select(v => v.PropertyId).Distinct().ToList();

        var properties = propertyIds.Count == 0
            ? []
            : await Db.Properties.ForCompany(Tenant).Where(p => propertyIds.Contains(p.Id)).ToListAsync();

        result.Rows = voids.Select(v => new Dictionary<string, object?>
        {
            ["address"] = properties.Where(p => p.Id == v.PropertyId)
                .Select(RealEstateMapper.OneLineAddress).FirstOrDefault() ?? "—",
            ["vacantFrom"] = v.VacantFrom,
            ["letFrom"] = v.LetFrom,
            ["days"] = v.LetFrom is null ? Today.DayNumber - v.VacantFrom.DayNumber : v.DaysVoid,
            ["askingRent"] = v.AskingRent,
            ["lostRent"] = v.LostRent,
            ["holdingCost"] = v.HoldingCost,
            ["enquiries"] = v.EnquiryCount,
            ["viewings"] = v.ViewingCount,
            ["refurb"] = v.RefurbishmentRequired ? "Required" : "No",
        }).ToList();

        Total(result);
    }

    private async Task ComplianceStatusAsync(ReportResultDto result)
    {
        result.Columns =
        [
            Text("address", "Property", 260),
            Tag("kind", "Certificate"),
            Text("number", "Number", 150),
            Date("issuedOn", "Issued"),
            Date("expiresOn", "Expires"),
            Number("daysToExpiry", "Days", false),
            Tag("state", "State"),
            Text("served", "Served to tenant", 140),
            Text("failures", "Failures", 110),
        ];

        var certificates = await Db.ComplianceCertificates.ForCompany(Tenant)
            .Where(c => c.IsCurrent)
            .OrderBy(c => c.ExpiresOn)
            .ToListAsync();

        var propertyIds = certificates.Select(c => c.PropertyId).Distinct().ToList();

        var properties = propertyIds.Count == 0
            ? []
            : await Db.Properties.ForCompany(Tenant).Where(p => propertyIds.Contains(p.Id)).ToListAsync();

        result.Rows = certificates.Select(c =>
        {
            var days = c.ExpiresOn.DayNumber - Today.DayNumber;

            return new Dictionary<string, object?>
            {
                ["address"] = properties.Where(p => p.Id == c.PropertyId)
                    .Select(RealEstateMapper.OneLineAddress).FirstOrDefault() ?? "—",
                ["kind"] = SplitCamel(c.Kind.ToString()),
                ["number"] = c.CertificateNumber ?? "—",
                ["issuedOn"] = c.IssuedOn,
                ["expiresOn"] = c.ExpiresOn,
                ["daysToExpiry"] = days,
                ["state"] = days < 0 ? "Expired" : days <= 30 ? "Expiring" : "Current",
                ["served"] = c.ServedToTenant ? "Yes" : "No",
                ["failures"] = c.HasFailures ? "Yes" : "No",
            };
        }).ToList();

        result.Breakdown = certificates
            .GroupBy(c => c.ExpiresOn < Today ? "Expired" : c.ExpiresOn <= Today.AddDays(30) ? "Expiring" : "Current")
            .Select(g => new BreakdownSliceDto
            {
                Label = g.Key,
                Count = g.Count(),
                Value = g.Count(),
                Percent = RealEstateMapper.Percent(g.Count(), certificates.Count),
                Tone = g.Key switch { "Expired" => "danger", "Expiring" => "warning", _ => "positive" },
            }).ToList();

        result.Totals["__rowCount"] = result.Rows.Count;
    }

    private async Task LandlordStatementsAsync(ReportResultDto result, ReportRequestDto request, DateOnly from, DateOnly to)
    {
        result.Columns =
        [
            Text("reference", "Statement", 130),
            Text("landlord", "Landlord", 200),
            Date("periodTo", "Period to"),
            Money("opening", "Opening", false),
            Money("rentCollected", "Rent collected"),
            Money("otherIncome", "Other income"),
            Money("managementFee", "Management fee"),
            Money("maintenance", "Maintenance"),
            Money("taxWithheld", "Tax withheld"),
            Money("netPayable", "Net payable"),
            Money("closing", "Closing", false),
            Text("sent", "Sent", 90),
        ];

        var statements = await Db.OwnerStatements.ForCompany(Tenant)
            .WhereIf(request.LandlordId.HasValue, s => s.LandlordId == request.LandlordId)
            .Where(s => s.PeriodTo >= from && s.PeriodTo <= to)
            .OrderByDescending(s => s.PeriodTo)
            .ToListAsync();

        var landlordIds = statements.Select(s => s.LandlordId).Distinct().ToList();

        var landlords = landlordIds.Count == 0
            ? []
            : await Db.Landlords.ForCompany(Tenant)
                .Where(l => landlordIds.Contains(l.Id))
                .Select(l => new { l.Id, l.PartyId })
                .ToListAsync();

        var names = await PartyNamesAsync(landlords.Select(l => l.PartyId));

        result.Rows = statements.Select(s =>
        {
            var landlord = landlords.FirstOrDefault(l => l.Id == s.LandlordId);

            return new Dictionary<string, object?>
            {
                ["reference"] = s.Reference,
                ["landlord"] = landlord is null ? "—" : names.GetValueOrDefault(landlord.PartyId, "—"),
                ["periodTo"] = s.PeriodTo,
                ["opening"] = s.OpeningBalance,
                ["rentCollected"] = s.RentCollected,
                ["otherIncome"] = s.OtherIncome,
                ["managementFee"] = s.ManagementFee,
                ["maintenance"] = s.MaintenanceCost,
                ["taxWithheld"] = s.TaxWithheld,
                ["netPayable"] = s.NetPayable,
                ["closing"] = s.ClosingBalance,
                ["sent"] = s.IsSent ? "Yes" : "No",
            };
        }).ToList();

        Total(result);
    }

    private async Task ServiceChargeRecoveryAsync(ReportResultDto result, DateOnly from, DateOnly to)
    {
        result.Columns =
        [
            Text("reference", "Budget", 140),
            Number("year", "Year", false),
            Date("periodTo", "Period to"),
            Money("budget", "Budgeted"),
            Money("actual", "Actual"),
            Money("billed", "Billed"),
            Money("collected", "Collected"),
            Money("shortfall", "Under-recovery"),
            Pct("recovery", "Recovery"),
            Text("reconciled", "Reconciled", 110),
        ];

        var budgets = await Db.ServiceChargeBudgets.ForCompany(Tenant)
            .Where(b => b.PeriodTo >= from && b.PeriodTo <= to)
            .OrderByDescending(b => b.PeriodTo)
            .ToListAsync();

        var invoices = await Db.ServiceChargeInvoices.ForCompany(Tenant)
            .Where(i => budgets.Select(b => b.Id).Contains(i.ServiceChargeBudgetId))
            .GroupBy(i => i.ServiceChargeBudgetId)
            .Select(g => new { BudgetId = g.Key, Billed = g.Sum(i => i.TotalAmount), Paid = g.Sum(i => i.PaidAmount) })
            .ToDictionaryAsync(x => x.BudgetId, x => x);

        result.Rows = budgets.Select(b =>
        {
            var money = invoices.GetValueOrDefault(b.Id);
            var billed = money?.Billed ?? b.TotalBilled;
            var collected = money?.Paid ?? 0m;

            return new Dictionary<string, object?>
            {
                ["reference"] = b.Reference,
                ["year"] = b.FinancialYear,
                ["periodTo"] = b.PeriodTo,
                ["budget"] = b.TotalBudget,
                ["actual"] = b.TotalActual,
                ["billed"] = RealEstateMapper.Money(billed),
                ["collected"] = RealEstateMapper.Money(collected),

                // Under-recovery is measured against actual cost, not against the budget. Cost the
                // landlord could not recover is the number that matters to the landlord.
                ["shortfall"] = RealEstateMapper.Money(Math.Max(0m, b.TotalActual - billed)),
                ["recovery"] = RealEstateMapper.Percent(billed, b.TotalActual),
                ["reconciled"] = b.IsReconciled ? "Yes" : "No",
            };
        }).ToList();

        Total(result);
    }
}
