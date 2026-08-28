using Microsoft.EntityFrameworkCore;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>Sales, inventory and money reports.</summary>
public partial class RealEstateReportService
{
    private static readonly BookingStatus[] LiveBookings =
    [
        BookingStatus.Confirmed, BookingStatus.AgreementSigned, BookingStatus.Defaulting,
        BookingStatus.PossessionOffered, BookingStatus.Possessed, BookingStatus.Completed,
    ];

    private async Task SalesRegisterAsync(ReportResultDto result, ReportRequestDto request, DateOnly from, DateOnly to)
    {
        result.Columns =
        [
            Text("reference", "Booking", 130),
            Date("bookingDate", "Booked"),
            Text("customer", "Customer", 200),
            Text("project", "Project", 180),
            Text("unit", "Unit", 110),
            Number("area", "Area", false),
            Money("rate", "Rate", false),
            Money("listPrice", "List price"),
            Money("discount", "Discount"),
            Money("consideration", "Consideration"),
            Money("paid", "Collected"),
            Money("outstanding", "Outstanding"),
            Pct("collected", "Collected %"),
            Tag("status", "Status"),
            Text("source", "Source", 130),
        ];

        var bookings = await Db.Bookings.ForCompany(Tenant)
            .WhereIf(request.ProjectId.HasValue, b => b.ProjectId == request.ProjectId)
            .Where(b => b.BookingDate >= from && b.BookingDate <= to)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();

        var names = await PartyNamesAsync(bookings.Select(b => b.PrimaryApplicantPartyId));
        var projects = await ProjectNamesAsync(bookings.Select(b => (Guid?)b.ProjectId));
        var units = await UnitNumbersAsync(bookings.Where(b => b.UnitId != null).Select(b => b.UnitId!.Value));

        result.Rows = bookings.Select(b => new Dictionary<string, object?>
        {
            ["reference"] = b.Reference,
            ["bookingDate"] = b.BookingDate,
            ["customer"] = names.GetValueOrDefault(b.PrimaryApplicantPartyId, "—"),
            ["project"] = projects.GetValueOrDefault(b.ProjectId, "—"),
            ["unit"] = b.UnitId is null ? "—" : units.GetValueOrDefault(b.UnitId.Value, "—"),
            ["area"] = b.AreaSqFt,
            ["rate"] = b.RatePerSqFt,
            ["listPrice"] = b.ListPrice,
            ["discount"] = b.DiscountAmount,
            ["consideration"] = b.TotalConsideration,
            ["paid"] = b.TotalPaid,
            ["outstanding"] = b.Outstanding,
            ["collected"] = b.CollectionPercent,
            ["status"] = b.Status.ToString(),
            ["source"] = b.SourcingChannel.ToString(),
        }).ToList();

        var live = bookings.Where(b => LiveBookings.Contains(b.Status)).ToList();

        result.Breakdown = bookings
            .GroupBy(b => b.Status)
            .OrderByDescending(g => g.Count())
            .Select(g => new BreakdownSliceDto
            {
                Label = g.Key.ToString(),
                Count = g.Count(),
                Value = RealEstateMapper.Money(g.Sum(b => b.TotalConsideration)),
                Percent = RealEstateMapper.Percent(g.Count(), bookings.Count),
                Tone = g.Key == BookingStatus.Cancelled ? "danger" : "neutral",
            }).ToList();

        result.Trend = MonthlyTrend(from, to,
            live.Select(b => (b.BookingDate, b.TotalConsideration, 1m)));

        Total(result);
    }

