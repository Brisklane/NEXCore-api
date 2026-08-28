using RealEstate.Domain.Enums;

namespace RealEstate.Application.DTOs;

// =====================================================================================
// Tenancies, the rent roll, service charges, landlords and client money.
// =====================================================================================

public class TenancyListItemDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public TenancyKind Kind { get; set; }
    public TenancyStatus Status { get; set; }

    public Guid PropertyId { get; set; }
    public string PropertyReference { get; set; } = string.Empty;
    public string AddressOneLine { get; set; } = string.Empty;
    public string? UnitNumber { get; set; }
    public string? BuildingName { get; set; }
    public PropertySubType? SubType { get; set; }
    public AreaDto? Area { get; set; }

    public string TenantName { get; set; } = string.Empty;
    public string? TenantPhone { get; set; }
    public Guid? LandlordId { get; set; }
    public string? LandlordName { get; set; }
    public string? ManagedByName { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int? DaysToExpiry { get; set; }
    public bool IsExpiringSoon { get; set; }

    public decimal Rent { get; set; }
    public RentFrequency Frequency { get; set; }
    public decimal? AnnualRent { get; set; }
    public decimal? RentPerSqFt { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public decimal DepositAmount { get; set; }
    public bool DepositRegistered { get; set; }
    public bool DepositRegistrationOverdue { get; set; }

    public decimal ArrearsAmount { get; set; }
    public int DaysInArrears { get; set; }
    public decimal MonthsInArrears { get; set; }
    public DateOnly? NextDueDate { get; set; }

    public ManagementService ManagementService { get; set; }
    public decimal ManagementFeePercent { get; set; }
    public bool ServiceChargeApplies { get; set; }
    public bool TurnoverRentApplies { get; set; }
    public int OpenComplianceIssues { get; set; }
    public int CriticalDatesDue { get; set; }
}

public class TenancyDetailDto : TenancyListItemDto
{
    public Guid? UnitId { get; set; }
    public Guid? InstructionId { get; set; }
    public int? TermMonths { get; set; }
    public bool RollsToPeriodic { get; set; }
    public DateOnly? ActualEndDate { get; set; }

    public int PaymentDay { get; set; }
    public bool PaidInAdvance { get; set; }
    public EscalationKind Escalation { get; set; }
    public decimal EscalationPercent { get; set; }
    public int EscalationMonths { get; set; }
    public DateOnly? NextEscalationDate { get; set; }

    public decimal AdvanceRentMonths { get; set; }
    public decimal TotalCharged { get; set; }
    public decimal TotalPaid { get; set; }

    public ApportionmentBasis? ServiceChargeBasis { get; set; }
    public decimal ServiceChargePercent { get; set; }
    public bool UtilitiesRecharged { get; set; }
    public bool PropertyTaxRecharged { get; set; }
    public bool InsuranceRecharged { get; set; }
    public decimal RepairAuthorityLimit { get; set; }

    public bool PetsAllowed { get; set; }
    public bool SmokingAllowed { get; set; }
    public bool SublettingAllowed { get; set; }
    public int? MaxOccupants { get; set; }
    public string? PermittedUse { get; set; }
    public int NoticePeriodDaysTenant { get; set; }
    public int NoticePeriodDaysLandlord { get; set; }

    public string? AgreementUrl { get; set; }
    public DateOnly? SignedOn { get; set; }
    public bool IsRegistered { get; set; }
    public string? RegistrationNumber { get; set; }
    public Guid? PreviousTenancyId { get; set; }
    public string? Notes { get; set; }

    public List<TenancyPartyDto> Parties { get; set; } = [];
    public List<RentChargeDto> RentCharges { get; set; } = [];
    public List<LeaseOptionDto> Options { get; set; } = [];
    public List<CriticalDateDto> CriticalDates { get; set; } = [];
    public List<EscalationRuleDto> Escalations { get; set; } = [];
    public List<RecoveryChargeDto> Recoveries { get; set; } = [];
    public SecurityDepositDto? Deposit { get; set; }
    public ReferencingCaseDto? Referencing { get; set; }
    public TurnoverRentTermDto? TurnoverRent { get; set; }
    public List<MoveInspectionDto> Inspections { get; set; } = [];
    public List<ComplianceCertificateDto> Certificates { get; set; } = [];
    public List<WorkOrderListItemDto> WorkOrders { get; set; } = [];
    public List<CustomerLedgerEntryDto> Ledger { get; set; } = [];
    public List<TimelineEntryDto> Timeline { get; set; } = [];
    public AgeingBucketsDto Ageing { get; set; } = new();
}

public class TenancyPartyDto
{
    public Guid? Id { get; set; }
    public Guid PartyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string Role { get; set; } = "Tenant";
    public bool IsLeadTenant { get; set; }
    public bool IsJointlyAndSeverallyLiable { get; set; }
    public decimal LiabilitySharePercent { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public KycStatus KycStatus { get; set; }
    public ReferencingOutcome? ReferencingOutcome { get; set; }
}

public class TenancyCreateDto
{
    public Guid PropertyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? LandlordId { get; set; }
    public Guid? InstructionId { get; set; }
    public Guid? OfficeId { get; set; }
    public Guid? ManagedByUserId { get; set; }
    public Guid? EnquiryId { get; set; }

    public TenancyKind Kind { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int? TermMonths { get; set; }
    public bool RollsToPeriodic { get; set; } = true;

    public decimal Rent { get; set; }
    public RentFrequency Frequency { get; set; } = RentFrequency.Monthly;
    public string? CurrencyCode { get; set; }
    public int PaymentDay { get; set; } = 1;
    public bool PaidInAdvance { get; set; } = true;

    public EscalationKind Escalation { get; set; }
    public decimal EscalationPercent { get; set; }
    public int EscalationMonths { get; set; } = 12;

    public decimal DepositAmount { get; set; }
    public DepositScheme DepositScheme { get; set; }
    public decimal AdvanceRentMonths { get; set; }

    public ManagementService ManagementService { get; set; }
    public decimal ManagementFeePercent { get; set; }
    public decimal RepairAuthorityLimit { get; set; }

    public bool ServiceChargeApplies { get; set; }
    public ApportionmentBasis? ServiceChargeBasis { get; set; }
    public decimal ServiceChargePercent { get; set; }
    public bool TurnoverRentApplies { get; set; }
    public bool UtilitiesRecharged { get; set; }
    public bool PropertyTaxRecharged { get; set; }
    public bool InsuranceRecharged { get; set; }

    public bool PetsAllowed { get; set; }
    public bool SmokingAllowed { get; set; }
    public bool SublettingAllowed { get; set; }
    public int? MaxOccupants { get; set; }
    public string? PermittedUse { get; set; }
    public int NoticePeriodDaysTenant { get; set; } = 30;
    public int NoticePeriodDaysLandlord { get; set; } = 60;

    public List<TenancyPartyDto> Parties { get; set; } = [];
    public List<LeaseOptionDto> Options { get; set; } = [];
    public List<RecoveryChargeDto> Recoveries { get; set; } = [];
    public TurnoverRentTermDto? TurnoverRent { get; set; }
    public List<LeaseConcessionDto> Concessions { get; set; } = [];

    /// <summary>Generate the whole rent schedule now, so the term is visible in advance.</summary>
    public bool GenerateSchedule { get; set; } = true;

    public string? Notes { get; set; }
}

public class RentChargeDto
{
    public Guid Id { get; set; }
    public int SequenceNumber { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public InstalmentStatus Status { get; set; }
    public int DaysOverdue { get; set; }
    public decimal LateFeeAccrued { get; set; }
    public bool IsProRated { get; set; }
    public bool IsRentFree { get; set; }
    public DateOnly? SettledOn { get; set; }
}

public class EscalationRuleDto
{
    public Guid? Id { get; set; }
    public EscalationKind Kind { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public decimal Percent { get; set; }
    public decimal FixedAmount { get; set; }
    public string? IndexName { get; set; }
    public decimal? FloorPercent { get; set; }
    public decimal? CapPercent { get; set; }
    public int IntervalMonths { get; set; }
    public bool IsCompounding { get; set; }
    public bool IsApplied { get; set; }
    public DateOnly? AppliedOn { get; set; }
    public decimal? ResultingRent { get; set; }
    public int SortOrder { get; set; }
}

public class RentReviewDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid TenancyId { get; set; }
    public string TenancyReference { get; set; } = string.Empty;
    public string? AddressOneLine { get; set; }
    public DateOnly ReviewDate { get; set; }
    public DateOnly? NoticeDeadline { get; set; }
    public bool NoticeServed { get; set; }
    public DateOnly? NoticeServedOn { get; set; }
    public bool NoticeDeadlineMissed { get; set; }
    public decimal CurrentRent { get; set; }
    public decimal? ProposedRent { get; set; }
    public decimal? CounterProposedRent { get; set; }
    public decimal? AgreedRent { get; set; }
    public decimal? UpliftPercent { get; set; }
    public DateOnly? AgreedOn { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public string? Outcome { get; set; }
    public bool IsUpwardOnly { get; set; }
    public string? MemorandumUrl { get; set; }
    public string? Note { get; set; }
}

public class LeaseOptionDto
{
    public Guid? Id { get; set; }
    public LeaseOptionKind Kind { get; set; }
    public string HeldBy { get; set; } = "Tenant";
    public DateOnly OptionDate { get; set; }
    public DateOnly NoticeWindowFrom { get; set; }
    public DateOnly NoticeWindowTo { get; set; }
    public int NoticeMonths { get; set; }
    public string? Conditions { get; set; }
    public decimal? OptionPrice { get; set; }
    public decimal? PenaltyAmount { get; set; }
    public bool IsExercised { get; set; }
    public DateOnly? ExercisedOn { get; set; }
    public bool IsLapsed { get; set; }
    public bool IsWindowOpen { get; set; }
    public int? DaysToWindowClose { get; set; }
}

public class CriticalDateDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string DateType { get; set; } = string.Empty;
    public Guid? TenancyId { get; set; }
    public string? TenancyReference { get; set; }
    public Guid? PropertyId { get; set; }
    public string? AddressOneLine { get; set; }
    public DateOnly DueDate { get; set; }
    public int DaysToDue { get; set; }
    public int AlertDaysBefore { get; set; }
    public string? OwnerName { get; set; }
    public AlertSeverity Severity { get; set; }
    public bool AlertSent { get; set; }
    public bool IsActioned { get; set; }
    public bool IsOverdue { get; set; }
    public string? Note { get; set; }
    public string? Route { get; set; }
}

public class TenancyRenewalDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid TenancyId { get; set; }
    public string TenancyReference { get; set; } = string.Empty;
    public string AddressOneLine { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public DateOnly ExpiryDate { get; set; }
    public int DaysToExpiry { get; set; }
    public DateOnly? OfferedOn { get; set; }
    public decimal CurrentRent { get; set; }
    public decimal? ProposedRent { get; set; }
    public decimal? AgreedRent { get; set; }
    public decimal? UpliftPercent { get; set; }
    public int? ProposedTermMonths { get; set; }
    public string Status { get; set; } = "Offered";
    public DateOnly? RespondedOn { get; set; }
    public Guid? NewTenancyId { get; set; }
    public string? DeclineReason { get; set; }
    public bool LandlordApproved { get; set; }

    /// <summary>Annual rent at risk if this one walks. What the renewal pipeline sorts on.</summary>
    public decimal ValueAtRisk { get; set; }

    public string? Note { get; set; }
}

public class ReferencingCaseDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public Guid? TenancyId { get; set; }
    public Guid? PropertyId { get; set; }
    public string? AddressOneLine { get; set; }
    public ReferencingOutcome Outcome { get; set; }
    public DateOnly StartedOn { get; set; }
    public DateOnly? CompletedOn { get; set; }
    public string? AssignedToName { get; set; }
    public decimal? DeclaredIncome { get; set; }
    public decimal? IncomeMultiple { get; set; }
    public bool GuarantorRequired { get; set; }
    public string? Conditions { get; set; }
    public string? FailureReason { get; set; }
    public int DaysOpen { get; set; }
    public List<ReferencingCheckDto> Checks { get; set; } = [];
}

public class ReferencingCheckDto
{
    public Guid? Id { get; set; }
    public ReferencingCheckKind Kind { get; set; }
    public ReferencingOutcome Outcome { get; set; }
    public DateOnly? RequestedOn { get; set; }
    public DateOnly? CompletedOn { get; set; }
    public string? RefereeName { get; set; }
    public string? RefereeContact { get; set; }
    public string? Findings { get; set; }
    public string? DocumentUrl { get; set; }
    public bool IsMandatory { get; set; }
    public DateOnly? RecheckDue { get; set; }
}

public class SecurityDepositDto
{
    public Guid Id { get; set; }
    public Guid TenancyId { get; set; }
    public string TenancyReference { get; set; } = string.Empty;
    public string? AddressOneLine { get; set; }
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public DateOnly ReceivedOn { get; set; }
    public DepositScheme Scheme { get; set; }
    public string? SchemeName { get; set; }
    public string? RegistrationReference { get; set; }
    public DateOnly? RegistrationDeadline { get; set; }
    public DateOnly? RegisteredOn { get; set; }
    public bool IsRegistered { get; set; }

    /// <summary>Late registration is a multiple-of-deposit penalty in some markets. Counted down.</summary>
    public int? DaysToRegistrationDeadline { get; set; }
    public bool RegistrationOverdue { get; set; }

    public bool PrescribedInformationServed { get; set; }
    public DateOnly? PrescribedInformationServedOn { get; set; }
    public decimal DeductionTotal { get; set; }
    public decimal ReturnedToTenant { get; set; }
    public decimal PaidToLandlord { get; set; }
    public DateOnly? ReleasedOn { get; set; }
    public bool IsDisputed { get; set; }
    public string? DisputeReference { get; set; }
    public string? DisputeOutcome { get; set; }
    public List<DepositDeductionDto> Deductions { get; set; } = [];
}

public class DepositDeductionDto
{
    public Guid? Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal ProposedAmount { get; set; }
    public decimal AgreedAmount { get; set; }
    public string? EvidenceUrl { get; set; }
    public Guid? InspectionFindingId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public string TenantResponse { get; set; } = "Pending";
    public DateOnly? RespondedOn { get; set; }
    public string? AdjudicatorNote { get; set; }
}

public class MoveInspectionDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string? AddressOneLine { get; set; }
    public Guid? TenancyId { get; set; }
    public InspectionKind Kind { get; set; }
    public DateTime InspectedAt { get; set; }
    public string? InspectorName { get; set; }
    public bool TenantPresent { get; set; }
    public bool LandlordPresent { get; set; }
    public string? OverallCondition { get; set; }
    public string? CleanlinessRating { get; set; }
    public string? ReportUrl { get; set; }
    public Guid? ComparedToInspectionId { get; set; }
    public bool TenantDisputed { get; set; }
    public string? TenantComments { get; set; }
    public DateOnly? TenantResponseDeadline { get; set; }
    public bool IsFinalised { get; set; }
    public int ItemCount { get; set; }
    public int DeterioratedCount { get; set; }
    public decimal? EstimatedDeductions { get; set; }
    public List<InspectionRoomItemDto> Items { get; set; } = [];
}

public class InspectionRoomItemDto
{
    public Guid? Id { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public ConditionGrade Condition { get; set; }
    public string? CleanlinessGrade { get; set; }
    public string? Note { get; set; }
    public List<string> PhotoUrls { get; set; } = [];
    public ConditionGrade? PreviousCondition { get; set; }
    public bool HasDeteriorated { get; set; }
    public bool IsFairWearAndTear { get; set; }
    public decimal? EstimatedCost { get; set; }
    public int SortOrder { get; set; }
}

public class TenancyNoticeDto
{
    public Guid Id { get; set; }
    public string NoticeNumber { get; set; } = string.Empty;
    public Guid TenancyId { get; set; }
    public string NoticeType { get; set; } = string.Empty;
    public string ServedBy { get; set; } = "Landlord";
    public DateOnly ServedOn { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public int NoticePeriodDays { get; set; }
    public string? Grounds { get; set; }
    public string? ServiceMethod { get; set; }
    public string? ServiceEvidenceUrl { get; set; }
    public bool IsAcknowledged { get; set; }
    public bool IsWithdrawn { get; set; }
    public bool IsValid { get; set; }
    public string? ValidityNote { get; set; }
    public string? DocumentUrl { get; set; }
}

public class ComplianceCertificateDto
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string? AddressOneLine { get; set; }
    public Guid? TenancyId { get; set; }
    public ComplianceCertificateKind Kind { get; set; }
    public string? CertificateNumber { get; set; }
    public DateOnly IssuedOn { get; set; }
    public DateOnly ExpiresOn { get; set; }
    public int DaysToExpiry { get; set; }
    public bool IsExpired { get; set; }
    public bool IsExpiringSoon { get; set; }
    public string? IssuerName { get; set; }
    public string? IssuerRegistration { get; set; }
    public decimal? Cost { get; set; }
    public CostBearer BorneBy { get; set; }
    public string? DocumentUrl { get; set; }
    public bool ServedToTenant { get; set; }
    public bool HasFailures { get; set; }
    public string? Findings { get; set; }
    public Guid? RemedialWorkOrderId { get; set; }
    public bool RenewalBooked { get; set; }
    public bool IsCurrent { get; set; }
}

// ── Rent roll ────────────────────────────────────────────────────────────────

/// <summary>
/// The estate manager's home screen: every unit, its tenant, its rent, its arrears and its expiry
/// in one grid that reconciles to the ledger.
/// </summary>
public class RentRollDto
{
    public Guid? PropertyId { get; set; }
    public string? PropertyName { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public DateOnly AsOfDate { get; set; }

    public List<RentRollRowDto> Rows { get; set; } = [];

    public int TotalUnits { get; set; }
    public int OccupiedUnits { get; set; }
    public int VacantUnits { get; set; }
    public decimal OccupancyPercent { get; set; }
    public AreaDto TotalArea { get; set; } = new();
    public AreaDto OccupiedArea { get; set; } = new();

    public decimal MonthlyRent { get; set; }
    public decimal AnnualRent { get; set; }
    public decimal AverageRentPerSqFt { get; set; }
    public decimal TotalArrears { get; set; }
    public decimal TotalDeposits { get; set; }
    public decimal PassingRentVsMarket { get; set; }

    public AgeingBucketsDto Ageing { get; set; } = new();
    public List<BreakdownSliceDto> ExpiryProfile { get; set; } = [];
    public List<BreakdownSliceDto> TenantMix { get; set; } = [];
}

public class RentRollRowDto
{
    public Guid? UnitId { get; set; }
    public Guid PropertyId { get; set; }
    public string UnitLabel { get; set; } = string.Empty;
    public string? FloorLabel { get; set; }
    public AreaDto Area { get; set; } = new();
    public PropertySubType? SubType { get; set; }

    public Guid? TenancyId { get; set; }
    public string? TenantName { get; set; }
    public string? TenantCategory { get; set; }
    public TenancyStatus? Status { get; set; }

    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int? MonthsRemaining { get; set; }

    public decimal Rent { get; set; }
    public RentFrequency? Frequency { get; set; }
    public decimal AnnualRent { get; set; }
    public decimal RentPerSqFt { get; set; }
    public decimal ServiceCharge { get; set; }
    public decimal OtherRecoveries { get; set; }
    public decimal TotalIncome { get; set; }

    public decimal Deposit { get; set; }
    public decimal Arrears { get; set; }
    public int DaysInArrears { get; set; }

    public EscalationKind? Escalation { get; set; }
    public DateOnly? NextReviewDate { get; set; }
    public DateOnly? NextBreakDate { get; set; }
    public bool IsVacant { get; set; }
    public int? DaysVoid { get; set; }
    public decimal? AskingRent { get; set; }
    public bool TurnoverRentApplies { get; set; }
    public decimal? LastDeclaredSales { get; set; }
}

public class RentRunDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string? PropertyName { get; set; }
    public DateOnly RunDate { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public bool IsDryRun { get; set; }
    public int CandidateCount { get; set; }
    public int ChargedCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? RunByName { get; set; }
    public string? ErrorSummary { get; set; }
    public bool IsCompleted { get; set; }
    public List<RentRunLineDto> Lines { get; set; } = [];
}

public class RentRunLineDto
{
    public Guid Id { get; set; }
    public Guid TenancyId { get; set; }
    public string TenancyReference { get; set; } = string.Empty;
    public string? AddressOneLine { get; set; }
    public string? TenantName { get; set; }
    public decimal Amount { get; set; }
    public bool WasSkipped { get; set; }
    public string? SkipReason { get; set; }
    public bool Failed { get; set; }
    public string? FailureReason { get; set; }
}

public class RentRunRequestDto
{
    public Guid? PropertyId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? OfficeId { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public bool IsDryRun { get; set; } = true;
    public List<Guid> ExcludeTenancyIds { get; set; } = [];
    public bool IncludeRecoveries { get; set; } = true;
}

public class ArrearsCaseDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid TenancyId { get; set; }
    public string TenancyReference { get; set; } = string.Empty;
    public string? AddressOneLine { get; set; }
    public Guid PartyId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateOnly OpenedOn { get; set; }
    public decimal ArrearsAmount { get; set; }
    public decimal LateFeeAmount { get; set; }
    public int DaysInArrears { get; set; }
    public decimal MonthsInArrears { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string? AssignedToName { get; set; }
    public Guid? ActivePromiseId { get; set; }
    public DateOnly? PromisedDate { get; set; }
    public bool LandlordNotified { get; set; }
    public Guid? NoticeId { get; set; }
    public bool ReferredToLegal { get; set; }
    public int DunningStep { get; set; }
    public bool IsClosed { get; set; }
    public string? Outcome { get; set; }
}

// ── Service charge ───────────────────────────────────────────────────────────

public class ServiceChargeBudgetDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid? PropertyId { get; set; }
    public string? PropertyName { get; set; }
    public Guid? SocietyId { get; set; }
    public int FinancialYear { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }

    public decimal TotalBudget { get; set; }
    public decimal TotalActual { get; set; }
    public decimal TotalBilled { get; set; }
    public decimal Variance { get; set; }
    public decimal VariancePercent { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal ManagementFeePercent { get; set; }

    public AreaDto TotalGrossLettableArea { get; set; } = new();
    public AreaDto OccupiedArea { get; set; } = new();
    public decimal OccupancyPercent { get; set; }

    public int? BaseYear { get; set; }
    public bool GrossUpEnabled { get; set; }
    public decimal GrossUpToOccupancyPercent { get; set; }
    public decimal? AnnualCapPercent { get; set; }
    public decimal? CumulativeCapPercent { get; set; }

    public bool IsApproved { get; set; }
    public DateOnly? ApprovedOn { get; set; }
    public bool IsReconciled { get; set; }
    public string? DocumentUrl { get; set; }
    public List<ServiceChargeBudgetLineDto> Lines { get; set; } = [];
}

public class ServiceChargeBudgetLineDto
{
    public Guid? Id { get; set; }
    public ServiceChargeHead Head { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal BudgetAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal VarianceAmount { get; set; }
    public decimal VariancePercent { get; set; }
    public ApportionmentBasis Basis { get; set; }
    public bool IsVariableCost { get; set; }
    public bool IsExcludedFromRecovery { get; set; }
    public string? ExclusionReason { get; set; }
    public bool IsCapped { get; set; }
    public decimal? CapAmount { get; set; }
    public decimal GrossedUpAmount { get; set; }
    public decimal RecoverableAmount { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// The year-end true-up with its working kept: budget, actual, gross-up, caps, exclusions, and
/// the balancing charge or credit per tenant.
/// </summary>
public class ServiceChargeReconciliationDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid ServiceChargeBudgetId { get; set; }
    public string? PropertyName { get; set; }
    public int FinancialYear { get; set; }
    public DateOnly ReconciledOn { get; set; }

    public decimal TotalBudget { get; set; }
    public decimal TotalActual { get; set; }
    public decimal TotalGrossedUp { get; set; }
    public decimal TotalExcluded { get; set; }
    public decimal TotalCapAdjustment { get; set; }
    public decimal TotalRecoverable { get; set; }
    public decimal TotalBilledOnAccount { get; set; }
    public decimal NetDifference { get; set; }
    public ReconciliationOutcome Outcome { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public bool IsAudited { get; set; }
    public string? AuditorName { get; set; }
    public DateOnly? AuditedOn { get; set; }
    public string? StatementUrl { get; set; }
    public bool IsIssuedToTenants { get; set; }
    public bool IsFinalised { get; set; }

    /// <summary>Each step of the calculation in words, so a tenant can challenge it line by line.</summary>
    public List<string> Workings { get; set; } = [];

    public List<ServiceChargeBudgetLineDto> HeadBreakdown { get; set; } = [];
    public List<ReconciliationLineDto> TenantLines { get; set; } = [];
}

public class ReconciliationLineDto
{
    public Guid Id { get; set; }
    public Guid? TenancyId { get; set; }
    public string? TenantName { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitLabel { get; set; }
    public decimal SharePercent { get; set; }
    public decimal RecoverableShare { get; set; }
    public decimal BilledOnAccount { get; set; }
    public decimal Difference { get; set; }
    public decimal CapAdjustment { get; set; }
    public ReconciliationOutcome Outcome { get; set; }
    public Guid? BalancingInvoiceId { get; set; }
    public Guid? CreditNoteId { get; set; }
    public bool IsDisputed { get; set; }
}

public class ApportionmentScheduleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? PropertyId { get; set; }
    public ApportionmentBasis Basis { get; set; }
    public ServiceChargeHead? AppliesToHead { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public decimal TotalPercent { get; set; }
    public bool IsBalanced { get; set; }
    public List<ApportionmentLineDto> Lines { get; set; } = [];
}

public class ApportionmentLineDto
{
    public Guid? Id { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitLabel { get; set; }
    public Guid? TenancyId { get; set; }
    public string? TenantName { get; set; }
    public AreaDto? Area { get; set; }
    public decimal SharePercent { get; set; }
    public decimal FixedAmount { get; set; }
    public bool IsExempt { get; set; }
    public string? ExemptionReason { get; set; }
}

public class ServiceChargeInvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid? TenancyId { get; set; }
    public string? TenantName { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitLabel { get; set; }
    public string InvoiceType { get; set; } = "OnAccount";
    public DateOnly IssuedOn { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public decimal SharePercent { get; set; }
    public InstalmentStatus Status { get; set; }
    public bool IsDisputed { get; set; }
    public string? DocumentUrl { get; set; }
}

// ── Turnover rent ────────────────────────────────────────────────────────────

public class TurnoverRentTermDto
{
    public Guid? Id { get; set; }
    public Guid TenancyId { get; set; }
    public TurnoverRentBasis Basis { get; set; }
    public decimal BreakpointAmount { get; set; }
    public decimal Percent { get; set; }
    public string CalculationPeriod { get; set; } = "Annual";
    public DateOnly PeriodStartMonth { get; set; }
    public string? ExcludedSalesCategories { get; set; }
    public bool RequiresAuditedFigures { get; set; }
    public int DeclarationDueDays { get; set; }
    public bool OffsetBaseRent { get; set; }
    public bool IsActive { get; set; }
    public List<TurnoverRentSlabDto> Slabs { get; set; } = [];
}

public class TurnoverRentSlabDto
{
    public Guid? Id { get; set; }
    public decimal FromSales { get; set; }
    public decimal? ToSales { get; set; }
    public decimal Percent { get; set; }
    public int SortOrder { get; set; }
}

public class TenantSalesDeclarationDto
{
    public Guid Id { get; set; }
    public Guid TenancyId { get; set; }
    public string TenancyReference { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string? UnitLabel { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly DueOn { get; set; }
    public DateOnly? DeclaredOn { get; set; }
    public decimal GrossSales { get; set; }
    public decimal ExcludedSales { get; set; }
    public decimal NetSales { get; set; }
    public int? TransactionCount { get; set; }
    public decimal? FootfallCount { get; set; }
    public string DeclarationSource { get; set; } = "Portal";
    public bool IsAudited { get; set; }
    public decimal? AuditedSales { get; set; }
    public decimal? Variance { get; set; }
    public bool IsLate { get; set; }
    public decimal? LatePenalty { get; set; }
    public bool IsEstimated { get; set; }
    public string? SupportingDocumentUrl { get; set; }
    public decimal? SalesPerSqFt { get; set; }
}

public class OverageInvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid TenancyId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string? UnitLabel { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly IssuedOn { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal DeclaredSales { get; set; }
    public decimal BreakpointApplied { get; set; }
    public decimal SalesAboveBreakpoint { get; set; }
    public decimal PercentApplied { get; set; }
    public decimal GrossOverage { get; set; }
    public decimal BaseRentOffset { get; set; }
    public decimal NetOverage { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public InstalmentStatus Status { get; set; }
    public bool IsProvisional { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string? DocumentUrl { get; set; }
}

public class RecoveryChargeDto
{
    public Guid? Id { get; set; }
    public string ChargeType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public ChargeBasis Basis { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public RentFrequency Frequency { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsTaxable { get; set; }
    public decimal TaxPercent { get; set; }
    public bool IncludeInRentRun { get; set; }
    public bool IsActive { get; set; }
}

public class LeaseConcessionDto
{
    public Guid? Id { get; set; }
    public string ConcessionType { get; set; } = "RentFree";
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public decimal Amount { get; set; }
    public decimal? DiscountPercent { get; set; }
    public bool IsAmortised { get; set; }
    public int AmortisationMonths { get; set; }
    public decimal MonthlyAmortisation { get; set; }
    public bool ClawbackOnEarlyBreak { get; set; }
    public string? Note { get; set; }
}

public class VoidRecordDto
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string AddressOneLine { get; set; } = string.Empty;
    public Guid? UnitId { get; set; }
    public string? UnitLabel { get; set; }
    public AreaDto? Area { get; set; }
    public DateOnly VacantFrom { get; set; }
    public DateOnly? LetFrom { get; set; }
    public int DaysVoid { get; set; }
    public decimal AskingRent { get; set; }
    public decimal LostRent { get; set; }
    public decimal HoldingCost { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public int EnquiryCount { get; set; }
    public int ViewingCount { get; set; }
    public string? VoidReason { get; set; }
    public bool RefurbishmentRequired { get; set; }
    public bool IsClosed { get; set; }
}

public class TenantCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public decimal TargetMixPercent { get; set; }
    public decimal CurrentMixPercent { get; set; }
    public decimal Variance { get; set; }
    public int UnitCount { get; set; }
    public AreaDto? Area { get; set; }
    public string? ColourHex { get; set; }
    public int SortOrder { get; set; }
}

// ── Landlords & client money ─────────────────────────────────────────────────

public class LandlordListItemDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid PartyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public ManagementService DefaultService { get; set; }
    public decimal DefaultFeePercent { get; set; }
    public int PropertyCount { get; set; }
    public int TenancyCount { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal TotalRentCollected { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal ArrearsOnPortfolio { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool PayoutsOnHold { get; set; }
    public string? HoldReason { get; set; }
    public bool IsNonResident { get; set; }
    public decimal WithholdingPercent { get; set; }
    public string? ManagedByName { get; set; }
    public int OpenComplianceIssues { get; set; }
}

public class LandlordDetailDto : LandlordListItemDto
{
    public string PayoutFrequency { get; set; } = "Monthly";
    public int PayoutDay { get; set; }
    public string? BankName { get; set; }
    public string? AccountTitle { get; set; }
    public string? AccountNumber { get; set; }
    public string? SortCodeOrIban { get; set; }
    public bool BankDetailsVerified { get; set; }
    public string? TaxExemptionReference { get; set; }
    public DateOnly? ExemptionValidUntil { get; set; }
    public decimal FloatRequired { get; set; }
    public decimal FloatBalance { get; set; }
    public decimal RepairAuthorityLimit { get; set; }
    public NotificationChannel StatementChannel { get; set; }
    public bool PortalAccessEnabled { get; set; }
    public string? Notes { get; set; }

    public List<ManagementAgreementDto> Agreements { get; set; } = [];
    public List<PropertyListItemDto> Properties { get; set; } = [];
    public List<TenancyListItemDto> Tenancies { get; set; } = [];
    public List<OwnerStatementDto> Statements { get; set; } = [];
    public List<WorkOrderListItemDto> WorkOrders { get; set; } = [];
    public List<TimelineEntryDto> Timeline { get; set; } = [];
}

public class ManagementAgreementDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid? PropertyId { get; set; }
    public string? AddressOneLine { get; set; }
    public ManagementService Service { get; set; }
    public decimal FeePercent { get; set; }
    public decimal FixedMonthlyFee { get; set; }
    public decimal SetupFee { get; set; }
    public decimal RenewalFee { get; set; }
    public decimal TenantFindFee { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int NoticePeriodDays { get; set; }
    public decimal RepairAuthorityLimit { get; set; }
    public bool CanSignTenancyOnBehalf { get; set; }
    public bool CanServeNoticeOnBehalf { get; set; }
    public bool HoldsDeposit { get; set; }
    public string? DocumentUrl { get; set; }
    public DateOnly? SignedOn { get; set; }
    public bool IsActive { get; set; }
    public DateOnly? TerminatedOn { get; set; }
}

public class OwnerStatementDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid LandlordId { get; set; }
    public string LandlordName { get; set; } = string.Empty;
    public Guid? PropertyId { get; set; }
    public string? AddressOneLine { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly IssuedOn { get; set; }

    public decimal OpeningBalance { get; set; }
    public decimal RentCollected { get; set; }
    public decimal OtherIncome { get; set; }
    public decimal ManagementFee { get; set; }
    public decimal MaintenanceCost { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal TaxWithheld { get; set; }
    public decimal FloatRetained { get; set; }
    public decimal NetPayable { get; set; }
    public decimal ClosingBalance { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public string? DocumentUrl { get; set; }
    public Guid? OwnerPayoutId { get; set; }
    public bool IsPublishedToPortal { get; set; }
    public bool IsSent { get; set; }
    public List<OwnerStatementLineDto> Lines { get; set; } = [];
}

public class OwnerStatementLineDto
{
    public Guid Id { get; set; }
    public DateOnly EntryDate { get; set; }
    public string? AddressOneLine { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal IncomeAmount { get; set; }
    public decimal DeductionAmount { get; set; }
    public Guid? WorkOrderId { get; set; }
    public string? SupportingDocumentUrl { get; set; }
    public int SortOrder { get; set; }
}

public class OwnerPayoutDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateOnly PayoutDate { get; set; }
    public string? OfficeName { get; set; }
    public int LandlordCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalWithheld { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string PaymentMethod { get; set; } = "Bacs";
    public string? BankFileUrl { get; set; }
    public string? BatchReference { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? SubmittedByName { get; set; }
    public ApprovalOutcome? ApprovalOutcome { get; set; }
    public bool IsCompleted { get; set; }
    public int FailedCount { get; set; }
    public int HeldCount { get; set; }
    public string? FailureSummary { get; set; }
    public List<OwnerPayoutLineDto> Lines { get; set; } = [];
}

public class OwnerPayoutLineDto
{
    public Guid Id { get; set; }
    public Guid LandlordId { get; set; }
    public string LandlordName { get; set; } = string.Empty;
    public Guid? OwnerStatementId { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal WithheldAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string? AccountNumber { get; set; }
    public string? PaymentReference { get; set; }
    public bool IsHeld { get; set; }
    public string? HoldReason { get; set; }
    public bool Failed { get; set; }
    public string? FailureReason { get; set; }
    public bool IsPaid { get; set; }
}

public class ClientAccountDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public ClientAccountKind Kind { get; set; }
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string? OfficeName { get; set; }
    public string? LandlordName { get; set; }
    public decimal Balance { get; set; }
    public decimal UnallocatedBalance { get; set; }
    public bool IsOverdrawn { get; set; }
    public DateOnly? LastReconciledOn { get; set; }
    public int ReconciliationIntervalDays { get; set; }
    public bool ReconciliationOverdue { get; set; }
    public int OpenExceptionCount { get; set; }
    public bool IsActive { get; set; }
}

public class ClientLedgerEntryDto
{
    public Guid Id { get; set; }
    public DateOnly EntryDate { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal RunningBalance { get; set; }
    public string? PartyName { get; set; }
    public string? AddressOneLine { get; set; }
    public bool IsReconciled { get; set; }
    public DateOnly? ReconciledOn { get; set; }
    public string? BankReference { get; set; }
    public bool IsCorrection { get; set; }
    public string? AuthorisedByName { get; set; }
}

/// <summary>
/// The three-way reconciliation. Bank, control account and the sum of client balances must all
/// agree, and this says plainly when they do not.
/// </summary>
public class ClientMoneyReconciliationDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid? ClientAccountId { get; set; }
    public string? AccountName { get; set; }
    public DateOnly ReconciliationDate { get; set; }

    public decimal BankStatementBalance { get; set; }
    public decimal LedgerControlBalance { get; set; }
    public decimal SumOfClientBalances { get; set; }
    public decimal UnpresentedPayments { get; set; }
    public decimal UndepositedReceipts { get; set; }
    public decimal AdjustedBankBalance { get; set; }
    public decimal Difference { get; set; }
    public bool IsBalanced { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public int ExceptionCount { get; set; }
    public string? PreparedByName { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? SignedOffAt { get; set; }
    public string? BankStatementUrl { get; set; }
    public string? Notes { get; set; }
    public bool IsOverdue { get; set; }
    public List<ClientMoneyExceptionDto> Exceptions { get; set; } = [];
}

public class ClientMoneyExceptionDto
{
    public Guid Id { get; set; }
    public ClientMoneyExceptionKind Kind { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public DateOnly RaisedOn { get; set; }
    public string? PartyName { get; set; }
    public string? AssignedToName { get; set; }
    public bool IsResolved { get; set; }
    public DateOnly? ResolvedOn { get; set; }
    public string? Resolution { get; set; }
    public bool RequiresRegulatoryReport { get; set; }
    public bool IsReported { get; set; }
    public int DaysOpen { get; set; }
}
