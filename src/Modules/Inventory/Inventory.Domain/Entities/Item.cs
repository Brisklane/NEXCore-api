using Inv = Inventory.Domain.Constants;
using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Item (Product Master)
/// Master data for inventory items.
/// Supports: GL accounts, costing, stock control, images, barcodes, prices,
/// attributes, taxes, colors, sizes, variants, shipping, SEO, channel listings,
/// suppliers, bundles, substitutions, warranty, discounts, and comments.
/// Suitable for POS, Cash &amp; Carry, Daraz, AliExpress, B2B portals.
/// </summary>
public class Item : BaseEntity
{
    // Identity

    /// <summary>Item code (SKU)</summary>
    public new string Code { get; set; } = null!;

    /// <summary>Item name/description</summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Item type: Inventory | Service | Bundle | Digital | RawMaterial | FinishedGood
    /// </summary>
    public string ItemType { get; set; } = Inv.ItemType.Inventory;

    /// <summary>Short description (used in POS receipt and marketplace subtitle)</summary>
    public string? ShortDescription { get; set; }

    // Classification

    /// <summary>Category ID</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Brand ID (optional)</summary>
    public Guid? BrandId { get; set; }

    /// <summary>
    /// Display color ID - single color shown next to the item in inventory lists and POS grids.
    /// Independent of the ItemColor variants collection.
    /// </summary>
    public Guid? DisplayColorId { get; set; }

    /// <summary>Base Unit of Measure ID</summary>
    public Guid BaseUnitId { get; set; }

    /// <summary>Item condition: New | Refurbished | Used | OpenBox</summary>
    public string Condition { get; set; } = Inv.ItemCondition.New;

    /// <summary>
    /// Age / maturity restriction (0 = no restriction, 18 = adults only).
    /// Used for marketplace compliance (alcohol, tobacco, etc.)
    /// </summary>
    public int AgeRestriction { get; set; }

    // GL Accounts

    /// <summary>GL Account ID for inventory (Balance Sheet)</summary>
    public Guid? InventoryAccountId { get; set; }

    /// <summary>GL Account ID for COGS (Income Statement)</summary>
    public Guid? CogsAccountId { get; set; }

    /// <summary>GL Account ID for purchases (Asset/Expense)</summary>
    public Guid? PurchaseAccountId { get; set; }

    /// <summary>GL Account ID for sales (Revenue)</summary>
    public Guid? SalesAccountId { get; set; }

    // Effective GL Accounts (override chain: Item ? Category ? null)

    /// <summary>Item-level GL override, falling back to the parent category default.</summary>
    public Guid? EffectiveInventoryAccountId => InventoryAccountId ?? Category?.InventoryAccountId;

    /// <summary>Item-level GL override, falling back to the parent category default.</summary>
    public Guid? EffectiveCogsAccountId => CogsAccountId ?? Category?.CogsAccountId;

    /// <summary>Item-level GL override, falling back to the parent category default.</summary>
    public Guid? EffectivePurchaseAccountId => PurchaseAccountId ?? Category?.PurchaseAccountId;

    /// <summary>Item-level GL override, falling back to the parent category default.</summary>
    public Guid? EffectiveSalesAccountId => SalesAccountId ?? Category?.SalesAccountId;

    // Costing &amp; Tracking

    /// <summary>Costing method: FIFO | MovingAverage | Standard | LIFO</summary>
    public string CostingMethod { get; set; } = Inv.CostingMethod.MovingAverage;

    /// <summary>
    /// Unit-level tracking mode — None | Lot | Serial (see <see cref="Inv.ItemTrackingType"/>).
    /// Single source of truth; <see cref="IsBatchTracked"/> / <see cref="IsSerialTracked"/> are kept
    /// in sync from this so existing queries and UI badges keep working.
    /// </summary>
    public string TrackingType { get; set; } = Inv.ItemTrackingType.None;

    /// <summary>Whether item is tracked by batch/lot number (derived: TrackingType == Lot)</summary>
    public bool IsBatchTracked { get; set; }

    /// <summary>Whether item is tracked by serial number, 1 serial per unit (derived: TrackingType == Serial)</summary>
    public bool IsSerialTracked { get; set; }

    /// <summary>Whether item has color/size/custom variants</summary>
    public bool HasVariants { get; set; }

    /// <summary>Whether item can be sold as part of a bundle</summary>
    public bool IsComponent { get; set; }

    // Stock Control

    /// <summary>Minimum / reorder stock level - alert triggers when stock ? this</summary>
    public decimal? ReorderLevel { get; set; }

    /// <summary>Maximum stock level - alert triggers when stock &gt; this</summary>
    public decimal? MaxStockLevel { get; set; }