    private async Task InventoryStatusAsync(ReportResultDto result, ReportRequestDto request)
    {
        result.Columns =
        [
            Text("block", "Block", 180),
            Number("total", "Units"),
            Number("available", "Available"),
            Number("held", "Held"),
            Number("booked", "Booked"),
            Number("sold", "Sold"),
            Number("blocked", "Blocked"),
            Pct("absorption", "Absorbed"),
            Money("listValue", "List value"),
            Money("availableValue", "Value available"),
        ];

        var units = await Db.Units.ForCompany(Tenant)
            .WhereIf(request.ProjectId.HasValue, u => u.ProjectId == request.ProjectId)
            .Where(u => u.IsSaleable)
            .Select(u => new
            {
                u.ProjectNodeId,
                u.Status,
                Price = u.TotalPrice > 0m ? u.TotalPrice : u.BasePrice,
            })
            .ToListAsync();

        var nodeIds = units.Where(u => u.ProjectNodeId != null).Select(u => u.ProjectNodeId!.Value).Distinct().ToList();

        var nodeNames = nodeIds.Count == 0
            ? []
            : await Db.ProjectNodes.ForCompany(Tenant)
                .Where(n => nodeIds.Contains(n.Id))
                .ToDictionaryAsync(n => n.Id, n => n.Name);

        result.Rows = units
            .GroupBy(u => u.ProjectNodeId)
            .OrderBy(g => g.Key is null ? "—" : nodeNames.GetValueOrDefault(g.Key.Value, "—"))
            .Select(g =>
            {
                var total = g.Count();
                var booked = g.Count(u => u.Status == PropertyStatus.Booked);
                var sold = g.Count(u => u.Status is PropertyStatus.Sold or PropertyStatus.Registered
                                                or PropertyStatus.Possessed);

                return new Dictionary<string, object?>
                {
                    ["block"] = g.Key is null ? "Unassigned" : nodeNames.GetValueOrDefault(g.Key.Value, "—"),
                    ["total"] = total,
                    ["available"] = g.Count(u => u.Status == PropertyStatus.Available),
                    ["held"] = g.Count(u => u.Status is PropertyStatus.Held or PropertyStatus.Reserved),
                    ["booked"] = booked,
                    ["sold"] = sold,
                    ["blocked"] = g.Count(u => u.Status is PropertyStatus.Blocked or PropertyStatus.Litigation),
                    ["absorption"] = RealEstateMapper.Percent(booked + sold, total),
                    ["listValue"] = RealEstateMapper.Money(g.Sum(u => u.Price)),
                    ["availableValue"] = RealEstateMapper.Money(
                        g.Where(u => u.Status == PropertyStatus.Available).Sum(u => u.Price)),
                };
            }).ToList();

        var all = units.Count;

        result.Breakdown = units
            .GroupBy(u => u.Status)
            .OrderByDescending(g => g.Count())
            .Select(g => new BreakdownSliceDto
            {
                Label = g.Key.ToString(),
                Count = g.Count(),
                Value = RealEstateMapper.Money(g.Sum(u => u.Price)),
                Percent = RealEstateMapper.Percent(g.Count(), all),
                Tone = g.Key switch
                {
                    PropertyStatus.Available => "positive",
                    PropertyStatus.Held or PropertyStatus.Reserved => "warning",
                    PropertyStatus.Blocked or PropertyStatus.Litigation => "danger",
                    _ => "neutral",
                },
            }).ToList();

        Total(result);
    }

    private async Task AbsorptionAsync(ReportResultDto result, ReportRequestDto request, DateOnly from, DateOnly to)
    {
        result.Columns =
        [
            Text("month", "Month", 110),
            Number("booked", "Booked"),
            Number("cancelled", "Cancelled"),
            Number("net", "Net"),
            Money("value", "Value"),
            Number("remaining", "Remaining", false),
            Number("monthsToSellOut", "Months to sell out", false),
        ];

        var bookings = await Db.Bookings.ForCompany(Tenant)
            .WhereIf(request.ProjectId.HasValue, b => b.ProjectId == request.ProjectId)
            .Where(b => b.BookingDate >= from && b.BookingDate <= to)
            .Select(b => new { b.BookingDate, b.Status, b.TotalConsideration })
            .ToListAsync();

        var available = await Db.Units.ForCompany(Tenant)
            .WhereIf(request.ProjectId.HasValue, u => u.ProjectId == request.ProjectId)
            .CountAsync(u => u.IsSaleable && u.Status == PropertyStatus.Available);

        var months = MonthsBetween(from, to);
        var rows = new List<Dictionary<string, object?>>();

        var netSoFar = new List<int>();

        foreach (var m in months)
        {
            var mine = bookings.Where(b => b.BookingDate.Year == m.Year && b.BookingDate.Month == m.Month).ToList();
            var booked = mine.Count(b => LiveBookings.Contains(b.Status));
            var cancelled = mine.Count(b => b.Status == BookingStatus.Cancelled);
            var net = booked - cancelled;

            netSoFar.Add(net);

            // Run-out is projected on the last three months rather than the whole history, because
            // a scheme's launch month tells you nothing about how fast it is selling now.
            var recent = netSoFar.TakeLast(3).ToList();
            var rate = recent.Count == 0 ? 0m : (decimal)recent.Sum() / recent.Count;

            rows.Add(new Dictionary<string, object?>
            {
                ["month"] = m.ToString("MMM yyyy"),
                ["booked"] = booked,
                ["cancelled"] = cancelled,
                ["net"] = net,
                ["value"] = RealEstateMapper.Money(mine.Where(b => LiveBookings.Contains(b.Status))
                    .Sum(b => b.TotalConsideration)),
                ["remaining"] = available,
                ["monthsToSellOut"] = rate <= 0m ? null : Math.Round(available / rate, 1),
            });
        }

        result.Rows = rows;

        result.Trend = months.Select((m, i) => new TrendPointDto
        {
            Label = m.ToString("MMM yy"),
            Date = m,
            Value = netSoFar[i],
        }).ToList();

        Total(result);
    }

