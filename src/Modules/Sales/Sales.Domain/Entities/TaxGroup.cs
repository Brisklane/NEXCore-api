using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// Tax Group - bundles one or more TaxRate components that are applied together.
///
/// Example:
///   "US-CA Sales Tax" group = [Federal 0%, CA State 7.25%, LA County 1%]
///   "Pakistan GST 17%"      = [Standard GST 17%]
///
/// Aligned with:
///   Odoo  - account.tax.group
///   Dynamics 365 - Sales Tax Group
///   SAP   - Tax Classification / Condition group
/// </summary>
public class TaxGroup : BaseEntity
{
    public new string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    // ? Navigation
    public ICollection<TaxGroupRate> Rates { get; set; } = new List<TaxGroupRate>();
    public ICollection<TaxRule> Rules { get; set; } = new List<TaxRule>();
}
