using Fitness.Domain.Enums;
using Nexcore.SharedKernel;

namespace Fitness.Domain.Entities;

/// <summary>
/// Something the club sells: a membership, a pack, a pass, a service.
///
/// The plan is the *template*. What a specific member actually holds is an <see cref="Agreement"/>,
/// and the price they pay is frozen onto it. That separation is what lets a club raise the price
/// of "Gold Monthly" tomorrow without silently raising it for four hundred existing members —
/// which is both a commercial decision and, in several markets, a legal one.
/// </summary>
public class MembershipPlan : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public PlanKind Kind { get; set; } = PlanKind.RecurringMembership;

    /// <summary>Shown to the member on the join page and the app.</summary>
    public string? MarketingBlurb { get; set; }
    public string? ImageUrl { get; set; }
    public string? ColourHex { get; set; }
    public int DisplayOrder { get; set; }

    // ── Price ────────────────────────────────────────────────────────────────

    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal TaxPercent { get; set; }

    /// <summary>True when <see cref="Price"/> already contains the tax, which is how most consumer markets quote.</summary>
    public bool PriceIncludesTax { get; set; } = true;

    public BillingPeriod BillingPeriod { get; set; } = BillingPeriod.Monthly;
    public BillingAnchor BillingAnchor { get; set; } = BillingAnchor.JoinAnniversary;
    public ProrationRule JoinProration { get; set; } = ProrationRule.Daily;
    public ProrationRule CancelProration { get; set; } = ProrationRule.None;

    // ── Fees ─────────────────────────────────────────────────────────────────

    public decimal JoiningFee { get; set; }
    public decimal AdminFee { get; set; }
    public decimal CardFee { get; set; }

    /// <summary>The annual maintenance fee large clubs charge on a fixed month.</summary>
    public decimal AnnualMaintenanceFee { get; set; }
    public int? AnnualFeeMonth { get; set; }

    // ── Term ─────────────────────────────────────────────────────────────────

    /// <summary>Committed months. Zero means rolling with no commitment.</summary>
    public int MinimumTermMonths { get; set; }

    /// <summary>Total length for a term membership. Zero means it runs until cancelled.</summary>
    public int? DurationMonths { get; set; }

    public int NoticePeriodDays { get; set; } = 30;
    public bool AutoRenews { get; set; } = true;

    /// <summary>
    /// Charged when someone leaves inside the minimum term. Expressed as a fixed amount, a
    /// percentage of the remaining commitment, or both.
    /// </summary>
    public decimal EarlyTerminationFee { get; set; }
    public decimal EarlyTerminationPercentOfRemaining { get; set; }

    // ── Pack & pass shapes ───────────────────────────────────────────────────

    /// <summary>Credits a pack grants. Zero for anything that is not a pack.</summary>
    public int CreditCount { get; set; }

    /// <summary>Days a pack or pass is valid for from purchase. Zero means it never expires.</summary>
    public int ValidForDays { get; set; }

    /// <summary>Whether unused credits can be moved to another member.</summary>
    public bool CreditsTransferable { get; set; }

    /// <summary>Whether unused credits refund, or simply lapse.</summary>
    public bool CreditsRefundable { get; set; }

    // ── Access ───────────────────────────────────────────────────────────────

    /// <summary>Null means every club in the company. A single club id restricts to that site.</summary>
    public Guid? RestrictedToClubId { get; set; }

    public bool AllowsCrossClubAccess { get; set; } = true;

    /// <summary>Visits allowed in a period; zero is unlimited.</summary>
    public int VisitsPerPeriod { get; set; }
    public EntitlementLimit VisitLimitBasis { get; set; } = EntitlementLimit.Unlimited;

    public int GuestPassesPerPeriod { get; set; }

    // ── Booking ──────────────────────────────────────────────────────────────

    /// <summary>How many days ahead a holder may book. Premium plans get a longer window.</summary>
    public int BookingWindowDays { get; set; } = 14;

    /// <summary>How many future bookings they may hold at once. Zero is unlimited.</summary>
    public int MaxConcurrentBookings { get; set; }

    // ── Availability ─────────────────────────────────────────────────────────

    public DateTime? SellableFrom { get; set; }
    public DateTime? SellableTo { get; set; }

    public bool SellableAtDesk { get; set; } = true;
    public bool SellableOnline { get; set; } = true;
    public bool SellableInApp { get; set; } = true;
    public bool SellableAtKiosk { get; set; }

    /// <summary>Hidden from public lists — a staff rate, a corporate rate, a legacy plan.</summary>
    public bool IsPrivate { get; set; }

    // ── Eligibility ──────────────────────────────────────────────────────────

    public int? MinimumAge { get; set; }
    public int? MaximumAge { get; set; }

    /// <summary>Student, senior, corporate or staff rates need proof before the plan can be sold.</summary>
    public EligibilityProof RequiredProof { get; set; } = EligibilityProof.None;

    // ── Paperwork ────────────────────────────────────────────────────────────

    public Guid? WaiverTemplateId { get; set; }
    public Guid? AgreementTemplateId { get; set; }

    /// <summary>Health screening is mandatory before this plan can be used.</summary>
    public bool RequiresHealthScreening { get; set; } = true;

    // ── Retail link ──────────────────────────────────────────────────────────

    /// <summary>Inventory item, for a <see cref="PlanKind.RetailProduct"/>. Stock is never held here.</summary>
    public Guid? InventoryItemId { get; set; }

    // ── Revenue ──────────────────────────────────────────────────────────────

    public RevenueRecognitionBasis RecognitionBasis { get; set; } = RevenueRecognitionBasis.StraightLine;

    /// <summary>Which GL account revenue from this plan posts to, when Accounting is installed.</summary>
    public Guid? RevenueAccountId { get; set; }
    public Guid? DeferredRevenueAccountId { get; set; }

    /// <summary>Bumped whenever the price or terms change, so an agreement can name what it bought.</summary>
    public int Version { get; set; } = 1;

    public ICollection<PlanPrice> ClubPrices { get; set; } = [];
    public ICollection<PlanEntitlement> Entitlements { get; set; } = [];
}

