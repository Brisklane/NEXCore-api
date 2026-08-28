using Nexcore.SharedKernel;
using RealEstate.Domain.Enums;

namespace RealEstate.Domain.Entities;

/// <summary>
/// A dealer or broker who sells the developer's stock for a commission. In most South Asian and
/// Gulf launches these partners produce the majority of bookings, so they get their own onboarding,
/// their own portal and their own money.
/// </summary>
public class ChannelPartner : BaseEntity
{
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? TradingName { get; set; }
    public Guid? PartyId { get; set; }

    public PartnerStatus Status { get; set; } = PartnerStatus.Applied;
    public Guid? TierId { get; set; }

    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public Guid? GeoAreaId { get; set; }

    public string? RegistrationNumber { get; set; }
    public string? LicenceNumber { get; set; }
    public DateOnly? LicenceExpiresOn { get; set; }
    public string? TaxNumber { get; set; }

    public string? BankName { get; set; }
    public string? AccountTitle { get; set; }
    public string? AccountNumber { get; set; }
    public bool BankDetailsVerified { get; set; }
    public decimal WithholdingPercent { get; set; }

    public Guid? RelationshipManagerUserId { get; set; }
    public DateOnly? OnboardedOn { get; set; }
    public Guid? ApprovalRequestId { get; set; }

    // ── Performance, recomputed nightly ──────────────────────────────────────

    public int LeadsRegistered { get; set; }
    public int SiteVisitsDone { get; set; }
    public int BookingsMade { get; set; }
    public decimal BookingValue { get; set; }
    public decimal CollectionContribution { get; set; }
    public int CancellationCount { get; set; }
    public decimal ConversionPercent { get; set; }

    public decimal CommissionEarned { get; set; }
    public decimal CommissionPaid { get; set; }
    public decimal CommissionPending { get; set; }
    public decimal AdvanceOutstanding { get; set; }

    public DateOnly? SuspendedOn { get; set; }
    public string? SuspensionReason { get; set; }
    public string? Notes { get; set; }

    public ICollection<PartnerUser> Users { get; set; } = [];
    public ICollection<PartnerAuthorisation> Authorisations { get; set; } = [];
    public ICollection<PartnerDocument> Documents { get; set; } = [];
}

/// <summary>A login under one partner. Several, because a dealer has a team and each seller has their own book.</summary>
public class PartnerUser : BaseEntity
{
    public Guid ChannelPartnerId { get; set; }
    public ChannelPartner? Partner { get; set; }

    public Guid? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Designation { get; set; }

    /// <summary>Only the primary user sees the partner's money. The rest see their own leads.</summary>
    public bool IsPrimary { get; set; }

    public bool CanViewCommission { get; set; }
    public bool CanRegisterLeads { get; set; } = true;
    public bool CanBookVisits { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    public int LeadsRegistered { get; set; }
    public int BookingsMade { get; set; }
}

public class PartnerAgreement : BaseEntity
{
    public Guid ChannelPartnerId { get; set; }
    public string Reference { get; set; } = string.Empty;

    public DateOnly SignedOn { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public int NoticePeriodDays { get; set; } = 30;

    public Guid? CommissionPlanId { get; set; }
    public decimal? SecurityDeposit { get; set; }
    public string? DocumentUrl { get; set; }
    public Guid? SignatureSessionId { get; set; }
    public string? TerminationReason { get; set; }
}

public class PartnerDocument : BaseEntity
{
    public Guid ChannelPartnerId { get; set; }
    public ChannelPartner? Partner { get; set; }

    public string DocumentType { get; set; } = string.Empty;
    public string? Url { get; set; }
    public DocumentState State { get; set; } = DocumentState.Required;
    public bool IsMandatory { get; set; } = true;
    public DateOnly? ExpiresOn { get; set; }

    /// <summary>Expired mandatory documents stop new registrations rather than only warning.</summary>
    public bool BlocksActivityWhenExpired { get; set; }

    public Guid? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAt { get; set; }
}

/// <summary>Which projects a partner may sell, and on what rate card.</summary>
public class PartnerAuthorisation : BaseEntity
{
    public Guid ChannelPartnerId { get; set; }
    public ChannelPartner? Partner { get; set; }

    public Guid ProjectId { get; set; }
    public Guid? CommissionPlanId { get; set; }
    public Guid? TerritoryId { get; set; }

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    /// <summary>Cap on how much of the project one partner may take. Prevents a single dealer cornering stock.</summary>
    public int? MaxBookings { get; set; }

    public int BookingsMade { get; set; }
    public bool CanSeePrices { get; set; } = true;
    public bool CanHoldUnits { get; set; }
}

/// <summary>A performance band that changes the rate card and moves on volume.</summary>
public class PartnerTier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }

    public decimal MinBookingValue { get; set; }
    public int MinBookingCount { get; set; }

    /// <summary>Extra points on top of the base rate card for this tier.</summary>
    public decimal CommissionUpliftPercent { get; set; }

    public bool PriorityAllocation { get; set; }
    public int LeadValidityDays { get; set; } = 90;
    public string? Benefits { get; set; }
    public bool AutoPromote { get; set; } = true;
}

