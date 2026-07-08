using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// One discount rule within a <see cref="Promotion"/>.
/// Targets either a specific item (<see cref="ItemId"/>) or an entire
/// item category (<see cref="ItemCategoryId"/>).
/// At least one of those must be set.
/// </summary>
public class PromotionItem : BaseEntity
{
    public Guid PromotionId { get; set; }

    // ─── Scope: item or category ─────────────────────────────────────────────
    /// <summary>Cross-module FK to Inventory.Item. Null when targeting a whole category.</summary>
    public Guid? ItemId { get; set; }
    /// <summary>Cross-module FK to Inventory.ItemCategory. Null when targeting a specific item.</summary>
    public Guid? ItemCategoryId { get; set; }

    // ─── Discount ────────────────────────────────────────────────────────────
    public PromotionDiscountType DiscountType { get; set; }
    /// <summary>
    /// Meaning depends on DiscountType:
    /// PercentageOff → percentage (e.g. 10 = 10%)
    /// FixedAmountOff → currency amount off per unit
    /// NewPrice → the new unit price
    /// BuyXGetYFree → not used (see BuyQuantity / GetQuantity)
    /// FreeItem → not used
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>Which base price the discount is evaluated against.</summary>
    public PromotionPriceTarget PriceTarget { get; set; } = PromotionPriceTarget.AnyPrice;

    // ─── Condition ───────────────────────────────────────────────────────────
    public bool IsConditional { get; set; } = false;
    public PromotionConditionType ConditionType { get; set; } = PromotionConditionType.None;
    /// <summary>Minimum unit quantity required to trigger this rule.</summary>
    public decimal? ConditionQuantity { get; set; }
    /// <summary>Minimum line/basket amount required to trigger this rule.</summary>
    public decimal? ConditionAmount { get; set; }

    // ─── Per-Transaction Cap ─────────────────────────────────────────────────
    /// <summary>Maximum quantity eligible for the discount in a single transaction.</summary>
    public decimal? MaxDiscountedQuantity { get; set; }

    // ─── BuyXGetYFree Fields ─────────────────────────────────────────────────
    /// <summary>Units the customer must buy to trigger a free allocation (BuyXGetYFree only).</summary>
    public decimal? BuyQuantity { get; set; }
    /// <summary>Free units to add (BuyXGetYFree only).</summary>
    public decimal? GetQuantity { get; set; }
    /// <summary>
    /// Item to give for free. Null = same as ItemId.
    /// Cross-module FK to Inventory.Item.
    /// </summary>
    public Guid? FreeItemId { get; set; }

    // ─── Navigation ──────────────────────────────────────────────────────────
    public Promotion? Promotion { get; set; }
}