/// <summary>
/// A price override for one club. Plans are company-wide; what a member pays is not — the same
/// "Gold Monthly" is a different number in the city-centre club and the suburban one.
/// </summary>
public class PlanPrice : BaseEntity
{
    public Guid PlanId { get; set; }
    public MembershipPlan? Plan { get; set; }

    public Guid ClubId { get; set; }

    public decimal Price { get; set; }
    public decimal? JoiningFee { get; set; }
    public string? CurrencyCode { get; set; }

    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    /// <summary>Stops this plan being sold at this club without removing the price.</summary>
    public bool IsAvailable { get; set; } = true;
}

/// <summary>
/// One thing a plan grants and how much of it.
///
/// Modelled as rows rather than columns because the list is open-ended — a club will invent
/// "two sauna sessions a month" next quarter — and because the access engine and the booking
/// engine both read the same table, so an entitlement can never mean one thing at the door and
/// another in the timetable.
/// </summary>
public class PlanEntitlement : BaseEntity
{
    public Guid PlanId { get; set; }
    public MembershipPlan? Plan { get; set; }

    public EntitlementKind Kind { get; set; }

    /// <summary>What it applies to: a club, an area, a class type, a service. Null means all of that kind.</summary>
    public Guid? TargetId { get; set; }
    public string? TargetName { get; set; }

    public EntitlementLimit Limit { get; set; } = EntitlementLimit.Unlimited;

    /// <summary>How many, for a limited entitlement. Ignored when unlimited.</summary>
    public int Quantity { get; set; }

    /// <summary>Time bands this entitlement is usable in. Empty means any time the club is open.</summary>
    public ICollection<AccessTimeBand> TimeBands { get; set; } = [];

    /// <summary>Charged when the member exceeds the allowance rather than being refused.</summary>
    public decimal OverageFee { get; set; }

    public bool AllowOverage { get; set; }
}

