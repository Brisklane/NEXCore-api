using RealEstate.Domain.Enums;

namespace RealEstate.Application.DTOs;

// =====================================================================================
// Construction: WBS, bills of quantities, progress, certificates, subcontracts,
// variations, materials — and the turnkey client build that uses all of it.
// =====================================================================================

public class ConstructionProjectListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public Guid? ClientBuildContractId { get; set; }
    public ProjectStatus Status { get; set; }

    public DateOnly? StartDate { get; set; }
    public DateOnly? PlannedCompletionDate { get; set; }
    public DateOnly? ForecastCompletionDate { get; set; }
    public int ExtensionDaysGranted { get; set; }
    public int? SlipDays { get; set; }

    public decimal ContractValue { get; set; }
    public decimal ApprovedVariations { get; set; }
    public decimal RevisedContractValue { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public decimal BudgetCost { get; set; }
    public decimal CommittedCost { get; set; }
    public decimal ActualCost { get; set; }
    public decimal ForecastFinalCost { get; set; }
    public decimal ForecastMargin { get; set; }
    public decimal MarginPercent { get; set; }

    public decimal CertifiedValue { get; set; }
    public decimal ReceivedValue { get; set; }
    public decimal RetentionHeld { get; set; }
    public decimal PhysicalProgressPercent { get; set; }
    public decimal FinancialProgressPercent { get; set; }

    public string? ProjectManagerName { get; set; }
    public int OpenVariations { get; set; }
    public int OpenDelays { get; set; }
    public bool IsAtRisk { get; set; }
}

public class ConstructionProjectDetailDto : ConstructionProjectListItemDto
{
    public Guid? PropertyId { get; set; }
    public ProgressMethod ProgressMethod { get; set; }
    public DateOnly? ActualCompletionDate { get; set; }
    public decimal InvoicedValue { get; set; }
    public string? QuantitySurveyorName { get; set; }
    public string? SiteEngineerName { get; set; }
    public Guid? SiteWarehouseId { get; set; }
    public decimal DefaultRetentionPercent { get; set; }
    public decimal RetentionCapPercent { get; set; }
    public int DefectsPeriodMonths { get; set; }

    public List<WbsNodeDto> Wbs { get; set; } = [];
    public List<LookupDto> BillsOfQuantities { get; set; } = [];
    public List<ProgrammeActivityDto> Programme { get; set; } = [];
    public List<SubcontractListItemDto> Subcontracts { get; set; } = [];
    public List<VariationOrderListItemDto> Variations { get; set; } = [];
    public List<InterimPaymentCertificateListItemDto> Certificates { get; set; } = [];
    public CostToCompleteDto? CostToComplete { get; set; }
}

public class WbsNodeDto
{
    public Guid Id { get; set; }
    public Guid? ParentNodeId { get; set; }
    public WbsKind Kind { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Depth { get; set; }
    public int SortOrder { get; set; }
    public Guid? ProjectNodeId { get; set; }
    public string? BlockName { get; set; }

    public decimal BudgetAmount { get; set; }
    public decimal CommittedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal ForecastAmount { get; set; }
    public decimal VarianceAmount { get; set; }
    public decimal EarnedValue { get; set; }
    public decimal WeightPercent { get; set; }
    public decimal ProgressPercent { get; set; }
    public string? ResponsibleName { get; set; }
    public Guid? SubcontractId { get; set; }
    public string? SubcontractorName { get; set; }
    public List<WbsNodeDto> Children { get; set; } = [];
}

public class WbsNodeUpsertDto
{
    public Guid? Id { get; set; }
    public Guid ConstructionProjectId { get; set; }
    public Guid? ParentNodeId { get; set; }
    public WbsKind Kind { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public Guid? ProjectNodeId { get; set; }
    public decimal BudgetAmount { get; set; }
    public decimal WeightPercent { get; set; }
    public Guid? ResponsibleUserId { get; set; }
}

// ── Bill of quantities ───────────────────────────────────────────────────────

public class BillOfQuantitiesDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid ConstructionProjectId { get; set; }
    public string? ProjectName { get; set; }
    public Guid? SubcontractId { get; set; }
    public Guid? TenderId { get; set; }
    public int Version { get; set; }
    public bool IsCurrent { get; set; }
    public string BoqType { get; set; } = "Contract";

    public decimal TotalAmount { get; set; }
    public decimal ProvisionalSumsTotal { get; set; }
    public decimal ContingencyTotal { get; set; }
    public decimal MeasuredTotal { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public DateOnly? PreparedOn { get; set; }
    public string? PreparedByName { get; set; }
    public bool IsApproved { get; set; }
    public string? DocumentUrl { get; set; }
    public int LineCount { get; set; }
    public decimal OverallProgressPercent { get; set; }

    public List<BoqSectionDto> Sections { get; set; } = [];
}

public class BoqSectionDto
{
    public Guid Id { get; set; }
    public Guid? ParentSectionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal ExecutedAmount { get; set; }
    public decimal ProgressPercent { get; set; }
    public int SortOrder { get; set; }
    public Guid? WbsNodeId { get; set; }
    public List<BoqLineDto> Lines { get; set; } = [];
    public List<BoqSectionDto> Children { get; set; } = [];
}

public class BoqLineDto
{
    public Guid Id { get; set; }
    public Guid? BoqSectionId { get; set; }
    public Guid? WbsNodeId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public BoqLineKind Kind { get; set; }
    public string Uom { get; set; } = string.Empty;

    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public decimal? RemeasuredQuantity { get; set; }
    public decimal ExecutedQuantity { get; set; }
    public decimal CertifiedQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public decimal ProgressPercent { get; set; }

    public decimal BudgetCostRate { get; set; }
    public decimal ActualCost { get; set; }
    public decimal MarginPerUnit { get; set; }
    public decimal MarginPercent { get; set; }