    private async Task RealisationAsync(ReportResultDto result, ReportRequestDto request, DateOnly from, DateOnly to)
    {
        result.Columns =
        [
            Text("reference", "Booking", 130),
            Text("unit", "Unit", 110),
            Number("area", "Area", false),
            Money("listPrice", "List price"),
            Money("discount", "Discount"),
            Money("net", "Net price"),
            Pct("discountPct", "Discount %"),
            Money("listRate", "List rate", false),
            Money("achievedRate", "Achieved rate", false),
            Text("approvedBy", "Discount approved", 160),
        ];

        var bookings = await Db.Bookings.ForCompany(Tenant)
            .WhereIf(request.ProjectId.HasValue, b => b.ProjectId == request.ProjectId)
            .Where(b => b.BookingDate >= from && b.BookingDate <= to && b.Status != BookingStatus.Cancelled)
            .OrderByDescending(b => b.DiscountPercent)
            .ToListAsync();

        var units = await UnitNumbersAsync(bookings.Where(b => b.UnitId != null).Select(b => b.UnitId!.Value));

        var approvals = await Db.ApprovalRequests.ForCompany(Tenant)
            .Where(a => bookings.Where(b => b.DiscountApprovalRequestId != null)
                                .Select(b => b.DiscountApprovalRequestId!.Value).Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Outcome.ToString());

        result.Rows = bookings.Select(b => new Dictionary<string, object?>
        {
            ["reference"] = b.Reference,
            ["unit"] = b.UnitId is null ? "—" : units.GetValueOrDefault(b.UnitId.Value, "—"),
            ["area"] = b.AreaSqFt,
            ["listPrice"] = b.ListPrice,
            ["discount"] = b.DiscountAmount,
            ["net"] = b.NetSalePrice,
            ["discountPct"] = b.DiscountPercent,
            ["listRate"] = b.AreaSqFt <= 0m ? 0m : RealEstateMapper.Money(b.ListPrice / b.AreaSqFt),
            ["achievedRate"] = b.AreaSqFt <= 0m ? 0m : RealEstateMapper.Money(b.NetSalePrice / b.AreaSqFt),
            ["approvedBy"] = b.DiscountApprovalRequestId is null
                ? (b.DiscountAmount > 0m ? "Within authority" : "—")
                : approvals.GetValueOrDefault(b.DiscountApprovalRequestId.Value, "Pending"),
        }).ToList();

        Total(result);
    }

    private async Task CancellationsAsync(ReportResultDto result, ReportRequestDto request, DateOnly from, DateOnly to)
    {
        result.Columns =
        [
            Text("reference", "Cancellation", 140),
            Date("requestedOn", "Requested"),
            Text("customer", "Customer", 200),
            Tag("trigger", "Trigger"),
            Text("reason", "Reason", 180),
            Money("consideration", "Consideration"),
            Money("paid", "Paid"),
            Money("deduction", "Deducted"),
            Money("clawback", "Commission clawback"),
            Money("refundable", "Refundable"),
            Tag("outcome", "Outcome"),
        ];

        var cancellations = await Db.Cancellations.ForCompany(Tenant)
            .Where(c => c.RequestedOn >= from && c.RequestedOn <= to)
            .OrderByDescending(c => c.RequestedOn)
            .ToListAsync();

        var names = await PartyNamesAsync(cancellations.Select(c => c.PartyId));
        var reasons = await ReasonLabelsAsync(cancellations.Select(c => (Guid?)c.ReasonCodeId));

        result.Rows = cancellations.Select(c => new Dictionary<string, object?>
        {
            ["reference"] = c.Reference,
            ["requestedOn"] = c.RequestedOn,
            ["customer"] = names.GetValueOrDefault(c.PartyId, "—"),
            ["trigger"] = c.Trigger.ToString(),
            ["reason"] = reasons.GetValueOrDefault(c.ReasonCodeId, "—"),
            ["consideration"] = c.TotalConsideration,
            ["paid"] = c.TotalPaid,
            ["deduction"] = c.DeductionAmount + c.AdministrativeCharge,
            ["clawback"] = c.CommissionClawback,
            ["refundable"] = c.RefundableAmount,
            ["outcome"] = c.Outcome.ToString(),
        }).ToList();

        result.Breakdown = cancellations
            .GroupBy(c => c.Trigger)
            .OrderByDescending(g => g.Count())
            .Select(g => new BreakdownSliceDto
            {
                Label = g.Key.ToString(),
                Count = g.Count(),
                Value = RealEstateMapper.Money(g.Sum(c => c.TotalConsideration)),
                Percent = RealEstateMapper.Percent(g.Count(), cancellations.Count),
                Tone = g.Key is CancellationTrigger.Default or CancellationTrigger.Fraud ? "danger" : "neutral",
            }).ToList();

        Total(result);
    }

