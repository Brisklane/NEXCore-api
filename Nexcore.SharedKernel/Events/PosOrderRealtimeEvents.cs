namespace Nexcore.SharedKernel.Events;

// ── POS real-time order notifications ─────────────────────────────────────────
// These events drive the store's POS screen: when an online order arrives the POS
// app plays an animation and the "Orders" button shows the pending count. They are
// published in-process by the Sales module and handled by a SignalR-aware handler
// that pushes to the store's connected POS clients.

/// <summary>
/// Published when a customer-placed online order (app / web store / marketplace)
/// becomes visible to a store — i.e. it enters <c>Placed</c> status.
/// Triggers the "new order" animation on that store's POS screen and refreshes the badge.
/// </summary>
public class OnlineOrderPlacedEvent
{
    public Guid OrderId          { get; init; }
    public string OrderNumber    { get; init; } = string.Empty;

    /// <summary>Store the order is fulfilled from — the SignalR group key (SalesOrder.OriginBranchId).</summary>
    public Guid StoreId          { get; init; }

    public Guid? ContactId       { get; init; }
    public string? ContactName   { get; init; }

    /// <summary>Total distinct line count — used for a quick "N items" label on the toast.</summary>
    public int ItemCount         { get; init; }
    public decimal TotalAmount   { get; init; }
    public string CurrencyCode   { get; init; } = "USD";

    /// <summary>0 = OnlineStore, 9 = OnlineApp, 7 = Marketplace (SalesChannel).</summary>
    public int SalesChannel      { get; init; }
    /// <summary>0 Immediate, 1 Delivery, 2 ClickAndCollect, ... (FulfillmentType).</summary>
    public int FulfillmentType   { get; init; }

    public DateTime PlacedAt     { get; init; } = DateTime.UtcNow;

    // ── Tenant context (lets the handler scope its count query) ──
    public Guid CompanyId        { get; init; }
    public Guid BranchId         { get; init; }
    public Guid BusinessUnitId   { get; init; }
}

/// <summary>
/// Published whenever the set of pending (unacknowledged) online orders for a store
/// likely changed without a brand-new arrival — e.g. the store accepted, rejected or
/// cancelled an online order. Tells the POS screen to re-render the badge count
/// (no arrival animation).
/// </summary>
public class OnlineOrderQueueChangedEvent
{
    /// <summary>Store whose pending count changed (SalesOrder.OriginBranchId).</summary>
    public Guid StoreId          { get; init; }

    public Guid CompanyId        { get; init; }
    public Guid BranchId         { get; init; }
    public Guid BusinessUnitId   { get; init; }
}
