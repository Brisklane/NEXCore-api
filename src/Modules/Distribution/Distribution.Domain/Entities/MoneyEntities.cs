using Distribution.Domain.Enums;
using Nexcore.SharedKernel;

namespace Distribution.Domain.Entities;

/// <summary>
/// A claim from a channel partner — the single largest source of dispute in distribution.
///
/// The design point is that a claim is a *state machine with evidence*, not an email thread. It
/// carries what was claimed, what the system independently computed, the variance between the
/// two, and every transition that got it from submitted to settled. The settlement SLA measured
/// off this record is the number distributors judge a principal by.
/// </summary>
public class ChannelClaim : BaseEntity
{
    public string ClaimNumber { get; set; } = string.Empty;

    public ClaimKind Kind { get; set; } = ClaimKind.Scheme;
    public ClaimStatus Status { get; set; } = ClaimStatus.Draft;

    public Guid? PartnerId { get; set; }
    public ChannelPartner? Partner { get; set; }
    public Guid? OutletId { get; set; }
    public Guid? TerritoryId { get; set; }

    /// <summary>The period the claim covers, for schemes evaluated in arrears.</summary>
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime SubmittedOn { get; set; }

    public Guid? SchemeId { get; set; }
    public TradeScheme? Scheme { get; set; }
    public Guid? ReturnId { get; set; }
    public Guid? MrpRevisionId { get; set; }

    public string CurrencyCode { get; set; } = "USD";

    /// <summary>What the partner asked for.</summary>
    public decimal ClaimedAmount { get; set; }

    /// <summary>
    /// What the system independently worked out from its own records. The gap between this and
    /// the claimed amount is the entire review conversation, pre-computed.
    /// </summary>
    public decimal ComputedAmount { get; set; }

    public decimal VarianceAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal SettledAmount { get; set; }

    /// <summary>
    /// True when the system raised this itself from a deferred scheme, rather than the partner
    /// typing back numbers the system already had.
    /// </summary>
    public bool IsSystemGenerated { get; set; }

    public DateTime? ReviewStartedAt { get; set; }
    public Guid? ReviewerUserId { get; set; }
    public string? QueryNote { get; set; }
    public DateTime? QueriedAt { get; set; }
    public DateTime? ResubmittedAt { get; set; }

    public DateTime? DecidedAt { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public Guid? RejectionReasonCodeId { get; set; }
    public string? RejectionNote { get; set; }

    public ClaimSettlementMode? SettlementMode { get; set; }
    public DateTime? SettledAt { get; set; }
    public Guid? CreditNoteId { get; set; }
    public string? SettlementReference { get; set; }

    /// <summary>Days from submission to settlement. The SLA nobody measures and everybody feels.</summary>
    public int? SettlementDays { get; set; }

    /// <summary>Days old while still open, refreshed by the ageing job.</summary>
    public int AgeingDays { get; set; }

    public string? Note { get; set; }

    public ICollection<ChannelClaimLine> Lines { get; set; } = [];
    public ICollection<ClaimDocument> Documents { get; set; } = [];
    public ICollection<ClaimStatusEvent> StatusEvents { get; set; } = [];
}

/// <summary>One line of a claim, approvable independently of its siblings.</summary>
public class ChannelClaimLine : BaseEntity
{
    public Guid ClaimId { get; set; }
    public ChannelClaim? Claim { get; set; }

    public int DisplayOrder { get; set; }

    public Guid? ItemId { get; set; }
    public string? ItemName { get; set; }
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }

    /// <summary>The invoice this line is claimed against, where the claim is invoice-based.</summary>
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

    /// <summary>Mandatory whenever approved is less than claimed. Partial approval without a reason is a dispute.</summary>
    public Guid? RejectionReasonCodeId { get; set; }
    public string? RejectionNote { get; set; }

    public string? Note { get; set; }
}

/// <summary>Supporting evidence attached to a claim — invoice scan, photo, destruction certificate.</summary>
public class ClaimDocument : BaseEntity
{
    public Guid ClaimId { get; set; }
    public ChannelClaim? Claim { get; set; }

