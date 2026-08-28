using Nexcore.SharedKernel;
using RealEstate.Domain.Enums;

namespace RealEstate.Domain.Entities;

/// <summary>
/// A reusable payment structure for a project — "20% down, 36 monthly, 4 half-yearly, 15% on
/// possession". Approved and versioned, because it is quoted to customers and printed on the cost
/// sheet before anybody signs anything.
/// </summary>
public class PaymentPlanTemplate : BaseEntity
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; } = 1;

    public bool IsDefault { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    public Guid? SurchargePolicyId { get; set; }
    public Guid? DunningPolicyId { get; set; }

    // ── Early-payment rebate ─────────────────────────────────────────────────

    /// <summary>Rebate percent per month of prepayment, or a flat cash discount for paying in full.</summary>
    public decimal EarlyPaymentRebatePercentPerMonth { get; set; }
    public decimal LumpSumDiscountPercent { get; set; }

    /// <summary>Days after booking within which the lump-sum discount is still available.</summary>
    public int LumpSumWindowDays { get; set; }

    public Guid? ApprovalRequestId { get; set; }
    public string? Note { get; set; }

    public ICollection<PaymentPlanTemplateLine> Lines { get; set; } = [];
}

/// <summary>
/// One block of the template — the down payment, the run of monthlies, the half-yearly balloons,
/// the possession balance. Expanded into individual instalments when a booking picks the plan.
/// </summary>
public class PaymentPlanTemplateLine : BaseEntity
{
    public Guid PaymentPlanTemplateId { get; set; }
    public PaymentPlanTemplate? Template { get; set; }

    public InstalmentKind Kind { get; set; }
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    /// <summary>Percentage of total consideration, or zero when <see cref="FixedAmount"/> is used.</summary>
    public decimal Percent { get; set; }
    public decimal FixedAmount { get; set; }

    /// <summary>How many instalments this block generates. One for a down payment, 36 for monthlies.</summary>
    public int Count { get; set; } = 1;

    public InstalmentFrequency Frequency { get; set; } = InstalmentFrequency.OneOff;

    /// <summary>Days after booking the first instalment in this block falls due.</summary>
    public int StartOffsetDays { get; set; }

    /// <summary>Months after booking, used where the offset is naturally monthly rather than daily.</summary>
    public int StartOffsetMonths { get; set; }

    /// <summary>Set for a milestone-linked block. The demand waits for certification, not a date.</summary>
    public Guid? ProjectMilestoneId { get; set; }
    public string? MilestoneCode { get; set; }

    /// <summary>Set on a charge line — corpus, club, documentation — so it bills separately.</summary>
    public ChargeKind? ChargeKind { get; set; }

    public bool IsTaxable { get; set; }
    public decimal TaxPercent { get; set; }
}

/// <summary>
/// The live schedule for one booking. Versioned: a restructure creates a new version and keeps the
/// old one, so the write-back can never lose the original terms.
/// </summary>
public class PaymentPlan : BaseEntity
{
    public Guid BookingId { get; set; }
    public Guid? PaymentPlanTemplateId { get; set; }

