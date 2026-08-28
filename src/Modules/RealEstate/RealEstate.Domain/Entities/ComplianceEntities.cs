using Nexcore.SharedKernel;
using RealEstate.Domain.Enums;

namespace RealEstate.Domain.Entities;

/// <summary>
/// A permission the project needs from an authority. Inward-facing — the NOCs we *issue* are
/// <see cref="NocIssuance"/> and behave completely differently.
/// </summary>
public class ApprovalRecord : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid? ProjectId { get; set; }
    public Guid? LandParcelId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? ClientBuildContractId { get; set; }

    public ApprovalKind Kind { get; set; }
    public ApprovalState State { get; set; } = ApprovalState.NotStarted;

    public string? Authority { get; set; }
    public string? ApplicationNumber { get; set; }
    public string? ApprovalNumber { get; set; }

    public DateOnly? AppliedOn { get; set; }
    public DateOnly? GrantedOn { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidUntil { get; set; }

    public decimal ApplicationFee { get; set; }
    public decimal OtherCharges { get; set; }
    public decimal TotalCost { get; set; }

    public Guid? OwnerUserId { get; set; }
    public Guid? LiaisonPartyId { get; set; }
    public string? Conditions { get; set; }
    public string? QueryRaised { get; set; }
    public DateOnly? QueryResponseDue { get; set; }
    public string? RejectionReason { get; set; }
    public string? DocumentUrl { get; set; }

    /// <summary>
    /// Which project milestone this approval gates. Without the link, a delayed fire NOC surprises
    /// everybody at the end instead of visibly moving the completion date the day it slips.
    /// </summary>
    public Guid? BlocksMilestoneId { get; set; }
    public bool IsBlocking { get; set; }

    public int AlertDaysBefore { get; set; } = 60;
    public bool RenewalAlertSent { get; set; }
    public bool IsMandatory { get; set; } = true;

    public ICollection<ApprovalRenewal> Renewals { get; set; } = [];
}

public class ApprovalRenewal : BaseEntity
{
    public Guid ApprovalRecordId { get; set; }
    public ApprovalRecord? Approval { get; set; }

