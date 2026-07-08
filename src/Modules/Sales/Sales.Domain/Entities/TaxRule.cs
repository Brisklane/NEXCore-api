using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// Tax Rule - the engine that maps a (product tax category - customer country) combination
/// to the correct TaxGroup to apply.
///
/// The tax engine evaluates rules in Priority order (lowest number = highest priority).
/// First matching rule wins.
///
/// Aligned with:
///   Odoo  - account.fiscal.position (Fiscal Position mapping)
///   Dynamics 365 - Item Sales Tax Group / Customer Tax Group matrix
///   SAP   - Tax Determination Procedure
/// </summary>
public class TaxRule : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Lower number = evaluated first.</summary>
    public int Priority { get; set; } = 10;

    // ? Match Criteria
    /// <summary>
    /// Product-side tax classification (null = matches all categories).
    /// Maps to TaxCategory enum on SalesOrderLine.
    /// </summary>
    public TaxCategory? ProductTaxCategory { get; set; }

    /// <summary>
    /// ISO 3166-1 alpha-2 country code of the ship-to address (null = any country).
    /// </summary>
    public string? CustomerCountryCode { get; set; }

    /// <summary>
    /// Customer type restriction (null = all customer types).
    /// Stored as string to avoid cross-module enum coupling.
    /// e.g., "B2B", "B2C", "WalkIn"
    /// </summary>
    public string? CustomerType { get; set; }

    /// <summary>
    /// Sales channel restriction (null = all channels).
    /// </summary>
    public SalesChannel? SalesChannel { get; set; }

    // ? Outcome
    /// <summary>The TaxGroup to apply when this rule matches.</summary>
    public Guid TaxGroupId { get; set; }
    public TaxGroup TaxGroup { get; set; } = null!;

    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime? ValidTo { get; set; }
}