    public string Reference { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public bool IsCurrent { get; set; } = true;
    public Guid? SupersedesPlanId { get; set; }

    /// <summary>Set when the plan departs from the template. Forces an approval rather than a silent edit.</summary>
    public bool IsCustom { get; set; }
    public Guid? DeviationApprovalRequestId { get; set; }

    /// <summary>"Original", "UnitChange", "AreaRevision", "Restructure", "PriceRevision", "DiscountApplied".</summary>
    public string? RevisionReason { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public decimal TotalAmount { get; set; }
    public decimal TotalDemanded { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Outstanding { get; set; }

    public Guid? SurchargePolicyId { get; set; }
    public Guid? DunningPolicyId { get; set; }

    public decimal EarlyPaymentRebateEarned { get; set; }

    // ── Restructure specifics ────────────────────────────────────────────────

    public bool IsRestructure { get; set; }
    public decimal RestructureFee { get; set; }
    public Guid? RestructureApprovalRequestId { get; set; }

    public ICollection<Instalment> Instalments { get; set; } = [];
}

/// <summary>
/// One thing the customer owes on one date. The atom the whole collections machine works on.
/// </summary>
public class Instalment : BaseEntity
{
    public Guid PaymentPlanId { get; set; }
    public PaymentPlan? Plan { get; set; }
    public Guid BookingId { get; set; }

    public int SequenceNumber { get; set; }
    public InstalmentKind Kind { get; set; }
    public string Label { get; set; } = string.Empty;

    /// <summary>Null on a milestone-linked instalment until the milestone is certified.</summary>
    public DateOnly? DueDate { get; set; }

    public Guid? ProjectMilestoneId { get; set; }

    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal WaivedAmount { get; set; }
    public decimal Balance { get; set; }

    public InstalmentStatus Status { get; set; } = InstalmentStatus.NotDue;

    // ── Surcharge, accrued nightly ───────────────────────────────────────────

    public decimal SurchargeAccrued { get; set; }
    public decimal SurchargePaid { get; set; }
    public decimal SurchargeWaived { get; set; }
    public DateOnly? SurchargeAccruedUpTo { get; set; }
    public int DaysOverdue { get; set; }

    public Guid? DemandId { get; set; }
    public DateOnly? FirstPaidOn { get; set; }
    public DateOnly? SettledOn { get; set; }

    public ChargeKind? ChargeKind { get; set; }

    /// <summary>Excluded from surcharge and from dunning — a disputed line under investigation.</summary>
    public bool IsOnHold { get; set; }
    public string? HoldReason { get; set; }
}

/// <summary>
/// The notice that money is due, as sent. Generated ahead of the due date, addressed from the
/// party's mailing address, and archived — a demand nobody can produce is a demand that was
/// never made.
/// </summary>
public class Demand : BaseEntity
{
    public string DemandNumber { get; set; } = string.Empty;

    public Guid BookingId { get; set; }
    public Guid? PaymentPlanId { get; set; }
    public Guid? DemandBatchId { get; set; }
    public Guid PartyId { get; set; }

    public DemandStatus Status { get; set; } = DemandStatus.Draft;
    public DateOnly IssuedOn { get; set; }
    public DateOnly DueDate { get; set; }

    public decimal PrincipalAmount { get; set; }
    public decimal SurchargeAmount { get; set; }
    public decimal ArrearsAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public string CurrencyCode { get; set; } = AppConstants.DefaultCurrency;

    /// <summary>Snapshotted from the party at generation, because people move and the archive must not.</summary>
    public string? MailingAddress { get; set; }

    public Guid? GeneratedDocumentId { get; set; }
    public string? DocumentUrl { get; set; }

    public DateTime? SentAt { get; set; }
    public NotificationChannel? SentVia { get; set; }
    public bool EmailSent { get; set; }
    public bool SmsSent { get; set; }
    public bool WhatsAppSent { get; set; }
    public bool PostSent { get; set; }
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>A reminder rather than the first notice, so the ladder does not double-count.</summary>
    public bool IsReminder { get; set; }
    public int ReminderNumber { get; set; }

    public Guid? CancelReasonCodeId { get; set; }

    public ICollection<DemandLine> Lines { get; set; } = [];
}

public class DemandLine : BaseEntity
{
    public Guid DemandId { get; set; }
    public Demand? Demand { get; set; }
    public Guid? InstalmentId { get; set; }

    public DateOnly? DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public LedgerEntryKind Kind { get; set; } = LedgerEntryKind.Demand;
    public int SortOrder { get; set; }
}

/// <summary>
/// One run of the demand generator. Batched so a failure is visible, resumable and never
/// double-raises — a customer who receives the same demand twice stops trusting the developer.
/// </summary>
public class DemandBatch : BaseEntity
{
    public string Reference { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public Guid? ProjectNodeId { get; set; }
    public Guid? ProjectMilestoneId { get; set; }

    public DateOnly RunDate { get; set; }
    public DateOnly? DueDateFrom { get; set; }
    public DateOnly? DueDateTo { get; set; }

    /// <summary>"Scheduled", "Milestone", "Manual", "Reminder".</summary>
    public string RunType { get; set; } = "Scheduled";

    /// <summary>Preview only. Nothing is issued and no customer is contacted.</summary>
    public bool IsDryRun { get; set; }

    public int CandidateCount { get; set; }
    public int GeneratedCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public int SentCount { get; set; }
    public decimal TotalAmount { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public Guid? RunByUserId { get; set; }

    /// <summary>Bookings excluded by hand this run — a disputed file, a customer on a promise.</summary>
    public string? ExclusionsJson { get; set; }

    public string? ErrorSummary { get; set; }
    public bool IsCompleted { get; set; }
}

/// <summary>
/// How late payment is charged for. A policy, not a number somebody types onto a demand — which is
/// how it becomes both disputable and quietly forgotten.
/// </summary>
public class SurchargePolicy : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }

    public SurchargeBasis Basis { get; set; } = SurchargeBasis.PerDayOnOverdue;

    /// <summary>Percentage per day or per month, depending on the basis.</summary>
    public decimal Rate { get; set; }

    /// <summary>Flat penalty per missed instalment, for the flat basis.</summary>
    public decimal FlatAmount { get; set; }

    /// <summary>Days after the due date before anything accrues. Counted in calendar days.</summary>
    public int GraceDays { get; set; } = 7;

    /// <summary>Compounding turns a late instalment into a debt the customer will never clear.</summary>
    public bool IsCompounding { get; set; }

    /// <summary>Ceiling as a percentage of the overdue principal. Zero means no cap.</summary>
    public decimal CapPercent { get; set; }

    public decimal CapAmount { get; set; }

    /// <summary>Stop accruing after this many days, usually because the file is heading for cancellation.</summary>
    public int? StopAfterDays { get; set; }

    /// <summary>Waiving needs an approval. The alternative is quiet revenue leakage.</summary>
    public bool WaiverRequiresApproval { get; set; } = true;

}

/// <summary>One night's accrual on one instalment. Kept per-day so any figure can be re-derived.</summary>
public class SurchargeAccrual : BaseEntity
{
    public Guid InstalmentId { get; set; }
    public Guid BookingId { get; set; }
    public Guid SurchargePolicyId { get; set; }

    public DateOnly AccrualDate { get; set; }
    public decimal OverdueBase { get; set; }
    public decimal RateApplied { get; set; }
    public decimal Amount { get; set; }
    public decimal CumulativeAmount { get; set; }
    public int DaysOverdue { get; set; }

    /// <summary>Set when the cap bit that night, so the customer can be shown why it stopped growing.</summary>
    public bool CapReached { get; set; }
}

/// <summary>A surcharge written off, with who allowed it and why.</summary>
public class SurchargeWaiver : BaseEntity
{
    public Guid BookingId { get; set; }
    public Guid? InstalmentId { get; set; }
    public string Reference { get; set; } = string.Empty;

    public decimal RequestedAmount { get; set; }
    public decimal ApprovedAmount { get; set; }

    public Guid RequestedByUserId { get; set; }
    public DateOnly RequestedOn { get; set; }
    public Guid ReasonCodeId { get; set; }
    public string? Note { get; set; }

    public Guid? ApprovalRequestId { get; set; }
    public ApprovalOutcome Outcome { get; set; } = ApprovalOutcome.Pending;
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? LedgerEntryId { get; set; }
}

/// <summary>
/// Money in. Against a booking, a tenancy, a maintenance bill or an invoice — one receipt table,
/// because a cashier's day is one queue and reconciling four is how cash goes missing.
/// </summary>
public class Receipt : BaseEntity
{
    public string ReceiptNumber { get; set; } = string.Empty;

    public Guid PartyId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? MaintenanceBillId { get; set; }
    public Guid? ServiceChargeInvoiceId { get; set; }
    public Guid? DealId { get; set; }
    public Guid? ClientBuildContractId { get; set; }
    public Guid? OfficeId { get; set; }

    public ReceiptStatus Status { get; set; } = ReceiptStatus.Draft;
    public DateOnly ReceivedOn { get; set; }
    public decimal Amount { get; set; }
    public decimal AllocatedAmount { get; set; }

    /// <summary>Money in that no instalment has claimed yet. Visible on the ledger, never lost.</summary>
    public decimal UnallocatedAmount { get; set; }

    public string CurrencyCode { get; set; } = AppConstants.DefaultCurrency;
    public decimal ExchangeRate { get; set; } = 1m;

    // ── Instrument ───────────────────────────────────────────────────────────
    // Recorded, never processed. No card number, no CVV, no track data — anywhere.

    public PaymentInstrument Instrument { get; set; } = PaymentInstrument.BankTransfer;
    public string? BankName { get; set; }
    public string? BranchName { get; set; }
    public string? InstrumentNumber { get; set; }
    public DateOnly? InstrumentDate { get; set; }
    public string? TransactionReference { get; set; }
    public Guid? ChequeRecordId { get; set; }
    public Guid? BankAccountId { get; set; }

    /// <summary>Card brand and last four only, for reconciliation against the merchant statement.</summary>
    public string? CardBrand { get; set; }
    public string? CardLastFour { get; set; }
    public string? AuthorisationCode { get; set; }

    // ── Escrow ───────────────────────────────────────────────────────────────

    public Guid? ProjectId { get; set; }

    /// <summary>Split at allocation, not reconstructed later at audit time.</summary>
    public decimal EscrowAmount { get; set; }
    public decimal FreeAmount { get; set; }
    public bool EscrowSplitApplied { get; set; }

    // ── Client money ─────────────────────────────────────────────────────────

    /// <summary>Belongs to a landlord, a tenant or a fund, and must never touch operating cash.</summary>
    public bool IsClientMoney { get; set; }
    public Guid? ClientAccountId { get; set; }

    public Guid ReceivedByUserId { get; set; }
    public DateTime? PostedAt { get; set; }
    public Guid? PostedByUserId { get; set; }
    public Guid? JournalEntryId { get; set; }

    public DateTime? ReversedAt { get; set; }
    public Guid? ReversalReasonCodeId { get; set; }
    public string? ReversalNote { get; set; }

    public string? Narration { get; set; }
    public string? DocumentUrl { get; set; }
    public bool IsPrinted { get; set; }

    public ICollection<ReceiptAllocation> Allocations { get; set; } = [];
}

/// <summary>
/// How one receipt was spread across what was owed. The single most-argued table in the module,
/// so every line records the rule that produced it and whether a human overrode it.
/// </summary>
public class ReceiptAllocation : BaseEntity
{
    public Guid ReceiptId { get; set; }
    public Receipt? Receipt { get; set; }

    public Guid? InstalmentId { get; set; }
    public Guid? RentChargeId { get; set; }
    public Guid? MaintenanceBillId { get; set; }
    public Guid? DemandId { get; set; }

    public LedgerEntryKind AppliedTo { get; set; } = LedgerEntryKind.Demand;
    public decimal Amount { get; set; }
    public DateOnly AllocatedOn { get; set; }
    public int SortOrder { get; set; }

    /// <summary>The rule that chose this line, so the customer can be shown why.</summary>
    public AllocationOrder AppliedRule { get; set; } = AllocationOrder.SurchargeFirstThenOldest;

    public bool IsManualOverride { get; set; }
    public Guid? OverriddenByUserId { get; set; }
    public string? OverrideReason { get; set; }
    public bool IsReversed { get; set; }
}

/// <summary>
/// A cheque and its life. Whole markets run on post-dated cheques, and a bounced one has to
/// reverse an allocation, charge a fee, tell the customer and appear on a register — automatically.
/// </summary>
public class ChequeRecord : BaseEntity
{
    public Guid PartyId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? ReceiptId { get; set; }

    public string ChequeNumber { get; set; } = string.Empty;
    public string? BankName { get; set; }
    public string? BranchName { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = AppConstants.DefaultCurrency;

    public DateOnly ChequeDate { get; set; }
    public DateOnly ReceivedOn { get; set; }
    public ChequeState State { get; set; } = ChequeState.Received;

    /// <summary>Dated forward. Sits on the maturity calendar until its date arrives.</summary>
    public bool IsPostDated { get; set; }

    public DateOnly? DepositedOn { get; set; }
    public Guid? DepositBankAccountId { get; set; }
    public DateOnly? ClearedOn { get; set; }

    public DateOnly? BouncedOn { get; set; }
    public string? BounceReason { get; set; }
    public decimal BounceCharge { get; set; }
    public bool BounceChargeRecovered { get; set; }
    public bool CustomerNotifiedOfBounce { get; set; }

    /// <summary>Handed back rather than banked — typical on cancellation.</summary>
    public DateOnly? ReturnedOn { get; set; }

    public Guid? ReplacementChequeId { get; set; }
    public string? ImageUrl { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// The running account of one customer against one commitment. Every money event lands here, so
/// the statement is a read of one table rather than a reconstruction of six.
/// </summary>
public class CustomerLedgerEntry : BaseEntity
{
    public Guid PartyId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? SocietyUnitId { get; set; }
    public Guid? ClientBuildContractId { get; set; }

    public DateOnly EntryDate { get; set; }
    public LedgerEntryKind Kind { get; set; }

    /// <summary>What the customer owes us. Demands, surcharges and charges.</summary>
    public decimal DebitAmount { get; set; }

    /// <summary>What reduces it. Receipts, waivers, credits and refunds.</summary>
    public decimal CreditAmount { get; set; }

    /// <summary>Balance after this entry. Stored so a statement never has to sum a decade of rows.</summary>
    public decimal RunningBalance { get; set; }

    public string CurrencyCode { get; set; } = AppConstants.DefaultCurrency;

    public Guid? SourceDemandId { get; set; }
    public Guid? SourceReceiptId { get; set; }
    public Guid? SourceInstalmentId { get; set; }
    public Guid? JournalEntryId { get; set; }

    /// <summary>Reversals write a new row rather than editing the old one. The ledger is append-only.</summary>
    public bool IsReversal { get; set; }
    public Guid? ReversesEntryId { get; set; }

    public bool IsVisibleToCustomer { get; set; } = true;
}

/// <summary>
/// A customer said they would pay by a date. Recorded, because a kept promise should stop the
/// dunning ladder and a broken one should escalate it — neither of which happens if it lives in a
/// collector's notebook.
/// </summary>
public class PromiseToPay : BaseEntity
{
    public Guid BookingId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid PartyId { get; set; }
    public Guid? DunningCaseId { get; set; }

    public decimal PromisedAmount { get; set; }
    public DateOnly PromisedDate { get; set; }
    public DateTime MadeAt { get; set; } = DateTime.UtcNow;
    public Guid TakenByUserId { get; set; }
    public NotificationChannel TakenVia { get; set; } = NotificationChannel.Call;

    public PromiseState State { get; set; } = PromiseState.Open;
    public decimal PaidAmount { get; set; }
    public DateOnly? SettledOn { get; set; }

    /// <summary>Holds the ladder while the promise is open, so a paying customer is not chased.</summary>
    public bool SuspendsDunning { get; set; } = true;

    public string? Note { get; set; }
}

/// <summary>
/// A designed sequence of chasing. Multi-touch and multi-channel by construction, because
/// email-only chasing recovers materially less than a ladder that escalates.
/// </summary>
public class DunningPolicy : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }

    /// <summary>"Booking", "Tenancy", "Maintenance". A society's ladder is not a developer's.</summary>
    public string AppliesTo { get; set; } = "Booking";

    public bool IsDefault { get; set; }

    /// <summary>Below this the ladder does not start. Chasing small change costs more than it recovers.</summary>
    public decimal MinimumOverdueAmount { get; set; }

    public ICollection<DunningStep> Steps { get; set; } = [];
}

public class DunningStep : BaseEntity
{
    public Guid DunningPolicyId { get; set; }
    public DunningPolicy? Policy { get; set; }

    public int StepNumber { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Days past due this step fires. Negative fires before the due date as a courtesy reminder.</summary>
    public int DaysAfterDue { get; set; }

    public DunningAction Action { get; set; } = DunningAction.SendReminder;
    public NotificationChannel Channel { get; set; } = NotificationChannel.Email;
    public Guid? MessageTemplateId { get; set; }
    public Guid? DocumentTemplateId { get; set; }

    /// <summary>Who gets the task, when the step creates one rather than sending a message.</summary>
    public string? AssignToRole { get; set; }

    public decimal? FeeAmount { get; set; }
    public bool RequiresApproval { get; set; }
    public bool StopsOnPromise { get; set; } = true;
}

/// <summary>One customer's journey down the ladder.</summary>
public class DunningCase : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid PartyId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? SocietyUnitId { get; set; }
    public Guid DunningPolicyId { get; set; }

    public DateOnly OpenedOn { get; set; }
    public int CurrentStep { get; set; }
    public DateTime? NextStepDueAt { get; set; }

    public decimal OverdueAmount { get; set; }
    public decimal SurchargeAmount { get; set; }
    public int DaysOverdue { get; set; }

    public Guid? AssignedToUserId { get; set; }
    public DateTime? LastContactedAt { get; set; }
    public Guid? ActivePromiseId { get; set; }

    public bool IsSuspended { get; set; }
    public string? SuspensionReason { get; set; }
    public DateOnly? SuspendedUntil { get; set; }

    public bool IsClosed { get; set; }
    public DateOnly? ClosedOn { get; set; }

    /// <summary>"Paid", "Restructured", "Cancelled", "WrittenOff", "Legal".</summary>
    public string? Outcome { get; set; }

    public ICollection<DunningEvent> Events { get; set; } = [];
}

public class DunningEvent : BaseEntity
{
    public Guid DunningCaseId { get; set; }
    public DunningCase? Case { get; set; }

    public int StepNumber { get; set; }
    public DunningAction Action { get; set; }
    public NotificationChannel? Channel { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public bool Succeeded { get; set; } = true;
    public string? FailureReason { get; set; }
    public Guid? ActivityId { get; set; }
    public Guid? LegalNoticeId { get; set; }
    public Guid? FollowUpTaskId { get; set; }
    public decimal? AmountAtEvent { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// A formal notice served on a defaulting customer. Numbered and with proof of service, because
/// this is the document a hearing turns on.
/// </summary>
public class LegalNotice : BaseEntity
{
    public string NoticeNumber { get; set; } = string.Empty;

    public Guid PartyId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? DunningCaseId { get; set; }

    /// <summary>"FirstNotice", "SecondNotice", "FinalNotice", "CancellationNotice",
    /// "NoticeToQuit", "PossessionNotice", "LegalDemand".</summary>
    public string NoticeType { get; set; } = string.Empty;

    public DateOnly IssuedOn { get; set; }

    /// <summary>The deadline the notice sets. Computed on working days from the office calendar.</summary>
    public DateOnly? ComplyByDate { get; set; }

    public decimal AmountDemanded { get; set; }
    public Guid? TemplateVersionId { get; set; }
    public Guid? GeneratedDocumentId { get; set; }
    public string? DocumentUrl { get; set; }

    // ── Proof of service ─────────────────────────────────────────────────────

    public DateOnly? ServedOn { get; set; }

    /// <summary>"Post", "RegisteredPost", "Courier", "Email", "WhatsApp", "InPerson", "Publication".</summary>
    public string? ServiceMethod { get; set; }

    public string? ServiceReference { get; set; }
    public string? ServiceEvidenceUrl { get; set; }
    public bool IsAcknowledged { get; set; }
    public DateOnly? AcknowledgedOn { get; set; }

    public Guid? IssuedByUserId { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public bool IsComplied { get; set; }
    public bool IsWithdrawn { get; set; }
}

/// <summary>An amount given up on, with an authority and an accounting posting.</summary>
public class WriteOff : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid PartyId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? TenancyId { get; set; }

    public decimal PrincipalAmount { get; set; }
    public decimal SurchargeAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public DateOnly WriteOffDate { get; set; }
    public Guid ReasonCodeId { get; set; }
    public string? Note { get; set; }

    public Guid RequestedByUserId { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public ApprovalOutcome Outcome { get; set; } = ApprovalOutcome.Pending;
    public Guid? JournalEntryId { get; set; }

    /// <summary>A provision, not a write-off. Reversible if the money turns up.</summary>
    public bool IsProvisionOnly { get; set; }
}
