using Nexcore.SharedKernel;

namespace Accounting.Domain.Entities;

/// <summary>
/// Dimension value - individual values for a dimension
/// </summary>
public class DimensionValue : BaseEntity
{
    /// <summary>
    /// Dimension reference
    /// </summary>
    public Guid DimensionId { get; set; }

    /// <summary>
    /// Value code
    /// </summary>
    public required string ValueCode { get; set; }

    /// <summary>
    /// Value name
    /// </summary>
    public required string ValueName { get; set; }

    // Navigation properties
    public Dimension? Dimension { get; set; }
}
