using Microsoft.EntityFrameworkCore;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>Society, facility, construction and finance reports.</summary>
public partial class RealEstateReportService
{
    private async Task SocietyBillingAsync(ReportResultDto result, ReportRequestDto request, DateOnly from, DateOnly to)
    {
        result.Columns =
        [
            Text("month", "Period", 120),
            Number("bills", "Bills"),
            Money("maintenance", "Maintenance"),
            Money("utilities", "Utilities"),
            Money("other", "Other charges"),
            Money("penalties", "Penalties"),
            Money("arrears", "Arrears b/f"),
            Money("total", "Billed"),
            Money("collected", "Collected"),
            Money("outstanding", "Outstanding"),
            Pct("recovery", "Recovery"),
        ];

        var bills = await Db.MaintenanceBills.ForCompany(Tenant)
            .WhereIf(request.SocietyId.HasValue, b => b.SocietyId == request.SocietyId)
            .Where(b => b.IssuedOn >= from && b.IssuedOn <= to)
            .Select(b => new
            {
                b.IssuedOn, b.MaintenanceAmount, b.UtilityAmount, b.OtherChargesAmount,
                b.PenaltyAmount, b.ArrearsBroughtForward, b.LateFeeAmount,
                b.TotalAmount, b.PaidAmount, b.Balance,
            })
            .ToListAsync();

        result.Rows = MonthsBetween(from, to).Select(m =>
        {
            var mine = bills.Where(b => b.IssuedOn.Year == m.Year && b.IssuedOn.Month == m.Month).ToList();
            var total = mine.Sum(b => b.TotalAmount);
            var paid = mine.Sum(b => b.PaidAmount);

            return new Dictionary<string, object?>
            {
                ["month"] = m.ToString("MMM yyyy"),
                ["bills"] = mine.Count,
                ["maintenance"] = RealEstateMapper.Money(mine.Sum(b => b.MaintenanceAmount)),
                ["utilities"] = RealEstateMapper.Money(mine.Sum(b => b.UtilityAmount)),
                ["other"] = RealEstateMapper.Money(mine.Sum(b => b.OtherChargesAmount)),
                ["penalties"] = RealEstateMapper.Money(mine.Sum(b => b.PenaltyAmount + b.LateFeeAmount)),
                ["arrears"] = RealEstateMapper.Money(mine.Sum(b => b.ArrearsBroughtForward)),
                ["total"] = RealEstateMapper.Money(total),
                ["collected"] = RealEstateMapper.Money(paid),
                ["outstanding"] = RealEstateMapper.Money(mine.Sum(b => b.Balance)),
                ["recovery"] = RealEstateMapper.Percent(paid, total),
            };
        }).ToList();

        result.Trend = MonthlyTrend(from, to, bills.Select(b => (b.IssuedOn, b.PaidAmount, b.TotalAmount)));

        Total(result);
    }

    private async Task ComplaintPerformanceAsync(ReportResultDto result, ReportRequestDto request, DateOnly from, DateOnly to)
    {
        result.Columns =
        [
            Text("category", "Category", 180),
            Number("raised", "Raised"),
            Number("resolved", "Resolved"),
            Number("open", "Still open"),
            Number("breached", "SLA breached"),
            Pct("breachRate", "Breach rate"),
            Number("avgHours", "Average hours to resolve", false),
            Number("reopened", "Reopened"),
            Number("avgRating", "Average rating", false),
        ];

        var complaints = await Db.Complaints.ForCompany(Tenant)
            .WhereIf(request.SocietyId.HasValue, c => c.SocietyId == request.SocietyId)
            .Where(c => c.RaisedAt >= from.ToDateTime(TimeOnly.MinValue)
                     && c.RaisedAt <= to.ToDateTime(TimeOnly.MaxValue))
            .Select(c => new
            {
                c.Category, c.Priority, c.RaisedAt, c.ResolvedAt, c.ClosedAt,
                c.SlaBreached, c.WasReopened, c.SatisfactionRating,
            })
            .ToListAsync();

        result.Rows = complaints
            .GroupBy(c => c.Category)
            .OrderByDescending(g => g.Count())
            .Select(g =>
            {
                var resolved = g.Where(c => c.ResolvedAt is not null).ToList();
                var rated = g.Where(c => c.SatisfactionRating is not null).ToList();

                return new Dictionary<string, object?>
                {
                    ["category"] = SplitCamel(g.Key.ToString()),
                    ["raised"] = g.Count(),
                    ["resolved"] = resolved.Count,
                    ["open"] = g.Count(c => c.ResolvedAt is null && c.ClosedAt is null),
                    ["breached"] = g.Count(c => c.SlaBreached),
                    ["breachRate"] = RealEstateMapper.Percent(g.Count(c => c.SlaBreached), g.Count()),
                    ["avgHours"] = resolved.Count == 0
                        ? null
                        : Math.Round(resolved.Average(c => (c.ResolvedAt!.Value - c.RaisedAt).TotalHours), 1),
                    ["reopened"] = g.Count(c => c.WasReopened),
                    ["avgRating"] = rated.Count == 0 ? null : Math.Round(rated.Average(c => c.SatisfactionRating!.Value), 1),
                };
            }).ToList();

        result.Breakdown = complaints
            .GroupBy(c => c.Priority)
            .OrderBy(g => g.Key)
            .Select(g => new BreakdownSliceDto
            {
                Label = g.Key.ToString(),
                Count = g.Count(),
                Value = g.Count(),
                Percent = RealEstateMapper.Percent(g.Count(), complaints.Count),
                Tone = g.Key == TicketPriority.Emergency ? "danger" : g.Key == TicketPriority.High ? "warning" : "neutral",
            }).ToList();

        Total(result);
    }