    public Guid? RateAnalysisId { get; set; }
    public bool IsVariation { get; set; }
    public Guid? VariationOrderId { get; set; }
    public string? VariationNumber { get; set; }
    public int SortOrder { get; set; }
    public string? Note { get; set; }
}

public class BoqLineUpsertDto
{
    public Guid? Id { get; set; }
    public Guid BillOfQuantitiesId { get; set; }
    public Guid? BoqSectionId { get; set; }
    public Guid? WbsNodeId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public BoqLineKind Kind { get; set; }
    public string Uom { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal BudgetCostRate { get; set; }
    public Guid? RateAnalysisId { get; set; }
    public Guid? ItemId { get; set; }
    public int SortOrder { get; set; }
    public string? Note { get; set; }
}

public class RateAnalysisDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Uom { get; set; } = string.Empty;
    public Guid? ConstructionProjectId { get; set; }
    public decimal OutputQuantity { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal LabourCost { get; set; }
    public decimal PlantCost { get; set; }
    public decimal SubtotalCost { get; set; }
    public decimal OverheadPercent { get; set; }
    public decimal ProfitPercent { get; set; }
    public decimal FinalRate { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public DateOnly? PricedOn { get; set; }
    public bool IsLibraryItem { get; set; }
    public bool IsActive { get; set; }
    public int UsageCount { get; set; }
    public List<RateComponentDto> Components { get; set; } = [];
}

public class RateComponentDto
{
    public Guid? Id { get; set; }
    public string ComponentType { get; set; } = "Material";
    public string Description { get; set; } = string.Empty;
    public Guid? ItemId { get; set; }
    public string? Uom { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public decimal WastagePercent { get; set; }
    public int SortOrder { get; set; }
}

public class EstimateDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? ConstructionProjectId { get; set; }
    public Guid? ClientBuildContractId { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? PartyId { get; set; }
    public string? ClientName { get; set; }
    public int Version { get; set; }
    public DateOnly PreparedOn { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public bool IsExpired { get; set; }

    public decimal DirectCost { get; set; }
    public decimal OverheadPercent { get; set; }
    public decimal ProfitPercent { get; set; }
    public decimal ContingencyPercent { get; set; }
    public decimal TotalAmount { get; set; }
    public AreaDto? Area { get; set; }
    public decimal? RatePerSqFt { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public SpecificationGrade? Grade { get; set; }
    public string Status { get; set; } = "Draft";
    public string? DocumentUrl { get; set; }
    public string? Assumptions { get; set; }
    public string? Exclusions { get; set; }
    public List<EstimateLineDto> Lines { get; set; } = [];
}

public class EstimateLineDto
{
    public Guid? Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Uom { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public Guid? RateAnalysisId { get; set; }
    public Guid? WbsNodeId { get; set; }
    public int SortOrder { get; set; }
}

public class SpecificationScheduleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ConstructionProjectId { get; set; }
    public Guid? ClientBuildContractId { get; set; }
    public Guid? ProjectId { get; set; }
    public string? UnitTypeCode { get; set; }
    public SpecificationGrade Grade { get; set; }
    public int Version { get; set; }
    public bool IsFrozen { get; set; }
    public DateOnly? FrozenOn { get; set; }
    public string? ApprovedByName { get; set; }
    public string? DocumentUrl { get; set; }
    public int ItemCount { get; set; }
    public int ClientSelectableCount { get; set; }
    public int PendingSelectionCount { get; set; }
    public List<SpecificationItemDto> Items { get; set; } = [];
}

public class SpecificationItemDto
{
    public Guid? Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? Specification { get; set; }
    public string? Brand { get; set; }
    public string? ModelOrCode { get; set; }
    public decimal? AllowanceRate { get; set; }
    public string? Uom { get; set; }
    public bool IsClientSelectable { get; set; }
    public string? SelectedOption { get; set; }
    public decimal? UpgradeCost { get; set; }
    public Guid? ClientVariationId { get; set; }
    public bool IsClientSupplied { get; set; }
    public int SortOrder { get; set; }
}

// ── Programme ────────────────────────────────────────────────────────────────

public class ProgrammeActivityDto
{
    public Guid Id { get; set; }
    public Guid? WbsNodeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateOnly PlannedStart { get; set; }
    public DateOnly PlannedFinish { get; set; }
    public int DurationDays { get; set; }
    public DateOnly? BaselineStart { get; set; }
    public DateOnly? BaselineFinish { get; set; }
    public DateOnly? ActualStart { get; set; }
    public DateOnly? ActualFinish { get; set; }
    public decimal ProgressPercent { get; set; }
    public int? TotalFloatDays { get; set; }
    public bool IsCritical { get; set; }
    public bool IsMilestone { get; set; }
    public Guid? ProjectMilestoneId { get; set; }
    public string? ResponsibleName { get; set; }
    public Guid? SubcontractId { get; set; }
    public string? SubcontractorName { get; set; }

    /// <summary>Planned finish against baseline. Positive is late.</summary>
    public int? VarianceDays { get; set; }

    public bool IsBehindSchedule { get; set; }
    public int SortOrder { get; set; }
    public List<Guid> PredecessorIds { get; set; } = [];
}

// ── Progress & certificates ──────────────────────────────────────────────────

public class ProgressMeasurementDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid ConstructionProjectId { get; set; }
    public string? ProjectName { get; set; }
    public Guid? SubcontractId { get; set; }
    public string? SubcontractorName { get; set; }
    public Guid? WbsNodeId { get; set; }
    public string? WbsName { get; set; }

    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly MeasuredOn { get; set; }
    public string? MeasuredByName { get; set; }
    public decimal PeriodValue { get; set; }
    public decimal CumulativeValue { get; set; }
    public decimal ProgressPercent { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public ProgressMethod Method { get; set; }

    public bool IsCertified { get; set; }
    public string? CertifiedByName { get; set; }
    public DateOnly? CertifiedOn { get; set; }
    public Guid? InterimPaymentCertificateId { get; set; }
    public bool WasOffline { get; set; }
    public List<string> PhotoUrls { get; set; } = [];
    public string? Note { get; set; }
    public List<ProgressMeasurementLineDto> Lines { get; set; } = [];
}

public class ProgressMeasurementLineDto
{
    public Guid? Id { get; set; }
    public Guid BoqLineId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Uom { get; set; } = string.Empty;
    public decimal ContractQuantity { get; set; }
    public decimal PreviousQuantity { get; set; }
    public decimal ThisPeriodQuantity { get; set; }
    public decimal CumulativeQuantity { get; set; }
    public decimal Rate { get; set; }
    public decimal ThisPeriodValue { get; set; }
    public decimal CumulativeValue { get; set; }
    public decimal CertifiedQuantity { get; set; }
    public decimal CertifiedValue { get; set; }
    public string? MeasurementNote { get; set; }
    public string? Location { get; set; }
}

/// <summary>A batch of measurements taken on site with no signal, posted when the tablet syncs.</summary>
public class ProgressSyncBatchDto
{
    public Guid ConstructionProjectId { get; set; }
    public Guid? SubcontractId { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly MeasuredOn { get; set; }
    public List<ProgressMeasurementLineDto> Lines { get; set; } = [];
    public List<string> PhotoUrls { get; set; } = [];
    public string? Note { get; set; }
    public string? ClientReference { get; set; }
}

public class MilestoneCertificateDto
{
    public Guid Id { get; set; }
    public string CertificateNumber { get; set; } = string.Empty;
    public Guid ProjectMilestoneId { get; set; }
    public string MilestoneName { get; set; } = string.Empty;
    public string? BlockName { get; set; }
    public DateOnly CertifiedOn { get; set; }
    public string CertifiedByName { get; set; } = string.Empty;
    public string? CertifierName { get; set; }
    public string? CertifierQualification { get; set; }
    public decimal ProgressPercent { get; set; }
    public string? Observations { get; set; }
    public List<string> PhotoUrls { get; set; } = [];
    public string? DocumentUrl { get; set; }
    public bool DemandsTriggered { get; set; }
    public decimal DemandValueTriggered { get; set; }
    public int DemandCountTriggered { get; set; }
    public bool IsCountersigned { get; set; }
}

public class InterimPaymentCertificateListItemDto
{
    public Guid Id { get; set; }
    public string CertificateNumber { get; set; } = string.Empty;
    public string Direction { get; set; } = "Payable";
    public int SequenceNumber { get; set; }
    public Guid ConstructionProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public Guid? SubcontractId { get; set; }
    public string? SubcontractorName { get; set; }
    public Guid? ClientBuildContractId { get; set; }
    public string? ClientName { get; set; }

    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly IssuedOn { get; set; }
    public DateOnly? DueDate { get; set; }
    public CertificateStatus Status { get; set; }

    public decimal GrossValueToDate { get; set; }
    public decimal ThisCertificateGross { get; set; }
    public decimal RetentionThisCertificate { get; set; }
    public decimal AdvanceRecovery { get; set; }
    public decimal ContraCharges { get; set; }
    public decimal NetPayable { get; set; }
    public decimal PaidAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool IsOverdue { get; set; }
    public string? CertifiedByName { get; set; }
}

public class InterimPaymentCertificateDetailDto : InterimPaymentCertificateListItemDto
{
    public decimal WorkDoneToDate { get; set; }
    public decimal VariationsToDate { get; set; }
    public decimal MaterialsOnSite { get; set; }
    public decimal PreviouslyCertified { get; set; }
    public decimal RetentionPercent { get; set; }
    public decimal RetentionCumulative { get; set; }
    public decimal Penalties { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal NetBeforeTax { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal WithholdingTax { get; set; }

    public string? MeasuredByName { get; set; }
    public DateOnly? CertifiedOn { get; set; }
    public string? DocumentUrl { get; set; }
    public string? Note { get; set; }

    public List<IpcLineDto> Lines { get; set; } = [];
    public List<ContraChargeDto> ContraChargeLines { get; set; } = [];

    /// <summary>Every step of the arithmetic in words, which is how this document is read.</summary>
    public List<string> Workings { get; set; } = [];
}

public class IpcLineDto
{
    public Guid? Id { get; set; }
    public Guid? BoqLineId { get; set; }
    public Guid? VariationOrderId { get; set; }
    public Guid? WbsNodeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Uom { get; set; }
    public decimal ContractQuantity { get; set; }
    public decimal Rate { get; set; }
    public decimal PreviousQuantity { get; set; }
    public decimal ClaimedQuantity { get; set; }
    public decimal CertifiedQuantity { get; set; }
    public decimal CertifiedValue { get; set; }
    public decimal DisallowedValue { get; set; }
    public string? CertificationNote { get; set; }
    public int SortOrder { get; set; }
}

public class IpcCreateDto
{
    public Guid ConstructionProjectId { get; set; }
    public Guid? SubcontractId { get; set; }
    public Guid? ClientBuildContractId { get; set; }
    public string Direction { get; set; } = "Payable";
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly IssuedOn { get; set; }
    public decimal MaterialsOnSite { get; set; }
    public decimal Penalties { get; set; }
    public decimal OtherDeductions { get; set; }
    public List<IpcLineDto> Lines { get; set; } = [];
    public List<Guid> ContraChargeIds { get; set; } = [];
    public string? Note { get; set; }
    public bool DryRun { get; set; }
}

public class RetentionLedgerEntryDto
{
    public Guid Id { get; set; }
    public Guid ConstructionProjectId { get; set; }
    public string? ProjectName { get; set; }
    public Guid? SubcontractId { get; set; }
    public string? SubcontractorName { get; set; }
    public Guid? ClientBuildContractId { get; set; }
    public string Direction { get; set; } = "Payable";
    public RetentionMovement Movement { get; set; }
    public DateOnly EntryDate { get; set; }
    public Guid? InterimPaymentCertificateId { get; set; }
    public string? CertificateNumber { get; set; }
    public decimal Amount { get; set; }
    public decimal RunningBalance { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public DateOnly? DueForReleaseOn { get; set; }
    public bool IsDueForRelease { get; set; }
    public Guid? PunchListId { get; set; }
    public bool PunchListClosed { get; set; }
    public Guid? BankGuaranteeId { get; set; }
    public string? Note { get; set; }
}

public class AdvancePaymentDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid ConstructionProjectId { get; set; }
    public Guid? SubcontractId { get; set; }
    public string? SubcontractorName { get; set; }
    public Guid? ClientBuildContractId { get; set; }
    public string Direction { get; set; } = "Paid";
    public decimal Amount { get; set; }
    public decimal PercentOfContract { get; set; }
    public DateOnly PaidOn { get; set; }
    public decimal RecoveryPercent { get; set; }
    public decimal RecoveryStartsAtProgressPercent { get; set; }
    public decimal RecoveredAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public DateOnly? FullyRecoveredOn { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public Guid? BankGuaranteeId { get; set; }
    public string? GuaranteeNumber { get; set; }
    public DateOnly? GuaranteeExpiresOn { get; set; }
}

public class MaterialsOnSiteDto
{
    public Guid Id { get; set; }
    public Guid ConstructionProjectId { get; set; }
    public Guid? SubcontractId { get; set; }
    public Guid? ItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Uom { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Value { get; set; }
    public decimal AllowedPercent { get; set; }
    public decimal AllowedValue { get; set; }
    public DateOnly DeliveredOn { get; set; }
    public bool IsInsured { get; set; }
    public bool IsSecured { get; set; }
    public decimal ConsumedQuantity { get; set; }
    public decimal ReversedValue { get; set; }
    public bool IsFullyReversed { get; set; }
    public string? PhotoUrl { get; set; }
}

public class CostToCompleteDto
{
    public Guid Id { get; set; }
    public Guid ConstructionProjectId { get; set; }
    public Guid? WbsNodeId { get; set; }
    public string? WbsName { get; set; }
    public DateOnly AsOfDate { get; set; }
    public decimal BudgetCost { get; set; }
    public decimal CostIncurred { get; set; }
    public decimal CommittedNotIncurred { get; set; }
    public decimal EstimatedRemaining { get; set; }
    public decimal ForecastFinalCost { get; set; }
    public decimal VarianceToBudget { get; set; }
    public decimal PercentComplete { get; set; }
    public decimal EarnedValue { get; set; }
    public decimal CostPerformanceIndex { get; set; }
    public decimal SchedulePerformanceIndex { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string? PreparedByName { get; set; }
    public string? Assumptions { get; set; }
    public bool IsOverrunning { get; set; }
}

// ── Tender & subcontract ─────────────────────────────────────────────────────

public class TenderDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid ConstructionProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public Guid? WbsNodeId { get; set; }
    public string? PackageName { get; set; }
    public TenderStatus Status { get; set; }
    public string? Scope { get; set; }
    public decimal EstimatedValue { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public DateOnly? IssuedOn { get; set; }
    public DateTime? SubmissionDeadline { get; set; }
    public DateOnly? RequiredStartDate { get; set; }
    public DateOnly? RequiredFinishDate { get; set; }
    public decimal? EarnestMoneyDeposit { get; set; }
    public string? DocumentUrl { get; set; }
    public int BidderCount { get; set; }
    public int BidCount { get; set; }
    public Guid? AwardedBidId { get; set; }
    public DateOnly? AwardedOn { get; set; }
    public Guid? SubcontractId { get; set; }
    public string? AwardJustification { get; set; }
    public bool IsDeadlinePassed { get; set; }
    public List<TenderBidDto> Bids { get; set; } = [];
}

public class TenderBidDto
{
    public Guid Id { get; set; }
    public Guid TenderBidderId { get; set; }
    public Guid? ContractorId { get; set; }
    public string BidderName { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public decimal BidAmount { get; set; }
    public decimal? VarianceFromEstimate { get; set; }
    public decimal? VariancePercent { get; set; }
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
    public bool IsLowest { get; set; }
}

public class SubcontractListItemDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid ConstructionProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public Guid ContractorId { get; set; }
    public string ContractorName { get; set; } = string.Empty;
    public SubcontractStatus Status { get; set; }
    public ContractKind Kind { get; set; }

    public decimal ContractValue { get; set; }
    public decimal ApprovedVariations { get; set; }
    public decimal RevisedValue { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public DateOnly StartDate { get; set; }
    public DateOnly FinishDate { get; set; }
    public DateOnly? ActualFinishDate { get; set; }
    public int ExtensionDaysGranted { get; set; }
    public int? DelayDays { get; set; }

    public decimal CertifiedToDate { get; set; }
    public decimal PaidToDate { get; set; }
    public decimal RetentionHeld { get; set; }
    public decimal RetentionReleased { get; set; }
    public decimal AdvanceOutstanding { get; set; }
    public decimal ProgressPercent { get; set; }

    public bool InsuranceVerified { get; set; }
    public bool LicenceVerified { get; set; }
    public DateOnly? DefectsPeriodEndsOn { get; set; }
    public int OpenClaimCount { get; set; }
    public int OpenVariationCount { get; set; }
    public decimal UnrecoveredContraCharges { get; set; }
}

public class SubcontractDetailDto : SubcontractListItemDto
{
    public Guid? WbsNodeId { get; set; }
    public string? PackageName { get; set; }
    public Guid? TenderId { get; set; }
    public DateOnly? AwardedOn { get; set; }
    public decimal RetentionPercent { get; set; }
    public decimal RetentionCapPercent { get; set; }
    public decimal AdvancePercent { get; set; }
    public decimal AdvancePaid { get; set; }
    public decimal AdvanceRecovered { get; set; }
    public int PaymentTermDays { get; set; }
    public decimal LiquidatedDamagesPerDay { get; set; }
    public decimal LiquidatedDamagesCapPercent { get; set; }
    public decimal LiquidatedDamagesAccrued { get; set; }
    public int DefectsPeriodMonths { get; set; }
    public Guid? BillOfQuantitiesId { get; set; }
    public string? DocumentUrl { get; set; }
    public string? TerminationReason { get; set; }

    public List<SubcontractBoqLineDto> BoqLines { get; set; } = [];
    public List<SubcontractorClaimDto> Claims { get; set; } = [];
    public List<VariationOrderListItemDto> Variations { get; set; } = [];
    public List<ContraChargeDto> ContraCharges { get; set; } = [];
    public List<RetentionLedgerEntryDto> Retention { get; set; } = [];
    public List<ContractorComplianceDto> Compliance { get; set; } = [];
}

public class SubcontractBoqLineDto
{
    public Guid? Id { get; set; }
    public Guid BoqLineId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Uom { get; set; } = string.Empty;
    public decimal AwardedQuantity { get; set; }
    public decimal AwardedRate { get; set; }
    public decimal AwardedAmount { get; set; }

    /// <summary>What we charge for the same line. The margin on the package, line by line.</summary>
    public decimal SaleRate { get; set; }
    public decimal MarginPercent { get; set; }

    public decimal ExecutedQuantity { get; set; }
    public decimal CertifiedQuantity { get; set; }
    public decimal ProgressPercent { get; set; }
}

public class SubcontractCreateDto
{
    public Guid? Id { get; set; }
    public Guid ConstructionProjectId { get; set; }
    public Guid? WbsNodeId { get; set; }
    public Guid? TenderId { get; set; }
    public Guid ContractorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ContractKind Kind { get; set; }
    public decimal ContractValue { get; set; }
    public string? CurrencyCode { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly FinishDate { get; set; }
    public decimal RetentionPercent { get; set; } = 10m;
    public decimal RetentionCapPercent { get; set; } = 5m;
    public decimal AdvancePercent { get; set; }
    public int PaymentTermDays { get; set; } = 30;
    public decimal LiquidatedDamagesPerDay { get; set; }
    public decimal LiquidatedDamagesCapPercent { get; set; }
    public int DefectsPeriodMonths { get; set; } = 12;
    public Guid? BillOfQuantitiesId { get; set; }
    public List<SubcontractBoqLineDto> BoqLines { get; set; } = [];
    public string? DocumentUrl { get; set; }
}

public class SubcontractorClaimDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid SubcontractId { get; set; }
    public string SubcontractReference { get; set; } = string.Empty;
    public Guid ContractorId { get; set; }
    public string ContractorName { get; set; } = string.Empty;
    public int SequenceNumber { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly SubmittedOn { get; set; }

    public decimal ClaimedGross { get; set; }
    public decimal CertifiedGross { get; set; }
    public decimal DisallowedAmount { get; set; }
    public decimal DisallowedPercent { get; set; }
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
    public string CurrencyCode { get; set; } = "USD";

    public CertificateStatus Status { get; set; }
    public Guid? InterimPaymentCertificateId { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateOnly? PaidOn { get; set; }
    public bool IsOverdue { get; set; }
    public string? ClaimDocumentUrl { get; set; }
    public string? DisallowanceReason { get; set; }
    public bool IsDisputed { get; set; }
    public List<ClaimCertificationDto> Certifications { get; set; } = [];
}

public class ClaimCertificationDto
{
    public Guid? Id { get; set; }
    public Guid? BoqLineId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal ClaimedQuantity { get; set; }
    public decimal CertifiedQuantity { get; set; }
    public decimal Rate { get; set; }
    public decimal ClaimedValue { get; set; }
    public decimal CertifiedValue { get; set; }
    public decimal DisallowedValue { get; set; }
    public string? Reason { get; set; }
    public string? CertifiedByName { get; set; }
}

public class ContraChargeDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid SubcontractId { get; set; }
    public string? SubcontractReference { get; set; }
    public Guid ContractorId { get; set; }
    public string ContractorName { get; set; } = string.Empty;
    public ContraChargeKind Kind { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateOnly IncurredOn { get; set; }
    public decimal Quantity { get; set; }
    public string? Uom { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public Guid? MaterialIssueId { get; set; }
    public Guid? PlantAllocationId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public bool IsAgreed { get; set; }
    public bool IsDisputed { get; set; }
    public string? DisputeNote { get; set; }
    public bool IsRecovered { get; set; }
    public string? EvidenceUrl { get; set; }
}

// ── Variations ───────────────────────────────────────────────────────────────

public class VariationOrderListItemDto
{
    public Guid Id { get; set; }
    public string VariationNumber { get; set; } = string.Empty;
    public Guid ConstructionProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public Guid? SubcontractId { get; set; }
    public string? SubcontractorName { get; set; }
    public Guid? ClientBuildContractId { get; set; }
    public string? ClientName { get; set; }

    public VariationOrigin Origin { get; set; }
    public VariationStatus Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly RaisedOn { get; set; }
    public string? RaisedByName { get; set; }

    public decimal AdditionAmount { get; set; }
    public decimal OmissionAmount { get; set; }
    public decimal NetAmount { get; set; }
    public int TimeImpactDays { get; set; }
    public decimal RevisedContractValue { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public DateOnly? QuotedOn { get; set; }
    public DateOnly? ApprovedOn { get; set; }
    public bool ClientApproved { get; set; }
    public bool IsMeasured { get; set; }
    public int DaysOpen { get; set; }
    public bool IsAwaitingApproval { get; set; }
}

public class VariationOrderDetailDto : VariationOrderListItemDto
{
    public Guid? WbsNodeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Justification { get; set; }
    public Guid? SiteInstructionId { get; set; }
    public string? SiteInstructionNumber { get; set; }
    public string? ApprovedByName { get; set; }
    public string? RejectionReason { get; set; }
    public string? DocumentUrl { get; set; }
    public Guid? SignatureSessionId { get; set; }
    public List<VariationLineDto> Lines { get; set; } = [];
}

public class VariationLineDto
{
    public Guid? Id { get; set; }
    public Guid? BoqLineId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Uom { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public bool IsOmission { get; set; }
    public Guid? RateAnalysisId { get; set; }
    public bool IsNewRate { get; set; }
    public int SortOrder { get; set; }
}

public class VariationOrderUpsertDto
{
    public Guid? Id { get; set; }
    public Guid ConstructionProjectId { get; set; }
    public Guid? SubcontractId { get; set; }
    public Guid? ClientBuildContractId { get; set; }
    public Guid? WbsNodeId { get; set; }
    public VariationOrigin Origin { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Justification { get; set; }
    public DateOnly RaisedOn { get; set; }
    public Guid? SiteInstructionId { get; set; }
    public int TimeImpactDays { get; set; }
    public List<VariationLineDto> Lines { get; set; } = [];
}

public class SiteInstructionDto
{
    public Guid Id { get; set; }
    public string InstructionNumber { get; set; } = string.Empty;
    public Guid ConstructionProjectId { get; set; }
    public string? ProjectName { get; set; }
    public Guid? SubcontractId { get; set; }
    public string? ContractorName { get; set; }
    public DateOnly IssuedOn { get; set; }
    public string IssuedByName { get; set; } = string.Empty;
    public string Instruction { get; set; } = string.Empty;
    public string? Location { get; set; }
    public DateOnly? ComplyBy { get; set; }
    public bool IsAcknowledged { get; set; }
    public DateOnly? AcknowledgedOn { get; set; }
    public bool HasCostImpact { get; set; }
    public bool HasTimeImpact { get; set; }
    public Guid? VariationOrderId { get; set; }
    public string? VariationNumber { get; set; }
    public List<string> PhotoUrls { get; set; } = [];
    public string? DocumentUrl { get; set; }
    public bool IsComplied { get; set; }
    public bool IsOverdue { get; set; }
}

public class DelayEventDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid ConstructionProjectId { get; set; }
    public string? ProjectName { get; set; }
    public Guid? SubcontractId { get; set; }
    public string? SubcontractorName { get; set; }
    public Guid? ProgrammeActivityId { get; set; }
    public string? ActivityName { get; set; }
    public string Cause { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly StartedOn { get; set; }
    public DateOnly? EndedOn { get; set; }
    public int DelayDays { get; set; }
    public bool IsExcusable { get; set; }
    public bool IsCompensable { get; set; }
    public CostBearer ResponsibleParty { get; set; }
    public decimal? CostImpact { get; set; }
    public bool AffectsCriticalPath { get; set; }
    public Guid? ExtensionOfTimeId { get; set; }
    public string? EvidenceUrl { get; set; }
    public bool IsNotified { get; set; }
    public DateOnly? NotifiedOn { get; set; }
    public bool IsOngoing { get; set; }
}

public class ExtensionOfTimeDto
{
    public Guid Id { get; set; }
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
    public string Status { get; set; } = "Claimed";
    public decimal? ProlongationCost { get; set; }
    public bool ProlongationCostGranted { get; set; }
    public string? AssessedByName { get; set; }
    public DateOnly? DecidedOn { get; set; }
    public string? DecisionNote { get; set; }
    public string? DocumentUrl { get; set; }
}

// ── Materials, labour, plant, safety ─────────────────────────────────────────

public class MaterialRequisitionDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid ConstructionProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public Guid? WbsNodeId { get; set; }
    public string? WbsName { get; set; }
    public DateOnly RequestedOn { get; set; }
    public DateOnly RequiredBy { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public Guid? PurchaseRequisitionId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public decimal EstimatedValue { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool IsUrgent { get; set; }
    public bool IsOverdue { get; set; }
    public string? Note { get; set; }
    public List<MaterialRequisitionLineDto> Lines { get; set; } = [];
}

public class MaterialRequisitionLineDto
{
    public Guid? Id { get; set; }
    public Guid? ItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Uom { get; set; } = string.Empty;
    public decimal RequestedQuantity { get; set; }
    public decimal ApprovedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal EstimatedRate { get; set; }
    public Guid? WbsNodeId { get; set; }
    public Guid? BoqLineId { get; set; }
    public int SortOrder { get; set; }
}

public class MaterialIssueDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid ConstructionProjectId { get; set; }
    public Guid? WbsNodeId { get; set; }
    public string? WbsName { get; set; }
    public Guid? BoqLineId { get; set; }
    public string? BoqItemCode { get; set; }
    public Guid? SubcontractId { get; set; }
    public string? ContractorName { get; set; }
    public DateOnly IssuedOn { get; set; }
    public Guid? ItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Uom { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Value { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string? IssuedToName { get; set; }
    public string? ReceivedByName { get; set; }
    public bool IsContraChargeable { get; set; }
    public Guid? ContraChargeId { get; set; }
    public bool IsReturn { get; set; }
    public string? Note { get; set; }
}

public class WastageRecordDto
{
    public Guid Id { get; set; }
    public Guid ConstructionProjectId { get; set; }
    public string? ProjectName { get; set; }
    public Guid? WbsNodeId { get; set; }
    public string? WbsName { get; set; }
    public Guid? BoqLineId { get; set; }
    public string? BoqItemCode { get; set; }
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
    public decimal ExcessWastagePercent { get; set; }
    public decimal ExcessValue { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool IsExplained { get; set; }
    public string? Explanation { get; set; }
    public bool IsRecovered { get; set; }
    public bool IsExcessive { get; set; }
}

public class LabourRecordDto
{
    public Guid Id { get; set; }
    public Guid ConstructionProjectId { get; set; }
    public Guid? WbsNodeId { get; set; }
    public string? WbsName { get; set; }
    public Guid? SubcontractId { get; set; }
    public string? ContractorName { get; set; }
    public DateOnly WorkDate { get; set; }
    public string Trade { get; set; } = string.Empty;
    public string LabourType { get; set; } = "Contractor";
    public int HeadCount { get; set; }
    public decimal Hours { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal DailyRate { get; set; }
    public decimal TotalCost { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string? WorkDescription { get; set; }
    public bool FromGateAttendance { get; set; }
    public string? RecordedByName { get; set; }
}

public class PlantItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AssetCode { get; set; } = string.Empty;
    public string PlantType { get; set; } = string.Empty;
    public string Ownership { get; set; } = "Owned";
    public string? SupplierName { get; set; }
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
    public string Status { get; set; } = "Available";
    public string? CurrentProjectName { get; set; }
    public bool IsActive { get; set; }
}

public class PlantAllocationDto
{
    public Guid Id { get; set; }
    public Guid PlantItemId { get; set; }
    public string PlantName { get; set; } = string.Empty;
    public Guid ConstructionProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public Guid? WbsNodeId { get; set; }
    public Guid? SubcontractId { get; set; }
    public string? ContractorName { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public decimal HoursUsed { get; set; }
    public decimal IdleHours { get; set; }
    public decimal Rate { get; set; }
    public decimal Cost { get; set; }
    public decimal FuelCost { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string? OperatorName { get; set; }
    public bool IsChargeable { get; set; }
    public Guid? ContraChargeId { get; set; }
    public decimal UtilisationPercent { get; set; }
}

public class SiteGateEntryDto
{
    public Guid Id { get; set; }
    public Guid ConstructionProjectId { get; set; }
    public DateTime EnteredAt { get; set; }
    public string EntryType { get; set; } = "MaterialIn";
    public string? VehicleNumber { get; set; }
    public string? DriverName { get; set; }
    public string? SupplierName { get; set; }
    public string? ChallanNumber { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid? GoodsReceiptId { get; set; }
    public string? MaterialDescription { get; set; }
    public decimal? Quantity { get; set; }
    public string? Uom { get; set; }
    public decimal? GrossWeight { get; set; }
    public decimal? TareWeight { get; set; }
    public decimal? NetWeight { get; set; }
    public string? RecordedByName { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsVerified { get; set; }
    public string? Discrepancy { get; set; }
}

public class SafetyIncidentDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid ConstructionProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public Guid? SubcontractId { get; set; }
    public string? ContractorName { get; set; }
    public DateTime OccurredAt { get; set; }
    public SafetySeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public int PersonsAffected { get; set; }
    public string? InjuredPersonName { get; set; }
    public int? LostTimeDays { get; set; }
    public string? ImmediateAction { get; set; }
    public string? RootCause { get; set; }
    public string? CorrectiveAction { get; set; }
    public string? ReportedByName { get; set; }
    public string? InvestigatedByName { get; set; }
    public bool IsReportableToAuthority { get; set; }
    public bool IsReported { get; set; }
    public DateOnly? ReportedOn { get; set; }
    public string? AuthorityReference { get; set; }
    public List<string> PhotoUrls { get; set; } = [];
    public decimal? Cost { get; set; }
    public bool IsClosed { get; set; }
}

// ── Client build (turnkey) ───────────────────────────────────────────────────

public class ClientBuildContractListItemDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid ClientPartyId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string? ClientPhone { get; set; }
    public string? SiteAddress { get; set; }

    public ContractKind Kind { get; set; }
    public SpecificationGrade Grade { get; set; }
    public AreaDto PlotArea { get; set; } = new();
    public AreaDto CoveredArea { get; set; } = new();
    public decimal RatePerSqFt { get; set; }

    public decimal ContractValue { get; set; }
    public decimal ApprovedVariations { get; set; }
    public decimal RevisedContractValue { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public string Status { get; set; } = "Draft";
    public DateOnly? SignedOn { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? PlannedCompletionDate { get; set; }
    public DateOnly? ForecastCompletionDate { get; set; }
    public int? SlipDays { get; set; }

    public decimal TotalDemanded { get; set; }
    public decimal TotalReceived { get; set; }
    public decimal Outstanding { get; set; }
    public decimal RetentionHeldByClient { get; set; }
    public decimal ProgressPercent { get; set; }

    public decimal BudgetCost { get; set; }
    public decimal ActualCost { get; set; }
    public decimal ForecastFinalCost { get; set; }
    public decimal ForecastMargin { get; set; }
    public decimal MarginPercent { get; set; }

    public string? ProjectManagerName { get; set; }
    public int OpenVariationCount { get; set; }
    public int PendingClientDecisions { get; set; }
    public bool IsMarginAtRisk { get; set; }
}

public class ClientBuildContractDetailDto : ClientBuildContractListItemDto
{
    public Guid? CoClientPartyId { get; set; }
    public string? CoClientName { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? LandParcelId { get; set; }
    public Guid? SourceBookingId { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? EstimateId { get; set; }
    public Guid? SpecificationScheduleId { get; set; }
    public Guid? ConstructionProjectId { get; set; }

    public decimal? FeePercent { get; set; }
    public decimal? FixedFee { get; set; }
    public decimal? GuaranteedMaximumPrice { get; set; }
    public DateOnly? ActualCompletionDate { get; set; }
    public int ExtensionDaysGranted { get; set; }
    public decimal LiquidatedDamagesPerDay { get; set; }
    public decimal LiquidatedDamagesCapPercent { get; set; }

    public Guid? PaymentPlanId { get; set; }
    public decimal AdvanceReceived { get; set; }
    public decimal RetentionPercent { get; set; }
    public decimal RetentionReleased { get; set; }

    public int DefectsPeriodMonths { get; set; }
    public Guid? HandoverId { get; set; }
    public Guid? SnagInspectionId { get; set; }
    public string? ArchitectName { get; set; }
    public string? DocumentUrl { get; set; }
    public bool ClientPortalEnabled { get; set; }
    public string? Notes { get; set; }

    public List<ContractScopeItemDto> ScopeItems { get; set; } = [];
    public List<ClientSuppliedMaterialDto> ClientSuppliedMaterials { get; set; } = [];
    public List<ClientVariationDto> Variations { get; set; } = [];
    public SpecificationScheduleDto? Specification { get; set; }
    public PaymentPlanDto? PaymentPlan { get; set; }
    public List<InterimPaymentCertificateListItemDto> Certificates { get; set; } = [];
    public List<DrawingRegisterDto> Drawings { get; set; } = [];
    public ContractCostSheetDto? CostSheet { get; set; }
    public List<ProjectMilestoneDto> Milestones { get; set; } = [];
    public List<TimelineEntryDto> Timeline { get; set; } = [];
}

public class ClientBuildContractUpsertDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ClientPartyId { get; set; }
    public PartyUpsertDto? NewClient { get; set; }
    public Guid? CoClientPartyId { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? EstimateId { get; set; }
    public Guid? OfficeId { get; set; }

    public Guid? PropertyId { get; set; }
    public Guid? LandParcelId { get; set; }
    public Guid? SourceBookingId { get; set; }
    public string? SiteAddress { get; set; }
    public AreaUnit InputAreaUnit { get; set; } = AreaUnit.SquareFeet;
    public decimal PlotArea { get; set; }
    public decimal CoveredArea { get; set; }

    public ContractKind Kind { get; set; }
    public SpecificationGrade Grade { get; set; }
    public Guid? SpecificationScheduleId { get; set; }
    public decimal RatePerSqFt { get; set; }
    public decimal? ContractValue { get; set; }
    public decimal? FeePercent { get; set; }
    public decimal? FixedFee { get; set; }
    public decimal? GuaranteedMaximumPrice { get; set; }
    public string? CurrencyCode { get; set; }

    public DateOnly? SignedOn { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? PlannedCompletionDate { get; set; }
    public decimal LiquidatedDamagesPerDay { get; set; }
    public decimal LiquidatedDamagesCapPercent { get; set; }
    public decimal RetentionPercent { get; set; }
    public int DefectsPeriodMonths { get; set; } = 12;

    public Guid? PaymentPlanTemplateId { get; set; }
    public PaymentPlanCustomDto? CustomPlan { get; set; }
    public List<ContractScopeItemDto> ScopeItems { get; set; } = [];
    public List<ClientSuppliedMaterialDto> ClientSuppliedMaterials { get; set; } = [];
    public Guid? ProjectManagerUserId { get; set; }
    public Guid? ArchitectPartyId { get; set; }
    public bool ClientPortalEnabled { get; set; } = true;
    public string? Notes { get; set; }
}

public class ContractScopeItemDto
{
    public Guid? Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsIncluded { get; set; } = true;
    public decimal? Value { get; set; }
    public string? Note { get; set; }
    public int SortOrder { get; set; }
}

public class ClientSuppliedMaterialDto
{
    public Guid? Id { get; set; }
    public Guid? ItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Uom { get; set; } = string.Empty;
    public decimal AgreedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal ConsumedQuantity { get; set; }
    public decimal OutstandingQuantity { get; set; }
    public decimal? EstimatedValue { get; set; }
    public decimal RateExclusionAmount { get; set; }
    public DateOnly? ExpectedBy { get; set; }
    public DateOnly? LastReceivedOn { get; set; }
    public bool IsDelayingWork { get; set; }
    public bool IsOverdue { get; set; }
    public string? Note { get; set; }
}

public class ClientVariationDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid ClientBuildContractId { get; set; }
    public string? ContractReference { get; set; }
    public Guid? VariationOrderId { get; set; }
    public Guid? SpecificationItemId { get; set; }
    public string? SpecificationItemName { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public VariationOrigin Origin { get; set; }
    public VariationStatus Status { get; set; }
    public DateOnly RequestedOn { get; set; }
    public string? RequestedByName { get; set; }
    public bool RaisedViaPortal { get; set; }

    public decimal QuotedAmount { get; set; }
    public decimal? OmissionCredit { get; set; }
    public decimal NetAmount { get; set; }
    public int TimeImpactDays { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public DateOnly? QuotedOn { get; set; }
    public DateOnly? QuoteValidUntil { get; set; }
    public bool QuoteExpired { get; set; }

    public bool ClientApproved { get; set; }
    public DateOnly? ApprovedOn { get; set; }
    public string? ApprovalEvidenceUrl { get; set; }
    public string? RejectionReason { get; set; }
    public bool IsExecuted { get; set; }
    public DateOnly? ExecutedOn { get; set; }
    public bool IsBilled { get; set; }
    public Guid? DemandId { get; set; }
    public List<string> BeforeAfterPhotoUrls { get; set; } = [];
}

public class ClientVariationUpsertDto
{
    public Guid? Id { get; set; }
    public Guid ClientBuildContractId { get; set; }
    public Guid? SpecificationItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public VariationOrigin Origin { get; set; } = VariationOrigin.ClientRequest;
    public DateOnly RequestedOn { get; set; }
    public Guid? RequestedByPartyId { get; set; }
    public bool RaisedViaPortal { get; set; }
    public decimal QuotedAmount { get; set; }
    public decimal? OmissionCredit { get; set; }
    public int TimeImpactDays { get; set; }
    public DateOnly? QuoteValidUntil { get; set; }
    public List<VariationLineDto> Lines { get; set; } = [];
}

public class DrawingRegisterDto
{
    public Guid Id { get; set; }
    public string DrawingNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public Guid? ClientBuildContractId { get; set; }
    public Guid? ConstructionProjectId { get; set; }
    public Guid? ProjectId { get; set; }
    public string Discipline { get; set; } = "Architectural";
    public string? Scale { get; set; }
    public string? PreparedByName { get; set; }
    public string CurrentRevision { get; set; } = "A";
    public DateOnly? CurrentRevisionDate { get; set; }
    public string Status { get; set; } = "Draft";
    public bool ClientApproved { get; set; }
    public DateOnly? ClientApprovedOn { get; set; }
    public bool IsFrozen { get; set; }
    public string? CurrentFileUrl { get; set; }
    public bool IsIssuedToSite { get; set; }
    public DateOnly? IssuedToSiteOn { get; set; }
    public int RevisionCount { get; set; }
    public List<DrawingRevisionDto> Revisions { get; set; } = [];
}

public class DrawingRevisionDto
{
    public Guid Id { get; set; }
    public string Revision { get; set; } = string.Empty;
    public DateOnly RevisionDate { get; set; }
    public string? ChangeDescription { get; set; }
    public string? FileUrl { get; set; }
    public string? IssuedByName { get; set; }
    public DateOnly? IssuedOn { get; set; }
    public bool IsSuperseded { get; set; }
    public bool HasCostImpact { get; set; }
    public Guid? VariationOrderId { get; set; }
}

/// <summary>
/// Budget, committed, actual, forecast and margin for one contract, with the erosion explained by
/// cause. The screen a project manager on a client build opens first.
/// </summary>
public class ContractCostSheetDto
{
    public Guid Id { get; set; }
    public Guid ClientBuildContractId { get; set; }
    public string? ContractReference { get; set; }
    public Guid? ConstructionProjectId { get; set; }
    public DateOnly AsOfDate { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public decimal ContractValue { get; set; }
    public decimal VariationsApproved { get; set; }
    public decimal RevisedValue { get; set; }

    public decimal BudgetCost { get; set; }
    public decimal CommittedCost { get; set; }
    public decimal ActualCost { get; set; }
    public decimal CostToComplete { get; set; }
    public decimal ForecastFinalCost { get; set; }

    public decimal BudgetMargin { get; set; }
    public decimal ForecastMargin { get; set; }
    public decimal MarginPercent { get; set; }
    public decimal MarginErosion { get; set; }

    public decimal ErosionFromVariationsAbsorbed { get; set; }
    public decimal ErosionFromWastage { get; set; }
    public decimal ErosionFromRework { get; set; }
    public decimal ErosionFromDelay { get; set; }
    public decimal ErosionFromRateIncrease { get; set; }
    public decimal ErosionOther { get; set; }

    public decimal ProgressPercent { get; set; }
    public decimal CertifiedValue { get; set; }
    public decimal CollectedValue { get; set; }
    public decimal CashPosition { get; set; }

    public string? PreparedByName { get; set; }
    public string? Commentary { get; set; }
    public List<ContractCostLineDto> Lines { get; set; } = [];
}

public class ContractCostLineDto
{
    public Guid Id { get; set; }
    public Guid? WbsNodeId { get; set; }
    public string CostHead { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BudgetAmount { get; set; }
    public decimal CommittedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal ForecastAmount { get; set; }
    public decimal VarianceAmount { get; set; }
    public decimal VariancePercent { get; set; }
    public string? VarianceExplanation { get; set; }
    public bool IsOverrunning { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>One line of the bid comparison — what each bidder priced for the same BOQ item.</summary>
public class BidComparisonLineDto
{
    public Guid? BoqLineId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Uom { get; set; }
    public decimal Quantity { get; set; }
    public decimal? EstimateRate { get; set; }

    /// <summary>One entry per bidder, in the same order as the tender's bid list.</summary>
    public List<BidComparisonCellDto> Bids { get; set; } = [];
}

public class BidComparisonCellDto
{
    public Guid TenderBidId { get; set; }
    public string BidderName { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public decimal? VariancePercent { get; set; }

    /// <summary>Far off the estimate and the other bids — usually a misread or a loaded rate.</summary>
    public bool IsOutlier { get; set; }

    public bool IsLowest { get; set; }
    public string? Note { get; set; }
}
