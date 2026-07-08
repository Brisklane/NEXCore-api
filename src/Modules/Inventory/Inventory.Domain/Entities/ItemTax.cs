using Nexcore.SharedKernel;
using Inventory.Domain.Enums;

namespace Inventory.Domain.Entities;

/// <summary>
/// Tax Definition - the rate master record for a single tax component.
/// Defined once per company/branch and reused across all items and transactions.
///
/// Multiple TaxDefinitions are bundled into a Sales.TaxGroup by the Sales module's
/// tax determination engine. The Sales module references TaxDefinition by cross-module Guid only.
///
/// Aligned with:
///   Odoo  - account.tax
///   Dynamics 365 - Sales Tax Code
///   SAP   - Tax Code (MWSKZ) rate component
/// </summary>
public class TaxDefinition : BaseEntity
{
    /// <summary>Short tax code used in documents and rules (e.g., "VAT15", "GST5", "EXCISE10").</summary>
    public new string Code { get; set; } = null!;

    /// <summary>Human-readable name (e.g., "Value Added Tax 15%").</summary>
    public string Name { get; set; } = null!;

    /// <summary>Regulatory classification of this tax component.</summary>
    public TaxType TaxType { get; set; } = TaxType.VAT;

    /// <summary>
    /// Whether the rate is expressed as a percentage (e.g., 15.00 = 15%)
    /// or as a fixed amount per unit (e.g., 0.50 = $0.50 per item).
    /// </summary>
    public bool IsPercentage { get; set; } = true;

    /// <summary>
    /// Rate value - percentage (e.g., 15.00) or fixed amount per unit depending on IsPercentage.
    /// </summary>
    public decimal Rate { get; set; }

    /// <summary>Whether the tax is included in the entered/displayed price or added on top.</summary>
    public TaxInclusionType InclusionType { get; set; } = TaxInclusionType.Exclusive;

    /// <summary>
    /// GL Account ID for tax payable (sales) / tax receivable (purchases).
    /// Cross-module Guid to Accounting.LedgerAccount.
    /// </summary>
    public Guid? TaxPayableAccountId { get; set; }

    /// <summary>
    /// GL Account ID for recoverable input tax (purchases only).
    /// Cross-module Guid to Accounting.LedgerAccount.
    /// </summary>
    public Guid? TaxRecoverableAccountId { get; set; }

    /// <summary>Whether this tax applies on outgoing sales transactions.</summary>
    public bool ApplyOnSales { get; set; } = true;

    /// <summary>Whether this tax applies on incoming purchase transactions.</summary>
    public bool ApplyOnPurchases { get; set; } = true;

    /// <summary>ISO 3166-1 alpha-2 country this rate applies to (null = all countries).</summary>
    public string? CountryCode { get; set; }

    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime? ValidTo { get; set; }

    // ? Navigation
    public ICollection<ItemTax> ItemTaxes { get; set; } = new List<ItemTax>();
}

/// <summary>
/// Item Tax - junction table that links a TaxDefinition to an Item.
/// An item can have multiple taxes applied simultaneously (e.g., VAT + Excise).
/// Each row can override the default rate for that specific item.
/// </summary>
public class ItemTax : BaseEntity
{
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public Guid TaxDefinitionId { get; set; }
    public TaxDefinition TaxDefinition { get; set; } = null!;

    /// <summary>Item-specific rate override. Null = use TaxDefinition.Rate.</summary>
    public decimal? OverrideRate { get; set; }

    /// <summary>Effective rate = OverrideRate ?? TaxDefinition.Rate. Stored for fast reads.</summary>
    public decimal EffectiveRate { get; set; }

}
