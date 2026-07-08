namespace Nexcore.SharedKernel.Events;

/// <summary>
/// One product line in a goods-receipt event — tells Inventory how much accepted stock to add,
/// to which warehouse, at what cost. Mirrors <see cref="StockDeductionLine"/> but inbound.
/// </summary>
public class StockReceiptLine
{
    public Guid ProductId { get; init; }
    /// <summary>Variant SKU, when the product is variant-tracked. Null = item-level.</summary>
    public Guid? VariantId { get; init; }
    /// <summary>Receiving warehouse for this line. Null falls back to the event-level warehouse.</summary>
    public Guid? WarehouseId { get; init; }
    /// <summary>Inventory Unit id (from the PO/GRN line). Null falls back to the item's base unit.</summary>
    public Guid? UnitId { get; init; }
    /// <summary>Accepted quantity entering stock (rejected units are excluded).</summary>
    public decimal Quantity { get; init; }
    /// <summary>Unit cost, sourced from the linked purchase-order line, for moving-average costing.</summary>
    public decimal UnitCost { get; init; }
}

/// <summary>
/// Published by Procurement when a Goods Receipt is posted. Inventory.Infrastructure handles this to
/// add the accepted stock — the inbound counterpart of <see cref="PosTransactionCompletedEvent"/>.
///
/// Trigger:  GoodsReceipt.Status → Posted
/// Handler:  Inventory → creates a posted "GRN" InventoryDocument + InventoryTransaction (IN) per line
///           → updates InventoryBalance (QuantityOnHand += accepted qty, moving-average cost)
/// </summary>
public class GoodsReceiptPostedEvent
{
    public Guid GoodsReceiptId { get; init; }
    public string ReceiptNumber { get; init; } = string.Empty;
    /// <summary>Warehouse selected on the receipt; Guid.Empty if none (handler picks a fallback).</summary>
    public Guid WarehouseId { get; init; }
    public Guid CompanyId { get; init; }
    public Guid BranchId { get; init; }
    public Guid BusinessUnitId { get; init; }
    public Guid CreatedByUserId { get; init; }
    public DateTime PostedAt { get; init; } = DateTime.UtcNow;
    public IReadOnlyList<StockReceiptLine> Lines { get; init; } = [];
}