    /// <summary>Economic / preferred order quantity</summary>
    public decimal? EconomicOrderQuantity { get; set; }

    /// <summary>Send alert when stock falls at or below ReorderLevel</summary>
    public bool AlertOnLowStock { get; set; }

    /// <summary>Send alert when stock exceeds MaxStockLevel</summary>
    public bool AlertOnExcessStock { get; set; }

    // Status

    /// <summary>Whether item is published / visible on e-commerce portals</summary>
    public bool IsPublished { get; set; }

    /// <summary>Whether item is featured (shown in banners, home page, POS quick-access)</summary>
    public bool IsFeatured { get; set; }

    // Navigation properties

    public Unit? BaseUnit { get; set; }
    public ItemCategory? Category { get; set; }

    /// <summary>Brand (manufacturer)</summary>
    public Brand? Brand { get; set; }

    /// <summary>Single display color for inventory lists and POS grids</summary>
    public Color? DisplayColor { get; set; }

    // Item Detail Collections

    /// <summary>Images in multiple resolutions</summary>
    public ICollection<ItemImage> Images { get; set; } = new List<ItemImage>();

    /// <summary>Barcodes - per UOM (EAN13, QR, Code128, etc.)</summary>
    public ICollection<ItemBarcode> Barcodes { get; set; } = new List<ItemBarcode>();

    /// <summary>Sales and purchase prices per UOM and price list</summary>
    public ICollection<ItemPrice> Prices { get; set; } = new List<ItemPrice>();

    /// <summary>Typed attribute values (Color, Size, Weight, Material, etc.)</summary>
    public ICollection<ItemAttribute> Attributes { get; set; } = new List<ItemAttribute>();

    /// <summary>Taxes applicable (VAT, GST, Excise, etc.)</summary>
    public ICollection<ItemTax> Taxes { get; set; } = new List<ItemTax>();

    /// <summary>Available color variants</summary>
    public ICollection<ItemColor> Colors { get; set; } = new List<ItemColor>();

    /// <summary>Available size variants</summary>
    public ICollection<ItemSize> Sizes { get; set; } = new List<ItemSize>();

    /// <summary>SKU variants (Color + Size + extra dimension combinations)</summary>
    public ICollection<ItemVariant> Variants { get; set; } = new List<ItemVariant>();

    /// <summary>Shipping / logistics properties (weight, dimensions, HS code)</summary>
    public ItemShipping? Shipping { get; set; }

    /// <summary>SEO metadata (slug, meta title, meta description)</summary>
    public ItemSeo? Seo { get; set; }

    /// <summary>Per-channel listing status and overrides (Daraz, AliExpress, POS, etc.)</summary>
    public ICollection<ItemChannelListing> ChannelListings { get; set; } = new List<ItemChannelListing>();

    /// <summary>Supplier pricing and lead time records</summary>
    public ICollection<ItemSupplier> Suppliers { get; set; } = new List<ItemSupplier>();

    /// <summary>Bundle components (when this item IS the bundle)</summary>
    public ICollection<ItemBundle> BundleComponents { get; set; } = new List<ItemBundle>();

    /// <summary>Bundles this item belongs to as a component</summary>
    public ICollection<ItemBundle> BundleParents { get; set; } = new List<ItemBundle>();

    /// <summary>Substitute / alternative items when out of stock</summary>
    public ICollection<ItemSubstitution> Substitutions { get; set; } = new List<ItemSubstitution>();

    /// <summary>Warranty terms (type, duration, policy)</summary>
    public ItemWarranty? Warranty { get; set; }

    /// <summary>Promotional discounts (flash sale, volume pricing, buy-X-get-Y)</summary>
    public ICollection<ItemDiscount> Discounts { get; set; } = new List<ItemDiscount>();

    /// <summary>Internal and supplier notes</summary>
    public ICollection<ItemComment> Comments { get; set; } = new List<ItemComment>();

    // Inventory Collections

    public ICollection<ItemUomConversion> UomConversions { get; set; } = new List<ItemUomConversion>();
    public ICollection<InventoryBalance> Balances { get; set; } = new List<InventoryBalance>();
    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
    public ICollection<InventoryValuation> Valuations { get; set; } = new List<InventoryValuation>();
    public ICollection<InventoryCostLayer> CostLayers { get; set; } = new List<InventoryCostLayer>();

    /// <summary>Serialized units (TrackingType = Serial).</summary>
    public ICollection<ItemSerial> Serials { get; set; } = new List<ItemSerial>();

    /// <summary>Batches / lots (TrackingType = Lot).</summary>
    public ICollection<ItemBatch> Batches { get; set; } = new List<ItemBatch>();
}