/// <summary>
/// A window during which an entitlement is usable — the off-peak plan that stops at 16:00, the
/// weekend-only pass. Evaluated by the access engine and by the booking engine alike.
/// </summary>
public class AccessTimeBand : BaseEntity
{
    public Guid? EntitlementId { get; set; }
    public PlanEntitlement? Entitlement { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Bit flags, Sunday = 1 … Saturday = 64, so one row covers "weekdays".</summary>
    public int DaysOfWeekMask { get; set; } = 127;

    public TimeSpan StartsAt { get; set; }
    public TimeSpan EndsAt { get; set; }
}

/// <summary>
/// A price change on a plan, expressed as a rule rather than by editing the plan.
///
/// Kept as its own record so the intro-then-step-up shape ("£19 for three months, then £39") is
/// something billing can execute for years afterwards, rather than something a human has to
/// remember to do on a date.
/// </summary>
public class PromotionRule : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Null applies it to every plan.</summary>
    public Guid? PlanId { get; set; }
    public Guid? ClubId { get; set; }

    public DiscountKind DiscountKind { get; set; } = DiscountKind.Percentage;
    public decimal Value { get; set; }

    /// <summary>How many billing periods the promotion runs for. Zero means for the life of the agreement.</summary>
    public int PeriodCount { get; set; }

    public DateTime? ActiveFrom { get; set; }
    public DateTime? ActiveTo { get; set; }

    /// <summary>Applies to new joins only, or to existing members too.</summary>
    public bool NewMembersOnly { get; set; } = true;

    /// <summary>Total redemptions allowed across all members. Zero is unlimited.</summary>
    public int MaxRedemptions { get; set; }
    public int RedemptionCount { get; set; }

    public Guid? CampaignId { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>A code a member types to claim a promotion, with its own limits and attribution.</summary>
public class PromoCode : BaseEntity
{
    public Guid PromotionRuleId { get; set; }
    public PromotionRule? PromotionRule { get; set; }

    public string CodeText { get; set; } = string.Empty;

    public int MaxUses { get; set; }
    public int UseCount { get; set; }

    /// <summary>One use per member, which is what stops a code leaking onto a deals site.</summary>
    public bool OnePerMember { get; set; } = true;

    public DateTime? ExpiresOn { get; set; }

    /// <summary>Who the code was issued to, for a personal referral or win-back code.</summary>
    public Guid? IssuedToMemberId { get; set; }
}

/// <summary>
/// A legal move between two plans, and what it costs.
///
/// Upgrades and downgrades are not symmetrical — most clubs let you upgrade today and make you
/// wait until the next period to downgrade — so the direction carries its own rules rather than
/// sharing one setting.
/// </summary>
public class PlanChangePath : BaseEntity
{
    public Guid FromPlanId { get; set; }
    public Guid ToPlanId { get; set; }

    /// <summary>Takes effect immediately, or at the start of the next billing period.</summary>
    public bool EffectiveImmediately { get; set; } = true;

    public ProrationRule Proration { get; set; } = ProrationRule.Daily;

    public decimal ChangeFee { get; set; }

    /// <summary>Whether the minimum term restarts on change. Usually true upward, false downward.</summary>
    public bool RestartsMinimumTerm { get; set; }

    public bool RequiresApproval { get; set; }
}

/// <summary>
/// A service that can be booked and billed on its own — a PT session, an assessment, a sports
/// massage, a swim lesson. Distinct from a plan because it has a duration and a deliverer.
/// </summary>
public class AppointmentService : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public AppointmentKind Kind { get; set; } = AppointmentKind.PersonalTraining;

    public int DurationMinutes { get; set; } = 60;

    /// <summary>Gap after the session before the trainer's next booking can start.</summary>
    public int BufferMinutes { get; set; }

    public decimal Price { get; set; }
    public decimal TaxPercent { get; set; }

    /// <summary>Maximum participants. One for a private, up to four for semi-private, more for small group.</summary>
    public int MaxParticipants { get; set; } = 1;

    /// <summary>Needed to deliver it — a room, a court, a piece of equipment.</summary>
    public Guid? RequiredResourceId { get; set; }
    public Guid? RequiredRoomId { get; set; }

    /// <summary>Hours before the start after which a cancellation is "late".</summary>
    public int FreeCancelHours { get; set; } = 24;
    public PolicyOutcome LateCancelOutcome { get; set; } = PolicyOutcome.ForfeitCredit;
    public PolicyOutcome NoShowOutcome { get; set; } = PolicyOutcome.ForfeitCredit;

    public string? ColourHex { get; set; }
    public int DisplayOrder { get; set; }

    public bool BookableOnline { get; set; } = true;
}