    public DateOnly AppliedOn { get; set; }
    public DateOnly? GrantedOn { get; set; }
    public DateOnly? NewValidUntil { get; set; }
    public decimal Fee { get; set; }
    public ApprovalState State { get; set; } = ApprovalState.Submitted;
    public string? DocumentUrl { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// A No Objection Certificate we issue to a customer — for a transfer, a mortgage, a connection,
/// possession, or building on a plot.
///
/// Numbered, conditional, time-limited, verifiable by reference and revocable. The dues check is
/// what gives it teeth: an NOC issued to somebody who owes money is a control that has failed.
/// </summary>
public class NocIssuance : BaseEntity
{
    public string NocNumber { get; set; } = string.Empty;

    public NocKind Kind { get; set; }
    public NocStatus Status { get; set; } = NocStatus.Requested;

    public Guid? ProjectId { get; set; }
    public Guid? SocietyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid PartyId { get; set; }

    public Guid? TransferRequestId { get; set; }
    public Guid? BuildingPlanApplicationId { get; set; }
    public Guid? CustomerMortgageId { get; set; }

    public DateOnly RequestedOn { get; set; }
    public DateOnly? IssuedOn { get; set; }
    public DateOnly? ValidUntil { get; set; }

    // ── The gate ─────────────────────────────────────────────────────────────

    public Guid? DuesClearanceId { get; set; }
    public bool DuesCleared { get; set; }
    public decimal OutstandingAtIssue { get; set; }
    public Guid? DuesOverrideApprovalId { get; set; }

    public decimal Fee { get; set; }
    public bool FeePaid { get; set; }
    public Guid? ReceiptId { get; set; }

    public Guid? IssuedByUserId { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public Guid? GeneratedDocumentId { get; set; }
    public string? DocumentUrl { get; set; }

    /// <summary>Printed on the certificate so a third party can confirm it is genuine.</summary>
    public string? VerificationCode { get; set; }

    public string? AddressedTo { get; set; }
    public string? Purpose { get; set; }
    public string? RejectionReason { get; set; }

    public bool IsRevoked { get; set; }
    public DateOnly? RevokedOn { get; set; }
    public string? RevocationReason { get; set; }

    public ICollection<NocCondition> Conditions { get; set; } = [];
}

public class NocCondition : BaseEntity
{
    public Guid NocIssuanceId { get; set; }
    public NocIssuance? Noc { get; set; }

    public string Condition { get; set; } = string.Empty;
    public DateOnly? ComplyByDate { get; set; }
    public bool IsSatisfied { get; set; }
    public DateOnly? SatisfiedOn { get; set; }

    /// <summary>Failing this one revokes the NOC rather than merely being noted.</summary>
    public bool BreachRevokesNoc { get; set; }

    public int SortOrder { get; set; }
}

/// <summary>The firm's own licences and registrations, with the expiry that stops it trading.</summary>
public class LicenceRecord : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    /// <summary>"AgencyRegistration", "BrokerLicence", "AgentRegistration", "ContractorLicence",
    /// "TradeLicence", "ProfessionalIndemnity", "ClientMoneyProtection".</summary>
    public string LicenceType { get; set; } = string.Empty;

    public string LicenceNumber { get; set; } = string.Empty;
    public string? Authority { get; set; }
    public Guid? OfficeId { get; set; }
    public Guid? AgentProfileId { get; set; }

    public DateOnly IssuedOn { get; set; }
    public DateOnly ExpiresOn { get; set; }
    public decimal RenewalFee { get; set; }
    public int AlertDaysBefore { get; set; } = 60;
    public bool RenewalAlertSent { get; set; }

    /// <summary>Trading without it is unlawful in this market, so an expiry is not a soft warning.</summary>
    public bool IsMandatoryToTrade { get; set; }

    public string? DocumentUrl { get; set; }
    public bool IsCurrent { get; set; } = true;
}

/// <summary>
/// One calendar of every statutory date across projects, properties and the firm. Built so nobody
/// has to remember which of eleven registers a date lives in.
/// </summary>
public class ComplianceCalendarEntry : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    /// <summary>"Approval", "Licence", "Certificate", "Filing", "Audit", "Escrow", "Tax", "Insurance".</summary>
    public string Category { get; set; } = string.Empty;

    public Guid? ProjectId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? OfficeId { get; set; }
    public Guid? SocietyId { get; set; }

    public Guid? ApprovalRecordId { get; set; }
    public Guid? LicenceRecordId { get; set; }
    public Guid? ComplianceCertificateId { get; set; }
    public Guid? RegulatoryFilingId { get; set; }

    public DateOnly DueDate { get; set; }

    /// <summary>"Monthly", "Quarterly", "Annual", "OneOff".</summary>
    public string? Recurrence { get; set; }

    public Guid? OwnerUserId { get; set; }
    public AlertSeverity Severity { get; set; } = AlertSeverity.Warning;
    public int AlertDaysBefore { get; set; } = 30;
    public bool AlertSent { get; set; }

    public bool IsCompleted { get; set; }
    public DateOnly? CompletedOn { get; set; }
    public string? EvidenceUrl { get; set; }
    public bool IsOverdue { get; set; }

    /// <summary>Penalty for missing it. Concentrates the mind better than a red dot.</summary>
    public decimal? PenaltyIfMissed { get; set; }
}

/// <summary>Something filed with a regulator, archived exactly as it was submitted.</summary>
public class RegulatoryFiling : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    /// <summary>"QuarterlyProgress", "AnnualReturn", "EscrowAudit", "TaxReturn",
    /// "ClientMoneyReport", "AntiMoneyLaunderingReport".</summary>
    public string FilingType { get; set; } = string.Empty;

    public Guid? ProjectId { get; set; }
    public Guid? OfficeId { get; set; }
    public string? Authority { get; set; }

    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly DueOn { get; set; }
    public DateOnly? FiledOn { get; set; }

    /// <summary>"Draft", "Prepared", "Reviewed", "Filed", "Accepted", "Queried", "Rejected", "Overdue".</summary>
    public string Status { get; set; } = "Draft";

    public int Version { get; set; } = 1;
    public string? AcknowledgementNumber { get; set; }
    public Guid? PreparedByUserId { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public string? DocumentUrl { get; set; }
    public string? QueryFromAuthority { get; set; }
    public decimal? LateFilingPenalty { get; set; }
}

/// <summary>
/// The periodic progress report a developer must file. Generated from live data rather than
/// assembled by hand, versioned, and archived as filed — the whole point is that it agrees with
/// the system it was drawn from.
/// </summary>
public class QuarterlyProgressReport : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid ProjectId { get; set; }
    public Guid? RegulatoryFilingId { get; set; }

    public int Year { get; set; }
    public int Quarter { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }

    // ── Sales ────────────────────────────────────────────────────────────────

    public int TotalUnits { get; set; }
    public int UnitsBooked { get; set; }
    public int UnitsBookedThisQuarter { get; set; }
    public decimal AreaBookedSqFt { get; set; }
    public decimal TotalBookingValue { get; set; }

