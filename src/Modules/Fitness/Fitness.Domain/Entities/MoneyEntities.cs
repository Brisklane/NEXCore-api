using Fitness.Domain.Enums;
using Nexcore.SharedKernel;

namespace Fitness.Domain.Entities;

/// <summary>
/// The forward schedule of what an agreement will charge and when.
///
/// Materialised rather than computed on demand, because "what is my next payment and when?" is
/// the single most-asked question at a gym desk, and because a schedule row is what a freeze
/// shifts, an amendment rewrites and a dunning case chases. Computing it from the agreement each
/// time would mean every one of those had to reproduce the same arithmetic.
/// </summary>
public class BillingSchedule : BaseEntity
{
    public Guid AgreementId { get; set; }
    public Agreement? Agreement { get; set; }

    public Guid MemberId { get; set; }
    public Guid ClubId { get; set; }

    public DateTime DueOn { get; set; }

    /// <summary>Which billing period this row is — 1 of 12 on an instalment plan.</summary>
    public int PeriodNumber { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public ChargeKind ChargeKind { get; set; } = ChargeKind.MembershipDues;

    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>Set once the billing run turns this row into a real invoice.</summary>
    public Guid? InvoiceId { get; set; }
    public bool IsBilled { get; set; }

    /// <summary>Skipped because the member was frozen over this period.</summary>
    public bool IsSkipped { get; set; }
    public string? SkipReason { get; set; }

    /// <summary>Set when a promotion or amendment changed the amount after the row was created.</summary>
    public decimal? OriginalAmount { get; set; }
    public string? AdjustmentNote { get; set; }
}

/// <summary>
/// One execution of the billing job.
///
/// Recorded as a row rather than run and forgotten because it must be resumable and idempotent:
/// a run that dies halfway through four thousand members has to be restartable without
/// double-charging the first two thousand, and the only way to know where it got to is to have
/// written it down.
/// </summary>
public class BillingRun : BaseEntity
{
    public string RunNumber { get; set; } = string.Empty;

    /// <summary>Charges due on or before this date are picked up.</summary>
    public DateTime BillingDate { get; set; }

    /// <summary>Null runs every club.</summary>
    public Guid? ClubId { get; set; }
    public Guid? PlanId { get; set; }

    public BillingRunStatus Status { get; set; } = BillingRunStatus.Draft;

    /// <summary>A preview computes everything and writes nothing, so a manager can look before it fires.</summary>
    public bool IsPreview { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public int TotalScheduled { get; set; }
    public int InvoicesCreated { get; set; }
    public int PaymentsCollected { get; set; }
    public int PaymentsFailed { get; set; }
    public int Skipped { get; set; }
    public int Errors { get; set; }

    public decimal TotalBilled { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalFailed { get; set; }

    public string? ErrorSummary { get; set; }
    public Guid? RunByUserId { get; set; }

    /// <summary>Set when the nightly scheduler started it rather than a person.</summary>
    public bool IsAutomatic { get; set; }

    public ICollection<BillingRunLine> Lines { get; set; } = [];
}

/// <summary>What happened to one member in one billing run. The audit trail behind every charge.</summary>
public class BillingRunLine : BaseEntity
{
    public Guid BillingRunId { get; set; }
    public BillingRun? BillingRun { get; set; }

    public Guid MemberId { get; set; }
    public Guid? AgreementId { get; set; }
    public Guid? BillingScheduleId { get; set; }
    public Guid? InvoiceId { get; set; }

    public decimal Amount { get; set; }

    /// <summary>Billed, collected, failed, skipped or errored — in plain words for the run report.</summary>
    public string Outcome { get; set; } = string.Empty;
    public PaymentFailureReason? FailureReason { get; set; }
    public string? Message { get; set; }
}

/// <summary>A bill raised against a member. Numbered by the platform document-sequence engine.</summary>
public class FitnessInvoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;

    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid ClubId { get; set; }
    public Guid? AgreementId { get; set; }

    /// <summary>Set when an employer, insurer or another member is paying rather than the member.</summary>
    public Guid? CorporateAccountId { get; set; }
    public Guid? ThirdPartyPayerId { get; set; }
    public Guid? PayerMemberId { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public DateTime IssuedOn { get; set; } = DateTime.UtcNow;
    public DateTime DueOn { get; set; }
    public DateTime? PaidOn { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountRefunded { get; set; }

    /// <summary>Total less paid less refunded less credited. Held rather than derived so ageing is one indexed read.</summary>
    public decimal BalanceDue { get; set; }

    public string CurrencyCode { get; set; } = "USD";

    /// <summary>Rate against the company's reporting currency, stamped at issue.</summary>
    public decimal ExchangeRate { get; set; } = 1;

    public Guid? BillingRunId { get; set; }

    /// <summary>Set when collection failed and the dunning ladder took over.</summary>
    public Guid? DunningCaseId { get; set; }

    public string? DocumentUrl { get; set; }
    public string? Notes { get; set; }

    /// <summary>Suppresses the reminder ladder for an invoice being handled by hand.</summary>
    public bool RemindersSuppressed { get; set; }

    public ICollection<FitnessInvoiceLine> Lines { get; set; } = [];
    public ICollection<FitnessPayment> Payments { get; set; } = [];
}

/// <summary>One charge on an invoice, with enough on it to explain itself to the member.</summary>
public class FitnessInvoiceLine : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public FitnessInvoice? Invoice { get; set; }

    public ChargeKind ChargeKind { get; set; }

    public string LineDescription { get; set; } = string.Empty;

    /// <summary>The period this line covers, printed on the invoice so a pro-rata makes sense.</summary>
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }

    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }

