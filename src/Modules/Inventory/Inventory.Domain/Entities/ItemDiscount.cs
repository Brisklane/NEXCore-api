using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Item Discount
/// Promotional discounts applied to an item.
/// Supports flat, percentage, buy-X-get-Y, and bundle discounts.
/// Used in POS promotions, Daraz Flash Sales, AliExpress Super Deals,
/// and Cash &amp; Carry volume pricing.
/// </summary>
public class ItemDiscount : BaseEntity
{
    public Guid ItemId { get; set; }

    /// <summary>Discount name / campaign label (e.g., "Eid Sale 20%", "Buy 3 Get 1 Free")</summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Discount type:
    /// Percentage       - percentage off sale price
    /// FixedAmount      - fixed amount off (e.g., $5 off)
    /// BuyXGetY         - buy X quantity, get Y free
    /// VolumePrice      - tiered price for quantity ranges (Cash &amp; Carry)
    /// </summary>
    public string DiscountType { get; set; } = "Percentage";

    /// <summary>Discount value - percentage (15.00) or fixed amount (5.00)</summary>
    public decimal DiscountValue { get; set; }

    /// <summary>For BuyXGetY: quantity customer must buy</summary>
    public int? BuyQuantity { get; set; }

    /// <summary>For BuyXGetY: quantity customer gets free</summary>
    public int? GetQuantity { get; set; }

    /// <summary>Minimum quantity required to trigger this discount (for volume pricing)</summary>
    public decimal? MinQuantity { get; set; }

    /// <summary>Maximum quantity this discount applies to per transaction</summary>
    public decimal? MaxQuantity { get; set; }

    /// <summary>
    /// Channel scope - null means all channels, otherwise comma-separated:
    /// POS | DARAZ | ALIEXPRESS | CASH_CARRY | B2B
    /// </summary>
    public string? ApplicableChannels { get; set; }

    /// <summary>Promotion start date/time</summary>
    public DateTime ValidFrom { get; set; }

    /// <summary>Promotion end date/time</summary>
    public DateTime ValidTo { get; set; }

    /// <summary>Priority - when multiple discounts apply, highest priority wins (1 = highest)</summary>
    public int Priority { get; set; } = 1;

    // Navigation property
    public Item? Item { get; set; }
}
