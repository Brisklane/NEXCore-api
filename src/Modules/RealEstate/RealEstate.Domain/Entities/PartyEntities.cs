using Nexcore.SharedKernel;
using RealEstate.Domain.Enums;

namespace RealEstate.Domain.Entities;

/// <summary>
/// A person or organisation the business deals with, in any capacity.
///
/// One record, many roles. The same individual is routinely a buyer on one unit, the seller of
/// another, the landlord of a third and a tenant in a fourth — and if the app cannot show all four
/// on one screen it will be corrected by a customer who can.
///
/// Deliberately called a Party and not a Customer: "customer" begs the question of which side of
/// the transaction they are on, and in this business they are frequently on both.
/// </summary>
public class Party : BaseEntity
{
    public string Reference { get; set; } = string.Empty;
    public PartyKind Kind { get; set; } = PartyKind.Individual;

    // ── Individual ───────────────────────────────────────────────────────────

    public string? Salutation { get; set; }
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }

    /// <summary>
    /// Printed on South Asian allotment letters and transfer deeds as a matter of course, and used
    /// to distinguish two people with identical names. Not optional in those markets.
    /// </summary>
    public string? FatherOrGuardianName { get; set; }

    public string? DisplayName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? ResidencyStatus { get; set; }
    public string? Occupation { get; set; }
    public string? Employer { get; set; }
    public string? PhotoUrl { get; set; }
    public string? SignatureSpecimenUrl { get; set; }

    // ── Organisation ─────────────────────────────────────────────────────────

    public string? OrganisationName { get; set; }
    public string? TradingName { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TaxNumber { get; set; }
    public DateOnly? IncorporatedOn { get; set; }
    public string? Industry { get; set; }

    // ── Contact ──────────────────────────────────────────────────────────────

    public string? PrimaryPhone { get; set; }
    public string? PrimaryEmail { get; set; }
    public NotificationChannel PreferredChannel { get; set; } = NotificationChannel.WhatsApp;
    public string PreferredLanguage { get; set; } = "en";

    // ── Compliance ───────────────────────────────────────────────────────────

    public KycStatus KycStatus { get; set; } = KycStatus.NotStarted;
    public RiskRating RiskRating { get; set; } = RiskRating.Low;
    public DateOnly? KycVerifiedOn { get; set; }
    public DateOnly? KycExpiresOn { get; set; }

    /// <summary>Politically exposed person. Triggers enhanced due diligence, not refusal.</summary>
    public bool IsPoliticallyExposed { get; set; }

    /// <summary>Flagged for default, fraud or abuse. Surfaces an alert wherever they appear.</summary>
    public bool IsCautioned { get; set; }

    // ── Links out ────────────────────────────────────────────────────────────

    /// <summary>The CRM account, where CRM is installed, so campaign and case history stay shared.</summary>
    public Guid? CrmAccountId { get; set; }

    public Guid? OwnerAgentId { get; set; }
    public Guid? OfficeId { get; set; }
    public Guid? SourceEnquiryId { get; set; }

    // ── Money summary, maintained by the ledger so a 360 needs one read ──────

    public decimal TotalInvested { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalOutstanding { get; set; }
    public DateOnly? NextDueDate { get; set; }

    public string? Notes { get; set; }

    public ICollection<PartyRole> Roles { get; set; } = [];
    public ICollection<PartyContact> Contacts { get; set; } = [];
    public ICollection<PartyAddress> Addresses { get; set; } = [];
    public ICollection<PartyIdentity> Identities { get; set; } = [];
    public ICollection<PartyConsent> Consents { get; set; } = [];
}

/// <summary>
/// One hat a party wears. Additive and dated, so "was our tenant in 2019, is our buyer now" is a
/// fact the system holds rather than something an agent remembers.
/// </summary>
public class PartyRole : BaseEntity
{
    public Guid PartyId { get; set; }
    public Party? Party { get; set; }

    public PartyRoleKind Kind { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly? ToDate { get; set; }

    /// <summary>The booking, tenancy or instruction that gave them this role.</summary>
    public string? ContextType { get; set; }
    public Guid? ContextId { get; set; }
}

public class PartyContact : BaseEntity
{
    public Guid PartyId { get; set; }
    public Party? Party { get; set; }

    /// <summary>"Phone", "Mobile", "Email", "Fax", "Website", "SocialHandle".</summary>
    public string ContactType { get; set; } = "Phone";

    public string Value { get; set; } = string.Empty;
    public string? Label { get; set; }

    /// <summary>Drives whether the WhatsApp template path or the SMS path is used.</summary>
    public bool IsWhatsApp { get; set; }

    public bool IsPrimary { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }

    /// <summary>Bounced, disconnected or wrong number. Kept, so the app stops trying.</summary>
    public bool IsUnreachable { get; set; }

    /// <summary>For an organisation: whose desk this is.</summary>
    public string? PersonName { get; set; }
    public string? Designation { get; set; }
    public bool IsAuthorisedSignatory { get; set; }
}

/// <summary>
/// Correspondence, permanent and current addresses are genuinely different, and a demand letter
/// sent to the wrong one is a dispute the developer loses.
/// </summary>
public class PartyAddress : BaseEntity
{
    public Guid PartyId { get; set; }
    public Party? Party { get; set; }

    /// <summary>"Current", "Permanent", "Correspondence", "Office", "Overseas".</summary>
    public string AddressType { get; set; } = "Correspondence";

    public string? Line1 { get; set; }
    public string? Line2 { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostCode { get; set; }
    public string? CountryCode { get; set; }
    public Guid? GeoAreaId { get; set; }

    /// <summary>Where post is actually sent. Exactly one per party.</summary>
    public bool IsMailingAddress { get; set; }

    public bool IsVerified { get; set; }
}

public class PartyIdentity : BaseEntity
{
    public Guid PartyId { get; set; }
    public Party? Party { get; set; }

    public IdentityKind Kind { get; set; }

    /// <summary>Local name for it — "CNIC", "Aadhaar", "Emirates ID", "NTN", "PAN", "TRN".</summary>
    public string? LocalLabel { get; set; }

    public string Number { get; set; } = string.Empty;
    public string? IssuingCountry { get; set; }
    public string? IssuingAuthority { get; set; }
    public DateOnly? IssuedOn { get; set; }
    public DateOnly? ExpiresOn { get; set; }

    public string? FrontImageUrl { get; set; }
    public string? BackImageUrl { get; set; }

    public DocumentState State { get; set; } = DocumentState.Received;
    public Guid? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public bool IsPrimary { get; set; }
}

/// <summary>
/// The compliance file on one party. Separate from the identity documents because a party can be
/// re-screened many times over the years and each screening is its own event with its own verdict.
/// </summary>
public class KycCase : BaseEntity
{
    public Guid PartyId { get; set; }
    public string Reference { get; set; } = string.Empty;

    public KycStatus Status { get; set; } = KycStatus.NotStarted;
    public RiskRating RiskRating { get; set; } = RiskRating.Low;

    public DateOnly OpenedOn { get; set; }
    public DateOnly? CompletedOn { get; set; }
    public DateOnly? NextReviewDue { get; set; }

    public Guid? AssignedToUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }

    /// <summary>Reference to the screening provider's result. We record the outcome, not the list.</summary>
    public string? SanctionsScreeningRef { get; set; }
    public DateOnly? SanctionsScreenedOn { get; set; }
    public bool SanctionsHit { get; set; }

    /// <summary>Where the money came from. Required above a threshold in most regimes.</summary>
    public string? SourceOfFunds { get; set; }
    public decimal? DeclaredNetWorth { get; set; }

    public string? RejectionReason { get; set; }
    public string? Notes { get; set; }

    public ICollection<KycDocument> Documents { get; set; } = [];
}

public class KycDocument : BaseEntity
{
    public Guid KycCaseId { get; set; }
    public KycCase? Case { get; set; }

    public string DocumentType { get; set; } = string.Empty;
    public string? Url { get; set; }
    public DocumentState State { get; set; } = DocumentState.Required;
    public bool IsMandatory { get; set; } = true;
    public DateOnly? ExpiresOn { get; set; }
    public Guid? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? RejectionNote { get; set; }
}

/// <summary>
/// Spouse, co-applicant, nominee, guardian, power of attorney. Printed on documents and, in the
/// case of a POA, checked for validity and scope before a transfer is allowed to proceed.
/// </summary>
public class PartyRelationship : BaseEntity
{
    public Guid PartyId { get; set; }
    public Guid RelatedPartyId { get; set; }

    /// <summary>"Spouse", "Father", "Guardian", "CoApplicant", "Nominee", "Heir", "PowerOfAttorney", "Director".</summary>
    public string RelationshipType { get; set; } = string.Empty;

    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }

    // ── Power of attorney specifics ──────────────────────────────────────────

    /// <summary>What the POA actually permits. A general POA and a sale-specific POA are not the same.</summary>
    public string? PowerScope { get; set; }

    public string? PoaDocumentNumber { get; set; }
    public DateOnly? PoaValidFrom { get; set; }
    public DateOnly? PoaValidTo { get; set; }
    public bool PoaIsRegistered { get; set; }
    public string? DocumentUrl { get; set; }

    /// <summary>Nominee share, where several are named.</summary>
    public decimal? SharePercent { get; set; }

    public bool IsVerified { get; set; }
}

/// <summary>
/// Permission to contact, per channel, with when and how it was given. Every automated message in
/// the app checks this before it sends.
/// </summary>
public class PartyConsent : BaseEntity
{
    public Guid PartyId { get; set; }
    public Party? Party { get; set; }

    public NotificationChannel Channel { get; set; }

    /// <summary>"Marketing", "Transactional", "ThirdParty". Transactional consent is rarely withdrawable.</summary>
    public string Purpose { get; set; } = "Marketing";

    public bool IsGranted { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    /// <summary>How it was captured — "WebForm", "BookingForm", "Portal", "Verbal", "Import".</summary>
    public string? Source { get; set; }

    public DateTime? WithdrawnAt { get; set; }
    public string? EvidenceUrl { get; set; }
}

public class PartyPreference : BaseEntity
{
    public Guid PartyId { get; set; }
    public string PreferenceKey { get; set; } = string.Empty;
    public string? Value { get; set; }
}

/// <summary>
/// A party flagged for default, fraud or abuse — with the evidence, because a caution that cannot
/// be justified is a liability rather than a control.
/// </summary>
public class CautionListEntry : BaseEntity
{
    public Guid PartyId { get; set; }

    /// <summary>"Default", "ChequeFraud", "DocumentFraud", "Abuse", "Litigation", "SanctionsHit".</summary>
    public string Category { get; set; } = string.Empty;

    public AlertSeverity Severity { get; set; } = AlertSeverity.Warning;
    public string Reason { get; set; } = string.Empty;
    public string? EvidenceUrl { get; set; }

    public Guid RaisedByUserId { get; set; }
    public DateOnly RaisedOn { get; set; }
    public Guid? ApprovedByUserId { get; set; }

    public DateOnly? ExpiresOn { get; set; }
    public DateOnly? ClearedOn { get; set; }
    public Guid? ClearedByUserId { get; set; }

    /// <summary>Refuse new business outright rather than only warning about it.</summary>
    public bool BlocksNewBusiness { get; set; }

}