    private async Task BookingSourceAsync(ReportResultDto result, ReportRequestDto request, DateOnly from, DateOnly to)
    {
        result.Columns =
        [
            Text("source", "Source", 170),
            Number("bookings", "Bookings"),
            Number("cancelled", "Cancelled"),
            Money("value", "Value"),
            Money("collected", "Collected"),
            Money("average", "Average ticket", false),
            Pct("cancellationRate", "Cancellation rate"),
        ];

        var bookings = await Db.Bookings.ForCompany(Tenant)
            .WhereIf(request.ProjectId.HasValue, b => b.ProjectId == request.ProjectId)
            .Where(b => b.BookingDate >= from && b.BookingDate <= to)
            .Select(b => new { b.SourcingChannel, b.Status, b.TotalConsideration, b.TotalPaid })
            .ToListAsync();

        result.Rows = bookings
            .GroupBy(b => b.SourcingChannel)
            .OrderByDescending(g => g.Sum(b => b.TotalConsideration))
            .Select(g =>
            {
                var live = g.Where(b => LiveBookings.Contains(b.Status)).ToList();
                var cancelled = g.Count(b => b.Status == BookingStatus.Cancelled);

                return new Dictionary<string, object?>
                {
                    ["source"] = g.Key.ToString(),
                    ["bookings"] = live.Count,
                    ["cancelled"] = cancelled,
                    ["value"] = RealEstateMapper.Money(live.Sum(b => b.TotalConsideration)),
                    ["collected"] = RealEstateMapper.Money(live.Sum(b => b.TotalPaid)),
                    ["average"] = live.Count == 0 ? 0m : RealEstateMapper.Money(live.Average(b => b.TotalConsideration)),
                    ["cancellationRate"] = RealEstateMapper.Percent(cancelled, g.Count()),
                };
            }).ToList();

        Total(result);
    }

    // ═══ Money ═══════════════════════════════════════════════════════════════

    private async Task CollectionSummaryAsync(ReportResultDto result, ReportRequestDto request, DateOnly from, DateOnly to)
    {
        result.Columns =
        [
            Text("month", "Month", 110),
            Money("demanded", "Demanded"),
            Money("collected", "Collected"),
            Money("variance", "Variance"),
            Pct("efficiency", "Efficiency"),
            Number("receipts", "Receipts"),
        ];

        var demands = await Db.Demands.ForCompany(Tenant)
            .Where(d => d.IssuedOn >= from && d.IssuedOn <= to && d.Status != DemandStatus.Cancelled)
            .Select(d => new { d.IssuedOn, d.TotalAmount })
            .ToListAsync();

        var receipts = await Db.Receipts.ForCompany(Tenant)
            .WhereIf(request.ProjectId.HasValue, r => r.ProjectId == request.ProjectId)
            .Where(r => r.ReceivedOn >= from && r.ReceivedOn <= to && r.Status == ReceiptStatus.Posted)
            .Select(r => new { r.ReceivedOn, r.Amount })
            .ToListAsync();

        var months = MonthsBetween(from, to);

        result.Rows = months.Select(m =>
        {
            var demanded = demands.Where(d => d.IssuedOn.Year == m.Year && d.IssuedOn.Month == m.Month)
                .Sum(d => d.TotalAmount);

            var mine = receipts.Where(r => r.ReceivedOn.Year == m.Year && r.ReceivedOn.Month == m.Month).ToList();
            var collected = mine.Sum(r => r.Amount);

            return new Dictionary<string, object?>
            {
                ["month"] = m.ToString("MMM yyyy"),
                ["demanded"] = RealEstateMapper.Money(demanded),
                ["collected"] = RealEstateMapper.Money(collected),
                ["variance"] = RealEstateMapper.Money(collected - demanded),
                ["efficiency"] = RealEstateMapper.Percent(collected, demanded),
                ["receipts"] = mine.Count,
            };
        }).ToList();

        result.Trend = months.Select(m => new TrendPointDto
        {
            Label = m.ToString("MMM yy"),
            Date = m,
            Value = RealEstateMapper.Money(receipts
                .Where(r => r.ReceivedOn.Year == m.Year && r.ReceivedOn.Month == m.Month).Sum(r => r.Amount)),
            SecondaryValue = RealEstateMapper.Money(demands
                .Where(d => d.IssuedOn.Year == m.Year && d.IssuedOn.Month == m.Month).Sum(d => d.TotalAmount)),
        }).ToList();

        Total(result);
    }

