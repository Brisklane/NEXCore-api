using Fitness.Domain.Enums;

namespace Fitness.Application.DTOs;

// ── Plans ────────────────────────────────────────────────────────────────────

public class MembershipPlanDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public PlanKind Kind { get; set; }

    public string? MarketingBlurb { get; set; }
    public string? ImageUrl { get; set; }
    public string? ColourHex { get; set; }
    public int DisplayOrder { get; set; }

    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal TaxPercent { get; set; }
    public bool PriceIncludesTax { get; set; }

    public BillingPeriod BillingPeriod { get; set; }
    public BillingAnchor BillingAnchor { get; set; }
    public ProrationRule JoinProration { get; set; }
    public ProrationRule CancelProration { get; set; }

    public decimal JoiningFee { get; set; }
    public decimal AdminFee { get; set; }
    public decimal CardFee { get; set; }
    public decimal AnnualMaintenanceFee { get; set; }
    public int? AnnualFeeMonth { get; set; }

    public int MinimumTermMonths { get; set; }
    public int? DurationMonths { get; set; }
    public int NoticePeriodDays { get; set; }
    public bool AutoRenews { get; set; }
    public decimal EarlyTerminationFee { get; set; }
    public decimal EarlyTerminationPercentOfRemaining { get; set; }

    public int CreditCount { get; set; }
    public int ValidForDays { get; set; }
    public bool CreditsTransferable { get; set; }
    public bool CreditsRefundable { get; set; }

    public Guid? RestrictedToClubId { get; set; }
    public bool AllowsCrossClubAccess { get; set; }
    public int VisitsPerPeriod { get; set; }
    public EntitlementLimit VisitLimitBasis { get; set; }
    public int GuestPassesPerPeriod { get; set; }

    public int BookingWindowDays { get; set; }
    public int MaxConcurrentBookings { get; set; }

    public DateTime? SellableFrom { get; set; }
    public DateTime? SellableTo { get; set; }
    public bool SellableAtDesk { get; set; }
    public bool SellableOnline { get; set; }
    public bool SellableInApp { get; set; }
    public bool SellableAtKiosk { get; set; }
    public bool IsPrivate { get; set; }

    public int? MinimumAge { get; set; }
    public int? MaximumAge { get; set; }
    public EligibilityProof RequiredProof { get; set; }

    public Guid? WaiverTemplateId { get; set; }
    public Guid? AgreementTemplateId { get; set; }
    public bool RequiresHealthScreening { get; set; }

    public Guid? InventoryItemId { get; set; }

    public RevenueRecognitionBasis RecognitionBasis { get; set; }
    public Guid? RevenueAccountId { get; set; }
    public Guid? DeferredRevenueAccountId { get; set; }

    public int Version { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }

    public List<PlanPriceDto> ClubPrices { get; set; } = [];
    public List<PlanEntitlementDto> Entitlements { get; set; } = [];

    // Roll-ups so the catalogue screen can show what is actually selling.
    public int ActiveMemberCount { get; set; }
    public decimal MonthlyRecurringRevenue { get; set; }
    public int SoldLast30Days { get; set; }
}

public class SavePlanDto
{
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public PlanKind Kind { get; set; } = PlanKind.RecurringMembership;

    public string? MarketingBlurb { get; set; }
    public string? ImageUrl { get; set; }
    public string? ColourHex { get; set; }
    public int DisplayOrder { get; set; }

    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal TaxPercent { get; set; }
    public bool PriceIncludesTax { get; set; } = true;

    public BillingPeriod BillingPeriod { get; set; } = BillingPeriod.Monthly;
    public BillingAnchor BillingAnchor { get; set; } = BillingAnchor.JoinAnniversary;
    public ProrationRule JoinProration { get; set; } = ProrationRule.Daily;
    public ProrationRule CancelProration { get; set; } = ProrationRule.None;

    public decimal JoiningFee { get; set; }
    public decimal AdminFee { get; set; }
    public decimal CardFee { get; set; }
    public decimal AnnualMaintenanceFee { get; set; }
    public int? AnnualFeeMonth { get; set; }

    public int MinimumTermMonths { get; set; }
    public int? DurationMonths { get; set; }
    public int NoticePeriodDays { get; set; } = 30;
    public bool AutoRenews { get; set; } = true;
    public decimal EarlyTerminationFee { get; set; }
    public decimal EarlyTerminationPercentOfRemaining { get; set; }

    public int CreditCount { get; set; }
    public int ValidForDays { get; set; }
    public bool CreditsTransferable { get; set; }
    public bool CreditsRefundable { get; set; }

    public Guid? RestrictedToClubId { get; set; }
    public bool AllowsCrossClubAccess { get; set; } = true;
    public int VisitsPerPeriod { get; set; }
    public EntitlementLimit VisitLimitBasis { get; set; } = EntitlementLimit.Unlimited;
    public int GuestPassesPerPeriod { get; set; }

    public int BookingWindowDays { get; set; } = 14;
    public int MaxConcurrentBookings { get; set; }

    public DateTime? SellableFrom { get; set; }
    public DateTime? SellableTo { get; set; }
    public bool SellableAtDesk { get; set; } = true;
    public bool SellableOnline { get; set; } = true;
    public bool SellableInApp { get; set; } = true;
    public bool SellableAtKiosk { get; set; }
    public bool IsPrivate { get; set; }

    public int? MinimumAge { get; set; }
    public int? MaximumAge { get; set; }
    public EligibilityProof RequiredProof { get; set; } = EligibilityProof.None;

