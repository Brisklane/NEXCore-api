using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Inventory Document Line
/// Line items within an inventory document
/// </summary>
public class InventoryDocumentLine : BaseEntity
{
    /// <summary>
    /// Inventory document ID
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Item ID
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Warehouse ID
    /// </summary>
    public Guid WarehouseId { get; set; }

    /// <summary>
    /// Bin ID (optional, for advanced WMS)
    /// </summary>
    public Guid? BinId { get; set; }

    /// <summary>
    /// Variant ID (optional) — tracks stock at variant level (Color/Size SKU).
    /// Null = item-level line (no variant). Must be set for variant-tracked items
    /// so the balance is keyed/updated at variant level.
    /// </summary>
    public Guid? VariantId { get; set; }

    /// <summary>
    /// Quantity
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
    /// Line number
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Reference to source transaction (e.g., PO line)
    /// </summary>
    public Guid? ReferenceLineId { get; set; }

    // ── Lot capture (lot-tracked items) ──────────────────────────────────────

    /// <summary>Batch/lot number for this line (lot-tracked items).</summary>
    public string? BatchNumber { get; set; }

    /// <summary>Manufacture date for the batch (lot-tracked receipts).</summary>
    public DateTime? ManufactureDate { get; set; }

    /// <summary>Expiry date for the batch (lot-tracked receipts).</summary>
    public DateTime? ExpiryDate { get; set; }

    // Navigation properties
    public InventoryDocument? Document { get; set; }
    public Item? Item { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Bin? Bin { get; set; }
    public Unit? Unit { get; set; }
    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();

    /// <summary>Serialized units captured on this line (serial-tracked items).</summary>
    public ICollection<InventoryDocumentLineSerial> LineSerials { get; set; } = new List<InventoryDocumentLineSerial>();
}
