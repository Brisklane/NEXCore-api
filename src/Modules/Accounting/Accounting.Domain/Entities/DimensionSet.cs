using Nexcore.SharedKernel;

namespace Accounting.Domain.Entities;

/// <summary>
/// Dimension set - collection of dimension values for a transaction
/// </summary>
public class DimensionSet : BaseEntity
{
    /// <summary>
    /// Hash of dimension values for quick lookup
    /// </summary>
    public required string HashCode { get; set; }

    // Navigation properties
    public ICollection<DimensionSetItem> Items { get; set; } = [];
}
