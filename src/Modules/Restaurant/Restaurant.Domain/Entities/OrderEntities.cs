using Nexcore.SharedKernel;
using Restaurant.Domain.Enums;

namespace Restaurant.Domain.Entities;

/// <summary>
/// What a table (or a takeaway customer) has asked for. Long-lived: opened when guests sit,
/// added to over an hour, and only then billed. This is the aggregate root of service.
///
/// Money on the order is the running total of its lines. The <see cref="RestaurantCheck"/>
/// records what was actually collected, and one order can produce several checks — which is why
/// the two are not the same row.
/// </summary>
public class RestaurantOrder : BaseEntity
{
    public Guid OutletId { get; set; }

    /// <summary>Human-facing number from the document-sequence engine.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Short queue token for counter service ("A-42"). Null for dine-in.</summary>
    public string? TokenNumber { get; set; }

    public OrderType OrderType { get; set; } = OrderType.DineIn;
    public OrderChannel Channel { get; set; } = OrderChannel.InHouse;
    public RestaurantOrderStatus Status { get; set; } = RestaurantOrderStatus.Draft;

    // ── Dine-in context ──────────────────────────────────────────────────────
    public Guid? TableId { get; set; }
    public DiningTable? Table { get; set; }
    public string? TableNumber { get; set; }
    public Guid? SectionId { get; set; }
    public int GuestCount { get; set; } = 1;

    public Guid? WaiterId { get; set; }
    public string? WaiterName { get; set; }
    public Guid? SessionId { get; set; }

    // ── Guest ────────────────────────────────────────────────────────────────
    public Guid? GuestProfileId { get; set; }
    /// <summary>CRM contact, when the guest is a known customer of the company.</summary>
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }

    // ── Totals ───────────────────────────────────────────────────────────────
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ServiceChargeAmount { get; set; }
    public decimal PackagingChargeAmount { get; set; }
    public decimal DeliveryFeeAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TipAmount { get; set; }
    public decimal RoundingAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>Sum of line standard costs — food cost for this order, without a report join.</summary>
    public decimal CostAmount { get; set; }

    // ── Timeline ─────────────────────────────────────────────────────────────
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FirstFiredAt { get; set; }
    public DateTime? ServedAt { get; set; }
    public DateTime? BilledAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    /// <summary>Promised time for takeaway/delivery, quoted from item prep times.</summary>
    public DateTime? PromisedAt { get; set; }

    // ── Provenance ───────────────────────────────────────────────────────────
    /// <summary>Marketplace name when the order arrived from an aggregator.</summary>
    public string? ExternalSource { get; set; }
    public string? ExternalReference { get; set; }

    /// <summary>
    /// Client-generated key. A double-tap on a flaky connection must not fire two orders, so
    /// submission is idempotent on this value.
    /// </summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>Order was taken while the till was offline and reconciled afterwards.</summary>
    public bool WasOffline { get; set; }

    public string? Note { get; set; }
    public string? CancelReason { get; set; }

    public ICollection<RestaurantOrderLine> Lines { get; set; } = [];
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = [];
    public ICollection<KitchenTicket> Tickets { get; set; } = [];
    public ICollection<RestaurantCheck> Checks { get; set; } = [];
}

/// <summary>One dish on an order, attributed to a seat and a course.</summary>
public class RestaurantOrderLine : BaseEntity
{
    public Guid OrderId { get; set; }
    public RestaurantOrder? Order { get; set; }

    public Guid MenuItemId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid? ComboMealId { get; set; }

    /// <summary>Set on the components of a combo, pointing at the combo's own line.</summary>
    public Guid? ParentLineId { get; set; }