    public string DocumentType { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public DateTime UploadedAt { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public string? Note { get; set; }
}

/// <summary>Every transition a claim made, so the settlement SLA is auditable rather than asserted.</summary>
public class ClaimStatusEvent : BaseEntity
{
    public Guid ClaimId { get; set; }
    public ChannelClaim? Claim { get; set; }

    public ClaimStatus FromStatus { get; set; }
    public ClaimStatus ToStatus { get; set; }
    public DateTime OccurredAt { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? ActorName { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// A rebate we earn from a supplier — the mirror image of a claim.
///
/// Volume, growth and market-development rebates are contractual and accrue monthly, which means
/// they belong on the books as they are earned rather than as a windfall when the credit note
/// eventually arrives.
/// </summary>
public class SupplierRebateAgreement : BaseEntity
{
    public string AgreementNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public Guid SupplierId { get; set; }
    public string? SupplierName { get; set; }

    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>"Volume", "Growth", "MarketDevelopment" — kept as text because contracts invent new ones.</summary>
    public string RebateBasis { get; set; } = "Volume";

    public decimal ThresholdQuantity { get; set; }
    public decimal ThresholdValue { get; set; }
    public decimal RebatePercent { get; set; }
    public decimal RebatePerUnit { get; set; }

    /// <summary>Growth rebates compare against this baseline.</summary>
    public decimal BaselineValue { get; set; }

    public Guid? BrandId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? ItemId { get; set; }

    public decimal AccruedAmount { get; set; }
    public decimal ReceivedAmount { get; set; }

    public string? Terms { get; set; }
    public string? FileUrl { get; set; }

    public ICollection<RebateAccrual> Accruals { get; set; } = [];
}

/// <summary>One period's rebate accrual under an agreement, and whether it has been collected.</summary>
public class RebateAccrual : BaseEntity
{
    public Guid AgreementId { get; set; }
    public SupplierRebateAgreement? Agreement { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public decimal QualifyingQuantity { get; set; }
    public decimal QualifyingValue { get; set; }
    public decimal AccruedAmount { get; set; }
    public decimal ReceivedAmount { get; set; }

    public bool IsPosted { get; set; }
    public DateTime? PostedAt { get; set; }
    public bool IsReconciled { get; set; }
    public DateTime? ReconciledAt { get; set; }
    public string? SupplierCreditReference { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// A chargeback: the gap between what we paid for goods and the contracted price at which we were
/// required to sell them to a nominated end customer, claimed back from the principal.
/// </summary>
public class Chargeback : BaseEntity
{
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

    /// <summary>(Acquisition − contract) × quantity. What we are owed.</summary>
    public decimal ChargebackAmount { get; set; }

    public decimal ApprovedAmount { get; set; }
    public decimal SettledAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public ClaimStatus Status { get; set; } = ClaimStatus.Draft;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? SettledAt { get; set; }
    public string? SettlementReference { get; set; }
    public string? RejectionNote { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// Live credit state for an outlet or a partner.
///
/// Kept as its own record rather than computed on demand because the field terminal has to
/// evaluate a credit block at the counter with no network. This is the snapshot it syncs down.
/// </summary>
public class CreditProfile : BaseEntity
{
    public Guid? OutletId { get; set; }
    public RetailOutlet? Outlet { get; set; }
    public Guid? PartnerId { get; set; }
    public ChannelPartner? Partner { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal CreditLimit { get; set; }

    /// <summary>An uplift someone signed for, which expires on its own rather than living forever.</summary>
    public decimal TemporaryLimit { get; set; }
    public DateTime? TemporaryLimitExpiresOn { get; set; }

    public int CreditDays { get; set; }
    public CreditEnforcement Enforcement { get; set; } = CreditEnforcement.Warn;

    public decimal OutstandingAmount { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal UnbilledOrderValue { get; set; }

    /// <summary>Limit − outstanding − unbilled. What one more order may be worth.</summary>
    public decimal AvailableCredit { get; set; }

    // ── Ageing buckets. Boundaries are configurable in settings. ─────────────
    public decimal Bucket0To30 { get; set; }
    public decimal Bucket31To60 { get; set; }
    public decimal Bucket61To90 { get; set; }
    public decimal Bucket90Plus { get; set; }

    public int OldestInvoiceDays { get; set; }
    public DateTime? LastPaymentAt { get; set; }
    public decimal LastPaymentAmount { get; set; }

    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public DateTime? BlockedAt { get; set; }

    public int BouncedChequeCount { get; set; }
    public decimal SecurityDeposit { get; set; }
    public decimal AdvanceHeld { get; set; }

    /// <summary>Provisioned as doubtful. Written off only with approval.</summary>
    public decimal ProvisionedAmount { get; set; }

    public DateTime? RecalculatedAt { get; set; }
}

/// <summary>
/// A signed exception to a credit rule.
///
/// Overrides expire. A permanent override is just a higher limit, and calling it an override is
/// how limits quietly stop meaning anything.
/// </summary>
public class CreditOverride : BaseEntity
{
    public Guid? OutletId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? OrderId { get; set; }

    public decimal RequestedAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public Guid? ReasonCodeId { get; set; }
    public string? Justification { get; set; }

    public Guid? RequestedByUserId { get; set; }
    public DateTime RequestedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? ApproverName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public bool? IsApproved { get; set; }
    public string? DecisionNote { get; set; }

    /// <summary>The override stops working on this date whether anyone revisits it or not.</summary>
    public DateTime? ExpiresOn { get; set; }
    public bool IsConsumed { get; set; }
}

/// <summary>
/// Money taken in the market.
///
/// Collections are *recorded* here, never processed: a cheque number, a bank, a UPI reference, a
/// card's last four digits. No card number or track data is accepted or stored anywhere in this
/// module.
/// </summary>
public class CollectionReceipt : BaseEntity
{
    public string ReceiptNumber { get; set; } = string.Empty;

    public Guid? OutletId { get; set; }
    public RetailOutlet? Outlet { get; set; }
    public Guid? PartnerId { get; set; }

    public Guid? VisitId { get; set; }
    public Guid? FieldDayId { get; set; }
    public Guid? FieldRepId { get; set; }
    public Guid? TripId { get; set; }
    public Guid? SettlementId { get; set; }

    public DateTime CollectedAt { get; set; }
    public PaymentTender Tender { get; set; } = PaymentTender.Cash;

    public string CurrencyCode { get; set; } = "USD";
    public decimal Amount { get; set; }
    public decimal ExchangeRate { get; set; } = 1;

    /// <summary>Discount given for early settlement, under a cash-discount scheme.</summary>
    public decimal CashDiscountAmount { get; set; }
    public Guid? CashDiscountSchemeId { get; set; }

    public Guid? ChequeId { get; set; }
    public string? Reference { get; set; }
    public string? BankName { get; set; }
    public string? CardLast4 { get; set; }
    public string? CardScheme { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>Allocated across invoices; the unallocated remainder sits on account.</summary>
    public decimal AllocatedAmount { get; set; }
    public decimal UnallocatedAmount { get; set; }

    /// <summary>False until the cash reaches a bank — the difference between collected and banked.</summary>
    public bool IsDeposited { get; set; }
    public Guid? DepositId { get; set; }

    public bool IsReversed { get; set; }
    public string? ReversalReason { get; set; }

    public string? ReceiptSentTo { get; set; }
    public string? Note { get; set; }

    /// <summary>Client key: a retried sync must not double-credit an outlet.</summary>
    public string? IdempotencyKey { get; set; }
}

/// <summary>
/// A cheque, tracked through its life.
///
/// A bounce is not just a failed payment — it reverses an allocation, adds a charge, and usually
/// blocks the outlet. Modelling the cheque as an object rather than a receipt field is what makes
/// that automatic.
/// </summary>
public class ChequeRecord : BaseEntity
{
    public string ChequeNumber { get; set; } = string.Empty;

    public Guid? OutletId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? CollectionReceiptId { get; set; }

    public string? BankName { get; set; }
    public string? BranchName { get; set; }
    public string? AccountName { get; set; }

    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public DateTime ChequeDate { get; set; }
    public DateTime ReceivedOn { get; set; }

    /// <summary>Post-dated cheques sit on the diary until their date arrives.</summary>
    public bool IsPostDated { get; set; }

    public ChequeStatus Status { get; set; } = ChequeStatus.Received;
    public DateTime? DepositedOn { get; set; }
    public string? DepositBankAccount { get; set; }
    public DateTime? ClearedOn { get; set; }

    public DateTime? BouncedOn { get; set; }
    public string? BounceReason { get; set; }
    public decimal BounceCharges { get; set; }

    /// <summary>Set when the bounce automatically blocked the outlet, so the block can be traced.</summary>
    public bool TriggeredCreditBlock { get; set; }

    public string? Note { get; set; }
}

/// <summary>
/// A day's route reconciled: invoices, collections, returns, van stock, expenses and cash.
///
/// This is the moment a distribution day is proved correct, and the rule that makes it work is
/// simple and absolute — a route cannot close with an unexplained variance. Everything else here
/// is bookkeeping around that one constraint.
/// </summary>
public class RouteSettlement : BaseEntity
{
    public string SettlementNumber { get; set; } = string.Empty;

    public DateTime SettlementDate { get; set; }
    public SettlementStatus Status { get; set; } = SettlementStatus.Open;

    public Guid? FieldRepId { get; set; }
    public FieldRep? FieldRep { get; set; }
    public Guid? RouteId { get; set; }
    public Guid? FieldDayId { get; set; }
    public Guid? VanUnitId { get; set; }
    public Guid? TripId { get; set; }
    public Guid? PartnerId { get; set; }

    public string CurrencyCode { get; set; } = "USD";

    // ── Sales side ───────────────────────────────────────────────────────────
    public int InvoiceCount { get; set; }
    public decimal CashSalesValue { get; set; }
    public decimal CreditSalesValue { get; set; }
    public decimal TotalSalesValue { get; set; }
    public decimal TaxValue { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal SchemeValue { get; set; }
    public decimal FreeGoodsValue { get; set; }
    public decimal ReturnValue { get; set; }

    // ── Collections ──────────────────────────────────────────────────────────
    public decimal CashCollected { get; set; }
    public decimal ChequeCollected { get; set; }
    public decimal DigitalCollected { get; set; }
    public decimal TotalCollected { get; set; }

    // ── Cash reconciliation ──────────────────────────────────────────────────
    public decimal OpeningFloat { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal DeclaredCash { get; set; }
    public decimal CashVariance { get; set; }
    public decimal ExpenseAmount { get; set; }
    public decimal CashToDeposit { get; set; }

    // ── Van stock reconciliation ─────────────────────────────────────────────
    public decimal OpeningStockValue { get; set; }
    public decimal LoadedStockValue { get; set; }
    public decimal SoldStockValue { get; set; }
    public decimal ReturnedStockValue { get; set; }
    public decimal ExpectedClosingStockValue { get; set; }
    public decimal CountedClosingStockValue { get; set; }
    public decimal StockVarianceValue { get; set; }
    public Guid? ClosingCountId { get; set; }

    /// <summary>Every one needs a reason. This is the gate on closing the day.</summary>
    public int VarianceCount { get; set; }
    public int UnexplainedVarianceCount { get; set; }

    public DateTime? SubmittedAt { get; set; }
    public Guid? SubmittedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ClosedAt { get; set; }

    /// <summary>An overnight route settles partially and carries the balance forward.</summary>
    public bool IsPartial { get; set; }
    public Guid? PreviousSettlementId { get; set; }

    public bool IsReversed { get; set; }
    public DateTime? ReversedAt { get; set; }
    public Guid? ReversedByUserId { get; set; }
    public string? ReversalReason { get; set; }

    /// <summary>True once the accounting entries have been published.</summary>
    public bool IsPosted { get; set; }
    public DateTime? PostedAt { get; set; }

    public string? Note { get; set; }

    public ICollection<SettlementVariance> Variances { get; set; } = [];
}

/// <summary>
/// One gap found at settlement, with the reason that lets the day close.
///
/// Above a configured threshold it also needs an approver — a hundred units short is a miscount,
/// a thousand is a conversation.
/// </summary>
public class SettlementVariance : BaseEntity
{
    public Guid SettlementId { get; set; }
    public RouteSettlement? Settlement { get; set; }

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
    public string? ReasonNote { get; set; }

    /// <summary>Above the tolerance threshold, so it cannot be waved through by the rep alone.</summary>
    public bool RequiresApproval { get; set; }
    public bool? IsApproved { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    /// <summary>Recovered from the rep's dues rather than written off.</summary>
    public bool IsRecoverable { get; set; }
    public decimal RecoveredAmount { get; set; }
}

/// <summary>Cash physically banked, and whether the bank agrees it arrived.</summary>
public class CashDeposit : BaseEntity
{
    public string DepositNumber { get; set; } = string.Empty;

    public Guid? FieldRepId { get; set; }
    public Guid? SettlementId { get; set; }
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
    public Guid? ReconciledByUserId { get; set; }

    /// <summary>Days between collection and banking. An ageing deposit is cash at risk.</summary>
    public int AgeingDays { get; set; }

    public string? Note { get; set; }
}
