namespace Manufacturing.Application.DTOs;

/// <summary>
/// Request to produce a BOM-backed item in one express step (e.g. the POS "Produce" action).
///
/// The service resolves the item's active Bill of Materials, creates a Completed production order,
/// backflushes the BOM components from inventory and receives the finished goods — no manual
/// Draft → Released → InProgress → Completed walk-through. Routing is optional: when the item has
/// none (e.g. food), a minimal default routing is auto-created to satisfy the order.
/// </summary>
public class ProduceExpressDto
{
    /// <summary>Finished product to produce (must have an active BOM).</summary>
    public Guid ProductId { get; set; }

    /// <summary>Quantity of the finished product to produce.</summary>
    public decimal Quantity { get; set; } = 1;

    /// <summary>
    /// Warehouse to draw components from / receive finished goods into. Optional — when empty,
    /// Inventory resolves a sensible fallback (stocked warehouse for issues, Main/Retail for receipts).
    /// </summary>
    public Guid? WarehouseId { get; set; }
}
