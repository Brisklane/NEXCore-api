using Nexcore.SharedKernel;
using RealEstate.Domain.Enums;

namespace RealEstate.Domain.Entities;

/// <summary>
/// A booking ending badly. Customer-initiated and developer-initiated cancellations run different
/// workflows with different notice requirements, so the trigger is on the record rather than
/// implied by who clicked the button.
/// </summary>
public class Cancellation : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid BookingId { get; set; }
    public Guid PartyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? PlotFileId { get; set; }

    public CancellationTrigger Trigger { get; set; } = CancellationTrigger.CustomerWithdrawal;
    public Guid ReasonCodeId { get; set; }
    public string? Note { get; set; }

    public DateOnly RequestedOn { get; set; }
    public DateOnly? EffectiveOn { get; set; }

    /// <summary>The notice that had to run before a default cancellation could complete.</summary>
    public Guid? LegalNoticeId { get; set; }

    // ── The arithmetic, all stored so the customer can be shown the working ──

    public decimal TotalConsideration { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal SurchargeOutstanding { get; set; }
    public Guid? DeductionPolicyId { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal AdministrativeCharge { get; set; }
    public decimal CommissionClawback { get; set; }

    /// <summary>Paid less everything deducted. Can be zero, and occasionally is.</summary>
    public decimal RefundableAmount { get; set; }

    public Guid RequestedByUserId { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public ApprovalOutcome Outcome { get; set; } = ApprovalOutcome.Pending;
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public Guid? RefundRequestId { get; set; }

    /// <summary>Back on the board at today's price list, not the one it was sold on.</summary>
    public bool UnitReleased { get; set; }
    public DateOnly? UnitReleasedOn { get; set; }

    public bool CustomerAcknowledged { get; set; }
    public Guid? JournalEntryId { get; set; }

    public ICollection<DeductionLine> Deductions { get; set; } = [];
}

/// <summary>
/// How much is kept when a booking cancels. A slab schedule, because every developer's terms say
/// something different depending on how far into the plan the customer got.
/// </summary>
public class DeductionPolicy : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }

    public DeductionBasis Basis { get; set; } = DeductionBasis.SlabByElapsed;
    public decimal FlatPercent { get; set; }
    public decimal FlatAmount { get; set; }
    public decimal AdministrativeCharge { get; set; }

    /// <summary>Surcharge already accrued is kept as well as the deduction. Common, and contested.</summary>
    public bool ForfeitAccruedSurcharge { get; set; } = true;

    /// <summary>Take back what the dealer was paid on a booking that has now cancelled.</summary>
    public bool ClawBackCommission { get; set; } = true;

    /// <summary>Refunds only fund once the unit resells. A real term, and it must be modelled, not hidden.</summary>
    public bool RefundOnlyAfterResale { get; set; }

    public int RefundInstalmentCount { get; set; } = 1;

    public ICollection<DeductionSlab> Slabs { get; set; } = [];
}

/// <summary>One band of the deduction schedule.</summary>
public class DeductionSlab : BaseEntity
{
    public Guid DeductionPolicyId { get; set; }
    public DeductionPolicy? Policy { get; set; }

    /// <summary>Months since booking this slab starts at.</summary>
    public int FromMonth { get; set; }
    public int? ToMonth { get; set; }

    /// <summary>Or express the band as how much has been paid, which some terms do instead.</summary>
    public decimal? FromPaidPercent { get; set; }
    public decimal? ToPaidPercent { get; set; }

    public decimal DeductionPercent { get; set; }
    public decimal DeductionAmount { get; set; }

    /// <summary>Whether the percentage is of the sale price or of what was actually paid.</summary>
    public bool AppliesToPaidAmount { get; set; } = true;

    public int SortOrder { get; set; }
}

public class DeductionLine : BaseEntity
{
    public Guid CancellationId { get; set; }
    public Cancellation? Cancellation { get; set; }

    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    /// <summary>The slab or rule that produced it, so the number is defensible.</summary>
    public string? Basis { get; set; }