    private async Task WorkOrderCostsAsync(ReportResultDto result, DateOnly from, DateOnly to)
    {
        result.Columns =
        [
            Text("trade", "Trade", 170),
            Number("orders", "Work orders"),
            Money("labour", "Labour"),
            Money("materials", "Materials"),
            Money("contractor", "Contractor"),
            Money("total", "Total"),
            Money("average", "Average", false),
            Number("recharged", "Recharged"),
            Number("breached", "SLA breached"),
        ];

        var orders = await Db.WorkOrders.ForCompany(Tenant)
            .Where(w => w.RaisedAt >= from.ToDateTime(TimeOnly.MinValue)
                     && w.RaisedAt <= to.ToDateTime(TimeOnly.MaxValue))
            .Where(w => w.Status != WorkOrderStatus.Cancelled)
            .Select(w => new
            {
                w.Trade, w.LabourCost, w.MaterialCost, w.ContractorCost, w.TotalCost,
                w.IsRecharged, w.SlaBreached, w.CostBearer,
            })
            .ToListAsync();

        result.Rows = orders
            .GroupBy(w => string.IsNullOrWhiteSpace(w.Trade) ? "Unclassified" : w.Trade)
            .OrderByDescending(g => g.Sum(w => w.TotalCost))
            .Select(g => new Dictionary<string, object?>
            {
                ["trade"] = g.Key,
                ["orders"] = g.Count(),
                ["labour"] = RealEstateMapper.Money(g.Sum(w => w.LabourCost)),
                ["materials"] = RealEstateMapper.Money(g.Sum(w => w.MaterialCost)),
                ["contractor"] = RealEstateMapper.Money(g.Sum(w => w.ContractorCost)),
                ["total"] = RealEstateMapper.Money(g.Sum(w => w.TotalCost)),
                ["average"] = g.Count() == 0 ? 0m : RealEstateMapper.Money(g.Average(w => w.TotalCost)),
                ["recharged"] = g.Count(w => w.IsRecharged),
                ["breached"] = g.Count(w => w.SlaBreached),
            }).ToList();

        result.Breakdown = orders
            .GroupBy(w => w.CostBearer)
            .OrderByDescending(g => g.Sum(w => w.TotalCost))
            .Select(g => new BreakdownSliceDto
            {
                Label = SplitCamel(g.Key.ToString()),
                Count = g.Count(),
                Value = RealEstateMapper.Money(g.Sum(w => w.TotalCost)),
                Percent = RealEstateMapper.Percent(g.Sum(w => w.TotalCost), orders.Sum(w => w.TotalCost)),
            }).ToList();

        Total(result);
    }

    private async Task MeterConsumptionAsync(ReportResultDto result, ReportRequestDto request, DateOnly from, DateOnly to)
    {
        result.Columns =
        [
            Text("meter", "Meter", 140),
            Tag("kind", "Utility"),
            Text("location", "Location", 180),
            Number("readings", "Readings"),
            Number("consumption", "Consumption"),
            Number("average", "Average per reading", false),
            Number("implausible", "Flagged"),
            Date("lastRead", "Last read"),
            Text("commonArea", "Common area", 120),
            Text("faulty", "Faulty", 90),
        ];

        var meters = await Db.Meters.ForCompany(Tenant)
            .WhereIf(request.SocietyId.HasValue, m => m.SocietyId == request.SocietyId)
            .OrderBy(m => m.Kind)
            .ThenBy(m => m.MeterNumber)
            .ToListAsync();

        var meterIds = meters.Select(m => m.Id).ToList();

        var readings = meterIds.Count == 0
            ? []
            : await Db.MeterReadings.ForCompany(Tenant)
                .Where(r => meterIds.Contains(r.MeterId) && r.ReadingDate >= from && r.ReadingDate <= to)
                .Select(r => new { r.MeterId, r.Consumption, r.IsImplausible })
                .ToListAsync();

        result.Rows = meters.Select(m =>
        {
            var mine = readings.Where(r => r.MeterId == m.Id).ToList();

            return new Dictionary<string, object?>
            {
                ["meter"] = m.MeterNumber,
                ["kind"] = m.Kind.ToString(),
                ["location"] = m.Location ?? "—",
                ["readings"] = mine.Count,
                ["consumption"] = RealEstateMapper.Money(mine.Sum(r => r.Consumption)),
                ["average"] = mine.Count == 0 ? null : RealEstateMapper.Money(mine.Average(r => r.Consumption)),
                ["implausible"] = mine.Count(r => r.IsImplausible),
                ["lastRead"] = m.LastReadOn,
                ["commonArea"] = m.IsCommonArea ? "Yes" : "No",
                ["faulty"] = m.IsFaulty ? "Yes" : "No",
            };
        }).ToList();

        // Bulk supply against the sum of the sub-meters is the only number that shows theft or
        // leakage. Presented as a slice so it sits next to the consumption it belongs to.
        var bulk = readings.Where(r => meters.Any(m => m.Id == r.MeterId && m.Kind == MeterKind.BulkSupply))
            .Sum(r => r.Consumption);

        var submetered = readings.Where(r => meters.Any(m => m.Id == r.MeterId
                                                          && m.Kind != MeterKind.BulkSupply && !m.IsCommonArea))
            .Sum(r => r.Consumption);

        var commonArea = readings.Where(r => meters.Any(m => m.Id == r.MeterId && m.IsCommonArea))
            .Sum(r => r.Consumption);

        if (bulk > 0m)
        {
            var unexplained = bulk - submetered - commonArea;

            result.Breakdown =
            [
                new BreakdownSliceDto { Label = "Billed to units", Value = RealEstateMapper.Money(submetered), Percent = RealEstateMapper.Percent(submetered, bulk), Tone = "positive" },
                new BreakdownSliceDto { Label = "Common area", Value = RealEstateMapper.Money(commonArea), Percent = RealEstateMapper.Percent(commonArea, bulk), Tone = "neutral" },
                new BreakdownSliceDto { Label = "Unexplained loss", Value = RealEstateMapper.Money(Math.Max(0m, unexplained)), Percent = RealEstateMapper.Percent(Math.Max(0m, unexplained), bulk), Tone = "danger" },
            ];
        }

        Total(result);
    }

