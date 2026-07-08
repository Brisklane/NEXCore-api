using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// Price List header.
/// Aligned with SAP Pricing Condition Records, Oracle Price Lists, and Dynamics Price Lists.
/// Currency is referenced by ISO 4217 code string (e.g., "USD", "EUR") - same pattern as
/// Inventory.Domain.Entities.ItemPrice - no cross-module entity navigation.
/// </summary>
public class PriceList : BaseEntity
{
    public new string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PriceListType Type { get; set; } = PriceListType.Standard;

    /// <summary>ISO 4217 currency code, e.g., "USD", "EUR", "PKR".</summary>
    public string CurrencyCode { get; set; } = "USD";

    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    /// <summary>Whether prices in this list include tax.</summary>
    public bool IsTaxInclusive { get; set; }

    // ? Navigation
    public ICollection<PriceListItem> Items { get; set; } = new List<PriceListItem>();
}
