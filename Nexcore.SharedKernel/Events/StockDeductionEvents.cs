namespace Nexcore.SharedKernel.Events;

// ?? Shared payload ????????????????????????????????????????????????????????????

/// <summary>
/// One product line in a stock deduction event.
/// Tells Inventory exactly what quantity to remove from which warehouse/bin.
/// </summary>
public class StockDeductionLine
{
    public Guid ProductId { get; init; }
    public string ProductCode { get; init; } = string.Empty;
    /// <summary>Variant (Color/Size SKU) being sold, when the product is variant-tracked. Null = item-level.</summary>
    public Guid? VariantId { get; init; }
    public Guid? WarehouseId { get; init; }
    public Guid? BinId { get; init; }
    public decimal Quantity { get; init; }
    public string UnitOfMeasure { get; init; } = string.Empty;
    public decimal UnitCost { get; init; }
}

// ?? POS trigger ???????????????????????????????????????????????????????????????

/// <summary>
/// Published by Sales when a POS Transaction is completed (immediate payment).
/// Inventory.Infrastructure handles this to deduct stock instantly.
///
/// Trigger:  PosTransaction.Status ? PaidAndClosed
/// Handler:  Inventory ? creates InventoryTransaction (Issue) per line
///           ? updates InventoryBalance (QuantityOnHand -= qty)
/// </summary>
public class PosTransactionCompletedEvent
{
    public Guid PosTransactionId { get; init; }
    public Guid PosStoreId { get; init; }
    public Guid WarehouseId { get; init; }
    public Guid CompanyId { get; init; }
    public Guid BranchId { get; init; }
    public Guid BusinessUnitId { get; init; }
    public Guid CreatedByUserId { get; init; }
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
    public IReadOnlyList<StockDeductionLine> Lines { get; init; } = [];
}

// ?? Delivery trigger ??????????????????????????????????????????????????????????

/// <summary>
/// Published by Sales when a Delivery document is posted (status ? Shipped).
/// Inventory.Infrastructure handles this to deduct stock on shipment.
///
/// Trigger:  Delivery.Status ? Shipped
/// Handler:  Inventory ? creates InventoryTransaction (Issue) per line
///           ? updates InventoryBalance (QuantityOnHand -= qty)
/// </summary>
public class DeliveryPostedEvent
{
    public Guid DeliveryId { get; init; }
    public Guid SalesOrderId { get; init; }
    public Guid WarehouseId { get; init; }
    public Guid CompanyId { get; init; }
    public Guid BranchId { get; init; }
    public Guid BusinessUnitId { get; init; }
    public Guid CreatedByUserId { get; init; }
    public DateTime PostedAt { get; init; } = DateTime.UtcNow;
    public IReadOnlyList<StockDeductionLine> Lines { get; init; } = [];
}

// ── Reservation (soft allocation) ───────────────────────────────────────────────

/// <summary>One product line in a stock reservation change.</summary>
public class StockReservationLine
{
    public Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public Guid? WarehouseId { get; init; }
    public Guid? BinId { get; init; }
    /// <summary>Signed: positive = reserve more, negative = release.</summary>
    public decimal Quantity { get; init; }
}

/// <summary>
/// Published by Sales when an order is committed (Confirmed → reserve) or released
/// (Cancelled/Rejected → release). Inventory adjusts InventoryBalance.QuantityReserved
/// (and recomputes QuantityAvailable = OnHand − Reserved) WITHOUT moving physical on-hand.
///
/// Soft allocation only — no InventoryTransaction / ledger movement and no GL impact.
/// Physical on-hand moves later, at shipment (DeliveryPostedEvent) or POS sale, which also
/// releases the matching reservation.
/// </summary>
public class StockReservationChangedEvent
{
    public Guid ReferenceId { get; init; }
    public string ReferenceType { get; init; } = "SalesOrder";
    public Guid WarehouseId { get; init; }
    public Guid CompanyId { get; init; }
    public Guid BranchId { get; init; }
    public Guid BusinessUnitId { get; init; }
    public Guid CreatedByUserId { get; init; }
    public IReadOnlyList<StockReservationLine> Lines { get; init; } = [];
}
