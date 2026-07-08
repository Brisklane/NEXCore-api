namespace Inventory.Domain.Enums;

/// <summary>
/// Specifies whether the tax amount is included in the displayed/entered price,
/// or added on top of it.
///
/// This is a product-master-level setting stored on TaxDefinition
/// and controls how ItemPrice.SalePrice is interpreted when calculating tax.
///
/// Aligned with:
///   Odoo  — account.tax.price_include
///   Dynamics 365 — Sales Tax Code "Prices include sales tax"
///   SAP   — Tax Procedure condition type BASB vs MWST
/// </summary>
public enum TaxInclusionType
{
    /// <summary>
    /// Tax is added on top of the net price.
    /// Display: 100 + 15% VAT = 115 total.
    /// </summary>
    Exclusive = 0,

    /// <summary>
    /// Tax is embedded in the displayed price.
    /// Display: 115 (includes 15% VAT) ? net = 100.
    /// </summary>
    Inclusive = 1
}