    // ── Money ────────────────────────────────────────────────────────────────

    public decimal AmountCollected { get; set; }
    public decimal AmountCollectedThisQuarter { get; set; }
    public decimal AmountDepositedToEscrow { get; set; }
    public decimal AmountWithdrawnFromEscrow { get; set; }
    public decimal EscrowBalance { get; set; }
    public decimal AmountSpentOnConstruction { get; set; }
    public decimal AmountSpentOnLand { get; set; }

    // ── Physical ─────────────────────────────────────────────────────────────

    public decimal PhysicalProgressPercent { get; set; }
    public decimal FinancialProgressPercent { get; set; }

    public DateOnly? OriginalCompletionDate { get; set; }
    public DateOnly? RevisedCompletionDate { get; set; }
    public string? DelayReason { get; set; }

    public int ApprovalsObtained { get; set; }
    public int ApprovalsPending { get; set; }

    public Guid? CertifiedByEngineerUserId { get; set; }
    public string? ArchitectCertificateUrl { get; set; }
    public string? CaCertificateUrl { get; set; }

    public bool IsFiled { get; set; }
    public string? DocumentUrl { get; set; }

    public ICollection<QprLine> Lines { get; set; } = [];
}

/// <summary>Per-building progress inside a quarterly report, since towers finish at different rates.</summary>
public class QprLine : BaseEntity
{
    public Guid QuarterlyProgressReportId { get; set; }
    public QuarterlyProgressReport? Report { get; set; }

    public Guid? ProjectNodeId { get; set; }
    public string BuildingName { get; set; } = string.Empty;

    public int UnitCount { get; set; }
    public int BookedCount { get; set; }
    public decimal ProgressPercent { get; set; }
    public string? CurrentStage { get; set; }
    public DateOnly? ExpectedCompletion { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// A versioned document template with merge fields. Every printed document in the app comes from
/// one, so the exact wording a customer signed can be reproduced clause for clause years later.
/// </summary>
public class DocumentTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>"BookingForm", "AllotmentLetter", "SaleAgreement", "DemandNotice", "Receipt",
    /// "Statement", "Reminder", "LegalNotice", "Noc", "TransferDeed", "PossessionOffer",
    /// "PossessionCertificate", "TenancyAgreement", "NoticeToQuit", "OwnerStatement", "Ipc",
    /// "WorkOrder", "VariationOrder", "CommissionDisbursement", "MaintenanceBill".</summary>
    public string DocumentType { get; set; } = string.Empty;

    public Guid? ProjectId { get; set; }
    public Guid? OfficeId { get; set; }
    public string LanguageCode { get; set; } = "en";

    /// <summary>Arabic and Urdu are core markets, so direction is a first-class property.</summary>
    public bool IsRightToLeft { get; set; }

    public int CurrentVersion { get; set; } = 1;
    public bool IsDefault { get; set; }

    public string? LetterheadUrl { get; set; }
    public bool IncludeQrVerification { get; set; } = true;
    public bool IncludeAmountInWords { get; set; } = true;
    public string? PaperSize { get; set; } = "A4";

    public ICollection<TemplateVersion> Versions { get; set; } = [];
}

public class TemplateVersion : BaseEntity
{
    public Guid DocumentTemplateId { get; set; }
    public DocumentTemplate? Template { get; set; }

