using Nexcore.SharedKernel;
using RealEstate.Domain.Enums;

namespace RealEstate.Domain.Entities;

/// <summary>
/// A development agreement with a landowner. The "somebody gives the land, we build on it" case in
/// its three real shapes, plus the management-fee variant.
/// </summary>
public class JointVenture : BaseEntity
{
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public Guid ProjectId { get; set; }
    public Guid? LandParcelId { get; set; }

    public JvShareBasis Basis { get; set; } = JvShareBasis.RevenueShare;

    public DateOnly AgreementDate { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public string? DocumentUrl { get; set; }
    public Guid? SignatureSessionId { get; set; }

    // ── The split ────────────────────────────────────────────────────────────

    public decimal LandownerSharePercent { get; set; }
    public decimal DeveloperSharePercent { get; set; }

    /// <summary>Set for an area-share deal: how much built-up or saleable area the landowner takes.</summary>
    public decimal? LandownerAreaSqFt { get; set; }

    /// <summary>Set for a development-management deal, where we manage rather than own.</summary>
    public decimal? ManagementFeePercent { get; set; }

    /// <summary>Non-refundable money paid to the landowner at signing, set against their share.</summary>
    public decimal RefundableSecurity { get; set; }
    public decimal NonRefundableDeposit { get; set; }

    /// <summary>Share computed on collections rather than on booked value. The safer basis.</summary>
    public bool ShareOnCollection { get; set; } = true;

    public decimal TotalLandownerEntitlement { get; set; }
    public decimal TotalLandownerPaid { get; set; }
    public decimal LandownerBalance { get; set; }

    /// <summary>A separate company for this project, so its books stay separable.</summary>
    public bool HasSpecialPurposeVehicle { get; set; }
    public string? SpvName { get; set; }
    public Guid? SpvCompanyId { get; set; }

    public string? Terms { get; set; }

    public ICollection<JvPartner> Partners { get; set; } = [];
    public ICollection<JvShareTerm> ShareTerms { get; set; } = [];
}

public class JvPartner : BaseEntity
{
    public Guid JointVentureId { get; set; }
    public JointVenture? JointVenture { get; set; }

    public Guid PartyId { get; set; }

    /// <summary>"Landowner", "Developer", "Financier", "Manager".</summary>
    public string Role { get; set; } = "Landowner";

    public decimal SharePercent { get; set; }
    public decimal ContributedValue { get; set; }

    /// <summary>"Land", "Cash", "Services", "Approvals".</summary>
    public string? ContributionType { get; set; }

    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public decimal WithholdingPercent { get; set; }
}

/// <summary>A tranche of the landowner's entitlement, with the trigger that releases it.</summary>
public class JvShareTerm : BaseEntity
{
    public Guid JointVentureId { get; set; }
    public JointVenture? JointVenture { get; set; }

    public string Label { get; set; } = string.Empty;

    /// <summary>"OnSigning", "OnApproval", "OnLaunch", "OnCollection", "OnMilestone", "OnCompletion".</summary>
    public string Trigger { get; set; } = "OnCollection";

    public Guid? ProjectMilestoneId { get; set; }
    public decimal? TriggerCollectionPercent { get; set; }

