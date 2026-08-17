using Nexcore.SharedKernel;
using Restaurant.Domain.Enums;

namespace Restaurant.Domain.Entities;

/// <summary>
/// A bill presented to a payer. One order can produce many checks — a table of six that pays
/// separately produces six — which is why splitting is modelled as several checks over one
/// order rather than as a property of the order.
/// </summary>
public class RestaurantCheck : BaseEntity
{
    public Guid OrderId { get; set; }
    public RestaurantOrder? Order { get; set; }

    public Guid OutletId { get; set; }

    public string CheckNumber { get; set; } = string.Empty;
    public CheckStatus Status { get; set; } = CheckStatus.Open;

    /// <summary>How this check was carved out of the order.</summary>
    public SplitMethod SplitMethod { get; set; } = SplitMethod.None;

    /// <summary>1-based position within the split ("check 2 of 4").</summary>
    public int SplitIndex { get; set; } = 1;
    public int SplitCount { get; set; } = 1;

    /// <summary>Seats this check covers, comma-separated, when split by seat.</summary>
    public string? SeatNumbers { get; set; }

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
    public decimal ChangeAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public Guid? SessionId { get; set; }
    public Guid? CashierId { get; set; }
    public string? CashierName { get; set; }
    public Guid? WaiterId { get; set; }

    public DateTime? PrintedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public int PrintCount { get; set; }

    /// <summary>Sales invoice raised for this check, when the guest asked for a tax invoice.</summary>
    public Guid? SalesInvoiceId { get; set; }

    /// <summary>Fiscal payload (QR/e-invoice) for jurisdictions that require one on the receipt.</summary>
    public string? FiscalReference { get; set; }

    public bool IsVoided { get; set; }
    public Guid? VoidReasonId { get; set; }
    public string? VoidNote { get; set; }
    public DateTime? VoidedAt { get; set; }
    public Guid? VoidedByStaffId { get; set; }

    public ICollection<CheckLine> Lines { get; set; } = [];
    public ICollection<CheckPayment> Payments { get; set; } = [];
    public ICollection<CheckDiscount> Discounts { get; set; } = [];
}

/// <summary>
/// The share of an order line that this check is paying for. Split-by-item moves whole lines;
/// split-evenly and split-by-amount move fractions, which is why quantity is decimal here.
/// </summary>
public class CheckLine : BaseEntity
{
    public Guid CheckId { get; set; }
    public RestaurantCheck? Check { get; set; }

    public Guid OrderLineId { get; set; }
    public Guid MenuItemId { get; set; }