/// <summary>
/// A partner staking a claim on a prospect before anybody else does.
///
/// The mechanism that stops the payout-time argument: a duplicate is rejected the moment it is
/// registered and the loser is told immediately, rather than three months later when the booking
/// lands and two people claim it.
/// </summary>
public class LeadRegistration : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid ChannelPartnerId { get; set; }
    public Guid? PartnerUserId { get; set; }
    public Guid ProjectId { get; set; }

    public string ProspectName { get; set; } = string.Empty;
    public string ProspectPhone { get; set; } = string.Empty;
    public string? ProspectEmail { get; set; }
    public string? ProspectIdentityNumber { get; set; }

    public Guid? PartyId { get; set; }
    public Guid? EnquiryId { get; set; }

    public LeadRegistrationStatus Status { get; set; } = LeadRegistrationStatus.Registered;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    /// <summary>The registration that beat this one, when it was rejected as a duplicate.</summary>
    public Guid? ConflictsWithRegistrationId { get; set; }
    public string? RejectionReason { get; set; }

    public Guid? SiteVisitId { get; set; }
    public Guid? BookingId { get; set; }
    public DateOnly? ConvertedOn { get; set; }

    public bool ExpiryWarningSent { get; set; }
    public int ExtensionCount { get; set; }
    public string? Note { get; set; }
}

/// <summary>A partner's rate for one project, banded by volume.</summary>
public class PartnerCommissionRate : BaseEntity
{
    public Guid? ChannelPartnerId { get; set; }
    public Guid? PartnerTierId { get; set; }
    public Guid ProjectId { get; set; }

    public PropertySubType? SubType { get; set; }
    public decimal FromValue { get; set; }
    public decimal? ToValue { get; set; }

    public decimal CommissionPercent { get; set; }
    public decimal RatePerSqFt { get; set; }
    public decimal FlatAmount { get; set; }

    public CommissionTrigger Trigger { get; set; } = CommissionTrigger.OnCollection;

    /// <summary>Collection the customer must reach before this instalment of commission is released.</summary>
    public decimal ReleaseAtCollectionPercent { get; set; }

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
}

/// <summary>Commission earned by a partner on one booking, released in step with collection.</summary>
public class PartnerCommissionEntry : BaseEntity
{
    public Guid ChannelPartnerId { get; set; }
    public Guid BookingId { get; set; }
    public Guid? PartnerCommissionRateId { get; set; }
    public Guid? CommissionCalculationId { get; set; }

    public decimal BookingValue { get; set; }
    public decimal CommissionPercent { get; set; }
    public decimal GrossCommission { get; set; }

    public decimal CollectionPercent { get; set; }

    /// <summary>Gross scaled by collection. The number that stops a developer paying for a file that dies.</summary>
    public decimal EarnedAmount { get; set; }

    public decimal WithholdingAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal ClawedBackAmount { get; set; }

    public CommissionStatus Status { get; set; } = CommissionStatus.Accrued;
    public DateOnly AccruedOn { get; set; }
    public DateOnly? LastPaidOn { get; set; }
    public Guid? StatementId { get; set; }
    public string? Note { get; set; }
}

/// <summary>A partner-visible statement of everything earned, paid and pending in a period.</summary>
public class PartnerStatement : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid ChannelPartnerId { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly IssuedOn { get; set; }

    public decimal OpeningBalance { get; set; }
    public decimal CommissionEarned { get; set; }
    public decimal CommissionPaid { get; set; }
    public decimal WithholdingDeducted { get; set; }
    public decimal AdvanceRecovered { get; set; }
    public decimal ClawbackApplied { get; set; }
    public decimal ClosingBalance { get; set; }

    public int BookingCount { get; set; }
    public Guid? DocumentId { get; set; }
    public string? DocumentUrl { get; set; }
    public bool IsPublishedToPortal { get; set; }
    public bool IsAcknowledged { get; set; }
}

/// <summary>Money advanced to a partner before it is earned, and recovered from future commission.</summary>
public class PartnerAdvance : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid ChannelPartnerId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly AdvancedOn { get; set; }
    public string? Purpose { get; set; }

    /// <summary>Share of each future payout that goes to clearing this. 100 means recover it all at once.</summary>
    public decimal RecoveryPercent { get; set; } = 100m;

    public decimal RecoveredAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public DateOnly? FullyRecoveredOn { get; set; }

    public Guid? ApprovalRequestId { get; set; }
    public Guid? PaymentReference { get; set; }
    public bool IsWrittenOff { get; set; }
}

/// <summary>A time-boxed sales contest with a target, a prize and a live leaderboard.</summary>
public class PartnerContest : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }

    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }

    /// <summary>"BookingCount", "BookingValue", "CollectionValue", "SiteVisits".</summary>
    public string MetricKey { get; set; } = "BookingValue";

    public decimal TargetValue { get; set; }
    public string? PrizeDescription { get; set; }
    public decimal? PrizeAmount { get; set; }

    public bool IsPublished { get; set; }
    public bool IsClosed { get; set; }
    public Guid? WinnerPartnerId { get; set; }
    public string? Rules { get; set; }
}
