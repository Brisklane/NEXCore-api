using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Item Variant
/// A purchasable combination of item + attributes (Color + Size + any other dimension).
/// Each variant has its own SKU code, barcode, price override, and stock tracking.
/// Used in AliExpress-style listings ("Red / XL"), Daraz variant tables, and POS grids.
/// </summary>
public class ItemVariant : BaseEntity
{
    public Guid ItemId { get; set; }

    /// <summary>Variant-specific SKU code (e.g., TSHIRT-RED-XL)</summary>
    public string VariantCode { get; set; } = null!;

    /// <summary>Variant display name (e.g., Red / XL)</summary>
    public string VariantName { get; set; } = null!;

    /// <summary>Color dimension - nullable if not applicable</summary>
    public Guid? ColorId { get; set; }

    /// <summary>Size dimension - nullable if not applicable</summary>
    public Guid? SizeId { get; set; }

    /// <summary>
    /// Extra variant dimension (e.g., Storage: 128GB / 256GB, Flavor: Vanilla / Chocolate)
    /// Stored as free text for flexibility
    /// </summary>
    public string? ExtraDimension { get; set; }

    /// <summary>Variant-specific barcode (overrides item-level barcode for this variant)</summary>
    public string? Barcode { get; set; }

    /// <summary>Variant image URL - overrides item primary image in product listings</summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Price override for this variant.
    /// Null = use item-level price.
    /// Set when this specific variant (e.g., XL) has a different price.
    /// </summary>
    public decimal? SalePriceOverride { get; set; }

    /// <summary>Purchase price override for this variant</summary>
    public decimal? PurchasePriceOverride { get; set; }

    /// <summary>Weight of this specific variant in KG (may differ from base item)</summary>
    public decimal? WeightKg { get; set; }

    /// <summary>Display order in the variant picker grid</summary>
    public int DisplayOrder { get; set; }

    // Navigation properties
    public Item? Item { get; set; }
    public Color? Color { get; set; }
    public Size? Size { get; set; }
    public ICollection<InventoryBalance> Balances { get; set; } = new List<InventoryBalance>();
}