    public string ItemName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public string? ModifierSummary { get; set; }
    public int? SeatNumber { get; set; }

    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public decimal UnitCost { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>One tender against a check. A check may take several — cash then card is routine.</summary>
public class CheckPayment : BaseEntity
{
    public Guid CheckId { get; set; }
    public RestaurantCheck? Check { get; set; }

    public Guid OutletId { get; set; }
    public Guid? SessionId { get; set; }

    public TenderType TenderType { get; set; } = TenderType.Cash;
    public decimal Amount { get; set; }

    /// <summary>Cash handed over; the difference from <see cref="Amount"/> is the change given.</summary>
    public decimal TenderedAmount { get; set; }
    public decimal ChangeAmount { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;

    /// <summary>Tip taken on this tender specifically — card tips are per-transaction.</summary>
    public decimal TipAmount { get; set; }

    public string? Reference { get; set; }
    public string? CardLast4 { get; set; }
    public string? CardScheme { get; set; }
    public string? AuthCode { get; set; }
    public Guid? GiftCardId { get; set; }
    public int? LoyaltyPointsUsed { get; set; }

    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    public Guid? StaffId { get; set; }

    public bool IsRefund { get; set; }
    public Guid? RefundOfPaymentId { get; set; }
    public string? RefundReason { get; set; }
}

/// <summary>A discount or comp applied to a check, always with a reason and an approver.</summary>
public class CheckDiscount : BaseEntity
{
    public Guid CheckId { get; set; }
    public RestaurantCheck? Check { get; set; }

    /// <summary>Null for a check-level discount; set when only one line was discounted.</summary>
    public Guid? OrderLineId { get; set; }

    public Guid? DiscountReasonId { get; set; }
    public string? ReasonName { get; set; }

    public DiscountKind Kind { get; set; } = DiscountKind.Percentage;
    public decimal Value { get; set; }

    /// <summary>Money actually taken off, after the percentage was resolved.</summary>
    public decimal Amount { get; set; }

    public string? PromoCode { get; set; }
    public Guid? AppliedByStaffId { get; set; }

    /// <summary>Set when the amount crossed the approval threshold and a manager signed it off.</summary>
    public Guid? ApprovedByStaffId { get; set; }
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Why an item, check or payment was cancelled. Configurable so reports mean something.</summary>
public class VoidReason : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    /// <summary>Voiding for this reason requires a supervisor PIN.</summary>
    public bool RequiresApproval { get; set; }

    /// <summary>Food was already cooked — the void should raise a wastage entry.</summary>
    public bool CountsAsWastage { get; set; }
}

/// <summary>Why money was taken off. Separate from void reasons because the reports differ.</summary>
public class DiscountReason : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool RequiresApproval { get; set; }

    /// <summary>Discounts above this need a manager. Zero means always allowed.</summary>
    public decimal MaxAmountWithoutApproval { get; set; }

    /// <summary>A comp is a giveaway (staff meal, service recovery), not a price reduction.</summary>
    public bool IsComp { get; set; }
}

/// <summary>
/// How service charge is added. Tiering by party size is the common case: no charge for two,
/// 10% for a party of eight.
/// </summary>
public class ServiceChargeRule : BaseEntity
{
    public Guid? OutletId { get; set; }
    public string Name { get; set; } = string.Empty;

    public ServiceChargeBasis Basis { get; set; } = ServiceChargeBasis.Percentage;
    public decimal Value { get; set; }

    /// <summary>Applies only from this party size upward. Zero means every party.</summary>
    public int MinPartySize { get; set; }

    /// <summary>Order types it applies to, comma-separated enum values. Empty = dine-in only.</summary>
    public string? ApplicableOrderTypes { get; set; }

    public bool IsTaxable { get; set; } = true;
    public Guid? TaxGroupId { get; set; }

    /// <summary>Guest may ask for it to be removed; doing so is logged.</summary>
    public bool IsWaivable { get; set; } = true;
    public bool RequiresApprovalToWaive { get; set; } = true;

    public int Priority { get; set; }
}

/// <summary>A tip received, kept separate from revenue because it is a liability, not a sale.</summary>
public class TipRecord : BaseEntity
{
    public Guid OutletId { get; set; }
    public Guid? CheckId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? SessionId { get; set; }

    public Guid? WaiterId { get; set; }
    public string? WaiterName { get; set; }

    public decimal Amount { get; set; }
    public TenderType TenderType { get; set; } = TenderType.Card;

    /// <summary>Cash tips are declared by staff rather than captured by the system.</summary>
    public bool IsDeclared { get; set; }

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Set once the tip has been swept into a pool for distribution.</summary>
    public Guid? TipPoolId { get; set; }
}

/// <summary>A shift's tips gathered for sharing out, with the rule used to split them.</summary>
public class TipPool : BaseEntity
{
    public Guid OutletId { get; set; }
    public string Name { get; set; } = string.Empty;

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public TipDistributionBasis Basis { get; set; } = TipDistributionBasis.ByHoursWorked;
    public decimal TotalAmount { get; set; }
    public decimal DistributedAmount { get; set; }

    /// <summary>Share of the pool kept for the kitchen, which never receives tips directly.</summary>
    public decimal KitchenSharePercent { get; set; }

    public bool IsFinalised { get; set; }
    public DateTime? FinalisedAt { get; set; }
    public Guid? FinalisedByStaffId { get; set; }

    public ICollection<TipDistribution> Distributions { get; set; } = [];
}

/// <summary>One staff member's share of a pool, and what it was computed from.</summary>
public class TipDistribution : BaseEntity
{
    public Guid TipPoolId { get; set; }
    public TipPool? TipPool { get; set; }

    public Guid StaffId { get; set; }
    public string? StaffName { get; set; }
    public StaffRole Role { get; set; } = StaffRole.Waiter;

    public decimal HoursWorked { get; set; }
    public decimal SalesAmount { get; set; }

    /// <summary>Share of the pool this person earned, before rounding.</summary>
    public decimal SharePercent { get; set; }
    public decimal Amount { get; set; }

    public bool IsPaidOut { get; set; }
    public DateTime? PaidOutAt { get; set; }
}