    private async Task GateMovementAsync(ReportResultDto result, ReportRequestDto request, DateOnly from, DateOnly to)
    {
        result.Columns =
        [
            Text("day", "Date", 120),
            Number("entries", "Entries"),
            Number("guests", "Guests"),
            Number("deliveries", "Deliveries"),
            Number("staff", "Domestic staff"),
            Number("contractors", "Contractors"),
            Number("denied", "Denied"),
            Number("stillInside", "Not checked out"),
            Number("offline", "Recorded offline"),
        ];

        var entries = await Db.GateEntries.ForCompany(Tenant)
            .WhereIf(request.SocietyId.HasValue, g => g.SocietyId == request.SocietyId)
            .Where(g => g.CheckedInAt != null
                     && g.CheckedInAt >= from.ToDateTime(TimeOnly.MinValue)
                     && g.CheckedInAt <= to.ToDateTime(TimeOnly.MaxValue))
            .Select(g => new { g.CheckedInAt, g.CheckedOutAt, g.Kind, g.IsDenied, g.WasOffline, g.PersonCount })
            .ToListAsync();

        result.Rows = entries
            .GroupBy(g => DateOnly.FromDateTime(g.CheckedInAt!.Value))
            .OrderByDescending(g => g.Key)
            .Select(g => new Dictionary<string, object?>
            {
                ["day"] = g.Key,
                ["entries"] = g.Count(),
                ["guests"] = g.Where(e => e.Kind == VisitorKind.Guest).Sum(e => Math.Max(1, e.PersonCount)),
                ["deliveries"] = g.Count(e => e.Kind == VisitorKind.Delivery),
                ["staff"] = g.Count(e => e.Kind == VisitorKind.DomesticStaff),
                ["contractors"] = g.Count(e => e.Kind is VisitorKind.Contractor or VisitorKind.ServiceProvider),
                ["denied"] = g.Count(e => e.IsDenied),
                ["stillInside"] = g.Count(e => e.CheckedOutAt is null),
                ["offline"] = g.Count(e => e.WasOffline),
            }).ToList();

        result.Breakdown = entries
            .GroupBy(g => g.Kind)
            .OrderByDescending(g => g.Count())
            .Select(g => new BreakdownSliceDto
            {
                Label = SplitCamel(g.Key.ToString()),
                Count = g.Count(),
                Value = g.Count(),
                Percent = RealEstateMapper.Percent(g.Count(), entries.Count),
            }).ToList();

        Total(result);
    }

    // ═══ Construction ════════════════════════════════════════════════════════

    private async Task CostAgainstBudgetAsync(ReportResultDto result, ReportRequestDto request)
    {
        result.Columns =
        [
            Text("package", "Work package", 240),
            Tag("kind", "Kind"),
            Money("budget", "Budget"),
            Money("committed", "Committed"),
            Money("actual", "Actual"),
            Money("forecast", "Forecast"),
            Money("variance", "Variance"),
            Pct("variancePct", "Variance %"),
            Pct("progress", "Progress"),
            Money("earnedValue", "Earned value"),
            Number("cpi", "CPI", false),
        ];

        var nodes = await Db.WbsNodes.ForCompany(Tenant)
            .WhereIf(request.ProjectId.HasValue, n => n.ConstructionProjectId == request.ProjectId)
            .OrderBy(n => n.Path)
            .ThenBy(n => n.SortOrder)
            .ToListAsync();

        result.Rows = nodes.Select(n =>
        {
            var forecast = n.ForecastAmount > 0m ? n.ForecastAmount : n.BudgetAmount;
            var variance = RealEstateMapper.Money(n.BudgetAmount - forecast);

            return new Dictionary<string, object?>
            {
                ["package"] = new string(' ', Math.Max(0, n.Depth) * 2) + n.Name,
                ["kind"] = n.Kind.ToString(),
                ["budget"] = n.BudgetAmount,
                ["committed"] = n.CommittedAmount,
                ["actual"] = n.ActualAmount,
                ["forecast"] = forecast,
                ["variance"] = variance,
                ["variancePct"] = RealEstateMapper.Percent(variance, n.BudgetAmount),
                ["progress"] = n.ProgressPercent,
                ["earnedValue"] = n.EarnedValue,

                // Cost performance index: earned value over actual cost. Below one means every
                // currency unit spent has bought less than a unit of progress.
                ["cpi"] = n.ActualAmount <= 0m ? null : Math.Round(n.EarnedValue / n.ActualAmount, 2),
            };
        }).ToList();

        Total(result);
    }

