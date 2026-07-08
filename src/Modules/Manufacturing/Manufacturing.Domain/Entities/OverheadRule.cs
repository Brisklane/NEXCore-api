using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Overhead Rule - defines how overhead costs are calculated and allocated to production orders.
/// Can be applied globally, per work center, or per cost center.
/// SAP Equivalent: Costing Sheet / Overhead Rate (KZS2)
/// Oracle Equivalent: Overhead Definition
/// </summary>
public class OverheadRule : BaseEntity
{
    /// <summary>
    /// Unique code for the overhead rule
    /// </summary>
    public new required string Code { get; set; }

    /// <summary>
    /// Display name of the overhead rule
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Work center this rule applies to (null = applies globally to all work centers)
    /// </summary>
    public Guid? WorkCenterId { get; set; }

    /// <summary>
    /// Type of overhead rate calculation
    /// </summary>
    public required string RateType { get; set; } // Percentage, PerHour, Fixed

    /// <summary>
    /// Rate value — percentage (e.g. 15 for 15%), per-hour amount, or fixed amount
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Cost basis on which this overhead is calculated
    /// </summary>
    public required string AppliesTo { get; set; } // Labor, Machine, Material, TotalCost

    /// <summary>
    /// Date from which this overhead rule is effective
    /// </summary>
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>
    /// Date until which this overhead rule is effective
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    /// Notes and remarks
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public WorkCenter? WorkCenter { get; set; }
}
