using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Per-warehouse on-hand quantity for a specific <see cref="ItemBatch"/>.
/// A single lot can be split across multiple warehouses/bins, so batch stock is tracked here rather
/// than as a single number on the batch. Exactly one row per (Batch, Warehouse, Bin).
/// </summary>
public class ItemLotStock : BaseEntity
{
    /// <summary>The batch/lot this quantity belongs to.</summary>
    public Guid ItemBatchId { get; set; }

    /// <summary>Warehouse holding this portion of the batch.</summary>
    public Guid WarehouseId { get; set; }

    /// <summary>Bin/location within the warehouse (optional).</summary>
    public Guid? BinId { get; set; }

    /// <summary>Quantity of the batch on hand in this warehouse/bin.</summary>
    public decimal Quantity { get; set; }

    // ── Navigation ───────────────────────────────────────────────────────────

    public ItemBatch? Batch { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Bin? Bin { get; set; }
}