    private async Task CertificateRegisterAsync(ReportResultDto result, ReportRequestDto request, DateOnly from, DateOnly to)
    {
        result.Columns =
        [
            Text("number", "Certificate", 140),
            Tag("direction", "Direction"),
            Date("issuedOn", "Issued"),
            Date("periodTo", "Period to"),
            Money("grossToDate", "Gross to date", false),
            Money("previously", "Previously certified", false),
            Money("thisPeriod", "This certificate"),
            Money("retention", "Retention"),
            Money("advanceRecovery", "Advance recovery"),
            Money("contraCharges", "Contra charges"),
            Money("netPayable", "Net payable"),
            Money("paid", "Paid"),
            Tag("status", "Status"),
            Date("dueDate", "Due"),
        ];

        var certificates = await Db.InterimPaymentCertificates.ForCompany(Tenant)
            .WhereIf(request.ProjectId.HasValue, c => c.ConstructionProjectId == request.ProjectId)
            .Where(c => c.IssuedOn >= from && c.IssuedOn <= to)
            .OrderByDescending(c => c.IssuedOn)
            .ToListAsync();

        result.Rows = certificates.Select(c => new Dictionary<string, object?>
        {
            ["number"] = c.CertificateNumber,
            ["direction"] = c.Direction,
            ["issuedOn"] = c.IssuedOn,
            ["periodTo"] = c.PeriodTo,
            ["grossToDate"] = c.GrossValueToDate,
            ["previously"] = c.PreviouslyCertified,
            ["thisPeriod"] = c.ThisCertificateGross,
            ["retention"] = c.RetentionThisCertificate,
            ["advanceRecovery"] = c.AdvanceRecovery,
            ["contraCharges"] = c.ContraCharges + c.Penalties + c.OtherDeductions,
            ["netPayable"] = c.NetPayable,
            ["paid"] = c.PaidAmount,
            ["status"] = c.Status.ToString(),
            ["dueDate"] = c.DueDate,
        }).ToList();

        Total(result);
    }

    private async Task VariationRegisterAsync(ReportResultDto result, ReportRequestDto request)
    {
        result.Columns =
        [
            Text("number", "Variation", 130),
            Text("title", "Title", 260),
            Tag("origin", "Origin"),
            Tag("status", "Status"),
            Date("raisedOn", "Raised"),
            Money("addition", "Addition"),
            Money("omission", "Omission"),
            Money("net", "Net"),
            Number("timeImpact", "Days"),
            Date("approvedOn", "Approved"),
            Text("clientApproved", "Client approved", 140),
        ];

        var variations = await Db.VariationOrders.ForCompany(Tenant)
            .WhereIf(request.ProjectId.HasValue, v => v.ConstructionProjectId == request.ProjectId)
            .OrderByDescending(v => v.RaisedOn)
            .ToListAsync();

        result.Rows = variations.Select(v => new Dictionary<string, object?>
        {
            ["number"] = v.VariationNumber,
            ["title"] = v.Title,
            ["origin"] = SplitCamel(v.Origin.ToString()),
            ["status"] = v.Status.ToString(),
            ["raisedOn"] = v.RaisedOn,
            ["addition"] = v.AdditionAmount,
            ["omission"] = v.OmissionAmount,
            ["net"] = v.NetAmount,
            ["timeImpact"] = v.TimeImpactDays,
            ["approvedOn"] = v.ApprovedOn,
            ["clientApproved"] = v.ClientApproved ? "Yes" : "No",
        }).ToList();

        result.Breakdown = variations
            .GroupBy(v => v.Origin)
            .OrderByDescending(g => Math.Abs(g.Sum(v => v.NetAmount)))
            .Select(g => new BreakdownSliceDto
            {
                Label = SplitCamel(g.Key.ToString()),
                Count = g.Count(),
                Value = RealEstateMapper.Money(g.Sum(v => v.NetAmount)),
                Percent = RealEstateMapper.Percent(
                    Math.Abs(g.Sum(v => v.NetAmount)), variations.Sum(v => Math.Abs(v.NetAmount))),
                Tone = g.Key is VariationOrigin.SiteCondition or VariationOrigin.ErrorCorrection ? "warning" : "neutral",
            }).ToList();

        Total(result);
    }

