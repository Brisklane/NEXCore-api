using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Bill of Materials - defines the product structure and recipe of a finished product
/// </summary>
public class BillOfMaterial : BaseEntity
{
    /// <summary>
    /// Finished product reference (External - from Inventory module)
    /// </summary>
    public Guid FinishedProductId { get; set; }

    /// <summary>
    /// BOM version number
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Effective from date
    /// </summary>
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>
    /// Effective to date
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    /// Notes and remarks
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public ICollection<BOMItem>? Items { get; set; }
    public ICollection<BOMByProduct>? ByProducts { get; set; }
}