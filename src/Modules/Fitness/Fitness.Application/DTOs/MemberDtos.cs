using Fitness.Domain.Enums;

namespace Fitness.Application.DTOs;

/// <summary>
/// A member in a list. Deliberately narrow — a twenty-thousand-row list must not carry the whole
/// 360 for each row, and the columns here are the ones a receptionist searches and scans by.
/// </summary>
public class MemberSummaryDto
{
    public Guid Id { get; set; }
    public string MemberNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PreferredName { get; set; }
    public string? PhotoUrl { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }

    public MemberStatus Status { get; set; }
    public Guid HomeClubId { get; set; }
    public string? HomeClubName { get; set; }

    public string? PlanName { get; set; }
    public DateTime? JoinedOn { get; set; }
    public DateTime? NextBillingOn { get; set; }

    public decimal AccountBalance { get; set; }
    public DateTime? LastVisitOn { get; set; }
    public int DaysSinceLastVisit { get; set; }
    public int TotalVisits { get; set; }

    public ChurnRiskBand RiskBand { get; set; }
    public bool HasBlockingAlert { get; set; }
    public int AlertCount { get; set; }

    public bool IsBanned { get; set; }
    public bool IsCheckedIn { get; set; }
}

/// <summary>
/// The whole member record on one payload.
///
/// One call rather than nine because the desk opens this with a person standing in front of them:
/// nine round trips is nine chances to be slow, and the receptionist reads the alerts, the
/// balance and the plan in the same glance.
/// </summary>
public class MemberDetailDto
{
    public Guid Id { get; set; }
    public string MemberNumber { get; set; } = string.Empty;
    public Guid? ContactId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PreferredName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public int? Age { get; set; }
    public Gender Gender { get; set; }
    public string? NationalId { get; set; }
    public string? Occupation { get; set; }
    public string? PhotoUrl { get; set; }

