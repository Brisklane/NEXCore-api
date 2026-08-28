using Nexcore.SharedKernel;
using RealEstate.Domain.Enums;

namespace RealEstate.Domain.Entities;

/// <summary>
/// A right to occupy, for rent. One entity for a residential letting, a commercial lease and a
/// mall kiosk licence, because they differ in their terms rather than in their shape — and an
/// estate manager wants one rent roll, not three.
/// </summary>
public class Tenancy : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? LandlordId { get; set; }
    public Guid? InstructionId { get; set; }
    public Guid? OfficeId { get; set; }
    public Guid? ManagedByUserId { get; set; }

    public TenancyKind Kind { get; set; } = TenancyKind.FixedTerm;
    public TenancyStatus Status { get; set; } = TenancyStatus.Application;

    // ── Term ─────────────────────────────────────────────────────────────────

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int? TermMonths { get; set; }

    /// <summary>Continues after the fixed term instead of ending. The default in most residential markets.</summary>
    public bool RollsToPeriodic { get; set; } = true;

    public DateOnly? ActualEndDate { get; set; }

    // ── Rent ─────────────────────────────────────────────────────────────────

    public decimal Rent { get; set; }
    public RentFrequency Frequency { get; set; } = RentFrequency.Monthly;
    public string CurrencyCode { get; set; } = AppConstants.DefaultCurrency;

    /// <summary>Day of the period rent falls due. 1 for the first of the month.</summary>
    public int PaymentDay { get; set; } = 1;

    /// <summary>Rent for a period is due at its start, not its end. Almost always true.</summary>
    public bool PaidInAdvance { get; set; } = true;

    public decimal? RentPerSqFt { get; set; }
    public decimal? AnnualRent { get; set; }

    // ── Escalation ───────────────────────────────────────────────────────────

    public EscalationKind Escalation { get; set; } = EscalationKind.None;
    public decimal EscalationPercent { get; set; }
    public int EscalationMonths { get; set; } = 12;
    public DateOnly? NextEscalationDate { get; set; }

    // ── Money in and out ─────────────────────────────────────────────────────

    public decimal DepositAmount { get; set; }
    public Guid? SecurityDepositId { get; set; }
    public decimal AdvanceRentMonths { get; set; }

    public decimal TotalCharged { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal ArrearsAmount { get; set; }
    public int DaysInArrears { get; set; }
    public DateOnly? NextDueDate { get; set; }

    // ── Recoveries ───────────────────────────────────────────────────────────

    public bool ServiceChargeApplies { get; set; }
    public ApportionmentBasis? ServiceChargeBasis { get; set; }
    public decimal ServiceChargePercent { get; set; }
    public bool TurnoverRentApplies { get; set; }
    public bool UtilitiesRecharged { get; set; }
    public bool PropertyTaxRecharged { get; set; }
    public bool InsuranceRecharged { get; set; }

    // ── Management ───────────────────────────────────────────────────────────

    public ManagementService ManagementService { get; set; } = ManagementService.FullManagement;
    public decimal ManagementFeePercent { get; set; }

    /// <summary>What the agent may spend on a repair without asking the landlord first.</summary>
    public decimal RepairAuthorityLimit { get; set; }

    // ── Terms & conditions ───────────────────────────────────────────────────

    public bool PetsAllowed { get; set; }
    public bool SmokingAllowed { get; set; }
    public bool SublettingAllowed { get; set; }
    public int? MaxOccupants { get; set; }
    public string? PermittedUse { get; set; }
    public int NoticePeriodDaysTenant { get; set; } = 30;
    public int NoticePeriodDaysLandlord { get; set; } = 60;

    public Guid? AgreementDocumentId { get; set; }
    public string? AgreementUrl { get; set; }
    public Guid? SignatureSessionId { get; set; }
    public DateOnly? SignedOn { get; set; }
    public bool IsRegistered { get; set; }
    public string? RegistrationNumber { get; set; }

    public Guid? ReferencingCaseId { get; set; }
    public Guid? PreviousTenancyId { get; set; }
    public Guid? DunningCaseId { get; set; }

    public string? Notes { get; set; }

    public ICollection<TenancyParty> Parties { get; set; } = [];
    public ICollection<RentCharge> RentCharges { get; set; } = [];
    public ICollection<LeaseOption> Options { get; set; } = [];
}

