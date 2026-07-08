namespace Procurement.Application.DTOs;

// ── Shared building blocks ──────────────────────────────────────────────────────

public class ReportBucketDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Value { get; set; }
}

public class VendorSpendDto
{
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal Value { get; set; }
}

// ── Purchase Analysis ───────────────────────────────────────────────────────────

public class PurchaseAnalysisDto
{
    public int TotalOrders { get; set; }
    public decimal TotalValue { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int OpenOrders { get; set; }
    public decimal InvoicedValue { get; set; }
    public decimal OutstandingValue { get; set; }
    public string CurrencyCode { get; set; } = "PKR";
    public List<ReportBucketDto> ByStatus { get; set; } = [];
    public List<ReportBucketDto> MonthlyTrend { get; set; } = [];
    public List<VendorSpendDto> TopVendors { get; set; } = [];
}

// ── Vendor Analysis ─────────────────────────────────────────────────────────────

public class VendorAnalysisRowDto
{
    public Guid VendorId { get; set; }
    public string VendorNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalPurchaseValue { get; set; }
    public decimal InvoicedValue { get; set; }
    public decimal PaidValue { get; set; }
    public decimal Outstanding { get; set; }
    public decimal? OverallRating { get; set; }
    public decimal? OnTimeDeliveryRate { get; set; }
    public bool IsPreferred { get; set; }
}

public class VendorAnalysisDto
{
    public int ActiveVendors { get; set; }
    public decimal TotalSpend { get; set; }
    public string CurrencyCode { get; set; } = "PKR";
    public List<VendorAnalysisRowDto> Vendors { get; set; } = [];
}

// ── AP Aging ────────────────────────────────────────────────────────────────────

public class ApAgingRowDto
{
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public decimal Current { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Days90Plus { get; set; }
    public decimal Total { get; set; }
}

public class ApAgingDto
{
    public decimal Current { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Days90Plus { get; set; }
    public decimal Total { get; set; }
    public string CurrencyCode { get; set; } = "PKR";
    public List<ApAgingRowDto> Rows { get; set; } = [];
}

// ── 3-Way Match ─────────────────────────────────────────────────────────────────

public class ThreeWayMatchRowDto
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public string? PurchaseOrderNumber { get; set; }
    public decimal OrderedAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public decimal InvoicedAmount { get; set; }
    public string MatchingStatus { get; set; } = string.Empty;
    public bool IsException { get; set; }
}

public class ThreeWayMatchDto
{
    public int TotalInvoices { get; set; }
    public int FullyMatched { get; set; }
    public int PartiallyMatched { get; set; }
    public int NotMatched { get; set; }
    public int Exceptions { get; set; }
    public string CurrencyCode { get; set; } = "PKR";
    public List<ReportBucketDto> ByStatus { get; set; } = [];
    public List<ThreeWayMatchRowDto> Rows { get; set; } = [];
}

// ── Spend by Category ───────────────────────────────────────────────────────────

public class CategorySpendDto
{
    public Guid? CategoryId { get; set; }
    public string CategoryName { get; set; } = "Uncategorised";
    public int LineCount { get; set; }
    public decimal Value { get; set; }
    public decimal Percent { get; set; }
}

public class SpendByCategoryDto
{
    public decimal TotalSpend { get; set; }
    public string CurrencyCode { get; set; } = "PKR";
    public List<CategorySpendDto> Categories { get; set; } = [];
}
