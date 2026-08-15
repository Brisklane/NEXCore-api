using System.ComponentModel.DataAnnotations;
using Inv = Inventory.Domain.Constants;
using Inventory.Domain.Enums;

namespace Inventory.Application.DTOs;

// ── Item (updated with new fields) ───────────────────────────────────────────

public class ItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string ItemType { get; set; } = null!;
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }

    /// <summary>
    /// Single display color shown next to the item in inventory lists and POS grids.
    /// Carries HexCode for direct swatch rendering without an extra lookup.
    /// </summary>
    public Guid? DisplayColorId { get; set; }
    public string? DisplayColorName { get; set; }
    public string? DisplayColorHex { get; set; }

    public Guid BaseUnitId { get; set; }
    public Guid? InventoryAccountId { get; set; }
    public Guid? CogsAccountId { get; set; }
    public Guid? PurchaseAccountId { get; set; }
    public Guid? SalesAccountId { get; set; }
    public string CostingMethod { get; set; } = null!;
    public string TrackingType { get; set; } = "None";
    public bool IsBatchTracked { get; set; }
    public bool IsSerialTracked { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }

    // Stock control
    public decimal? ReorderLevel { get; set; }
    public decimal? MaxStockLevel { get; set; }
    public decimal? EconomicOrderQuantity { get; set; }
    public bool AlertOnLowStock { get; set; }
    public bool AlertOnExcessStock { get; set; }

    // Nested collections
    public List<ItemImageDto> Images { get; set; } = new();
    public List<ItemBarcodeDto> Barcodes { get; set; } = new();
    public List<ItemPriceDto> Prices { get; set; } = new();
    public List<ItemAttributeDto> Attributes { get; set; } = new();
    public List<ItemTaxDto> Taxes { get; set; } = new();
    public List<ItemCommentDto> Comments { get; set; } = new();
    public List<ColorDto> Colors { get; set; } = new();

    // ── New fields ───────────────────────────────────────────────────────────────
    public string Condition { get; set; } = null!;
    public int AgeRestriction { get; set; }
    public string? ShortDescription { get; set; }
    public bool HasVariants { get; set; }
    public bool IsComponent { get; set; }
    public bool IsPublished { get; set; }
    public bool IsFeatured { get; set; }

    // New nested collections
    public List<ItemSizeDto> Sizes { get; set; } = new();
    public List<ItemVariantDto> Variants { get; set; } = new();
    public ItemShippingDto? Shipping { get; set; }
    public ItemSeoDto? Seo { get; set; }
    public List<ItemChannelListingDto> ChannelListings { get; set; } = new();
    public List<ItemSupplierDto> Suppliers { get; set; } = new();
    public List<ItemBundleDto> BundleComponents { get; set; } = new();
    public List<ItemSubstitutionDto> Substitutions { get; set; } = new();
    public ItemWarrantyDto? Warranty { get; set; }
    public List<ItemDiscountDto> Discounts { get; set; } = new();
}

public class CreateItemDto
{
    // ── Identity (Required) ───────────────────────────────────────────────────
    [Required(ErrorMessage = "Item code is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Item code must be between 1 and 50 characters")]
    public string Code { get; set; } = null!;

