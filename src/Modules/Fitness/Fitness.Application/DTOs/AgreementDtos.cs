using Fitness.Domain.Enums;

namespace Fitness.Application.DTOs;

public class AgreementSummaryDto
{
    public Guid Id { get; set; }
    public string AgreementNumber { get; set; } = string.Empty;
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? MemberNumber { get; set; }

    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public PlanKind PlanKind { get; set; }

    public Guid ClubId { get; set; }
    public string? ClubName { get; set; }

    public AgreementStatus Status { get; set; }

    public DateTime StartsOn { get; set; }
    public DateTime? MinimumTermEndsOn { get; set; }
    public DateTime? EndsOn { get; set; }
    public DateTime? CancellationEffectiveOn { get; set; }

    public decimal Price { get; set; }
    public decimal? PromotionalPrice { get; set; }
    public int PromotionalPeriodsRemaining { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public BillingPeriod BillingPeriod { get; set; }

    public DateTime? NextBillingOn { get; set; }
    public decimal? NextBillingAmount { get; set; }

    public int CreditsRemaining { get; set; }
    public int CreditsGranted { get; set; }
    public DateTime? CreditsExpireOn { get; set; }

    /// <summary>Live freeze, when one is running, so the badge on the record is accurate.</summary>
    public bool IsFrozen { get; set; }
    public DateTime? FrozenUntil { get; set; }

    public bool IsInMinimumTerm { get; set; }
    public bool PriceLocked { get; set; }
}

public class AgreementDetailDto : AgreementSummaryDto
{
    public int PlanVersion { get; set; }
    public DateTime? SignedOn { get; set; }
    public DateTime? CancelledOn { get; set; }
    public DateTime? CoolingOffEndsOn { get; set; }
    public bool IsInCoolingOff { get; set; }

    public decimal TaxPercent { get; set; }
    public BillingAnchor BillingAnchor { get; set; }
    public int? BillingDayOfMonth { get; set; }
    public int NoticePeriodDays { get; set; }
    public bool AutoRenews { get; set; }

    public Guid? PromotionRuleId { get; set; }
    public string? PromotionName { get; set; }

    public int PeriodsBilled { get; set; }
    public int TotalInstalments { get; set; }
    public DateTime? LastBilledOn { get; set; }

    public Guid? PaymentMethodRefId { get; set; }
    public string? PaymentMethodLabel { get; set; }

    public Guid? PayerMemberId { get; set; }
    public string? PayerName { get; set; }
    public Guid? CorporateAccountId { get; set; }
    public string? CorporateAccountName { get; set; }
    public Guid? ThirdPartyPayerId { get; set; }

    public LeaveReason? LeaveReason { get; set; }
    public string? LeaveNote { get; set; }
    public decimal EarlyTerminationFeeCharged { get; set; }

    public Guid? SupersedesAgreementId { get; set; }
    public Guid? SupersededByAgreementId { get; set; }

    public Guid? SoldByStaffId { get; set; }
    public string? SoldByName { get; set; }

    public string? SignatureImageUrl { get; set; }
    public string? DocumentUrl { get; set; }

    public List<AgreementAmendmentDto> Amendments { get; set; } = [];
    public List<MembershipFreezeDto> Freezes { get; set; } = [];
    public List<BillingScheduleDto> UpcomingCharges { get; set; } = [];
    public List<PlanEntitlementDto> Entitlements { get; set; } = [];

    /// <summary>Total billed under this agreement so far — the number a save conversation needs.</summary>
    public decimal LifetimeBilled { get; set; }
    public decimal LifetimeCollected { get; set; }
}

public class CreateAgreementDto
{
    public Guid MemberId { get; set; }
    public Guid PlanId { get; set; }
    public Guid ClubId { get; set; }

    public DateTime StartsOn { get; set; }

    public decimal? PriceOverride { get; set; }
    public string? PriceOverrideReason { get; set; }

