using RealEstate.Domain.Enums;

namespace RealEstate.Application.DTOs;

// =====================================================================================
// Payment plans, demands, the surcharge engine, receipts and the collections desk.
// The heaviest-used shapes in the module.
// =====================================================================================

public class PaymentPlanTemplateDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    public Guid? SurchargePolicyId { get; set; }
    public string? SurchargePolicyName { get; set; }
    public Guid? DunningPolicyId { get; set; }

    public decimal EarlyPaymentRebatePercentPerMonth { get; set; }
    public decimal LumpSumDiscountPercent { get; set; }
    public int LumpSumWindowDays { get; set; }
    public string? Note { get; set; }

    /// <summary>Percentages across all lines. Must reach 100 or the template is unusable.</summary>
    public decimal TotalPercent { get; set; }
    public bool IsBalanced { get; set; }
    public int InstalmentCount { get; set; }
    public int DurationMonths { get; set; }
    public int UsageCount { get; set; }

    public List<PaymentPlanTemplateLineDto> Lines { get; set; } = [];
}

public class PaymentPlanTemplateLineDto
{
    public Guid? Id { get; set; }
    public InstalmentKind Kind { get; set; }
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public decimal Percent { get; set; }
    public decimal FixedAmount { get; set; }
    public int Count { get; set; } = 1;
    public InstalmentFrequency Frequency { get; set; }
    public int StartOffsetDays { get; set; }
    public int StartOffsetMonths { get; set; }
    public Guid? ProjectMilestoneId { get; set; }
    public string? MilestoneName { get; set; }
    public string? MilestoneCode { get; set; }
    public ChargeKind? ChargeKind { get; set; }
    public bool IsTaxable { get; set; }
    public decimal TaxPercent { get; set; }
}

/// <summary>A plan built by hand for one booking, when the template does not fit.</summary>
public class PaymentPlanCustomDto
{
    public string? Name { get; set; }
    public DateOnly StartDate { get; set; }
    public Guid? SurchargePolicyId { get; set; }
    public Guid? DunningPolicyId { get; set; }
    public string? DeviationReason { get; set; }
    public List<PaymentPlanTemplateLineDto> Lines { get; set; } = [];
}