    [Required(ErrorMessage = "Item name is required")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Item name must be between 1 and 255 characters")]
    public string Name { get; set; } = null!;

    /// <summary>Inventory | Service | Bundle | Digital | RawMaterial | FinishedGood</summary>
    [Required(ErrorMessage = "Item type is required")]
    public string ItemType { get; set; } = Inv.ItemType.Inventory;

    public string? ShortDescription { get; set; }
    public string? Description { get; set; }

    // ── Classification (Required & Optional) ──────────────────────────────────
    [Required(ErrorMessage = "Category is required")]
    public Guid CategoryId { get; set; }

    public Guid? BrandId { get; set; }
    public Guid? DisplayColorId { get; set; }

    [Required(ErrorMessage = "Base unit is required")]
    public Guid BaseUnitId { get; set; }

    /// <summary>New | Refurbished | Used | OpenBox</summary>
    [Required(ErrorMessage = "Condition is required")]
    public string Condition { get; set; } = Inv.ItemCondition.New;

    /// <summary>0 = no restriction, 18 = adults only</summary>
    public int AgeRestriction { get; set; }

    // ── Simple Price & Barcode (for basic item creation) ─────────────────────
    /// <summary>Simple barcode value (will be converted to primary barcode)</summary>
    public string? Barcode { get; set; }

    /// <summary>Simple sale price (will be converted to Default price list)</summary>
    [Range(0, double.MaxValue, ErrorMessage = "Sale price must be a positive value")]
    public decimal? SalePrice { get; set; }

    /// <summary>Simple purchase price (will be converted to Default price list)</summary>
    [Range(0, double.MaxValue, ErrorMessage = "Purchase price must be a positive value")]
    public decimal? PurchasePrice { get; set; }

    // ── GL Accounts ───────────────────────────────────────────────────────────
    public Guid? InventoryAccountId { get; set; }
    public Guid? CogsAccountId { get; set; }
    public Guid? PurchaseAccountId { get; set; }
    public Guid? SalesAccountId { get; set; }

    // ── Costing & Tracking ────────────────────────────────────────────────────
    /// <summary>MovingAverage | FIFO | LIFO | Standard</summary>
    public string CostingMethod { get; set; } = Inv.CostingMethod.MovingAverage;

    /// <summary>Unit-level tracking mode: None | Lot | Serial. Derives IsBatchTracked/IsSerialTracked.</summary>
    public string TrackingType { get; set; } = Inv.ItemTrackingType.None;
    public bool IsBatchTracked { get; set; }
    public bool IsSerialTracked { get; set; }
    public bool HasVariants { get; set; }
    public bool IsComponent { get; set; }

    // ── Stock Control ─────────────────────────────────────────────────────────
    public decimal? ReorderLevel { get; set; }
    public decimal? MaxStockLevel { get; set; }
    public decimal? EconomicOrderQuantity { get; set; }
    public bool AlertOnLowStock { get; set; }
    public bool AlertOnExcessStock { get; set; }

    // ── Status ────────────────────────────────────────────────────────────────
    /// <summary>Whether the item is active. Defaults to true.</summary>
    public bool IsActive { get; set; } = true;
    public bool IsPublished { get; set; }
    public bool IsFeatured { get; set; }

    // ── Advanced/Nested collections (all optional for advanced scenarios) ───
    public List<CreateItemImageDto>? Images { get; set; }
    public List<CreateItemBarcodeDto>? Barcodes { get; set; }
    public List<CreateItemPriceDto>? Prices { get; set; }
    public List<AssignItemTaxDto>? Taxes { get; set; }
    public List<UpsertItemAttributeDto>? Attributes { get; set; }

    /// <summary>Color IDs to associate with this item (multi-color support)</summary>
    public List<Guid>? ColorIds { get; set; }

    public List<AssignItemSizeDto>? Sizes { get; set; }
    public List<CreateItemVariantDto>? Variants { get; set; }
    public UpsertItemShippingDto? Shipping { get; set; }
    public UpsertItemSeoDto? Seo { get; set; }
    public List<UpsertItemChannelListingDto>? ChannelListings { get; set; }
    public List<AssignItemSupplierDto>? Suppliers { get; set; }
    public List<AddBundleComponentDto>? BundleComponents { get; set; }
    public List<AddItemSubstitutionDto>? Substitutions { get; set; }
    public UpsertItemWarrantyDto? Warranty { get; set; }
    public List<CreateItemDiscountDto>? Discounts { get; set; }
    public List<CreateItemCommentDto>? Comments { get; set; }
}

public class UpdateItemDto
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? ItemType { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }

