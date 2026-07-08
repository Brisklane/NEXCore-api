namespace Sales.Domain.Enums;

/// <summary>
/// Sales order lifecycle status.
///
/// VISIBILITY RULES — enforced at repository/query layer:
///   Customer sees:  own orders in any status
///   Store sees:     OriginPosStoreId = their store AND Status >= Placed
///   POS cashier:    OriginPosTerminalId = their terminal AND Status = Draft/PosParked
/// </summary>
public enum SalesOrderStatus
{
    // ?? INTERNAL / NOT VISIBLE TO STORE ??????????????????????????????????????

    /// <summary>
    /// Customer is building the basket — items being added/removed.
    /// POS cashier is scanning items.
    /// NEVER shown to store staff or in order queues.
    /// Transitions to: Placed (app) | PosParked | PaidAndClosed (POS immediate)
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Walk-in basket saved at POS — customer stepped away.
    /// Only visible to the cashier who parked it, on the same terminal/store.
    /// NEVER shown in store's incoming order queue.
    /// </summary>
    PosParked = 9,

    /// <summary>
    /// Online payment initiated but gateway not yet confirmed.
    /// Invisible to store until PaymentConfirmed.
    /// </summary>
    PaymentPending = 17,

    // ?? VISIBLE TO STORE FROM THIS POINT FORWARD ?????????????????????????????

    /// <summary>
    /// Customer tapped "Place Order" — payment confirmed or COD selected.
    /// THIS is when the store sees the order in its incoming queue.
    /// Store must Accept or Reject within the configured SLA window.
    /// </summary>
    Placed = 18,

    /// <summary>Submitted for internal approval (B2B / high-value orders).</summary>
    PendingApproval = 1,

    /// <summary>Store accepted the order — now preparing it.</summary>
    Confirmed = 2,

    /// <summary>Store has acknowledged and is actively preparing the order.</summary>
    Preparing = 12,

    /// <summary>Order is packed and ready for rider pickup or customer collection.</summary>
    ReadyForPickup = 13,

    /// <summary>Rider has picked up the order and is en route to customer.</summary>
    OutForDelivery = 14,

    /// <summary>Order delivered to customer successfully.</summary>
    Delivered = 15,

    /// <summary>Delivery failed — rider could not complete. Store action required.</summary>
    DeliveryFailed = 16,

    // ?? POST-FULFILLMENT ??????????????????????????????????????????????????????

    /// <summary>Partially shipped (B2B multi-delivery).</summary>
    PartiallyDelivered = 3,

    /// <summary>All lines fully shipped.</summary>
    FullyDelivered = 4,

    /// <summary>Invoice raised (B2B).</summary>
    Invoiced = 5,

    /// <summary>Payment received — final state.</summary>
    Closed = 6,

    /// <summary>Customer paid deposit at POS — remaining balance due (lay-away).</summary>
    PartiallyPaid = 10,

    /// <summary>Full payment received at POS — goods handed over immediately.</summary>
    PaidAndClosed = 11,

    // ?? TERMINAL STATES ???????????????????????????????????????????????????????

    /// <summary>Order cancelled — by customer, store, or system.</summary>
    Cancelled = 7,

    /// <summary>On hold due to credit/compliance issues.</summary>
    OnHold = 8,

    /// <summary>Store rejected the order (e.g., item unavailable, store closing).</summary>
    Rejected = 19
}
