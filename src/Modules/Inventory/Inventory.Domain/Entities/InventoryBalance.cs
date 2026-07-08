using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Inventory Balance
/// Snapshot of current inventory levels
/// Used for fast reads (POS, Reports)
/// Updated after each posting
/// </summary>
public class InventoryBalance : BaseEntity
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
    /// Bin ID (optional, for precise location tracking)
    /// </summary>
    public Guid? BinId { get; set; }

    /// <summary>
    /// Variant ID — tracks stock at variant level (Color/Size SKU).
    /// Null = item-level balance (no variants)
    /// </summary>
    public Guid? VariantId { get; set; }

    /// <summary>
    /// Quantity on hand
    /// </summary>
    public decimal QuantityOnHand { get; set; }

    /// <summary>
    /// Quantity reserved (in open orders)
    /// </summary>
    public decimal QuantityReserved { get; set; }

    /// <summary>
    /// Quantity available (OnHand - Reserved)
    /// </summary>
    public decimal QuantityAvailable { get; set; }

    /// <summary>
    /// Average unit cost
    /// </summary>
    public decimal AverageCost { get; set; }

    /// <summary>
    /// Total value (QuantityOnHand * AverageCost)
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    /// Last transaction date
    /// </summary>
    public DateTime? LastTransactionDate { get; set; }

    /// <summary>
    /// Costing method used for this balance
    /// </summary>
    public string CostingMethod { get; set; } = "MovingAverage";

    // Navigation properties
    public Item? Item { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Bin? Bin { get; set; }
    public ItemVariant? Variant { get; set; }
}
