using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// A time-bound promotional campaign that can discount items, categories,
/// or entire baskets for qualifying customers.
/// </summary>
public class Promotion : BaseEntity
{
    // ─── Identity ────────────────────────────────────────────────────────────
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// If set, cashier/customer must enter this code to activate the promotion.
    /// Null means the promotion is evaluated and applied automatically.
    /// </summary>
    public string? PromotionCode { get; set; }
    public bool IsAutoApplied { get; set; } = true;

    public PromotionStatus Status { get; set; } = PromotionStatus.Draft;

    /// <summary>
    /// Higher value wins when multiple promotions apply to the same item
    /// and IsStackable is false.
    /// </summary>
    public int Priority { get; set; } = 0;

    // ─── Date / Time Window ──────────────────────────────────────────────────
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>Daily window start. Null means the promotion runs all day.</summary>
    public TimeOnly? StartTime { get; set; }
    /// <summary>Daily window end. Null means the promotion runs all day.</summary>
    public TimeOnly? EndTime { get; set; }

    /// <summary>
    /// Days of the week the promotion is active.
    /// Stored as an int bitmask — use <see cref="ScheduledDays"/> flags.
    /// Default: every day.
    /// </summary>
    public ScheduledDays ScheduledDays { get; set; } = ScheduledDays.EveryDay;

    // ─── Usage Limits ────────────────────────────────────────────────────────
    /// <summary>Total redemption cap across all customers. Null = unlimited.</summary>
    public int? MaxUsageCount { get; set; }
    /// <summary>Per-customer redemption cap. Null = unlimited.</summary>
    public int? MaxUsagePerCustomer { get; set; }
    /// <summary>Incremented each time the promotion is successfully applied.</summary>
    public int CurrentUsageCount { get; set; } = 0;

    // ─── Order-Level Conditions ──────────────────────────────────────────────
    /// <summary>Minimum basket subtotal before this promotion is evaluated.</summary>
    public decimal? MinOrderAmount { get; set; }

    // ─── Customer Targeting ──────────────────────────────────────────────────
    public PromotionTargetType TargetType { get; set; } = PromotionTargetType.AllCustomers;
    /// <summary>Minimum loyalty tier required. Null unless TargetType = LoyaltyTier.</summary>
    public LoyaltyTier? RequiredLoyaltyTier { get; set; }
    /// <summary>Price list that must be active for the customer. Null unless TargetType = PriceList.</summary>
    public Guid? RequiredPriceListId { get; set; }
    /// <summary>Specific customer this offer is personalised for.</summary>
    public Guid? TargetContactId { get; set; }

    // ─── Stacking ────────────────────────────────────────────────────────────
    /// <summary>When true, this promotion can be combined with other active promotions on the same order.</summary>
    public bool IsStackable { get; set; } = false;

    public string? Notes { get; set; }

    // ─── Navigation ──────────────────────────────────────────────────────────
    public ICollection<PromotionItem> Items { get; set; } = new List<PromotionItem>();
}
