using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Item Bundle
/// Groups multiple items into a bundle / kit sold as one unit.
/// Example: "Gaming PC Bundle" = Monitor + Keyboard + Mouse.
/// Used in POS combo deals, marketplace bundles, and Cash &amp; Carry promotional packs.
/// </summary>
public class ItemBundle : BaseEntity
{
    /// <summary>The parent bundle item (the item being sold as a kit)</summary>
    public Guid BundleItemId { get; set; }

    /// <summary>The component item included in this bundle</summary>
    public Guid ComponentItemId { get; set; }

    /// <summary>Quantity of the component in this bundle (e.g., 2 keyboards)</summary>
    public decimal Quantity { get; set; } = 1;

    /// <summary>Unit of measure for this component's quantity</summary>
    public Guid UnitId { get; set; }

    /// <summary>
    /// Whether this component's cost is included when calculating bundle cost.
    /// Set to false for free gift items in a bundle.
    /// </summary>
    public bool IsIncludedInCost { get; set; } = true;

    /// <summary>Display order of component in the bundle breakdown</summary>
    public int DisplayOrder { get; set; }

    // Navigation properties
    public Item? BundleItem { get; set; }
    public Item? ComponentItem { get; set; }
    public Unit? Unit { get; set; }
}

/// <summary>
/// Item Substitution
/// Defines allowed substitute / alternative items when the original is out of stock.
/// Used in POS out-of-stock suggestions, purchasing alternatives, and e-commerce "Similar Items".
/// </summary>
public class ItemSubstitution : BaseEntity
{
    /// <summary>The original item</summary>
    public Guid ItemId { get; set; }

    /// <summary>The substitute / alternative item</summary>
    public Guid SubstituteItemId { get; set; }

    /// <summary>Priority - lower number = preferred substitute (1 = first choice)</summary>
    public int Priority { get; set; } = 1;

    /// <summary>Note explaining why this is a substitute (e.g., "Same spec, different brand")</summary>
    public string? Note { get; set; }

    /// <summary>Whether substitution is bidirectional (A?B) or one-way (A?B only)</summary>
    public bool IsBidirectional { get; set; } = true;

    // Navigation properties
    public Item? Item { get; set; }
    public Item? SubstituteItem { get; set; }
}
