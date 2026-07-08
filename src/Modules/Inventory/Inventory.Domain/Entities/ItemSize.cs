using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Size
/// Master list of sizes used across items (apparel, footwear, electronics, etc.)
/// Supports numeric (28, 30, 32), alpha (S, M, L, XL), and custom (One Size) formats.
/// </summary>
public class Size : BaseEntity
{
    /// <summary>Unique size code (e.g., SM, LG, XL, SZ28)</summary>
    public new string Code { get; set; } = null!;

    /// <summary>Display name (e.g., Small, Large, 28, One Size)</summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Size category / chart: Apparel | Footwear | Ring | Screen | Weight | Custom
    /// Used to group sizes into the right picker per item type
    /// </summary>
    public string SizeChart { get; set; } = "Apparel";

    /// <summary>Numeric sort value for ordering (28 &lt; 30 &lt; 32, S=1 &lt; M=2 &lt; L=3)</summary>
    public int SortOrder { get; set; }

    // Navigation properties
    public ICollection<ItemSize> ItemSizes { get; set; } = new List<ItemSize>();
    public ICollection<ItemVariant> Variants { get; set; } = new List<ItemVariant>();
}

/// <summary>
/// Item Size
/// Junction - assigns available sizes to an item.
/// Example: T-Shirt is available in S, M, L, XL ? 4 rows.
/// </summary>
public class ItemSize : BaseEntity
{
    public Guid ItemId { get; set; }
    public Guid SizeId { get; set; }

    /// <summary>Optional: specific barcode SKU for this size variant</summary>
    public Guid? ItemBarcodeId { get; set; }

    /// <summary>Whether this is the default selected size</summary>
    public bool IsDefault { get; set; }

    /// <summary>Whether this size variant is currently available</summary>
    public bool IsAvailable { get; set; } = true;

    // Navigation properties
    public Item? Item { get; set; }
    public Size? Size { get; set; }
    public ItemBarcode? ItemBarcode { get; set; }
}