    private async Task AgeingAsync(ReportResultDto result, ReportRequestDto request)
    {
        result.Columns =
        [
            Text("bucket", "Age", 140),
            Number("count", "Instalments"),
            Money("principal", "Principal"),
            Money("surcharge", "Surcharge"),
            Money("total", "Total"),
            Pct("share", "Share"),
        ];

        var overdue = await Db.Instalments.ForCompany(Tenant)
            .Where(i => i.Balance > 0m && i.DueDate != null && i.DueDate < Today)
            .Where(i => i.Status != InstalmentStatus.Waived && i.Status != InstalmentStatus.Cancelled)
            .Select(i => new { i.DueDate, i.Balance, i.SurchargeAccrued, i.SurchargePaid, i.SurchargeWaived, i.BookingId })
            .ToListAsync();

        if (request.ProjectId is not null)
        {
            var inProject = await Db.Bookings.ForCompany(Tenant)
                .Where(b => b.ProjectId == request.ProjectId)
                .Select(b => b.Id)
                .ToListAsync();

            overdue = overdue.Where(i => inProject.Contains(i.BookingId)).ToList();
        }

        var buckets = overdue
            .Select(i => new
            {
                Bucket = RealEstateMapper.AgeingBucket(RealEstateMapper.DaysOverdue(i.DueDate, Today)),
                i.Balance,
                Surcharge = i.SurchargeAccrued - i.SurchargePaid - i.SurchargeWaived,
            })
            .GroupBy(x => x.Bucket)
            .ToList();

        var grandTotal = overdue.Sum(i => i.Balance);

        // The bucket order is fixed rather than alphabetical, because ageing only reads correctly
        // oldest-last and "121+" sorts before "31" in every other order.
        var order = new[] { "Current", "1–30", "31–60", "61–90", "91–120", "121–180", "180+" };

        result.Rows = buckets
            .OrderBy(g => Array.IndexOf(order, g.Key) is var i && i >= 0 ? i : 99)
            .Select(g =>
            {
                var principal = g.Sum(x => x.Balance);

                return new Dictionary<string, object?>
                {
                    ["bucket"] = g.Key,
                    ["count"] = g.Count(),
                    ["principal"] = RealEstateMapper.Money(principal),
                    ["surcharge"] = RealEstateMapper.Money(g.Sum(x => Math.Max(0m, x.Surcharge))),
                    ["total"] = RealEstateMapper.Money(principal + g.Sum(x => Math.Max(0m, x.Surcharge))),
                    ["share"] = RealEstateMapper.Percent(principal, grandTotal),
                };
            }).ToList();

        result.Breakdown = result.Rows.Select(r => new BreakdownSliceDto
        {
            Label = r["bucket"]?.ToString() ?? "—",
            Count = Convert.ToInt32(r["count"]),
            Value = Convert.ToDecimal(r["total"]),
            Percent = Convert.ToDecimal(r["share"]),
            Tone = r["bucket"]?.ToString() is "121–180" or "180+" ? "danger"
                : r["bucket"]?.ToString() is "61–90" or "91–120" ? "warning" : "neutral",
        }).ToList();

        Total(result);
    }

