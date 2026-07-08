using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Item Price
/// Stores sales and purchase prices for an item.
/// Supports multiple price lists (Retail, Wholesale, VIP, etc.)
/// and per-UOM pricing (e.g., unit price vs. box price).
///
/// Tax handling:
///   IsTaxInclusive = true  ? SalePrice already includes tax (tax-inclusive / gross price)
///   IsTaxInclusive = false ? SalePrice is the base price before tax (tax-exclusive / net price)
///
/// Computed fields (SalePriceExcludingTax, SaleTaxAmount, SalePriceIncludingTax,
/// PurchasePriceExcludingTax, PurchaseTaxAmount, PurchasePriceIncludingTax)
/// are populated by ItemPriceCalculator and stored for fast reads.
/// </summary>
public class ItemPrice : BaseEntity
{
    /// <summary>Item this price record belongs to</summary>
    public Guid ItemId { get; set; }

    /// <summary>Unit ID - which UOM this price applies to</summary>
    public Guid UnitId { get; set; }

    /// <summary>Price list name/code: Default | Retail | Wholesale | VIP | B2B | Staff</summary>
    public string PriceList { get; set; } = "Default";

    // Sale Price

    /// <summary>
    /// Entered sale price.
    /// Interpretation depends on IsTaxInclusive:
    ///   true  ? this IS the gross price (tax already inside)
    ///   false ? this is the net price (tax not yet added)
    /// </summary>
    public decimal SalePrice { get; set; }

    /// <summary>Minimum sale price floor - cannot sell below this (net/exclusive basis)</summary>
    public decimal? MinSalePrice { get; set; }

    /// <summary>
    /// Sale price before tax regardless of how SalePrice was entered.
    /// Populated by ItemPriceCalculator.
    /// </summary>
    public decimal SalePriceExcludingTax { get; set; }

    /// <summary>
    /// Total tax amount on the sale price.
    /// Populated by ItemPriceCalculator.
    /// </summary>
    public decimal SaleTaxAmount { get; set; }

    /// <summary>
    /// Sale price including all taxes (gross price).
    /// Populated by ItemPriceCalculator.
    /// </summary>
    public decimal SalePriceIncludingTax { get; set; }

    // Purchase / Cost Price

    /// <summary>
    /// Entered purchase (cost) price - last known supplier price.
    /// Interpretation depends on IsTaxInclusive.
    /// </summary>
    public decimal PurchasePrice { get; set; }

    /// <summary>Purchase price before tax. Populated by ItemPriceCalculator.</summary>
    public decimal PurchasePriceExcludingTax { get; set; }

    /// <summary>Total tax amount on the purchase price. Populated by ItemPriceCalculator.</summary>
    public decimal PurchaseTaxAmount { get; set; }

    /// <summary>Purchase price including all taxes. Populated by ItemPriceCalculator.</summary>
    public decimal PurchasePriceIncludingTax { get; set; }

    // Tax Settings

    /// <summary>
    /// Whether SalePrice and PurchasePrice are entered tax-inclusive (gross).
    ///   true  ? prices entered already include tax ? calculator extracts tax
    ///   false ? prices entered are net ? calculator adds tax on top
    /// Defaults to false (tax-exclusive entry is the standard for B2B/wholesale).
    /// </summary>
    public bool IsTaxInclusive { get; set; }

    /// <summary>
    /// Combined effective tax rate applied to this price record (sum of all active item taxes).
    /// Stored here after calculation so the price line is self-contained for reporting.
    /// </summary>
    public decimal EffectiveTaxRate { get; set; }

    // Currency & Validity

    /// <summary>Currency code (e.g., USD, PKR, SAR, AED)</summary>
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>Valid from date - null means always valid from now</summary>
    public DateTime? ValidFrom { get; set; }

    /// <summary>Valid to date - null means no expiry</summary>
    public DateTime? ValidTo { get; set; }

    // Navigation properties
    public Item? Item { get; set; }
    public Unit? Unit { get; set; }
}
