using RealEstate.Domain.Enums;

namespace RealEstate.Application.DTOs;

// =====================================================================================
// Brokerage deals, commission in both worlds, and the channel-partner network.
// =====================================================================================

public class DealListItemDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DealStatus Status { get; set; }
    public ListingKind Kind { get; set; }

    public Guid PropertyId { get; set; }
    public string PropertyReference { get; set; } = string.Empty;
    public string AddressOneLine { get; set; } = string.Empty;
    public string? HeroImageUrl { get; set; }

    public string BuyerName { get; set; } = string.Empty;
    public string? SellerName { get; set; }
    public string? ListingAgentName { get; set; }
    public string? SellingAgentName { get; set; }

    public decimal AgreedPrice { get; set; }
    public decimal GrossFee { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool DepositReceived { get; set; }
    public bool FeeInvoiced { get; set; }
    public bool FeeReceived { get; set; }

    public DateOnly AgreedOn { get; set; }
    public DateOnly? TargetExchangeDate { get; set; }
    public DateOnly? TargetCompletionDate { get; set; }
    public DateOnly? ActualCompletionDate { get; set; }
    public int DaysInProgress { get; set; }
    public int? DaysSinceLastMilestone { get; set; }

    /// <summary>Nothing has moved for too long. What the stalled-deal board sorts on.</summary>
    public bool IsStalled { get; set; }

    public int CompletedSteps { get; set; }
    public int TotalSteps { get; set; }
    public decimal ProgressPercent { get; set; }
    public string? NextStepLabel { get; set; }
    public DateOnly? NextStepDue { get; set; }
    public bool NextStepOverdue { get; set; }

    public Guid? ChainId { get; set; }
    public int? ChainPosition { get; set; }
    public bool ChainAtRisk { get; set; }
}