    public Guid? PromotionRuleId { get; set; }
    public string? PromoCodeText { get; set; }
    public bool WaiveJoiningFee { get; set; }

    public Guid? PaymentMethodRefId { get; set; }
    public Guid? PayerMemberId { get; set; }
    public Guid? CorporateAccountId { get; set; }
    public Guid? ThirdPartyPayerId { get; set; }

    public int? BillingDayOfMonth { get; set; }

    public Guid? SoldByStaffId { get; set; }
    public Guid? LeadId { get; set; }

    public bool TakeFirstPaymentNow { get; set; }
    public bool PriceLocked { get; set; }
}

/// <summary>
/// What a plan change will actually cost, computed before it is committed.
///
/// Shown to the member before they agree, because a proration they were not warned about is the
/// single most common billing complaint in this industry.
/// </summary>
public class PlanChangePreviewDto
{
    public Guid AgreementId { get; set; }
    public Guid NewPlanId { get; set; }
    public string NewPlanName { get; set; } = string.Empty;

    public bool IsAllowed { get; set; }
    public string? BlockReason { get; set; }

    public bool EffectiveImmediately { get; set; }
    public DateTime EffectiveOn { get; set; }

    public decimal CurrentPrice { get; set; }
    public decimal NewPrice { get; set; }
    public decimal PriceDifference { get; set; }

    public decimal ProrationCredit { get; set; }
    public decimal ProrationCharge { get; set; }
    public decimal ChangeFee { get; set; }
    public decimal DueNow { get; set; }

    /// <summary>Plain-English arithmetic, printed under the number.</summary>
    public string? Explanation { get; set; }

    public bool RestartsMinimumTerm { get; set; }
    public DateTime? NewMinimumTermEndsOn { get; set; }
    public bool RequiresApproval { get; set; }
    public DateTime NextBillingOn { get; set; }
    public decimal NextBillingAmount { get; set; }
}

public class ChangePlanDto
{
    public Guid AgreementId { get; set; }
    public Guid NewPlanId { get; set; }
    public DateTime? EffectiveOn { get; set; }
    public decimal? PriceOverride { get; set; }
    public bool WaiveChangeFee { get; set; }
    public string? Reason { get; set; }
    public bool CollectDueNow { get; set; } = true;
}

public class AgreementAmendmentDto
{
    public Guid Id { get; set; }
    public Guid AgreementId { get; set; }
    public AgreementChangeKind Kind { get; set; }
    public DateTime EffectiveOn { get; set; }
    public decimal? PreviousPrice { get; set; }
    public decimal? NewPrice { get; set; }
    public Guid? PreviousPlanId { get; set; }
    public string? PreviousPlanName { get; set; }
    public Guid? NewPlanId { get; set; }
    public string? NewPlanName { get; set; }
    public decimal ChangeFee { get; set; }
    public decimal ProrationAmount { get; set; }
    public string? Reason { get; set; }
    public string? ApprovedByName { get; set; }
    public string? DocumentUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── Templates & signatures ───────────────────────────────────────────────────

public class AgreementTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public Guid? ClubId { get; set; }
    public string? CountryCode { get; set; }
    public string? LanguageCode { get; set; }
    public string BodyHtml { get; set; } = string.Empty;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool RequiresGuardianSignature { get; set; }
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; }

    public int SignatureCount { get; set; }
}