public class PaymentPlanDto
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public string? BookingReference { get; set; }
    public string Reference { get; set; } = string.Empty;
    public int Version { get; set; }
    public bool IsCurrent { get; set; }
    public bool IsCustom { get; set; }
    public bool IsRestructure { get; set; }
    public string? RevisionReason { get; set; }
    public Guid? SupersedesPlanId { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalDemanded { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Outstanding { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public string? SurchargePolicyName { get; set; }
    public decimal EarlyPaymentRebateEarned { get; set; }
    public decimal RestructureFee { get; set; }

    public List<InstalmentDto> Instalments { get; set; } = [];
}

/// <summary>What a plan would look like before anything is written. Shown on the wizard's last step.</summary>
public class PaymentPlanPreviewDto
{
    public string? TemplateName { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public int InstalmentCount { get; set; }
    public int DurationMonths { get; set; }
    public decimal MonthlyAverage { get; set; }
    public decimal DownPayment { get; set; }
    public decimal PossessionBalance { get; set; }
    public decimal EarlyPaymentRebateAvailable { get; set; }
    public List<InstalmentDto> Instalments { get; set; } = [];

    /// <summary>Set when the plan departs from the template and will need approval.</summary>
    public bool IsDeviation { get; set; }
    public string? DeviationNote { get; set; }
}

public class InstalmentDto
{
    public Guid Id { get; set; }
    public int SequenceNumber { get; set; }
    public InstalmentKind Kind { get; set; }
    public string Label { get; set; } = string.Empty;
    public DateOnly? DueDate { get; set; }
    public Guid? ProjectMilestoneId { get; set; }
    public string? MilestoneName { get; set; }
    public MilestoneStatus? MilestoneStatus { get; set; }

    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal WaivedAmount { get; set; }
    public decimal Balance { get; set; }

    public InstalmentStatus Status { get; set; }
    public decimal SurchargeAccrued { get; set; }
    public decimal SurchargePaid { get; set; }
    public decimal SurchargeWaived { get; set; }
    public decimal SurchargeOutstanding { get; set; }
    public int DaysOverdue { get; set; }

    public Guid? DemandId { get; set; }
    public string? DemandNumber { get; set; }
    public DateOnly? FirstPaidOn { get; set; }
    public DateOnly? SettledOn { get; set; }
    public bool IsOnHold { get; set; }
    public string? HoldReason { get; set; }
    public ChargeKind? ChargeKind { get; set; }
}

/// <summary>Rebuilding a defaulter's remaining schedule over a longer horizon.</summary>
public class PlanRestructureDto
{
    public Guid BookingId { get; set; }
    public DateOnly NewStartDate { get; set; }
    public int NewInstalmentCount { get; set; }
    public InstalmentFrequency Frequency { get; set; } = InstalmentFrequency.Monthly;
    public decimal RestructureFee { get; set; }

    /// <summary>Roll accrued surcharge into the new schedule rather than demanding it up front.</summary>
    public bool CapitaliseSurcharge { get; set; }

    public decimal? DownPaymentRequired { get; set; }
    public Guid? ReasonCodeId { get; set; }
    public string? Note { get; set; }
    public bool DryRun { get; set; }
}

// ── Surcharge ────────────────────────────────────────────────────────────────

public class SurchargePolicyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public SurchargeBasis Basis { get; set; }
    public decimal Rate { get; set; }
    public decimal FlatAmount { get; set; }
    public int GraceDays { get; set; }
    public bool IsCompounding { get; set; }
    public decimal CapPercent { get; set; }
    public decimal CapAmount { get; set; }
    public int? StopAfterDays { get; set; }
    public bool WaiverRequiresApproval { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Worked example on 100,000 overdue for 30 days, so the policy can be sanity-checked.</summary>
    public decimal ExampleOn100kFor30Days { get; set; }
}

public class SurchargeWaiverDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid BookingId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public Guid? InstalmentId { get; set; }
    public string? InstalmentLabel { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public DateOnly RequestedOn { get; set; }
    public string ReasonLabel { get; set; } = string.Empty;
    public string? Note { get; set; }
    public ApprovalOutcome Outcome { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

public class SurchargeWaiverRequestDto
{
    public Guid BookingId { get; set; }
    public Guid? InstalmentId { get; set; }
    public decimal Amount { get; set; }
    public Guid ReasonCodeId { get; set; }
    public string? Note { get; set; }
}

// ── Demands ──────────────────────────────────────────────────────────────────

public class DemandListItemDto
{
    public Guid Id { get; set; }
    public string DemandNumber { get; set; } = string.Empty;
    public Guid BookingId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public string? ApplicantPhone { get; set; }
    public string? UnitNumber { get; set; }
    public string ProjectName { get; set; } = string.Empty;

    public DemandStatus Status { get; set; }
    public DateOnly IssuedOn { get; set; }
    public DateOnly DueDate { get; set; }
    public int DaysOverdue { get; set; }

    public decimal PrincipalAmount { get; set; }
    public decimal SurchargeAmount { get; set; }
    public decimal ArrearsAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public bool IsReminder { get; set; }
    public int ReminderNumber { get; set; }
    public DateTime? SentAt { get; set; }
    public bool EmailSent { get; set; }
    public bool SmsSent { get; set; }
    public bool WhatsAppSent { get; set; }
    public bool PostSent { get; set; }
    public string? DocumentUrl { get; set; }
}

public class DemandDetailDto : DemandListItemDto
{
    public Guid? DemandBatchId { get; set; }
    public string? MailingAddress { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public List<DemandLineDto> Lines { get; set; } = [];
}

public class DemandLineDto
{
    public Guid Id { get; set; }
    public Guid? InstalmentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateOnly? DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public LedgerEntryKind Kind { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>What the demand run is asked to do. Always previewable before it writes anything.</summary>
public class DemandRunRequestDto
{
    public Guid? ProjectId { get; set; }
    public Guid? ProjectNodeId { get; set; }
    public Guid? ProjectMilestoneId { get; set; }
    public DateOnly? DueDateFrom { get; set; }
    public DateOnly? DueDateTo { get; set; }

    /// <summary>"Scheduled", "Milestone", "Manual", "Reminder".</summary>
    public string RunType { get; set; } = "Scheduled";

    public bool IsDryRun { get; set; } = true;

    /// <summary>Bookings to leave out this run — a disputed file, a customer on a promise to pay.</summary>
    public List<Guid> ExcludeBookingIds { get; set; } = [];

    public bool SendImmediately { get; set; }
    public List<NotificationChannel> Channels { get; set; } = [];
    public Guid? DocumentTemplateId { get; set; }
}

public class DemandBatchDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public string? MilestoneName { get; set; }
    public DateOnly RunDate { get; set; }
    public DateOnly? DueDateFrom { get; set; }
    public DateOnly? DueDateTo { get; set; }
    public string RunType { get; set; } = string.Empty;
    public bool IsDryRun { get; set; }

    public int CandidateCount { get; set; }
    public int GeneratedCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public int SentCount { get; set; }
    public decimal TotalAmount { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? RunByName { get; set; }
    public string? ErrorSummary { get; set; }
    public bool IsCompleted { get; set; }

    public List<DemandListItemDto> Preview { get; set; } = [];
}

// ── Receipts ─────────────────────────────────────────────────────────────────

public class ReceiptListItemDto
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public ReceiptStatus Status { get; set; }
    public DateOnly ReceivedOn { get; set; }

    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public Guid? BookingId { get; set; }
    public string? BookingReference { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? MaintenanceBillId { get; set; }
    public string? UnitNumber { get; set; }
    public string? ProjectName { get; set; }

    public decimal Amount { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal UnallocatedAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public PaymentInstrument Instrument { get; set; }
    public string? BankName { get; set; }
    public string? InstrumentNumber { get; set; }
    public DateOnly? InstrumentDate { get; set; }
    public ChequeState? ChequeState { get; set; }
    public string? TransactionReference { get; set; }

    public decimal EscrowAmount { get; set; }
    public decimal FreeAmount { get; set; }
    public bool IsClientMoney { get; set; }
    public string? ReceivedByName { get; set; }
    public bool IsPrinted { get; set; }
    public string? Narration { get; set; }
}

public class ReceiptDetailDto : ReceiptListItemDto
{
    public string? BranchName { get; set; }
    public string? CardBrand { get; set; }
    public string? CardLastFour { get; set; }
    public string? AuthorisationCode { get; set; }
    public decimal ExchangeRate { get; set; }
    public DateTime? PostedAt { get; set; }
    public string? PostedByName { get; set; }
    public DateTime? ReversedAt { get; set; }
    public string? ReversalReason { get; set; }
    public string? DocumentUrl { get; set; }
    public ChequeRecordDto? Cheque { get; set; }
    public List<ReceiptAllocationDto> Allocations { get; set; } = [];
}

public class ReceiptAllocationDto
{
    public Guid Id { get; set; }
    public Guid? InstalmentId { get; set; }
    public Guid? RentChargeId { get; set; }
    public Guid? MaintenanceBillId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateOnly? DueDate { get; set; }
    public LedgerEntryKind AppliedTo { get; set; }
    public decimal Amount { get; set; }
    public DateOnly AllocatedOn { get; set; }
    public AllocationOrder AppliedRule { get; set; }
    public bool IsManualOverride { get; set; }
    public string? OverrideReason { get; set; }
    public bool IsReversed { get; set; }
    public int SortOrder { get; set; }
}

public class ReceiptCreateDto
{
    public Guid? PartyId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? MaintenanceBillId { get; set; }
    public Guid? ServiceChargeInvoiceId { get; set; }
    public Guid? DealId { get; set; }
    public Guid? ClientBuildContractId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? OfficeId { get; set; }

    public DateOnly ReceivedOn { get; set; }
    public decimal Amount { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal ExchangeRate { get; set; } = 1m;

    public PaymentInstrument Instrument { get; set; }
    public string? BankName { get; set; }
    public string? BranchName { get; set; }
    public string? InstrumentNumber { get; set; }
    public DateOnly? InstrumentDate { get; set; }
    public string? TransactionReference { get; set; }
    public Guid? BankAccountId { get; set; }
    public string? CardBrand { get; set; }
    public string? CardLastFour { get; set; }
    public string? AuthorisationCode { get; set; }

    /// <summary>Post-dated. Sits on the maturity calendar rather than clearing today.</summary>
    public bool IsPostDated { get; set; }

    public string? Narration { get; set; }

    /// <summary>Leave empty to let the allocation engine decide; supply to override it.</summary>
    public List<ManualAllocationDto> ManualAllocations { get; set; } = [];

    public string? AllocationOverrideReason { get; set; }
    public bool PrintReceipt { get; set; } = true;
}

public class ManualAllocationDto
{
    public Guid? InstalmentId { get; set; }
    public Guid? RentChargeId { get; set; }
    public Guid? MaintenanceBillId { get; set; }
    public LedgerEntryKind AppliedTo { get; set; }
    public decimal Amount { get; set; }
}

/// <summary>What the allocation engine would do, shown before the cashier commits.</summary>
public class AllocationPreviewDto
{
    public decimal Amount { get; set; }
    public AllocationOrder Rule { get; set; }
    public List<ReceiptAllocationDto> Allocations { get; set; } = [];
    public decimal Unallocated { get; set; }
    public decimal EscrowPortion { get; set; }
    public decimal FreePortion { get; set; }
    public decimal SurchargeCleared { get; set; }
    public decimal PrincipalCleared { get; set; }
    public decimal NewOutstanding { get; set; }
    public DateOnly? NewNextDueDate { get; set; }
    public decimal NewCollectionPercent { get; set; }

    /// <summary>Commission that becomes payable because collection crossed a release threshold.</summary>
    public decimal CommissionReleased { get; set; }
}

public class ChequeRecordDto
{
    public Guid Id { get; set; }
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public Guid? BookingId { get; set; }
    public string? BookingReference { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? ReceiptId { get; set; }
    public string? ReceiptNumber { get; set; }

    public string ChequeNumber { get; set; } = string.Empty;
    public string? BankName { get; set; }
    public string? BranchName { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public DateOnly ChequeDate { get; set; }
    public DateOnly ReceivedOn { get; set; }
    public ChequeState State { get; set; }
    public bool IsPostDated { get; set; }
    public int? DaysToMaturity { get; set; }

    public DateOnly? DepositedOn { get; set; }
    public DateOnly? ClearedOn { get; set; }
    public DateOnly? BouncedOn { get; set; }
    public string? BounceReason { get; set; }
    public decimal BounceCharge { get; set; }
    public bool BounceChargeRecovered { get; set; }
    public bool CustomerNotifiedOfBounce { get; set; }
    public DateOnly? ReturnedOn { get; set; }
    public Guid? ReplacementChequeId { get; set; }
    public string? ImageUrl { get; set; }
    public string? Note { get; set; }
}

public class ChequeStateChangeDto
{
    public Guid ChequeRecordId { get; set; }
    public ChequeState NewState { get; set; }
    public DateOnly OnDate { get; set; }
    public Guid? BankAccountId { get; set; }
    public string? BounceReason { get; set; }
    public decimal? BounceCharge { get; set; }
    public bool NotifyCustomer { get; set; } = true;
    public string? Note { get; set; }
}

// ── Ledger ───────────────────────────────────────────────────────────────────

public class CustomerLedgerEntryDto
{
    public Guid Id { get; set; }
    public DateOnly EntryDate { get; set; }
    public LedgerEntryKind Kind { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal RunningBalance { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string? DocumentNumber { get; set; }
    public Guid? SourceDemandId { get; set; }
    public Guid? SourceReceiptId { get; set; }
    public bool IsReversal { get; set; }
}

/// <summary>The customer-facing statement of account, ready to render or print.</summary>
public class StatementOfAccountDto
{
    public Guid BookingId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public string? FatherOrGuardianName { get; set; }
    public string? UnitNumber { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public AreaDto? Area { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public DateOnly StatementDate { get; set; }
    public DateOnly? PeriodFrom { get; set; }
    public DateOnly? PeriodTo { get; set; }

    public decimal TotalConsideration { get; set; }
    public decimal TotalDemanded { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal SurchargeAccrued { get; set; }
    public decimal SurchargeWaived { get; set; }
    public decimal Outstanding { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal CollectionPercent { get; set; }
    public string OutstandingInWords { get; set; } = string.Empty;

    public DateOnly? NextDueDate { get; set; }
    public decimal NextDueAmount { get; set; }

    public List<InstalmentDto> Schedule { get; set; } = [];
    public List<CustomerLedgerEntryDto> Ledger { get; set; } = [];
    public AgeingBucketsDto Ageing { get; set; } = new();
}

public class AgeingBucketsDto
{
    public decimal Current { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Days90Plus { get; set; }
    public decimal SurchargeOutstanding { get; set; }
    public decimal Total { get; set; }
}

// ── The collection desk ──────────────────────────────────────────────────────

/// <summary>
/// One row on the collection worklist. Sorted by recoverable amount rather than alphabetically,
/// because a collector's afternoon is finite.
/// </summary>
public class CollectionWorklistItemDto
{
    public Guid BookingId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public Guid PartyId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool HasWhatsApp { get; set; }
    public string? UnitNumber { get; set; }
    public string ProjectName { get; set; } = string.Empty;

    public decimal OverdueAmount { get; set; }
    public decimal SurchargeAmount { get; set; }
    public decimal TotalOutstanding { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public int DaysOverdue { get; set; }
    public string AgeingBucket { get; set; } = string.Empty;

    public Guid? DunningCaseId { get; set; }
    public int DunningStep { get; set; }
    public string? DunningStepName { get; set; }
    public DateTime? NextStepDueAt { get; set; }

    public DateTime? LastContactedAt { get; set; }
    public string? LastOutcome { get; set; }
    public Guid? ActivePromiseId { get; set; }
    public DateOnly? PromisedDate { get; set; }
    public decimal? PromisedAmount { get; set; }
    public bool PromiseBroken { get; set; }

    public string? AssignedToName { get; set; }
    public int ChequeBounceCount { get; set; }
    public bool IsUnderLitigation { get; set; }
    public int NoticesServed { get; set; }
}

public class CollectionWorklistDto
{
    public List<CollectionWorklistItemDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public decimal TotalOverdue { get; set; }
    public decimal TotalSurcharge { get; set; }
    public AgeingBucketsDto Ageing { get; set; } = new();
    public int PromisesDueToday { get; set; }
    public int BrokenPromises { get; set; }
    public decimal CollectedToday { get; set; }
    public decimal CollectedThisMonth { get; set; }
    public decimal DemandedThisMonth { get; set; }
    public decimal EfficiencyPercent { get; set; }
}

public class PromiseToPayDto
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public string? BookingReference { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public decimal PromisedAmount { get; set; }
    public DateOnly PromisedDate { get; set; }
    public DateTime MadeAt { get; set; }
    public string TakenByName { get; set; } = string.Empty;
    public NotificationChannel TakenVia { get; set; }
    public PromiseState State { get; set; }
    public decimal PaidAmount { get; set; }
    public DateOnly? SettledOn { get; set; }
    public bool SuspendsDunning { get; set; }
    public bool IsDueToday { get; set; }
    public bool IsOverdue { get; set; }
    public string? Note { get; set; }
}

public class PromiseToPayCreateDto
{
    public Guid? BookingId { get; set; }
    public Guid? TenancyId { get; set; }
    public decimal PromisedAmount { get; set; }
    public DateOnly PromisedDate { get; set; }
    public NotificationChannel TakenVia { get; set; } = NotificationChannel.Call;
    public bool SuspendsDunning { get; set; } = true;
    public string? Note { get; set; }
}

public class DunningPolicyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string AppliesTo { get; set; } = "Booking";
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public decimal MinimumOverdueAmount { get; set; }
    public int StepCount { get; set; }
    public int ActiveCaseCount { get; set; }
    public List<DunningStepDto> Steps { get; set; } = [];
}

public class DunningStepDto
{
    public Guid? Id { get; set; }
    public int StepNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DaysAfterDue { get; set; }
    public DunningAction Action { get; set; }
    public NotificationChannel Channel { get; set; }
    public Guid? MessageTemplateId { get; set; }
    public string? MessageTemplateName { get; set; }
    public Guid? DocumentTemplateId { get; set; }
    public string? AssignToRole { get; set; }
    public decimal? FeeAmount { get; set; }
    public bool RequiresApproval { get; set; }
    public bool StopsOnPromise { get; set; }
}

public class DunningCaseDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public Guid? BookingId { get; set; }
    public string? BookingReference { get; set; }
    public Guid? TenancyId { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public DateOnly OpenedOn { get; set; }
    public int CurrentStep { get; set; }
    public string? CurrentStepName { get; set; }
    public DateTime? NextStepDueAt { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal SurchargeAmount { get; set; }
    public int DaysOverdue { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime? LastContactedAt { get; set; }
    public bool IsSuspended { get; set; }
    public string? SuspensionReason { get; set; }
    public bool IsClosed { get; set; }
    public string? Outcome { get; set; }
    public List<DunningEventDto> Events { get; set; } = [];
}

public class DunningEventDto
{
    public Guid Id { get; set; }
    public int StepNumber { get; set; }
    public DunningAction Action { get; set; }
    public NotificationChannel? Channel { get; set; }
    public DateTime OccurredAt { get; set; }
    public bool Succeeded { get; set; }
    public string? FailureReason { get; set; }
    public decimal? AmountAtEvent { get; set; }
    public string? Note { get; set; }
    public Guid? LegalNoticeId { get; set; }
}

public class LegalNoticeDto
{
    public Guid Id { get; set; }
    public string NoticeNumber { get; set; } = string.Empty;
    public string NoticeType { get; set; } = string.Empty;
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public Guid? BookingId { get; set; }
    public string? BookingReference { get; set; }
    public Guid? TenancyId { get; set; }
    public DateOnly IssuedOn { get; set; }
    public DateOnly? ComplyByDate { get; set; }
    public decimal AmountDemanded { get; set; }
    public string? DocumentUrl { get; set; }
    public DateOnly? ServedOn { get; set; }
    public string? ServiceMethod { get; set; }
    public string? ServiceReference { get; set; }
    public string? ServiceEvidenceUrl { get; set; }
    public bool IsAcknowledged { get; set; }
    public bool IsComplied { get; set; }
    public bool IsWithdrawn { get; set; }
    public bool IsOverdue { get; set; }
    public string? IssuedByName { get; set; }
}

public class WriteOffDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public Guid? BookingId { get; set; }
    public string? BookingReference { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? ReasonCodeId { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal SurchargeAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateOnly WriteOffDate { get; set; }
    public string ReasonLabel { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public ApprovalOutcome Outcome { get; set; }
    public bool IsProvisionOnly { get; set; }
}
