using Microsoft.EntityFrameworkCore;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;
using Procurement.Domain.Enums;
using Procurement.Infrastructure.Persistence;

namespace Procurement.Infrastructure.Services;

public class ProcurementReportsService : IProcurementReportsService
{
    private readonly ProcurementDbContext _ctx;
    public ProcurementReportsService(ProcurementDbContext ctx) => _ctx = ctx;

    private static string Label(string enumName)
    {
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < enumName.Length; i++)
        {
            if (i > 0 && char.IsUpper(enumName[i]) && !char.IsUpper(enumName[i - 1])) sb.Append(' ');
            sb.Append(enumName[i]);
        }
        return sb.ToString();
    }

    private async Task<Dictionary<Guid, string>> VendorNamesAsync()
        => await _ctx.Vendors.AsNoTracking().Where(v => !v.IsDeleted).ToDictionaryAsync(v => v.Id, v => v.Name);

    // ── Purchase Analysis ───────────────────────────────────────────────────────
    public async Task<PurchaseAnalysisDto> GetPurchaseAnalysisAsync()
    {
        var pos = await _ctx.PurchaseOrders.AsNoTracking()
            .Where(o => !o.IsDeleted && o.Status != PurchaseOrderStatus.Cancelled && o.Status != PurchaseOrderStatus.Draft)
            .ToListAsync();
        var vendors = await VendorNamesAsync();

        var dto = new PurchaseAnalysisDto
        {
            TotalOrders       = pos.Count,
            TotalValue        = pos.Sum(o => o.TotalAmount),
            InvoicedValue     = pos.Sum(o => o.InvoicedAmount),
            OutstandingValue  = pos.Sum(o => o.OutstandingAmount),
            OpenOrders        = pos.Count(o => o.Status is PurchaseOrderStatus.Confirmed or PurchaseOrderStatus.SentToVendor
                                    or PurchaseOrderStatus.Acknowledged or PurchaseOrderStatus.PartiallyReceived),
        };
        dto.AverageOrderValue = pos.Count > 0 ? Math.Round(dto.TotalValue / pos.Count, 2) : 0;

        dto.ByStatus = pos.GroupBy(o => o.Status)
            .Select(g => new ReportBucketDto { Label = Label(g.Key.ToString()), Count = g.Count(), Value = g.Sum(o => o.TotalAmount) })
            .OrderByDescending(b => b.Value).ToList();

        // last 12 months
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-11);
        for (var i = 0; i < 12; i++)
        {
            var m = start.AddMonths(i);
            var monthPos = pos.Where(o => o.OrderDate.Year == m.Year && o.OrderDate.Month == m.Month).ToList();
            dto.MonthlyTrend.Add(new ReportBucketDto { Label = m.ToString("MMM yy"), Count = monthPos.Count, Value = monthPos.Sum(o => o.TotalAmount) });
        }

        dto.TopVendors = pos.GroupBy(o => o.VendorId)
            .Select(g => new VendorSpendDto { VendorId = g.Key, VendorName = vendors.GetValueOrDefault(g.Key, "—"), OrderCount = g.Count(), Value = g.Sum(o => o.TotalAmount) })
            .OrderByDescending(v => v.Value).Take(8).ToList();

        return dto;
    }

    // ── Vendor Analysis ─────────────────────────────────────────────────────────
    public async Task<VendorAnalysisDto> GetVendorAnalysisAsync()
    {
        var vendors = await _ctx.Vendors.AsNoTracking().Where(v => !v.IsDeleted).ToListAsync();
        var pos = await _ctx.PurchaseOrders.AsNoTracking().Where(o => !o.IsDeleted && o.Status != PurchaseOrderStatus.Cancelled).ToListAsync();
        var invoices = await _ctx.PurchaseInvoices.AsNoTracking().Where(i => !i.IsDeleted && i.Status != PurchaseInvoiceStatus.Cancelled).ToListAsync();

        var poByVendor = pos.GroupBy(o => o.VendorId).ToDictionary(g => g.Key, g => g.ToList());
        var invByVendor = invoices.GroupBy(i => i.VendorId).ToDictionary(g => g.Key, g => g.ToList());

        var rows = vendors.Select(v =>
        {
            var vpos = poByVendor.GetValueOrDefault(v.Id) ?? [];
            var vinv = invByVendor.GetValueOrDefault(v.Id) ?? [];
            return new VendorAnalysisRowDto
            {
                VendorId = v.Id, VendorNumber = v.VendorNumber, VendorName = v.Name,
                OrderCount = vpos.Count, TotalPurchaseValue = vpos.Sum(o => o.TotalAmount),
                InvoicedValue = vinv.Sum(i => i.TotalAmount), PaidValue = vinv.Sum(i => i.PaidAmount),
                Outstanding = vinv.Sum(i => i.OutstandingAmount),
                OverallRating = v.OverallRating, OnTimeDeliveryRate = v.OnTimeDeliveryRate, IsPreferred = v.IsPreferredVendor,
            };
        })
        .Where(r => r.OrderCount > 0 || r.InvoicedValue > 0)
        .OrderByDescending(r => r.TotalPurchaseValue).ToList();

        return new VendorAnalysisDto
        {
            ActiveVendors = rows.Count,
            TotalSpend = rows.Sum(r => r.TotalPurchaseValue),
            Vendors = rows,
        };
    }

    // ── AP Aging ──────────────────────────────────────────────────────────────────
    public async Task<ApAgingDto> GetApAgingAsync()
    {
        var open = await _ctx.PurchaseInvoices.AsNoTracking()
            .Where(i => !i.IsDeleted && i.OutstandingAmount > 0
                     && i.Status != PurchaseInvoiceStatus.Cancelled && i.Status != PurchaseInvoiceStatus.Draft)
            .ToListAsync();
        var vendors = await VendorNamesAsync();
        var today = DateTime.UtcNow.Date;

        var rows = open.GroupBy(i => i.VendorId).Select(g =>
        {
            var row = new ApAgingRowDto { VendorId = g.Key, VendorName = vendors.GetValueOrDefault(g.Key, "—") };
            foreach (var inv in g)
            {
                var days = (today - inv.DueDate.Date).Days;
                var amt = inv.OutstandingAmount;
                if (days <= 0) row.Current += amt;
                else if (days <= 30) row.Days1To30 += amt;
                else if (days <= 60) row.Days31To60 += amt;
                else if (days <= 90) row.Days61To90 += amt;
                else row.Days90Plus += amt;
            }
            row.Total = row.Current + row.Days1To30 + row.Days31To60 + row.Days61To90 + row.Days90Plus;
            return row;
        }).Where(r => r.Total > 0).OrderByDescending(r => r.Total).ToList();

        return new ApAgingDto
        {
            Current = rows.Sum(r => r.Current), Days1To30 = rows.Sum(r => r.Days1To30),
            Days31To60 = rows.Sum(r => r.Days31To60), Days61To90 = rows.Sum(r => r.Days61To90),
            Days90Plus = rows.Sum(r => r.Days90Plus), Total = rows.Sum(r => r.Total), Rows = rows,
        };
    }

    // ── 3-Way Match ───────────────────────────────────────────────────────────────
    public async Task<ThreeWayMatchDto> GetThreeWayMatchAsync()
    {
        var invoices = await _ctx.PurchaseInvoices.AsNoTracking()
            .Where(i => !i.IsDeleted && i.Status != PurchaseInvoiceStatus.Cancelled).ToListAsync();
        var vendors = await VendorNamesAsync();

        // PO ordered + received (from posted line quantities) for matching context
        var pos = await _ctx.PurchaseOrders.AsNoTracking().Include(o => o.Lines)
            .Where(o => !o.IsDeleted).ToListAsync();
        var poInfo = pos.ToDictionary(o => o.Id, o => (
            Number: o.OrderNumber,
            Ordered: o.TotalAmount,
            Received: o.Lines.Sum(l => l.QuantityReceived * l.UnitPrice)));

        var rows = invoices.Select(i =>
        {
            (string Number, decimal Ordered, decimal Received) po = i.PurchaseOrderId.HasValue && poInfo.TryGetValue(i.PurchaseOrderId.Value, out var p)
                ? p : (null!, 0m, 0m);
            return new ThreeWayMatchRowDto
            {
                InvoiceId = i.Id, InvoiceNumber = i.InvoiceNumber, VendorName = vendors.GetValueOrDefault(i.VendorId, "—"),
                PurchaseOrderNumber = po.Number, OrderedAmount = po.Ordered, ReceivedAmount = po.Received,
                InvoicedAmount = i.TotalAmount, MatchingStatus = Label(i.MatchingStatus.ToString()),
                IsException = i.MatchingStatus == InvoiceMatchingStatus.MatchException,
            };
        }).OrderByDescending(r => r.IsException).ThenBy(r => r.InvoiceNumber).ToList();

        return new ThreeWayMatchDto
        {
            TotalInvoices = invoices.Count,
            FullyMatched = invoices.Count(i => i.MatchingStatus == InvoiceMatchingStatus.FullyMatched),
            PartiallyMatched = invoices.Count(i => i.MatchingStatus == InvoiceMatchingStatus.PartiallyMatched),
            NotMatched = invoices.Count(i => i.MatchingStatus == InvoiceMatchingStatus.NotMatched),
            Exceptions = invoices.Count(i => i.MatchingStatus == InvoiceMatchingStatus.MatchException),
            ByStatus = invoices.GroupBy(i => i.MatchingStatus)
                .Select(g => new ReportBucketDto { Label = Label(g.Key.ToString()), Count = g.Count(), Value = g.Sum(i => i.TotalAmount) })
                .OrderByDescending(b => b.Count).ToList(),
            Rows = rows,
        };
    }

    // ── Spend by Category ───────────────────────────────────────────────────────
    public async Task<SpendByCategoryDto> GetSpendByCategoryAsync()
    {
        var pos = await _ctx.PurchaseOrders.AsNoTracking().Include(o => o.Lines).ThenInclude(l => l.ProcurementCategory)
            .Where(o => !o.IsDeleted && o.Status != PurchaseOrderStatus.Cancelled && o.Status != PurchaseOrderStatus.Draft)
            .ToListAsync();

        var lines = pos.SelectMany(o => o.Lines).ToList();
        var grouped = lines.GroupBy(l => l.ProcurementCategoryId)
            .Select(g => new CategorySpendDto
            {
                CategoryId = g.Key,
                CategoryName = g.First().ProcurementCategory?.Name ?? "Uncategorised",
                LineCount = g.Count(),
                Value = g.Sum(l => l.SubTotal),
            })
            .OrderByDescending(c => c.Value).ToList();

        var total = grouped.Sum(c => c.Value);
        foreach (var c in grouped) c.Percent = total > 0 ? Math.Round(c.Value / total * 100, 1) : 0;

        return new SpendByCategoryDto { TotalSpend = total, Categories = grouped };
    }
}
