using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Color
/// Master list of colors available for selection on items.
/// Each color has a display name, code, and visual identifiers (hex, RGB)
/// used in POS color swatches and product catalogs.
/// </summary>
public class Color : BaseEntity
{
    /// <summary>
    /// Unique color code (e.g., RED, NAVY-BLUE, OFF-WHITE)
    /// </summary>
    public new string Code { get; set; } = null!;

    /// <summary>
    /// Display name (e.g., Navy Blue, Off White, Charcoal Grey)
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Hex color value for UI swatch rendering (e.g., #1B2A6B)
    /// </summary>
    public string? HexCode { get; set; }

    /// <summary>
    /// Red channel 0-255
    /// </summary>
    public byte? R { get; set; }

    /// <summary>
    /// Green channel 0-255
    /// </summary>
    public byte? G { get; set; }

    /// <summary>
    /// Blue channel 0-255
    /// </summary>
    public byte? B { get; set; }

    /// <summary>
    /// Color family / group (e.g., Red, Blue, Neutral, Metallic)
    /// Used for grouping and filtering in the color picker
    /// </summary>
    public string? ColorFamily { get; set; }

    /// <summary>
    /// Optional swatch image URL for pattern/texture colors (e.g., Camouflage, Floral)
    /// </summary>
    public string? SwatchImageUrl { get; set; }

    /// <summary>
    /// Display order within the color family
    /// </summary>
    public int DisplayOrder { get; set; }

    // Navigation properties
    public ICollection<ItemColor> ItemColors { get; set; } = new List<ItemColor>();

    /// <summary>Items that use this color as their display color in inventory lists</summary>
    public ICollection<Item> DisplayColorItems { get; set; } = new List<Item>();
}

/// <summary>
/// Item Color
/// Junction table - assigns one or more colors to an item.
/// Example: T-Shirt is available in Red, Blue, and White ? 3 rows.
/// Each row can optionally link to a specific barcode/SKU variant for that color.
/// </summary>
public class ItemColor : BaseEntity
{
    /// <summary>
    /// Item this color assignment belongs to
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Color from the master color list
    /// </summary>
    public Guid ColorId { get; set; }

    /// <summary>
    /// Optional: links to a specific barcode/SKU for this color variant
    /// (e.g., Item "T-Shirt" + Color "Red" ? Barcode "6281234567890")
    /// </summary>
    public Guid? ItemBarcodeId { get; set; }

    /// <summary>
    /// Whether this is the default/primary display color for the item
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Whether this color variant is currently available / in stock
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    // Navigation properties
    public Item? Item { get; set; }
    public Color? Color { get; set; }
    public ItemBarcode? ItemBarcode { get; set; }
}