    /// <summary>What this line was for — a plan, a service, a product, a booking.</summary>
    public Guid? SourceEntityId { get; set; }
    public string? SourceEntityType { get; set; }

    public Guid? PlanId { get; set; }
    public Guid? InventoryItemId { get; set; }

    /// <summary>Shown under the line so "£12.90" does not look arbitrary.</summary>
    public string? ProrationExplanation { get; set; }

    public Guid? RevenueAccountId { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Money that arrived.
///
/// Recorded, never processed. The only card data here is what a receipt legitimately carries —
/// brand, last four, an auth code, a provider reference. No PAN, no CVV and no track data is
/// accepted by, transmitted through or stored in this model, anywhere.
/// </summary>
public class FitnessPayment : BaseEntity
{
    public string PaymentNumber { get; set; } = string.Empty;

    public Guid MemberId { get; set; }
    public Guid? InvoiceId { get; set; }
    public FitnessInvoice? Invoice { get; set; }

    public Guid ClubId { get; set; }

    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public decimal Amount { get; set; }
    public decimal RefundedAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public DateTime ReceivedOn { get; set; } = DateTime.UtcNow;
    public DateTime? SettledOn { get; set; }

    // ── Provider reference only ──────────────────────────────────────────────

    /// <summary>Opaque id from the gateway or bank. Meaningless without their system, which is the point.</summary>
    public string? ProviderReference { get; set; }
    public string? AuthorisationCode { get; set; }
    public string? CardBrand { get; set; }
    public string? CardLastFour { get; set; }
    public string? MandateReference { get; set; }

    public PaymentFailureReason? FailureReason { get; set; }
    public string? FailureMessage { get; set; }
    public int AttemptNumber { get; set; } = 1;

    /// <summary>The cash session this landed in, for a payment taken at the desk.</summary>
    public Guid? CashSessionId { get; set; }

    public Guid? TakenByStaffId { get; set; }
    public Guid? BillingRunId { get; set; }
    public Guid? PaymentMethodRefId { get; set; }

    /// <summary>Stops a retried request from taking the money twice.</summary>
    public string? IdempotencyKey { get; set; }

    public string? Notes { get; set; }
}

/// <summary>
/// A pointer to a payment instrument held by someone else.
///
/// The token belongs to the gateway; the mandate belongs to the bank. Fitness stores the handle
/// and the display fields a human needs to recognise which card is which, and nothing more.
/// </summary>
public class PaymentMethodRef : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public PaymentMethod Method { get; set; }

    /// <summary>Gateway token, or the bank's mandate id. Never card data.</summary>
    public string? ProviderToken { get; set; }
    public string? ProviderName { get; set; }

    public string? CardBrand { get; set; }
    public string? CardLastFour { get; set; }
    public int? ExpiryMonth { get; set; }
    public int? ExpiryYear { get; set; }

    public string? BankName { get; set; }
    public string? AccountLastFour { get; set; }
    public string? AccountHolderName { get; set; }

    public bool IsDefault { get; set; }

    /// <summary>Set when the card is within the warning window, so the reminder ladder can chase it.</summary>
    public bool IsExpiringSoon { get; set; }
    public DateTime? LastFailedOn { get; set; }
    public int ConsecutiveFailures { get; set; }
}

/// <summary>A direct-debit or standing-order mandate, and where it is in its own lifecycle.</summary>
public class PaymentMandate : BaseEntity
{
    public Guid MemberId { get; set; }
    public Guid? PaymentMethodRefId { get; set; }

    public string MandateReference { get; set; } = string.Empty;
    public string? SchemeName { get; set; }

    public DateTime? SignedOn { get; set; }
    public DateTime? FirstCollectionOn { get; set; }
    public DateTime? CancelledOn { get; set; }

    /// <summary>Days of notice the scheme requires before a collection. Drives how early billing must submit.</summary>
    public int AdvanceNoticeDays { get; set; } = 3;

