using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Standard Cost - predefined planned cost of a manufactured product per unit.
/// Used as the baseline for production variance analysis.
/// SAP Equivalent: Standard Cost Estimate (CK11N / CK40N)
/// Oracle Equivalent: Item Cost (frozen standard cost)
/// </summary>
public class StandardCost : BaseEntity
{
    /// <summary>
    /// Product reference (External - from Inventory module)
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Version of this cost estimate (e.g. 1, 2 for annual updates)
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Currency code for this standard cost
    /// </summary>
    public required string CurrencyCode { get; set; }

    /// <summary>
    /// Standard material cost per unit
    /// </summary>
    public decimal MaterialCost { get; set; }

    /// <summary>
    /// Standard labor cost per unit
    /// </summary>
    public decimal LaborCost { get; set; }

    /// <summary>
    /// Standard machine cost per unit
    /// </summary>
    public decimal MachineCost { get; set; }

    /// <summary>
    /// Standard overhead cost per unit
    /// </summary>
    public decimal OverheadCost { get; set; }

    /// <summary>
    /// Total standard cost per unit (sum of all components)
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Date from which this standard cost is effective
    /// </summary>
    public DateTime EffectiveFrom { get; set; }

    /// <summary>
    /// Date until which this standard cost is effective (null = still current)
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    /// Notes or description of this cost estimate
    /// </summary>
    public string? Notes { get; set; }
}