    private async Task DefaultersAsync(ReportResultDto result, ReportRequestDto request)
    {
        result.Columns =
        [
            Text("reference", "Booking", 130),
            Text("customer", "Customer", 200),
            Text("phone", "Phone", 140),
            Text("project", "Project", 170),
            Text("unit", "Unit", 100),
            Money("overdue", "Overdue"),
            Number("days", "Days", false),
            Text("bucket", "Age", 110),
            Money("outstanding", "Total outstanding"),
            Tag("dunning", "Dunning"),
            Date("lastPaid", "Last payment"),
        ];

        var bookings = await Db.Bookings.ForCompany(Tenant)
            .WhereIf(request.ProjectId.HasValue, b => b.ProjectId == request.ProjectId)
            .Where(b => b.OverdueAmount > 0m && b.Status != BookingStatus.Cancelled)
            .OrderByDescending(b => b.OverdueAmount)
            .ToListAsync();

        var names = await PartyNamesAsync(bookings.Select(b => b.PrimaryApplicantPartyId));
        var projects = await ProjectNamesAsync(bookings.Select(b => (Guid?)b.ProjectId));
        var units = await UnitNumbersAsync(bookings.Where(b => b.UnitId != null).Select(b => b.UnitId!.Value));

        var phones = await Db.Parties.ForCompany(Tenant)
            .Where(p => bookings.Select(b => b.PrimaryApplicantPartyId).Contains(p.Id))
            .Select(p => new { p.Id, p.PrimaryPhone })
            .ToDictionaryAsync(x => x.Id, x => x.PrimaryPhone);

        var bookingIds = bookings.Select(b => b.Id).ToList();

        var dunning = await Db.DunningCases.ForCompany(Tenant)
            .Where(d => d.BookingId != null && bookingIds.Contains(d.BookingId.Value) && !d.IsClosed)
            .ToDictionaryAsync(d => d.BookingId!.Value, d => d.CurrentStep);

        var lastPaid = await Db.Receipts.ForCompany(Tenant)
            .Where(r => r.BookingId != null && bookingIds.Contains(r.BookingId.Value)
                     && r.Status == ReceiptStatus.Posted)
            .GroupBy(r => r.BookingId!.Value)
            .Select(g => new { BookingId = g.Key, Last = g.Max(r => r.ReceivedOn) })
            .ToDictionaryAsync(x => x.BookingId, x => x.Last);

        result.Rows = bookings.Select(b => new Dictionary<string, object?>
        {
            ["reference"] = b.Reference,
            ["customer"] = names.GetValueOrDefault(b.PrimaryApplicantPartyId, "—"),
            ["phone"] = phones.GetValueOrDefault(b.PrimaryApplicantPartyId) ?? "—",
            ["project"] = projects.GetValueOrDefault(b.ProjectId, "—"),
            ["unit"] = b.UnitId is null ? "—" : units.GetValueOrDefault(b.UnitId.Value, "—"),
            ["overdue"] = b.OverdueAmount,
            ["days"] = b.DaysOverdue,
            ["bucket"] = RealEstateMapper.AgeingBucket(b.DaysOverdue),
            ["outstanding"] = b.Outstanding,
            ["dunning"] = dunning.TryGetValue(b.Id, out var step) ? $"Step {step}" : "Not started",
            ["lastPaid"] = lastPaid.TryGetValue(b.Id, out var paid) ? paid : null,
        }).ToList();

        Total(result);
    }

