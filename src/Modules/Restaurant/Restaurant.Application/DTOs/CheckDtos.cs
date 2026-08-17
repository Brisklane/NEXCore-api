using Restaurant.Domain.Enums;

namespace Restaurant.Application.DTOs;

public class RestaurantCheckDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public Guid OutletId { get; set; }
    public string CheckNumber { get; set; } = string.Empty;
    public CheckStatus Status { get; set; }
    public SplitMethod SplitMethod { get; set; }
    public int SplitIndex { get; set; }
    public int SplitCount { get; set; }
    public string? SeatNumbers { get; set; }
    public string? TableNumber { get; set; }

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
    public decimal ChangeAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public Guid? SessionId { get; set; }
    public Guid? CashierId { get; set; }
    public string? CashierName { get; set; }
    public Guid? WaiterId { get; set; }
    public string? WaiterName { get; set; }

    public DateTime? PrintedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public int PrintCount { get; set; }
    public Guid? SalesInvoiceId { get; set; }
    public string? FiscalReference { get; set; }

    public bool IsVoided { get; set; }
    public string? VoidNote { get; set; }
    public DateTime? VoidedAt { get; set; }

    public List<CheckLineDto> Lines { get; set; } = [];
    public List<CheckPaymentDto> Payments { get; set; } = [];
    public List<CheckDiscountDto> Discounts { get; set; } = [];
}

public class CheckLineDto
{
    public Guid Id { get; set; }
    public Guid CheckId { get; set; }
    public Guid OrderLineId { get; set; }
    public Guid MenuItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public string? ModifierSummary { get; set; }
    public int? SeatNumber { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public decimal UnitCost { get; set; }
    public int DisplayOrder { get; set; }
}

public class CheckPaymentDto
{
    public Guid Id { get; set; }
    public Guid CheckId { get; set; }
    public TenderType TenderType { get; set; }
    public decimal Amount { get; set; }
    public decimal TenderedAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public decimal TipAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;
    public string? Reference { get; set; }
    public string? CardLast4 { get; set; }
    public string? CardScheme { get; set; }
    public string? AuthCode { get; set; }
    public Guid? GiftCardId { get; set; }
    public int? LoyaltyPointsUsed { get; set; }
    public DateTime PaidAt { get; set; }
    public Guid? StaffId { get; set; }
    public bool IsRefund { get; set; }
    public string? RefundReason { get; set; }
}

public class CheckDiscountDto
{
    public Guid Id { get; set; }
    public Guid CheckId { get; set; }
    public Guid? OrderLineId { get; set; }
    public Guid? DiscountReasonId { get; set; }
    public string? ReasonName { get; set; }
    public DiscountKind Kind { get; set; }
    public decimal Value { get; set; }
    public decimal Amount { get; set; }
    public string? PromoCode { get; set; }
    public Guid? AppliedByStaffId { get; set; }
    public Guid? ApprovedByStaffId { get; set; }
    public DateTime AppliedAt { get; set; }
}

// ── Commands ─────────────────────────────────────────────────────────────────

/// <summary>
/// Turn an order into one or more checks. Splitting is a request to carve the order up, and the
/// server does the carving — a client that computed the split itself could produce checks that
/// do not add up to the order, which is the one thing that must never happen.
/// </summary>
public class CreateChecksDto
{
    public Guid OrderId { get; set; }
    public SplitMethod SplitMethod { get; set; } = SplitMethod.None;

    /// <summary>Number of ways for <see cref="SplitMethod.Evenly"/>.</summary>
    public int SplitCount { get; set; } = 1;

    /// <summary>One entry per check for by-seat, by-item, by-amount and by-percentage splits.</summary>
    public List<CheckSplitPartDto> Parts { get; set; } = [];

    public Guid? CashierId { get; set; }
    public Guid? SessionId { get; set; }

    /// <summary>Replace any existing unpaid checks. Refused once anything has been paid.</summary>
    public bool ReplaceExisting { get; set; } = true;
}

public class CheckSplitPartDto
{
    /// <summary>Seats this check covers, for a by-seat split.</summary>
    public List<int> SeatNumbers { get; set; } = [];

    /// <summary>Order lines assigned to this check, for a by-item split.</summary>
    public List<Guid> OrderLineIds { get; set; } = [];

    /// <summary>Fixed amount for a by-amount split.</summary>
    public decimal? Amount { get; set; }

    /// <summary>Share for a by-percentage split; the parts must total 100.</summary>
    public decimal? Percentage { get; set; }