    public string? Phone { get; set; }
    public string? AlternatePhone { get; set; }
    public string? Email { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? PostCode { get; set; }
    public string? CountryCode { get; set; }
    public string? PreferredLanguage { get; set; }
    public MessageChannel PreferredChannel { get; set; }

    public MemberStatus Status { get; set; }
    public Guid HomeClubId { get; set; }
    public string? HomeClubName { get; set; }
    public DateTime? JoinedOn { get; set; }
    public DateTime? FirstJoinedOn { get; set; }
    public DateTime? LeftOn { get; set; }
    public int TenureDays { get; set; }

    public Guid? HouseholdId { get; set; }
    public string? HouseholdName { get; set; }
    public Guid? CorporateAccountId { get; set; }
    public string? CorporateAccountName { get; set; }
    public Guid? AssignedCoachId { get; set; }
    public string? AssignedCoachName { get; set; }
    public Guid? ReferredByMemberId { get; set; }
    public string? ReferredByName { get; set; }
    public string? LeadSourceName { get; set; }

    // ── Money ────────────────────────────────────────────────────────────────

    public decimal AccountBalance { get; set; }
    public decimal CreditBalance { get; set; }
    public DateTime? NextBillingOn { get; set; }
    public decimal? NextBillingAmount { get; set; }
    public decimal LifetimeValue { get; set; }

    // ── Activity ─────────────────────────────────────────────────────────────

    public DateTime? LastVisitOn { get; set; }
    public int DaysSinceLastVisit { get; set; }
    public int TotalVisits { get; set; }
    public int VisitsThisMonth { get; set; }
    public decimal VisitFrequencyBaseline { get; set; }
    public int CurrentStreakDays { get; set; }
    public int LoyaltyPoints { get; set; }
    public string? LoyaltyTierName { get; set; }

    public ChurnRiskBand RiskBand { get; set; }
    public int RiskScore { get; set; }
    public List<string> RiskReasons { get; set; } = [];

    // ── Compliance ───────────────────────────────────────────────────────────

    public bool WaiverSigned { get; set; }
    public DateTime? WaiverSignedOn { get; set; }
    public bool WaiverCurrent { get; set; }
    public ClearanceStatus MedicalClearance { get; set; }
    public string? MedicalSummary { get; set; }

    public bool IsBanned { get; set; }
    public string? BanReason { get; set; }
    public DateTime? BanUntil { get; set; }

    public bool PhotoConsent { get; set; }
    public bool LeaderboardOptIn { get; set; }
    public bool IsAnonymised { get; set; }

    // ── Attached collections ─────────────────────────────────────────────────

    public List<AgreementSummaryDto> Agreements { get; set; } = [];
    public List<MemberAlertDto> Alerts { get; set; } = [];
    public List<EmergencyContactDto> EmergencyContacts { get; set; } = [];
    public List<MedicalFlagDto> MedicalFlags { get; set; } = [];
    public List<MemberCredentialDto> Credentials { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public List<SessionCreditSummaryDto> Credits { get; set; } = [];
    public List<HouseholdMemberDto> HouseholdMembers { get; set; } = [];

    /// <summary>The next few things in their diary — classes, PT, court bookings, all in one list.</summary>
    public List<UpcomingBookingDto> UpcomingBookings { get; set; } = [];

    public bool IsCheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
}

public class SaveMemberDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PreferredName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Gender Gender { get; set; } = Gender.Unspecified;
    public string? NationalId { get; set; }
    public string? Occupation { get; set; }
    public string? PhotoUrl { get; set; }

    public string? Phone { get; set; }
    public string? AlternatePhone { get; set; }
    public string? Email { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? PostCode { get; set; }
    public string? CountryCode { get; set; }
    public string? PreferredLanguage { get; set; }
    public MessageChannel PreferredChannel { get; set; } = MessageChannel.Email;

    public Guid HomeClubId { get; set; }
    public Guid? HouseholdId { get; set; }
    public Guid? CorporateAccountId { get; set; }
    public Guid? AssignedCoachId { get; set; }
    public Guid? LeadSourceId { get; set; }
    public Guid? ReferredByMemberId { get; set; }

    public string? MedicalSummary { get; set; }
    public bool PhotoConsent { get; set; }
    public bool LeaderboardOptIn { get; set; } = true;

    public List<EmergencyContactDto> EmergencyContacts { get; set; } = [];
    public List<string> Tags { get; set; } = [];
}

/// <summary>
/// The join wizard's payload: person, plan, paperwork and payment in one transaction.
///
/// One call because joining is one decision. Splitting it across four endpoints creates the
/// half-joined member — signed but unbilled, or billed but with no waiver — which every club has
/// a hundred of and nobody can explain.
/// </summary>
public class JoinMemberDto
{
    public SaveMemberDto Member { get; set; } = new();

    public Guid PlanId { get; set; }
    public Guid ClubId { get; set; }

    public DateTime StartsOn { get; set; }

    public Guid? PromotionRuleId { get; set; }
    public string? PromoCodeText { get; set; }

    public decimal? PriceOverride { get; set; }
    public string? PriceOverrideReason { get; set; }
    public bool WaiveJoiningFee { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Card;
    public Guid? PaymentMethodRefId { get; set; }

    /// <summary>Collect the first payment now rather than waiting for the next billing run.</summary>
    public bool TakeFirstPaymentNow { get; set; } = true;
    public decimal? AmountTendered { get; set; }

    public Guid? SoldByStaffId { get; set; }
    public Guid? LeadId { get; set; }

    // ── Paperwork ────────────────────────────────────────────────────────────

    public Guid? WaiverTemplateId { get; set; }
    public string? SignatureImageUrl { get; set; }
    public string? GuardianName { get; set; }
    public string? GuardianRelationship { get; set; }

    public HealthScreeningDto? HealthScreening { get; set; }

    // ── Access ───────────────────────────────────────────────────────────────

    public CredentialType? CredentialType { get; set; }
    public string? CredentialIdentifier { get; set; }

    public List<MemberConsentDto> Consents { get; set; } = [];
}

/// <summary>What the join produced, so the wizard can print, hand over a fob and move on.</summary>
public class JoinResultDto
{
    public Guid MemberId { get; set; }
    public string MemberNumber { get; set; } = string.Empty;
    public Guid AgreementId { get; set; }
    public string AgreementNumber { get; set; } = string.Empty;

    public Guid? InvoiceId { get; set; }
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal ChangeDue { get; set; }

    public DateTime FirstBillingOn { get; set; }
    public decimal RecurringAmount { get; set; }

    public Guid? CredentialId { get; set; }

    /// <summary>Anything still outstanding before they can actually train.</summary>
    public List<string> OutstandingRequirements { get; set; } = [];
}

// ── Attached records ─────────────────────────────────────────────────────────

public class EmergencyContactDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? AlternatePhone { get; set; }
    public string? Email { get; set; }
    public bool IsPrimary { get; set; }
}

public class MedicalFlagDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public bool VisibleToInstructors { get; set; }
    public DateTime? ReviewOn { get; set; }
    public DateTime? ResolvedOn { get; set; }
}

public class MemberNoteDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public InteractionKind Kind { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public bool IsPrivate { get; set; }
    public bool IsPinned { get; set; }
    public Guid? StaffId { get; set; }
    public string? AuthorName { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
}

public class MemberAlertDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public MemberAlertKind Kind { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ActionLabel { get; set; }
    public string? ActionRoute { get; set; }
    public bool BlocksAccess { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
}

public class MemberCredentialDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public CredentialType Type { get; set; }
    public string Identifier { get; set; } = string.Empty;
    public CredentialStatus Status { get; set; }
    public DateTime IssuedOn { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public DateTime? DeactivatedOn { get; set; }
    public string? DeactivationReason { get; set; }
    public decimal ReplacementFee { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

public class IssueCredentialDto
{
    public Guid MemberId { get; set; }
    public CredentialType Type { get; set; }
    public string Identifier { get; set; } = string.Empty;
    public DateTime? ExpiresOn { get; set; }

    /// <summary>Deactivates the credential being replaced and charges the fee, in one step.</summary>
    public Guid? ReplacesCredentialId { get; set; }
    public decimal ReplacementFee { get; set; }
    public bool ChargeReplacementFee { get; set; }
}

public class MemberConsentDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public MessageChannel Channel { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public bool Granted { get; set; }
    public DateTime DecidedAt { get; set; }
    public string? ConsentText { get; set; }
    public string? CapturedVia { get; set; }
}

public class MemberDocumentDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public DocumentKind Kind { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long SizeBytes { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public bool IsSensitive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MemberStatusHistoryDto
{
    public Guid Id { get; set; }
    public MemberStatus FromStatus { get; set; }
    public MemberStatus ToStatus { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Reason { get; set; }
    public string? ChangedByName { get; set; }
}

// ── Household ────────────────────────────────────────────────────────────────

public class HouseholdDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid PrimaryMemberId { get; set; }
    public string? PrimaryMemberName { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? PostCode { get; set; }
    public bool AnyAdultMayCheckInChildren { get; set; }

    public decimal CombinedBalance { get; set; }
    public List<HouseholdMemberDto> Members { get; set; } = [];
}

public class HouseholdMemberDto
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public MemberStatus MemberStatus { get; set; }
    public int? Age { get; set; }
    public HouseholdRole Role { get; set; }
    public bool MayCollectChildren { get; set; }
    public DateTime? AgesOutOn { get; set; }
    public bool AgesOutSoon { get; set; }
}

public class SaveHouseholdDto
{
    /// <summary>Null creates a household; set edits the one it names.</summary>
    public Guid? Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public Guid PrimaryMemberId { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? PostCode { get; set; }
    public bool AnyAdultMayCheckInChildren { get; set; } = true;
    public List<HouseholdMemberDto> Members { get; set; } = [];
}

// ── Cross-cutting little payloads ────────────────────────────────────────────

public class UpcomingBookingDto
{
    public Guid Id { get; set; }

    /// <summary>Class, appointment or resource — the member does not care which table it came from.</summary>
    public string BookingType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string? Location { get; set; }
    public string? StaffName { get; set; }
    public string? SpotLabel { get; set; }
    public BookingStatus Status { get; set; }
    public bool IsWaitlisted { get; set; }
    public int? WaitlistPosition { get; set; }
    public string Route { get; set; } = string.Empty;
}

public class SessionCreditSummaryDto
{
    public Guid Id { get; set; }
    public EntitlementKind Kind { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Remaining { get; set; }
    public int Held { get; set; }
    public int Granted { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public bool ExpiringSoon { get; set; }
}

public class ChangeMemberStatusDto
{
    public Guid MemberId { get; set; }
    public MemberStatus NewStatus { get; set; }
    public string? Reason { get; set; }
}

public class BanMemberDto
{
    public Guid MemberId { get; set; }
    public bool IsBanned { get; set; }
    public string? Reason { get; set; }
    public DateTime? Until { get; set; }
}

/// <summary>
/// Folding one duplicate member into another. Previewed before it is committed, because a merge
/// cannot be un-run and "same person joined twice" is usually only mostly true.
/// </summary>
public class MergeMembersDto
{
    /// <summary>The record that survives.</summary>
    public Guid KeepMemberId { get; set; }

    /// <summary>The record folded in and then closed.</summary>
    public Guid MergeMemberId { get; set; }

    /// <summary>Field-level choices, where the two records disagree.</summary>
    public Dictionary<string, string> FieldChoices { get; set; } = [];

    public bool PreviewOnly { get; set; } = true;
}

public class MergePreviewDto
{
    public Guid KeepMemberId { get; set; }
    public Guid MergeMemberId { get; set; }

    /// <summary>Fields where the records disagree, with both values, so a human decides.</summary>
    public List<MergeConflictDto> Conflicts { get; set; } = [];

    public int AgreementsMoved { get; set; }
    public int InvoicesMoved { get; set; }
    public int VisitsMoved { get; set; }
    public int BookingsMoved { get; set; }
    public decimal CombinedBalance { get; set; }

    public List<string> Warnings { get; set; } = [];
}

public class MergeConflictDto
{
    public string Field { get; set; } = string.Empty;
    public string? KeepValue { get; set; }
    public string? MergeValue { get; set; }
}

/// <summary>Everything held about a member, for a data-subject access request.</summary>
public class MemberExportDto
{
    public MemberDetailDto Member { get; set; } = new();
    public List<AgreementSummaryDto> Agreements { get; set; } = [];
    public List<InvoiceSummaryDto> Invoices { get; set; } = [];
    public List<MemberLedgerEntryDto> Ledger { get; set; } = [];
    public List<VisitHistoryDto> Visits { get; set; } = [];
    public List<MemberNoteDto> Notes { get; set; } = [];
    public List<MemberConsentDto> Consents { get; set; } = [];
    public List<MemberDocumentDto> Documents { get; set; } = [];
    public List<AssessmentDto> Assessments { get; set; } = [];
    public List<MemberStatusHistoryDto> StatusHistory { get; set; } = [];
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Erasure. Identity is replaced with a tombstone; what records law requires the club to keep —
/// financial transactions, incident reports — survives with the person's name removed.
/// </summary>
public class AnonymiseMemberDto
{
    public Guid MemberId { get; set; }
    public string Reason { get; set; } = string.Empty;

    /// <summary>Confirms the operator understands it cannot be undone.</summary>
    public bool ConfirmIrreversible { get; set; }
}