public class DealDetailDto : DealListItemDto
{
    public Guid? ListingId { get; set; }
    public Guid? OfferId { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? InstructionId { get; set; }
    public Guid BuyerPartyId { get; set; }
    public Guid? SellerPartyId { get; set; }

    public decimal DepositAmount { get; set; }
    public decimal FeePercent { get; set; }
    public decimal FeeTax { get; set; }
    public decimal ListingSideFee { get; set; }
    public decimal SellingSideFee { get; set; }
    public Guid? CommissionCalculationId { get; set; }

    public DateOnly? ActualExchangeDate { get; set; }
    public Guid? FallThroughRecordId { get; set; }
    public Guid? ConveyancingId { get; set; }
    public Guid? ResultingTenancyId { get; set; }
    public string? Notes { get; set; }

    public List<DealPartyDto> Parties { get; set; } = [];
    public List<DealChecklistItemDto> Checklist { get; set; } = [];
    public List<DealMilestoneDto> Milestones { get; set; } = [];
    public ConveyancingDto? Conveyancing { get; set; }
    public SalesChainDto? Chain { get; set; }
    public CommissionCalculationDto? Commission { get; set; }
    public List<GeneratedDocumentDto> Documents { get; set; } = [];
    public List<TimelineEntryDto> Timeline { get; set; } = [];
}

public class DealPartyDto
{
    public Guid? Id { get; set; }
    public DealPartyRole Role { get; set; }
    public Guid? PartyId { get; set; }
    public string? OrganisationName { get; set; }
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Reference { get; set; }
    public DateTime? LastContactedAt { get; set; }
    public bool IsResponsive { get; set; }
    public int? DaysSinceContact { get; set; }
    public string? Note { get; set; }
}

public class DealChecklistItemDto
{
    public Guid Id { get; set; }
    public string StepKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? OwnerName { get; set; }
    public DealPartyRole? ResponsibleParty { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateOnly? CompletedOn { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsMandatory { get; set; }
    public bool IsBlocking { get; set; }
    public bool IsOverdue { get; set; }
    public string? Note { get; set; }
}

public class DealMilestoneDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly? TargetDate { get; set; }
    public DateOnly? ActualDate { get; set; }
    public int? VarianceDays { get; set; }
    public string? DelayReason { get; set; }
    public int SortOrder { get; set; }
}

public class DealCreateDto
{
    public Guid PropertyId { get; set; }
    public Guid? ListingId { get; set; }
    public Guid? OfferId { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? InstructionId { get; set; }
    public ListingKind Kind { get; set; }
    public Guid BuyerPartyId { get; set; }
    public Guid? SellerPartyId { get; set; }
    public Guid? ListingAgentId { get; set; }
    public Guid? SellingAgentId { get; set; }
    public Guid? OfficeId { get; set; }

    public decimal AgreedPrice { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal DepositAmount { get; set; }
    public DateOnly AgreedOn { get; set; }
    public DateOnly? TargetExchangeDate { get; set; }
    public DateOnly? TargetCompletionDate { get; set; }

    /// <summary>Override the fee the instruction implies. Recorded as a deviation.</summary>
    public decimal? OverrideFeePercent { get; set; }
    public decimal? OverrideFeeAmount { get; set; }

    public List<DealPartyDto> Parties { get; set; } = [];
    public string? Notes { get; set; }
}

public class DealBoardDto
{
    public List<DealBoardColumnDto> Columns { get; set; } = [];
    public int TotalCount { get; set; }
    public decimal TotalValue { get; set; }
    public decimal TotalFee { get; set; }
    public int StalledCount { get; set; }
    public int ChainAtRiskCount { get; set; }
    public decimal FallThroughRatePercent { get; set; }
    public decimal AverageDaysToComplete { get; set; }
}

public class DealBoardColumnDto
{
    public DealStatus Status { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Value { get; set; }
    public decimal Fee { get; set; }
    public List<DealListItemDto> Items { get; set; } = [];
    public bool HasMore { get; set; }
}

public class SalesChainDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public int LinkCount { get; set; }
    public bool IsComplete { get; set; }
    public bool IsBroken { get; set; }
    public DateOnly? BrokenOn { get; set; }
    public int? BrokenAtPosition { get; set; }
    public DateOnly? TargetCompletionDate { get; set; }
    public List<SalesChainLinkDto> Links { get; set; } = [];
}

public class SalesChainLinkDto
{
    public Guid Id { get; set; }
    public int Position { get; set; }
    public Guid? DealId { get; set; }
    public string? DealReference { get; set; }
    public string? AddressOneLine { get; set; }
    public string? ExternalDescription { get; set; }
    public string? ExternalAgentName { get; set; }
    public string? ExternalAgentPhone { get; set; }
    public DealStatus Status { get; set; }
    public string? SolicitorName { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public bool IsHoldingUpChain { get; set; }
    public string? HoldUpReason { get; set; }
    public bool IsOurs { get; set; }
}

public class FallThroughRecordDto
{
    public Guid Id { get; set; }
    public Guid DealId { get; set; }
    public string DealReference { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string AddressOneLine { get; set; } = string.Empty;
    public FallThroughCause Cause { get; set; }
    public string? ReasonLabel { get; set; }
    public string? Detail { get; set; }
    public DateOnly OccurredOn { get; set; }
    public string? StageReached { get; set; }
    public int DaysInProgress { get; set; }
    public decimal CostIncurred { get; set; }
    public decimal LostFee { get; set; }
    public bool RelistedImmediately { get; set; }
    public bool PreviousViewersNotified { get; set; }
}

public class ConveyancingDto
{
    public Guid Id { get; set; }
    public Guid? DealId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid PropertyId { get; set; }
    public string? BuyerSolicitorName { get; set; }
    public string? SellerSolicitorName { get; set; }
    public DateOnly? DraftDeedOn { get; set; }
    public DateOnly? DeedApprovedOn { get; set; }

    public decimal ConsiderationValue { get; set; }
    public decimal? GovernmentValue { get; set; }
    public decimal StampDutyRate { get; set; }
    public decimal StampDutyAmount { get; set; }
    public decimal RegistrationFee { get; set; }
    public decimal WithholdingTax { get; set; }
    public decimal OtherLevies { get; set; }
    public decimal TotalTransactionCost { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public DateOnly? RegistrationAppointmentOn { get; set; }
    public string? TokenNumber { get; set; }
    public string? RegistrarOffice { get; set; }
    public DateOnly? RegisteredOn { get; set; }
    public string? DeedNumber { get; set; }
    public string? DeedUrl { get; set; }
    public DateOnly? MutationAppliedOn { get; set; }
    public string? MutationNumber { get; set; }
    public DateOnly? MutationCompletedOn { get; set; }
    public bool IsMutationComplete { get; set; }
    public string? Note { get; set; }
}

// ── Commission ───────────────────────────────────────────────────────────────

public class CommissionPlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public CommissionPlanKind Kind { get; set; }
    public CommissionTrigger Trigger { get; set; }
    public string AppliesTo { get; set; } = "Agent";
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }

    public decimal AgentSharePercent { get; set; }
    public decimal HouseSharePercent { get; set; }
    public decimal FixedFeePerDeal { get; set; }
    public decimal AnnualCapAmount { get; set; }
    public bool CapRollsOver { get; set; }
    public decimal PostCapFeePerDeal { get; set; }
    public decimal PostCapPercent { get; set; }
    public decimal FranchiseRoyaltyPercent { get; set; }
    public decimal TransactionFee { get; set; }
    public decimal MonthlyDeskFee { get; set; }

    public bool PayProRataWithCollection { get; set; }
    public decimal MinimumCollectionPercent { get; set; }
    public decimal WithholdingPercent { get; set; }
    public bool ClawBackOnCancellation { get; set; }

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public int AssignedCount { get; set; }
    public List<CommissionPlanTierDto> Tiers { get; set; } = [];
}

public class CommissionPlanTierDto
{
    public Guid? Id { get; set; }
    public int TierNumber { get; set; }
    public string? Label { get; set; }
    public decimal FromAmount { get; set; }
    public decimal? ToAmount { get; set; }
    public int? FromCount { get; set; }
    public int? ToCount { get; set; }
    public decimal SharePercent { get; set; }
    public decimal RatePerSqFt { get; set; }
    public decimal FixedAmount { get; set; }
    public bool IsRetrospective { get; set; }
}

public class CommissionCalculationDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid? DealId { get; set; }
    public string? DealReference { get; set; }
    public Guid? BookingId { get; set; }
    public string? BookingReference { get; set; }
    public Guid? TenancyId { get; set; }
    public string? ProjectName { get; set; }

    public CommissionTrigger Trigger { get; set; }
    public CommissionStatus Status { get; set; }
    public decimal TransactionValue { get; set; }
    public decimal GrossFee { get; set; }
    public decimal TaxOnFee { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetDistributable { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public DateOnly CalculatedOn { get; set; }
    public DateOnly? EarnedOn { get; set; }
    public DateOnly? DueOn { get; set; }
    public decimal CollectionPercentAtCalculation { get; set; }
    public Guid? DisbursementId { get; set; }
    public bool IsDisputed { get; set; }
    public string? DisputeNote { get; set; }

    /// <summary>Every step of the arithmetic, in words. An agent will read it line by line.</summary>
    public List<string> CalculationTrace { get; set; } = [];

    public List<CommissionSplitDto> Splits { get; set; } = [];
}

public class CommissionSplitDto
{
    public Guid Id { get; set; }
    public Guid? AgentProfileId { get; set; }
    public string? AgentName { get; set; }
    public Guid? SalesTeamId { get; set; }
    public string? TeamName { get; set; }
    public Guid? ChannelPartnerId { get; set; }
    public string? PartnerName { get; set; }
    public string? ReferrerName { get; set; }
    public string Role { get; set; } = "SellingAgent";

    public decimal BaseAmount { get; set; }
    public decimal SharePercent { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DeductionTotal { get; set; }
    public decimal WithholdingAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public CommissionStatus Status { get; set; }
    public int? TierApplied { get; set; }
    public bool CapReached { get; set; }
    public decimal CapContribution { get; set; }
    public List<CommissionDeductionDto> Deductions { get; set; } = [];
}

public class CommissionDeductionDto
{
    public Guid Id { get; set; }
    public DeductionKind Kind { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Percent { get; set; }
    public decimal Amount { get; set; }
    public string? PayableToName { get; set; }
    public string? Note { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>The commission disbursement authorisation — exactly who gets what from this deal.</summary>
public class CommissionDisbursementDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid CommissionCalculationId { get; set; }
    public Guid? DealId { get; set; }
    public Guid? BookingId { get; set; }
    public string? Subject { get; set; }
    public DateOnly IssuedOn { get; set; }
    public decimal GrossFee { get; set; }
    public decimal TotalDisbursed { get; set; }
    public decimal HouseRetained { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string? PreparedByName { get; set; }
    public ApprovalOutcome Outcome { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? DocumentUrl { get; set; }
    public bool FromClientAccount { get; set; }
    public List<CommissionSplitDto> Splits { get; set; } = [];
}

public class CommissionPayoutDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid? AgentProfileId { get; set; }
    public string? AgentName { get; set; }
    public Guid? ChannelPartnerId { get; set; }
    public string? PartnerName { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly PaidOn { get; set; }

    public decimal GrossAmount { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal WithholdingAmount { get; set; }
    public decimal AdvanceRecovered { get; set; }
    public decimal ClawbackAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public PaymentInstrument Instrument { get; set; }
    public string? PaymentReference { get; set; }
    public bool PaidViaPayroll { get; set; }
    public string? StatementUrl { get; set; }
    public bool IsPaid { get; set; }
    public List<CommissionPayoutLineDto> Lines { get; set; } = [];
}

public class CommissionPayoutLineDto
{
    public Guid Id { get; set; }
    public Guid? CommissionSplitId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsClawback { get; set; }
}

/// <summary>An agent's cap position — the number they check constantly and dispute if it moves.</summary>
public class AgentCapLedgerDto
{
    public Guid AgentProfileId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public decimal CapAmount { get; set; }
    public decimal ContributedAmount { get; set; }
    public decimal RemainingToCap { get; set; }
    public decimal PercentToCap { get; set; }
    public bool CapReached { get; set; }
    public DateOnly? CapReachedOn { get; set; }
    public decimal RolledOverAmount { get; set; }
    public decimal GrossCommissionEarned { get; set; }
    public decimal NetCommissionEarned { get; set; }
    public int DealCount { get; set; }
    public decimal TransactionVolume { get; set; }
    public string CurrencyCode { get; set; } = "USD";
}

// ── Channel partners ─────────────────────────────────────────────────────────

public class ChannelPartnerListItemDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? TradingName { get; set; }
    public PartnerStatus Status { get; set; }
    public string? TierName { get; set; }
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? City { get; set; }
    public string? RelationshipManagerName { get; set; }
    public DateOnly? OnboardedOn { get; set; }

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
    public string CurrencyCode { get; set; } = "USD";

    public int AuthorisedProjectCount { get; set; }
    public bool HasExpiredDocuments { get; set; }
    public bool LicenceExpiring { get; set; }
}

public class ChannelPartnerDetailDto : ChannelPartnerListItemDto
{
    public Guid? PartyId { get; set; }
    public string? AddressLine { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? LicenceNumber { get; set; }
    public DateOnly? LicenceExpiresOn { get; set; }
    public string? TaxNumber { get; set; }
    public string? BankName { get; set; }
    public string? AccountTitle { get; set; }
    public string? AccountNumber { get; set; }
    public bool BankDetailsVerified { get; set; }
    public decimal WithholdingPercent { get; set; }
    public DateOnly? SuspendedOn { get; set; }
    public string? SuspensionReason { get; set; }
    public string? Notes { get; set; }

    public List<PartnerUserDto> Users { get; set; } = [];
    public List<PartnerAuthorisationDto> Authorisations { get; set; } = [];
    public List<ChecklistItemDto> Documents { get; set; } = [];
    public List<LeadRegistrationDto> RecentRegistrations { get; set; } = [];
    public List<BookingListItemDto> Bookings { get; set; } = [];
    public List<PartnerStatementDto> Statements { get; set; } = [];
    public List<PartnerAdvanceDto> Advances { get; set; } = [];
    public List<TimelineEntryDto> Timeline { get; set; } = [];
}

public class ChannelPartnerUpsertDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TradingName { get; set; }
    public Guid? PartyId { get; set; }
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
    public decimal WithholdingPercent { get; set; }
    public Guid? RelationshipManagerUserId { get; set; }
    public string? Notes { get; set; }
    public List<PartnerAuthorisationDto> Authorisations { get; set; } = [];
}

public class PartnerUserDto
{
    public Guid? Id { get; set; }
    public Guid? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Designation { get; set; }
    public bool IsPrimary { get; set; }
    public bool CanViewCommission { get; set; }
    public bool CanRegisterLeads { get; set; }
    public bool CanBookVisits { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int LeadsRegistered { get; set; }
    public int BookingsMade { get; set; }
}

public class PartnerAuthorisationDto
{
    public Guid? Id { get; set; }
    public Guid ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public Guid? CommissionPlanId { get; set; }
    public string? CommissionPlanName { get; set; }
    public Guid? TerritoryId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public int? MaxBookings { get; set; }
    public int BookingsMade { get; set; }
    public bool CanSeePrices { get; set; }
    public bool CanHoldUnits { get; set; }
    public bool IsActive { get; set; }
}

public class PartnerTierDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int Level { get; set; }
    public decimal MinBookingValue { get; set; }
    public int MinBookingCount { get; set; }
    public decimal CommissionUpliftPercent { get; set; }
    public bool PriorityAllocation { get; set; }
    public int LeadValidityDays { get; set; }
    public string? Benefits { get; set; }
    public bool AutoPromote { get; set; }
    public int PartnerCount { get; set; }
}

public class LeadRegistrationDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid ChannelPartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public string? PartnerUserName { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;

    public string ProspectName { get; set; } = string.Empty;
    public string ProspectPhone { get; set; } = string.Empty;
    public string? ProspectEmail { get; set; }
    public string? ProspectIdentityNumber { get; set; }

    public LeadRegistrationStatus Status { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int DaysRemaining { get; set; }
    public bool IsExpiringSoon { get; set; }

    public Guid? ConflictsWithRegistrationId { get; set; }
    public string? ConflictsWithPartnerName { get; set; }
    public string? RejectionReason { get; set; }

    public Guid? EnquiryId { get; set; }
    public Guid? SiteVisitId { get; set; }
    public Guid? BookingId { get; set; }
    public DateOnly? ConvertedOn { get; set; }
    public int ExtensionCount { get; set; }
    public string? Note { get; set; }
}

public class LeadRegistrationCreateDto
{
    public Guid ChannelPartnerId { get; set; }
    public Guid? PartnerUserId { get; set; }
    public Guid ProjectId { get; set; }
    public string ProspectName { get; set; } = string.Empty;
    public string ProspectPhone { get; set; } = string.Empty;
    public string? ProspectEmail { get; set; }
    public string? ProspectIdentityNumber { get; set; }
    public string? Note { get; set; }
}

public class PartnerCommissionRateDto
{
    public Guid? Id { get; set; }
    public Guid? ChannelPartnerId { get; set; }
    public Guid? PartnerTierId { get; set; }
    public string? TierName { get; set; }
    public Guid ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public PropertySubType? SubType { get; set; }
    public decimal FromValue { get; set; }
    public decimal? ToValue { get; set; }
    public decimal CommissionPercent { get; set; }
    public decimal RatePerSqFt { get; set; }
    public decimal FlatAmount { get; set; }
    public CommissionTrigger Trigger { get; set; }
    public decimal ReleaseAtCollectionPercent { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
}

public class PartnerCommissionEntryDto
{
    public Guid Id { get; set; }
    public Guid ChannelPartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public Guid BookingId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string? UnitNumber { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;

    public decimal BookingValue { get; set; }
    public decimal CommissionPercent { get; set; }
    public decimal GrossCommission { get; set; }
    public decimal CollectionPercent { get; set; }
    public decimal EarnedAmount { get; set; }
    public decimal WithholdingAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal ClawedBackAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public CommissionStatus Status { get; set; }
    public DateOnly AccruedOn { get; set; }
    public DateOnly? LastPaidOn { get; set; }
    public string? Note { get; set; }
}

public class PartnerStatementDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid ChannelPartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
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
    public string CurrencyCode { get; set; } = "USD";
    public int BookingCount { get; set; }
    public string? DocumentUrl { get; set; }
    public bool IsPublishedToPortal { get; set; }
    public bool IsAcknowledged { get; set; }
    public List<PartnerCommissionEntryDto> Entries { get; set; } = [];
}

public class PartnerAdvanceDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly AdvancedOn { get; set; }
    public string? Purpose { get; set; }
    public decimal RecoveryPercent { get; set; }
    public decimal RecoveredAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public DateOnly? FullyRecoveredOn { get; set; }
    public bool IsWrittenOff { get; set; }
}

public class PartnerContestDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public string MetricKey { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public string? PrizeDescription { get; set; }
    public decimal? PrizeAmount { get; set; }
    public bool IsPublished { get; set; }
    public bool IsClosed { get; set; }
    public string? WinnerPartnerName { get; set; }
    public string? Rules { get; set; }
    public List<ContestLeaderboardRowDto> Leaderboard { get; set; } = [];
}

public class ContestLeaderboardRowDto
{
    public int Rank { get; set; }
    public Guid ChannelPartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal PercentOfTarget { get; set; }
    public int BookingCount { get; set; }
}