    private async Task SubcontractorLiabilityAsync(ReportResultDto result, ReportRequestDto request)
    {
        result.Columns =
        [
            Text("reference", "Subcontract", 140),
            Text("name", "Package", 220),
            Text("contractor", "Contractor", 190),
            Tag("status", "Status"),
            Money("contractValue", "Contract"),
            Money("variations", "Variations"),
            Money("revised", "Revised value"),
            Money("certified", "Certified"),
            Money("paid", "Paid"),
            Money("due", "Due"),
            Money("retentionHeld", "Retention held"),
            Money("advanceOutstanding", "Advance outstanding"),
            Pct("progress", "Progress"),
        ];

        var subcontracts = await Db.Subcontracts.ForCompany(Tenant)
            .WhereIf(request.ProjectId.HasValue, s => s.ConstructionProjectId == request.ProjectId)
            .OrderByDescending(s => s.RevisedValue)
            .ToListAsync();

        var contractorIds = subcontracts.Select(s => s.ContractorId).Distinct().ToList();

        var contractors = contractorIds.Count == 0
            ? []
            : await Db.Contractors.ForCompany(Tenant)
                .Where(c => contractorIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name);

        result.Rows = subcontracts.Select(s => new Dictionary<string, object?>
        {
            ["reference"] = s.Reference,
            ["name"] = s.Name,
            ["contractor"] = contractors.GetValueOrDefault(s.ContractorId, "—"),
            ["status"] = SplitCamel(s.Status.ToString()),
            ["contractValue"] = s.ContractValue,
            ["variations"] = s.ApprovedVariations,
            ["revised"] = s.RevisedValue > 0m ? s.RevisedValue : s.ContractValue,
            ["certified"] = s.CertifiedToDate,
            ["paid"] = s.PaidToDate,
            ["due"] = RealEstateMapper.Money(Math.Max(0m, s.CertifiedToDate - s.PaidToDate)),
            ["retentionHeld"] = RealEstateMapper.Money(s.RetentionHeld - s.RetentionReleased),
            ["advanceOutstanding"] = RealEstateMapper.Money(Math.Max(0m, s.AdvancePaid - s.AdvanceRecovered)),
            ["progress"] = s.ProgressPercent,
        }).ToList();

        Total(result);
    }

    private async Task RetentionLedgerAsync(ReportResultDto result, ReportRequestDto request)
    {
        result.Columns =
        [
            Date("entryDate", "Date"),
            Tag("direction", "Direction"),
            Tag("movement", "Movement"),
            Text("party", "Party", 200),
            Money("amount", "Amount"),
            Money("balance", "Balance", false),
            Date("dueForRelease", "Due for release"),
            Text("note", "Note", 220),
        ];

        var entries = await Db.RetentionLedgerEntries.ForCompany(Tenant)
            .WhereIf(request.ProjectId.HasValue, e => e.ConstructionProjectId == request.ProjectId)
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync();

        var names = await PartyNamesAsync(entries.Where(e => e.PartyId != null).Select(e => e.PartyId!.Value));

        result.Rows = entries.Select(e => new Dictionary<string, object?>
        {
            ["entryDate"] = e.EntryDate,
            ["direction"] = e.Direction,
            ["movement"] = SplitCamel(e.Movement.ToString()),
            ["party"] = e.PartyId is null ? "—" : names.GetValueOrDefault(e.PartyId.Value, "—"),
            ["amount"] = e.Amount,
            ["balance"] = e.RunningBalance,
            ["dueForRelease"] = e.DueForReleaseOn,
            ["note"] = e.Note ?? "—",
        }).ToList();

        Total(result);
    }

    private async Task DelayRegisterAsync(ReportResultDto result, ReportRequestDto request)
    {
        result.Columns =
        [
            Text("reference", "Event", 130),
            Text("cause", "Cause", 260),
            Date("startedOn", "From"),
            Date("endedOn", "To"),
            Number("days", "Days"),
            Text("excusable", "Excusable", 110),
            Text("compensable", "Compensable", 120),
            Tag("responsible", "Responsible"),
            Money("costImpact", "Cost impact"),
            Text("criticalPath", "On critical path", 140),
            Text("notified", "Notified", 100),
        ];

        var delays = await Db.DelayEvents.ForCompany(Tenant)
            .WhereIf(request.ProjectId.HasValue, d => d.ConstructionProjectId == request.ProjectId)
            .OrderByDescending(d => d.StartedOn)
            .ToListAsync();

        result.Rows = delays.Select(d => new Dictionary<string, object?>
        {
            ["reference"] = d.Reference,
            ["cause"] = d.Cause,
            ["startedOn"] = d.StartedOn,
            ["endedOn"] = d.EndedOn,
            ["days"] = d.DelayDays,
            ["excusable"] = d.IsExcusable ? "Yes" : "No",
            ["compensable"] = d.IsCompensable ? "Yes" : "No",
            ["responsible"] = SplitCamel(d.ResponsibleParty.ToString()),
            ["costImpact"] = d.CostImpact ?? 0m,
            ["criticalPath"] = d.AffectsCriticalPath ? "Yes" : "No",

            // Whether a delay was notified in time is usually what decides the entitlement, so it
            // sits on the register rather than in a file somebody has to go and find.
            ["notified"] = d.IsNotified ? "Yes" : "No",
        }).ToList();

        result.Breakdown = delays
            .GroupBy(d => d.ResponsibleParty)
            .OrderByDescending(g => g.Sum(d => d.DelayDays))
            .Select(g => new BreakdownSliceDto
            {
                Label = SplitCamel(g.Key.ToString()),
                Count = g.Count(),
                Value = g.Sum(d => d.DelayDays),
                Percent = RealEstateMapper.Percent(g.Sum(d => d.DelayDays), delays.Sum(d => d.DelayDays)),
            }).ToList();

        Total(result);
    }

