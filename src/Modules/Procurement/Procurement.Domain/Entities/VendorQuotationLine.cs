using Nexcore.SharedKernel;

namespace Procurement.Domain.Entities;

/// <summary>
/// Individual line in a vendor's quotation responding to an RFQ line.
/// </summary>
public class VendorQuotationLine : BaseEntity
{
    public Guid QuotationId { get; set; }
    public VendorQuotation Quotation { get; set; } = null!;

    public Guid RFQLineId { get; set; }
    public RFQLine RFQLine { get; set; } = null!;

    public int LineNumber { get; set; }

    // ─── Item ──────────────────────────────────────────────────────────────────
    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public required string ItemDescription { get; set; }

    // ─── Pricing ───────────────────────────────────────────────────────────────
    public decimal Quantity { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public string? UnitOfMeasureName { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TotalPrice { get; set; }

    // ─── Delivery ──────────────────────────────────────────────────────────────
    public DateTime? PromisedDeliveryDate { get; set; }
    public int? LeadTimeDays { get; set; }

    /// <summary>Vendor's alternative item if exact item not available.</summary>
    public bool IsAlternative { get; set; }

    public string? Notes { get; set; }
}
