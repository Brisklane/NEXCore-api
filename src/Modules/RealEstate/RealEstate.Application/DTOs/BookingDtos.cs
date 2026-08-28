using RealEstate.Domain.Enums;

namespace RealEstate.Application.DTOs;

// =====================================================================================
// Offers, tokens, bookings, allotment and balloting.
// =====================================================================================

public class OfferListItemDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string PropertyReference { get; set; } = string.Empty;
    public string AddressOneLine { get; set; } = string.Empty;
    public Guid? ListingId { get; set; }

    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerPhone { get; set; }
    public string? SellerName { get; set; }
    public string? AgentName { get; set; }

    public decimal Amount { get; set; }
    public decimal? AskingPrice { get; set; }
    public decimal? DifferenceFromAsking { get; set; }
    public decimal? DifferencePercent { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public OfferStatus Status { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int RoundNumber { get; set; }
    public bool IsBestAndFinal { get; set; }

    public FundingKind Funding { get; set; }
    public bool ProofOfFundsProvided { get; set; }
    public bool MortgageInPrinciple { get; set; }
    public bool IsChainFree { get; set; }
    public int? ChainLength { get; set; }
    public int ConditionCount { get; set; }
    public bool VendorNotified { get; set; }

    /// <summary>Composite of price, funding, chain and conditions. Why a lower offer can win.</summary>
    public int StrengthScore { get; set; }
}

public class OfferDetailDto : OfferListItemDto
{
    public Guid BuyerPartyId { get; set; }
    public Guid? SellerPartyId { get; set; }
    public Guid? EnquiryId { get; set; }
    public decimal? DepositAmount { get; set; }
    public string? ProofOfFundsUrl { get; set; }
    public DateOnly? ProposedCompletionDate { get; set; }
    public DateTime? DecidedAt { get; set; }
    public DateTime? VendorNotifiedAt { get; set; }
    public string? RejectReason { get; set; }
    public string? Note { get; set; }
    public Guid? ResultingDealId { get; set; }
    public List<OfferConditionDto> Conditions { get; set; } = [];
    public List<OfferCounterDto> Counters { get; set; } = [];
}

public class OfferConditionDto
{
    public Guid? Id { get; set; }
    public OfferConditionKind Kind { get; set; }
    public string? Detail { get; set; }
    public DateOnly? SatisfyByDate { get; set; }
    public bool IsSatisfied { get; set; }
    public DateOnly? SatisfiedOn { get; set; }
    public bool IsOverdue { get; set; }
}

public class OfferCounterDto
{
    public Guid Id { get; set; }
    public int RoundNumber { get; set; }
    public string ProposedBy { get; set; } = "Seller";
    public decimal Amount { get; set; }
    public DateTime ProposedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public OfferStatus Outcome { get; set; }
    public string? Note { get; set; }
}

public class OfferUpsertDto
{
    public Guid? Id { get; set; }
    public Guid PropertyId { get; set; }
    public Guid? ListingId { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid BuyerPartyId { get; set; }
    public Guid? SellerPartyId { get; set; }
    public Guid? AgentId { get; set; }

    public decimal Amount { get; set; }
    public string? CurrencyCode { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateOnly? ProposedCompletionDate { get; set; }

    public FundingKind Funding { get; set; }
    public decimal? DepositAmount { get; set; }
    public bool ProofOfFundsProvided { get; set; }
    public string? ProofOfFundsUrl { get; set; }
    public bool MortgageInPrinciple { get; set; }
    public bool IsChainFree { get; set; }
    public int? ChainLength { get; set; }
    public bool IsBestAndFinal { get; set; }
    public string? Note { get; set; }

    public List<OfferConditionDto> Conditions { get; set; } = [];
    public bool NotifyVendor { get; set; } = true;
}

public class OfferDecisionDto
{
    public Guid OfferId { get; set; }
    public OfferStatus Decision { get; set; }
    public decimal? CounterAmount { get; set; }
    public Guid? RejectReasonCodeId { get; set; }
    public string? Note { get; set; }

    /// <summary>Accepting an offer opens a deal. Set false to accept without progressing yet.</summary>
    public bool CreateDeal { get; set; } = true;
}

// ── EOI & token ──────────────────────────────────────────────────────────────

public class ExpressionOfInterestDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string? PartyPhone { get; set; }
    public string? PartnerName { get; set; }
    public string? CategoryCode { get; set; }
    public decimal Amount { get; set; }
    public DateTime ReceivedAt { get; set; }
    public int PriorityNumber { get; set; }
    public ReservationStatus Status { get; set; }
    public Guid? ReceiptId { get; set; }
    public string? ReceiptNumber { get; set; }
    public Guid? ConvertedBookingId { get; set; }
    public bool IsRefundable { get; set; }
    public DateTime? RefundedAt { get; set; }
}

public class TokenReservationDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid UnitId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public Guid? PlotFileId { get; set; }
    public string? FileNumber { get; set; }

    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string? PartyPhone { get; set; }
    public string? PartnerName { get; set; }
    public string? SalesExecutiveName { get; set; }

    public decimal Amount { get; set; }
    public decimal? AgreedPrice { get; set; }
    public decimal? ListPrice { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public DateTime ReceivedAt { get; set; }
    public DateTime ValidUntil { get; set; }
    public int HoursRemaining { get; set; }
    public ReservationStatus Status { get; set; }
    public bool IsAdjustableAgainstPrice { get; set; }
    public bool IsForfeitableOnWithdrawal { get; set; }
    public string? ReceiptNumber { get; set; }
    public Guid? ConvertedBookingId { get; set; }
    public string? AgreementUrl { get; set; }
}

public class TokenReservationCreateDto
{
    public Guid UnitId { get; set; }
    public Guid? PlotFileId { get; set; }
    public Guid? PartyId { get; set; }
    public PartyUpsertDto? NewParty { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? ChannelPartnerId { get; set; }
    public Guid? SalesExecutiveId { get; set; }

    public decimal Amount { get; set; }
    public decimal? AgreedPrice { get; set; }
    public int ValidDays { get; set; } = 7;
    public bool IsAdjustableAgainstPrice { get; set; } = true;
    public bool IsForfeitableOnWithdrawal { get; set; } = true;

    public ReceiptCreateDto? Payment { get; set; }
}

// ── Bookings ─────────────────────────────────────────────────────────────────

public class BookingListItemDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public BookingStatus Status { get; set; }
    public DateOnly BookingDate { get; set; }

    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public Guid? UnitId { get; set; }
    public string? UnitNumber { get; set; }
    public string? BlockName { get; set; }
    public string? FileNumber { get; set; }
    public PropertySubType? SubType { get; set; }
    public AreaDto? Area { get; set; }

    public Guid PrimaryApplicantPartyId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string? ApplicantPhone { get; set; }
    public string? FatherOrGuardianName { get; set; }

    public SourcingChannel SourcingChannel { get; set; }
    public string? PartnerName { get; set; }
    public string? SalesExecutiveName { get; set; }

    public decimal TotalConsideration { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Outstanding { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal CollectionPercent { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public DateOnly? NextDueDate { get; set; }
    public decimal NextDueAmount { get; set; }
    public int DaysOverdue { get; set; }
    public bool IsDefaulting { get; set; }
    public bool IsUnderLitigation { get; set; }
    public KycStatus KycStatus { get; set; }
}

public class BookingDetailDto : BookingListItemDto
{
    public Guid? PropertyId { get; set; }
    public Guid? PlotFileId { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? SiteVisitId { get; set; }
    public Guid? TokenReservationId { get; set; }
    public Guid? ExpressionOfInterestId { get; set; }
    public Guid? LeadRegistrationId { get; set; }
    public Guid? CampaignId { get; set; }

    public decimal ListPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal NetSalePrice { get; set; }
    public decimal RatePerSqFt { get; set; }
    public decimal TotalDemanded { get; set; }
    public decimal TotalSurcharge { get; set; }
    public decimal TotalWaived { get; set; }

    public Guid? PaymentPlanId { get; set; }
    public string? PaymentPlanName { get; set; }
    public Guid? AllotmentId { get; set; }
    public string? AllotmentNumber { get; set; }
    public Guid? SaleAgreementId { get; set; }

    public DateTime? ConfirmedAt { get; set; }
    public DateOnly? AgreementSignedOn { get; set; }
    public DateOnly? PossessionOfferedOn { get; set; }
    public DateOnly? PossessionTakenOn { get; set; }
    public DateOnly? RegisteredOn { get; set; }

    public Guid? CancellationId { get; set; }
    public Guid? TransferRequestId { get; set; }
    public Guid? PreviousBookingId { get; set; }
    public Guid? DunningCaseId { get; set; }
    public Guid? CustomerMortgageId { get; set; }
    public string? Notes { get; set; }

    public List<BookingApplicantDto> Applicants { get; set; } = [];
    public List<CostSheetLineDto> ChargeLines { get; set; } = [];
    public PaymentPlanDto? PaymentPlan { get; set; }
    public List<DemandListItemDto> Demands { get; set; } = [];
    public List<ReceiptListItemDto> Receipts { get; set; } = [];
    public List<CustomerLedgerEntryDto> Ledger { get; set; } = [];
    public List<GeneratedDocumentDto> Documents { get; set; } = [];
    public List<BookingStatusHistoryDto> StatusHistory { get; set; } = [];
    public List<TimelineEntryDto> Timeline { get; set; } = [];
    public List<ChecklistItemDto> DocumentChecklist { get; set; } = [];

    public PossessionOfferDto? Possession { get; set; }
    public List<ApprovalRequestDto> PendingApprovals { get; set; } = [];
}

public class BookingApplicantDto
{
    public Guid? Id { get; set; }
    public Guid PartyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? FatherOrGuardianName { get; set; }
    public string? Phone { get; set; }
    public string? IdentityNumber { get; set; }
    public string? PhotoUrl { get; set; }
    public int SequenceNumber { get; set; }
    public bool IsPrimary { get; set; }
    public decimal SharePercent { get; set; }
    public string Role { get; set; } = "Applicant";
    public KycStatus KycStatus { get; set; }
    public DateOnly AddedOn { get; set; }
    public DateOnly? RemovedOn { get; set; }
}

public class BookingStatusHistoryDto
{
    public BookingStatus FromStatus { get; set; }
    public BookingStatus ToStatus { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? ChangedByName { get; set; }
    public string? Reason { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// What the booking wizard posts. One call creates the party (if new), the booking, the charge
/// lines, the payment plan and the first receipt — because a salesperson with a customer in front
/// of them cannot make five.
/// </summary>
public class BookingCreateDto
{
    public Guid ProjectId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? PlotFileId { get; set; }
    public Guid? HoldId { get; set; }
    public Guid? TokenReservationId { get; set; }
    public Guid? ExpressionOfInterestId { get; set; }

    public DateOnly BookingDate { get; set; }

    /// <summary>Existing party, or a new one created in the same call.</summary>
    public Guid? PrimaryApplicantPartyId { get; set; }
    public PartyUpsertDto? NewApplicant { get; set; }
    public List<BookingApplicantDto> CoApplicants { get; set; } = [];
    public Guid? NomineePartyId { get; set; }

    public SourcingChannel SourcingChannel { get; set; }
    public Guid? ChannelPartnerId { get; set; }
    public Guid? LeadRegistrationId { get; set; }
    public Guid? SalesExecutiveId { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? CampaignId { get; set; }
    public Guid? SiteVisitId { get; set; }

    // Price
    public Guid? PriceListId { get; set; }
    public decimal? OverrideListPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public Guid? DiscountReasonCodeId { get; set; }
    public string? DiscountNote { get; set; }

    /// <summary>Optional premiums the customer declined — a second parking bay, a club membership.</summary>
    public List<ChargeKind> DeclinedOptionalCharges { get; set; } = [];

    // Plan
    public Guid? PaymentPlanTemplateId { get; set; }
    public PaymentPlanCustomDto? CustomPlan { get; set; }

    // First money
    public ReceiptCreateDto? BookingPayment { get; set; }

    public string? Notes { get; set; }

    /// <summary>Skip document generation — used by the bulk importer, never by the wizard.</summary>
    public bool SuppressDocuments { get; set; }
}

/// <summary>
/// What the server says before anything is written: the price it computed, the plan it would
/// build, the approvals it will need, and anything that blocks. The wizard shows this on its last
/// step, so nobody presses Confirm and then discovers a discount needed a director.
/// </summary>
public class BookingPreviewDto
{
    public CostSheetDto CostSheet { get; set; } = new();
    public PaymentPlanPreviewDto PaymentPlan { get; set; } = new();

    public bool RequiresApproval { get; set; }
    public List<string> ApprovalsRequired { get; set; } = [];
    public GateResultDto Gate { get; set; } = new();

    public decimal CommissionEstimate { get; set; }
    public string? PartnerName { get; set; }
    public decimal EscrowPortion { get; set; }
    public decimal FreePortion { get; set; }
}

public class BookingApprovalDto
{
    public Guid BookingId { get; set; }
    public ApprovalOutcome Outcome { get; set; }
    public string? Comment { get; set; }
}

public class BookingAmendmentDto
{
    public Guid? Id { get; set; }
    public Guid BookingId { get; set; }
    public string AmendmentType { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string? BeforeValue { get; set; }
    public string? AfterValue { get; set; }
    public string? Reason { get; set; }
    public decimal Fee { get; set; }
    public DateOnly RequestedOn { get; set; }
    public string? RequestedByName { get; set; }
    public ApprovalOutcome Outcome { get; set; }
    public DateOnly? EffectiveFrom { get; set; }

    // Unit change
    public Guid? NewUnitId { get; set; }
    public string? NewUnitNumber { get; set; }
    public decimal? PriceDifference { get; set; }

    // Applicant change
    public Guid? PartyId { get; set; }
    public string? PartyName { get; set; }
}

// ── Allotment & ballot ───────────────────────────────────────────────────────

public class AllotmentDto
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string AllotmentNumber { get; set; } = string.Empty;
    public AllotmentStatus Status { get; set; }
    public DateOnly IssuedOn { get; set; }
    public string? IssuedByName { get; set; }
    public string? UnitNumber { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public DateOnly? PossessionTargetDate { get; set; }
    public string? DocumentUrl { get; set; }
    public int Version { get; set; }
    public Guid? SupersedesAllotmentId { get; set; }
    public string? ReissueReason { get; set; }
    public bool IsDelivered { get; set; }
    public DateOnly? DeliveredOn { get; set; }
    public string? DeliveryReference { get; set; }
}

public class BallotDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public BallotStatus Status { get; set; }
    public DateOnly? ScheduledOn { get; set; }
    public DateTime? DrawnAt { get; set; }
    public DateTime? PublishedAt { get; set; }

    public int EntryCount { get; set; }
    public int PrizeCount { get; set; }
    public int AllocatedCount { get; set; }
    public int UnallocatedCount { get; set; }

    public string? ConductedByName { get; set; }
    public string? WitnessNames { get; set; }
    public string? VideoUrl { get; set; }
    public string? ResultDocumentUrl { get; set; }
    public bool IsSupplementary { get; set; }

    /// <summary>Present once drawn, so anybody can re-run the draw and get the same result.</summary>
    public string? RandomSeed { get; set; }
    public string? PoolHash { get; set; }

    public List<BallotCategoryDto> Categories { get; set; } = [];
}

public class BallotCategoryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AreaDto NominalArea { get; set; } = new();
    public string QuotaType { get; set; } = "General";
    public int ReservedCount { get; set; }
    public int EntryCount { get; set; }
    public int PrizeCount { get; set; }
    public int AllocatedCount { get; set; }
    public int SortOrder { get; set; }
}

public class BallotEntryDto
{
    public Guid Id { get; set; }
    public Guid? BallotCategoryId { get; set; }
    public string? CategoryCode { get; set; }
    public Guid? PlotFileId { get; set; }
    public string? FileNumber { get; set; }
    public Guid? BookingId { get; set; }
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string? FatherOrGuardianName { get; set; }
    public string? IdentityNumber { get; set; }
    public int SequenceInPool { get; set; }
    public int? PriorityNumber { get; set; }
    public bool IsEligible { get; set; }
    public string? IneligibilityReason { get; set; }

    public Guid? AllottedUnitId { get; set; }
    public string? AllottedPlotNumber { get; set; }
    public int? DrawOrder { get; set; }
    public bool WasManuallyAssigned { get; set; }
    public string? OverrideReason { get; set; }
    public bool IsClaimed { get; set; }
    public DateOnly? ClaimDeadline { get; set; }
}

public class BallotCreateDto
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly? ScheduledOn { get; set; }
    public bool IsSupplementary { get; set; }
    public Guid? ParentBallotId { get; set; }
    public List<BallotCategoryUpsertDto> Categories { get; set; } = [];
}

public class BallotCategoryUpsertDto
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AreaUnit InputAreaUnit { get; set; } = AreaUnit.SquareFeet;
    public decimal NominalArea { get; set; }
    public string QuotaType { get; set; } = "General";
    public int ReservedCount { get; set; }
    public List<Guid> PrizeUnitIds { get; set; } = [];
    public int SortOrder { get; set; }
}

/// <summary>
/// Running the draw. The seed is either supplied (so a public ceremony can use a number everybody
/// watched being generated) or produced by the server and recorded — either way the draw is
/// reproducible, which is the only thing that makes it defensible.
/// </summary>
public class BallotDrawDto
{
    public Guid BallotId { get; set; }
    public string? Seed { get; set; }
    public string? WitnessNames { get; set; }
    public string? VideoUrl { get; set; }
    public bool DryRun { get; set; }
}

public class BallotResultDto
{
    public Guid BallotId { get; set; }
    public string Seed { get; set; } = string.Empty;
    public string PoolHash { get; set; } = string.Empty;
    public DateTime DrawnAt { get; set; }
    public int AllocatedCount { get; set; }
    public int UnallocatedEntries { get; set; }
    public int UnallocatedPrizes { get; set; }
    public List<BallotEntryDto> Results { get; set; } = [];
}

public class SaleAgreementDto
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public string AgreementNumber { get; set; } = string.Empty;
    public string AgreementType { get; set; } = string.Empty;
    public DateOnly? ExecutedOn { get; set; }
    public DateOnly? RegisteredOn { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? RegistrarOffice { get; set; }
    public decimal StampDuty { get; set; }
    public decimal RegistrationFee { get; set; }
    public string LanguageCode { get; set; } = "en";
    public string? DocumentUrl { get; set; }
    public bool IsSigned { get; set; }
    public bool IsSuperseded { get; set; }
    public Guid? SignatureSessionId { get; set; }
    public List<AgreementClauseDto> Clauses { get; set; } = [];
}

public class AgreementClauseDto
{
    public Guid? Id { get; set; }
    public string ClauseKey { get; set; } = string.Empty;
    public string? Heading { get; set; }
    public string Body { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsNonStandard { get; set; }
    public bool IsMandatory { get; set; }
}