    public Guid? WaiverTemplateId { get; set; }
    public Guid? AgreementTemplateId { get; set; }
    public bool RequiresHealthScreening { get; set; } = true;

    public Guid? InventoryItemId { get; set; }

    public RevenueRecognitionBasis RecognitionBasis { get; set; } = RevenueRecognitionBasis.StraightLine;
    public Guid? RevenueAccountId { get; set; }
    public Guid? DeferredRevenueAccountId { get; set; }

    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }

    public List<PlanPriceDto> ClubPrices { get; set; } = [];
    public List<PlanEntitlementDto> Entitlements { get; set; } = [];

    /// <summary>
    /// Whether a price change applies to members already on this plan. Defaults to no, because
    /// silently repricing four hundred live agreements is a decision, not a side effect.
    /// </summary>
    public bool ApplyPriceChangeToExisting { get; set; }
}

public class PlanPriceDto
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public Guid ClubId { get; set; }
    public string? ClubName { get; set; }
    public decimal Price { get; set; }
    public decimal? JoiningFee { get; set; }
    public string? CurrencyCode { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsAvailable { get; set; }
}

public class PlanEntitlementDto
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public EntitlementKind Kind { get; set; }
    public Guid? TargetId { get; set; }
    public string? TargetName { get; set; }
    public EntitlementLimit Limit { get; set; }
    public int Quantity { get; set; }
    public decimal OverageFee { get; set; }
    public bool AllowOverage { get; set; }
    public List<AccessTimeBandDto> TimeBands { get; set; } = [];
}

public class AccessTimeBandDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DaysOfWeekMask { get; set; }
    public TimeSpan StartsAt { get; set; }
    public TimeSpan EndsAt { get; set; }
}

public class PlanChangePathDto
{
    public Guid Id { get; set; }
    public Guid FromPlanId { get; set; }
    public string? FromPlanName { get; set; }
    public Guid ToPlanId { get; set; }
    public string? ToPlanName { get; set; }
    public bool EffectiveImmediately { get; set; }
    public ProrationRule Proration { get; set; }
    public decimal ChangeFee { get; set; }
    public bool RestartsMinimumTerm { get; set; }
    public bool RequiresApproval { get; set; }
}

// ── Promotions ───────────────────────────────────────────────────────────────

public class PromotionRuleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? PlanId { get; set; }
    public string? PlanName { get; set; }
    public Guid? ClubId { get; set; }
    public DiscountKind DiscountKind { get; set; }
    public decimal Value { get; set; }
    public int PeriodCount { get; set; }
    public DateTime? ActiveFrom { get; set; }
    public DateTime? ActiveTo { get; set; }
    public bool NewMembersOnly { get; set; }
    public int MaxRedemptions { get; set; }
    public int RedemptionCount { get; set; }
    public Guid? CampaignId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }

    public List<PromoCodeDto> Codes { get; set; } = [];
}

public class PromoCodeDto
{
    public Guid Id { get; set; }
    public Guid PromotionRuleId { get; set; }
    public string CodeText { get; set; } = string.Empty;
    public int MaxUses { get; set; }
    public int UseCount { get; set; }
    public bool OnePerMember { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public Guid? IssuedToMemberId { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Whether a code can be used here, and what it is worth — checked before it is applied.</summary>
public class PromoCodeCheckDto
{
    public string CodeText { get; set; } = string.Empty;
    public Guid? PlanId { get; set; }
    public Guid? ClubId { get; set; }
    public Guid? MemberId { get; set; }
}

public class PromoCodeResultDto
{
    public bool IsValid { get; set; }
    public string? Reason { get; set; }
    public Guid? PromotionRuleId { get; set; }
    public string? PromotionName { get; set; }
    public DiscountKind? DiscountKind { get; set; }
    public decimal? Value { get; set; }
    public int PeriodCount { get; set; }

    public decimal? OriginalPrice { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public decimal? SavingPerPeriod { get; set; }
}

// ── Appointment services ─────────────────────────────────────────────────────

public class AppointmentServiceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AppointmentKind Kind { get; set; }
    public int DurationMinutes { get; set; }
    public int BufferMinutes { get; set; }
    public decimal Price { get; set; }
    public decimal TaxPercent { get; set; }
    public int MaxParticipants { get; set; }
    public Guid? RequiredResourceId { get; set; }
    public Guid? RequiredRoomId { get; set; }
    public int FreeCancelHours { get; set; }
    public PolicyOutcome LateCancelOutcome { get; set; }
    public PolicyOutcome NoShowOutcome { get; set; }
    public string? ColourHex { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool BookableOnline { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// The catalogue a join wizard or a till renders: what may be sold here, priced for this club,
/// already filtered to what the operator is allowed to offer.
/// </summary>
public class SalesCatalogueDto
{
    public Guid ClubId { get; set; }
    public string ClubName { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "USD";

    public List<MembershipPlanDto> Memberships { get; set; } = [];
    public List<MembershipPlanDto> Packs { get; set; } = [];
    public List<MembershipPlanDto> Passes { get; set; } = [];
    public List<AppointmentServiceDto> Services { get; set; } = [];
    public List<RetailProductDto> Products { get; set; } = [];
    public List<PromotionRuleDto> ActivePromotions { get; set; } = [];
}

/// <summary>A pro-shop line resolved against Inventory, with the stock figure the till needs.</summary>
public class RetailProductDto
{
    public Guid Id { get; set; }
    public Guid? InventoryItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? Category { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal UnitCost { get; set; }
    public decimal StockOnHand { get; set; }
    public bool TracksStock { get; set; }
    public bool IsActive { get; set; }
}
