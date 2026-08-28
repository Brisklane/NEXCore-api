using Nexcore.SharedKernel;
using RealEstate.Domain.Enums;

namespace RealEstate.Domain.Entities;

/// <summary>
/// The building side of a project. Either our own development, or a contract we are executing for
/// somebody else — the cost model is identical, only who pays differs.
/// </summary>
public class ConstructionProject : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public Guid? ProjectId { get; set; }
    public Guid? ClientBuildContractId { get; set; }
    public Guid? PropertyId { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;
    public ProgressMethod ProgressMethod { get; set; } = ProgressMethod.ByQuantity;

    public DateOnly? StartDate { get; set; }
    public DateOnly? PlannedCompletionDate { get; set; }
    public DateOnly? ForecastCompletionDate { get; set; }
    public DateOnly? ActualCompletionDate { get; set; }

    /// <summary>Approved extensions of time. The contractual completion date, not the original one.</summary>
    public int ExtensionDaysGranted { get; set; }

    public decimal ContractValue { get; set; }
    public decimal ApprovedVariations { get; set; }
    public decimal RevisedContractValue { get; set; }
    public string CurrencyCode { get; set; } = AppConstants.DefaultCurrency;

    public decimal BudgetCost { get; set; }
    public decimal CommittedCost { get; set; }
    public decimal ActualCost { get; set; }

    /// <summary>Actual plus what is still to come. The number that says whether the job makes money.</summary>
    public decimal ForecastFinalCost { get; set; }

    public decimal CertifiedValue { get; set; }
    public decimal InvoicedValue { get; set; }
    public decimal ReceivedValue { get; set; }
    public decimal RetentionHeld { get; set; }

    public decimal PhysicalProgressPercent { get; set; }
    public decimal FinancialProgressPercent { get; set; }

    public Guid? ProjectManagerUserId { get; set; }
    public Guid? QuantitySurveyorUserId { get; set; }
    public Guid? SiteEngineerUserId { get; set; }
    public Guid? SiteWarehouseId { get; set; }

    public decimal DefaultRetentionPercent { get; set; } = 10m;
    public decimal RetentionCapPercent { get; set; } = 5m;
    public int DefectsPeriodMonths { get; set; } = 12;

    public ICollection<WbsNode> WbsNodes { get; set; } = [];
}

/// <summary>
/// The work breakdown structure. Self-referencing to any depth, because a project is packages is
/// activities is tasks and every level needs its own budget and its own responsible party.
/// </summary>
public class WbsNode : BaseEntity
{
    public Guid ConstructionProjectId { get; set; }
    public ConstructionProject? Project { get; set; }

    public Guid? ParentNodeId { get; set; }
    public WbsNode? Parent { get; set; }

    public WbsKind Kind { get; set; } = WbsKind.Activity;
    public string Name { get; set; } = string.Empty;
    public int Depth { get; set; }
    public string? Path { get; set; }
    public int SortOrder { get; set; }

    /// <summary>Set when the node is a specific block or tower rather than a trade package.</summary>
    public Guid? ProjectNodeId { get; set; }

    public decimal BudgetAmount { get; set; }
    public decimal CommittedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal ForecastAmount { get; set; }
    public decimal EarnedValue { get; set; }

    /// <summary>Share of the whole project this node represents, for rolled-up progress.</summary>
    public decimal WeightPercent { get; set; }

    public decimal ProgressPercent { get; set; }
    public Guid? ResponsibleUserId { get; set; }
    public Guid? SubcontractId { get; set; }

    public ICollection<WbsNode> Children { get; set; } = [];
}

/// <summary>The priced schedule of work the whole cost engine runs on.</summary>
public class BillOfQuantities : BaseEntity
{
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public Guid ConstructionProjectId { get; set; }
    public Guid? SubcontractId { get; set; }
    public Guid? TenderId { get; set; }

    public int Version { get; set; } = 1;
    public bool IsCurrent { get; set; } = true;
    public Guid? SupersedesBoqId { get; set; }

    /// <summary>"Estimate", "Tender", "Contract", "AsBuilt".</summary>
    public string BoqType { get; set; } = "Contract";

    public decimal TotalAmount { get; set; }
    public decimal ProvisionalSumsTotal { get; set; }
    public decimal ContingencyTotal { get; set; }
    public string CurrencyCode { get; set; } = AppConstants.DefaultCurrency;

    public DateOnly? PreparedOn { get; set; }
    public Guid? PreparedByUserId { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public bool IsApproved { get; set; }
    public string? DocumentUrl { get; set; }

    public ICollection<BoqSection> Sections { get; set; } = [];
}

public class BoqSection : BaseEntity
{
    public Guid BillOfQuantitiesId { get; set; }
    public BillOfQuantities? Boq { get; set; }

    public Guid? ParentSectionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int SortOrder { get; set; }
    public Guid? WbsNodeId { get; set; }
}

/// <summary>
/// One measurable item of work. Every claim, certificate and progress measurement points at one of
/// these, which is why the quantity and the rate are the two most audited numbers on a site.
/// </summary>
public class BoqLine : BaseEntity
{
    public Guid BillOfQuantitiesId { get; set; }
    public Guid? BoqSectionId { get; set; }
    public Guid? WbsNodeId { get; set; }

    public string ItemCode { get; set; } = string.Empty;
    public BoqLineKind Kind { get; set; } = BoqLineKind.Measured;