public class TenancyParty : BaseEntity
{
    public Guid TenancyId { get; set; }
    public Tenancy? Tenancy { get; set; }

    public Guid PartyId { get; set; }

    /// <summary>"Tenant", "JointTenant", "Guarantor", "Occupier", "Licensee", "AuthorisedSignatory".</summary>
    public string Role { get; set; } = "Tenant";

    public bool IsLeadTenant { get; set; }

    /// <summary>Jointly and severally liable — each tenant owes the whole rent, not their share.</summary>
    public bool IsJointlyAndSeverallyLiable { get; set; } = true;

    public decimal LiabilitySharePercent { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public KycStatus KycStatus { get; set; } = KycStatus.NotStarted;
    public ReferencingOutcome? ReferencingOutcome { get; set; }
}

/// <summary>The guarantor's own liability, which can be narrower than the tenant's.</summary>
public class TenancyGuarantor : BaseEntity
{
    public Guid TenancyId { get; set; }
    public Guid PartyId { get; set; }

    public decimal? LiabilityCapAmount { get; set; }
    public bool CoversWholeRent { get; set; } = true;
    public bool CoversDamage { get; set; } = true;
    public DateOnly? LiabilityFrom { get; set; }
    public DateOnly? LiabilityTo { get; set; }

    public Guid? DeedDocumentId { get; set; }
    public bool IsVerified { get; set; }
    public ReferencingOutcome? ReferencingOutcome { get; set; }
}

/// <summary>
/// The generated series of rent charges for the whole term. Materialised rather than computed, so
/// a stepped rent, a rent-free month and a mid-term escalation are all visible in advance and can
/// be reconciled against what was actually billed.
/// </summary>
public class RentSchedule : BaseEntity
{
    public Guid TenancyId { get; set; }
    public int Version { get; set; } = 1;
    public bool IsCurrent { get; set; } = true;
    public Guid? SupersedesScheduleId { get; set; }

    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public decimal TotalRent { get; set; }
    public string? RevisionReason { get; set; }
    public Guid? RentReviewId { get; set; }
}

/// <summary>One period's rent, as raised.</summary>
public class RentCharge : BaseEntity
{
    public Guid TenancyId { get; set; }
    public Tenancy? Tenancy { get; set; }
    public Guid? RentScheduleId { get; set; }
    public Guid? RentRunId { get; set; }

    public int SequenceNumber { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly DueDate { get; set; }

    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }

    public InstalmentStatus Status { get; set; } = InstalmentStatus.NotDue;
    public int DaysOverdue { get; set; }
    public decimal LateFeeAccrued { get; set; }

    /// <summary>A partial first or last period, charged pro rata by days.</summary>
    public bool IsProRated { get; set; }

    /// <summary>A concession period. Raised at zero so the schedule stays continuous and auditable.</summary>
    public bool IsRentFree { get; set; }

    public Guid? ConcessionId { get; set; }
    public Guid? InvoiceId { get; set; }
    public DateOnly? SettledOn { get; set; }
}

/// <summary>How and when the rent moves, held apart from the tenancy because a lease can have several.</summary>
public class EscalationRule : BaseEntity
{
    public Guid TenancyId { get; set; }