    public int Version { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? HeaderHtml { get; set; }
    public string? FooterHtml { get; set; }
    public string? StyleCss { get; set; }

    /// <summary>Declared merge fields, validated before a send so nobody receives "Dear {{name}}".</summary>
    public string? MergeFieldsJson { get; set; }

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public Guid? CreatedByUserIdRef { get; set; }
    public bool IsPublished { get; set; }
    public string? ChangeNote { get; set; }
}

/// <summary>A reusable clause, snapshotted into an agreement rather than referenced from it.</summary>
public class ClauseLibraryItem : BaseEntity
{
    public string ClauseKey { get; set; } = string.Empty;
    public string Heading { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    /// <summary>"Payment", "Possession", "Cancellation", "Transfer", "Maintenance", "Dispute",
    /// "ForceMajeure", "Warranty", "Indemnity".</summary>
    public string Category { get; set; } = string.Empty;

    public string LanguageCode { get; set; } = "en";
    public int Version { get; set; } = 1;

    /// <summary>Appears in every agreement unless deliberately removed with an approval.</summary>
    public bool IsMandatory { get; set; }

    public bool IsNegotiable { get; set; } = true;
    public Guid? ProjectId { get; set; }
    public Guid? LegalApprovalRequestId { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>A rendered document, archived exactly as it went out.</summary>
public class GeneratedDocument : BaseEntity
{
    public string DocumentNumber { get; set; } = string.Empty;

    public Guid? DocumentTemplateId { get; set; }
    public Guid? TemplateVersionId { get; set; }
    public string DocumentType { get; set; } = string.Empty;

    public Guid? PartyId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? DealId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? ClientBuildContractId { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public Guid? GeneratedByUserId { get; set; }

    public string? Url { get; set; }
    public string? Title { get; set; }
    public string LanguageCode { get; set; } = "en";
    public long? FileSizeBytes { get; set; }
    public int? PageCount { get; set; }

    /// <summary>Hash of the rendered file, so tampering with an archived document is detectable.</summary>
    public string? ContentHash { get; set; }

    public string? VerificationCode { get; set; }

    public bool IsSent { get; set; }
    public DateTime? SentAt { get; set; }
    public NotificationChannel? SentVia { get; set; }
    public bool IsSigned { get; set; }
    public Guid? SignatureSessionId { get; set; }
    public bool IsSuperseded { get; set; }
    public Guid? SupersededByDocumentId { get; set; }
}

/// <summary>
/// A signing ceremony. Supports electronic signing, and equally the wet signature and thumb
/// impression that most of this market still runs on — pretending otherwise would make the app
/// unusable in its main markets.
/// </summary>
public class SignatureSession : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid? GeneratedDocumentId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }

    public SignatureMethod Method { get; set; } = SignatureMethod.Electronic;
    public SignatureState State { get; set; } = SignatureState.Pending;

    public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
    public Guid? InitiatedByUserId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>Signers must sign in order, where the document requires it.</summary>
    public bool IsSequential { get; set; }

    public string? ProviderReference { get; set; }
    public string? AuditCertificateUrl { get; set; }
    public string? SignedDocumentUrl { get; set; }
    public string? DeclineReason { get; set; }

    public ICollection<SignatureParty> Parties { get; set; } = [];
}

public class SignatureParty : BaseEntity
{
    public Guid SignatureSessionId { get; set; }
    public SignatureSession? Session { get; set; }

    public Guid? PartyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }

    /// <summary>"Signer", "Witness", "Approver", "Viewer".</summary>
    public string Role { get; set; } = "Signer";

    public int SigningOrder { get; set; }
    public SignatureState State { get; set; } = SignatureState.Pending;

    public DateTime? SentAt { get; set; }
    public DateTime? ViewedAt { get; set; }
    public DateTime? SignedAt { get; set; }

    public string? SignatureImageUrl { get; set; }
    public string? ThumbImpressionUrl { get; set; }
    public string? PhotoUrl { get; set; }

    public bool OtpVerified { get; set; }
    public string? IpAddress { get; set; }
    public string? DeviceInfo { get; set; }
    public string? DeclineReason { get; set; }
}

/// <summary>
/// Where the paper file physically is.
///
/// Unglamorous and essential: in these markets the hard file is still the legal artefact, and a
/// firm that cannot produce it on demand has a problem no digital archive solves.
/// </summary>
public class PhysicalFile : BaseEntity
{
    public string FileNumber { get; set; } = string.Empty;

    public Guid? BookingId { get; set; }
    public Guid? PlotFileId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? PartyId { get; set; }
    public Guid? ProjectId { get; set; }

    public PhysicalFileState State { get; set; } = PhysicalFileState.InRecordRoom;

    public string? RoomLocation { get; set; }
    public string? Rack { get; set; }
    public string? Cabinet { get; set; }
    public string? Shelf { get; set; }
    public string? BarcodeOrRfid { get; set; }

    public Guid? IssuedToUserId { get; set; }
    public string? IssuedToName { get; set; }
    public DateOnly? IssuedOn { get; set; }
    public DateOnly? DueBackOn { get; set; }
    public bool IsOverdue { get; set; }

    public int DocumentCount { get; set; }
    public DateOnly? LastAuditedOn { get; set; }
    public bool IsMissing { get; set; }
    public DateOnly? ReportedMissingOn { get; set; }
    public string? Note { get; set; }

    public ICollection<PhysicalFileMovement> Movements { get; set; } = [];
}

public class PhysicalFileMovement : BaseEntity
{
    public Guid PhysicalFileId { get; set; }
    public PhysicalFile? File { get; set; }

