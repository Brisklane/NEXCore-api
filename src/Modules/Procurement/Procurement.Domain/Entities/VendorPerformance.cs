using Nexcore.SharedKernel;

namespace Procurement.Domain.Entities;

/// <summary>
/// Periodic vendor performance scorecard.
/// Aligned with SAP Vendor Evaluation (ME6B), Oracle Supplier Performance.
/// </summary>
public class VendorPerformance : BaseEntity
{
    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    /// <summary>Evaluation period start date.</summary>
    public DateTime PeriodFrom { get; set; }
    /// <summary>Evaluation period end date.</summary>
    public DateTime PeriodTo { get; set; }

    // ─── KPIs ──────────────────────────────────────────────────────────────────
    public decimal OnTimeDeliveryRate { get; set; }
    public decimal QualityScore { get; set; }
    public decimal PriceComplianceRate { get; set; }
    public decimal ResponsivenessScore { get; set; }
    public decimal DocumentAccuracyScore { get; set; }

    /// <summary>Weighted overall score (0–100).</summary>
    public decimal OverallRating { get; set; }

    // ─── Counts ────────────────────────────────────────────────────────────────
    public int TotalOrders { get; set; }
    public int LateDeliveries { get; set; }
    public int QualityRejections { get; set; }
    public int InvoiceDiscrepancies { get; set; }
    public decimal TotalPurchaseValue { get; set; }

    public Guid? EvaluatedByUserId { get; set; }
    public DateTime? EvaluatedAt { get; set; }
    public string? Comments { get; set; }
}