    public string Uom { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Quantity re-measured on site, where the contract is measured rather than lump sum.</summary>
    public decimal? RemeasuredQuantity { get; set; }

    public decimal ExecutedQuantity { get; set; }
    public decimal CertifiedQuantity { get; set; }
    public decimal ProgressPercent { get; set; }

    public Guid? RateAnalysisId { get; set; }
    public Guid? ItemId { get; set; }

    /// <summary>Cost we expect to incur, against the rate we charge. The margin on this line.</summary>
    public decimal BudgetCostRate { get; set; }

    public decimal ActualCost { get; set; }

    /// <summary>Added by a variation rather than present in the original bill.</summary>
    public bool IsVariation { get; set; }
    public Guid? VariationOrderId { get; set; }

    public int SortOrder { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// How a rate was built up from material, labour, plant, overhead and profit. Kept so a rate can
/// be defended in a negotiation and re-derived when cement moves 20%.
/// </summary>
public class RateAnalysis : BaseEntity
{
    public string Uom { get; set; } = string.Empty;

    public Guid? ConstructionProjectId { get; set; }
    public decimal OutputQuantity { get; set; } = 1m;

    public decimal MaterialCost { get; set; }
    public decimal LabourCost { get; set; }
    public decimal PlantCost { get; set; }
    public decimal SubtotalCost { get; set; }
    public decimal OverheadPercent { get; set; }
    public decimal ProfitPercent { get; set; }
    public decimal FinalRate { get; set; }

    public DateOnly? PricedOn { get; set; }
    public bool IsLibraryItem { get; set; }

    public ICollection<RateComponent> Components { get; set; } = [];
}

public class RateComponent : BaseEntity
{
    public Guid RateAnalysisId { get; set; }
    public RateAnalysis? Analysis { get; set; }

    /// <summary>"Material", "Labour", "Plant", "Sundry".</summary>
    public string ComponentType { get; set; } = "Material";

    public Guid? ItemId { get; set; }
    public string? Uom { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Allowance for cutting, breakage and over-ordering. Real, and always underestimated.</summary>
    public decimal WastagePercent { get; set; }

    public int SortOrder { get; set; }
}

/// <summary>A priced estimate, which becomes the BOQ when the job is won.</summary>
public class Estimate : BaseEntity
{
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public Guid? ConstructionProjectId { get; set; }
    public Guid? ClientBuildContractId { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? PartyId { get; set; }

    public int Version { get; set; } = 1;
    public DateOnly PreparedOn { get; set; }
    public DateOnly? ValidUntil { get; set; }

    public decimal DirectCost { get; set; }
    public decimal OverheadPercent { get; set; }
    public decimal ProfitPercent { get; set; }
    public decimal ContingencyPercent { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal? AreaSqFt { get; set; }
    public decimal? RatePerSqFt { get; set; }

    public SpecificationGrade? Grade { get; set; }

    /// <summary>"Draft", "Issued", "Accepted", "Rejected", "Expired", "Superseded".</summary>
    public string Status { get; set; } = "Draft";

    public Guid? BillOfQuantitiesId { get; set; }
    public string? DocumentUrl { get; set; }
    public string? Assumptions { get; set; }
    public string? Exclusions { get; set; }

    public ICollection<EstimateLine> Lines { get; set; } = [];
}

public class EstimateLine : BaseEntity
{
    public Guid EstimateId { get; set; }
    public Estimate? Estimate { get; set; }

    public string? Uom { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public Guid? RateAnalysisId { get; set; }
    public Guid? WbsNodeId { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// The specification a customer signs and a site builds to. Changes to it are variations — which
/// is exactly why it has to be a record rather than a paragraph in a contract PDF.
/// </summary>
public class SpecificationSchedule : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public Guid? ConstructionProjectId { get; set; }
    public Guid? ClientBuildContractId { get; set; }
    public Guid? ProjectId { get; set; }
    public string? UnitTypeCode { get; set; }

    public SpecificationGrade Grade { get; set; } = SpecificationGrade.Standard;
    public int Version { get; set; } = 1;
    public bool IsFrozen { get; set; }
    public DateOnly? FrozenOn { get; set; }
    public Guid? ApprovedByPartyId { get; set; }
    public string? DocumentUrl { get; set; }

    public ICollection<SpecificationItem> Items { get; set; } = [];
}

public class SpecificationItem : BaseEntity
{
    public Guid SpecificationScheduleId { get; set; }
    public SpecificationSchedule? Schedule { get; set; }

    /// <summary>"Flooring", "WallFinish", "Doors", "Windows", "Kitchen", "Sanitary", "Electrical",
    /// "Hvac", "Joinery", "External", "Landscaping".</summary>
    public string Category { get; set; } = string.Empty;

    public string? Location { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? Specification { get; set; }
    public string? Brand { get; set; }
    public string? ModelOrCode { get; set; }
    public decimal? AllowanceRate { get; set; }
    public string? Uom { get; set; }

    /// <summary>The client picks from a list. Upgrading beyond the allowance becomes a variation.</summary>
    public bool IsClientSelectable { get; set; }
    public string? SelectedOption { get; set; }
    public decimal? UpgradeCost { get; set; }
    public Guid? ClientVariationId { get; set; }

    /// <summary>Supplied by the client, so its value is excluded from the contract rate.</summary>
    public bool IsClientSupplied { get; set; }

    public int SortOrder { get; set; }
}

/// <summary>An activity on the programme, with dependencies so a critical path can be derived.</summary>
public class ProgrammeActivity : BaseEntity
{
    public Guid ConstructionProjectId { get; set; }
    public Guid? WbsNodeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateOnly PlannedStart { get; set; }
    public DateOnly PlannedFinish { get; set; }
    public int DurationDays { get; set; }

    /// <summary>The frozen original. Everything is measured against it, so it must not drift.</summary>
    public DateOnly? BaselineStart { get; set; }
    public DateOnly? BaselineFinish { get; set; }

    public DateOnly? ActualStart { get; set; }
    public DateOnly? ActualFinish { get; set; }
    public decimal ProgressPercent { get; set; }

    /// <summary>Slack. Zero means it is on the critical path and any slip moves completion.</summary>
    public int? TotalFloatDays { get; set; }

    public bool IsCritical { get; set; }
    public bool IsMilestone { get; set; }
    public Guid? ProjectMilestoneId { get; set; }
    public Guid? ResponsibleUserId { get; set; }
    public Guid? SubcontractId { get; set; }
    public int SortOrder { get; set; }
}

public class ActivityDependency : BaseEntity
{
    public Guid ProgrammeActivityId { get; set; }
    public Guid PredecessorActivityId { get; set; }

    /// <summary>"FinishToStart", "StartToStart", "FinishToFinish", "StartToFinish".</summary>
    public string DependencyType { get; set; } = "FinishToStart";

    /// <summary>Positive is a lag, negative is a lead — plaster can start two days before block-work ends.</summary>
    public int LagDays { get; set; }
}

/// <summary>What was actually built this period, measured on site against the bill.</summary>
public class ProgressMeasurement : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid ConstructionProjectId { get; set; }
    public Guid? SubcontractId { get; set; }
    public Guid? WbsNodeId { get; set; }

    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly MeasuredOn { get; set; }
    public Guid? MeasuredByUserId { get; set; }

    public decimal PeriodValue { get; set; }
    public decimal CumulativeValue { get; set; }
    public decimal ProgressPercent { get; set; }
    public ProgressMethod Method { get; set; } = ProgressMethod.ByQuantity;

    public bool IsCertified { get; set; }
    public Guid? CertifiedByUserId { get; set; }
    public DateOnly? CertifiedOn { get; set; }
    public Guid? InterimPaymentCertificateId { get; set; }

    /// <summary>Entered on a tablet on site, sometimes with no signal. Synced when there is one.</summary>
    public bool WasOffline { get; set; }
    public DateTime? OfflineSyncedAt { get; set; }

    public string? PhotoUrls { get; set; }
    public string? Note { get; set; }

    public ICollection<ProgressMeasurementLine> Lines { get; set; } = [];
}

public class ProgressMeasurementLine : BaseEntity
{
    public Guid ProgressMeasurementId { get; set; }
    public ProgressMeasurement? Measurement { get; set; }

    public Guid BoqLineId { get; set; }

    public decimal PreviousQuantity { get; set; }
    public decimal ThisPeriodQuantity { get; set; }
    public decimal CumulativeQuantity { get; set; }
    public decimal Rate { get; set; }
    public decimal ThisPeriodValue { get; set; }
    public decimal CumulativeValue { get; set; }

    /// <summary>What the surveyor allowed, which is rarely exactly what was claimed.</summary>
    public decimal CertifiedQuantity { get; set; }
    public decimal CertifiedValue { get; set; }
    public string? MeasurementNote { get; set; }
    public string? Location { get; set; }
}

/// <summary>
/// An engineer's certificate that a stage is complete. The event that both releases a customer
/// demand and permits a contractor claim — which is why it is a record with a signature and not a
/// checkbox.
/// </summary>
public class MilestoneCertificate : BaseEntity
{
    public string CertificateNumber { get; set; } = string.Empty;

    public Guid ProjectMilestoneId { get; set; }
    public Guid? ConstructionProjectId { get; set; }
    public Guid? ProjectNodeId { get; set; }

    public DateOnly CertifiedOn { get; set; }
    public Guid CertifiedByUserId { get; set; }
    public string? CertifierName { get; set; }
    public string? CertifierQualification { get; set; }

    public decimal ProgressPercent { get; set; }
    public string? Observations { get; set; }
    public string? PhotoUrls { get; set; }
    public string? DocumentUrl { get; set; }

    /// <summary>Set once the demands this certificate unlocks have been raised, so they raise once.</summary>
    public bool DemandsTriggered { get; set; }
    public Guid? DemandBatchId { get; set; }

    public bool IsCountersigned { get; set; }
    public Guid? CountersignedByUserId { get; set; }
}

/// <summary>
/// The periodic bill on a construction contract. Everybody in construction knows this document by
/// its shape: work to date, less previously certified, plus materials on site, plus variations,
/// less retention, less advance recovery, less contra-charges.
/// </summary>
public class InterimPaymentCertificate : BaseEntity
{
    public string CertificateNumber { get; set; } = string.Empty;

    public Guid ConstructionProjectId { get; set; }
    public Guid? SubcontractId { get; set; }
    public Guid? ClientBuildContractId { get; set; }

    /// <summary>"Payable" — we owe a subcontractor; "Receivable" — a client owes us.</summary>
    public string Direction { get; set; } = "Payable";

    public int SequenceNumber { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly IssuedOn { get; set; }
    public DateOnly? DueDate { get; set; }

    public CertificateStatus Status { get; set; } = CertificateStatus.Draft;

    // ── The arithmetic ───────────────────────────────────────────────────────

    public decimal WorkDoneToDate { get; set; }
    public decimal VariationsToDate { get; set; }
    public decimal MaterialsOnSite { get; set; }
    public decimal GrossValueToDate { get; set; }
    public decimal PreviouslyCertified { get; set; }
    public decimal ThisCertificateGross { get; set; }

    public decimal RetentionPercent { get; set; }
    public decimal RetentionThisCertificate { get; set; }
    public decimal RetentionCumulative { get; set; }

    public decimal AdvanceRecovery { get; set; }
    public decimal ContraCharges { get; set; }
    public decimal Penalties { get; set; }
    public decimal OtherDeductions { get; set; }

    public decimal NetBeforeTax { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal WithholdingTax { get; set; }
    public decimal NetPayable { get; set; }
    public decimal PaidAmount { get; set; }

    public string CurrencyCode { get; set; } = AppConstants.DefaultCurrency;

    public Guid? MeasuredByUserId { get; set; }
    public Guid? CertifiedByUserId { get; set; }
    public DateOnly? CertifiedOn { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public Guid? DocumentId { get; set; }
    public Guid? JournalEntryId { get; set; }
    public Guid? SupplierInvoiceId { get; set; }

    public string? Note { get; set; }

    public ICollection<IpcLine> Lines { get; set; } = [];
}

public class IpcLine : BaseEntity
{
    public Guid InterimPaymentCertificateId { get; set; }
    public InterimPaymentCertificate? Certificate { get; set; }

    public Guid? BoqLineId { get; set; }
    public Guid? VariationOrderId { get; set; }
    public Guid? WbsNodeId { get; set; }

    public string? Uom { get; set; }
    public decimal ContractQuantity { get; set; }
    public decimal Rate { get; set; }

    public decimal PreviousQuantity { get; set; }
    public decimal ClaimedQuantity { get; set; }

    /// <summary>What the surveyor allowed. The gap from claimed is where every argument lives.</summary>
    public decimal CertifiedQuantity { get; set; }

    public decimal CertifiedValue { get; set; }
    public string? CertificationNote { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// Money held back from every certificate as security. Released in tranches — half at practical
/// completion, half when the defects period ends — and tracked because forgetting to claim it back
/// is one of the most common ways contractors lose money.
/// </summary>
public class RetentionLedgerEntry : BaseEntity
{
    public Guid ConstructionProjectId { get; set; }
    public Guid? SubcontractId { get; set; }
    public Guid? ClientBuildContractId { get; set; }
    public Guid? PartyId { get; set; }

    /// <summary>"Payable" — we hold theirs; "Receivable" — they hold ours.</summary>
    public string Direction { get; set; } = "Payable";

    public RetentionMovement Movement { get; set; } = RetentionMovement.Held;
    public DateOnly EntryDate { get; set; }

    public Guid? InterimPaymentCertificateId { get; set; }
    public decimal Amount { get; set; }
    public decimal RunningBalance { get; set; }

    public DateOnly? DueForReleaseOn { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public Guid? PunchListId { get; set; }

    /// <summary>Released early against a bank guarantee instead of cash.</summary>
    public Guid? BankGuaranteeId { get; set; }

    public Guid? JournalEntryId { get; set; }
    public string? Note { get; set; }
}

/// <summary>Mobilisation money paid up front against a guarantee, recovered from later certificates.</summary>
public class AdvancePayment : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid ConstructionProjectId { get; set; }
    public Guid? SubcontractId { get; set; }
    public Guid? ClientBuildContractId { get; set; }
    public Guid? PartyId { get; set; }

    public string Direction { get; set; } = "Paid";
    public decimal Amount { get; set; }
    public decimal PercentOfContract { get; set; }
    public DateOnly PaidOn { get; set; }

    /// <summary>Share of each certificate that goes to clearing the advance.</summary>
    public decimal RecoveryPercent { get; set; } = 20m;

    /// <summary>Recovery does not start until the job is this far along, in some contracts.</summary>
    public decimal RecoveryStartsAtProgressPercent { get; set; }

    public decimal RecoveredAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public DateOnly? FullyRecoveredOn { get; set; }

    public Guid? BankGuaranteeId { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public Guid? JournalEntryId { get; set; }
}

public class AdvanceRecovery : BaseEntity
{
    public Guid AdvancePaymentId { get; set; }
    public Guid? InterimPaymentCertificateId { get; set; }

    public DateOnly RecoveredOn { get; set; }
    public decimal CertificateGross { get; set; }
    public decimal RecoveryPercent { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
}

/// <summary>
/// Delivered but unfixed material, allowed into a certificate at an agreed percentage and reversed
/// as it is consumed. Forgetting the reversal is how a contractor gets paid twice and then finds
/// out at final account.
/// </summary>
public class MaterialsOnSite : BaseEntity
{
    public Guid ConstructionProjectId { get; set; }
    public Guid? SubcontractId { get; set; }
    public Guid? InterimPaymentCertificateId { get; set; }

    public Guid? ItemId { get; set; }
    public string? Uom { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Value { get; set; }

    /// <summary>Usually less than 100 — the material is on site but not yet built in.</summary>
    public decimal AllowedPercent { get; set; } = 90m;

    public decimal AllowedValue { get; set; }
    public DateOnly DeliveredOn { get; set; }
    public bool IsInsured { get; set; }
    public bool IsSecured { get; set; }

    public decimal ConsumedQuantity { get; set; }
    public decimal ReversedValue { get; set; }
    public bool IsFullyReversed { get; set; }
    public string? PhotoUrl { get; set; }
}

/// <summary>The re-estimate of what is left to spend. Recomputed every period, because a stale one is worse than none.</summary>
public class CostToComplete : BaseEntity
{
    public Guid ConstructionProjectId { get; set; }
    public Guid? WbsNodeId { get; set; }

    public DateOnly AsOfDate { get; set; }

    public decimal BudgetCost { get; set; }
    public decimal CostIncurred { get; set; }
    public decimal CommittedNotIncurred { get; set; }
    public decimal EstimatedRemaining { get; set; }
    public decimal ForecastFinalCost { get; set; }

    /// <summary>Forecast against budget. Negative is an overrun, and it is the headline number.</summary>
    public decimal VarianceToBudget { get; set; }

    public decimal PercentComplete { get; set; }
    public decimal EarnedValue { get; set; }
    public decimal CostPerformanceIndex { get; set; }
    public decimal SchedulePerformanceIndex { get; set; }

    public Guid? PreparedByUserId { get; set; }
    public string? Assumptions { get; set; }
}

/// <summary>A package put out to bid, and the comparison that follows.</summary>
public class Tender : BaseEntity
{
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public Guid ConstructionProjectId { get; set; }
    public Guid? WbsNodeId { get; set; }
    public Guid? BillOfQuantitiesId { get; set; }

    public TenderStatus Status { get; set; } = TenderStatus.Draft;
    public string? Scope { get; set; }

    public decimal EstimatedValue { get; set; }
    public DateOnly? IssuedOn { get; set; }
    public DateTime? SubmissionDeadline { get; set; }
    public DateOnly? RequiredStartDate { get; set; }
    public DateOnly? RequiredFinishDate { get; set; }

    public decimal? EarnestMoneyDeposit { get; set; }
    public string? DocumentUrl { get; set; }

    public Guid? AwardedBidId { get; set; }
    public DateOnly? AwardedOn { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public Guid? SubcontractId { get; set; }
    public string? AwardJustification { get; set; }

    public ICollection<TenderBidder> Bidders { get; set; } = [];
}

public class TenderBidder : BaseEntity
{
    public Guid TenderId { get; set; }
    public Tender? Tender { get; set; }

    public Guid? ContractorId { get; set; }
    public Guid? PartyId { get; set; }
    public string? Name { get; set; }

    public DateOnly? InvitedOn { get; set; }
    public bool HasResponded { get; set; }
    public bool HasDeclined { get; set; }
    public string? DeclineReason { get; set; }
    public bool EmdReceived { get; set; }
}

public class TenderBid : BaseEntity
{
    public Guid TenderId { get; set; }
    public Guid TenderBidderId { get; set; }

    public DateTime SubmittedAt { get; set; }
    public decimal BidAmount { get; set; }
    public int? ProposedDurationDays { get; set; }
    public string? PaymentTerms { get; set; }
    public decimal? AdvanceRequested { get; set; }
    public decimal? RetentionOffered { get; set; }

    public int? TechnicalScore { get; set; }
    public int? CommercialScore { get; set; }
    public int? TotalScore { get; set; }
    public int? Rank { get; set; }

    public string? Qualifications { get; set; }
    public string? Exclusions { get; set; }
    public string? DocumentUrl { get; set; }

    public decimal? NegotiatedAmount { get; set; }
    public bool IsAwarded { get; set; }
    public bool IsDisqualified { get; set; }
    public string? DisqualificationReason { get; set; }
}

/// <summary>A line-by-line comparison of what each bidder priced. The document a tender decision rests on.</summary>
public class BidComparisonLine : BaseEntity
{
    public Guid TenderId { get; set; }
    public Guid? BoqLineId { get; set; }
    public Guid TenderBidId { get; set; }

    public decimal Quantity { get; set; }
    public decimal BidRate { get; set; }
    public decimal BidAmount { get; set; }

    public decimal? EstimateRate { get; set; }
    public decimal? VariancePercent { get; set; }

    /// <summary>Far off the estimate and the other bids — usually a misread or a loaded rate.</summary>
    public bool IsOutlier { get; set; }

    public bool IsLowest { get; set; }
    public string? Note { get; set; }
}

/// <summary>A package awarded to a subcontractor, and everything owed under it.</summary>
public class Subcontract : BaseEntity
{
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public Guid ConstructionProjectId { get; set; }
    public Guid? WbsNodeId { get; set; }
    public Guid? TenderId { get; set; }
    public Guid ContractorId { get; set; }
    public Guid? PartyId { get; set; }

    public SubcontractStatus Status { get; set; } = SubcontractStatus.Draft;
    public ContractKind Kind { get; set; } = ContractKind.Measured;

    public decimal ContractValue { get; set; }
    public decimal ApprovedVariations { get; set; }
    public decimal RevisedValue { get; set; }
    public string CurrencyCode { get; set; } = AppConstants.DefaultCurrency;

    public DateOnly? AwardedOn { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly FinishDate { get; set; }
    public DateOnly? ActualFinishDate { get; set; }
    public int ExtensionDaysGranted { get; set; }

    public decimal RetentionPercent { get; set; } = 10m;
    public decimal RetentionCapPercent { get; set; } = 5m;
    public decimal RetentionHeld { get; set; }
    public decimal RetentionReleased { get; set; }

    public decimal AdvancePercent { get; set; }
    public decimal AdvancePaid { get; set; }
    public decimal AdvanceRecovered { get; set; }

    public int PaymentTermDays { get; set; } = 30;

    /// <summary>Liquidated damages per day of delay. Toothless unless it is on the record.</summary>
    public decimal LiquidatedDamagesPerDay { get; set; }
    public decimal LiquidatedDamagesCapPercent { get; set; }

    public int DefectsPeriodMonths { get; set; } = 12;
    public DateOnly? DefectsPeriodEndsOn { get; set; }

    public decimal CertifiedToDate { get; set; }
    public decimal PaidToDate { get; set; }
    public decimal ProgressPercent { get; set; }

    public Guid? BillOfQuantitiesId { get; set; }
    public Guid? DocumentId { get; set; }
    public bool InsuranceVerified { get; set; }
    public bool LicenceVerified { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public string? TerminationReason { get; set; }

    public ICollection<SubcontractBoqLine> BoqLines { get; set; } = [];
}

/// <summary>Which BOQ lines and rates a subcontract covers. Their rate is rarely our rate.</summary>
public class SubcontractBoqLine : BaseEntity
{
    public Guid SubcontractId { get; set; }
    public Subcontract? Subcontract { get; set; }

    public Guid BoqLineId { get; set; }
    public decimal AwardedQuantity { get; set; }

    /// <summary>What we pay them, against the BOQ rate we charge. The margin on the package.</summary>
    public decimal AwardedRate { get; set; }

    public decimal AwardedAmount { get; set; }
    public decimal ExecutedQuantity { get; set; }
    public decimal CertifiedQuantity { get; set; }
    public Guid? BudgetCodeWbsNodeId { get; set; }
}

/// <summary>A specific instruction to a subcontractor for a scope and a quantity.</summary>
public class SubcontractWorkOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;

    public Guid SubcontractId { get; set; }
    public Guid? WbsNodeId { get; set; }

    public DateOnly IssuedOn { get; set; }
    public DateOnly? RequiredBy { get; set; }
    public decimal Value { get; set; }

    /// <summary>"Issued", "Acknowledged", "InProgress", "Completed", "Cancelled".</summary>
    public string Status { get; set; } = "Issued";

    public Guid? IssuedByUserId { get; set; }
    public bool IsAcknowledged { get; set; }
    public DateOnly? CompletedOn { get; set; }
    public string? DocumentUrl { get; set; }
}

/// <summary>What a subcontractor says they are owed, and what the surveyor certifies.</summary>
public class SubcontractorClaim : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid SubcontractId { get; set; }
    public Guid ContractorId { get; set; }

    public int SequenceNumber { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly SubmittedOn { get; set; }

    public decimal ClaimedGross { get; set; }
    public decimal CertifiedGross { get; set; }

    /// <summary>Claimed less certified. A persistent gap is a conversation with the contractor.</summary>
    public decimal DisallowedAmount { get; set; }

    public decimal PreviouslyCertified { get; set; }
    public decimal ThisPeriodCertified { get; set; }
    public decimal RetentionDeducted { get; set; }
    public decimal AdvanceRecovered { get; set; }
    public decimal ContraChargesDeducted { get; set; }
    public decimal PenaltiesDeducted { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal WithholdingTax { get; set; }
    public decimal NetPayable { get; set; }
    public decimal PaidAmount { get; set; }

    public CertificateStatus Status { get; set; } = CertificateStatus.SubmittedForCertification;
    public Guid? InterimPaymentCertificateId { get; set; }
    public Guid? SupplierInvoiceId { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateOnly? PaidOn { get; set; }

    public string? ClaimDocumentUrl { get; set; }
    public string? DisallowanceReason { get; set; }
    public bool IsDisputed { get; set; }
}

public class ClaimCertification : BaseEntity
{
    public Guid SubcontractorClaimId { get; set; }
    public Guid? BoqLineId { get; set; }

    public decimal ClaimedQuantity { get; set; }
    public decimal CertifiedQuantity { get; set; }
    public decimal Rate { get; set; }
    public decimal ClaimedValue { get; set; }
    public decimal CertifiedValue { get; set; }
    public string? Reason { get; set; }
    public Guid? CertifiedByUserId { get; set; }
}

/// <summary>
/// Something we supplied or did that the subcontractor should pay for — material from our stores,
/// our plant, our power, damage they caused, rework. Deducted from their next claim.
/// </summary>
public class ContraCharge : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid SubcontractId { get; set; }
    public Guid? SubcontractorClaimId { get; set; }
    public Guid ContractorId { get; set; }

    public ContraChargeKind Kind { get; set; }
    public DateOnly IncurredOn { get; set; }

    public decimal Quantity { get; set; }
    public string? Uom { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }

    public Guid? MaterialIssueId { get; set; }
    public Guid? PlantAllocationId { get; set; }
    public Guid? WorkOrderId { get; set; }

    public bool IsAgreed { get; set; }
    public bool IsDisputed { get; set; }
    public string? DisputeNote { get; set; }
    public bool IsRecovered { get; set; }
    public string? EvidenceUrl { get; set; }
}

/// <summary>
/// A formal change to the contract. Every change routes through one of these, not through a
/// conversation on site — which is the single most valuable discipline in construction.
/// </summary>
public class VariationOrder : BaseEntity
{
    public string VariationNumber { get; set; } = string.Empty;

    public Guid ConstructionProjectId { get; set; }
    public Guid? SubcontractId { get; set; }
    public Guid? ClientBuildContractId { get; set; }
    public Guid? WbsNodeId { get; set; }

    public VariationOrigin Origin { get; set; }
    public VariationStatus Status { get; set; } = VariationStatus.Proposed;

    public string Title { get; set; } = string.Empty;
    public string? Justification { get; set; }

    public DateOnly RaisedOn { get; set; }
    public Guid? RaisedByUserId { get; set; }
    public Guid? SiteInstructionId { get; set; }

    // ── Impact ───────────────────────────────────────────────────────────────

    public decimal AdditionAmount { get; set; }
    public decimal OmissionAmount { get; set; }

    /// <summary>Addition less omission. Can be negative, and often is on a value-engineering variation.</summary>
    public decimal NetAmount { get; set; }

    /// <summary>Days added to the programme. The half of a variation everyone forgets to agree.</summary>
    public int TimeImpactDays { get; set; }

    public decimal RevisedContractValue { get; set; }

    public DateOnly? QuotedOn { get; set; }
    public DateOnly? ApprovedOn { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public Guid? ApprovedByPartyId { get; set; }

    /// <summary>The client signed for it. Without this a variation is not collectable.</summary>
    public bool ClientApproved { get; set; }
    public Guid? SignatureSessionId { get; set; }

    public string? RejectionReason { get; set; }
    public bool IsMeasured { get; set; }
    public string? DocumentUrl { get; set; }

    public ICollection<VariationLine> Lines { get; set; } = [];
}

public class VariationLine : BaseEntity
{
    public Guid VariationOrderId { get; set; }
    public VariationOrder? Variation { get; set; }

    public Guid? BoqLineId { get; set; }
    public string? Uom { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Work removed from the contract rather than added to it.</summary>
    public bool IsOmission { get; set; }

    public Guid? RateAnalysisId { get; set; }

    /// <summary>Priced at a new rate because no comparable BOQ rate exists. Needs agreement.</summary>
    public bool IsNewRate { get; set; }

    public int SortOrder { get; set; }
}

/// <summary>A written instruction on site, numbered and acknowledged, and the origin of most variations.</summary>
public class SiteInstruction : BaseEntity
{
    public string InstructionNumber { get; set; } = string.Empty;

    public Guid ConstructionProjectId { get; set; }
    public Guid? SubcontractId { get; set; }
    public Guid? ContractorId { get; set; }

    public DateOnly IssuedOn { get; set; }
    public Guid IssuedByUserId { get; set; }
    public string Instruction { get; set; } = string.Empty;
    public string? Location { get; set; }
    public DateOnly? ComplyBy { get; set; }

    public bool IsAcknowledged { get; set; }
    public DateOnly? AcknowledgedOn { get; set; }

    /// <summary>Flagged when it changes scope, so the variation is raised rather than absorbed silently.</summary>
    public bool HasCostImpact { get; set; }
    public bool HasTimeImpact { get; set; }
    public Guid? VariationOrderId { get; set; }

    public string? PhotoUrls { get; set; }
    public string? DocumentUrl { get; set; }
    public bool IsComplied { get; set; }
}

/// <summary>Something that delayed the job, recorded when it happened rather than reconstructed later.</summary>
public class DelayEvent : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid ConstructionProjectId { get; set; }
    public Guid? SubcontractId { get; set; }
    public Guid? ProgrammeActivityId { get; set; }

    /// <summary>"Weather", "ClientDelay", "DesignChange", "MaterialShortage", "LabourShortage",
    /// "Statutory", "SiteAccess", "Payment", "Force Majeure", "ContractorFault".</summary>
    public string Cause { get; set; } = string.Empty;

    public DateOnly StartedOn { get; set; }
    public DateOnly? EndedOn { get; set; }
    public int DelayDays { get; set; }

    /// <summary>Whether it entitles anybody to more time or more money. The contractual question.</summary>
    public bool IsExcusable { get; set; }
    public bool IsCompensable { get; set; }
    public CostBearer ResponsibleParty { get; set; } = CostBearer.Contractor;

    public decimal? CostImpact { get; set; }
    public bool AffectsCriticalPath { get; set; }
    public Guid? ExtensionOfTimeId { get; set; }
    public string? EvidenceUrl { get; set; }
    public bool IsNotified { get; set; }
    public DateOnly? NotifiedOn { get; set; }
}

public class ExtensionOfTime : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid ConstructionProjectId { get; set; }
    public Guid? SubcontractId { get; set; }
    public Guid? ClientBuildContractId { get; set; }

    public DateOnly ClaimedOn { get; set; }
    public int DaysClaimed { get; set; }
    public int DaysGranted { get; set; }
    public string? Grounds { get; set; }

    public DateOnly OriginalCompletionDate { get; set; }
    public DateOnly? RevisedCompletionDate { get; set; }

    /// <summary>"Claimed", "UnderAssessment", "Granted", "PartiallyGranted", "Rejected".</summary>
    public string Status { get; set; } = "Claimed";

    public decimal? ProlongationCost { get; set; }
    public bool ProlongationCostGranted { get; set; }
    public Guid? AssessedByUserId { get; set; }
    public DateOnly? DecidedOn { get; set; }
    public string? DecisionNote { get; set; }
    public string? DocumentUrl { get; set; }
}

/// <summary>Labour on site, direct or through a contractor, costed to a WBS element.</summary>
public class LabourRecord : BaseEntity
{
    public Guid ConstructionProjectId { get; set; }
    public Guid? WbsNodeId { get; set; }
    public Guid? SubcontractId { get; set; }
    public Guid? ContractorId { get; set; }

    public DateOnly WorkDate { get; set; }

    /// <summary>"Mason", "Helper", "Carpenter", "Steelfixer", "Electrician", "Plumber", "Painter", "Operator".</summary>
    public string Trade { get; set; } = string.Empty;

    /// <summary>"Direct", "Contractor", "DailyWage".</summary>
    public string LabourType { get; set; } = "Contractor";

    public int HeadCount { get; set; }
    public decimal Hours { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal DailyRate { get; set; }
    public decimal TotalCost { get; set; }

    public Guid? EmployeeId { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public string? WorkDescription { get; set; }

    /// <summary>Counted at the gate rather than taken from a muster roll. The number that gets paid.</summary>
    public bool FromGateAttendance { get; set; }
}

public class PlantItem : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string AssetCode { get; set; } = string.Empty;

    /// <summary>"Excavator", "Crane", "Mixer", "Hoist", "Compactor", "Generator", "Scaffolding", "Pump".</summary>
    public string PlantType { get; set; } = string.Empty;

    /// <summary>"Owned" or "Hired". Their costs behave completely differently.</summary>
    public string Ownership { get; set; } = "Owned";

    public Guid? SupplierPartyId { get; set; }
    public string? Make { get; set; }
    public string? RegistrationNumber { get; set; }
    public decimal? Capacity { get; set; }

    public decimal HourlyRate { get; set; }
    public decimal DailyRate { get; set; }
    public decimal MonthlyRate { get; set; }
    public decimal? FuelConsumptionPerHour { get; set; }

    public decimal TotalHoursRun { get; set; }
    public decimal UtilisationPercent { get; set; }
    public DateOnly? NextServiceDue { get; set; }

    /// <summary>"Available", "Deployed", "UnderMaintenance", "Breakdown", "Returned".</summary>
    public string Status { get; set; } = "Available";

}

public class PlantAllocation : BaseEntity
{
    public Guid PlantItemId { get; set; }
    public Guid ConstructionProjectId { get; set; }
    public Guid? WbsNodeId { get; set; }
    public Guid? SubcontractId { get; set; }

    public DateOnly FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public decimal HoursUsed { get; set; }
    public decimal IdleHours { get; set; }
    public decimal Rate { get; set; }
    public decimal Cost { get; set; }
    public decimal FuelCost { get; set; }
    public Guid? OperatorEmployeeId { get; set; }

    /// <summary>Hired to a subcontractor, so it becomes a contra-charge against their claim.</summary>
    public bool IsChargeable { get; set; }
    public Guid? ContraChargeId { get; set; }
}

/// <summary>Material the site asks for, against the bill and the programme.</summary>
public class MaterialRequisition : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid ConstructionProjectId { get; set; }
    public Guid? WbsNodeId { get; set; }
    public Guid? BoqLineId { get; set; }

    public DateOnly RequestedOn { get; set; }
    public DateOnly RequiredBy { get; set; }
    public Guid RequestedByUserId { get; set; }

    /// <summary>"Draft", "Submitted", "Approved", "Ordered", "PartiallyReceived", "Received", "Rejected".</summary>
    public string Status { get; set; } = "Draft";

    public Guid? ApprovalRequestId { get; set; }

    /// <summary>Handed to Procurement rather than duplicated here — Real Estate only adds the coding.</summary>
    public Guid? PurchaseRequisitionId { get; set; }
    public Guid? PurchaseOrderId { get; set; }

    public decimal EstimatedValue { get; set; }
    public bool IsUrgent { get; set; }
    public string? Note { get; set; }

    public ICollection<MaterialRequisitionLine> Lines { get; set; } = [];
}

public class MaterialRequisitionLine : BaseEntity
{
    public Guid MaterialRequisitionId { get; set; }
    public MaterialRequisition? Requisition { get; set; }

    public Guid? ItemId { get; set; }
    public string Uom { get; set; } = string.Empty;
    public decimal RequestedQuantity { get; set; }
    public decimal ApprovedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal EstimatedRate { get; set; }
    public Guid? WbsNodeId { get; set; }
    public Guid? BoqLineId { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>Material leaving the site store, costed to a WBS element and to a BOQ line.</summary>
public class MaterialIssue : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid ConstructionProjectId { get; set; }
    public Guid? WbsNodeId { get; set; }
    public Guid? BoqLineId { get; set; }
    public Guid? SubcontractId { get; set; }
    public Guid? WarehouseId { get; set; }

    public DateOnly IssuedOn { get; set; }
    public Guid? ItemId { get; set; }
    public string Uom { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Value { get; set; }

    public Guid? IssuedToUserId { get; set; }
    public Guid? IssuedToContractorId { get; set; }
    public Guid? ReceivedByPartyId { get; set; }

    /// <summary>Issued to a subcontractor, so it is recoverable from their next claim.</summary>
    public bool IsContraChargeable { get; set; }
    public Guid? ContraChargeId { get; set; }

    /// <summary>Sent back unused. Reverses the consumption and the wastage calculation.</summary>
    public bool IsReturn { get; set; }

    public Guid? StockMovementId { get; set; }
    public string? Note { get; set; }
}

/// <summary>What the bill says a unit of work should consume, so actual consumption can be judged.</summary>
public class MaterialConsumptionNorm : BaseEntity
{
    public Guid? ConstructionProjectId { get; set; }
    public Guid? BoqLineId { get; set; }
    public Guid? RateAnalysisId { get; set; }

    public Guid? ItemId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public string Uom { get; set; } = string.Empty;

    /// <summary>Quantity per unit of the BOQ item — bags of cement per cubic metre, say.</summary>
    public decimal NormQuantity { get; set; }

    public decimal AllowedWastagePercent { get; set; }
    public bool IsLibraryNorm { get; set; }
}

/// <summary>
/// Theoretical consumption against actual issue. Where construction money leaks, and where a
/// client build gets disputed — so it is a first-class report, not a spreadsheet somebody keeps.
/// </summary>
public class WastageRecord : BaseEntity
{
    public Guid ConstructionProjectId { get; set; }
    public Guid? WbsNodeId { get; set; }
    public Guid? BoqLineId { get; set; }
    public Guid? ItemId { get; set; }

    public string MaterialName { get; set; } = string.Empty;
    public string Uom { get; set; } = string.Empty;
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }

    public decimal ExecutedQuantity { get; set; }
    public decimal TheoreticalConsumption { get; set; }
    public decimal ActualIssued { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public decimal NetConsumed { get; set; }

    public decimal WastageQuantity { get; set; }
    public decimal WastagePercent { get; set; }
    public decimal AllowedWastagePercent { get; set; }

    /// <summary>Beyond the allowance. The line a project manager has to explain.</summary>
    public decimal ExcessWastagePercent { get; set; }

    public decimal ExcessValue { get; set; }
    public bool IsExplained { get; set; }
    public string? Explanation { get; set; }
    public bool IsRecovered { get; set; }
}

/// <summary>Material arriving at the gate, matched to the order before it is received into store.</summary>
public class SiteGateEntry : BaseEntity
{
    public Guid ConstructionProjectId { get; set; }

    public DateTime EnteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>"MaterialIn", "MaterialOut", "PlantIn", "PlantOut", "Visitor", "Labour".</summary>
    public string EntryType { get; set; } = "MaterialIn";

    public string? VehicleNumber { get; set; }
    public string? DriverName { get; set; }
    public string? SupplierName { get; set; }
    public Guid? SupplierPartyId { get; set; }

    public string? ChallanNumber { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid? GoodsReceiptId { get; set; }
    public string? MaterialDescription { get; set; }
    public decimal? Quantity { get; set; }
    public string? Uom { get; set; }

    public decimal? GrossWeight { get; set; }
    public decimal? TareWeight { get; set; }
    public decimal? NetWeight { get; set; }

    public Guid? RecordedByUserId { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsVerified { get; set; }
    public string? Discrepancy { get; set; }
}

/// <summary>A safety incident on site. Legally reportable above a severity in every market.</summary>
public class SafetyIncident : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid ConstructionProjectId { get; set; }
    public Guid? SubcontractId { get; set; }
    public Guid? ContractorId { get; set; }

    public DateTime OccurredAt { get; set; }
    public SafetySeverity Severity { get; set; }
    public string? Location { get; set; }

    public int PersonsAffected { get; set; }
    public string? InjuredPersonName { get; set; }
    public int? LostTimeDays { get; set; }

    public string? ImmediateAction { get; set; }
    public string? RootCause { get; set; }
    public string? CorrectiveAction { get; set; }
    public Guid? ReportedByUserId { get; set; }
    public Guid? InvestigatedByUserId { get; set; }

    public bool IsReportableToAuthority { get; set; }
    public bool IsReported { get; set; }
    public DateOnly? ReportedOn { get; set; }
    public string? AuthorityReference { get; set; }

    public string? PhotoUrls { get; set; }
    public decimal? Cost { get; set; }
    public bool IsClosed { get; set; }
}