    /// <summary>Display color ID — shown in inventory lists and POS grids</summary>
    public Guid? DisplayColorId { get; set; }

    public Guid? InventoryAccountId { get; set; }
    public Guid? CogsAccountId { get; set; }
    public Guid? PurchaseAccountId { get; set; }
    public Guid? SalesAccountId { get; set; }
    /// <summary>Unit-level tracking mode: None | Lot | Serial. Null = leave unchanged.</summary>
    public string? TrackingType { get; set; }
    public bool? IsBatchTracked { get; set; }
    public bool? IsSerialTracked { get; set; }
    public bool? IsActive { get; set; }
    public string? Description { get; set; }
    public decimal? ReorderLevel { get; set; }
    public decimal? MaxStockLevel { get; set; }
    public decimal? EconomicOrderQuantity { get; set; }
    public bool? AlertOnLowStock { get; set; }
    public bool? AlertOnExcessStock { get; set; }

    // ── New fields ───────────────────────────────────────────────────────────────
    public string? Condition { get; set; }
    public int? AgeRestriction { get; set; }
    public string? ShortDescription { get; set; }
    public bool? HasVariants { get; set; }
    public bool? IsComponent { get; set; }
    public bool? IsPublished { get; set; }
    public bool? IsFeatured { get; set; }
    public string? CostingMethod { get; set; }
    public Guid? BaseUnitId { get; set; }
    public List<Guid>? ColorIds { get; set; }

    /// <summary>Quick sale price — upserts the Default price list entry for the base unit.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "Sale price must be a positive value")]
    public decimal? SalePrice { get; set; }

    /// <summary>Quick purchase price — upserts the Default price list entry for the base unit.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "Purchase price must be a positive value")]
    public decimal? PurchasePrice { get; set; }

    /// <summary>
    /// Full replacement set of barcodes. When non-null, the item's barcodes are
    /// synced to exactly this list (rows not present are removed, new ones added).
    /// </summary>
    public List<CreateItemBarcodeDto>? Barcodes { get; set; }

    /// <summary>
    /// Desired set of variants. When non-null the variants are synced to this list:
    /// rows carrying an <c>Id</c> are updated, rows without one are added, and rows
    /// absent from the list are retired. Unlike barcodes this is NOT a delete-and-
    /// reinsert — variant ids are referenced by stock, batches, serials and sales history.
    /// </summary>
    public List<UpdateItemVariantDto>? Variants { get; set; }
}

// ── Brand ─────────────────────────────────────────────────────────────────────

public class BrandDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? Website { get; set; }
    public bool IsActive { get; set; }
}

public class CreateBrandDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? Website { get; set; }
}

public class UpdateBrandDto
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? Website { get; set; }
    public bool? IsActive { get; set; }
}

// ── Item Image ────────────────────────────────────────────────────────────────

public class ItemImageDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string Url { get; set; } = null!;

    /// <summary>Thumbnail | Small | Medium | Large | Original</summary>
    public string Resolution { get; set; } = null!;
    public int? Width { get; set; }
    public int? Height { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? ContentType { get; set; }
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }
}

public class CreateItemImageDto
{
    public string Url { get; set; } = null!;
    public string Resolution { get; set; } = Inv.ImageResolution.Original;
    public int? Width { get; set; }
    public int? Height { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? ContentType { get; set; }
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }
}

// ── Attribute Definition ──────────────────────────────────────────────────────

public class AttributeDefinitionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    /// <summary>Text | Number | Boolean | Date | List</summary>
    public string DataType { get; set; } = null!;
    public string? Unit { get; set; }
    public string? AllowedValues { get; set; }
    public bool IsRequired { get; set; }
    public bool IsVariant { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public class CreateAttributeDefinitionDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string DataType { get; set; } = Inv.AttributeDataType.Text;
    public string? Unit { get; set; }
    public string? AllowedValues { get; set; }
    public bool IsRequired { get; set; }
    public bool IsVariant { get; set; }
    public int DisplayOrder { get; set; }
}