    public string? Label { get; set; }
}

public class TakePaymentDto
{
    public Guid CheckId { get; set; }
    public TenderType TenderType { get; set; } = TenderType.Cash;
    public decimal Amount { get; set; }
    public decimal TenderedAmount { get; set; }
    public decimal TipAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;
    public string? Reference { get; set; }
    public string? CardLast4 { get; set; }
    public string? CardScheme { get; set; }
    public string? AuthCode { get; set; }
    public Guid? GiftCardId { get; set; }
    public int? LoyaltyPointsUsed { get; set; }
    public Guid? StaffId { get; set; }
    public Guid? SessionId { get; set; }

    /// <summary>A retry with the same key must not take the money twice.</summary>
    public string? IdempotencyKey { get; set; }
}

public class ApplyDiscountDto
{
    public Guid CheckId { get; set; }
    public Guid? OrderLineId { get; set; }
    public Guid? DiscountReasonId { get; set; }
    public DiscountKind Kind { get; set; } = DiscountKind.Percentage;
    public decimal Value { get; set; }
    public string? PromoCode { get; set; }
    public Guid? StaffId { get; set; }

    /// <summary>Supervisor PIN, required above the configured approval threshold.</summary>
    public string? ApprovalPin { get; set; }
}

public class AddTipDto
{
    public Guid CheckId { get; set; }
    public decimal Amount { get; set; }
    public TenderType TenderType { get; set; } = TenderType.Card;
    public Guid? WaiterId { get; set; }
    public bool IsDeclared { get; set; }
}

public class VoidCheckDto
{
    public Guid CheckId { get; set; }
    public Guid? VoidReasonId { get; set; }
    public string? Note { get; set; }
    public Guid? StaffId { get; set; }
    public string? ApprovalPin { get; set; }
}

public class RefundPaymentDto
{
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public Guid? StaffId { get; set; }
    public string? ApprovalPin { get; set; }
}

public class WaiveServiceChargeDto
{
    public Guid CheckId { get; set; }
    public string? Reason { get; set; }
    public Guid? StaffId { get; set; }
    public string? ApprovalPin { get; set; }
}

// ── Reference data ───────────────────────────────────────────────────────────

public class VoidReasonDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool RequiresApproval { get; set; }
    public bool CountsAsWastage { get; set; }
    public bool IsActive { get; set; }
}

public class DiscountReasonDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool RequiresApproval { get; set; }
    public decimal MaxAmountWithoutApproval { get; set; }
    public bool IsComp { get; set; }
    public bool IsActive { get; set; }
}

public class ServiceChargeRuleDto
{
    public Guid Id { get; set; }
    public Guid? OutletId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ServiceChargeBasis Basis { get; set; }
    public decimal Value { get; set; }
    public int MinPartySize { get; set; }
    public string? ApplicableOrderTypes { get; set; }
    public bool IsTaxable { get; set; }
    public Guid? TaxGroupId { get; set; }
    public bool IsWaivable { get; set; }
    public bool RequiresApprovalToWaive { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
}

// ── Tips ─────────────────────────────────────────────────────────────────────

public class TipRecordDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public Guid? CheckId { get; set; }
    public string? CheckNumber { get; set; }
    public Guid? WaiterId { get; set; }
    public string? WaiterName { get; set; }
    public decimal Amount { get; set; }
    public TenderType TenderType { get; set; }
    public bool IsDeclared { get; set; }
    public DateTime ReceivedAt { get; set; }
    public Guid? TipPoolId { get; set; }
}

public class TipPoolDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public TipDistributionBasis Basis { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DistributedAmount { get; set; }
    public decimal KitchenSharePercent { get; set; }
    public bool IsFinalised { get; set; }
    public DateTime? FinalisedAt { get; set; }
    public List<TipDistributionDto> Distributions { get; set; } = [];
}

public class TipDistributionDto
{
    public Guid Id { get; set; }
    public Guid TipPoolId { get; set; }
    public Guid StaffId { get; set; }
    public string? StaffName { get; set; }
    public StaffRole Role { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal SalesAmount { get; set; }
    public decimal SharePercent { get; set; }
    public decimal Amount { get; set; }
    public bool IsPaidOut { get; set; }
    public DateTime? PaidOutAt { get; set; }
}

public class CreateTipPoolDto
{
    public Guid OutletId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public TipDistributionBasis Basis { get; set; } = TipDistributionBasis.ByHoursWorked;
    public decimal KitchenSharePercent { get; set; }
}