    public string? CancellationReason { get; set; }
}

/// <summary>
/// The ladder a failed collection walks down.
///
/// Configured as ordered steps rather than a retry count because the thing that recovers money is
/// the *sequence* — retry, then email, then text, then ask for a new card, then have someone
/// phone, then suspend — and every club wants that sequence to be slightly different.
/// </summary>
public class DunningPolicy : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }

    public bool IsDefault { get; set; }

    /// <summary>Gives up and writes off after this many days. Zero means never automatically.</summary>
    public int WriteOffAfterDays { get; set; }

    /// <summary>Suspends door access once the case is this many days old.</summary>
    public int SuspendAccessAfterDays { get; set; } = 14;

    public ICollection<DunningStep> Steps { get; set; } = [];
}

/// <summary>One rung of the ladder: wait this long, then do this.</summary>
public class DunningStep : BaseEntity
{
    public Guid DunningPolicyId { get; set; }
    public DunningPolicy? DunningPolicy { get; set; }

    public int StepNumber { get; set; }

    /// <summary>Days after the failure (or after the previous step) before this one fires.</summary>
    public int DelayDays { get; set; }

    public DunningAction Action { get; set; }
    public MessageChannel? Channel { get; set; }
    public Guid? MessageTemplateId { get; set; }

    /// <summary>Amount for an <see cref="DunningAction.AddLateFee"/> step.</summary>
    public decimal FeeAmount { get; set; }

    /// <summary>Skips this step for a member whose failure was technical rather than a lack of funds.</summary>
    public bool SkipOnTechnicalFailure { get; set; } = true;
}

/// <summary>
/// A live collection problem for one member. Opened when a payment fails, closed when the money
/// arrives or the debt is written off.
/// </summary>
public class DunningCase : BaseEntity
{
    public string CaseNumber { get; set; } = string.Empty;

    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid? InvoiceId { get; set; }
    public Guid? AgreementId { get; set; }
    public Guid ClubId { get; set; }
    public Guid DunningPolicyId { get; set; }

    public DunningCaseStatus Status { get; set; } = DunningCaseStatus.Open;

    public DateTime OpenedOn { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedOn { get; set; }

    public decimal AmountOutstanding { get; set; }
    public decimal AmountRecovered { get; set; }
    public decimal LateFeesAdded { get; set; }

    public PaymentFailureReason InitialFailureReason { get; set; }

    public int CurrentStep { get; set; }
    public DateTime? NextStepDueOn { get; set; }

    public int RetryAttempts { get; set; }
    public DateTime? LastRetryOn { get; set; }

    /// <summary>Someone is dealing with it by hand; the automation stands down.</summary>
    public bool IsPaused { get; set; }
    public string? PauseReason { get; set; }

    public Guid? AssignedToStaffId { get; set; }

    public ICollection<DunningEvent> Events { get; set; } = [];
}

/// <summary>Something the ladder did, or something a human did instead. One line per action.</summary>
public class DunningEvent : BaseEntity
{
    public Guid DunningCaseId { get; set; }
    public DunningCase? DunningCase { get; set; }