    /// <summary>"Issued", "Returned", "Transferred", "Archived", "ReleasedToOwner", "ReportedMissing", "Found".</summary>
    public string Movement { get; set; } = "Issued";

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Guid? FromUserId { get; set; }
    public Guid? ToUserId { get; set; }
    public string? ToName { get; set; }
    public string? Purpose { get; set; }
    public DateOnly? DueBackOn { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public string? SignatureUrl { get; set; }
    public string? Note { get; set; }
}

/// <summary>A dispute, flagged wherever the unit, person or contract it concerns appears.</summary>
public class LegalCase : BaseEntity
{
    public string Reference { get; set; } = string.Empty;
    public string? CaseNumber { get; set; }

    /// <summary>"Title", "Possession", "Recovery", "Cancellation", "Consumer", "Labour",
    /// "Contract", "Tax", "Regulatory", "Eviction".</summary>
    public string CaseType { get; set; } = string.Empty;

    public LegalCaseStatus Status { get; set; } = LegalCaseStatus.Filed;

    public Guid? ProjectId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? LandParcelId { get; set; }
    public Guid? SubcontractId { get; set; }
    public Guid? PartyId { get; set; }

    /// <summary>"Plaintiff", "Defendant", "Respondent", "Intervenor".</summary>
    public string OurRole { get; set; } = "Defendant";

    public string? OpposingParty { get; set; }
    public string? Court { get; set; }
    public string? Jurisdiction { get; set; }

    public DateOnly FiledOn { get; set; }
    public DateOnly? NextHearingDate { get; set; }
    public DateOnly? DecidedOn { get; set; }

    public decimal? ClaimAmount { get; set; }

    /// <summary>Our honest estimate of what this could cost. Provided for where material.</summary>
    public decimal? ExposureAmount { get; set; }

    public decimal LegalCostsIncurred { get; set; }

    public Guid? AdvocatePartyId { get; set; }
    public string? AdvocateName { get; set; }
    public string? AdvocateContact { get; set; }
    public Guid? OwnerUserId { get; set; }

    /// <summary>Blocks transfer, sale and possession on whatever it is attached to.</summary>
    public bool BlocksTransaction { get; set; } = true;

    public string? Outcome { get; set; }
    public string? Summary { get; set; }
    public bool IsClosed { get; set; }

    public ICollection<LegalHearing> Hearings { get; set; } = [];
}

public class LegalHearing : BaseEntity
{
    public Guid LegalCaseId { get; set; }
    public LegalCase? Case { get; set; }

    public DateOnly HearingDate { get; set; }
    public string? Purpose { get; set; }
    public bool Attended { get; set; }
    public Guid? AttendedByUserId { get; set; }

    public string? Outcome { get; set; }
    public DateOnly? NextDate { get; set; }
    public string? NextPurpose { get; set; }
    public string? OrderSummary { get; set; }
    public string? OrderDocumentUrl { get; set; }
    public decimal? CostIncurred { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// The list of documents a process requires. Blocking by construction: a booking, transfer or
/// tenancy cannot proceed while a mandatory item is missing.
/// </summary>
public class DocumentChecklist : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>"Booking", "Transfer", "Possession", "Tenancy", "Kyc", "PartnerOnboarding",
    /// "LandAcquisition", "ClientBuild", "Noc".</summary>
    public string ProcessKey { get; set; } = string.Empty;

    public Guid? ProjectId { get; set; }
    public Guid? OfficeId { get; set; }

    /// <summary>The instance's subject — the booking, the transfer, the tenancy this list belongs to.</summary>
    public Guid? EntityId { get; set; }

    /// <summary>A reusable definition rather than a live checklist against one transaction.</summary>
    public bool IsTemplate { get; set; }

    public int RequiredCount { get; set; }
    public int ReceivedCount { get; set; }
    public int VerifiedCount { get; set; }
    public bool IsComplete { get; set; }

    /// <summary>Refuse to complete the process while mandatory items are outstanding.</summary>
    public bool BlocksProcess { get; set; } = true;

    public ICollection<DocumentChecklistItem> Items { get; set; } = [];
}

public class DocumentChecklistItem : BaseEntity
{
    public Guid DocumentChecklistId { get; set; }
    public DocumentChecklist? Checklist { get; set; }

    public string DocumentType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsMandatory { get; set; } = true;
    public DocumentState State { get; set; } = DocumentState.Required;

    public string? Url { get; set; }
    public Guid? PartyId { get; set; }
    public DateOnly? ReceivedOn { get; set; }
    public DateOnly? ExpiresOn { get; set; }

    public Guid? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? RejectionNote { get; set; }

    public Guid? WaiverApprovalRequestId { get; set; }
    public string? WaiverReason { get; set; }
    public int SortOrder { get; set; }
}
