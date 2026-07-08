namespace Procurement.Application.DTOs;

// ── Vendor Performance Scorecard Read ───────────────────────────────────────────

public class VendorPerformanceDto
{
    public Guid Id { get; set; }

    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string VendorNumber { get; set; } = string.Empty;

    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }

    // KPIs (0–100)
    public decimal OnTimeDeliveryRate { get; set; }
    public decimal QualityScore { get; set; }
    public decimal PriceComplianceRate { get; set; }
    public decimal ResponsivenessScore { get; set; }
    public decimal DocumentAccuracyScore { get; set; }
    public decimal OverallRating { get; set; }

    // Counts
    public int TotalOrders { get; set; }
    public int LateDeliveries { get; set; }
    public int QualityRejections { get; set; }
    public int InvoiceDiscrepancies { get; set; }
    public decimal TotalPurchaseValue { get; set; }

    public Guid? EvaluatedByUserId { get; set; }
    public DateTime? EvaluatedAt { get; set; }
    public string? Comments { get; set; }
}

// ── Create / Update ─────────────────────────────────────────────────────────────
// OverallRating is computed server-side from the weighted KPIs; it is not accepted from the client.

public class CreateVendorPerformanceDto
{
    public required Guid VendorId { get; set; }
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }

    public decimal OnTimeDeliveryRate { get; set; }
    public decimal QualityScore { get; set; }
    public decimal PriceComplianceRate { get; set; }
    public decimal ResponsivenessScore { get; set; }
    public decimal DocumentAccuracyScore { get; set; }

    public int TotalOrders { get; set; }
    public int LateDeliveries { get; set; }
    public int QualityRejections { get; set; }
    public int InvoiceDiscrepancies { get; set; }
    public decimal TotalPurchaseValue { get; set; }

    public string? Comments { get; set; }
}

public class UpdateVendorPerformanceDto
{
    public DateTime? PeriodFrom { get; set; }
    public DateTime? PeriodTo { get; set; }

    public decimal? OnTimeDeliveryRate { get; set; }
    public decimal? QualityScore { get; set; }
    public decimal? PriceComplianceRate { get; set; }
    public decimal? ResponsivenessScore { get; set; }
    public decimal? DocumentAccuracyScore { get; set; }

    public int? TotalOrders { get; set; }
    public int? LateDeliveries { get; set; }
    public int? QualityRejections { get; set; }
    public int? InvoiceDiscrepancies { get; set; }
    public decimal? TotalPurchaseValue { get; set; }

    public string? Comments { get; set; }
}