    public int StepNumber { get; set; }
    public DunningAction Action { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public bool Succeeded { get; set; }
    public string? Detail { get; set; }

    public decimal? AmountCollected { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? MessageLogId { get; set; }

    /// <summary>Set when a member of staff did it rather than the scheduler.</summary>
    public Guid? PerformedByUserId { get; set; }
}

/// <summary>A reduction of what is owed, raised against an invoice with a reason and an approver.</summary>
public class CreditNote : BaseEntity
{
    public string CreditNoteNumber { get; set; } = string.Empty;

    public Guid MemberId { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid ClubId { get; set; }

    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public DateTime IssuedOn { get; set; } = DateTime.UtcNow;
    public string Reason { get; set; } = string.Empty;

    /// <summary>Whether it went back to the card, or onto the member's credit balance.</summary>
    public bool AppliedToBalance { get; set; }

    public Guid? ApprovedByUserId { get; set; }
    public string? DocumentUrl { get; set; }
}

/// <summary>Money going back out, with the trail of who authorised it.</summary>
public class Refund : BaseEntity
{
    public string RefundNumber { get; set; } = string.Empty;

    public Guid MemberId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid ClubId { get; set; }

    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public DateTime RequestedOn { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedOn { get; set; }

    public string Reason { get; set; } = string.Empty;

    /// <summary>Refunded to the original method, or issued as member credit.</summary>
    public bool ToOriginalMethod { get; set; } = true;

    public string? ProviderReference { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public Guid? CashSessionId { get; set; }
}

/// <summary>Debt the club has given up on, recorded rather than deleted so the loss is visible.</summary>
public class WriteOff : BaseEntity
{
    public Guid MemberId { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid? DunningCaseId { get; set; }
    public Guid ClubId { get; set; }

    public decimal Amount { get; set; }
    public DateTime WrittenOffOn { get; set; } = DateTime.UtcNow;
    public string Reason { get; set; } = string.Empty;

    public Guid? ApprovedByUserId { get; set; }

    /// <summary>Set if the member later paid anyway, which happens more than people expect.</summary>
    public bool WasRecovered { get; set; }
    public DateTime? RecoveredOn { get; set; }
    public decimal RecoveredAmount { get; set; }
}

/// <summary>
/// Every money movement on a member's account, in order, with a running balance.
///
/// One append-only table rather than deriving a balance by summing four others. The balance a
/// receptionist reads to a member has to be the same number every time it is asked for, and the
/// only way to guarantee that is to write it down once.
/// </summary>
public class MemberLedgerEntry : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid ClubId { get; set; }

    public LedgerEntryKind Kind { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>Positive increases what the member owes; negative reduces it.</summary>
    public decimal Amount { get; set; }

    /// <summary>Account balance immediately after this entry.</summary>
    public decimal BalanceAfter { get; set; }

    public string EntryDescription { get; set; } = string.Empty;

    public Guid? InvoiceId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? CreditNoteId { get; set; }
    public Guid? RefundId { get; set; }
    public Guid? WriteOffId { get; set; }

    public string CurrencyCode { get; set; } = "USD";
}

/// <summary>
/// Money paid up front that has not been earned yet.
///
/// An annual membership collected in January is a liability in February, not revenue, and a
/// ten-pack is a liability until the tenth session is taken. Every serious club platform reports
/// this and almost no SMB tool does, which is why a club's own P&amp;L usually disagrees with its
/// accountant's.
/// </summary>
public class DeferredRevenueSchedule : BaseEntity
{
    public Guid MemberId { get; set; }
    public Guid? AgreementId { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid ClubId { get; set; }

    public RevenueRecognitionBasis Basis { get; set; } = RevenueRecognitionBasis.StraightLine;

    public decimal TotalAmount { get; set; }
    public decimal RecognisedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public DateTime ServiceStart { get; set; }
    public DateTime ServiceEnd { get; set; }

    /// <summary>For consumption-based deferral: how many credits the amount covers, and how many are gone.</summary>
    public int TotalUnits { get; set; }
    public int ConsumedUnits { get; set; }

    public Guid? RevenueAccountId { get; set; }
    public Guid? DeferredAccountId { get; set; }

    public bool IsClosed { get; set; }
    public DateTime? ClosedOn { get; set; }

    public ICollection<DeferredRevenueEntry> Entries { get; set; } = [];
}

/// <summary>One release of deferred revenue into earned income, on a date, for a reason.</summary>
public class DeferredRevenueEntry : BaseEntity
{
    public Guid ScheduleId { get; set; }
    public DeferredRevenueSchedule? Schedule { get; set; }

    public DateTime RecognisedOn { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Time passing, or a credit consumed — which is what makes the number auditable.</summary>
    public string Trigger { get; set; } = string.Empty;

    public int UnitsConsumed { get; set; }
    public Guid? SourceEntityId { get; set; }

    /// <summary>Set once the entry has been posted to Accounting.</summary>
    public bool IsPosted { get; set; }
    public DateTime? PostedAt { get; set; }
}

/// <summary>Prepaid value a member holds, usable against any future charge.</summary>
public class MemberCreditBalance : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public decimal Balance { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>Credit issued as goodwill often expires; credit from a refund usually does not.</summary>
    public DateTime? ExpiresOn { get; set; }

    public DateTime? LastMovementOn { get; set; }
}

/// <summary>A gift card or voucher, and the liability it represents until it is redeemed.</summary>
public class GiftCard : BaseEntity
{
    public string CardNumber { get; set; } = string.Empty;
    public Guid ClubId { get; set; }

    public decimal InitialValue { get; set; }
    public decimal Balance { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public DateTime IssuedOn { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresOn { get; set; }

    public Guid? PurchasedByMemberId { get; set; }
    public string? RecipientName { get; set; }
    public string? RecipientEmail { get; set; }
    public string? Message { get; set; }

    public bool IsRedeemed { get; set; }
    public bool IsCancelled { get; set; }

    public ICollection<GiftCardTransaction> Transactions { get; set; } = [];
}

/// <summary>A top-up or redemption against a gift card.</summary>
public class GiftCardTransaction : BaseEntity
{
    public Guid GiftCardId { get; set; }
    public GiftCard? GiftCard { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>Negative for a redemption, positive for a top-up.</summary>
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }

    public Guid? MemberId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? SaleId { get; set; }
    public string? Note { get; set; }
}