    public bool IsWaived { get; set; }
    public Guid? WaiverApprovalRequestId { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// Money going back. Frequently paid in instalments, and frequently only after the unit resells —
/// both of which are terms, not workarounds, so both are modelled.
/// </summary>
public class RefundRequest : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid PartyId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? CancellationId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? TokenReservationId { get; set; }

    public RefundStatus Status { get; set; } = RefundStatus.Requested;
    public decimal RequestedAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string CurrencyCode { get; set; } = AppConstants.DefaultCurrency;

    public DateOnly RequestedOn { get; set; }
    public Guid RequestedByUserId { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Where it goes. Verified against the payer, because refund fraud is real.
    public string? PayeeName { get; set; }
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public bool BankDetailsVerified { get; set; }

    /// <summary>Blocks payment until the unit resells, where the terms say so.</summary>
    public bool AwaitingResale { get; set; }
    public Guid? ResaleBookingId { get; set; }

    /// <summary>Second sign-off above the dual-control threshold.</summary>
    public Guid? SecondApproverUserId { get; set; }

    public string? RejectionReason { get; set; }

    public ICollection<RefundSchedule> Schedule { get; set; } = [];
}

public class RefundSchedule : BaseEntity
{
    public Guid RefundRequestId { get; set; }
    public RefundRequest? Request { get; set; }

    public int SequenceNumber { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateOnly? PaidOn { get; set; }
    public PaymentInstrument? Instrument { get; set; }
    public string? PaymentReference { get; set; }
    public Guid? JournalEntryId { get; set; }
    public bool IsPaid { get; set; }
}

/// <summary>
/// The customer selling their booked unit on before possession, mediated by the developer.
/// Chained to a new booking rather than editing the old one, so both halves keep their history.
/// </summary>
public class ResaleRequest : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid BookingId { get; set; }
    public Guid SellerPartyId { get; set; }
    public Guid? BuyerPartyId { get; set; }

    public DateOnly RequestedOn { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal ResalePrice { get; set; }

    /// <summary>Resale less original. Taxable in several markets, so it is computed and kept.</summary>
    public decimal GainAmount { get; set; }

    public decimal ResaleFee { get; set; }
    public decimal WithholdingTax { get; set; }

    public Guid? TransferRequestId { get; set; }
    public Guid? NewBookingId { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public TransferStatus Status { get; set; } = TransferStatus.Requested;
    public string? Note { get; set; }
}

/// <summary>
/// A booking changing hands. The defining transaction of the South Asian plot market, and modelled
/// as the multi-gate process it actually is: dues clearance, NOC, fees, a witnessed session, and an
/// ownership chain entry that is never edited afterwards.
/// </summary>
public class TransferRequest : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid? BookingId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? PlotFileId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid ProjectId { get; set; }

    public TransferKind Kind { get; set; } = TransferKind.Sale;
    public TransferStatus Status { get; set; } = TransferStatus.Requested;

    public DateOnly RequestedOn { get; set; }
    public Guid RequestedByPartyId { get; set; }
    public Guid? HandledByUserId { get; set; }

    public decimal? SaleConsideration { get; set; }

    // ── Gate 1: dues ─────────────────────────────────────────────────────────

    public Guid? DuesClearanceId { get; set; }
    public decimal OutstandingAtRequest { get; set; }
    public bool DuesCleared { get; set; }

    /// <summary>Proceeding with dues outstanding needs an authority, and that authority is recorded.</summary>
    public Guid? DuesOverrideApprovalId { get; set; }

    // ── Gate 2: NOC ──────────────────────────────────────────────────────────

    public Guid? NocIssuanceId { get; set; }

    // ── Gate 3: fees ─────────────────────────────────────────────────────────

    public decimal TotalTransferFee { get; set; }
    public decimal FeesPaid { get; set; }
    public Guid? FeeReceiptId { get; set; }

    // ── Gate 4: the session ──────────────────────────────────────────────────

    public Guid? TransferSessionId { get; set; }
    public DateOnly? CompletedOn { get; set; }

    /// <summary>The new booking opened in the buyer's name, carrying the remaining schedule across.</summary>
    public Guid? NewBookingId { get; set; }
    public Guid? NewAllotmentId { get; set; }

    // ── Special cases ────────────────────────────────────────────────────────

    /// <summary>Succession certificate reference for an inheritance transfer.</summary>
    public string? SuccessionCertificateNumber { get; set; }

    public Guid? PowerOfAttorneyRelationshipId { get; set; }
    public string? CourtDecreeReference { get; set; }
    public decimal? ShareTransferredPercent { get; set; }

    /// <summary>Litigation blocks a transfer outright unless an authority overrides it.</summary>
    public bool BlockedByLitigation { get; set; }
    public Guid? LitigationOverrideApprovalId { get; set; }

    public Guid? RejectReasonCodeId { get; set; }
    public string? Note { get; set; }

    public ICollection<TransferParty> Parties { get; set; } = [];
    public ICollection<TransferFeeLine> FeeLines { get; set; } = [];
}

public class TransferParty : BaseEntity
{
    public Guid TransferRequestId { get; set; }
    public TransferRequest? Request { get; set; }

    public Guid PartyId { get; set; }

    /// <summary>"Transferor", "Transferee", "Attorney", "Heir", "Guardian".</summary>
    public string Side { get; set; } = "Transferee";

    public decimal SharePercent { get; set; }
    public KycStatus KycStatus { get; set; } = KycStatus.NotStarted;
    public bool IdentityVerified { get; set; }
    public bool IsPresent { get; set; }
    public string? PhotoUrl { get; set; }
    public string? SignatureUrl { get; set; }
    public string? ThumbImpressionUrl { get; set; }
}

public class TransferFeeLine : BaseEntity
{
    public Guid TransferRequestId { get; set; }
    public TransferRequest? Request { get; set; }

    /// <summary>"TransferFee", "MembershipChange", "GovernmentLevy", "WithholdingTax",
    /// "DocumentationCharge", "NocFee", "LateTransferPenalty".</summary>
    public string FeeType { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
    public ChargeBasis Basis { get; set; } = ChargeBasis.Fixed;
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Some fees fall on the seller, some on the buyer, and getting it wrong holds up the session.</summary>
    public string PayableBy { get; set; } = "Transferee";

    public bool IsPaid { get; set; }
    public Guid? ReceiptId { get; set; }
    public bool IsWaived { get; set; }
    public Guid? WaiverApprovalRequestId { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// A statement that nothing is owed, issued at a point in time. Kept as a record rather than
/// computed on demand, because a transfer completed on Tuesday relied on Tuesday's position.
/// </summary>
public class DuesClearance : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid? BookingId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid PartyId { get; set; }

    public DateOnly IssuedOn { get; set; }
    public DateOnly ValidUntil { get; set; }

    public decimal InstalmentsOutstanding { get; set; }
    public decimal SurchargeOutstanding { get; set; }
    public decimal MaintenanceOutstanding { get; set; }
    public decimal UtilityOutstanding { get; set; }
    public decimal OtherOutstanding { get; set; }
    public decimal TotalOutstanding { get; set; }

    public bool IsClear { get; set; }
    public Guid? IssuedByUserId { get; set; }
    public string? DocumentUrl { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// The appointment where a transfer is actually executed: both parties present, identities
/// checked, deed signed, thumbs pressed, photographs taken, two witnesses recorded.
/// </summary>
public class TransferSession : BaseEntity
{
    public Guid TransferRequestId { get; set; }

    public DateTime ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Venue { get; set; }

    public Guid? ConductedByUserId { get; set; }
    public bool TransferorPresent { get; set; }
    public bool TransfereePresent { get; set; }
    public bool IdentitiesVerified { get; set; }

    public Guid? DeedDocumentId { get; set; }
    public string? DeedNumber { get; set; }
    public string? SessionPhotoUrl { get; set; }
    public string? VideoUrl { get; set; }

    public bool IsCompleted { get; set; }
    public string? AbortReason { get; set; }

    public ICollection<TransferWitness> Witnesses { get; set; } = [];
}

public class TransferWitness : BaseEntity
{
    public Guid TransferSessionId { get; set; }
    public TransferSession? Session { get; set; }

    public Guid? PartyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? IdentityNumber { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? SignatureUrl { get; set; }
    public string? ThumbImpressionUrl { get; set; }
    public int SequenceNumber { get; set; }
}

/// <summary>
/// One link in a unit's ownership chain. Append-only by design — this is the record that has to
/// stand up twenty years and one court case later.
/// </summary>
public class OwnershipChainEntry : BaseEntity
{
    public Guid? UnitId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? PlotFileId { get; set; }

    public int SequenceNumber { get; set; }
    public Guid PartyId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string? FatherOrGuardianName { get; set; }
    public string? IdentityNumber { get; set; }

    public decimal SharePercent { get; set; } = 100m;
    public DateOnly FromDate { get; set; }
    public DateOnly? ToDate { get; set; }

    public TransferKind? AcquiredBy { get; set; }
    public Guid? TransferRequestId { get; set; }
    public Guid? BookingId { get; set; }
    public decimal? Consideration { get; set; }
    public string? DocumentReference { get; set; }
    public bool IsCurrent { get; set; }
}

/// <summary>
/// A lost paper file, and the indemnity trail before a duplicate is issued. A duplicate file with
/// no affidavit behind it is a fraud waiting to be discovered.
/// </summary>
public class DuplicateFileRequest : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid? PlotFileId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid PartyId { get; set; }

    public DateOnly RequestedOn { get; set; }
    public string? LossCircumstances { get; set; }

    public bool AffidavitReceived { get; set; }
    public string? AffidavitUrl { get; set; }
    public bool PoliceReportReceived { get; set; }
    public string? PoliceReportNumber { get; set; }

    /// <summary>The public notice that gives a rival claimant a chance to object.</summary>
    public bool NewspaperNoticePublished { get; set; }
    public DateOnly? NoticePublishedOn { get; set; }
    public string? NoticeClippingUrl { get; set; }
    public int ObjectionWindowDays { get; set; } = 14;
    public bool ObjectionReceived { get; set; }

    public bool IndemnityBondReceived { get; set; }
    public string? IndemnityBondUrl { get; set; }

    public decimal Fee { get; set; }
    public Guid? FeeReceiptId { get; set; }

    public Guid? ApprovalRequestId { get; set; }
    public bool IsIssued { get; set; }
    public DateOnly? IssuedOn { get; set; }
    public string? DuplicateFileNumber { get; set; }
}

/// <summary>The letter that says the unit is ready and here is what is left to pay.</summary>
public class PossessionOffer : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid BookingId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid PartyId { get; set; }

    public PossessionStatus Status { get; set; } = PossessionStatus.NotEligible;
    public DateOnly? OfferedOn { get; set; }
    public DateOnly? WindowFrom { get; set; }
    public DateOnly? WindowTo { get; set; }

    public decimal BalanceDue { get; set; }
    public decimal PossessionChargesDue { get; set; }
    public decimal MaintenanceAdvanceDue { get; set; }
    public decimal CorpusFundDue { get; set; }
    public decimal UtilityDepositsDue { get; set; }
    public decimal TotalDueAtPossession { get; set; }

    public Guid? DemandId { get; set; }
    public Guid? GeneratedDocumentId { get; set; }
    public string? DocumentUrl { get; set; }

    public DateOnly? AppointmentOn { get; set; }
    public Guid? HandoverId { get; set; }
    public bool CustomerDeclined { get; set; }
    public string? DeclineReason { get; set; }

    /// <summary>Delay past the promised date. Carries a compensation cost in some regimes.</summary>
    public int? DelayDays { get; set; }
    public decimal? DelayCompensation { get; set; }

    public ICollection<PossessionChecklistItem> Checklist { get; set; } = [];
}

/// <summary>One gate on the way to handover. All must pass, or an authority must override in writing.</summary>
public class PossessionChecklistItem : BaseEntity
{
    public Guid PossessionOfferId { get; set; }
    public PossessionOffer? Offer { get; set; }

    /// <summary>"DuesCleared", "DocumentsComplete", "OccupancyCertificate", "UnitReady",
    /// "SnagsClosed", "UtilitiesConnected", "KycComplete", "AgreementRegistered".</summary>
    public string CheckKey { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
    public bool IsSatisfied { get; set; }
    public DateOnly? SatisfiedOn { get; set; }
    public bool IsMandatory { get; set; } = true;

    public Guid? OverrideApprovalRequestId { get; set; }
    public string? OverrideReason { get; set; }
    public string? Note { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>The handover itself, and the pack that goes with it.</summary>
public class Handover : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid? BookingId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? ClientBuildContractId { get; set; }
    public Guid PartyId { get; set; }

    public DateTime HandedOverAt { get; set; }
    public Guid? HandedOverByUserId { get; set; }
    public Guid? SnagInspectionId { get; set; }

    public Guid? CertificateDocumentId { get; set; }
    public string? CertificateUrl { get; set; }
    public string? CustomerSignatureUrl { get; set; }

    /// <summary>Starts the defect liability clock. Nothing else does.</summary>
    public DateOnly DefectLiabilityStartsOn { get; set; }

    public bool IsCompleted { get; set; }
    public string? Note { get; set; }

    public ICollection<HandoverItem> Items { get; set; } = [];
}

/// <summary>One thing physically handed across, or one reading taken, at handover.</summary>
public class HandoverItem : BaseEntity
{
    public Guid HandoverId { get; set; }
    public Handover? Handover { get; set; }

    /// <summary>"Keys", "MeterReading", "Warranty", "AsBuiltDrawing", "Manual", "SocietyRules",
    /// "EmergencyContacts", "AccessCard", "RemoteControl".</summary>
    public string ItemType { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public int? Quantity { get; set; }

    // For a meter reading handed over at the door.
    public Guid? MeterId { get; set; }
    public decimal? ReadingValue { get; set; }

    public string? DocumentUrl { get; set; }
    public bool IsHandedOver { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// A structured walk through a unit before it is handed over. Zones and a three-tier severity,
/// because "there are some snags" is not a punch list anybody can work from.
/// </summary>
public class SnagInspection : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid? UnitId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? ClientBuildContractId { get; set; }

    /// <summary>"PreHandover", "Joint", "ReInspection", "DefectPeriod", "Internal".</summary>
    public string InspectionType { get; set; } = "Joint";

    public DateTime InspectedAt { get; set; }
    public Guid? InspectorUserId { get; set; }
    public bool CustomerPresent { get; set; }
    public Guid? CustomerPartyId { get; set; }
    public Guid? ContractorPartyId { get; set; }

    public int CriticalCount { get; set; }
    public int MajorCount { get; set; }
    public int MinorCount { get; set; }
    public int ClosedCount { get; set; }

    /// <summary>Any critical snag open means handover is blocked. Computed, not typed.</summary>
    public bool BlocksHandover { get; set; }

    public Guid? PunchListId { get; set; }
    public string? CustomerSignatureUrl { get; set; }
    public string? InspectorSignatureUrl { get; set; }
    public DateOnly? TargetClosureDate { get; set; }
    public bool IsClosed { get; set; }

    public ICollection<Snag> Snags { get; set; } = [];
}

public class Snag : BaseEntity
{
    public Guid SnagInspectionId { get; set; }
    public SnagInspection? Inspection { get; set; }

    public int SnagNumber { get; set; }
    public SnagZone Zone { get; set; }
    public SnagSeverity Severity { get; set; } = SnagSeverity.Minor;
    public SnagStatus Status { get; set; } = SnagStatus.Open;

    public string? Location { get; set; }

    /// <summary>Pin coordinates on the floor plan, so a trade can find it without a guided tour.</summary>
    public decimal? PlanX { get; set; }
    public decimal? PlanY { get; set; }

    public CostBearer ResponsibleParty { get; set; } = CostBearer.Contractor;
    public Guid? ResponsiblePartyId { get; set; }
    public Guid? SubcontractId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public Guid? WorkOrderId { get; set; }

    public DateOnly? TargetDate { get; set; }
    public DateOnly? FixedOn { get; set; }
    public DateOnly? VerifiedOn { get; set; }
    public Guid? VerifiedByUserId { get; set; }

    public decimal? EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public string? RejectionReason { get; set; }

    public ICollection<SnagPhoto> Photos { get; set; } = [];
}

public class SnagPhoto : BaseEntity
{
    public Guid SnagId { get; set; }
    public Snag? Snag { get; set; }

    public string Url { get; set; } = string.Empty;

    /// <summary>"Before" or "After". The pair is what closes a snag, not a status click.</summary>
    public string Stage { get; set; } = "Before";

    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
    public Guid? CapturedByUserId { get; set; }
    public string? Caption { get; set; }
}

/// <summary>The signed list of outstanding items, against which retention tranches are released.</summary>
public class PunchList : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid? SnagInspectionId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? SubcontractId { get; set; }

    public DateOnly IssuedOn { get; set; }
    public DateOnly? AgreedClosureDate { get; set; }

    public int ItemCount { get; set; }
    public int ClosedCount { get; set; }
    public decimal PercentComplete { get; set; }

    public bool IssuerSigned { get; set; }
    public bool CounterpartySigned { get; set; }
    public string? DocumentUrl { get; set; }

    /// <summary>Retention that stays held until this list closes.</summary>
    public decimal RetentionHeldAgainst { get; set; }

    public bool IsClosed { get; set; }
    public DateOnly? ClosedOn { get; set; }
}

/// <summary>
/// The warranty period after handover. Per category, because structure is covered for years and a
/// paint blemish for months, and a single duration would be wrong for both.
/// </summary>
public class DefectLiability : BaseEntity
{
    public Guid? UnitId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? HandoverId { get; set; }
    public Guid? ClientBuildContractId { get; set; }
    public Guid? SubcontractId { get; set; }

    public DefectCategory Category { get; set; }
    public DateOnly StartsOn { get; set; }
    public int DurationMonths { get; set; }
    public DateOnly ExpiresOn { get; set; }

    public CostBearer LiableParty { get; set; } = CostBearer.Developer;
    public Guid? LiablePartyId { get; set; }

    /// <summary>Working days to respond, by severity. Enforced by the work-order SLA.</summary>
    public int ResponseSlaDaysCritical { get; set; } = 1;
    public int ResponseSlaDaysMajor { get; set; } = 7;
    public int ResponseSlaDaysMinor { get; set; } = 30;

    public bool ExpiryNoticeSent { get; set; }
    public bool IsExpired { get; set; }
    public string? Note { get; set; }
}

/// <summary>A defect reported by the customer inside the liability period.</summary>
public class DefectClaim : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid? DefectLiabilityId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid PartyId { get; set; }

    public DefectCategory Category { get; set; }
    public SnagSeverity Severity { get; set; } = SnagSeverity.Minor;
    public TicketStatus Status { get; set; } = TicketStatus.Open;

    public DateOnly ReportedOn { get; set; }
    public DateTime? SlaDueAt { get; set; }
    public bool SlaBreached { get; set; }

    public Guid? WorkOrderId { get; set; }
    public CostBearer CostBearer { get; set; } = CostBearer.Developer;
    public decimal? Cost { get; set; }

    /// <summary>Outside the period, outside the scope, or caused by the occupier. Refused with a reason.</summary>
    public bool IsRejected { get; set; }
    public string? RejectionReason { get; set; }

    public DateOnly? ResolvedOn { get; set; }
    public int? CustomerRating { get; set; }
}