public class AgreementSignatureDto
{
    public Guid Id { get; set; }
    public Guid AgreementId { get; set; }
    public Guid? TemplateId { get; set; }
    public int TemplateVersion { get; set; }
    public string SignerName { get; set; } = string.Empty;
    public string? GuardianName { get; set; }
    public string? GuardianRelationship { get; set; }
    public SignatureStatus Status { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? SignatureImageUrl { get; set; }
    public string? CapturedVia { get; set; }
    public string? IpAddress { get; set; }
}

public class SignAgreementDto
{
    public Guid AgreementId { get; set; }
    public Guid? TemplateId { get; set; }
    public string SignerName { get; set; } = string.Empty;
    public string? SignatureImageUrl { get; set; }
    public string? GuardianName { get; set; }
    public string? GuardianRelationship { get; set; }
    public string? GuardianSignatureUrl { get; set; }
    public string? CapturedVia { get; set; }
}

/// <summary>Sends the agreement out to be signed remotely, rather than at the desk.</summary>
public class RequestRemoteSignatureDto
{
    public Guid AgreementId { get; set; }
    public MessageChannel Channel { get; set; } = MessageChannel.Email;
    public int ExpiryHours { get; set; } = 72;
    public string? Message { get; set; }
}

// ── Freezes ──────────────────────────────────────────────────────────────────

public class MembershipFreezeDto
{
    public Guid Id { get; set; }
    public Guid AgreementId { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }

    public DateTime StartsOn { get; set; }
    public DateTime EndsOn { get; set; }
    public DateTime? ActuallyEndedOn { get; set; }

    public FreezeReason Reason { get; set; }
    public string? ReasonNote { get; set; }

    public decimal FeePerPeriod { get; set; }
    public decimal TotalFeeCharged { get; set; }
    public int DaysExtended { get; set; }

    public bool IsMedical { get; set; }
    public bool CountsAgainstAllowance { get; set; }
    public Guid? SupportingDocumentId { get; set; }

    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public bool IsReleased { get; set; }
    public bool IsCurrentlyActive { get; set; }
}

public class RequestFreezeDto
{
    public Guid AgreementId { get; set; }
    public DateTime StartsOn { get; set; }
    public DateTime EndsOn { get; set; }
    public FreezeReason Reason { get; set; }
    public string? ReasonNote { get; set; }
    public bool IsMedical { get; set; }
    public Guid? SupportingDocumentId { get; set; }
    public decimal? FeeOverride { get; set; }
    public bool WaiveFee { get; set; }
}

/// <summary>
/// What a freeze will do, before it is agreed: the fee, the days added, the billing that moves.
/// </summary>
public class FreezePreviewDto
{
    public bool IsAllowed { get; set; }
    public string? BlockReason { get; set; }

    public DateTime StartsOn { get; set; }
    public DateTime EndsOn { get; set; }
    public int FreezeDays { get; set; }

    public int FreezeDaysUsedThisYear { get; set; }
    public int FreezeDaysAllowance { get; set; }
    public int FreezeDaysRemaining { get; set; }

    public decimal Fee { get; set; }

    /// <summary>Charges that will be skipped, and the new dates they move to.</summary>
    public List<BillingScheduleDto> SkippedCharges { get; set; } = [];

    public DateTime? NewMinimumTermEndsOn { get; set; }
    public DateTime NewNextBillingOn { get; set; }
    public string? Explanation { get; set; }
}

public class EndFreezeDto
{
    public Guid FreezeId { get; set; }
    public DateTime? EndOn { get; set; }
    public string? Reason { get; set; }
}

// ── Suspensions ──────────────────────────────────────────────────────────────

public class MembershipSuspensionDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public Guid? AgreementId { get; set; }

    public DateTime StartsOn { get; set; }
    public DateTime? EndsOn { get; set; }
    public DateTime? LiftedOn { get; set; }

    public SuspensionReason Reason { get; set; }
    public string? ReasonNote { get; set; }
    public bool ContinuesBilling { get; set; }
    public bool AutoLiftsWhenResolved { get; set; }

    public string? ImposedByName { get; set; }
    public string? LiftedByName { get; set; }
    public bool IsCurrentlyActive { get; set; }
}

public class SuspendMemberDto
{
    public Guid MemberId { get; set; }
    public Guid? AgreementId { get; set; }
    public SuspensionReason Reason { get; set; }
    public string? ReasonNote { get; set; }
    public DateTime? EndsOn { get; set; }
    public bool ContinuesBilling { get; set; } = true;
    public bool AutoLiftsWhenResolved { get; set; } = true;
}