    // ═══ Finance ═════════════════════════════════════════════════════════════

    private async Task EscrowMovementAsync(ReportResultDto result, ReportRequestDto request, DateOnly from, DateOnly to)
    {
        result.Columns =
        [
            Date("entryDate", "Date"),
            Tag("kind", "Movement"),
            Text("description", "Description", 280),
            Money("credit", "In"),
            Money("debit", "Out"),
            Money("balance", "Balance", false),
            Text("receipt", "Receipt", 130),
            Text("reconciled", "Reconciled", 110),
        ];

        var accounts = await Db.ProjectBankAccounts.ForCompany(Tenant)
            .WhereIf(request.ProjectId.HasValue, a => a.ProjectId == request.ProjectId)
            .Where(a => a.Kind == ProjectAccountKind.Escrow)
            .Select(a => a.Id)
            .ToListAsync();

        if (accounts.Count == 0) { Total(result); return; }

        var entries = await Db.EscrowLedgerEntries.ForCompany(Tenant)
            .Where(e => accounts.Contains(e.ProjectBankAccountId) && e.EntryDate >= from && e.EntryDate <= to)
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync();

        var receiptIds = entries.Where(e => e.ReceiptId != null).Select(e => e.ReceiptId!.Value).Distinct().ToList();

        var receipts = receiptIds.Count == 0
            ? []
            : await Db.Receipts.ForCompany(Tenant)
                .Where(r => receiptIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.ReceiptNumber);

        result.Rows = entries.Select(e => new Dictionary<string, object?>
        {
            ["entryDate"] = e.EntryDate,
            ["kind"] = SplitCamel(e.Kind.ToString()),
            ["description"] = e.Description ?? SplitCamel(e.Kind.ToString()),
            ["credit"] = e.CreditAmount,
            ["debit"] = e.DebitAmount,
            ["balance"] = e.RunningBalance,
            ["receipt"] = e.ReceiptId is null ? "—" : receipts.GetValueOrDefault(e.ReceiptId.Value, "—"),
            ["reconciled"] = e.IsReconciled ? "Yes" : "No",
        }).ToList();

        Total(result);
    }

    private async Task ProfitabilityAsync(ReportResultDto result, ReportRequestDto request)
    {
        result.Columns =
        [
            Text("unit", "Unit", 120),
            Text("block", "Block", 150),
            Tag("subType", "Type"),
            Number("area", "Area", false),
            Money("listPrice", "List price"),
            Money("discount", "Discount"),
            Money("revenue", "Revenue"),
            Money("cost", "Allocated cost"),
            Money("margin", "Margin"),
            Pct("marginPct", "Margin %"),
            Money("realisationPerSqFt", "Realisation / sq ft", false),
            Money("costPerSqFt", "Cost / sq ft", false),
        ];

        if (request.ProjectId is null)
            throw new InvalidOperationException("Unit profitability has to be run for one project at a time.");

        var latest = await Db.UnitProfitabilities.ForCompany(Tenant)
            .Where(p => p.ProjectId == request.ProjectId)
            .Select(p => (DateOnly?)p.AsOfDate)
            .MaxAsync();

        if (latest is null)
            throw new InvalidOperationException(
                "Costs have not been allocated for this project yet, so there is no profitability to report. "
                + "Run the cost allocation first.");

        var rows = await Db.UnitProfitabilities.ForCompany(Tenant)
            .Where(p => p.ProjectId == request.ProjectId && p.AsOfDate == latest)
            .OrderBy(p => p.MarginPercent)
            .ToListAsync();

        var units = await UnitNumbersAsync(rows.Select(p => p.UnitId));

        var nodeIds = rows.Where(p => p.ProjectNodeId != null).Select(p => p.ProjectNodeId!.Value).Distinct().ToList();

        var nodes = nodeIds.Count == 0
            ? []
            : await Db.ProjectNodes.ForCompany(Tenant)
                .Where(n => nodeIds.Contains(n.Id)).ToDictionaryAsync(n => n.Id, n => n.Name);

        result.Subtitle = $"As allocated on {latest:dd MMM yyyy}.";

        result.Rows = rows.Select(p => new Dictionary<string, object?>
        {
            ["unit"] = units.GetValueOrDefault(p.UnitId, "—"),
            ["block"] = p.ProjectNodeId is null ? "—" : nodes.GetValueOrDefault(p.ProjectNodeId.Value, "—"),
            ["subType"] = p.SubType is null ? "—" : SplitCamel(p.SubType.Value.ToString()),
            ["area"] = p.AreaSqFt,
            ["listPrice"] = p.ListPrice,
            ["discount"] = p.DiscountGiven,
            ["revenue"] = p.TotalRevenue,
            ["cost"] = p.TotalCost,
            ["margin"] = p.GrossMargin,
            ["marginPct"] = p.MarginPercent,
            ["realisationPerSqFt"] = p.RealisationPerSqFt,
            ["costPerSqFt"] = p.CostPerSqFt,
        }).ToList();

        result.Breakdown = rows
            .Where(p => p.SubType is not null)
            .GroupBy(p => p.SubType!.Value)
            .OrderByDescending(g => g.Sum(p => p.GrossMargin))
            .Select(g => new BreakdownSliceDto
            {
                Label = SplitCamel(g.Key.ToString()),
                Count = g.Count(),
                Value = RealEstateMapper.Money(g.Sum(p => p.GrossMargin)),
                Percent = RealEstateMapper.Percent(g.Sum(p => p.GrossMargin), rows.Sum(p => p.GrossMargin)),
                Tone = g.Sum(p => p.GrossMargin) < 0m ? "danger" : "neutral",
            }).ToList();

        Total(result);
    }

