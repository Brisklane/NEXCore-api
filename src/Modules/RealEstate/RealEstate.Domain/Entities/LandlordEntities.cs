using Nexcore.SharedKernel;
using RealEstate.Domain.Enums;

namespace RealEstate.Domain.Entities;

/// <summary>
/// Somebody whose property we manage and whose money we hold. Distinct from the Party record
/// because the management terms, the fee, the payout arrangements and the tax position all belong
/// to the *relationship*, not to the person.
/// </summary>
public class Landlord : BaseEntity
{
    public Guid PartyId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid? OfficeId { get; set; }
    public Guid? ManagedByUserId { get; set; }

    public ManagementService DefaultService { get; set; } = ManagementService.FullManagement;
    public decimal DefaultFeePercent { get; set; }
    public bool FeeIncludesTax { get; set; }
    public decimal RepairAuthorityLimit { get; set; }

    // ── Payouts ──────────────────────────────────────────────────────────────

    /// <summary>"Monthly", "OnReceipt", "Quarterly". On-receipt is what most small landlords want.</summary>
    public string PayoutFrequency { get; set; } = "Monthly";

    public int PayoutDay { get; set; } = 5;
    public string? BankName { get; set; }
    public string? AccountTitle { get; set; }
    public string? AccountNumber { get; set; }
    public string? SortCodeOrIban { get; set; }
    public bool BankDetailsVerified { get; set; }

    /// <summary>Payouts stopped pending a dispute, a verification or a legal instruction.</summary>
    public bool PayoutsOnHold { get; set; }
    public string? HoldReason { get; set; }

    // ── Tax ──────────────────────────────────────────────────────────────────

    public bool IsNonResident { get; set; }

    /// <summary>Withholding on rent paid to a non-resident, which the agent is liable for.</summary>
    public decimal WithholdingPercent { get; set; }

    public string? TaxExemptionReference { get; set; }
    public DateOnly? ExemptionValidUntil { get; set; }

    // ── Float ────────────────────────────────────────────────────────────────

    public decimal FloatRequired { get; set; }
    public decimal FloatBalance { get; set; }

    public NotificationChannel StatementChannel { get; set; } = NotificationChannel.Email;
    public bool PortalAccessEnabled { get; set; } = true;

    public int PropertyCount { get; set; }
    public decimal TotalRentCollected { get; set; }
    public decimal CurrentBalance { get; set; }

    public string? Notes { get; set; }

    public ICollection<ManagementAgreement> Agreements { get; set; } = [];
}

/// <summary>The signed terms for one landlord, or for one property of theirs.</summary>
public class ManagementAgreement : BaseEntity
{
    public Guid LandlordId { get; set; }
    public Landlord? Landlord { get; set; }

    public Guid? PropertyId { get; set; }
    public string Reference { get; set; } = string.Empty;

    public ManagementService Service { get; set; } = ManagementService.FullManagement;
    public decimal FeePercent { get; set; }
    public decimal FixedMonthlyFee { get; set; }
    public decimal SetupFee { get; set; }
    public decimal RenewalFee { get; set; }
    public decimal TenantFindFee { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int NoticePeriodDays { get; set; } = 30;

    public decimal RepairAuthorityLimit { get; set; }
    public bool CanSignTenancyOnBehalf { get; set; }
    public bool CanServeNoticeOnBehalf { get; set; }
    public bool HoldsDeposit { get; set; } = true;

    public Guid? DocumentId { get; set; }
    public DateOnly? SignedOn { get; set; }
    public DateOnly? TerminatedOn { get; set; }
    public Guid? TerminationReasonCodeId { get; set; }
}

/// <summary>
/// What the landlord earned and what we took, per period. The single document that decides whether
/// a landlord keeps their business with an agency.
/// </summary>
public class OwnerStatement : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid LandlordId { get; set; }
    public Guid? PropertyId { get; set; }

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

    public string CurrencyCode { get; set; } = AppConstants.DefaultCurrency;

    public Guid? DocumentId { get; set; }
    public string? DocumentUrl { get; set; }
    public Guid? OwnerPayoutId { get; set; }

    public bool IsPublishedToPortal { get; set; }
    public bool IsSent { get; set; }
    public DateTime? SentAt { get; set; }

    public ICollection<OwnerStatementLine> Lines { get; set; } = [];
}

public class OwnerStatementLine : BaseEntity
{
    public Guid OwnerStatementId { get; set; }
    public OwnerStatement? Statement { get; set; }

    public DateOnly EntryDate { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? TenancyId { get; set; }

    /// <summary>"Rent", "ManagementFee", "Repair", "Compliance", "Insurance", "Tax", "Float", "Adjustment".</summary>
    public string Category { get; set; } = string.Empty;

    public decimal IncomeAmount { get; set; }
    public decimal DeductionAmount { get; set; }

    public Guid? WorkOrderId { get; set; }
    public Guid? ReceiptId { get; set; }

    /// <summary>The contractor's invoice, attached so the landlord can see what they paid for.</summary>
    public string? SupportingDocumentUrl { get; set; }

    public int SortOrder { get; set; }
}

/// <summary>A batch payment to landlords, with a bank file and an advice each.</summary>
public class OwnerPayout : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public DateOnly PayoutDate { get; set; }
    public Guid? OfficeId { get; set; }
    public Guid ClientAccountId { get; set; }

    public int LandlordCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalWithheld { get; set; }

    /// <summary>"Bacs", "Ach", "Sepa", "Rtgs", "Cheque", "Manual".</summary>
    public string PaymentMethod { get; set; } = "Bacs";

