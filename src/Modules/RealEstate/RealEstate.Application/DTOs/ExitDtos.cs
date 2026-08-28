using RealEstate.Domain.Enums;

namespace RealEstate.Application.DTOs;

// =====================================================================================
// Cancellation, refund, transfer, possession and snagging — how a booking ends, one way
// or another.
// =====================================================================================

public class CancellationDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid BookingId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string? UnitNumber { get; set; }
    public string ProjectName { get; set; } = string.Empty;

    public CancellationTrigger Trigger { get; set; }
    public string ReasonLabel { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateOnly RequestedOn { get; set; }
    public DateOnly? EffectiveOn { get; set; }

    public decimal TotalConsideration { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal SurchargeOutstanding { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal AdministrativeCharge { get; set; }
    public decimal CommissionClawback { get; set; }
    public decimal RefundableAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public string RequestedByName { get; set; } = string.Empty;
    public ApprovalOutcome Outcome { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? RefundRequestId { get; set; }
    public bool UnitReleased { get; set; }
    public DateOnly? UnitReleasedOn { get; set; }
    public bool CustomerAcknowledged { get; set; }
    public Guid? LegalNoticeId { get; set; }

    public List<DeductionLineDto> Deductions { get; set; } = [];
}

public class DeductionLineDto
{
    public Guid? Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    /// <summary>The slab or rule that produced it. A deduction nobody can explain is not collectable.</summary>
    public string? Basis { get; set; }

    public bool IsWaived { get; set; }
    public int SortOrder { get; set; }
}

public class CancellationRequestDto
{
    public Guid BookingId { get; set; }
    public CancellationTrigger Trigger { get; set; }
    public Guid ReasonCodeId { get; set; }
    public string? Note { get; set; }
    public DateOnly RequestedOn { get; set; }
    public DateOnly? EffectiveOn { get; set; }
    public Guid? DeductionPolicyId { get; set; }

    /// <summary>Waive part of the deduction. Needs its own approval on top of the cancellation's.</summary>
    public decimal? OverrideDeductionAmount { get; set; }
    public string? OverrideReason { get; set; }

    public bool ReleaseUnitImmediately { get; set; } = true;
    public bool DryRun { get; set; }
}

/// <summary>The arithmetic before anything is committed, with the working shown to the customer.</summary>
public class CancellationPreviewDto
{
    public decimal TotalConsideration { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal SurchargeOutstanding { get; set; }
    public List<DeductionLineDto> Deductions { get; set; } = [];
    public decimal DeductionTotal { get; set; }
    public decimal RefundableAmount { get; set; }
    public string RefundableInWords { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "USD";

    public int MonthsSinceBooking { get; set; }
    public decimal PaidPercent { get; set; }
    public string? SlabApplied { get; set; }
    public decimal CommissionClawback { get; set; }
    public bool RefundOnlyAfterResale { get; set; }
    public int RefundInstalmentCount { get; set; }
    public bool RequiresApproval { get; set; }
    public List<string> ApprovalsRequired { get; set; } = [];
}

public class DeductionPolicyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public DeductionBasis Basis { get; set; }
    public decimal FlatPercent { get; set; }
    public decimal FlatAmount { get; set; }
    public decimal AdministrativeCharge { get; set; }
    public bool ForfeitAccruedSurcharge { get; set; }
    public bool ClawBackCommission { get; set; }
    public bool RefundOnlyAfterResale { get; set; }
    public int RefundInstalmentCount { get; set; }
    public bool IsActive { get; set; }
    public List<DeductionSlabDto> Slabs { get; set; } = [];
}

public class DeductionSlabDto
{
    public Guid? Id { get; set; }
    public int FromMonth { get; set; }
    public int? ToMonth { get; set; }
    public decimal? FromPaidPercent { get; set; }
    public decimal? ToPaidPercent { get; set; }
    public decimal DeductionPercent { get; set; }
    public decimal DeductionAmount { get; set; }
    public bool AppliesToPaidAmount { get; set; }
    public int SortOrder { get; set; }
}

public class RefundRequestDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public Guid? BookingId { get; set; }
    public string? BookingReference { get; set; }
    public Guid? CancellationId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? TokenReservationId { get; set; }

    public RefundStatus Status { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Outstanding { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public DateOnly RequestedOn { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? SecondApproverName { get; set; }

    public string? PayeeName { get; set; }
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public bool BankDetailsVerified { get; set; }
    public bool AwaitingResale { get; set; }
    public Guid? ResaleBookingId { get; set; }
    public string? RejectionReason { get; set; }

    public List<RefundScheduleDto> Schedule { get; set; } = [];
}

public class RefundScheduleDto
{
    public Guid Id { get; set; }
    public int SequenceNumber { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateOnly? PaidOn { get; set; }
    public PaymentInstrument? Instrument { get; set; }
    public string? PaymentReference { get; set; }
    public bool IsPaid { get; set; }
    public bool IsOverdue { get; set; }
}

public class ResaleRequestDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid BookingId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public Guid SellerPartyId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public Guid? BuyerPartyId { get; set; }
    public string? BuyerName { get; set; }
    public DateOnly RequestedOn { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal ResalePrice { get; set; }
    public decimal GainAmount { get; set; }
    public decimal ResaleFee { get; set; }
    public decimal WithholdingTax { get; set; }
    public TransferStatus Status { get; set; }
    public Guid? TransferRequestId { get; set; }
    public Guid? NewBookingId { get; set; }
    public string? Note { get; set; }
}

// ── Transfer ─────────────────────────────────────────────────────────────────

public class TransferRequestListItemDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public TransferKind Kind { get; set; }
    public TransferStatus Status { get; set; }
    public DateOnly RequestedOn { get; set; }

    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string? UnitNumber { get; set; }
    public string? FileNumber { get; set; }
    public Guid? BookingId { get; set; }
    public string? BookingReference { get; set; }

    public string TransferorName { get; set; } = string.Empty;
    public string? TransfereeName { get; set; }
    public decimal? SaleConsideration { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public decimal OutstandingAtRequest { get; set; }
    public bool DuesCleared { get; set; }
    public bool NocIssued { get; set; }
    public decimal TotalTransferFee { get; set; }
    public decimal FeesPaid { get; set; }
    public bool FeesCleared { get; set; }
    public DateTime? SessionScheduledAt { get; set; }
    public DateOnly? CompletedOn { get; set; }
    public bool BlockedByLitigation { get; set; }
    public string? HandledByName { get; set; }
    public int DaysOpen { get; set; }
}

public class TransferRequestDetailDto : TransferRequestListItemDto
{
    public Guid? UnitId { get; set; }
    public Guid? PlotFileId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid RequestedByPartyId { get; set; }

    public Guid? DuesClearanceId { get; set; }
    public Guid? NocIssuanceId { get; set; }
    public Guid? FeeReceiptId { get; set; }
    public Guid? NewBookingId { get; set; }
    public Guid? NewAllotmentId { get; set; }

    public string? SuccessionCertificateNumber { get; set; }
    public Guid? PowerOfAttorneyRelationshipId { get; set; }
    public string? CourtDecreeReference { get; set; }
    public decimal? ShareTransferredPercent { get; set; }
    public string? RejectReason { get; set; }
    public string? Note { get; set; }

    public List<TransferPartyDto> Parties { get; set; } = [];
    public List<TransferFeeLineDto> FeeLines { get; set; } = [];
    public DuesClearanceDto? DuesClearance { get; set; }
    public NocIssuanceDto? Noc { get; set; }
    public TransferSessionDto? Session { get; set; }
    public List<ChecklistItemDto> DocumentChecklist { get; set; } = [];

    /// <summary>The four gates, in order, with the first unmet one flagged.</summary>
    public List<TransferGateDto> Gates { get; set; } = [];
}

/// <summary>One of the gates a transfer has to pass, and whether it has.</summary>
public class TransferGateDto
{
    public int Order { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsSatisfied { get; set; }
    public string? Detail { get; set; }
    public string? BlockingReason { get; set; }
    public bool CanOverride { get; set; }
    public string? OverrideRole { get; set; }
    public bool WasOverridden { get; set; }
    public string? Route { get; set; }
}

public class TransferPartyDto
{
    public Guid? Id { get; set; }
    public Guid PartyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? FatherOrGuardianName { get; set; }
    public string? IdentityNumber { get; set; }
    public string? Phone { get; set; }
    public string? PhotoUrl { get; set; }
    public string Side { get; set; } = "Transferee";
    public decimal SharePercent { get; set; }
    public KycStatus KycStatus { get; set; }
    public bool IdentityVerified { get; set; }
    public bool IsPresent { get; set; }
    public string? SignatureUrl { get; set; }
    public string? ThumbImpressionUrl { get; set; }
}

public class TransferFeeLineDto
{
    public Guid? Id { get; set; }
    public string FeeType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public ChargeBasis Basis { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public string PayableBy { get; set; } = "Transferee";
    public bool IsPaid { get; set; }
    public bool IsWaived { get; set; }
    public int SortOrder { get; set; }
}

public class TransferRequestCreateDto
{
    public Guid? BookingId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? PlotFileId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid ProjectId { get; set; }

    public TransferKind Kind { get; set; }
    public DateOnly RequestedOn { get; set; }
    public Guid RequestedByPartyId { get; set; }
    public decimal? SaleConsideration { get; set; }
    public decimal? ShareTransferredPercent { get; set; }

    public List<TransferPartyDto> Parties { get; set; } = [];

    public string? SuccessionCertificateNumber { get; set; }
    public Guid? PowerOfAttorneyRelationshipId { get; set; }
    public string? CourtDecreeReference { get; set; }
    public string? Note { get; set; }

    /// <summary>Create the buyer as a new party in the same call.</summary>
    public PartyUpsertDto? NewTransferee { get; set; }
}

public class DuesClearanceDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateOnly IssuedOn { get; set; }
    public DateOnly ValidUntil { get; set; }
    public decimal InstalmentsOutstanding { get; set; }
    public decimal SurchargeOutstanding { get; set; }
    public decimal MaintenanceOutstanding { get; set; }
    public decimal UtilityOutstanding { get; set; }
    public decimal OtherOutstanding { get; set; }
    public decimal TotalOutstanding { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool IsClear { get; set; }
    public bool IsExpired { get; set; }
    public string? IssuedByName { get; set; }
    public string? DocumentUrl { get; set; }
    public string? Note { get; set; }
}

public class TransferSessionDto
{
    public Guid Id { get; set; }
    public Guid TransferRequestId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Venue { get; set; }
    public string? ConductedByName { get; set; }
    public bool TransferorPresent { get; set; }
    public bool TransfereePresent { get; set; }
    public bool IdentitiesVerified { get; set; }
    public string? DeedNumber { get; set; }
    public string? SessionPhotoUrl { get; set; }
    public string? VideoUrl { get; set; }
    public bool IsCompleted { get; set; }
    public string? AbortReason { get; set; }
    public List<TransferWitnessDto> Witnesses { get; set; } = [];
}

public class TransferWitnessDto
{
    public Guid? Id { get; set; }
    public Guid? PartyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? IdentityNumber { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? SignatureUrl { get; set; }
    public string? ThumbImpressionUrl { get; set; }
    public int SequenceNumber { get; set; }
}

public class OwnershipChainEntryDto
{
    public Guid Id { get; set; }
    public int SequenceNumber { get; set; }
    public Guid PartyId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string? FatherOrGuardianName { get; set; }
    public string? IdentityNumber { get; set; }
    public decimal SharePercent { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public TransferKind? AcquiredBy { get; set; }
    public decimal? Consideration { get; set; }
    public string? DocumentReference { get; set; }
    public Guid? TransferRequestId { get; set; }
    public bool IsCurrent { get; set; }
    public int? HeldForDays { get; set; }
}

public class DuplicateFileRequestDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid? PlotFileId { get; set; }
    public string? FileNumber { get; set; }
    public Guid? BookingId { get; set; }
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public DateOnly RequestedOn { get; set; }
    public string? LossCircumstances { get; set; }

    public bool AffidavitReceived { get; set; }
    public string? AffidavitUrl { get; set; }
    public bool PoliceReportReceived { get; set; }
    public string? PoliceReportNumber { get; set; }
    public bool NewspaperNoticePublished { get; set; }
    public DateOnly? NoticePublishedOn { get; set; }
    public string? NoticeClippingUrl { get; set; }
    public int ObjectionWindowDays { get; set; }
    public DateOnly? ObjectionWindowEndsOn { get; set; }
    public bool ObjectionReceived { get; set; }
    public bool IndemnityBondReceived { get; set; }
    public string? IndemnityBondUrl { get; set; }

    public decimal Fee { get; set; }
    public bool IsIssued { get; set; }
    public DateOnly? IssuedOn { get; set; }
    public string? DuplicateFileNumber { get; set; }
    public List<ChecklistItemDto> Checklist { get; set; } = [];
}

// ── Possession & handover ────────────────────────────────────────────────────

public class PossessionOfferDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid BookingId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public Guid? UnitId { get; set; }
    public string? UnitNumber { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string? PartyPhone { get; set; }

    public PossessionStatus Status { get; set; }
    public DateOnly? OfferedOn { get; set; }
    public DateOnly? WindowFrom { get; set; }
    public DateOnly? WindowTo { get; set; }
    public DateOnly? AppointmentOn { get; set; }

    public decimal BalanceDue { get; set; }
    public decimal PossessionChargesDue { get; set; }
    public decimal MaintenanceAdvanceDue { get; set; }
    public decimal CorpusFundDue { get; set; }
    public decimal UtilityDepositsDue { get; set; }
    public decimal TotalDueAtPossession { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public string? DocumentUrl { get; set; }
    public Guid? HandoverId { get; set; }
    public bool CustomerDeclined { get; set; }
    public string? DeclineReason { get; set; }
    public int? DelayDays { get; set; }
    public decimal? DelayCompensation { get; set; }

    public List<ChecklistItemDto> Checklist { get; set; } = [];
    public bool IsEligible { get; set; }
    public int OpenCriticalSnags { get; set; }
}

public class HandoverDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid? BookingId { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitNumber { get; set; }
    public Guid? ClientBuildContractId { get; set; }
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public DateTime HandedOverAt { get; set; }
    public string? HandedOverByName { get; set; }
    public Guid? SnagInspectionId { get; set; }
    public string? CertificateUrl { get; set; }
    public string? CustomerSignatureUrl { get; set; }
    public DateOnly DefectLiabilityStartsOn { get; set; }
    public bool IsCompleted { get; set; }
    public string? Note { get; set; }
    public List<HandoverItemDto> Items { get; set; } = [];
    public List<DefectLiabilityDto> Liabilities { get; set; } = [];
}

public class HandoverItemDto
{
    public Guid? Id { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public int? Quantity { get; set; }
    public Guid? MeterId { get; set; }
    public string? MeterNumber { get; set; }
    public decimal? ReadingValue { get; set; }
    public string? DocumentUrl { get; set; }
    public bool IsHandedOver { get; set; }
    public int SortOrder { get; set; }
}

public class SnagInspectionDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid? UnitId { get; set; }
    public string? UnitNumber { get; set; }
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? ClientBuildContractId { get; set; }

    public string InspectionType { get; set; } = "Joint";
    public DateTime InspectedAt { get; set; }
    public string? InspectorName { get; set; }
    public bool CustomerPresent { get; set; }
    public string? CustomerName { get; set; }
    public string? ContractorName { get; set; }

    public int CriticalCount { get; set; }
    public int MajorCount { get; set; }
    public int MinorCount { get; set; }
    public int ClosedCount { get; set; }
    public int OpenCount { get; set; }
    public decimal PercentClosed { get; set; }
    public bool BlocksHandover { get; set; }

    public DateOnly? TargetClosureDate { get; set; }
    public string? CustomerSignatureUrl { get; set; }
    public string? InspectorSignatureUrl { get; set; }
    public bool IsClosed { get; set; }
    public Guid? PunchListId { get; set; }

    public List<SnagDto> Snags { get; set; } = [];
}

public class SnagDto
{
    public Guid Id { get; set; }
    public int SnagNumber { get; set; }
    public SnagZone Zone { get; set; }
    public SnagSeverity Severity { get; set; }
    public SnagStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public decimal? PlanX { get; set; }
    public decimal? PlanY { get; set; }
    public CostBearer ResponsibleParty { get; set; }
    public string? ResponsibleName { get; set; }
    public string? AssignedToName { get; set; }
    public Guid? WorkOrderId { get; set; }
    public DateOnly? TargetDate { get; set; }
    public DateOnly? FixedOn { get; set; }
    public DateOnly? VerifiedOn { get; set; }
    public string? VerifiedByName { get; set; }
    public decimal? EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public string? RejectionReason { get; set; }
    public bool IsOverdue { get; set; }
    public List<SnagPhotoDto> Photos { get; set; } = [];
}

public class SnagPhotoDto
{
    public Guid? Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Stage { get; set; } = "Before";
    public DateTime CapturedAt { get; set; }
    public string? Caption { get; set; }
}

public class SnagUpsertDto
{
    public Guid? Id { get; set; }
    public Guid SnagInspectionId { get; set; }
    public SnagZone Zone { get; set; }
    public SnagSeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public decimal? PlanX { get; set; }
    public decimal? PlanY { get; set; }
    public CostBearer ResponsibleParty { get; set; }
    public Guid? ResponsiblePartyId { get; set; }
    public Guid? SubcontractId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public DateOnly? TargetDate { get; set; }
    public decimal? EstimatedCost { get; set; }
    public List<SnagPhotoDto> Photos { get; set; } = [];

    /// <summary>Captured on a tablet with no signal and queued. Reconciled when it syncs.</summary>
    public string? ClientReference { get; set; }
}

/// <summary>A batch of snags captured offline during a walk, posted when the connection returns.</summary>
public class SnagSyncBatchDto
{
    public Guid SnagInspectionId { get; set; }
    public List<SnagUpsertDto> Snags { get; set; } = [];
    public DateTime CapturedAt { get; set; }
}

public class PunchListDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid? SnagInspectionId { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitNumber { get; set; }
    public Guid? SubcontractId { get; set; }
    public string? ContractorName { get; set; }
    public DateOnly IssuedOn { get; set; }
    public DateOnly? AgreedClosureDate { get; set; }
    public int ItemCount { get; set; }
    public int ClosedCount { get; set; }
    public decimal PercentComplete { get; set; }
    public bool IssuerSigned { get; set; }
    public bool CounterpartySigned { get; set; }
    public string? DocumentUrl { get; set; }
    public decimal RetentionHeldAgainst { get; set; }
    public bool IsClosed { get; set; }
    public DateOnly? ClosedOn { get; set; }
    public bool IsOverdue { get; set; }
}

public class DefectLiabilityDto
{
    public Guid Id { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitNumber { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? ClientBuildContractId { get; set; }
    public Guid? SubcontractId { get; set; }
    public DefectCategory Category { get; set; }
    public DateOnly StartsOn { get; set; }
    public int DurationMonths { get; set; }
    public DateOnly ExpiresOn { get; set; }
    public int DaysRemaining { get; set; }
    public CostBearer LiableParty { get; set; }
    public string? LiablePartyName { get; set; }
    public int ResponseSlaDaysCritical { get; set; }
    public int ResponseSlaDaysMajor { get; set; }
    public int ResponseSlaDaysMinor { get; set; }
    public bool ExpiryNoticeSent { get; set; }
    public bool IsExpired { get; set; }
    public int OpenClaimCount { get; set; }
}

public class DefectClaimDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid? UnitId { get; set; }
    public string? UnitNumber { get; set; }
    public Guid? BookingId { get; set; }
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string? PartyPhone { get; set; }
    public DefectCategory Category { get; set; }
    public SnagSeverity Severity { get; set; }
    public TicketStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateOnly ReportedOn { get; set; }
    public DateTime? SlaDueAt { get; set; }
    public bool SlaBreached { get; set; }
    public Guid? WorkOrderId { get; set; }
    public CostBearer CostBearer { get; set; }
    public decimal? Cost { get; set; }
    public bool IsRejected { get; set; }
    public string? RejectionReason { get; set; }
    public DateOnly? ResolvedOn { get; set; }
    public int? CustomerRating { get; set; }
    public bool IsInsideLiabilityPeriod { get; set; }
}

public class NocIssuanceDto
{
    public Guid Id { get; set; }
    public string NocNumber { get; set; } = string.Empty;
    public NocKind Kind { get; set; }
    public NocStatus Status { get; set; }
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitNumber { get; set; }
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;

    public DateOnly RequestedOn { get; set; }
    public DateOnly? IssuedOn { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public bool IsExpired { get; set; }

    public bool DuesCleared { get; set; }
    public decimal OutstandingAtIssue { get; set; }
    public decimal Fee { get; set; }
    public bool FeePaid { get; set; }

    public string? IssuedByName { get; set; }
    public string? DocumentUrl { get; set; }
    public string? VerificationCode { get; set; }
    public string? AddressedTo { get; set; }
    public string? Purpose { get; set; }
    public string? RejectionReason { get; set; }
    public bool IsRevoked { get; set; }
    public string? RevocationReason { get; set; }
    public List<NocConditionDto> Conditions { get; set; } = [];
}

public class NocConditionDto
{
    public Guid? Id { get; set; }
    public string Condition { get; set; } = string.Empty;
    public DateOnly? ComplyByDate { get; set; }
    public bool IsSatisfied { get; set; }
    public DateOnly? SatisfiedOn { get; set; }
    public bool BreachRevokesNoc { get; set; }
    public int SortOrder { get; set; }
}

public class NocRequestDto
{
    public NocKind Kind { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? SocietyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid PartyId { get; set; }
    public Guid? TransferRequestId { get; set; }
    public Guid? BuildingPlanApplicationId { get; set; }
    public Guid? CustomerMortgageId { get; set; }
    public string? AddressedTo { get; set; }
    public string? Purpose { get; set; }
    public int ValidDays { get; set; } = 90;
    public decimal? Fee { get; set; }
    public List<NocConditionDto> Conditions { get; set; } = [];
}
