using Fitness.Domain.Enums;

namespace Fitness.Application.DTOs;

// ── Schedule & runs ──────────────────────────────────────────────────────────

public class BillingScheduleDto
{
    public Guid Id { get; set; }
    public Guid AgreementId { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public Guid ClubId { get; set; }

    public DateTime DueOn { get; set; }
    public int PeriodNumber { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public ChargeKind ChargeKind { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public Guid? InvoiceId { get; set; }
    public bool IsBilled { get; set; }
    public bool IsSkipped { get; set; }
    public string? SkipReason { get; set; }
    public decimal? OriginalAmount { get; set; }
    public string? AdjustmentNote { get; set; }
}

public class BillingRunDto
{
    public Guid Id { get; set; }
    public string RunNumber { get; set; } = string.Empty;
    public DateTime BillingDate { get; set; }
    public Guid? ClubId { get; set; }
    public string? ClubName { get; set; }
    public Guid? PlanId { get; set; }

    public BillingRunStatus Status { get; set; }
    public bool IsPreview { get; set; }
    public bool IsAutomatic { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? DurationSeconds { get; set; }

    public int TotalScheduled { get; set; }
    public int InvoicesCreated { get; set; }
    public int PaymentsCollected { get; set; }
    public int PaymentsFailed { get; set; }
    public int Skipped { get; set; }
    public int Errors { get; set; }

    public decimal TotalBilled { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalFailed { get; set; }

    /// <summary>Collected as a share of billed. The one number a manager looks at after a run.</summary>
    public int CollectionRatePercent { get; set; }

    public string? ErrorSummary { get; set; }
    public string? RunByName { get; set; }
}

public class BillingRunLineDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? MemberNumber { get; set; }
    public Guid? AgreementId { get; set; }
    public Guid? InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public PaymentFailureReason? FailureReason { get; set; }
    public string? Message { get; set; }
}

public class StartBillingRunDto
{
    public DateTime BillingDate { get; set; }
    public Guid? ClubId { get; set; }
    public Guid? PlanId { get; set; }

    /// <summary>Computes everything and writes nothing, so a manager can look before it fires.</summary>
    public bool PreviewOnly { get; set; } = true;

    /// <summary>Attempt collection as well as raising invoices.</summary>
    public bool CollectPayments { get; set; } = true;
}

// ── Invoices & payments ──────────────────────────────────────────────────────

public class InvoiceSummaryDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? MemberNumber { get; set; }
    public Guid ClubId { get; set; }
    public string? ClubName { get; set; }

    public InvoiceStatus Status { get; set; }
    public DateTime IssuedOn { get; set; }
    public DateTime DueOn { get; set; }
    public DateTime? PaidOn { get; set; }

    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public int DaysOverdue { get; set; }
    public bool HasDunningCase { get; set; }
    public string? SummaryLine { get; set; }
}

public class InvoiceDetailDto : InvoiceSummaryDto
{
    public Guid? AgreementId { get; set; }
    public string? AgreementNumber { get; set; }
    public Guid? CorporateAccountId { get; set; }
    public string? CorporateAccountName { get; set; }
    public Guid? PayerMemberId { get; set; }
    public string? PayerName { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal AmountRefunded { get; set; }
    public decimal ExchangeRate { get; set; }

    public Guid? BillingRunId { get; set; }
    public Guid? DunningCaseId { get; set; }
    public string? DocumentUrl { get; set; }
    public string? Notes { get; set; }
    public bool RemindersSuppressed { get; set; }

    public List<InvoiceLineDto> Lines { get; set; } = [];
    public List<PaymentDto> Payments { get; set; } = [];
    public List<CreditNoteDto> CreditNotes { get; set; } = [];
}

public class InvoiceLineDto
{
    public Guid Id { get; set; }
    public ChargeKind ChargeKind { get; set; }
    public string LineDescription { get; set; } = string.Empty;
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public string? ProrationExplanation { get; set; }
    public Guid? PlanId { get; set; }
    public int DisplayOrder { get; set; }
}

public class PaymentDto
{
    public Guid Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public Guid? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public Guid ClubId { get; set; }

    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
    public decimal RefundedAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public DateTime ReceivedOn { get; set; }
    public DateTime? SettledOn { get; set; }

    public string? ProviderReference { get; set; }
    public string? AuthorisationCode { get; set; }
    public string? CardBrand { get; set; }
    public string? CardLastFour { get; set; }
    public string? MandateReference { get; set; }

    public PaymentFailureReason? FailureReason { get; set; }
    public string? FailureMessage { get; set; }
    public int AttemptNumber { get; set; }

    public string? TakenByName { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Taking money at the desk, or recording that it arrived elsewhere.</summary>
public class TakePaymentDto
{
    public Guid MemberId { get; set; }
    public Guid ClubId { get; set; }

    /// <summary>Null pays down the oldest outstanding balance rather than one invoice.</summary>
    public Guid? InvoiceId { get; set; }

    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public Guid? PaymentMethodRefId { get; set; }

    public string? ProviderReference { get; set; }
    public string? AuthorisationCode { get; set; }
    public string? CardBrand { get; set; }
    public string? CardLastFour { get; set; }

    public Guid? CashSessionId { get; set; }
    public decimal? AmountTendered { get; set; }

    public string? Notes { get; set; }

    /// <summary>Stops a double-tap taking the money twice.</summary>
    public string? IdempotencyKey { get; set; }
}

public class TakePaymentResultDto
{
    public Guid PaymentId { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public decimal AmountTaken { get; set; }
    public decimal ChangeDue { get; set; }
    public decimal RemainingBalance { get; set; }
    public PaymentStatus Status { get; set; }

    /// <summary>Invoices this payment settled or part-settled.</summary>
    public List<InvoiceSummaryDto> AppliedTo { get; set; } = [];

    /// <summary>Set when paying the balance lifted a suspension, so the desk can say so.</summary>
    public bool AccessRestored { get; set; }
}

// ── Payment methods ──────────────────────────────────────────────────────────

public class PaymentMethodRefDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public PaymentMethod Method { get; set; }
    public string? ProviderName { get; set; }

    public string? CardBrand { get; set; }
    public string? CardLastFour { get; set; }
    public int? ExpiryMonth { get; set; }
    public int? ExpiryYear { get; set; }

    public string? BankName { get; set; }
    public string? AccountLastFour { get; set; }
    public string? AccountHolderName { get; set; }

    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public bool IsExpiringSoon { get; set; }
    public bool IsExpired { get; set; }
    public DateTime? LastFailedOn { get; set; }
    public int ConsecutiveFailures { get; set; }

    /// <summary>"Visa •••• 4242, expires 09/27" — what a human recognises.</summary>
    public string DisplayLabel { get; set; } = string.Empty;
}

public class SavePaymentMethodDto
{
    public Guid MemberId { get; set; }
    public PaymentMethod Method { get; set; }

    /// <summary>Gateway token or bank mandate id. Never card data — this API refuses a PAN.</summary>
    public string? ProviderToken { get; set; }
    public string? ProviderName { get; set; }

    public string? CardBrand { get; set; }
    public string? CardLastFour { get; set; }
    public int? ExpiryMonth { get; set; }
    public int? ExpiryYear { get; set; }

    public string? BankName { get; set; }
    public string? AccountLastFour { get; set; }
    public string? AccountHolderName { get; set; }
    public string? MandateReference { get; set; }

    public bool MakeDefault { get; set; } = true;
}

// ── Dunning ──────────────────────────────────────────────────────────────────

public class DunningPolicyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }
    public bool IsDefault { get; set; }
    public int WriteOffAfterDays { get; set; }
    public int SuspendAccessAfterDays { get; set; }
    public bool IsActive { get; set; }

    public List<DunningStepDto> Steps { get; set; } = [];

    public int OpenCaseCount { get; set; }
    public decimal AmountInRecovery { get; set; }
}

public class DunningStepDto
{
    public Guid Id { get; set; }
    public Guid DunningPolicyId { get; set; }
    public int StepNumber { get; set; }
    public int DelayDays { get; set; }
    public DunningAction Action { get; set; }
    public MessageChannel? Channel { get; set; }
    public Guid? MessageTemplateId { get; set; }
    public string? MessageTemplateName { get; set; }
    public decimal FeeAmount { get; set; }
    public bool SkipOnTechnicalFailure { get; set; }
}

public class DunningCaseDto
{
    public Guid Id { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? MemberNumber { get; set; }
    public string? MemberPhone { get; set; }
    public Guid? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public Guid ClubId { get; set; }
    public string? ClubName { get; set; }

    public DunningCaseStatus Status { get; set; }
    public DateTime OpenedOn { get; set; }
    public DateTime? ClosedOn { get; set; }
    public int DaysOpen { get; set; }

    public decimal AmountOutstanding { get; set; }
    public decimal AmountRecovered { get; set; }
    public decimal LateFeesAdded { get; set; }

    public PaymentFailureReason InitialFailureReason { get; set; }
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public string? NextAction { get; set; }
    public DateTime? NextStepDueOn { get; set; }

    public int RetryAttempts { get; set; }
    public DateTime? LastRetryOn { get; set; }

    public bool IsPaused { get; set; }
    public string? PauseReason { get; set; }
    public Guid? AssignedToStaffId { get; set; }
    public string? AssignedToName { get; set; }

    /// <summary>Whether the card on file is itself the problem, which changes what to do next.</summary>
    public bool HasValidPaymentMethod { get; set; }
    public bool AccessSuspended { get; set; }

    public List<DunningEventDto> Events { get; set; } = [];
}

public class DunningEventDto
{
    public Guid Id { get; set; }
    public int StepNumber { get; set; }
    public DunningAction Action { get; set; }
    public DateTime OccurredAt { get; set; }
    public bool Succeeded { get; set; }
    public string? Detail { get; set; }
    public decimal? AmountCollected { get; set; }
    public string? PerformedByName { get; set; }
}

public class DunningActionDto
{
    public Guid DunningCaseId { get; set; }

    /// <summary>Retry now, pause, resume, assign, write off, or record a promise to pay.</summary>
    public string Action { get; set; } = string.Empty;

    public string? Note { get; set; }
    public Guid? AssignToStaffId { get; set; }
    public DateTime? PromiseToPayOn { get; set; }
}

// ── Credits, refunds, write-offs ─────────────────────────────────────────────

public class CreditNoteDto
{
    public Guid Id { get; set; }
    public string CreditNoteNumber { get; set; } = string.Empty;
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public Guid? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public DateTime IssuedOn { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool AppliedToBalance { get; set; }
    public string? ApprovedByName { get; set; }
    public string? DocumentUrl { get; set; }
}

public class IssueCreditNoteDto
{
    public Guid MemberId { get; set; }
    public Guid? InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;

    /// <summary>Credit the member's balance rather than reducing a specific invoice.</summary>
    public bool AppliedToBalance { get; set; }
}

public class RefundDto
{
    public Guid Id { get; set; }
    public string RefundNumber { get; set; } = string.Empty;
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime RequestedOn { get; set; }
    public DateTime? ProcessedOn { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool ToOriginalMethod { get; set; }
    public string? ApprovedByName { get; set; }
}

public class IssueRefundDto
{
    public Guid MemberId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;

    /// <summary>Back to the card, or onto the member's credit balance.</summary>
    public bool ToOriginalMethod { get; set; } = true;

    public Guid? CashSessionId { get; set; }
}

public class WriteOffDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid? DunningCaseId { get; set; }
    public decimal Amount { get; set; }
    public DateTime WrittenOffOn { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ApprovedByName { get; set; }
    public bool WasRecovered { get; set; }
    public DateTime? RecoveredOn { get; set; }
    public decimal RecoveredAmount { get; set; }
}

public class MemberLedgerEntryDto
{
    public Guid Id { get; set; }
    public LedgerEntryKind Kind { get; set; }
    public DateTime OccurredAt { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string EntryDescription { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "USD";

    public Guid? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? CreditNoteId { get; set; }
    public Guid? RefundId { get; set; }

    /// <summary>Where clicking the row goes.</summary>
    public string? DrillRoute { get; set; }
}

// ── Deferred revenue ─────────────────────────────────────────────────────────

public class DeferredRevenueScheduleDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public Guid? AgreementId { get; set; }
    public Guid? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }

    public RevenueRecognitionBasis Basis { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal RecognisedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public DateTime ServiceStart { get; set; }
    public DateTime ServiceEnd { get; set; }
    public int TotalUnits { get; set; }
    public int ConsumedUnits { get; set; }

    public bool IsClosed { get; set; }
    public int PercentRecognised { get; set; }
}

/// <summary>
/// The roll-forward an accountant asks for: opening liability, what was added, what was earned,
/// closing liability. The report that reconciles this app's revenue with the ledger's.
/// </summary>
public class DeferredRevenueReportDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public decimal OpeningBalance { get; set; }
    public decimal Additions { get; set; }
    public decimal Recognised { get; set; }
    public decimal Released { get; set; }
    public decimal ClosingBalance { get; set; }

    public List<DeferredRevenueLineDto> ByCategory { get; set; } = [];
    public List<DeferredRevenueLineDto> ByMonth { get; set; } = [];
}

public class DeferredRevenueLineDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Opening { get; set; }
    public decimal Additions { get; set; }
    public decimal Recognised { get; set; }
    public decimal Closing { get; set; }
    public int ScheduleCount { get; set; }
}

// ── Cash ─────────────────────────────────────────────────────────────────────

public class CashSessionDto
{
    public Guid Id { get; set; }
    public string SessionNumber { get; set; } = string.Empty;
    public Guid ClubId { get; set; }
    public string? ClubName { get; set; }
    public string? OpenedByName { get; set; }
    public string? ClosedByName { get; set; }

    public CashSessionStatus Status { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public decimal OpeningFloat { get; set; }
    public decimal CashSales { get; set; }
    public decimal CardSales { get; set; }
    public decimal OtherSales { get; set; }
    public decimal Refunds { get; set; }
    public decimal PaidIn { get; set; }
    public decimal PaidOut { get; set; }
    public decimal Drops { get; set; }

    public decimal ExpectedCash { get; set; }
    public decimal CountedCash { get; set; }
    public decimal Variance { get; set; }
    public bool WasBlindCount { get; set; }

    public int TransactionCount { get; set; }
    public string? VarianceNote { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public List<CashMovementDto> Movements { get; set; } = [];
}

public class CashMovementDto
{
    public Guid Id { get; set; }
    public CashMovementKind Kind { get; set; }
    public DateTime OccurredAt { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public string? Reference { get; set; }
    public string? StaffName { get; set; }
}

public class OpenCashSessionDto
{
    public Guid ClubId { get; set; }
    public decimal OpeningFloat { get; set; }
    public Guid? StaffId { get; set; }
}

public class CloseCashSessionDto
{
    public Guid SessionId { get; set; }
    public decimal CountedCash { get; set; }
    public string? VarianceNote { get; set; }

    /// <summary>Denomination breakdown, when the club counts that way.</summary>
    public Dictionary<string, int>? DenominationCounts { get; set; }
}

public class CashMovementRequestDto
{
    public Guid SessionId { get; set; }
    public CashMovementKind Kind { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public string? Reference { get; set; }
}

/// <summary>The day-end read: what was taken, by method, with the variance.</summary>
public class DayEndReadDto
{
    public Guid ClubId { get; set; }
    public string ClubName { get; set; } = string.Empty;
    public DateTime ForDate { get; set; }
    public bool IsZRead { get; set; }

    public decimal TotalTakings { get; set; }
    public decimal CashTakings { get; set; }
    public decimal CardTakings { get; set; }
    public decimal DirectDebitCollected { get; set; }
    public decimal OtherTakings { get; set; }
    public decimal Refunds { get; set; }
    public decimal NetTakings { get; set; }

    public decimal ExpectedCash { get; set; }
    public decimal CountedCash { get; set; }
    public decimal Variance { get; set; }

    public int TransactionCount { get; set; }
    public int JoinCount { get; set; }
    public int CancellationCount { get; set; }
    public int VisitCount { get; set; }

    public List<RevenueLineDto> ByCategory { get; set; } = [];
    public string CurrencyCode { get; set; } = "USD";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class RevenueLineDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Count { get; set; }
    public decimal PercentOfTotal { get; set; }
}

// ── Gift cards ───────────────────────────────────────────────────────────────

public class GiftCardDto
{
    public Guid Id { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public Guid ClubId { get; set; }
    public decimal InitialValue { get; set; }
    public decimal Balance { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public DateTime IssuedOn { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public Guid? PurchasedByMemberId { get; set; }
    public string? PurchasedByName { get; set; }
    public string? RecipientName { get; set; }
    public string? RecipientEmail { get; set; }
    public string? Message { get; set; }
    public bool IsRedeemed { get; set; }
    public bool IsCancelled { get; set; }
    public bool IsExpired { get; set; }
}

public class IssueGiftCardDto
{
    public Guid ClubId { get; set; }
    public decimal Value { get; set; }
    public string? CardNumber { get; set; }
    public Guid? PurchasedByMemberId { get; set; }
    public string? RecipientName { get; set; }
    public string? RecipientEmail { get; set; }
    public string? Message { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Card;
    public Guid? CashSessionId { get; set; }
}
