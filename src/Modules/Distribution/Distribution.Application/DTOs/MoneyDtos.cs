using Distribution.Domain.Enums;

namespace Distribution.Application.DTOs;

// ── Credit ───────────────────────────────────────────────────────────────────

/// <summary>
/// The credit position, small enough to sync to a phone and complete enough to enforce a block
/// at the counter with no network.
/// </summary>
public class CreditSnapshotDto
{
    public Guid? OutletId { get; set; }
    public Guid? PartnerId { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public decimal CreditLimit { get; set; }
    public decimal TemporaryLimit { get; set; }
    public DateTime? TemporaryLimitExpiresOn { get; set; }
    public decimal EffectiveLimit { get; set; }
    public int CreditDays { get; set; }
    public CreditEnforcement Enforcement { get; set; }

    public decimal OutstandingAmount { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal UnbilledOrderValue { get; set; }
    public decimal AvailableCredit { get; set; }

    public decimal Bucket0To30 { get; set; }
    public decimal Bucket31To60 { get; set; }
    public decimal Bucket61To90 { get; set; }
    public decimal Bucket90Plus { get; set; }

    public int OldestInvoiceDays { get; set; }
    public DateTime? LastPaymentAt { get; set; }
    public decimal LastPaymentAmount { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public int BouncedChequeCount { get; set; }
    public decimal SecurityDeposit { get; set; }
    public decimal AdvanceHeld { get; set; }
    public decimal ProvisionedAmount { get; set; }
    public DateTime? RecalculatedAt { get; set; }
}

public class CreditProfileDto : CreditSnapshotDto
{
    public Guid Id { get; set; }
    public string? OutletName { get; set; }
    public string? PartnerName { get; set; }
    public string? OutletCode { get; set; }
    public OutletChannel? Channel { get; set; }
    public string? TerritoryName { get; set; }
    public string? RouteName { get; set; }
}

/// <summary>The answer to "can this order go through", with the reason when it cannot.</summary>
public class CreditCheckResultDto
{
    public bool IsAllowed { get; set; } = true;
    public bool RequiresOverride { get; set; }
    public CreditEnforcement Enforcement { get; set; }
    public decimal OrderValue { get; set; }
    public decimal AvailableCredit { get; set; }
    public decimal ExcessAmount { get; set; }
    public decimal OverdueAmount { get; set; }
    public bool IsBlocked { get; set; }
    public string? Message { get; set; }
    public CreditSnapshotDto? Snapshot { get; set; }
}

public class SetCreditLimitDto
{
    public Guid? OutletId { get; set; }
    public Guid? PartnerId { get; set; }
    public decimal CreditLimit { get; set; }
    public int CreditDays { get; set; }
    public CreditEnforcement Enforcement { get; set; }
    public string? Reason { get; set; }
}

public class RequestCreditOverrideDto
{
    public Guid? OutletId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? OrderId { get; set; }
    public decimal RequestedAmount { get; set; }
    public Guid? ReasonCodeId { get; set; }
    public string? Justification { get; set; }
    public DateTime? ExpiresOn { get; set; }
}

public class CreditOverrideDto
{
    public Guid Id { get; set; }
    public Guid? OutletId { get; set; }
    public string? OutletName { get; set; }
    public Guid? PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public Guid? OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string? ReasonCodeName { get; set; }
    public string? Justification { get; set; }
    public DateTime RequestedAt { get; set; }
    public string? ApproverName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public bool? IsApproved { get; set; }
    public string? DecisionNote { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public bool IsConsumed { get; set; }
    public bool IsExpired { get; set; }
}

public class DecideCreditOverrideDto
{
    public bool IsApproved { get; set; }
    public decimal ApprovedAmount { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public string? DecisionNote { get; set; }
}

// ── Collections & cheques ────────────────────────────────────────────────────

public class CollectionDto
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public Guid? OutletId { get; set; }
    public string? OutletName { get; set; }
    public Guid? PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public Guid? VisitId { get; set; }
    public Guid? FieldDayId { get; set; }
    public Guid? FieldRepId { get; set; }
    public string? FieldRepName { get; set; }
    public Guid? TripId { get; set; }
    public Guid? SettlementId { get; set; }

    public DateTime CollectedAt { get; set; }
    public PaymentTender Tender { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal Amount { get; set; }
    public decimal CashDiscountAmount { get; set; }
    public Guid? CashDiscountSchemeId { get; set; }
    public Guid? ChequeId { get; set; }
    public string? Reference { get; set; }
    public string? BankName { get; set; }
    public string? CardLast4 { get; set; }
    public string? CardScheme { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal UnallocatedAmount { get; set; }
    public bool IsDeposited { get; set; }
    public bool IsReversed { get; set; }
    public string? ReversalReason { get; set; }
    public string? ReceiptSentTo { get; set; }
    public string? Note { get; set; }

    public ChequeDto? Cheque { get; set; }
    public List<PaymentAllocationDto> Allocations { get; set; } = [];
}

public class CollectionSummaryDto
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime CollectedAt { get; set; }
    public PaymentTender Tender { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string? OutletName { get; set; }
    public string? FieldRepName { get; set; }
    public string? Reference { get; set; }
    public bool IsDeposited { get; set; }
    public ChequeStatus? ChequeStatus { get; set; }
}

public class RecordCollectionDto
{
    public Guid? OutletId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? VisitId { get; set; }
    public Guid? FieldDayId { get; set; }
    public Guid? FieldRepId { get; set; }
    public Guid? TripId { get; set; }
    public DateTime? CollectedAt { get; set; }
    public PaymentTender Tender { get; set; } = PaymentTender.Cash;
    public string? CurrencyCode { get; set; }
    public decimal Amount { get; set; }
    public decimal CashDiscountAmount { get; set; }
    public string? Reference { get; set; }
    public string? BankName { get; set; }
    public string? CardLast4 { get; set; }
    public string? CardScheme { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? ReceiptSentTo { get; set; }
    public string? Note { get; set; }

    /// <summary>Cheque detail. Required when the tender is a cheque.</summary>
    public SaveChequeDto? Cheque { get; set; }

    /// <summary>Empty allocates oldest-first; otherwise the caller decides invoice by invoice.</summary>
    public List<PaymentAllocationDto> Allocations { get; set; } = [];

    public string? IdempotencyKey { get; set; }
}

public class PaymentAllocationDto
{
    public Guid InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public decimal InvoiceAmount { get; set; }
    public decimal OutstandingBefore { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal OutstandingAfter { get; set; }
    public int AgeingDays { get; set; }
}

public class ChequeDto
{
    public Guid Id { get; set; }
    public string ChequeNumber { get; set; } = string.Empty;
    public Guid? OutletId { get; set; }
    public string? OutletName { get; set; }
    public Guid? PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public Guid? CollectionReceiptId { get; set; }
    public string? BankName { get; set; }
    public string? BranchName { get; set; }
    public string? AccountName { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public DateTime ChequeDate { get; set; }
    public DateTime ReceivedOn { get; set; }
    public bool IsPostDated { get; set; }
    public ChequeStatus Status { get; set; }
    public DateTime? DepositedOn { get; set; }
    public string? DepositBankAccount { get; set; }
    public DateTime? ClearedOn { get; set; }
    public DateTime? BouncedOn { get; set; }
    public string? BounceReason { get; set; }
    public decimal BounceCharges { get; set; }
    public bool TriggeredCreditBlock { get; set; }
    public string? Note { get; set; }

    /// <summary>Days until a post-dated cheque becomes bankable.</summary>
    public int? DaysToMaturity { get; set; }
}

public class SaveChequeDto
{
    public string ChequeNumber { get; set; } = string.Empty;
    public string? BankName { get; set; }
    public string? BranchName { get; set; }
    public string? AccountName { get; set; }
    public decimal Amount { get; set; }
    public DateTime ChequeDate { get; set; }
    public string? Note { get; set; }
}

public class UpdateChequeStatusDto
{
    public ChequeStatus Status { get; set; }
    public DateTime? EffectiveOn { get; set; }
    public string? DepositBankAccount { get; set; }
    public string? BounceReason { get; set; }
    public decimal BounceCharges { get; set; }
    public string? Note { get; set; }
}

// ── Claims ───────────────────────────────────────────────────────────────────

public class ClaimDto
{
    public Guid Id { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public ClaimKind Kind { get; set; }
    public ClaimStatus Status { get; set; }
    public Guid? PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public Guid? OutletId { get; set; }
    public string? OutletName { get; set; }
    public Guid? TerritoryId { get; set; }
    public string? TerritoryName { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime SubmittedOn { get; set; }
    public Guid? SchemeId { get; set; }
    public string? SchemeName { get; set; }
    public Guid? ReturnId { get; set; }
    public string? ReturnNumber { get; set; }
    public Guid? MrpRevisionId { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal ClaimedAmount { get; set; }
    public decimal ComputedAmount { get; set; }
    public decimal VarianceAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal SettledAmount { get; set; }
    public bool IsSystemGenerated { get; set; }

    public DateTime? ReviewStartedAt { get; set; }
    public string? QueryNote { get; set; }
    public DateTime? QueriedAt { get; set; }
    public DateTime? ResubmittedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? RejectionNote { get; set; }
    public ClaimSettlementMode? SettlementMode { get; set; }
    public DateTime? SettledAt { get; set; }
    public Guid? CreditNoteId { get; set; }
    public string? SettlementReference { get; set; }
    public int? SettlementDays { get; set; }
    public int AgeingDays { get; set; }
    public string? Note { get; set; }

    /// <summary>True once the claim has aged past the configured settlement SLA.</summary>
    public bool IsBreachingSla { get; set; }

    public List<ClaimLineDto> Lines { get; set; } = [];
    public List<ClaimDocumentDto> Documents { get; set; } = [];
    public List<ClaimStatusEventDto> StatusEvents { get; set; } = [];
}

public class ClaimSummaryDto
{
    public Guid Id { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public ClaimKind Kind { get; set; }
    public ClaimStatus Status { get; set; }
    public string? PartnerName { get; set; }
    public string? SchemeName { get; set; }
    public DateTime SubmittedOn { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal ClaimedAmount { get; set; }
    public decimal ComputedAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal VarianceAmount { get; set; }
    public int AgeingDays { get; set; }
    public bool IsBreachingSla { get; set; }
    public bool IsSystemGenerated { get; set; }
    public int LineCount { get; set; }
    public int DocumentCount { get; set; }
}

public class ClaimLineDto
{
    public Guid Id { get; set; }
    public int DisplayOrder { get; set; }
    public Guid? ItemId { get; set; }
    public string? ItemName { get; set; }
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public Guid? SourceInvoiceId { get; set; }
    public string? SourceInvoiceNumber { get; set; }
    public Guid? SourceOrderId { get; set; }
    public Guid? SchemeApplicationId { get; set; }
    public string Uom { get; set; } = "PCS";
    public decimal Quantity { get; set; }
    public decimal UnitRate { get; set; }
    public decimal ClaimedAmount { get; set; }
    public decimal ComputedAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public Guid? RejectionReasonCodeId { get; set; }
    public string? RejectionReasonName { get; set; }
    public string? RejectionNote { get; set; }
    public string? Note { get; set; }
}

public class ClaimDocumentDto
{
    public Guid Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? Note { get; set; }
}

public class ClaimStatusEventDto
{
    public ClaimStatus FromStatus { get; set; }
    public ClaimStatus ToStatus { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? ActorName { get; set; }
    public string? Note { get; set; }
}

public class SubmitClaimDto
{
    public ClaimKind Kind { get; set; } = ClaimKind.Scheme;
    public Guid? PartnerId { get; set; }
    public Guid? OutletId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public Guid? SchemeId { get; set; }
    public Guid? ReturnId { get; set; }
    public Guid? MrpRevisionId { get; set; }
    public string? CurrencyCode { get; set; }
    public string? Note { get; set; }
    public List<ClaimLineDto> Lines { get; set; } = [];
    public List<ClaimDocumentDto> Documents { get; set; } = [];

    /// <summary>Save as draft rather than submitting for review.</summary>
    public bool SaveAsDraft { get; set; }
}

public class DecideClaimDto
{
    public bool IsApproved { get; set; }

    /// <summary>Per-line approved amounts. A line approved below its claim needs a reason.</summary>
    public List<ClaimLineDecisionDto> Lines { get; set; } = [];
    public Guid? RejectionReasonCodeId { get; set; }
    public string? Note { get; set; }
}

public class ClaimLineDecisionDto
{
    public Guid LineId { get; set; }
    public decimal ApprovedAmount { get; set; }
    public Guid? RejectionReasonCodeId { get; set; }
    public string? RejectionNote { get; set; }
}

public class QueryClaimDto
{
    public string QueryNote { get; set; } = string.Empty;
}

public class SettleClaimDto
{
    public ClaimSettlementMode SettlementMode { get; set; } = ClaimSettlementMode.CreditNote;
    public decimal SettledAmount { get; set; }
    public string? SettlementReference { get; set; }
    public DateTime? SettledAt { get; set; }
    public string? Note { get; set; }
}

// ── Rebates & chargebacks ────────────────────────────────────────────────────

public class RebateAgreementDto
{
    public Guid Id { get; set; }
    public string AgreementNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string RebateBasis { get; set; } = "Volume";
    public decimal ThresholdQuantity { get; set; }
    public decimal ThresholdValue { get; set; }
    public decimal RebatePercent { get; set; }
    public decimal RebatePerUnit { get; set; }
    public decimal BaselineValue { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? ItemId { get; set; }
    public decimal AccruedAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public string? Terms { get; set; }
    public string? FileUrl { get; set; }
    public bool IsActive { get; set; }
    public List<RebateAccrualDto> Accruals { get; set; } = [];
}

public class RebateAccrualDto
{
    public Guid Id { get; set; }
    public Guid AgreementId { get; set; }
    public string? AgreementName { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal QualifyingQuantity { get; set; }
    public decimal QualifyingValue { get; set; }
    public decimal AccruedAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public bool IsPosted { get; set; }
    public bool IsReconciled { get; set; }
    public string? SupplierCreditReference { get; set; }
    public string? Note { get; set; }
}

public class ChargebackDto
{
    public Guid Id { get; set; }
    public string ChargebackNumber { get; set; } = string.Empty;
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public Guid? ContractCustomerId { get; set; }
    public string? ContractCustomerName { get; set; }
    public string? ContractReference { get; set; }
    public Guid? ItemId { get; set; }
    public string? ItemName { get; set; }
    public Guid? SourceInvoiceId { get; set; }
    public string? SourceInvoiceNumber { get; set; }
    public DateTime SaleDate { get; set; }
    public string Uom { get; set; } = "PCS";
    public decimal Quantity { get; set; }
    public decimal AcquisitionPrice { get; set; }
    public decimal ContractPrice { get; set; }
    public decimal ChargebackAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal SettledAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public ClaimStatus Status { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? SettledAt { get; set; }
    public string? SettlementReference { get; set; }
    public string? RejectionNote { get; set; }
    public string? Note { get; set; }
}

// ── Route settlement ─────────────────────────────────────────────────────────

public class SettlementDto
{
    public Guid Id { get; set; }
    public string SettlementNumber { get; set; } = string.Empty;
    public DateTime SettlementDate { get; set; }
    public SettlementStatus Status { get; set; }

    public Guid? FieldRepId { get; set; }
    public string? FieldRepName { get; set; }
    public Guid? RouteId { get; set; }
    public string? RouteName { get; set; }
    public Guid? FieldDayId { get; set; }
    public Guid? VanUnitId { get; set; }
    public string? VanUnitName { get; set; }
    public Guid? TripId { get; set; }
    public Guid? PartnerId { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public int InvoiceCount { get; set; }
    public decimal CashSalesValue { get; set; }
    public decimal CreditSalesValue { get; set; }
    public decimal TotalSalesValue { get; set; }
    public decimal TaxValue { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal SchemeValue { get; set; }
    public decimal FreeGoodsValue { get; set; }
    public decimal ReturnValue { get; set; }

    public decimal CashCollected { get; set; }
    public decimal ChequeCollected { get; set; }
    public decimal DigitalCollected { get; set; }
    public decimal TotalCollected { get; set; }

    public decimal OpeningFloat { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal DeclaredCash { get; set; }
    public decimal CashVariance { get; set; }
    public decimal ExpenseAmount { get; set; }
    public decimal CashToDeposit { get; set; }

    public decimal OpeningStockValue { get; set; }
    public decimal LoadedStockValue { get; set; }
    public decimal SoldStockValue { get; set; }
    public decimal ReturnedStockValue { get; set; }
    public decimal ExpectedClosingStockValue { get; set; }
    public decimal CountedClosingStockValue { get; set; }
    public decimal StockVarianceValue { get; set; }
    public Guid? ClosingCountId { get; set; }

    public int VarianceCount { get; set; }
    public int UnexplainedVarianceCount { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool IsPartial { get; set; }
    public bool IsReversed { get; set; }
    public string? ReversalReason { get; set; }
    public bool IsPosted { get; set; }
    public string? Note { get; set; }

    public List<SettlementVarianceDto> Variances { get; set; } = [];
    public List<OrderSummaryDto> Invoices { get; set; } = [];
    public List<CollectionSummaryDto> Collections { get; set; } = [];
    public List<ReturnSummaryDto> Returns { get; set; } = [];
    public List<TripExpenseDto> Expenses { get; set; } = [];

    /// <summary>True while any variance is still missing its reason; the gate on closing.</summary>
    public bool CanClose { get; set; }
    public List<string> Blockers { get; set; } = [];
}

public class SettlementVarianceDto
{
    public Guid Id { get; set; }
    public VarianceKind Kind { get; set; }
    public Guid? ItemId { get; set; }
    public string? ItemName { get; set; }
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public string? Uom { get; set; }
    public decimal ExpectedQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal VarianceQuantity { get; set; }
    public decimal ExpectedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal VarianceAmount { get; set; }
    public Guid? ReasonCodeId { get; set; }
    public string? ReasonCodeName { get; set; }
    public string? ReasonNote { get; set; }
    public bool RequiresApproval { get; set; }
    public bool? IsApproved { get; set; }
    public bool IsRecoverable { get; set; }
    public decimal RecoveredAmount { get; set; }
}

public class OpenSettlementDto
{
    public Guid? FieldDayId { get; set; }
    public Guid? FieldRepId { get; set; }
    public Guid? RouteId { get; set; }
    public Guid? VanUnitId { get; set; }
    public Guid? TripId { get; set; }
    public DateTime? SettlementDate { get; set; }
    public decimal OpeningFloat { get; set; }
    public bool IsPartial { get; set; }
}

public class SubmitSettlementDto
{
    public Guid SettlementId { get; set; }
    public decimal DeclaredCash { get; set; }
    public Guid? ClosingCountId { get; set; }
    public string? Note { get; set; }
}

public class ExplainVarianceDto
{
    public Guid VarianceId { get; set; }
    public Guid ReasonCodeId { get; set; }
    public string? Note { get; set; }
    public bool IsRecoverable { get; set; }
    public decimal RecoveredAmount { get; set; }
}

public class ApproveSettlementDto
{
    public Guid SettlementId { get; set; }
    public bool IsApproved { get; set; }
    public string? Note { get; set; }
}

public class ReverseSettlementDto
{
    public string Reason { get; set; } = string.Empty;
}

public class CashDepositDto
{
    public Guid Id { get; set; }
    public string DepositNumber { get; set; } = string.Empty;
    public Guid? FieldRepId { get; set; }
    public string? FieldRepName { get; set; }
    public Guid? SettlementId { get; set; }
    public string? SettlementNumber { get; set; }
    public Guid? PartnerId { get; set; }
    public DateTime DepositedOn { get; set; }
    public string? BankName { get; set; }
    public string? BankAccount { get; set; }
    public string? SlipReference { get; set; }
    public string? SlipImageUrl { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool IsReconciled { get; set; }
    public DateTime? ReconciledAt { get; set; }
    public int AgeingDays { get; set; }
    public string? Note { get; set; }
}

public class RecordDepositDto
{
    public Guid? FieldRepId { get; set; }
    public Guid? SettlementId { get; set; }
    public Guid? PartnerId { get; set; }
    public DateTime DepositedOn { get; set; }
    public string? BankName { get; set; }
    public string? BankAccount { get; set; }
    public string? SlipReference { get; set; }
    public string? SlipImageUrl { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

/// <summary>The settlement clerk's queue: which routes are still open, and how badly.</summary>
public class SettlementBoardDto
{
    public DateTime SettlementDate { get; set; }
    public int OpenCount { get; set; }
    public int SubmittedCount { get; set; }
    public int PendingApprovalCount { get; set; }
    public int ClosedCount { get; set; }

    /// <summary>Days whose route finished but never settled — the ones to chase tonight.</summary>
    public int UnsettledDayCount { get; set; }

    public decimal TotalSales { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalCashVariance { get; set; }
    public decimal TotalStockVariance { get; set; }
    public decimal CashToDeposit { get; set; }
    public decimal UndepositedCash { get; set; }

    public List<SettlementDto> Settlements { get; set; } = [];
    public List<FieldDayDto> UnsettledDays { get; set; } = [];
}