    public EscalationKind Kind { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public decimal Percent { get; set; }
    public decimal FixedAmount { get; set; }

    /// <summary>"CPI", "RPI", or a locally published index name.</summary>
    public string? IndexName { get; set; }

    /// <summary>Floor and cap on an index-linked rise, which is how these are always negotiated.</summary>
    public decimal? FloorPercent { get; set; }
    public decimal? CapPercent { get; set; }

    public int IntervalMonths { get; set; } = 12;
    public bool IsCompounding { get; set; } = true;
    public bool IsApplied { get; set; }
    public DateOnly? AppliedOn { get; set; }
    public decimal? ResultingRent { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// An open-market rent review: a date, a notice deadline, evidence, a negotiation and a memorandum
/// recording what was agreed. Missing the notice deadline can cost a landlord the whole uplift.
/// </summary>
public class RentReview : BaseEntity
{
    public Guid TenancyId { get; set; }
    public string Reference { get; set; } = string.Empty;

    public DateOnly ReviewDate { get; set; }
    public DateOnly? NoticeDeadline { get; set; }
    public bool NoticeServed { get; set; }
    public DateOnly? NoticeServedOn { get; set; }

    public decimal CurrentRent { get; set; }
    public decimal? ProposedRent { get; set; }
    public decimal? CounterProposedRent { get; set; }
    public decimal? AgreedRent { get; set; }
    public DateOnly? AgreedOn { get; set; }
    public DateOnly? EffectiveFrom { get; set; }

    /// <summary>"Agreed", "Determined", "Arbitration", "Referred", "Lapsed".</summary>
    public string? Outcome { get; set; }

    public Guid? ValuationId { get; set; }
    public Guid? MemorandumDocumentId { get; set; }
    public bool IsUpwardOnly { get; set; } = true;
    public string? Note { get; set; }
}

/// <summary>
/// A right the tenant or landlord holds, with a window in which it must be exercised. The alert on
/// this is worth more than most of the rest of the record.
/// </summary>
public class LeaseOption : BaseEntity
{
    public Guid TenancyId { get; set; }
    public Tenancy? Tenancy { get; set; }

    public LeaseOptionKind Kind { get; set; }

    /// <summary>"Tenant", "Landlord", "Mutual".</summary>
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
    public bool AlertSent { get; set; }
}

/// <summary>
/// Any date on a lease that has a consequence if missed — an option deadline, a break notice, a
/// review, an insurance renewal. One table so a single calendar can show them all.
/// </summary>
public class CriticalDate : BaseEntity
{
    public Guid? TenancyId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? ProjectId { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>"Option", "Review", "Expiry", "Insurance", "Compliance", "Licence", "Registration".</summary>
    public string DateType { get; set; } = string.Empty;

    public DateOnly DueDate { get; set; }
    public int AlertDaysBefore { get; set; } = 90;
    public Guid? OwnerUserId { get; set; }

    public AlertSeverity Severity { get; set; } = AlertSeverity.Warning;
    public bool AlertSent { get; set; }
    public DateTime? AlertSentAt { get; set; }
    public bool IsActioned { get; set; }
    public DateOnly? ActionedOn { get; set; }
    public string? Note { get; set; }
}

/// <summary>The renewal conversation, and the pipeline of value at risk that falls out of it.</summary>
public class TenancyRenewal : BaseEntity
{
    public Guid TenancyId { get; set; }
    public string Reference { get; set; } = string.Empty;

    public DateOnly ExpiryDate { get; set; }
    public DateOnly? OfferedOn { get; set; }
    public decimal CurrentRent { get; set; }
    public decimal? ProposedRent { get; set; }
    public decimal? AgreedRent { get; set; }
    public int? ProposedTermMonths { get; set; }

    /// <summary>"Offered", "Negotiating", "Accepted", "Declined", "Lapsed", "Vacating".</summary>
    public string Status { get; set; } = "Offered";

    public DateOnly? RespondedOn { get; set; }
    public Guid? NewTenancyId { get; set; }
    public Guid? DeclineReasonCodeId { get; set; }
    public bool LandlordApproved { get; set; }
    public string? Note { get; set; }
}

/// <summary>The vetting of an applicant, which gates the move-in rather than following it.</summary>
public class ReferencingCase : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid? TenancyId { get; set; }
    public Guid PartyId { get; set; }
    public Guid? PropertyId { get; set; }

    public ReferencingOutcome Outcome { get; set; } = ReferencingOutcome.Pending;
    public DateOnly StartedOn { get; set; }
    public DateOnly? CompletedOn { get; set; }
    public Guid? AssignedToUserId { get; set; }

    public decimal? DeclaredIncome { get; set; }

    /// <summary>Income against annual rent. The number a landlord actually asks for.</summary>
    public decimal? IncomeMultiple { get; set; }

    public bool GuarantorRequired { get; set; }
    public string? Conditions { get; set; }
    public string? FailureReason { get; set; }

    /// <summary>Reference into the provider's result. We store the outcome, not their raw report.</summary>
    public string? ProviderReference { get; set; }

    public ICollection<ReferencingCheck> Checks { get; set; } = [];
}

public class ReferencingCheck : BaseEntity
{
    public Guid ReferencingCaseId { get; set; }
    public ReferencingCase? Case { get; set; }

    public ReferencingCheckKind Kind { get; set; }
    public ReferencingOutcome Outcome { get; set; } = ReferencingOutcome.Pending;
    public DateOnly? RequestedOn { get; set; }
    public DateOnly? CompletedOn { get; set; }

    public string? RefereeName { get; set; }
    public string? RefereeContact { get; set; }
    public string? Findings { get; set; }
    public string? DocumentUrl { get; set; }
    public bool IsMandatory { get; set; } = true;

    /// <summary>Right-to-rent and visa checks carry a statutory re-check date.</summary>
    public DateOnly? RecheckDue { get; set; }
}

/// <summary>
/// A refundable amount taken to take the property off the market. Statutorily capped and
/// deadline-bound in several markets, with prescribed grounds for retaining it.
/// </summary>
public class HoldingDeposit : BaseEntity
{
    public Guid? TenancyId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid PartyId { get; set; }

    public decimal Amount { get; set; }
    public DateOnly ReceivedOn { get; set; }
    public Guid? ReceiptId { get; set; }

    /// <summary>Statutory deadline to conclude or refund. Missing it forfeits the landlord's position.</summary>
    public DateOnly? DeadlineDate { get; set; }

    /// <summary>"Held", "AppliedToRent", "AppliedToDeposit", "Refunded", "Retained".</summary>
    public string Status { get; set; } = "Held";

    public DateOnly? ResolvedOn { get; set; }
    public string? RetentionGround { get; set; }
    public Guid? RefundRequestId { get; set; }
}

/// <summary>
/// The tenant's deposit. Where a statutory scheme exists it must be registered within a deadline
/// and the prescribed information served; where none exists it sits in the client account.
/// </summary>
public class SecurityDeposit : BaseEntity
{
    public Guid TenancyId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid PartyId { get; set; }

    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = AppConstants.DefaultCurrency;
    public DateOnly ReceivedOn { get; set; }
    public Guid? ReceiptId { get; set; }

    public DepositScheme Scheme { get; set; } = DepositScheme.HeldInClientAccount;
    public Guid? ClientAccountId { get; set; }

    // ── Statutory protection ─────────────────────────────────────────────────

    public string? SchemeName { get; set; }
    public string? RegistrationReference { get; set; }

    /// <summary>The clock. Late registration is a penalty of several times the deposit in some markets.</summary>
    public DateOnly? RegistrationDeadline { get; set; }

    public DateOnly? RegisteredOn { get; set; }
    public bool IsRegistered { get; set; }
    public bool PrescribedInformationServed { get; set; }
    public DateOnly? PrescribedInformationServedOn { get; set; }
    public Guid? PrescribedInformationDocumentId { get; set; }

    // ── Release ──────────────────────────────────────────────────────────────

    public decimal DeductionTotal { get; set; }
    public decimal ReturnedToTenant { get; set; }
    public decimal PaidToLandlord { get; set; }
    public DateOnly? ReleasedOn { get; set; }

    public bool IsDisputed { get; set; }
    public string? DisputeReference { get; set; }

    /// <summary>"TenantAgreed", "Adjudicated", "Court", "Unresolved".</summary>
    public string? DisputeOutcome { get; set; }

    public ICollection<DepositDeduction> Deductions { get; set; } = [];
}

public class DepositDeduction : BaseEntity
{
    public Guid SecurityDepositId { get; set; }
    public SecurityDeposit? Deposit { get; set; }

    /// <summary>"RentArrears", "Damage", "Cleaning", "MissingItems", "Redecoration", "UnpaidBills".</summary>
    public string Category { get; set; } = string.Empty;

    public decimal ProposedAmount { get; set; }
    public decimal AgreedAmount { get; set; }

    /// <summary>The check-out evidence. A deduction without it does not survive adjudication.</summary>
    public string? EvidenceUrl { get; set; }
    public Guid? InspectionFindingId { get; set; }
    public Guid? WorkOrderId { get; set; }

    /// <summary>"Pending", "Accepted", "Disputed", "Adjudicated", "Withdrawn".</summary>
    public string TenantResponse { get; set; } = "Pending";

    public DateOnly? RespondedOn { get; set; }
    public string? AdjudicatorNote { get; set; }
}

/// <summary>
/// The condition report at move-in or move-out. The comparison between the two is what a deposit
/// deduction stands or falls on, so both are captured the same way.
/// </summary>
public class MoveInspection : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid PropertyId { get; set; }
    public Guid? TenancyId { get; set; }
    public InspectionKind Kind { get; set; } = InspectionKind.MoveIn;

    public DateTime InspectedAt { get; set; }
    public Guid? InspectorUserId { get; set; }
    public bool TenantPresent { get; set; }
    public bool LandlordPresent { get; set; }

    public string? OverallCondition { get; set; }
    public string? CleanlinessRating { get; set; }
    public string? TenantSignatureUrl { get; set; }
    public string? InspectorSignatureUrl { get; set; }
    public string? ReportUrl { get; set; }

    /// <summary>The move-in report this check-out is compared against.</summary>
    public Guid? ComparedToInspectionId { get; set; }

    public bool TenantDisputed { get; set; }
    public string? TenantComments { get; set; }
    public DateOnly? TenantResponseDeadline { get; set; }
    public bool IsFinalised { get; set; }

    public ICollection<InspectionRoomItem> Items { get; set; } = [];
}

public class InspectionRoomItem : BaseEntity
{
    public Guid MoveInspectionId { get; set; }
    public MoveInspection? Inspection { get; set; }

    public string RoomName { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public ConditionGrade Condition { get; set; } = ConditionGrade.Good;
    public string? CleanlinessGrade { get; set; }
    public string? Note { get; set; }

    /// <summary>Comma-separated URLs. Photographs are the whole point of this record.</summary>
    public string? PhotoUrls { get; set; }

    public ConditionGrade? PreviousCondition { get; set; }

    /// <summary>Worse than at move-in and not fair wear and tear. Feeds the deduction proposal.</summary>
    public bool HasDeteriorated { get; set; }
    public bool IsFairWearAndTear { get; set; }
    public decimal? EstimatedCost { get; set; }

    public int SortOrder { get; set; }
}

/// <summary>Notice given by either side, with the statutory period computed rather than guessed.</summary>
public class TenancyNotice : BaseEntity
{
    public Guid TenancyId { get; set; }
    public string NoticeNumber { get; set; } = string.Empty;

    /// <summary>"NoticeToQuit", "SectionNotice", "BreachNotice", "RentIncrease", "Possession", "Surrender".</summary>
    public string NoticeType { get; set; } = string.Empty;

    /// <summary>"Landlord" or "Tenant".</summary>
    public string ServedBy { get; set; } = "Landlord";

    public DateOnly ServedOn { get; set; }
    public DateOnly EffectiveFrom { get; set; }

    /// <summary>Computed from the office working calendar, not from a mental sum.</summary>
    public int NoticePeriodDays { get; set; }

    public string? Grounds { get; set; }
    public string? ServiceMethod { get; set; }
    public string? ServiceEvidenceUrl { get; set; }
    public Guid? DocumentId { get; set; }

    public bool IsAcknowledged { get; set; }
    public bool IsWithdrawn { get; set; }
    public bool IsValid { get; set; } = true;
    public string? ValidityNote { get; set; }
}

public class TenancyTermination : BaseEntity
{
    public Guid TenancyId { get; set; }

    /// <summary>"Expiry", "Surrender", "Notice", "Break", "Forfeiture", "Abandonment", "Eviction", "Death".</summary>
    public string Method { get; set; } = "Expiry";

    public DateOnly TerminatedOn { get; set; }
    public Guid? NoticeId { get; set; }
    public Guid? ReasonCodeId { get; set; }

    public decimal EarlyTerminationFee { get; set; }
    public decimal DilapidationsClaim { get; set; }
    public decimal OutstandingArrears { get; set; }

    public Guid? CheckOutInspectionId { get; set; }
    public bool KeysReturned { get; set; }
    public DateOnly? KeysReturnedOn { get; set; }
    public bool FinalMeterReadingsTaken { get; set; }
    public bool DepositResolved { get; set; }

    // Eviction, where it came to that.
    public string? CourtCaseNumber { get; set; }
    public DateOnly? PossessionOrderDate { get; set; }
    public DateOnly? EvictionDate { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// A safety or licensing certificate a property must hold. Missing one is a criminal offence in
/// several markets, so this is an alert engine rather than a filing cabinet.
/// </summary>
public class ComplianceCertificate : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? ProjectId { get; set; }

    public ComplianceCertificateKind Kind { get; set; }
    public string? CertificateNumber { get; set; }

    public DateOnly IssuedOn { get; set; }
    public DateOnly ExpiresOn { get; set; }
    public int ValidityMonths { get; set; }

    public Guid? ContractorId { get; set; }
    public string? IssuerName { get; set; }
    public string? IssuerRegistration { get; set; }
    public decimal? Cost { get; set; }
    public CostBearer BorneBy { get; set; } = CostBearer.Landlord;

    public string? DocumentUrl { get; set; }
    public bool ServedToTenant { get; set; }
    public DateOnly? ServedOn { get; set; }

    /// <summary>Any failed item found on inspection. Usually gates re-letting.</summary>
    public bool HasFailures { get; set; }
    public string? Findings { get; set; }
    public Guid? RemedialWorkOrderId { get; set; }

    public bool RenewalBooked { get; set; }
    public DateOnly? RenewalBookedFor { get; set; }
    public bool IsCurrent { get; set; } = true;
}

/// <summary>The recurring obligation that generates the certificates above.</summary>
public class ComplianceSchedule : BaseEntity
{
    public Guid? PropertyId { get; set; }
    public Guid? ProjectId { get; set; }

    public ComplianceCertificateKind Kind { get; set; }
    public int IntervalMonths { get; set; } = 12;
    public DateOnly? LastCompletedOn { get; set; }
    public DateOnly NextDueOn { get; set; }
    public int AlertDaysBefore { get; set; } = 45;

    public Guid? PreferredContractorId { get; set; }
    public Guid? OwnerUserId { get; set; }
    public bool IsMandatory { get; set; } = true;

    /// <summary>Refuse to publish a listing or start a tenancy while this is overdue.</summary>
    public bool BlocksLettingWhenOverdue { get; set; }

}