    public decimal Percent { get; set; }
    public decimal FixedAmount { get; set; }
    public decimal EntitlementAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateOnly? DueDate { get; set; }
    public bool IsSettled { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// Specific units allocated to the landowner under an area-share deal. Excluded from our saleable
/// stock and blocked on the inventory board, because selling one by accident is unrecoverable.
/// </summary>
public class LandownerAllocation : BaseEntity
{
    public Guid JointVentureId { get; set; }
    public Guid? JvPartnerId { get; set; }
    public Guid UnitId { get; set; }
    public Guid? PropertyId { get; set; }

    public DateOnly AllocatedOn { get; set; }
    public decimal AreaSqFt { get; set; }
    public decimal NotionalValue { get; set; }

    /// <summary>"Allocated", "Confirmed", "HandedOver", "SoldBack", "Released".</summary>
    public string Status { get; set; } = "Allocated";

    /// <summary>The landowner sold their unit back to us rather than taking it.</summary>
    public bool IsBoughtBack { get; set; }
    public decimal? BuyBackPrice { get; set; }
    public Guid? BuyBackBookingId { get; set; }

    public DateOnly? HandedOverOn { get; set; }
    public Guid? HandoverId { get; set; }
    public string? Note { get; set; }
}

public class LandownerLedgerEntry : BaseEntity
{
    public Guid JointVentureId { get; set; }
    public Guid? JvPartnerId { get; set; }

    public DateOnly EntryDate { get; set; }

    /// <summary>"Entitlement", "Payment", "UnitAllocation", "Adjustment", "Withholding", "Interest".</summary>
    public string EntryType { get; set; } = "Entitlement";

    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal RunningBalance { get; set; }

    public Guid? ReceiptId { get; set; }
    public Guid? JvShareTermId { get; set; }
    public Guid? LandownerAllocationId { get; set; }
    public Guid? JournalEntryId { get; set; }
}

/// <summary>Somebody putting money into a project for a return.</summary>
public class Investor : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid PartyId { get; set; }
    public Guid? ProjectId { get; set; }

    /// <summary>"Equity", "Debt", "Mezzanine", "PreferredEquity".</summary>
    public string InvestmentType { get; set; } = "Equity";

    public decimal CommittedAmount { get; set; }
    public decimal ContributedAmount { get; set; }
    public decimal DistributedAmount { get; set; }
    public decimal SharePercent { get; set; }

    /// <summary>The return they get before we take anything. The first tier of the waterfall.</summary>
    public decimal PreferredReturnPercent { get; set; }

    public DateOnly? InvestedOn { get; set; }
    public DateOnly? ExitDate { get; set; }
    public decimal? RealisedIrr { get; set; }

    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public decimal WithholdingPercent { get; set; }
    public string? AgreementUrl { get; set; }
    public bool PortalAccessEnabled { get; set; }
}

public class CapitalCommitment : BaseEntity
{
    public Guid InvestorId { get; set; }
    public Guid? ProjectId { get; set; }

    public decimal Amount { get; set; }
    public DateOnly CommittedOn { get; set; }
    public decimal DrawnAmount { get; set; }
    public decimal UndrawnAmount { get; set; }
    public DateOnly? CommitmentExpiresOn { get; set; }
    public string? DocumentUrl { get; set; }
}

/// <summary>A call on the investors' undrawn commitments.</summary>
public class CapitalCall : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid? ProjectId { get; set; }
    public Guid? InvestorId { get; set; }

    public DateOnly IssuedOn { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
    public string? Purpose { get; set; }

    public decimal ReceivedAmount { get; set; }
    public DateOnly? ReceivedOn { get; set; }

    /// <summary>Missing a call carries a penalty or a dilution in most agreements.</summary>
    public bool IsDefaulted { get; set; }
    public decimal? DefaultPenalty { get; set; }

    public string? NoticeUrl { get; set; }
    public bool IsSettled { get; set; }
}

public class Contribution : BaseEntity
{
    public Guid InvestorId { get; set; }
    public Guid? CapitalCallId { get; set; }
    public Guid? ProjectId { get; set; }

    public DateOnly ReceivedOn { get; set; }
    public decimal Amount { get; set; }
    public PaymentInstrument Instrument { get; set; } = PaymentInstrument.BankTransfer;
    public string? Reference { get; set; }
    public Guid? ReceiptId { get; set; }
    public Guid? JournalEntryId { get; set; }
}

public class Distribution : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid InvestorId { get; set; }
    public Guid? ProjectId { get; set; }

    public DateOnly DistributedOn { get; set; }

    /// <summary>"ReturnOfCapital", "PreferredReturn", "ProfitShare", "Interest".</summary>
    public string DistributionType { get; set; } = "ProfitShare";

    public decimal GrossAmount { get; set; }
    public decimal WithholdingAmount { get; set; }
    public decimal NetAmount { get; set; }