    public string? BankFileUrl { get; set; }
    public string? BatchReference { get; set; }

    public DateTime? SubmittedAt { get; set; }
    public Guid? SubmittedByUserId { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public Guid? JournalEntryId { get; set; }

    public bool IsCompleted { get; set; }
    public int FailedCount { get; set; }
    public string? FailureSummary { get; set; }

    public ICollection<OwnerPayoutLine> Lines { get; set; } = [];
}

public class OwnerPayoutLine : BaseEntity
{
    public Guid OwnerPayoutId { get; set; }
    public OwnerPayout? Payout { get; set; }

    public Guid LandlordId { get; set; }
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

/// <summary>What an agent may spend on a landlord's behalf, enforced by the work-order engine.</summary>
public class RepairAuthorityLimit : BaseEntity
{
    public Guid LandlordId { get; set; }
    public Guid? PropertyId { get; set; }

    public decimal LimitAmount { get; set; }

    /// <summary>An emergency — a burst main, a lift with somebody in it — overrides the limit.</summary>
    public decimal EmergencyLimitAmount { get; set; }

    public bool RequiresQuotes { get; set; }
    public int MinimumQuoteCount { get; set; } = 2;
    public DateOnly EffectiveFrom { get; set; }
}

/// <summary>
/// A segregated pot of money that is not ours.
///
/// Commingling client money with operating funds costs a broker their licence in most US states
/// and is regulated in the UK, the UAE and Australia. This ledger is the control that prevents it,
/// so the balance may never go negative and every movement is double-entered inside it.
/// </summary>
public class ClientAccount : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;

    public ClientAccountKind Kind { get; set; }
    public Guid? BankAccountId { get; set; }
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string CurrencyCode { get; set; } = AppConstants.DefaultCurrency;

    public Guid? OfficeId { get; set; }
    public Guid? LandlordId { get; set; }
    public Guid? SocietyId { get; set; }
    public Guid? PropertyId { get; set; }

    public decimal Balance { get; set; }
    public decimal UnallocatedBalance { get; set; }

    /// <summary>Never true in a compliant firm. Its existence is what makes the breach detectable.</summary>
    public bool IsOverdrawn { get; set; }

    public DateOnly? LastReconciledOn { get; set; }
    public int ReconciliationIntervalDays { get; set; } = 30;
}

/// <summary>One movement inside the client ledger. Append-only; corrections are new rows.</summary>
public class ClientLedgerEntry : BaseEntity
{
    public Guid ClientAccountId { get; set; }
    public ClientAccount? Account { get; set; }

    public DateOnly EntryDate { get; set; }

    /// <summary>"RentReceipt", "DepositReceipt", "LandlordPayout", "ContractorPayment",
    /// "FeeTransfer", "DepositReturn", "Interest", "BankCharge", "Correction".</summary>
    public string EntryType { get; set; } = string.Empty;

    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal RunningBalance { get; set; }

    public Guid? PartyId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? ReceiptId { get; set; }
    public Guid? OwnerPayoutLineId { get; set; }
    public Guid? SecurityDepositId { get; set; }

    /// <summary>Matched against the bank statement during reconciliation.</summary>
    public bool IsReconciled { get; set; }
    public DateOnly? ReconciledOn { get; set; }
    public string? BankReference { get; set; }

    public bool IsCorrection { get; set; }
    public Guid? CorrectsEntryId { get; set; }
    public Guid? AuthorisedByUserId { get; set; }
}

/// <summary>
/// The three-way reconciliation: bank statement, control account, and the sum of the individual
/// client balances. All three must agree, and the exception report says so when they do not.
/// </summary>
public class ClientMoneyReconciliation : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid? ClientAccountId { get; set; }
    public Guid? OfficeId { get; set; }
    public DateOnly ReconciliationDate { get; set; }

    public decimal BankStatementBalance { get; set; }
    public decimal LedgerControlBalance { get; set; }
    public decimal SumOfClientBalances { get; set; }

    public decimal UnpresentedPayments { get; set; }
    public decimal UndepositedReceipts { get; set; }
    public decimal AdjustedBankBalance { get; set; }

    /// <summary>Zero in a healthy firm. Anything else is an exception that must be explained.</summary>
    public decimal Difference { get; set; }

    public bool IsBalanced { get; set; }
    public int ExceptionCount { get; set; }

    public Guid? PreparedByUserId { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? SignedOffAt { get; set; }
    public string? BankStatementUrl { get; set; }
    public string? Notes { get; set; }
    public bool IsOverdue { get; set; }
}

/// <summary>A breach or anomaly in client money, raised so it cannot quietly persist to the audit.</summary>
public class ClientMoneyException : BaseEntity
{
    public Guid? ClientMoneyReconciliationId { get; set; }
    public Guid? ClientAccountId { get; set; }

    public ClientMoneyExceptionKind Kind { get; set; }
    public AlertSeverity Severity { get; set; } = AlertSeverity.Critical;

    public decimal? Amount { get; set; }
    public DateOnly RaisedOn { get; set; }

    public Guid? PartyId { get; set; }
    public Guid? LedgerEntryId { get; set; }

    public Guid? AssignedToUserId { get; set; }
    public bool IsResolved { get; set; }
    public DateOnly? ResolvedOn { get; set; }
    public string? Resolution { get; set; }

    /// <summary>Some breaches must be reported to the regulator, and the app should say which.</summary>
    public bool RequiresRegulatoryReport { get; set; }
    public bool IsReported { get; set; }
}