public class UpdateAttributeDefinitionDto
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? DataType { get; set; }
    public string? Unit { get; set; }
    public string? AllowedValues { get; set; }
    public bool? IsRequired { get; set; }
    public bool? IsVariant { get; set; }
    public int? DisplayOrder { get; set; }
    public bool? IsActive { get; set; }
}

// ── Item Attribute ────────────────────────────────────────────────────────────

public class ItemAttributeDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public Guid AttributeDefinitionId { get; set; }
    public string AttributeName { get; set; } = null!;
    public string DataType { get; set; } = null!;
    public string Value { get; set; } = null!;
}

public class UpsertItemAttributeDto
{
    public Guid AttributeDefinitionId { get; set; }
    public string Value { get; set; } = null!;
}

// ── Tax Definition ────────────────────────────────────────────────────────────

public class TaxDefinitionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    /// <summary>VAT | GST | SalesTax | Withholding | Excise | ServiceTax | Custom</summary>
    public string TaxType { get; set; } = null!;

    /// <summary>true = rate is a % (e.g., 15.00 = 15%); false = fixed amount per unit</summary>
    public bool IsPercentage { get; set; }

    public decimal Rate { get; set; }

    /// <summary>Exclusive | Inclusive</summary>
    public string InclusionType { get; set; } = null!;

    public Guid? TaxPayableAccountId { get; set; }
    public Guid? TaxRecoverableAccountId { get; set; }
    public bool ApplyOnSales { get; set; }
    public bool ApplyOnPurchases { get; set; }
    public string? CountryCode { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; }
}

public class CreateTaxDefinitionDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    /// <summary>VAT | GST | SalesTax | Withholding | Excise | ServiceTax | Custom</summary>
    public string TaxType { get; set; } = nameof(Inventory.Domain.Enums.TaxType.VAT);

    public bool IsPercentage { get; set; } = true;
    public decimal Rate { get; set; }

    /// <summary>Exclusive | Inclusive</summary>
    public string InclusionType { get; set; } = nameof(TaxInclusionType.Exclusive);

    public Guid? TaxPayableAccountId { get; set; }
    public Guid? TaxRecoverableAccountId { get; set; }
    public bool ApplyOnSales { get; set; } = true;
    public bool ApplyOnPurchases { get; set; } = true;
    public string? CountryCode { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}

public class UpdateTaxDefinitionDto
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? TaxType { get; set; }
    public bool? IsPercentage { get; set; }
    public decimal? Rate { get; set; }
    public string? InclusionType { get; set; }
    public Guid? TaxPayableAccountId { get; set; }
    public Guid? TaxRecoverableAccountId { get; set; }
    public bool? ApplyOnSales { get; set; }
    public bool? ApplyOnPurchases { get; set; }
    public string? CountryCode { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool? IsActive { get; set; }
}

// ── Item Tax ──────────────────────────────────────────────────────────────────

public class ItemTaxDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public Guid TaxDefinitionId { get; set; }
    public string TaxCode { get; set; } = null!;
    public string TaxName { get; set; } = null!;

    /// <summary>VAT | GST | SalesTax | Withholding | Excise | ServiceTax | Custom</summary>
    public string TaxType { get; set; } = null!;
    public bool IsPercentage { get; set; }
    public string InclusionType { get; set; } = null!;
    public decimal? OverrideRate { get; set; }
    public decimal EffectiveRate { get; set; }
    public bool IsActive { get; set; }
}

public class AssignItemTaxDto
{
    public Guid TaxDefinitionId { get; set; }
    public decimal? OverrideRate { get; set; }
}

// ── Item Barcode ──────────────────────────────────────────────────────────────

