using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// Junction: Sales Representative assigned to a Territory.
/// </summary>
public class SalesRepTerritory : BaseEntity
{
    public Guid SalesTerritoryId { get; set; }
    public SalesTerritory SalesTerritory { get; set; } = null!;

    /// <summary>FK to the Users module.</summary>
    public Guid SalesRepId { get; set; }

    public DateTime AssignedFrom { get; set; } = DateTime.UtcNow;
    public DateTime? AssignedTo { get; set; }
}