    private async Task LandownerPositionAsync(ReportResultDto result, ReportRequestDto request)
    {
        result.Columns =
        [
            Text("reference", "Venture", 130),
            Text("name", "Name", 220),
            Text("project", "Project", 180),
            Tag("basis", "Basis"),
            Pct("landownerShare", "Landowner share"),
            Money("entitlement", "Entitlement accrued"),
            Money("paid", "Paid"),
            Money("balance", "Balance"),
            Number("allocatedUnits", "Units allocated"),
            Money("allocatedValue", "Allocated value"),
        ];

        var ventures = await Db.JointVentures.ForCompany(Tenant)
            .WhereIf(request.ProjectId.HasValue, v => v.ProjectId == request.ProjectId)
            .OrderByDescending(v => v.LandownerBalance)
            .ToListAsync();

        var projects = await ProjectNamesAsync(ventures.Select(v => (Guid?)v.ProjectId));

        var allocations = await Db.LandownerAllocations.ForCompany(Tenant)
            .Where(a => ventures.Select(v => v.Id).Contains(a.JointVentureId) && a.Status == "Allocated")
            .GroupBy(a => a.JointVentureId)
            .Select(g => new { VentureId = g.Key, Count = g.Count(), Value = g.Sum(a => a.NotionalValue) })
            .ToDictionaryAsync(x => x.VentureId, x => x);

        result.Rows = ventures.Select(v =>
        {
            var allocation = allocations.GetValueOrDefault(v.Id);

            return new Dictionary<string, object?>
            {
                ["reference"] = v.Reference,
                ["name"] = v.Name,
                ["project"] = projects.GetValueOrDefault(v.ProjectId, "—"),
                ["basis"] = SplitCamel(v.Basis.ToString()),
                ["landownerShare"] = v.LandownerSharePercent,
                ["entitlement"] = v.TotalLandownerEntitlement,
                ["paid"] = v.TotalLandownerPaid,
                ["balance"] = v.LandownerBalance,
                ["allocatedUnits"] = allocation?.Count ?? 0,
                ["allocatedValue"] = RealEstateMapper.Money(allocation?.Value ?? 0m),
            };
        }).ToList();

        Total(result);
    }

    private async Task InvestorSummaryAsync(ReportResultDto result, ReportRequestDto request)
    {
        result.Columns =
        [
            Text("reference", "Investor", 130),
            Text("name", "Name", 210),
            Text("type", "Type", 120),
            Money("committed", "Committed"),
            Money("contributed", "Contributed"),
            Money("undrawn", "Undrawn"),
            Money("distributed", "Distributed"),
            Pct("share", "Share"),
            Pct("preferred", "Preferred return"),
            Number("moic", "Multiple", false),
            Date("exitDate", "Exited"),
        ];

        var investors = await Db.Investors.ForCompany(Tenant)
            .WhereIf(request.ProjectId.HasValue, i => i.ProjectId == request.ProjectId)
            .OrderByDescending(i => i.CommittedAmount)
            .ToListAsync();

        var names = await PartyNamesAsync(investors.Select(i => i.PartyId));

        var contributions = await Db.Contributions.ForCompany(Tenant)
            .Where(c => investors.Select(i => i.Id).Contains(c.InvestorId))
            .GroupBy(c => c.InvestorId)
            .Select(g => new { InvestorId = g.Key, Amount = g.Sum(c => c.Amount) })
            .ToDictionaryAsync(x => x.InvestorId, x => x.Amount);

        var distributions = await Db.Distributions.ForCompany(Tenant)
            .Where(d => investors.Select(i => i.Id).Contains(d.InvestorId))
            .GroupBy(d => d.InvestorId)
            .Select(g => new { InvestorId = g.Key, Amount = g.Sum(d => d.NetAmount) })
            .ToDictionaryAsync(x => x.InvestorId, x => x.Amount);

        result.Rows = investors.Select(i =>
        {
            var contributed = contributions.GetValueOrDefault(i.Id);
            var distributed = distributions.GetValueOrDefault(i.Id);

            return new Dictionary<string, object?>
            {
                ["reference"] = i.Reference,
                ["name"] = names.GetValueOrDefault(i.PartyId, "—"),
                ["type"] = i.InvestmentType,
                ["committed"] = i.CommittedAmount,
                ["contributed"] = RealEstateMapper.Money(contributed),
                ["undrawn"] = RealEstateMapper.Money(Math.Max(0m, i.CommittedAmount - contributed)),
                ["distributed"] = RealEstateMapper.Money(distributed),
                ["share"] = i.SharePercent,
                ["preferred"] = i.PreferredReturnPercent,
                ["moic"] = contributed <= 0m ? null : Math.Round(distributed / contributed, 2),
                ["exitDate"] = i.ExitDate,
            };
        }).ToList();

        Total(result);
    }