public class ItemBarcodeDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public Guid? UnitId { get; set; }
    public string Barcode { get; set; } = null!;

    /// <summary>EAN13 | EAN8 | UPCA | UPCE | Code128 | Code39 | QR | DataMatrix</summary>
    public string BarcodeType { get; set; } = null!;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
}

public class CreateItemBarcodeDto
{
    public Guid? UnitId { get; set; }
    public string Barcode { get; set; } = null!;
    public string BarcodeType { get; set; } = Inv.BarcodeType.EAN13;
    public bool IsPrimary { get; set; }
}

// ── Item Price ────────────────────────────────────────────────────────────────

public class ItemPriceDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public Guid UnitId { get; set; }

    /// <summary>Default | Retail | Wholesale | VIP | B2B | Staff</summary>
    public string PriceList { get; set; } = null!;

    // ── Entered prices
    public decimal SalePrice { get; set; }
    public decimal? MinSalePrice { get; set; }
    public decimal PurchasePrice { get; set; }

    // ── Tax settings
    /// <summary>
    /// true  → SalePrice / PurchasePrice include tax (gross entry)
    /// false → prices are net (tax-exclusive entry)
    /// </summary>
    public bool IsTaxInclusive { get; set; }
    /// <summary>Combined effective tax rate stored as percentage (e.g., 15.00 for 15%)</summary>
    public decimal EffectiveTaxRate { get; set; }

    // ── Computed sale fields (read-only — set by ItemPriceCalculator)
    public decimal SalePriceExcludingTax { get; set; }
    public decimal SaleTaxAmount { get; set; }
    public decimal SalePriceIncludingTax { get; set; }

    // ── Computed purchase fields (read-only — set by ItemPriceCalculator)
    public decimal PurchasePriceExcludingTax { get; set; }
    public decimal PurchaseTaxAmount { get; set; }
    public decimal PurchasePriceIncludingTax { get; set; }

    public string CurrencyCode { get; set; } = null!;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; }
}

public class CreateItemPriceDto
{
    public Guid UnitId { get; set; }
    public string PriceList { get; set; } = Inv.PriceList.Default;

    public decimal SalePrice { get; set; }
    public decimal? MinSalePrice { get; set; }
    public decimal PurchasePrice { get; set; }

    /// <summary>
    /// true  → prices entered are gross (tax-inclusive)
    /// false → prices entered are net (tax-exclusive); default
    /// </summary>
    public bool IsTaxInclusive { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}

// ── Item Comment ──────────────────────────────────────────────────────────────

public class ItemCommentDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }

    /// <summary>Internal | Supplier | Customer | Quality | Handling</summary>
    public string CommentType { get; set; } = null!;
    public string Comment { get; set; } = null!;
    public Guid AuthorUserId { get; set; }
    public bool IsPinned { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateItemCommentDto
{
    public string CommentType { get; set; } = Inv.CommentType.Internal;
    public string Comment { get; set; } = null!;
    public bool IsPinned { get; set; }
}

// ── Color ─────────────────────────────────────────────────────────────────────

public class ColorDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    /// <summary>Hex color value for UI swatch (e.g., #1B2A6B)</summary>
    public string? HexCode { get; set; }
    public byte? R { get; set; }
    public byte? G { get; set; }
    public byte? B { get; set; }
    public string? ColorFamily { get; set; }
    public string? SwatchImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public class CreateColorDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? HexCode { get; set; }
    public byte? R { get; set; }
    public byte? G { get; set; }
    public byte? B { get; set; }
    public string? ColorFamily { get; set; }
    public string? SwatchImageUrl { get; set; }
    public int DisplayOrder { get; set; }
}

public class UpdateColorDto
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? HexCode { get; set; }
    public byte? R { get; set; }
    public byte? G { get; set; }
    public byte? B { get; set; }
    public string? ColorFamily { get; set; }
    public string? SwatchImageUrl { get; set; }
    public int? DisplayOrder { get; set; }
    public bool? IsActive { get; set; }
}

