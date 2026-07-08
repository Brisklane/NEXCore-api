using Nexcore.SharedKernel;

namespace Accounting.Domain.Entities;

/// <summary>
/// Dimension set item - individual dimension in a dimension set
/// </summary>
public class DimensionSetItem : BaseEntity
{
    /// <summary>
    /// Dimension set reference
    /// </summary>
    public Guid DimensionSetId { get; set; }

    /// <summary>
    /// Dimension reference
    /// </summary>
    public Guid DimensionId { get; set; }

    /// <summary>
    /// Dimension value reference
    /// </summary>
    public Guid DimensionValueId { get; set; }

    // Navigation properties
    public DimensionSet? DimensionSet { get; set; }
    public Dimension? Dimension { get; set; }
    public DimensionValue? DimensionValue { get; set; }
}
