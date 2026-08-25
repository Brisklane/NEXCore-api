using Fitness.Domain.Enums;
using Nexcore.SharedKernel;

namespace Fitness.Domain.Entities;

/// <summary>
/// What a specific member actually bought, on the terms they bought it on.
///
/// The price, the term, the notice period and the entitlements are all copied onto the agreement
/// at signing rather than read through to the plan. That is the whole point: a plan is a price
/// list that changes, an agreement is a contract that does not. A club that raises its rate
/// should not silently raise it for everyone already signed, and a member who queries their
/// direct debit in eighteen months must be able to be shown the number they agreed to.
/// </summary>
public class Agreement : BaseEntity
{
    /// <summary>Human-facing agreement number: AGR-000123.</summary>
    public string AgreementNumber { get; set; } = string.Empty;

    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid PlanId { get; set; }
    public MembershipPlan? Plan { get; set; }

    /// <summary>The plan version signed, so the exact terms can always be reconstructed.</summary>
    public int PlanVersion { get; set; } = 1;

    public Guid ClubId { get; set; }

    public AgreementStatus Status { get; set; } = AgreementStatus.Draft;

    // ── Dates ────────────────────────────────────────────────────────────────

    public DateTime StartsOn { get; set; }

    /// <summary>End of the committed period. Null for a rolling agreement with no commitment.</summary>
    public DateTime? MinimumTermEndsOn { get; set; }

    /// <summary>End of the agreement itself. Null while it renews indefinitely.</summary>
    public DateTime? EndsOn { get; set; }

    public DateTime? SignedOn { get; set; }
    public DateTime? CancelledOn { get; set; }

    /// <summary>The day service actually stops, which is not the day notice was given.</summary>
    public DateTime? CancellationEffectiveOn { get; set; }

    /// <summary>Full refund is due if cancelled before this. Set from the market's statutory window.</summary>
    public DateTime? CoolingOffEndsOn { get; set; }

    // ── Frozen commercial terms ──────────────────────────────────────────────

    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal TaxPercent { get; set; }
    public BillingPeriod BillingPeriod { get; set; } = BillingPeriod.Monthly;
    public BillingAnchor BillingAnchor { get; set; } = BillingAnchor.JoinAnniversary;

    /// <summary>Day of month billing lands on, for a fixed-day anchor.</summary>
    public int? BillingDayOfMonth { get; set; }

    public int NoticePeriodDays { get; set; } = 30;
    public bool AutoRenews { get; set; } = true;

    /// <summary>Protects a legacy rate from a company-wide price rise, on purpose rather than by accident.</summary>
    public bool PriceLocked { get; set; }

    // ── Promotion ────────────────────────────────────────────────────────────

    public Guid? PromotionRuleId { get; set; }
    public Guid? PromoCodeId { get; set; }

    /// <summary>What is being paid while the promotion runs, before the step-up.</summary>
    public decimal? PromotionalPrice { get; set; }

    /// <summary>Billing periods left at the promotional price. Counts down each run.</summary>
    public int PromotionalPeriodsRemaining { get; set; }

    // ── Credits, for a pack ──────────────────────────────────────────────────

    public int CreditsGranted { get; set; }
    public int CreditsRemaining { get; set; }
    public DateTime? CreditsExpireOn { get; set; }

    // ── Billing state ────────────────────────────────────────────────────────

    public DateTime? NextBillingOn { get; set; }
    public DateTime? LastBilledOn { get; set; }

    /// <summary>How many periods have been billed, which is what drives an instalment plan's end.</summary>
    public int PeriodsBilled { get; set; }

    /// <summary>Total instalments for a paid-in-instalments term membership. Zero means it rolls.</summary>
    public int TotalInstalments { get; set; }

    public Guid? PaymentMethodRefId { get; set; }

    /// <summary>Who pays, when it is not the member: a corporate account, an insurer, a parent.</summary>
    public Guid? PayerMemberId { get; set; }
    public Guid? CorporateAccountId { get; set; }
    public Guid? ThirdPartyPayerId { get; set; }

    // ── Cancellation ─────────────────────────────────────────────────────────

    public LeaveReason? LeaveReason { get; set; }
    public string? LeaveNote { get; set; }
    public decimal EarlyTerminationFeeCharged { get; set; }