    private async Task DaybookAsync(ReportResultDto result, ReportRequestDto request, DateOnly from, DateOnly to)
    {
        result.Columns =
        [
            Text("number", "Receipt", 130),
            Date("date", "Date"),
            Text("payer", "Received from", 200),
            Tag("instrument", "Instrument"),
            Text("instrumentNumber", "Instrument no.", 140),
            Text("bank", "Bank", 150),
            Money("amount", "Amount"),
            Money("escrow", "To escrow"),
            Money("free", "To free"),
            Money("unallocated", "Unallocated"),
            Tag("status", "Status"),
        ];

        var receipts = await Db.Receipts.ForCompany(Tenant)
            .WhereIf(request.OfficeId.HasValue, r => r.OfficeId == request.OfficeId)
            .WhereIf(request.ProjectId.HasValue, r => r.ProjectId == request.ProjectId)
            .Where(r => r.ReceivedOn >= from && r.ReceivedOn <= to)
            .OrderByDescending(r => r.ReceivedOn)
            .ThenBy(r => r.ReceiptNumber)
            .ToListAsync();

        var names = await PartyNamesAsync(receipts.Select(r => r.PartyId));

        result.Rows = receipts.Select(r => new Dictionary<string, object?>
        {
            ["number"] = r.ReceiptNumber,
            ["date"] = r.ReceivedOn,
            ["payer"] = names.GetValueOrDefault(r.PartyId, "—"),
            ["instrument"] = r.Instrument.ToString(),
            ["instrumentNumber"] = r.InstrumentNumber ?? r.TransactionReference ?? "—",
            ["bank"] = r.BankName ?? "—",
            ["amount"] = r.Amount,
            ["escrow"] = r.EscrowAmount,
            ["free"] = r.FreeAmount,
            ["unallocated"] = r.UnallocatedAmount,
            ["status"] = r.Status.ToString(),
        }).ToList();

        result.Breakdown = receipts
            .Where(r => r.Status == ReceiptStatus.Posted)
            .GroupBy(r => r.Instrument)
            .OrderByDescending(g => g.Sum(r => r.Amount))
            .Select(g => new BreakdownSliceDto
            {
                Label = g.Key.ToString(),
                Count = g.Count(),
                Value = RealEstateMapper.Money(g.Sum(r => r.Amount)),
                Percent = RealEstateMapper.Percent(
                    g.Sum(r => r.Amount), receipts.Where(r => r.Status == ReceiptStatus.Posted).Sum(r => r.Amount)),
            }).ToList();

        Total(result);
    }

    private async Task SurchargeAsync(ReportResultDto result, ReportRequestDto request, DateOnly from, DateOnly to)
    {
        result.Columns =
        [
            Text("month", "Month", 110),
            Number("accruals", "Accruals"),
            Money("base", "Overdue base"),
            Money("accrued", "Accrued"),
            Money("capped", "Capped", false),
            Money("waived", "Waived"),
            Money("collected", "Collected"),
        ];

        var accruals = await Db.SurchargeAccruals.ForCompany(Tenant)
            .Where(s => s.AccrualDate >= from && s.AccrualDate <= to)
            .Select(s => new { s.AccrualDate, s.OverdueBase, s.Amount, s.CapReached })
            .ToListAsync();

        var waivers = await Db.SurchargeWaivers.ForCompany(Tenant)
            .Where(w => w.CreatedAt >= from.ToDateTime(TimeOnly.MinValue))
            .Select(w => new { w.CreatedAt, Amount = w.ApprovedAmount })
            .ToListAsync();

        var instalments = await Db.Instalments.ForCompany(Tenant)
            .Where(i => i.SurchargePaid > 0m)
            .Select(i => new { i.SettledOn, i.SurchargePaid })
            .ToListAsync();

        result.Rows = MonthsBetween(from, to).Select(m =>
        {
            var mine = accruals.Where(a => a.AccrualDate.Year == m.Year && a.AccrualDate.Month == m.Month).ToList();

            return new Dictionary<string, object?>
            {
                ["month"] = m.ToString("MMM yyyy"),
                ["accruals"] = mine.Count,
                ["base"] = RealEstateMapper.Money(mine.Sum(a => a.OverdueBase)),
                ["accrued"] = RealEstateMapper.Money(mine.Sum(a => a.Amount)),
                ["capped"] = mine.Count(a => a.CapReached),
                ["waived"] = RealEstateMapper.Money(waivers
                    .Where(w => w.CreatedAt.Year == m.Year && w.CreatedAt.Month == m.Month).Sum(w => w.Amount)),
                ["collected"] = RealEstateMapper.Money(instalments
                    .Where(i => i.SettledOn is not null && i.SettledOn.Value.Year == m.Year
                                                        && i.SettledOn.Value.Month == m.Month)
                    .Sum(i => i.SurchargePaid)),
            };
        }).ToList();

        Total(result);
    }

    private async Task RefundsAsync(ReportResultDto result, DateOnly from, DateOnly to)
    {
        result.Columns =
        [
            Text("reference", "Refund", 130),
            Date("requestedOn", "Requested"),
            Text("payee", "Payee", 200),
            Money("requested", "Requested"),
            Money("approved", "Approved"),
            Money("paid", "Paid"),
            Money("pending", "Pending"),
            Tag("status", "Status"),
            Text("bankVerified", "Bank verified", 130),
        ];

        var refunds = await Db.RefundRequests.ForCompany(Tenant)
            .Where(r => r.RequestedOn >= from && r.RequestedOn <= to)
            .OrderByDescending(r => r.RequestedOn)
            .ToListAsync();

        var names = await PartyNamesAsync(refunds.Select(r => r.PartyId));

        result.Rows = refunds.Select(r => new Dictionary<string, object?>
        {
            ["reference"] = r.Reference,
            ["requestedOn"] = r.RequestedOn,
            ["payee"] = r.PayeeName ?? names.GetValueOrDefault(r.PartyId, "—"),
            ["requested"] = r.RequestedAmount,
            ["approved"] = r.ApprovedAmount,
            ["paid"] = r.PaidAmount,
            ["pending"] = RealEstateMapper.Money(Math.Max(0m, r.ApprovedAmount - r.PaidAmount)),
            ["status"] = r.Status.ToString(),
            ["bankVerified"] = r.BankDetailsVerified ? "Yes" : "No",
        }).ToList();

        Total(result);
    }