    private async Task WithholdingRegisterAsync(ReportResultDto result, DateOnly from, DateOnly to)
    {
        result.Columns =
        [
            Text("reference", "Reference", 130),
            Tag("kind", "Kind"),
            Text("party", "Deducted from", 210),
            Text("taxNumber", "Tax number", 140),
            Text("filer", "Filer", 90),
            Date("deductedOn", "Deducted"),
            Money("gross", "Gross"),
            Pct("rate", "Rate"),
            Money("withheld", "Withheld"),
            Text("deposited", "Deposited", 110),
            Text("challan", "Challan", 140),
            Text("certificate", "Certificate issued", 150),
        ];

        var records = await Db.WithholdingRecords.ForCompany(Tenant)
            .Where(w => w.DeductedOn >= from && w.DeductedOn <= to)
            .OrderByDescending(w => w.DeductedOn)
            .ToListAsync();

        var names = await PartyNamesAsync(records.Select(w => w.PartyId));

        result.Rows = records.Select(w => new Dictionary<string, object?>
        {
            ["reference"] = w.Reference,
            ["kind"] = SplitCamel(w.Kind.ToString()),
            ["party"] = names.GetValueOrDefault(w.PartyId, "—"),
            ["taxNumber"] = w.TaxNumber ?? "—",
            ["filer"] = w.IsFiler ? "Yes" : "No",
            ["deductedOn"] = w.DeductedOn,
            ["gross"] = w.GrossAmount,
            ["rate"] = w.Rate,
            ["withheld"] = w.WithheldAmount,
            ["deposited"] = w.IsDeposited ? "Yes" : "No",
            ["challan"] = w.ChallanNumber ?? "—",
            ["certificate"] = w.CertificateIssued ? "Yes" : "No",
        }).ToList();

        var undeposited = records.Where(w => !w.IsDeposited).ToList();

        result.Breakdown =
        [
            new BreakdownSliceDto
            {
                Label = "Deposited",
                Count = records.Count - undeposited.Count,
                Value = RealEstateMapper.Money(records.Where(w => w.IsDeposited).Sum(w => w.WithheldAmount)),
                Percent = RealEstateMapper.Percent(records.Count - undeposited.Count, records.Count),
                Tone = "positive",
            },
            new BreakdownSliceDto
            {
                Label = "Still to deposit",
                Count = undeposited.Count,
                Value = RealEstateMapper.Money(undeposited.Sum(w => w.WithheldAmount)),
                Percent = RealEstateMapper.Percent(undeposited.Count, records.Count),
                Tone = "danger",
            },
        ];

        Total(result);
    }

    private async Task ApprovalStatusAsync(ReportResultDto result, ReportRequestDto request)
    {
        result.Columns =
        [
            Text("reference", "Reference", 130),
            Tag("kind", "Approval"),
            Text("authority", "Authority", 190),
            Tag("state", "State"),
            Text("number", "Approval number", 170),
            Date("appliedOn", "Applied"),
            Date("grantedOn", "Granted"),
            Date("validUntil", "Valid until"),
            Number("daysToExpiry", "Days", false),
            Money("cost", "Cost"),
            Text("mandatory", "Mandatory", 110),
            Text("blocking", "Blocking", 100),
        ];

        var records = await Db.ApprovalRecords.ForCompany(Tenant)
            .WhereIf(request.ProjectId.HasValue, a => a.ProjectId == request.ProjectId)
            .OrderBy(a => a.State)
            .ThenBy(a => a.ValidUntil ?? DateOnly.MaxValue)
            .ToListAsync();

        result.Rows = records.Select(a => new Dictionary<string, object?>
        {
            ["reference"] = a.Reference,
            ["kind"] = SplitCamel(a.Kind.ToString()),
            ["authority"] = a.Authority ?? "—",
            ["state"] = SplitCamel(a.State.ToString()),
            ["number"] = a.ApprovalNumber ?? a.ApplicationNumber ?? "—",
            ["appliedOn"] = a.AppliedOn,
            ["grantedOn"] = a.GrantedOn,
            ["validUntil"] = a.ValidUntil,
            ["daysToExpiry"] = a.ValidUntil is null ? null : a.ValidUntil.Value.DayNumber - Today.DayNumber,
            ["cost"] = a.TotalCost,
            ["mandatory"] = a.IsMandatory ? "Yes" : "No",
            ["blocking"] = a.IsBlocking ? "Yes" : "No",
        }).ToList();

        result.Breakdown = records
            .GroupBy(a => a.State)
            .OrderByDescending(g => g.Count())
            .Select(g => new BreakdownSliceDto
            {
                Label = SplitCamel(g.Key.ToString()),
                Count = g.Count(),
                Value = g.Count(),
                Percent = RealEstateMapper.Percent(g.Count(), records.Count),
                Tone = g.Key switch
                {
                    ApprovalState.Granted or ApprovalState.GrantedWithConditions => "positive",
                    ApprovalState.Rejected or ApprovalState.Expired => "danger",
                    ApprovalState.QueryRaised or ApprovalState.RenewalDue => "warning",
                    _ => "neutral",
                },
            }).ToList();

        Total(result);
    }
}