// ── Size ──────────────────────────────────────────────────────────────────────

public class SizeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    /// <summary>Apparel | Footwear | Ring | Screen | Weight | Custom</summary>
    public string SizeChart { get; set; } = null!;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class CreateSizeDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string SizeChart { get; set; } = Inv.SizeChart.Apparel;
    public int SortOrder { get; set; }
}

public class UpdateSizeDto
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? SizeChart { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
}

// ── Item Size ─────────────────────────────────────────────────────────────────

public class ItemSizeDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public Guid SizeId { get; set; }
    public string SizeCode { get; set; } = null!;
    public string SizeName { get; set; } = null!;
    public Guid? ItemBarcodeId { get; set; }
    public bool IsDefault { get; set; }
    public bool IsAvailable { get; set; }
}

public class AssignItemSizeDto
{
    public Guid SizeId { get; set; }
    public Guid? ItemBarcodeId { get; set; }
    public bool IsDefault { get; set; }
}

// ── Item Variant ──────────────────────────────────────────────────────────────

public class ItemVariantDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string VariantCode { get; set; } = null!;
    public string VariantName { get; set; } = null!;
    public Guid? ColorId { get; set; }
    public string? ColorName { get; set; }
    public string? ColorHex { get; set; }
    public Guid? SizeId { get; set; }
    public string? SizeName { get; set; }
    public string? ExtraDimension { get; set; }
    public string? Barcode { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? SalePriceOverride { get; set; }
    public decimal? PurchasePriceOverride { get; set; }
    public decimal? WeightKg { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// A variant in an update payload. <see cref="Id"/> identifies an existing row to edit;
/// omit it to add a new one. Variants missing from the list are retired (soft-deleted),
/// never hard-deleted — stock balances, batches, serials and past POS lines all point at
/// variant ids, and removing the row would orphan them.
/// </summary>
public class UpdateItemVariantDto
{
    public Guid? Id { get; set; }
    public string VariantCode { get; set; } = null!;
    public string VariantName { get; set; } = null!;
    public string? Barcode { get; set; }
    public Guid? ColorId { get; set; }
    public Guid? SizeId { get; set; }
    public string? ExtraDimension { get; set; }
    public decimal? SalePriceOverride { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CreateItemVariantDto
{
    public string VariantCode { get; set; } = null!;
    public string VariantName { get; set; } = null!;
    public Guid? ColorId { get; set; }
    public Guid? SizeId { get; set; }
    public string? ExtraDimension { get; set; }
    public string? Barcode { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? SalePriceOverride { get; set; }
    public decimal? PurchasePriceOverride { get; set; }
    public decimal? WeightKg { get; set; }
    public int DisplayOrder { get; set; }
}

// ── Item Shipping ─────────────────────────────────────────────────────────────

public class ItemShippingDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? LengthCm { get; set; }
    public decimal? WidthCm { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? VolumetricWeightKg { get; set; }
    public string? CountryOfOrigin { get; set; }
    public string? HsCode { get; set; }
    public int? UnitsPerCarton { get; set; }
    public int? CartonsPerPallet { get; set; }
    public decimal? CartonWeightKg { get; set; }
    public decimal? CartonLengthCm { get; set; }
    public decimal? CartonWidthCm { get; set; }
    public decimal? CartonHeightCm { get; set; }
    public bool RequiresSpecialHandling { get; set; }
    public string? HandlingNotes { get; set; }
    public bool IsHazmat { get; set; }
    public bool IsShippableInternational { get; set; }
}

public class UpsertItemShippingDto
{
    public decimal? WeightKg { get; set; }
    public decimal? LengthCm { get; set; }
    public decimal? WidthCm { get; set; }
    public decimal? HeightCm { get; set; }
    public string? CountryOfOrigin { get; set; }
    public string? HsCode { get; set; }
    public int? UnitsPerCarton { get; set; }
    public int? CartonsPerPallet { get; set; }
    public decimal? CartonWeightKg { get; set; }
    public decimal? CartonLengthCm { get; set; }
    public decimal? CartonWidthCm { get; set; }
    public decimal? CartonHeightCm { get; set; }
    public bool RequiresSpecialHandling { get; set; }
    public string? HandlingNotes { get; set; }
    public bool IsHazmat { get; set; }
    public bool IsShippableInternational { get; set; } = true;
}

// ── Item SEO ──────────────────────────────────────────────────────────────────

public class ItemSeoDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public string? Slug { get; set; }
    public string? CanonicalUrl { get; set; }
}

public class UpsertItemSeoDto
{
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public string? Slug { get; set; }
    public string? CanonicalUrl { get; set; }
}

// ── Item Channel Listing ──────────────────────────────────────────────────────

public class ItemChannelListingDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    /// <summary>POS | DARAZ | ALIEXPRESS | CASH_CARRY | B2B | WEBSITE | MOBILE_APP</summary>
    public string Channel { get; set; } = null!;
    /// <summary>Draft | Active | Paused | Rejected | OutOfStock | Discontinued</summary>
    public string ListingStatus { get; set; } = null!;
    public string? ChannelTitle { get; set; }
    public string? ChannelDescription { get; set; }
    public decimal? ChannelPrice { get; set; }
    public decimal? ChannelDiscount { get; set; }
    public string? DiscountType { get; set; }
    public string? ExternalProductId { get; set; }
    public string? ExternalSku { get; set; }
    public string? ListingUrl { get; set; }
    public DateTime? ListedAt { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public bool AutoSyncStock { get; set; }
    public bool AutoSyncPrice { get; set; }
}

public class UpsertItemChannelListingDto
{
    public string Channel { get; set; } = null!;
    public string ListingStatus { get; set; } = Inv.ListingStatus.Draft;
    public string? ChannelTitle { get; set; }
    public string? ChannelDescription { get; set; }
    public decimal? ChannelPrice { get; set; }
    public decimal? ChannelDiscount { get; set; }
    public string? DiscountType { get; set; }
    public string? ExternalProductId { get; set; }
    public string? ExternalSku { get; set; }
    public bool AutoSyncStock { get; set; } = true;
    public bool AutoSyncPrice { get; set; } = true;
}

// ── Item Supplier ─────────────────────────────────────────────────────────────

public class ItemSupplierDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public Guid SupplierId { get; set; }
    public string? SupplierItemCode { get; set; }
    public string? SupplierItemName { get; set; }
    public decimal? LastPurchasePrice { get; set; }
    public string CurrencyCode { get; set; } = null!;
    public decimal? MinOrderQuantity { get; set; }
    public int? LeadTimeDays { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastPurchaseDate { get; set; }
}

public class AssignItemSupplierDto
{
    public Guid SupplierId { get; set; }
    public string? SupplierItemCode { get; set; }
    public string? SupplierItemName { get; set; }
    public decimal? LastPurchasePrice { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal? MinOrderQuantity { get; set; }
    public int? LeadTimeDays { get; set; }
    public bool IsPrimary { get; set; }
}

// ── Item Bundle ───────────────────────────────────────────────────────────────

public class ItemBundleDto
{
    public Guid Id { get; set; }
    public Guid BundleItemId { get; set; }
    public Guid ComponentItemId { get; set; }
    public string ComponentItemCode { get; set; } = null!;
    public string ComponentItemName { get; set; } = null!;
    public decimal Quantity { get; set; }
    public Guid UnitId { get; set; }
    public string UnitCode { get; set; } = null!;
    public bool IsIncludedInCost { get; set; }
    public int DisplayOrder { get; set; }
}

public class AddBundleComponentDto
{
    public Guid ComponentItemId { get; set; }
    public decimal Quantity { get; set; } = 1;
    public Guid UnitId { get; set; }
    public bool IsIncludedInCost { get; set; } = true;
    public int DisplayOrder { get; set; }
}

// ── Item Substitution ─────────────────────────────────────────────────────────

public class ItemSubstitutionDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public Guid SubstituteItemId { get; set; }
    public string SubstituteItemCode { get; set; } = null!;
    public string SubstituteItemName { get; set; } = null!;
    public int Priority { get; set; }
    public string? Note { get; set; }
    public bool IsBidirectional { get; set; }
    public bool IsActive { get; set; }
}

public class AddItemSubstitutionDto
{
    public Guid SubstituteItemId { get; set; }
    public int Priority { get; set; } = 1;
    public string? Note { get; set; }
    public bool IsBidirectional { get; set; } = true;
}

// ── Item Warranty ─────────────────────────────────────────────────────────────

public class ItemWarrantyDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    /// <summary>Seller | Brand | NoWarranty | International | Local</summary>
    public string WarrantyType { get; set; } = null!;
    public int DurationMonths { get; set; }
    public string? PolicyDescription { get; set; }
    public string? ProviderName { get; set; }
    public string? ProviderContact { get; set; }
    public bool IsActive { get; set; }
}

public class UpsertItemWarrantyDto
{
    public string WarrantyType { get; set; } = Inv.WarrantyType.Seller;
    public int DurationMonths { get; set; }
    public string? PolicyDescription { get; set; }
    public string? ProviderName { get; set; }
    public string? ProviderContact { get; set; }
}

// ── Item Discount ─────────────────────────────────────────────────────────────

public class ItemDiscountDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string Name { get; set; } = null!;
    /// <summary>Percentage | FixedAmount | BuyXGetY | VolumePrice</summary>
    public string DiscountType { get; set; } = null!;
    public decimal DiscountValue { get; set; }
    public int? BuyQuantity { get; set; }
    public int? GetQuantity { get; set; }
    public decimal? MinQuantity { get; set; }
    public decimal? MaxQuantity { get; set; }
    /// <summary>Comma-separated channels: POS | DARAZ | ALIEXPRESS | CASH_CARRY | B2B</summary>
    public string? ApplicableChannels { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool IsActive { get; set; }
    public int Priority { get; set; }
}

public class CreateItemDiscountDto
{
    public string Name { get; set; } = null!;
    public string DiscountType { get; set; } = Inv.DiscountType.Percentage;
    public decimal DiscountValue { get; set; }
    public int? BuyQuantity { get; set; }
    public int? GetQuantity { get; set; }
    public decimal? MinQuantity { get; set; }
    public decimal? MaxQuantity { get; set; }
    public string? ApplicableChannels { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public int Priority { get; set; } = 1;
}

// ── Item Basic (Lightweight) ──────────────────────────────────────────────────

/// <summary>
/// Lightweight DTO for items to be used in dropdowns and lists.
/// Contains only basic information without heavy nested collections like images, variants, etc.
/// </summary>
public class ItemBasicDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string ItemType { get; set; } = null!;
    public string? ShortDescription { get; set; }

    /// <summary>Primary barcode for quick identification</summary>
    public string? Barcode { get; set; }

    /// <summary>Default sale price (from Default price list)</summary>
    public decimal? SalePrice { get; set; }

    /// <summary>Default purchase price (from Default price list)</summary>
    public decimal? PurchasePrice { get; set; }

    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }

    public Guid? BrandId { get; set; }
    public string? BrandName { get; set; }

    public Guid BaseUnitId { get; set; }
    public string? BaseUnitCode { get; set; }
    public string? BaseUnitName { get; set; }

    public bool IsActive { get; set; }
    public string TrackingType { get; set; } = "None";
    public bool IsBatchTracked { get; set; }
    public bool IsSerialTracked { get; set; }
}