    /// <summary>Which tier of the waterfall this came from, so an investor can follow the arithmetic.</summary>
    public int? WaterfallTier { get; set; }

    public string? PaymentReference { get; set; }
    public Guid? JournalEntryId { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public bool IsPaid { get; set; }
}

/// <summary>
/// A project bank account. The escrow one is the point: in RERA-style regimes it is a legal
/// container, not a naming convention, and money cannot leave it without certified progress.
/// </summary>
public class ProjectBankAccount : BaseEntity
{
    public Guid ProjectId { get; set; }
    public ProjectAccountKind Kind { get; set; }

    public string AccountTitle { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string? BankName { get; set; }
    public string? BranchName { get; set; }
    public string? IbanOrSwift { get; set; }
    public string CurrencyCode { get; set; } = AppConstants.DefaultCurrency;

    public Guid? LedgerAccountId { get; set; }

    public decimal Balance { get; set; }
    public decimal TotalCredited { get; set; }
    public decimal TotalWithdrawn { get; set; }

    // ── Escrow controls ──────────────────────────────────────────────────────

    /// <summary>Share of every collection that must land here.</summary>
    public decimal DepositPercent { get; set; }

    /// <summary>Percentage of escrow receipts released so far, against certified construction progress.</summary>
    public decimal WithdrawalEntitlement { get; set; }

    public decimal WithdrawnAgainstEntitlement { get; set; }

    /// <summary>Entitlement less withdrawn. May never go negative, and the app refuses when it would.</summary>
    public decimal AvailableForWithdrawal { get; set; }

    public string? RegulatorReference { get; set; }
    public bool RequiresEngineerCertificate { get; set; } = true;
    public bool RequiresArchitectCertificate { get; set; } = true;
    public bool RequiresAuditorCertificate { get; set; } = true;

    public DateOnly? LastAuditedOn { get; set; }
    public DateOnly? NextAuditDue { get; set; }
}

/// <summary>Every movement into and out of a project account, with the receipt that caused it.</summary>
public class EscrowLedgerEntry : BaseEntity
{
    public Guid ProjectBankAccountId { get; set; }
    public Guid ProjectId { get; set; }

    public DateOnly EntryDate { get; set; }
    public EscrowMovementKind Kind { get; set; }

    public decimal CreditAmount { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal RunningBalance { get; set; }

    public Guid? ReceiptId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? EscrowWithdrawalId { get; set; }
    public Guid? JournalEntryId { get; set; }

    public bool IsReconciled { get; set; }
    public string? BankReference { get; set; }
}

/// <summary>
/// Taking money out of escrow. Only against certified construction progress, and only within the
/// entitlement — this is the control the regulation exists to create, and it is enforced rather
/// than reported.
/// </summary>
public class EscrowWithdrawal : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid ProjectBankAccountId { get; set; }
    public Guid ProjectId { get; set; }

