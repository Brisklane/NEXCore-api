using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Inventory Transaction
/// Stock ledger entry - every posting creates transactions
/// This is the source of truth for inventory movements
/// </summary>
public class InventoryTransaction : BaseEntity
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
    /// Bin ID (optional)
    /// </summary>
    public Guid? BinId { get; set; }

    /// <summary>
    /// Variant ID (optional) — variant-level (Color/Size SKU) stock movement.
    /// Null = item-level movement.
    /// </summary>
    public Guid? VariantId { get; set; }

    /// <summary>
    /// Transaction type (IN, OUT, TRANSFER_OUT, TRANSFER_IN, ADJUSTMENT)
    /// </summary>
    public string TransactionType { get; set; } = null!;

    /// <summary>
    /// Quantity (+/-)
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit ID
    /// </summary>
    public Guid UnitId { get; set; }

    /// <summary>
    /// Unit cost
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Total cost (Quantity * UnitCost)
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Document ID (which document created this transaction)
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Document Line ID
    /// </summary>
    public Guid? DocumentLineId { get; set; }

    /// <summary>
    /// Transaction date
    /// </summary>
    public DateTime TransactionDate { get; set; }

    /// <summary>
    /// Reference to cost layer (for FIFO)
    /// </summary>
    public Guid? CostLayerId { get; set; }

    /// <summary>
    /// Batch number (if batch tracked) — denormalized display copy of the linked ItemBatch.
    /// </summary>
    public string? BatchNumber { get; set; }

    /// <summary>
    /// Serial number (if serial tracked) — denormalized display copy of the linked ItemSerial.
    /// </summary>
    public string? SerialNumber { get; set; }

    /// <summary>
    /// Expiry date (if applicable)
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Link to the serialized unit this transaction moved (serial-tracked items; qty is ±1).
    /// </summary>
    public Guid? ItemSerialId { get; set; }

    /// <summary>
    /// Link to the batch/lot this transaction moved (lot-tracked items).
    /// </summary>
    public Guid? ItemBatchId { get; set; }

    // Navigation properties
    public Item? Item { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Bin? Bin { get; set; }
    public Unit? Unit { get; set; }
    public InventoryDocument? Document { get; set; }
    public InventoryDocumentLine? DocumentLine { get; set; }
    public InventoryCostLayer? CostLayer { get; set; }
    public ItemSerial? ItemSerial { get; set; }
    public ItemBatch? ItemBatch { get; set; }
}
