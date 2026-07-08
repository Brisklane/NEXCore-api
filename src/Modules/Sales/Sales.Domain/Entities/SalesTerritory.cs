using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// Sales Territory for territory management and rep assignment.
/// Aligned with Dynamics 365 Sales Territory and SAP Sales Area.
/// </summary>
public class SalesTerritory : BaseEntity
{
    public new string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Region { get; set; }
    public string? Country { get; set; }

    /// <summary>Territory manager / owner user ID.</summary>
    public Guid? ManagerId { get; set; }

    public Guid? ParentTerritoryId { get; set; }
    public SalesTerritory? ParentTerritory { get; set; }

    public ICollection<SalesTerritory> SubTerritories { get; set; } = new List<SalesTerritory>();
    // Contacts are owned by CRM - Sales references them by ContactId (cross-module Guid)
    public ICollection<SalesRepTerritory> SalesReps { get; set; } = new List<SalesRepTerritory>();
}
