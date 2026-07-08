using Inv = Inventory.Domain.Constants;
using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// A batch / lot of an <see cref="Item"/> (TrackingType = Lot).
/// Many physical units share one batch code plus manufacture/expiry dates and a supplier — the norm
/// for pharma, food, cosmetics and chemicals. Enables expiry control and FEFO (first-expiry-first-out)
/// picking. Per-warehouse quantities are held in <see cref="ItemLotStock"/>; this record is the batch
/// master (identity, dates, supplier, cost, aggregate remaining).
/// </summary>
public class ItemBatch : BaseEntity
{
    /// <summary>Owning item (product) ID.</summary>
    public Guid ItemId { get; set; }

    /// <summary>Variant ID (Color/Size SKU) when applicable. Null = item-level.</summary>
    public Guid? VariantId { get; set; }

    /// <summary>Batch / lot number. Unique per item within a tenant.</summary>
    public string BatchNumber { get; set; } = null!;

    public DateTime? ManufactureDate { get; set; }
    public DateTime? ExpiryDate { get; set; }

    /// <summary>Supplier the batch was purchased from (optional).</summary>
    public Guid? SupplierId { get; set; }

    // ── Quantities (aggregate across warehouses; per-warehouse detail in ItemLotStock) ──

    /// <summary>Total quantity originally received into this batch.</summary>
    public decimal ReceivedQuantity { get; set; }

    /// <summary>Quantity still on hand (not yet issued/consumed).</summary>
    public decimal RemainingQuantity { get; set; }

    /// <summary>Quantity soft-reserved against open orders.</summary>
    public decimal ReservedQuantity { get; set; }

    /// <summary>Acquisition unit cost for the batch.</summary>
    public decimal UnitCost { get; set; }

    /// <summary>Batch status — see <see cref="Inv.BatchStatus"/>.</summary>
    public string Status { get; set; } = Inv.BatchStatus.Active;

    // ── Navigation ───────────────────────────────────────────────────────────

    public Item? Item { get; set; }
    public ItemVariant? Variant { get; set; }
    public ICollection<ItemLotStock> LotStocks { get; set; } = new List<ItemLotStock>();
}