    public DateOnly RequestedOn { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public string? Purpose { get; set; }

    public decimal ProgressPercentAtRequest { get; set; }
    public decimal EntitlementAtRequest { get; set; }
    public decimal AlreadyWithdrawn { get; set; }
    public decimal AvailableAtRequest { get; set; }

    /// <summary>"Requested", "CertificatesPending", "Approved", "Rejected", "Withdrawn".</summary>
    public string Status { get; set; } = "Requested";

    /// <summary>Set when the request exceeded the entitlement. Refused, and the attempt is kept.</summary>
    public bool ExceededEntitlement { get; set; }

    public Guid? RequestedByUserId { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public DateOnly? WithdrawnOn { get; set; }
    public string? BankReference { get; set; }
    public Guid? JournalEntryId { get; set; }

    public ICollection<WithdrawalCertificate> Certificates { get; set; } = [];
}

/// <summary>
/// One of the professional certificates an escrow withdrawal needs. The regulator asks for all
/// three — engineer, architect and chartered accountant — and the app will not release without them.
/// </summary>
public class WithdrawalCertificate : BaseEntity
{
    public Guid EscrowWithdrawalId { get; set; }
    public EscrowWithdrawal? Withdrawal { get; set; }

    /// <summary>"Engineer", "Architect", "CharteredAccountant".</summary>
    public string CertifierType { get; set; } = "Engineer";

    public string CertifierName { get; set; } = string.Empty;
    public string? RegistrationNumber { get; set; }
    public DateOnly CertifiedOn { get; set; }
    public decimal CertifiedProgressPercent { get; set; }
    public decimal? CertifiedCostIncurred { get; set; }
    public string? DocumentUrl { get; set; }
    public bool IsVerified { get; set; }
}

/// <summary>Construction finance, drawn against certified progress just as escrow is released against it.</summary>
public class ProjectLoan : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid ProjectId { get; set; }
    public Guid? LenderPartyId { get; set; }
    public string? LenderName { get; set; }

    public LoanStatus Status { get; set; } = LoanStatus.Applied;

    public decimal SanctionedAmount { get; set; }
    public decimal DrawnAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal RepaidAmount { get; set; }
    public string CurrencyCode { get; set; } = AppConstants.DefaultCurrency;

    public decimal InterestRate { get; set; }

    /// <summary>"Fixed", "Floating", "Kibor+", "Sofr+", "Base+".</summary>
    public string? RateBasis { get; set; }

    public decimal AccruedInterest { get; set; }
    public decimal PaidInterest { get; set; }

    public DateOnly? SanctionedOn { get; set; }
    public DateOnly? FirstDrawdownOn { get; set; }
    public DateOnly? MoratoriumEndsOn { get; set; }
    public DateOnly? MaturityDate { get; set; }
    public int TenureMonths { get; set; }

    /// <summary>Units pledged as security, which blocks them on the inventory board until released.</summary>
    public string? SecurityDescription { get; set; }
    public bool UnitsMortgaged { get; set; }
    public int MortgagedUnitCount { get; set; }

    public string? Covenants { get; set; }
    public DateOnly? NextCovenantTestOn { get; set; }
    public string? DocumentUrl { get; set; }
}

public class LoanDrawdown : BaseEntity
{
    public Guid ProjectLoanId { get; set; }

    public string Reference { get; set; } = string.Empty;
    public DateOnly RequestedOn { get; set; }
    public DateOnly? ReceivedOn { get; set; }
    public decimal Amount { get; set; }

    public decimal ProgressPercentAtDrawdown { get; set; }
    public Guid? MilestoneCertificateId { get; set; }
    public string? Purpose { get; set; }
    public decimal ProcessingFee { get; set; }
    public Guid? JournalEntryId { get; set; }
    public bool IsReceived { get; set; }
}

public class LoanRepayment : BaseEntity
{
    public Guid ProjectLoanId { get; set; }

    public int InstalmentNumber { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateOnly? PaidOn { get; set; }
    public decimal OutstandingAfter { get; set; }
    public bool IsPaid { get; set; }
    public bool IsOverdue { get; set; }
    public Guid? JournalEntryId { get; set; }
}

/// <summary>A performance, advance, retention or bid bond, with the expiry that must not be missed.</summary>
public class BankGuarantee : BaseEntity
{
    public string GuaranteeNumber { get; set; } = string.Empty;

    public GuaranteeKind Kind { get; set; }

    /// <summary>"Issued" — we gave it; "Received" — somebody gave it to us.</summary>
    public string Direction { get; set; } = "Received";

    public Guid? ProjectId { get; set; }
    public Guid? SubcontractId { get; set; }
    public Guid? ClientBuildContractId { get; set; }
    public Guid? PartyId { get; set; }

