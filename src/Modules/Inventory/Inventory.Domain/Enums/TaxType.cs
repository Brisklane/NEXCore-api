namespace Inventory.Domain.Enums;

/// <summary>
/// The nature of a tax component — determines regulatory classification and GL posting behavior.
/// Applies to TaxDefinition (the rate master record).
///
/// Aligned with:
///   Odoo  — account.tax.type_tax_use / tax_group
///   Dynamics 365 — Sales Tax Code type
///   SAP   — Tax Category (MWSKZ) type indicator
/// </summary>
public enum TaxType
{
    /// <summary>Value Added Tax — applied at each stage of production/sale chain.</summary>
    VAT = 0,

    /// <summary>Goods and Services Tax — single-stage consumption tax (AU, NZ, CA, IN, PK).</summary>
    GST = 1,

    /// <summary>Retail/state-level sales tax (US-style — applied only at final sale).</summary>
    SalesTax = 2,

    /// <summary>Withholding tax — deducted at source before payment (income/dividend tax).</summary>
    Withholding = 3,

    /// <summary>Excise tax — levied on specific goods (fuel, alcohol, tobacco).</summary>
    Excise = 4,

    /// <summary>Service tax — applied to service-only transactions.</summary>
    ServiceTax = 5,

    /// <summary>Custom / user-defined tax type for local regulations.</summary>
    Custom = 99
}
