using RealEstate.Domain.Enums;

namespace RealEstate.Application.DTOs;

// =====================================================================================
// Joint ventures, investors, escrow, project finance, revenue recognition, compliance
// and documents.
// =====================================================================================

public class JointVentureDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public Guid? LandParcelId { get; set; }
    public JvShareBasis Basis { get; set; }

    public DateOnly AgreementDate { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public string? DocumentUrl { get; set; }

    public decimal LandownerSharePercent { get; set; }
    public decimal DeveloperSharePercent { get; set; }
    public AreaDto? LandownerArea { get; set; }
    public decimal? ManagementFeePercent { get; set; }
    public decimal RefundableSecurity { get; set; }
    public decimal NonRefundableDeposit { get; set; }
    public bool ShareOnCollection { get; set; }

    public decimal TotalLandownerEntitlement { get; set; }
    public decimal TotalLandownerPaid { get; set; }
    public decimal LandownerBalance { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public bool HasSpecialPurposeVehicle { get; set; }
    public string? SpvName { get; set; }
    public bool IsActive { get; set; }
    public string? Terms { get; set; }

    public int AllocatedUnitCount { get; set; }
    public decimal AllocatedUnitValue { get; set; }
    public List<JvPartnerDto> Partners { get; set; } = [];
    public List<JvShareTermDto> ShareTerms { get; set; } = [];
    public List<LandownerAllocationDto> Allocations { get; set; } = [];
}

public class JvPartnerDto
{
    public Guid? Id { get; set; }
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = "Landowner";
    public decimal SharePercent { get; set; }
    public decimal ContributedValue { get; set; }
    public string? ContributionType { get; set; }
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public decimal WithholdingPercent { get; set; }
    public decimal Entitlement { get; set; }
    public decimal Paid { get; set; }
    public decimal Balance { get; set; }
    public bool IsActive { get; set; }
}

public class JvShareTermDto
{
    public Guid? Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Trigger { get; set; } = "OnCollection";
    public Guid? ProjectMilestoneId { get; set; }
    public string? MilestoneName { get; set; }
    public decimal? TriggerCollectionPercent { get; set; }
    public decimal Percent { get; set; }
    public decimal FixedAmount { get; set; }
    public decimal EntitlementAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateOnly? DueDate { get; set; }
    public bool IsSettled { get; set; }
    public bool IsTriggered { get; set; }
    public int SortOrder { get; set; }
}

public class LandownerAllocationDto
{
    public Guid Id { get; set; }
    public Guid JointVentureId { get; set; }
    public Guid? JvPartnerId { get; set; }
    public string? PartnerName { get; set; }
    public Guid UnitId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public string? BlockName { get; set; }
    public DateOnly AllocatedOn { get; set; }
    public AreaDto Area { get; set; } = new();
    public decimal NotionalValue { get; set; }
    public string Status { get; set; } = "Allocated";
    public bool IsBoughtBack { get; set; }
    public decimal? BuyBackPrice { get; set; }
    public DateOnly? HandedOverOn { get; set; }
    public string? Note { get; set; }
}

public class LandownerLedgerEntryDto
{
    public Guid Id { get; set; }
    public DateOnly EntryDate { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal RunningBalance { get; set; }
    public string? PartnerName { get; set; }
    public string? ReceiptNumber { get; set; }
}

public class InvestorDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid PartyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string InvestmentType { get; set; } = "Equity";

    public decimal CommittedAmount { get; set; }
    public decimal ContributedAmount { get; set; }
    public decimal UndrawnAmount { get; set; }
    public decimal DistributedAmount { get; set; }
    public decimal SharePercent { get; set; }
    public decimal PreferredReturnPercent { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public DateOnly? InvestedOn { get; set; }
    public DateOnly? ExitDate { get; set; }
    public decimal? RealisedIrr { get; set; }
    public decimal? UnrealisedIrr { get; set; }
    public decimal? MultipleOnInvestedCapital { get; set; }

    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public decimal WithholdingPercent { get; set; }
    public string? AgreementUrl { get; set; }
    public bool PortalAccessEnabled { get; set; }
    public bool IsActive { get; set; }

    public List<CapitalCallDto> Calls { get; set; } = [];
    public List<ContributionDto> Contributions { get; set; } = [];
    public List<DistributionDto> Distributions { get; set; } = [];
}

public class CapitalCallDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public Guid? InvestorId { get; set; }
    public string? InvestorName { get; set; }
    public DateOnly IssuedOn { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public DateOnly? ReceivedOn { get; set; }
    public string? Purpose { get; set; }
    public bool IsDefaulted { get; set; }
    public decimal? DefaultPenalty { get; set; }
    public bool IsSettled { get; set; }
    public bool IsOverdue { get; set; }
    public string? NoticeUrl { get; set; }
    public string CurrencyCode { get; set; } = "USD";
}

public class ContributionDto
{
    public Guid Id { get; set; }
    public Guid InvestorId { get; set; }
    public string? InvestorName { get; set; }
    public Guid? CapitalCallId { get; set; }
    public DateOnly ReceivedOn { get; set; }
    public decimal Amount { get; set; }
    public PaymentInstrument Instrument { get; set; }
    public string? Reference { get; set; }
}

public class DistributionDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid InvestorId { get; set; }
    public string InvestorName { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public DateOnly DistributedOn { get; set; }
    public string DistributionType { get; set; } = "ProfitShare";
    public decimal GrossAmount { get; set; }
    public decimal WithholdingAmount { get; set; }
    public decimal NetAmount { get; set; }
    public int? WaterfallTier { get; set; }
    public string? PaymentReference { get; set; }
    public bool IsPaid { get; set; }
    public string CurrencyCode { get; set; } = "USD";
}

// ── Escrow ───────────────────────────────────────────────────────────────────

public class ProjectBankAccountDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public ProjectAccountKind Kind { get; set; }
    public string AccountTitle { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string? BankName { get; set; }
    public string? BranchName { get; set; }
    public string? IbanOrSwift { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public decimal Balance { get; set; }
    public decimal TotalCredited { get; set; }
    public decimal TotalWithdrawn { get; set; }
    public decimal DepositPercent { get; set; }

    public decimal WithdrawalEntitlement { get; set; }
    public decimal WithdrawnAgainstEntitlement { get; set; }
    public decimal AvailableForWithdrawal { get; set; }
    public decimal PhysicalProgressPercent { get; set; }

    public string? RegulatorReference { get; set; }
    public bool RequiresEngineerCertificate { get; set; }
    public bool RequiresArchitectCertificate { get; set; }
    public bool RequiresAuditorCertificate { get; set; }
    public DateOnly? LastAuditedOn { get; set; }
    public DateOnly? NextAuditDue { get; set; }
    public bool AuditOverdue { get; set; }
    public bool IsActive { get; set; }
}

public class EscrowLedgerEntryDto
{
    public Guid Id { get; set; }
    public DateOnly EntryDate { get; set; }
    public EscrowMovementKind Kind { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal CreditAmount { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal RunningBalance { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? BookingReference { get; set; }
    public string? CustomerName { get; set; }
    public bool IsReconciled { get; set; }
    public string? BankReference { get; set; }
}

public class EscrowWithdrawalDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid ProjectBankAccountId { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateOnly RequestedOn { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public string? Purpose { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public decimal ProgressPercentAtRequest { get; set; }
    public decimal EntitlementAtRequest { get; set; }
    public decimal AlreadyWithdrawn { get; set; }
    public decimal AvailableAtRequest { get; set; }

    public string Status { get; set; } = "Requested";
    public bool ExceededEntitlement { get; set; }
    public string? RequestedByName { get; set; }
    public DateOnly? WithdrawnOn { get; set; }
    public string? BankReference { get; set; }
    public List<WithdrawalCertificateDto> Certificates { get; set; } = [];

    /// <summary>Which required certificates are still missing. The gate, made explicit.</summary>
    public List<string> MissingCertificates { get; set; } = [];
}

public class WithdrawalCertificateDto
{
    public Guid? Id { get; set; }
    public string CertifierType { get; set; } = "Engineer";
    public string CertifierName { get; set; } = string.Empty;
    public string? RegistrationNumber { get; set; }
    public DateOnly CertifiedOn { get; set; }
    public decimal CertifiedProgressPercent { get; set; }
    public decimal? CertifiedCostIncurred { get; set; }
    public string? DocumentUrl { get; set; }
    public bool IsVerified { get; set; }
}

public class EscrowWithdrawalRequestDto
{
    public Guid ProjectBankAccountId { get; set; }
    public decimal RequestedAmount { get; set; }
    public string? Purpose { get; set; }
    public List<WithdrawalCertificateDto> Certificates { get; set; } = [];
    public bool DryRun { get; set; }
}

public class ProjectLoanDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string? LenderName { get; set; }
    public LoanStatus Status { get; set; }

    public decimal SanctionedAmount { get; set; }
    public decimal DrawnAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal RepaidAmount { get; set; }
    public decimal UndrawnAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public decimal InterestRate { get; set; }
    public string? RateBasis { get; set; }
    public decimal AccruedInterest { get; set; }
    public decimal PaidInterest { get; set; }

    public DateOnly? SanctionedOn { get; set; }
    public DateOnly? FirstDrawdownOn { get; set; }
    public DateOnly? MoratoriumEndsOn { get; set; }
    public DateOnly? MaturityDate { get; set; }
    public int TenureMonths { get; set; }

    public string? SecurityDescription { get; set; }
    public bool UnitsMortgaged { get; set; }
    public int MortgagedUnitCount { get; set; }
    public string? Covenants { get; set; }
    public DateOnly? NextCovenantTestOn { get; set; }
    public string? DocumentUrl { get; set; }

    public List<LoanDrawdownDto> Drawdowns { get; set; } = [];
    public List<LoanRepaymentDto> Repayments { get; set; } = [];
}

public class LoanDrawdownDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateOnly RequestedOn { get; set; }
    public DateOnly? ReceivedOn { get; set; }
    public decimal Amount { get; set; }
    public decimal ProgressPercentAtDrawdown { get; set; }
    public Guid? MilestoneCertificateId { get; set; }
    public string? Purpose { get; set; }
    public decimal ProcessingFee { get; set; }
    public bool IsReceived { get; set; }
}

public class LoanRepaymentDto
{
    public Guid Id { get; set; }
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
}

public class BankGuaranteeDto
{
    public Guid Id { get; set; }
    public string GuaranteeNumber { get; set; } = string.Empty;
    public GuaranteeKind Kind { get; set; }
    public string Direction { get; set; } = "Received";
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public Guid? SubcontractId { get; set; }
    public string? CounterpartyName { get; set; }
    public string? IssuingBank { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public DateOnly IssuedOn { get; set; }
    public DateOnly ExpiresOn { get; set; }
    public DateOnly? ClaimPeriodEndsOn { get; set; }
    public int DaysToExpiry { get; set; }
    public bool IsExpiringSoon { get; set; }
    public decimal? Commission { get; set; }
    public decimal? MarginHeld { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsAutoRenewing { get; set; }
    public DateOnly? ReleasedOn { get; set; }
    public string? DocumentUrl { get; set; }
}

public class CustomerMortgageDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid BookingId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string? LenderName { get; set; }
    public string Status { get; set; } = "Applied";

    public decimal AppliedAmount { get; set; }
    public decimal SanctionedAmount { get; set; }
    public decimal DisbursedAmount { get; set; }
    public decimal PendingDisbursement { get; set; }
    public decimal InterestRate { get; set; }
    public int TenureYears { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public DateOnly? AppliedOn { get; set; }
    public DateOnly? SanctionedOn { get; set; }
    public DateOnly? SanctionValidUntil { get; set; }
    public bool SanctionExpiringSoon { get; set; }
    public bool TripartiteAgreementSigned { get; set; }
    public Guid? NocToMortgageId { get; set; }
    public string? RejectionReason { get; set; }
    public string? DocumentUrl { get; set; }
    public List<MortgageDisbursementDto> Disbursements { get; set; } = [];
}

public class MortgageDisbursementDto
{
    public Guid Id { get; set; }
    public int SequenceNumber { get; set; }
    public DateOnly? ExpectedOn { get; set; }
    public DateOnly? ReceivedOn { get; set; }
    public decimal Amount { get; set; }
    public Guid? ProjectMilestoneId { get; set; }
    public string? MilestoneName { get; set; }
    public Guid? DemandId { get; set; }
    public string? BankReference { get; set; }
    public bool IsReceived { get; set; }
    public bool IsOverdue { get; set; }
    public string? DelayReason { get; set; }
}

// ── Revenue recognition ──────────────────────────────────────────────────────

public class RecognitionPolicyDto
{
    public Guid Id { get; set; }
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? ClientBuildContractId { get; set; }
    public RecognitionBasis Basis { get; set; }
    public string? OverTimeMethod { get; set; }
    public string? PointInTimeTrigger { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public DateOnly DeterminedOn { get; set; }
    public string? DeterminedByName { get; set; }
    public bool IsActive { get; set; }
}

public class RevenueRecognitionRunDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
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
    public string CurrencyCode { get; set; } = "USD";
    public string? RunByName { get; set; }
    public bool IsPosted { get; set; }
    public string? ErrorSummary { get; set; }
    public List<RecognitionEntryDto> Entries { get; set; } = [];
}

public class RecognitionEntryDto
{
    public Guid Id { get; set; }
    public Guid? BookingId { get; set; }
    public string? BookingReference { get; set; }
    public Guid? ClientBuildContractId { get; set; }
    public string? ContractReference { get; set; }
    public string? CustomerName { get; set; }
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
    public decimal ContractAsset { get; set; }
    public decimal ContractLiability { get; set; }
    public bool IsLossMaking { get; set; }
    public decimal? ProvisionForLoss { get; set; }
}

public class WipEntryDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public Guid? WbsNodeId { get; set; }
    public Guid? UnitId { get; set; }
    public DateOnly EntryDate { get; set; }
    public string CostCategory { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal RunningBalance { get; set; }
    public string? SourceDocumentType { get; set; }
    public bool IsReleasedToCogs { get; set; }
}

public class UnitCostAllocationDto
{
    public Guid Id { get; set; }
    public Guid UnitId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
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
    public AreaDto Area { get; set; } = new();
    public decimal CostPerSqFt { get; set; }
    public CostAllocationBasis Basis { get; set; }
    public string CurrencyCode { get; set; } = "USD";
}

public class UnitProfitabilityDto
{
    public Guid UnitId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public string? BlockName { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public PropertySubType? SubType { get; set; }
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

    public AreaDto Area { get; set; } = new();
    public decimal RealisationPerSqFt { get; set; }
    public decimal CostPerSqFt { get; set; }
    public decimal MarginPerSqFt { get; set; }
    public string CurrencyCode { get; set; } = "USD";
}

public class ProjectPnlDto
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateOnly AsOfDate { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public RecognitionBasis RecognitionBasis { get; set; }

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

    public List<ProjectBudgetLineDto> CostBreakdown { get; set; } = [];
    public List<TrendPointDto> RevenueTrend { get; set; } = [];
}

public class TaxProfileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public decimal SalesTaxPercent { get; set; }
    public decimal RentTaxPercent { get; set; }
    public decimal ServiceTaxPercent { get; set; }
    public decimal StampDutyPercent { get; set; }
    public decimal RegistrationFeePercent { get; set; }
    public decimal WithholdingOnCommissionPercent { get; set; }
    public decimal WithholdingOnRentPercent { get; set; }
    public decimal WithholdingOnContractorPercent { get; set; }
    public decimal WithholdingOnPropertySalePercent { get; set; }
    public decimal NonFilerUpliftPercent { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
}

public class WithholdingRecordDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public WithholdingKind Kind { get; set; }
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public bool IsFiler { get; set; }
    public DateOnly DeductedOn { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal Rate { get; set; }
    public decimal WithheldAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool IsDeposited { get; set; }
    public DateOnly? DepositedOn { get; set; }
    public string? ChallanNumber { get; set; }
    public bool CertificateIssued { get; set; }
    public string? CertificateUrl { get; set; }
    public string? ReturnPeriod { get; set; }
}

// ── Compliance & documents ───────────────────────────────────────────────────

public class LicenceRecordDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string LicenceType { get; set; } = string.Empty;
    public string LicenceNumber { get; set; } = string.Empty;
    public string? Authority { get; set; }
    public string? OfficeName { get; set; }
    public string? AgentName { get; set; }
    public DateOnly IssuedOn { get; set; }
    public DateOnly ExpiresOn { get; set; }
    public int DaysToExpiry { get; set; }
    public bool IsExpired { get; set; }
    public bool IsExpiringSoon { get; set; }
    public decimal RenewalFee { get; set; }
    public bool IsMandatoryToTrade { get; set; }
    public string? DocumentUrl { get; set; }
    public bool IsCurrent { get; set; }
}

public class ComplianceCalendarEntryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public Guid? PropertyId { get; set; }
    public string? AddressOneLine { get; set; }
    public string? SocietyName { get; set; }
    public DateOnly DueDate { get; set; }
    public int DaysToDue { get; set; }
    public string? Recurrence { get; set; }
    public string? OwnerName { get; set; }
    public AlertSeverity Severity { get; set; }
    public bool AlertSent { get; set; }
    public bool IsCompleted { get; set; }
    public DateOnly? CompletedOn { get; set; }
    public string? EvidenceUrl { get; set; }
    public bool IsOverdue { get; set; }
    public decimal? PenaltyIfMissed { get; set; }
    public string? Route { get; set; }
}

public class RegulatoryFilingDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string FilingType { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string? Authority { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly DueOn { get; set; }
    public DateOnly? FiledOn { get; set; }
    public string Status { get; set; } = "Draft";
    public int Version { get; set; }
    public string? AcknowledgementNumber { get; set; }
    public string? PreparedByName { get; set; }
    public string? DocumentUrl { get; set; }
    public string? QueryFromAuthority { get; set; }
    public decimal? LateFilingPenalty { get; set; }
    public bool IsOverdue { get; set; }
    public int DaysToDue { get; set; }
}

public class QuarterlyProgressReportDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Quarter { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }

    public int TotalUnits { get; set; }
    public int UnitsBooked { get; set; }
    public int UnitsBookedThisQuarter { get; set; }
    public AreaDto AreaBooked { get; set; } = new();
    public decimal TotalBookingValue { get; set; }

    public decimal AmountCollected { get; set; }
    public decimal AmountCollectedThisQuarter { get; set; }
    public decimal AmountDepositedToEscrow { get; set; }
    public decimal AmountWithdrawnFromEscrow { get; set; }
    public decimal EscrowBalance { get; set; }
    public decimal AmountSpentOnConstruction { get; set; }
    public decimal AmountSpentOnLand { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public decimal PhysicalProgressPercent { get; set; }
    public decimal FinancialProgressPercent { get; set; }
    public DateOnly? OriginalCompletionDate { get; set; }
    public DateOnly? RevisedCompletionDate { get; set; }
    public string? DelayReason { get; set; }
    public int ApprovalsObtained { get; set; }
    public int ApprovalsPending { get; set; }

    public string? CertifiedByEngineerName { get; set; }
    public string? ArchitectCertificateUrl { get; set; }
    public string? CaCertificateUrl { get; set; }
    public bool IsFiled { get; set; }
    public string? DocumentUrl { get; set; }
    public List<QprLineDto> Buildings { get; set; } = [];
}

public class QprLineDto
{
    public Guid Id { get; set; }
    public Guid? ProjectNodeId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public int UnitCount { get; set; }
    public int BookedCount { get; set; }
    public decimal ProgressPercent { get; set; }
    public string? CurrentStage { get; set; }
    public DateOnly? ExpectedCompletion { get; set; }
    public int SortOrder { get; set; }
}

public class DocumentTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string LanguageCode { get; set; } = "en";
    public bool IsRightToLeft { get; set; }
    public int CurrentVersion { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public string? LetterheadUrl { get; set; }
    public bool IncludeQrVerification { get; set; }
    public bool IncludeAmountInWords { get; set; }
    public string? PaperSize { get; set; }
    public int UsageCount { get; set; }
    public List<TemplateVersionDto> Versions { get; set; } = [];
}

public class TemplateVersionDto
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? HeaderHtml { get; set; }
    public string? FooterHtml { get; set; }
    public string? StyleCss { get; set; }
    public List<string> MergeFields { get; set; } = [];
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsPublished { get; set; }
    public string? ChangeNote { get; set; }
    public string? CreatedByName { get; set; }
}

public class ClauseLibraryItemDto
{
    public Guid Id { get; set; }
    public string ClauseKey { get; set; } = string.Empty;
    public string Heading { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = "en";
    public int Version { get; set; }
    public bool IsMandatory { get; set; }
    public bool IsNegotiable { get; set; }
    public Guid? ProjectId { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

public class DocumentGenerationRequestDto
{
    public string DocumentType { get; set; } = string.Empty;
    public Guid? DocumentTemplateId { get; set; }
    public Guid EntityId { get; set; }
    public string LanguageCode { get; set; } = "en";

    /// <summary>Extra merge values the caller supplies on top of what the server resolves.</summary>
    public Dictionary<string, string> Overrides { get; set; } = [];

    public bool SendImmediately { get; set; }
    public List<NotificationChannel> Channels { get; set; } = [];
}

public class SignatureSessionDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid? GeneratedDocumentId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string? DocumentTitle { get; set; }
    public SignatureMethod Method { get; set; }
    public SignatureState State { get; set; }
    public DateTime InitiatedAt { get; set; }
    public string? InitiatedByName { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsSequential { get; set; }
    public string? SignedDocumentUrl { get; set; }
    public string? AuditCertificateUrl { get; set; }
    public string? DeclineReason { get; set; }
    public int SignedCount { get; set; }
    public int TotalSigners { get; set; }
    public List<SignaturePartyDto> Parties { get; set; } = [];
}

public class SignaturePartyDto
{
    public Guid? Id { get; set; }
    public Guid? PartyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Role { get; set; } = "Signer";
    public int SigningOrder { get; set; }
    public SignatureState State { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ViewedAt { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? SignatureImageUrl { get; set; }
    public string? ThumbImpressionUrl { get; set; }
    public string? PhotoUrl { get; set; }
    public bool OtpVerified { get; set; }
    public string? DeclineReason { get; set; }
}

public class PhysicalFileDto
{
    public Guid Id { get; set; }
    public string FileNumber { get; set; } = string.Empty;
    public Guid? BookingId { get; set; }
    public string? BookingReference { get; set; }
    public Guid? PlotFileId { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitNumber { get; set; }
    public string? PartyName { get; set; }
    public string? ProjectName { get; set; }
    public PhysicalFileState State { get; set; }
    public string? RoomLocation { get; set; }
    public string? Rack { get; set; }
    public string? Cabinet { get; set; }
    public string? Shelf { get; set; }
    public string? BarcodeOrRfid { get; set; }
    public string? IssuedToName { get; set; }
    public DateOnly? IssuedOn { get; set; }
    public DateOnly? DueBackOn { get; set; }
    public bool IsOverdue { get; set; }
    public int DocumentCount { get; set; }
    public DateOnly? LastAuditedOn { get; set; }
    public bool IsMissing { get; set; }
    public string? Note { get; set; }
    public List<PhysicalFileMovementDto> Movements { get; set; } = [];
}

public class PhysicalFileMovementDto
{
    public Guid Id { get; set; }
    public string Movement { get; set; } = "Issued";
    public DateTime OccurredAt { get; set; }
    public string? FromName { get; set; }
    public string? ToName { get; set; }
    public string? Purpose { get; set; }
    public DateOnly? DueBackOn { get; set; }
    public string? RecordedByName { get; set; }
    public string? SignatureUrl { get; set; }
    public string? Note { get; set; }
}

public class LegalCaseDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string? CaseNumber { get; set; }
    public string CaseType { get; set; } = string.Empty;
    public LegalCaseStatus Status { get; set; }

    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public Guid? PropertyId { get; set; }
    public string? AddressOneLine { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitNumber { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? SubcontractId { get; set; }
    public Guid? PartyId { get; set; }
    public string? PartyName { get; set; }

    public string OurRole { get; set; } = "Defendant";
    public string? OpposingParty { get; set; }
    public string? Court { get; set; }
    public string? Jurisdiction { get; set; }
    public DateOnly FiledOn { get; set; }
    public DateOnly? NextHearingDate { get; set; }
    public int? DaysToHearing { get; set; }
    public DateOnly? DecidedOn { get; set; }

    public decimal? ClaimAmount { get; set; }
    public decimal? ExposureAmount { get; set; }
    public decimal LegalCostsIncurred { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public string? AdvocateName { get; set; }
    public string? AdvocateContact { get; set; }
    public string? OwnerName { get; set; }
    public bool BlocksTransaction { get; set; }
    public string? Outcome { get; set; }
    public string? Summary { get; set; }
    public bool IsClosed { get; set; }
    public List<LegalHearingDto> Hearings { get; set; } = [];
}

public class LegalHearingDto
{
    public Guid Id { get; set; }
    public DateOnly HearingDate { get; set; }
    public string? Purpose { get; set; }
    public bool Attended { get; set; }
    public string? AttendedByName { get; set; }
    public string? Outcome { get; set; }
    public DateOnly? NextDate { get; set; }
    public string? NextPurpose { get; set; }
    public string? OrderSummary { get; set; }
    public string? OrderDocumentUrl { get; set; }
    public decimal? CostIncurred { get; set; }
    public string? Note { get; set; }
}