    /// <summary>The agreement this one replaced on an upgrade, so the history reads as a chain.</summary>
    public Guid? SupersedesAgreementId { get; set; }
    public Guid? SupersededByAgreementId { get; set; }

    // ── Sale attribution ─────────────────────────────────────────────────────

    /// <summary>Who sold it. Drives commission, and the sales leaderboard.</summary>
    public Guid? SoldByStaffId { get; set; }
    public Guid? LeadId { get; set; }

    public string? SignatureImageUrl { get; set; }
    public string? DocumentUrl { get; set; }

    public ICollection<AgreementAmendment> Amendments { get; set; } = [];
    public ICollection<MembershipFreeze> Freezes { get; set; } = [];
}

/// <summary>
/// A versioned contract template with merge fields, per-club and per-jurisdiction variants.
///
/// Versioned rather than edited in place so an agreement can always be reprinted exactly as it
/// was signed — which is the only version that means anything if it is ever disputed.
/// </summary>
public class AgreementTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; } = 1;

    public Guid? ClubId { get; set; }
    public string? CountryCode { get; set; }
    public string? LanguageCode { get; set; }

    /// <summary>Body with {{merge}} fields resolved at signing.</summary>
    public string BodyHtml { get; set; } = string.Empty;

    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }

    public bool RequiresGuardianSignature { get; set; }
    public bool IsPublished { get; set; }
}

/// <summary>
/// A change to a live agreement, recorded rather than applied by mutation.
///
/// Amendments are dated records because "what were they paying in March?" is a question that gets
/// asked, and an agreement whose price column was overwritten cannot answer it.
/// </summary>
public class AgreementAmendment : BaseEntity
{
    public Guid AgreementId { get; set; }
    public Agreement? Agreement { get; set; }

    public AgreementChangeKind Kind { get; set; }
    public DateTime EffectiveOn { get; set; }

    public decimal? PreviousPrice { get; set; }
    public decimal? NewPrice { get; set; }

    public Guid? PreviousPlanId { get; set; }
    public Guid? NewPlanId { get; set; }

    /// <summary>Charged for making the change — an upgrade fee, a transfer fee.</summary>
    public decimal ChangeFee { get; set; }

    /// <summary>Credit or charge raised to settle the part-period the change straddles.</summary>
    public decimal ProrationAmount { get; set; }

    public string? Reason { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? DocumentUrl { get; set; }
}

/// <summary>
/// An e-signature, with everything needed to stand behind it: who signed, when, from where, and
/// against exactly which template version.
/// </summary>
public class AgreementSignature : BaseEntity
{
    public Guid AgreementId { get; set; }
    public Agreement? Agreement { get; set; }

    public Guid? TemplateId { get; set; }
    public int TemplateVersion { get; set; }

    public string SignerName { get; set; } = string.Empty;

    /// <summary>Set when a parent or guardian signed for a minor.</summary>
    public string? GuardianName { get; set; }
    public string? GuardianRelationship { get; set; }

    public SignatureStatus Status { get; set; } = SignatureStatus.NotSigned;
    public DateTime? SignedAt { get; set; }

    public string? SignatureImageUrl { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    /// <summary>Desk signature pad, tablet, remote link, member app.</summary>
    public string? CapturedVia { get; set; }

    /// <summary>Token for a remote signing link, cleared once used.</summary>
    public string? RemoteToken { get; set; }
    public DateTime? RemoteTokenExpiresOn { get; set; }
}

/// <summary>
/// A member-requested pause.
///
/// The end date matters more than the start: a freeze extends the agreement's committed end by
/// the frozen days, so a twelve-month commitment stays twelve months of *service*. Getting that
/// wrong is what turns a freeze into a chargeback three months later.
/// </summary>
public class MembershipFreeze : BaseEntity
{
    public Guid AgreementId { get; set; }
    public Agreement? Agreement { get; set; }

    public Guid MemberId { get; set; }

    public DateTime StartsOn { get; set; }
    public DateTime EndsOn { get; set; }

    /// <summary>Set when the member came back early, which recalculates the fee and the extension.</summary>
    public DateTime? ActuallyEndedOn { get; set; }

