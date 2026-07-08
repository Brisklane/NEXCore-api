using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Inventory Cost Layer
/// Used for FIFO costing method
/// Tracks remaining quantity and cost of each receipt
/// </summary>
public class InventoryCostLayer : BaseEntity
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
    /// Receipt transaction ID (original IN transaction)
    /// </summary>
    public Guid ReceiptTransactionId { get; set; }

    /// <summary>
    /// Receipt date
    /// </summary>
    public DateTime ReceiptDate { get; set; }

    /// <summary>
    /// Original quantity received
    /// </summary>
    public decimal OriginalQuantity { get; set; }

    /// <summary>
    /// Remaining quantity
    /// </summary>
    public decimal RemainingQuantity { get; set; }

    /// <summary>
    /// Unit cost
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Whether this layer is exhausted (remaining qty = 0)
    /// </summary>
    public bool IsExhausted { get; set; }

    // Navigation properties
    public Item? Item { get; set; }
    public Warehouse? Warehouse { get; set; }
    public InventoryTransaction? ReceiptTransaction { get; set; }
}