    public string? IssuingBank { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = AppConstants.DefaultCurrency;

    public DateOnly IssuedOn { get; set; }
    public DateOnly ExpiresOn { get; set; }

    /// <summary>Often later than expiry — the window in which it may still be called.</summary>
    public DateOnly? ClaimPeriodEndsOn { get; set; }

    public decimal? Commission { get; set; }
    public decimal? MarginHeld { get; set; }

    /// <summary>"Active", "Expired", "Released", "Invoked", "Extended".</summary>
    public string Status { get; set; } = "Active";

    public bool IsAutoRenewing { get; set; }
    public int AlertDaysBefore { get; set; } = 45;
    public bool ExpiryAlertSent { get; set; }
    public DateOnly? ReleasedOn { get; set; }
    public string? DocumentUrl { get; set; }
}

/// <summary>
/// The buyer's home finance. Tracked, never originated — but the disbursement schedule ties into
/// construction stages, so it has to be on the record to forecast collection honestly.
/// </summary>
public class CustomerMortgage : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid BookingId { get; set; }
    public Guid PartyId { get; set; }
    public Guid? LenderPartyId { get; set; }
    public string? LenderName { get; set; }

    /// <summary>"Applied", "UnderProcess", "Sanctioned", "PartiallyDisbursed", "FullyDisbursed",
    /// "Rejected", "Withdrawn".</summary>
    public string Status { get; set; } = "Applied";

    public decimal AppliedAmount { get; set; }
    public decimal SanctionedAmount { get; set; }
    public decimal DisbursedAmount { get; set; }
    public decimal InterestRate { get; set; }
    public int TenureYears { get; set; }

    public DateOnly? AppliedOn { get; set; }
    public DateOnly? SanctionedOn { get; set; }
    public DateOnly? SanctionValidUntil { get; set; }

    /// <summary>Developer, buyer and lender all sign. Without it the lender will not disburse.</summary>
    public bool TripartiteAgreementSigned { get; set; }
    public DateOnly? TripartiteSignedOn { get; set; }

    /// <summary>Our permission for the lender to take a charge over the unit.</summary>
    public Guid? NocToMortgageId { get; set; }

    public string? RejectionReason { get; set; }
    public string? DocumentUrl { get; set; }
}

public class MortgageDisbursement : BaseEntity
{
    public Guid CustomerMortgageId { get; set; }
    public Guid? BookingId { get; set; }

    public int SequenceNumber { get; set; }
    public DateOnly? ExpectedOn { get; set; }
    public DateOnly? ReceivedOn { get; set; }
    public decimal Amount { get; set; }

    public Guid? ProjectMilestoneId { get; set; }
    public Guid? DemandId { get; set; }
    public Guid? ReceiptId { get; set; }
    public string? BankReference { get; set; }
    public bool IsReceived { get; set; }
    public string? DelayReason { get; set; }
}

/// <summary>The IFRIC 15 determination, recorded per contract with its reasoning for the auditor.</summary>
public class RecognitionPolicy : BaseEntity
{
    public Guid? ProjectId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? ClientBuildContractId { get; set; }

    public RecognitionBasis Basis { get; set; } = RecognitionBasis.PointInTime;

    /// <summary>"ByCost", "ByPhysicalProgress", "ByMilestone". Only meaningful for over-time.</summary>
    public string? OverTimeMethod { get; set; }

    /// <summary>The event that recognises everything, for point-in-time. Usually handover.</summary>
    public string? PointInTimeTrigger { get; set; }

    public string Rationale { get; set; } = string.Empty;
    public DateOnly DeterminedOn { get; set; }
    public Guid? DeterminedByUserId { get; set; }
    public Guid? ApprovalRequestId { get; set; }
}

/// <summary>One period's revenue recognition run.</summary>
public class RevenueRecognitionRun : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid? ProjectId { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly RunDate { get; set; }

    public bool IsDryRun { get; set; }
    public int ContractCount { get; set; }
    public decimal RevenueRecognised { get; set; }
    public decimal CostRecognised { get; set; }
    public decimal GrossMargin { get; set; }
    public decimal ContractAssetTotal { get; set; }
    public decimal ContractLiabilityTotal { get; set; }

    public Guid? RunByUserId { get; set; }
    public Guid? JournalEntryId { get; set; }
    public bool IsPosted { get; set; }
    public string? ErrorSummary { get; set; }
}

public class RecognitionEntry : BaseEntity
{
    public Guid RevenueRecognitionRunId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? ClientBuildContractId { get; set; }
    public Guid? ProjectId { get; set; }