    private async Task ChequesAsync(ReportResultDto result, DateOnly from, DateOnly to)
    {
        result.Columns =
        [
            Text("number", "Cheque", 130),
            Text("party", "From", 200),
            Text("bank", "Bank", 160),
            Date("chequeDate", "Cheque date"),
            Money("amount", "Amount"),
            Tag("state", "State"),
            Date("depositedOn", "Deposited"),
            Date("clearedOn", "Cleared"),
            Text("bounceReason", "Bounce reason", 200),
            Money("bounceCharge", "Bounce charge"),
        ];

        var cheques = await Db.ChequeRecords.ForCompany(Tenant)
            .Where(c => c.ChequeDate >= from && c.ChequeDate <= to)
            .OrderBy(c => c.ChequeDate)
            .ToListAsync();

        var names = await PartyNamesAsync(cheques.Select(c => c.PartyId));

        result.Rows = cheques.Select(c => new Dictionary<string, object?>
        {
            ["number"] = c.ChequeNumber,
            ["party"] = names.GetValueOrDefault(c.PartyId, "—"),
            ["bank"] = c.BankName ?? "—",
            ["chequeDate"] = c.ChequeDate,
            ["amount"] = c.Amount,
            ["state"] = c.State.ToString(),
            ["depositedOn"] = c.DepositedOn,
            ["clearedOn"] = c.ClearedOn,
            ["bounceReason"] = c.BounceReason ?? "—",
            ["bounceCharge"] = c.BounceCharge,
        }).ToList();

        result.Breakdown = cheques
            .GroupBy(c => c.State)
            .OrderByDescending(g => g.Sum(c => c.Amount))
            .Select(g => new BreakdownSliceDto
            {
                Label = g.Key.ToString(),
                Count = g.Count(),
                Value = RealEstateMapper.Money(g.Sum(c => c.Amount)),
                Percent = RealEstateMapper.Percent(g.Count(), cheques.Count),
                Tone = g.Key switch
                {
                    ChequeState.Bounced or ChequeState.StopPayment => "danger",
                    ChequeState.Cleared => "positive",
                    _ => "neutral",
                },
            }).ToList();

        Total(result);
    }

    // ═══ Shared helpers ══════════════════════════════════════════════════════

    private async Task<Dictionary<Guid, string>> UnitNumbersAsync(IEnumerable<Guid> unitIds)
    {
        var ids = unitIds.Where(i => i != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0) return [];

        return await Db.Units.ForCompany(Tenant)
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);
    }

    private static List<DateOnly> MonthsBetween(DateOnly from, DateOnly to)
    {
        var months = new List<DateOnly>();
        var cursor = new DateOnly(from.Year, from.Month, 1);
        var end = new DateOnly(to.Year, to.Month, 1);

        // A period longer than five years on a monthly axis is unreadable, and generating it costs
        // more than it tells anybody.
        while (cursor <= end && months.Count < 60)
        {
            months.Add(cursor);
            cursor = cursor.AddMonths(1);
        }

        return months;
    }

    private static List<TrendPointDto> MonthlyTrend(
        DateOnly from, DateOnly to, IEnumerable<(DateOnly Date, decimal Value, decimal Secondary)> points)
    {
        var data = points.ToList();

        return MonthsBetween(from, to).Select(m => new TrendPointDto
        {
            Label = m.ToString("MMM yy"),
            Date = m,
            Value = RealEstateMapper.Money(data
                .Where(p => p.Date.Year == m.Year && p.Date.Month == m.Month).Sum(p => p.Value)),
            SecondaryValue = data
                .Where(p => p.Date.Year == m.Year && p.Date.Month == m.Month).Sum(p => p.Secondary),
        }).ToList();
    }
}
