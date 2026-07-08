using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Warehouse
/// Physical or logical location for inventory storage
/// </summary>
public class Warehouse : BaseEntity
{
    /// <summary>
    /// Warehouse code
    /// </summary>
    public new string Code { get; set; } = null!;

    /// <summary>
    /// Warehouse name
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Warehouse address
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// City
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Region/Province
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Postal code
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Country
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Warehouse type (Main, Transit, Retail, etc.)
    /// </summary>
    public string WarehouseType { get; set; } = "Main";

    // Navigation properties
    public ICollection<Bin> Bins { get; set; } = new List<Bin>();
    public ICollection<InventoryBalance> Balances { get; set; } = new List<InventoryBalance>();
    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
}