    public RecognitionBasis Basis { get; set; }
    public DateOnly PeriodTo { get; set; }

    public decimal ContractValue { get; set; }
    public decimal CostIncurredToDate { get; set; }
    public decimal TotalEstimatedCost { get; set; }
    public decimal PercentComplete { get; set; }

    public decimal RevenueRecognisedToDate { get; set; }
    public decimal RevenueRecognisedPreviously { get; set; }
    public decimal RevenueThisPeriod { get; set; }
    public decimal CostRecognisedThisPeriod { get; set; }

    public decimal AmountsInvoiced { get; set; }
    public decimal AmountsCollected { get; set; }

    /// <summary>Recognised but not yet billed. The receivable that has no invoice behind it.</summary>
    public decimal ContractAsset { get; set; }

    /// <summary>Billed or collected but not yet earned. A liability, however good the cash feels.</summary>
    public decimal ContractLiability { get; set; }

    /// <summary>Estimated cost exceeds contract value. Provided in full immediately, per the standard.</summary>
    public bool IsLossMaking { get; set; }
    public decimal? ProvisionForLoss { get; set; }
}

/// <summary>Development cost capitalised against a project before any of it is recognised.</summary>
public class WipEntry : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid? WbsNodeId { get; set; }
    public Guid? UnitId { get; set; }

    public DateOnly EntryDate { get; set; }

    /// <summary>"Land", "Approvals", "Infrastructure", "Construction", "FinanceCost",
    /// "Marketing", "Overhead", "Transfer", "Recognition".</summary>
    public string CostCategory { get; set; } = string.Empty;

    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal RunningBalance { get; set; }

    public Guid? SourceDocumentId { get; set; }
    public string? SourceDocumentType { get; set; }
    public Guid? JournalEntryId { get; set; }

    /// <summary>Released to cost of sales as units are recognised.</summary>
    public bool IsReleasedToCogs { get; set; }
}

/// <summary>How a project's shared costs are pushed down onto individual units.</summary>
public class CostAllocationRule : BaseEntity
{
    public Guid ProjectId { get; set; }

    public string CostCategory { get; set; } = string.Empty;
    public CostAllocationBasis Basis { get; set; } = CostAllocationBasis.ByArea;

    /// <summary>Restricts the rule to one block or tower, where costs genuinely differ.</summary>
    public Guid? ProjectNodeId { get; set; }

    public PropertySubType? AppliesToSubType { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public string? Note { get; set; }
}

/// <summary>The allocated cost sitting behind one unit, which is what makes its margin a real number.</summary>
public class UnitCostAllocation : BaseEntity
{
    public Guid UnitId { get; set; }
    public Guid ProjectId { get; set; }

    public DateOnly AsOfDate { get; set; }

    public decimal LandCost { get; set; }
    public decimal ConstructionCost { get; set; }
    public decimal InfrastructureCost { get; set; }
    public decimal ApprovalCost { get; set; }
    public decimal FinanceCost { get; set; }
    public decimal MarketingCost { get; set; }
    public decimal CommissionCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal TotalAllocatedCost { get; set; }

    public decimal AreaSqFt { get; set; }
    public decimal CostPerSqFt { get; set; }
    public CostAllocationBasis Basis { get; set; } = CostAllocationBasis.ByArea;
}

/// <summary>
/// Realisation less allocated cost, per unit. The report that tells a developer which product to
/// build next, and the one almost nobody in this market can produce.
/// </summary>
public class UnitProfitability : BaseEntity
{
    public Guid UnitId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? BookingId { get; set; }

    public DateOnly AsOfDate { get; set; }

    public decimal ListPrice { get; set; }
    public decimal DiscountGiven { get; set; }
    public decimal NetRealisation { get; set; }
    public decimal SurchargeEarned { get; set; }
    public decimal TotalRevenue { get; set; }

    public decimal AllocatedCost { get; set; }
    public decimal DirectCost { get; set; }
    public decimal TotalCost { get; set; }

    public decimal GrossMargin { get; set; }
    public decimal MarginPercent { get; set; }

