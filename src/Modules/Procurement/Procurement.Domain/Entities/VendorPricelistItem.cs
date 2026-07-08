using Nexcore.SharedKernel;

namespace Procurement.Domain.Entities;

/// <summary>
/// Individual item pricing entry in a vendor pricelist.
/// Supports tiered/volume pricing via MinimumQuantity breaks.
/// </summary>
public class VendorPricelistItem : BaseEntity
{
    public Guid PricelistId { get; set; }
    public VendorPricelist Pricelist { get; set; } = null!;

    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public required string ItemDescription { get; set; }

    public Guid? UnitOfMeasureId { get; set; }
    public string? UnitOfMeasureName { get; set; }

    // ─── Tiered Pricing ────────────────────────────────────────────────────────
    /// <summary>This price applies when quantity ordered >= MinimumQuantity.</summary>
    public decimal MinimumQuantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal? DiscountPercent { get; set; }

    public int LeadTimeDays { get; set; }

    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    public string? Notes { get; set; }
}