// ── Cancellation ─────────────────────────────────────────────────────────────

public class CancellationRequestDto
{
    public Guid Id { get; set; }
    public Guid AgreementId { get; set; }
    public string? AgreementNumber { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? PlanName { get; set; }

    public DateTime RequestedOn { get; set; }
    public DateTime EffectiveOn { get; set; }

    public LeaveReason Reason { get; set; }
    public string? ReasonNote { get; set; }
    public string? Channel { get; set; }

    public decimal EarlyTerminationFee { get; set; }
    public decimal RefundDue { get; set; }
    public decimal OutstandingBalance { get; set; }

    public bool WasSaved { get; set; }
    public DateTime? SavedOn { get; set; }
    public bool IsProcessed { get; set; }

    public decimal LifetimeValue { get; set; }
    public int TenureMonths { get; set; }

    public List<SaveOfferDto> Offers { get; set; } = [];
}

/// <summary>What cancelling will cost and when it takes effect, shown before anyone commits.</summary>
public class CancellationPreviewDto
{
    public Guid AgreementId { get; set; }

    public DateTime RequestedOn { get; set; }
    public DateTime EffectiveOn { get; set; }
    public int NoticePeriodDays { get; set; }

    public bool IsInMinimumTerm { get; set; }
    public DateTime? MinimumTermEndsOn { get; set; }
    public int MonthsRemainingInTerm { get; set; }

    public bool IsInCoolingOff { get; set; }

    public decimal EarlyTerminationFee { get; set; }
    public decimal FinalCharge { get; set; }
    public decimal RefundDue { get; set; }
    public decimal OutstandingBalance { get; set; }
    public decimal NetDue { get; set; }

    public string? Explanation { get; set; }

    /// <summary>Everything still booked, which is what a member is actually giving up.</summary>
    public List<UpcomingBookingDto> BookingsToCancel { get; set; } = [];
    public int UnusedCredits { get; set; }
    public decimal UnusedCreditValue { get; set; }

    /// <summary>Offers worth making, given this member's history.</summary>
    public List<SaveOfferSuggestionDto> SuggestedOffers { get; set; } = [];
}

public class SaveOfferSuggestionDto
{
    public SaveOfferKind Kind { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public decimal? Value { get; set; }
    public int? PeriodCount { get; set; }
    public Guid? AlternativePlanId { get; set; }

    /// <summary>How often this offer has actually worked for this reason, at this club.</summary>
    public int HistoricAcceptRatePercent { get; set; }
}

public class RequestCancellationDto
{
    public Guid AgreementId { get; set; }
    public LeaveReason Reason { get; set; }
    public string? ReasonNote { get; set; }
    public DateTime? RequestedEffectiveOn { get; set; }
    public string? Channel { get; set; }
    public bool WaiveEarlyTerminationFee { get; set; }
    public string? WaiverReason { get; set; }
}

public class SaveOfferDto
{
    public Guid Id { get; set; }
    public Guid CancellationRequestId { get; set; }
    public SaveOfferKind Kind { get; set; }
    public string Summary { get; set; } = string.Empty;
    public decimal? DiscountValue { get; set; }
    public int? PeriodCount { get; set; }
    public Guid? AlternativePlanId { get; set; }
    public DateTime OfferedAt { get; set; }
    public string? OfferedByName { get; set; }
    public bool WasAccepted { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? DeclineNote { get; set; }
}

public class MakeSaveOfferDto
{
    public Guid CancellationRequestId { get; set; }
    public SaveOfferKind Kind { get; set; }
    public string Summary { get; set; } = string.Empty;
    public decimal? DiscountValue { get; set; }
    public int? PeriodCount { get; set; }
    public Guid? AlternativePlanId { get; set; }
}

public class RespondToSaveOfferDto
{
    public Guid SaveOfferId { get; set; }
    public bool Accepted { get; set; }
    public string? Note { get; set; }
}
