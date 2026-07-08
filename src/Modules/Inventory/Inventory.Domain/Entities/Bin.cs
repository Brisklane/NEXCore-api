using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Bin / Location
/// Physical storage location within a warehouse
/// Example: A1-Rack-001
/// </summary>
public class Bin : BaseEntity
{
    /// <summary>
    /// Warehouse ID
    /// </summary>
    public Guid WarehouseId { get; set; }

    /// <summary>
    /// Bin code (location identifier)
    /// </summary>
    public new string Code { get; set; } = null!;

    /// <summary>
    /// Bin name/description
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Aisle
    /// </summary>
    public string? Aisle { get; set; }

    /// <summary>
    /// Rack
    /// </summary>
    public string? Rack { get; set; }

    /// <summary>
    /// Level
    /// </summary>
    public string? Level { get; set; }

    /// <summary>
    /// Position
    /// </summary>
    public string? Position { get; set; }

    /// <summary>
    /// Bin capacity (for reference)
    /// </summary>
    public decimal? Capacity { get; set; }

    // Navigation properties
    public Warehouse? Warehouse { get; set; }
    public ICollection<InventoryBalance> Balances { get; set; } = new List<InventoryBalance>();
    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
}
