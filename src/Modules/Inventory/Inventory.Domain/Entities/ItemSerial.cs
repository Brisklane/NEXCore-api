using Inv = Inventory.Domain.Constants;
using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// A single physical serialized unit of an <see cref="Item"/> (TrackingType = Serial).
/// This is the first-class identity for one piece of stock — e.g. one Samsung A24 handset —
/// carrying its own serial/IMEI, lifecycle status, current location, cost, and warranty.
/// The quantity ledger (InventoryTransaction / InventoryBalance) still owns the numeric on-hand;
/// this record owns the <b>identity and lifecycle</b> of the individual unit.
/// </summary>
public class ItemSerial : BaseEntity
{
    /// <summary>Owning item (product) ID.</summary>
    public Guid ItemId { get; set; }

    /// <summary>Variant ID (Color/Size SKU) when the item has variants. Null = item-level.</summary>
    public Guid? VariantId { get; set; }

    // ── Identity ─────────────────────────────────────────────────────────────

    /// <summary>Primary unique unit identifier (serial number). Unique per item within a tenant.</summary>
    public string SerialNumber { get; set; } = null!;

    /// <summary>Primary IMEI (cellular devices). Optional secondary identifier.</summary>
    public string? Imei { get; set; }

    /// <summary>Secondary IMEI (dual-SIM devices). Optional.</summary>
    public string? Imei2 { get; set; }

    /// <summary>MAC address (networked devices). Optional.</summary>
    public string? MacAddress { get; set; }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    /// <summary>Current lifecycle status — see <see cref="Inv.SerialStatus"/>.</summary>
    public string Status { get; set; } = Inv.SerialStatus.InStock;

    /// <summary>Current warehouse holding the unit (null once sold/scrapped/lost).</summary>
    public Guid? WarehouseId { get; set; }

    /// <summary>Current bin/location within the warehouse (optional, for WMS).</summary>
    public Guid? BinId { get; set; }

    /// <summary>Acquisition unit cost captured at receipt (for margin/valuation per unit).</summary>
    public decimal UnitCost { get; set; }

    // ── Receipt provenance ───────────────────────────────────────────────────

    /// <summary>Document (GRN) that brought this unit into stock.</summary>
    public Guid? ReceiptDocumentId { get; set; }

    /// <summary>Ledger transaction that recorded the receipt of this unit.</summary>
    public Guid? ReceiptTransactionId { get; set; }

    /// <summary>Date the unit was received.</summary>
    public DateTime? ReceiptDate { get; set; }

    /// <summary>Supplier the unit was purchased from (optional).</summary>
    public Guid? SupplierId { get; set; }

    // ── Warranty ─────────────────────────────────────────────────────────────

    public DateTime? WarrantyStartDate { get; set; }
    public DateTime? WarrantyEndDate { get; set; }

    // ── Sale / disposal ──────────────────────────────────────────────────────

    /// <summary>Document (Issue/Delivery) that sold/issued the unit.</summary>
    public Guid? SoldDocumentId { get; set; }

    /// <summary>Date the unit was sold/issued.</summary>
    public DateTime? SoldDate { get; set; }

    /// <summary>Free-text sales/customer reference (order no., customer name).</summary>
    public string? SalesReference { get; set; }

    /// <summary>Internal notes for this unit.</summary>
    public string? Notes { get; set; }

    // ── Navigation ───────────────────────────────────────────────────────────

    public Item? Item { get; set; }
    public ItemVariant? Variant { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Bin? Bin { get; set; }
    public ICollection<ItemSerialHistory> History { get; set; } = new List<ItemSerialHistory>();
}
