using Restaurant.Domain.Enums;

namespace Restaurant.Application.DTOs;

// ── Order ────────────────────────────────────────────────────────────────────

public class RestaurantOrderDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? TokenNumber { get; set; }
    public OrderType OrderType { get; set; }
    public OrderChannel Channel { get; set; }
    public RestaurantOrderStatus Status { get; set; }

    public Guid? TableId { get; set; }
    public string? TableNumber { get; set; }
    public Guid? SectionId { get; set; }
    public string? SectionName { get; set; }
    public int GuestCount { get; set; }

    public Guid? WaiterId { get; set; }
    public string? WaiterName { get; set; }
    public Guid? SessionId { get; set; }

    public Guid? GuestProfileId { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }

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
    public decimal BalanceDue { get; set; }
    public decimal CostAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public DateTime OpenedAt { get; set; }
    public DateTime? FirstFiredAt { get; set; }
    public DateTime? ServedAt { get; set; }
    public DateTime? BilledAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? PromisedAt { get; set; }

    public string? ExternalSource { get; set; }
    public string? ExternalReference { get; set; }
    public bool WasOffline { get; set; }
    public string? Note { get; set; }
    public string? CancelReason { get; set; }

    /// <summary>Minutes the order has been open. Drives the "table is taking too long" flag.</summary>
    public int MinutesOpen { get; set; }

    /// <summary>Lines entered but not yet sent to the kitchen — the fire button lights up on this.</summary>
    public int HeldLineCount { get; set; }
    public int UnservedLineCount { get; set; }

    public List<RestaurantOrderLineDto> Lines { get; set; } = [];
    public List<RestaurantDeliveryDto> Deliveries { get; set; } = [];
    public List<OrderStatusHistoryDto> StatusHistory { get; set; } = [];
}

public class RestaurantOrderLineDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid MenuItemId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid? ComboMealId { get; set; }
    public Guid? ParentLineId { get; set; }

    public string ItemName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public string? ImageUrl { get; set; }

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal ModifierAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public decimal UnitCost { get; set; }
    public Guid? TaxGroupId { get; set; }
    public decimal TaxPercent { get; set; }

    public int? SeatNumber { get; set; }
    public CourseType Course { get; set; }
    public int CourseSequence { get; set; }
    public OrderLineStatus Status { get; set; }
    public bool IsHeld { get; set; }

    public Guid? StationId { get; set; }
    public string? StationName { get; set; }

    public DateTime? FiredAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? ServedAt { get; set; }

    public bool IsVoided { get; set; }
    public Guid? VoidReasonId { get; set; }
    public string? VoidReasonName { get; set; }
    public string? VoidNote { get; set; }
    public bool IsComped { get; set; }

    public string? SpecialInstructions { get; set; }
    public string? AllergenWarning { get; set; }
    public int DisplayOrder { get; set; }

    public List<OrderLineModifierDto> Modifiers { get; set; } = [];
}

public class OrderLineModifierDto
{
    public Guid Id { get; set; }
    public Guid OrderLineId { get; set; }
    public Guid ModifierId { get; set; }
    public Guid ModifierGroupId { get; set; }
    public string ModifierName { get; set; } = string.Empty;
    public string? GroupName { get; set; }
    public decimal Quantity { get; set; }
    public decimal PriceDelta { get; set; }
    public decimal CostDelta { get; set; }
    public bool IsRemoval { get; set; }
}

public class OrderStatusHistoryDto
{
    public Guid Id { get; set; }
    public RestaurantOrderStatus FromStatus { get; set; }
    public RestaurantOrderStatus ToStatus { get; set; }
    public DateTime OccurredAt { get; set; }
    public Guid? StaffId { get; set; }
    public string? StaffName { get; set; }
    public string? Note { get; set; }
}

// ── Commands ─────────────────────────────────────────────────────────────────

public class OpenOrderDto
{
    public Guid OutletId { get; set; }
    public OrderType OrderType { get; set; } = OrderType.DineIn;
    public OrderChannel Channel { get; set; } = OrderChannel.InHouse;
    public Guid? TableId { get; set; }
    public int GuestCount { get; set; } = 1;
    public Guid? WaiterId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid? GuestProfileId { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? Note { get; set; }

    /// <summary>Client-generated; a retry with the same key returns the original order.</summary>
    public string? IdempotencyKey { get; set; }

    public RestaurantDeliveryDto? Delivery { get; set; }

    /// <summary>Optional first round, so opening and ordering can be one round trip.</summary>
    public List<AddOrderLineDto> Lines { get; set; } = [];
}

public class AddOrderLineDto
{
    public Guid MenuItemId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid? ComboMealId { get; set; }
    public decimal Quantity { get; set; } = 1;