    public FreezeReason Reason { get; set; } = FreezeReason.Other;
    public string? ReasonNote { get; set; }

    /// <summary>Charged per month of freeze. Medical freezes usually waive it.</summary>
    public decimal FeePerPeriod { get; set; }
    public decimal TotalFeeCharged { get; set; }

    /// <summary>Days added to the agreement's end. Computed on unfreeze, not on request.</summary>
    public int DaysExtended { get; set; }

    /// <summary>Doctor's letter or similar, required for a medical freeze.</summary>
    public Guid? SupportingDocumentId { get; set; }

    public bool IsMedical { get; set; }

    /// <summary>Counts against the annual freeze-day allowance. Medical freezes usually do not.</summary>
    public bool CountsAgainstAllowance { get; set; } = true;

    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    /// <summary>The freeze has ended and access and billing have been restored.</summary>
    public bool IsReleased { get; set; }
}

/// <summary>
/// A pause the club imposed.
///
/// Deliberately not a freeze. A freeze is a favour the member asked for; a suspension is a
/// sanction. They have different reasons, different reporting and — crucially — different
/// meaning for churn: a suspended member is much closer to leaving than a frozen one, and a
/// system that stores both in one table cannot tell you that.
/// </summary>
public class MembershipSuspension : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid? AgreementId { get; set; }

    public DateTime StartsOn { get; set; }
    public DateTime? EndsOn { get; set; }
    public DateTime? LiftedOn { get; set; }

    public SuspensionReason Reason { get; set; }
    public string? ReasonNote { get; set; }

    /// <summary>Billing keeps running during a suspension for unpaid balance; it stops for conduct.</summary>
    public bool ContinuesBilling { get; set; } = true;

    /// <summary>Lifts itself when the blocking condition clears — the balance is paid, the waiver signed.</summary>
    public bool AutoLiftsWhenResolved { get; set; } = true;

    public Guid? ImposedByUserId { get; set; }
    public Guid? LiftedByUserId { get; set; }
}

/// <summary>
/// A member saying they want to leave, captured before it becomes a cancellation.
///
/// The gap between the request and the effective date is the only window the club has to save
/// them, so it is modelled as a first-class record with an offer attached rather than as a status
/// flip. The reason field on it is the most valuable data in the whole product.
/// </summary>
public class CancellationRequest : BaseEntity
{
    public Guid AgreementId { get; set; }
    public Agreement? Agreement { get; set; }

    public Guid MemberId { get; set; }

    public DateTime RequestedOn { get; set; } = DateTime.UtcNow;

    /// <summary>Requested date + notice period, and never earlier than the minimum term without a fee.</summary>
    public DateTime EffectiveOn { get; set; }

    public LeaveReason Reason { get; set; }
    public string? ReasonNote { get; set; }

    /// <summary>Where the request came in: desk, app, email, phone, letter.</summary>
    public string? Channel { get; set; }

    public decimal EarlyTerminationFee { get; set; }
    public decimal RefundDue { get; set; }
    public decimal OutstandingBalance { get; set; }

    /// <summary>Set when a save offer was accepted and the request was withdrawn.</summary>
    public bool WasSaved { get; set; }
    public DateTime? SavedOn { get; set; }

    public bool IsProcessed { get; set; }
    public Guid? ProcessedByUserId { get; set; }

    /// <summary>Raised automatically so somebody actually tries to win them back later.</summary>
    public bool WinBackTaskCreated { get; set; }

    public ICollection<SaveOffer> Offers { get; set; } = [];
}

/// <summary>What was offered to keep a leaving member, and whether it worked. Feeds the save-rate report.</summary>
public class SaveOffer : BaseEntity
{
    public Guid CancellationRequestId { get; set; }
    public CancellationRequest? CancellationRequest { get; set; }

    public SaveOfferKind Kind { get; set; }

    /// <summary>What was actually said, in the words it was said in.</summary>
    public string Summary { get; set; } = string.Empty;

    public decimal? DiscountValue { get; set; }
    public int? PeriodCount { get; set; }
    public Guid? AlternativePlanId { get; set; }

    public DateTime OfferedAt { get; set; } = DateTime.UtcNow;
    public Guid? OfferedByStaffId { get; set; }

    public bool WasAccepted { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? DeclineNote { get; set; }
}
