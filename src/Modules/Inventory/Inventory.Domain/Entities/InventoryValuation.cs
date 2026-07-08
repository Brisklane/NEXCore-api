using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Inventory Valuation
/// Accounting valuation of inventory at a point in time
/// </summary>
public class InventoryValuation : BaseEntity
{
    /// <summary>
    /// Item ID
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Warehouse ID
    /// </summary>
    public Guid WarehouseId { get; set; }

    /// <summary>
    /// Valuation date
    /// </summary>
    public DateTime ValuationDate { get; set; }

    /// <summary>
    /// Quantity for this valuation
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit cost
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Total value
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    /// Valuation method used
    /// </summary>
    public string ValuationMethod { get; set; } = "MovingAverage";

    /// <summary>
    /// Period reference
    /// </summary>
    public string? PeriodReference { get; set; }

    // Navigation properties
    public Item? Item { get; set; }
    public Warehouse? Warehouse { get; set; }
}