    /// <summary>Only honoured for open-price items; otherwise the server prices the line.</summary>
    public decimal? OverridePrice { get; set; }

    public int? SeatNumber { get; set; }
    public CourseType? Course { get; set; }
    public int? CourseSequence { get; set; }

    /// <summary>Withhold from the kitchen until the course is fired.</summary>
    public bool IsHeld { get; set; }

    public string? SpecialInstructions { get; set; }
    public List<SelectedModifierDto> Modifiers { get; set; } = [];

    /// <summary>Chosen options for a combo, one per component slot.</summary>
    public List<ComboSelectionDto> ComboSelections { get; set; } = [];
}

public class SelectedModifierDto
{
    public Guid ModifierId { get; set; }
    public decimal Quantity { get; set; } = 1;
}

public class ComboSelectionDto
{
    public Guid ComboComponentId { get; set; }
    public Guid MenuItemId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal Quantity { get; set; } = 1;
    public List<SelectedModifierDto> Modifiers { get; set; } = [];
}

public class AddLinesDto
{
    public Guid OrderId { get; set; }
    public Guid? WaiterId { get; set; }
    public List<AddOrderLineDto> Lines { get; set; } = [];

    /// <summary>Send everything not held straight to the kitchen. The normal waiter flow.</summary>
    public bool FireImmediately { get; set; } = true;

    public string? IdempotencyKey { get; set; }
}

public class UpdateOrderLineDto
{
    public decimal? Quantity { get; set; }
    public int? SeatNumber { get; set; }
    public CourseType? Course { get; set; }
    public bool? IsHeld { get; set; }
    public string? SpecialInstructions { get; set; }
    public List<SelectedModifierDto>? Modifiers { get; set; }
}

public class VoidLineDto
{
    public Guid OrderLineId { get; set; }
    public Guid? VoidReasonId { get; set; }
    public string? Note { get; set; }
    public Guid? StaffId { get; set; }

    /// <summary>Supervisor PIN, required when the reason is configured to need approval.</summary>
    public string? ApprovalPin { get; set; }
}

public class FireCourseDto
{
    public Guid OrderId { get; set; }

    /// <summary>Null fires everything currently held.</summary>
    public CourseType? Course { get; set; }

    /// <summary>Fire only these lines. Ignored when <see cref="Course"/> is set.</summary>
    public List<Guid> LineIds { get; set; } = [];

    public bool IsPriority { get; set; }
    public Guid? StaffId { get; set; }
}

public class MoveLinesDto
{
    public Guid FromOrderId { get; set; }
    public Guid ToOrderId { get; set; }
    public List<Guid> LineIds { get; set; } = [];
    public int? ToSeatNumber { get; set; }
}

public class UpdateOrderDto
{
    public int? GuestCount { get; set; }
    public Guid? WaiterId { get; set; }
    public Guid? GuestProfileId { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? Note { get; set; }
    public OrderType? OrderType { get; set; }
}

public class CancelOrderDto
{
    public string? Reason { get; set; }
    public Guid? StaffId { get; set; }
    public string? ApprovalPin { get; set; }
}

// ── Delivery ─────────────────────────────────────────────────────────────────

public class RestaurantDeliveryDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid OutletId { get; set; }
    public DeliveryStatus Status { get; set; }
    public string? RecipientName { get; set; }
    public string? Phone { get; set; }
    public string? AddressLine { get; set; }
    public string? Landmark { get; set; }
    public string? City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? ZoneName { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal DistanceKm { get; set; }
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

public class UpdateDeliveryStatusDto
{
    public DeliveryStatus Status { get; set; }
    public Guid? RiderId { get; set; }
    public string? RiderName { get; set; }
    public string? RiderPhone { get; set; }
    public string? FailureReason { get; set; }
}

/// <summary>Compact row for the orders list and the ticket rail on the order screen.</summary>
public class OrderSummaryDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? TokenNumber { get; set; }
    public OrderType OrderType { get; set; }
    public OrderChannel Channel { get; set; }
    public RestaurantOrderStatus Status { get; set; }
    public string? TableNumber { get; set; }
    public string? WaiterName { get; set; }
    public string? CustomerName { get; set; }
    public int GuestCount { get; set; }
    public int LineCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int MinutesOpen { get; set; }
    public int HeldLineCount { get; set; }
    public DeliveryStatus? DeliveryStatus { get; set; }
}