    public decimal AreaSqFt { get; set; }
    public decimal RealisationPerSqFt { get; set; }
    public decimal CostPerSqFt { get; set; }
    public decimal MarginPerSqFt { get; set; }

    public PropertySubType? SubType { get; set; }
    public Guid? ProjectNodeId { get; set; }
}

public class ProjectPnlSnapshot : BaseEntity
{
    public Guid ProjectId { get; set; }
    public DateOnly AsOfDate { get; set; }

    public decimal TotalSalesValue { get; set; }
    public decimal RevenueRecognised { get; set; }
    public decimal RevenueDeferred { get; set; }
    public decimal CollectionsToDate { get; set; }

    public decimal CostIncurred { get; set; }
    public decimal CostRecognised { get; set; }
    public decimal WipBalance { get; set; }
    public decimal ForecastTotalCost { get; set; }

    public decimal GrossMargin { get; set; }
    public decimal MarginPercent { get; set; }
    public decimal ForecastMargin { get; set; }

    public decimal EscrowBalance { get; set; }
    public decimal FreeCashBalance { get; set; }
    public decimal LoanOutstanding { get; set; }
    public decimal LandownerLiability { get; set; }

    public int UnitsTotal { get; set; }
    public int UnitsSold { get; set; }
    public int UnitsAvailable { get; set; }
    public decimal AbsorptionPercent { get; set; }
    public decimal PhysicalProgressPercent { get; set; }
}

/// <summary>Tax rates and treatments per project, because they differ by category and by market.</summary>
public class TaxProfile : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public Guid? OfficeId { get; set; }

    public decimal SalesTaxPercent { get; set; }
    public decimal RentTaxPercent { get; set; }
    public decimal ServiceTaxPercent { get; set; }
    public decimal StampDutyPercent { get; set; }
    public decimal RegistrationFeePercent { get; set; }

    public decimal WithholdingOnCommissionPercent { get; set; }
    public decimal WithholdingOnRentPercent { get; set; }
    public decimal WithholdingOnContractorPercent { get; set; }
    public decimal WithholdingOnPropertySalePercent { get; set; }

    /// <summary>Non-filers are withheld at a higher rate in several markets. A real, material difference.</summary>
    public decimal NonFilerUpliftPercent { get; set; }

    public Guid? SalesTaxGroupId { get; set; }
    public Guid? RentTaxGroupId { get; set; }

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
}

public class TaxComputation : BaseEntity
{
    public Guid? BookingId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? DealId { get; set; }
    public Guid? DemandId { get; set; }

    public string TaxType { get; set; } = string.Empty;
    public decimal TaxableAmount { get; set; }
    public decimal Rate { get; set; }
    public decimal TaxAmount { get; set; }

    /// <summary>The authority's own valuation, where duty is charged on the higher of the two.</summary>
    public decimal? GovernmentValue { get; set; }

    public string? ExemptionReference { get; set; }
    public DateOnly ComputedOn { get; set; }
    public Guid? TaxProfileId { get; set; }
    public bool IsPaid { get; set; }
    public string? ChallanReference { get; set; }
}

public class WithholdingRecord : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public WithholdingKind Kind { get; set; }
    public Guid PartyId { get; set; }
    public string? TaxNumber { get; set; }

    /// <summary>Filers are withheld at a lower rate. Recorded because the difference is large.</summary>
    public bool IsFiler { get; set; }

    public DateOnly DeductedOn { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal Rate { get; set; }
    public decimal WithheldAmount { get; set; }

    public Guid? CommissionPayoutId { get; set; }
    public Guid? OwnerPayoutLineId { get; set; }
    public Guid? SubcontractorClaimId { get; set; }
    public Guid? DistributionId { get; set; }

    public bool IsDeposited { get; set; }
    public DateOnly? DepositedOn { get; set; }
    public string? ChallanNumber { get; set; }
    public bool CertificateIssued { get; set; }
    public string? CertificateUrl { get; set; }
    public string? ReturnPeriod { get; set; }
}
