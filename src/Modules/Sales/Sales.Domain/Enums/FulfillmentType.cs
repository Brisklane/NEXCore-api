namespace Sales.Domain.Enums;

/// <summary>
/// How the order will be fulfilled after payment.
/// Drives the post-payment workflow for all channels.
/// </summary>
public enum FulfillmentType
{
    /// <summary>
    /// Goods handed over at the counter immediately (walk-in POS).
    /// No Delivery document needed — stock deducted at payment.
    /// </summary>
    Immediate = 0,

    /// <summary>
    /// Rider dispatched to deliver to customer address (online / phone order).
    /// Creates a Delivery + RiderAssignment after payment.
    /// </summary>
    Delivery = 1,

    /// <summary>
    /// Customer places order online and picks it up at the store counter.
    /// Creates a Delivery (internal pick + pack), no rider needed.
    /// </summary>
    ClickAndCollect = 2,

    /// <summary>
    /// Goods shipped via courier/freight (B2B / e-commerce).
    /// Creates a Delivery with a tracking number.
    /// </summary>
    Shipped = 3,

    /// <summary>
    /// Lay-away: customer pays a deposit, collects goods later.
    /// Multiple SalesPayments until balance = 0, then Immediate handover.
    /// </summary>
    LayAway = 4,

    /// <summary>
    /// Customer orders from table — dine-in restaurant scenario.
    /// </summary>
    DineIn = 5,

    /// <summary>
    /// Recurring subscription order fulfilled on a schedule.
    /// </summary>
    Subscription = 6
}