    public string ItemName { get; set; } = string.Empty;
    public string? VariantName { get; set; }

    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }

    /// <summary>Sum of the chosen modifiers' price deltas, per unit.</summary>
    public decimal ModifierAmount { get; set; }

    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public decimal UnitCost { get; set; }

    public Guid? TaxGroupId { get; set; }

    /// <summary>Rate this line was charged at, frozen at order time so a later rate change cannot rewrite history.</summary>
    public decimal TaxPercent { get; set; }

    // ── Service context ──────────────────────────────────────────────────────
    /// <summary>Which guest at the table ordered it. What makes a seat split exact.</summary>
    public int? SeatNumber { get; set; }
    public CourseType Course { get; set; } = CourseType.Main;

    /// <summary>Position of the course in the meal — courses fire in this order.</summary>
    public int CourseSequence { get; set; } = 1;

    public OrderLineStatus Status { get; set; } = OrderLineStatus.New;

    /// <summary>Entered but withheld from the kitchen until the waiter fires the course.</summary>
    public bool IsHeld { get; set; }

    public Guid? StationId { get; set; }
    public Guid? KitchenTicketLineId { get; set; }

    public DateTime? FiredAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? ServedAt { get; set; }

    // ── Adjustments ──────────────────────────────────────────────────────────
    public bool IsVoided { get; set; }
    public Guid? VoidReasonId { get; set; }
    public string? VoidNote { get; set; }
    public Guid? VoidedByStaffId { get; set; }
    public DateTime? VoidedAt { get; set; }

    /// <summary>
    /// True when the line was voided after the kitchen had already fired it. That food was
    /// cooked and thrown away, so it is a wastage event, not just a correction.
    /// </summary>
    public bool WasFiredWhenVoided { get; set; }

    public bool IsComped { get; set; }
    public Guid? DiscountReasonId { get; set; }

    public string? SpecialInstructions { get; set; }
    public int DisplayOrder { get; set; }

    public ICollection<RestaurantOrderLineModifier> Modifiers { get; set; } = [];
}

/// <summary>A modifier the guest actually chose, frozen with the price it was charged at.</summary>
public class RestaurantOrderLineModifier : BaseEntity
{
    public Guid OrderLineId { get; set; }
    public RestaurantOrderLine? OrderLine { get; set; }

    public Guid ModifierId { get; set; }
    public Guid ModifierGroupId { get; set; }

    public string ModifierName { get; set; } = string.Empty;
    public string? GroupName { get; set; }

    public decimal Quantity { get; set; } = 1;
    public decimal PriceDelta { get; set; }
    public decimal CostDelta { get; set; }

    /// <summary>"No onions" — the kitchen must see it, and it credits an ingredient back.</summary>
    public bool IsRemoval { get; set; }

    public Guid? InventoryItemId { get; set; }
    public decimal ConsumptionQuantity { get; set; }
    public string? ConsumptionUom { get; set; }
}

/// <summary>Every status transition of an order, for the audit trail and the service report.</summary>
public class OrderStatusHistory : BaseEntity
{
    public Guid OrderId { get; set; }
    public RestaurantOrder? Order { get; set; }

    public RestaurantOrderStatus FromStatus { get; set; }
    public RestaurantOrderStatus ToStatus { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Guid? StaffId { get; set; }
    public string? StaffName { get; set; }
    public string? Note { get; set; }
}

/// <summary>Dispatch detail for a delivery order: where it goes, who took it, and when.</summary>
public class RestaurantDelivery : BaseEntity
{
    public Guid OrderId { get; set; }
    public RestaurantOrder? Order { get; set; }

    public Guid OutletId { get; set; }

    public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;

    public string? RecipientName { get; set; }
    public string? Phone { get; set; }
    public string? AddressLine { get; set; }
    public string? Landmark { get; set; }
    public string? City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>Delivery zone this address falls in; sets the fee and the expected duration.</summary>
    public string? ZoneName { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal DistanceKm { get; set; }

    /// <summary>Sales-module rider, when the company already runs a rider pool.</summary>
    public Guid? RiderId { get; set; }
    public string? RiderName { get; set; }
    public string? RiderPhone { get; set; }

    public DateTime? AssignedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? EstimatedArrivalAt { get; set; }

    public string? FailureReason { get; set; }
    public string? DeliveryNote { get; set; }
}
