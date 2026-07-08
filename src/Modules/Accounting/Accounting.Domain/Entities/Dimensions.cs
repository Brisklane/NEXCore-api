using Nexcore.SharedKernel;

namespace Accounting.Domain.Entities;

/// <summary>
/// Dimension - financial dimension like cost center, department
/// </summary>
public class Dimension : BaseEntity
{
    /// <summary>
    /// Dimension code
    /// </summary>
    public new required string Code { get; set; }

    /// <summary>
    /// Dimension name
    /// </summary>
    public required string Name { get; set; }

    // Navigation properties
    public ICollection<DimensionValue> Values { get; set; } = [];
}
