using RealEstate.Domain.Enums;

namespace RealEstate.Application.DTOs;

// =====================================================================================
// Work orders, contractors, planned maintenance, assets, meters and utility billing.
// =====================================================================================

public class WorkOrderListItemDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public WorkOrderSource Source { get; set; }
    public WorkOrderStatus Status { get; set; }
    public TicketPriority Priority { get; set; }

    public Guid? PropertyId { get; set; }
    public string? AddressOneLine { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitLabel { get; set; }
    public string? SocietyName { get; set; }
    public string? LocationDetail { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Trade { get; set; }
    public DateTime RaisedAt { get; set; }
    public string? RaisedByName { get; set; }
    public string? AssignedToName { get; set; }
    public string? ContractorName { get; set; }

    public DateTime? AppointmentFrom { get; set; }
    public DateTime? AppointmentTo { get; set; }
    public DateTime? ResponseDueAt { get; set; }
    public DateTime? CompletionDueAt { get; set; }
    public bool SlaBreached { get; set; }
    public int? HoursToSla { get; set; }

    public decimal EstimatedCost { get; set; }
    public decimal TotalCost { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public CostBearer CostBearer { get; set; }
    public bool RequiresAuthorisation { get; set; }
    public bool IsAuthorised { get; set; }
    public bool OccupierSignedOff { get; set; }
    public int? SatisfactionRating { get; set; }
    public int AgeHours { get; set; }
}

public class WorkOrderDetailDto : WorkOrderListItemDto
{
    public Guid? ProjectId { get; set; }
    public Guid? SocietyId { get; set; }
    public Guid? FacilityAssetId { get; set; }
    public string? AssetName { get; set; }
    public string Description { get; set; } = string.Empty;

    public Guid? TenancyId { get; set; }
    public Guid? ComplaintId { get; set; }
    public Guid? SnagId { get; set; }
    public Guid? DefectClaimId { get; set; }
    public Guid? PpmTaskId { get; set; }
    public Guid? InspectionFindingId { get; set; }

    public AccessArrangement? Access { get; set; }
    public Guid? KeySetId { get; set; }
    public string? KeyLabel { get; set; }
    public bool OccupierNotified { get; set; }
    public Guid? AccessNoticeId { get; set; }

    public Guid? ApprovalRequestId { get; set; }
    public string? AuthorisedByName { get; set; }
    public DateTime? AuthorisedAt { get; set; }
    public bool IsEmergencyOverride { get; set; }
    public decimal? LandlordAuthorityLimit { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal? LabourHours { get; set; }
    public string? WorkDone { get; set; }
    public DateTime? SignedOffAt { get; set; }
    public string? SignatureUrl { get; set; }

    public decimal LabourCost { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal ContractorCost { get; set; }
    public bool IsRecharged { get; set; }
    public string? Note { get; set; }

    public List<WorkOrderLineDto> Lines { get; set; } = [];
    public List<WorkOrderPhotoDto> Photos { get; set; } = [];
    public List<WorkOrderCostShareDto> CostShares { get; set; } = [];
    public List<TimelineEntryDto> Timeline { get; set; } = [];
}

public class WorkOrderLineDto
{
    public Guid? Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string LineType { get; set; } = "Labour";
    public decimal Quantity { get; set; }
    public string? Uom { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public Guid? ItemId { get; set; }
    public string? ItemName { get; set; }
    public Guid? WarehouseId { get; set; }
    public bool StockIssued { get; set; }
    public int SortOrder { get; set; }
}

public class WorkOrderPhotoDto
{
    public Guid? Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Stage { get; set; } = "Before";
    public DateTime CapturedAt { get; set; }
    public string? Caption { get; set; }
}

public class WorkOrderCostShareDto
{
    public Guid? Id { get; set; }
    public CostBearer Bearer { get; set; }
    public Guid? PartyId { get; set; }
    public string? PartyName { get; set; }
    public decimal Amount { get; set; }
    public decimal SharePercent { get; set; }
    public string? Justification { get; set; }
    public bool IsInvoiced { get; set; }
}

public class WorkOrderCreateDto
{
    public Guid? Id { get; set; }
    public WorkOrderSource Source { get; set; }
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;

    public Guid? PropertyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? SocietyId { get; set; }
    public Guid? FacilityAssetId { get; set; }
    public string? LocationDetail { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Trade { get; set; }

    public Guid? TenancyId { get; set; }
    public Guid? ComplaintId { get; set; }
    public Guid? SnagId { get; set; }
    public Guid? DefectClaimId { get; set; }
    public Guid? PpmTaskId { get; set; }
    public Guid? InspectionFindingId { get; set; }
    public Guid? RaisedByPartyId { get; set; }

    public Guid? AssignedToUserId { get; set; }
    public Guid? ContractorId { get; set; }
    public DateTime? AppointmentFrom { get; set; }
    public DateTime? AppointmentTo { get; set; }
    public AccessArrangement? Access { get; set; }
    public Guid? KeySetId { get; set; }

    public decimal EstimatedCost { get; set; }
    public CostBearer CostBearer { get; set; }
    public List<WorkOrderLineDto> Lines { get; set; } = [];
    public List<WorkOrderPhotoDto> Photos { get; set; } = [];

    /// <summary>A burst main at midnight proceeds without waiting for a landlord's approval.</summary>
    public bool IsEmergencyOverride { get; set; }
    public string? OverrideReason { get; set; }

    public bool NotifyOccupier { get; set; } = true;
    public bool ServeAccessNotice { get; set; }
    public string? Note { get; set; }
}

public class WorkOrderCompletionDto
{
    public Guid WorkOrderId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public decimal? LabourHours { get; set; }
    public string WorkDone { get; set; } = string.Empty;
    public List<WorkOrderLineDto> Lines { get; set; } = [];
    public List<WorkOrderPhotoDto> Photos { get; set; } = [];
    public bool OccupierSignedOff { get; set; }
    public string? SignatureUrl { get; set; }
    public int? SatisfactionRating { get; set; }
    public CostBearer CostBearer { get; set; }
    public List<WorkOrderCostShareDto> CostShares { get; set; } = [];
}

public class ContractorListItemDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? EmergencyPhone { get; set; }
    public string? Email { get; set; }
    public List<string> Trades { get; set; } = [];
    public bool IsApproved { get; set; }
    public bool IsSuspended { get; set; }
    public bool IsEmergencyContractor { get; set; }
    public int ResponseSlaHours { get; set; }

    public int JobsCompleted { get; set; }
    public int OpenJobs { get; set; }
    public decimal AverageCost { get; set; }
    public decimal AverageDaysToComplete { get; set; }
    public decimal? AverageRating { get; set; }
    public int ReworkCount { get; set; }
    public int SlaBreachCount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>An expired insurance blocks new work. Surfaced on the list, not buried in a tab.</summary>
    public bool HasExpiredCompliance { get; set; }
    public int ComplianceExpiringCount { get; set; }
}

public class ContractorDetailDto : ContractorListItemDto
{
    public Guid? PartyId { get; set; }
    public Guid? SupplierId { get; set; }
    public string? AddressLine { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TaxNumber { get; set; }
    public DateOnly? ApprovedOn { get; set; }
    public string? SuspensionReason { get; set; }
    public List<ContractorTradeDto> TradeDetails { get; set; } = [];
    public List<ContractorRateDto> Rates { get; set; } = [];
    public List<ContractorComplianceDto> Compliance { get; set; } = [];
    public List<WorkOrderListItemDto> RecentJobs { get; set; } = [];
}

public class ContractorTradeDto
{
    public Guid? Id { get; set; }
    public string Trade { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public string? Certification { get; set; }
}

public class ContractorRateDto
{
    public Guid? Id { get; set; }
    public string? Trade { get; set; }
    public string Description { get; set; } = string.Empty;
    public string RateType { get; set; } = "Hourly";
    public decimal Rate { get; set; }
    public decimal OutOfHoursRate { get; set; }
    public decimal CalloutCharge { get; set; }
    public string? Uom { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
}

public class ContractorComplianceDto
{
    public Guid? Id { get; set; }
    public string ComplianceType { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string? Provider { get; set; }
    public decimal? CoverAmount { get; set; }
    public DateOnly? IssuedOn { get; set; }
    public DateOnly ExpiresOn { get; set; }
    public int DaysToExpiry { get; set; }
    public bool IsExpired { get; set; }
    public string? DocumentUrl { get; set; }
    public bool IsMandatory { get; set; }
    public bool BlocksAssignmentWhenExpired { get; set; }
    public bool IsVerified { get; set; }
}

public class PpmScheduleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? FacilityAssetId { get; set; }
    public string? AssetName { get; set; }
    public Guid? PropertyId { get; set; }
    public string? AddressOneLine { get; set; }
    public string? SocietyName { get; set; }
    public string? Trade { get; set; }
    public string? TaskDescription { get; set; }
    public string Frequency { get; set; } = "Monthly";
    public int IntervalDays { get; set; }
    public DateOnly? LastCompletedOn { get; set; }
    public DateOnly NextDueOn { get; set; }
    public int DaysToDue { get; set; }
    public bool IsOverdue { get; set; }
    public int GenerateDaysBefore { get; set; }
    public string? ContractorName { get; set; }
    public decimal? EstimatedCost { get; set; }
    public TicketPriority Priority { get; set; }
    public bool IsStatutory { get; set; }
    public bool IsActive { get; set; }
    public int CompletedCount { get; set; }
    public int MissedCount { get; set; }
    public decimal CompliancePercent { get; set; }
}

public class PpmTaskDto
{
    public Guid Id { get; set; }
    public Guid PpmScheduleId { get; set; }
    public string ScheduleName { get; set; } = string.Empty;
    public string? AssetName { get; set; }
    public string? AddressOneLine { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly? CompletedOn { get; set; }
    public Guid? WorkOrderId { get; set; }
    public string? WorkOrderNumber { get; set; }
    public string? ContractorName { get; set; }
    public decimal? Cost { get; set; }
    public string Status { get; set; } = "Pending";
    public bool IsOverdue { get; set; }
    public bool IsStatutory { get; set; }
    public string? CompletionNote { get; set; }
    public string? CertificateUrl { get; set; }
    public string? SkipReason { get; set; }
}

public class FacilityAssetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AssetCode { get; set; } = string.Empty;
    public AssetKind Kind { get; set; }
    public Guid? PropertyId { get; set; }
    public string? AddressOneLine { get; set; }
    public string? SocietyName { get; set; }
    public string? Location { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? Capacity { get; set; }
    public DateOnly? InstalledOn { get; set; }
    public DateOnly? WarrantyExpiresOn { get; set; }
    public bool WarrantyActive { get; set; }
    public int? ExpectedLifeYears { get; set; }
    public int? AgeYears { get; set; }
    public decimal? PurchaseCost { get; set; }
    public decimal? ReplacementCost { get; set; }
    public Guid? ServiceContractId { get; set; }
    public string? ServiceContractName { get; set; }
    public DateOnly? LastServicedOn { get; set; }
    public DateOnly? NextServiceDue { get; set; }
    public bool ServiceOverdue { get; set; }
    public string OperationalStatus { get; set; } = "Operational";
    public int BreakdownCount { get; set; }
    public decimal LifetimeMaintenanceCost { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string? ManualUrl { get; set; }
    public bool IsCritical { get; set; }
    public bool IsActive { get; set; }
}

public class AssetServiceRecordDto
{
    public Guid Id { get; set; }
    public DateOnly ServicedOn { get; set; }
    public string ServiceType { get; set; } = "Routine";
    public string? ContractorName { get; set; }
    public Guid? WorkOrderId { get; set; }
    public string? WorkDone { get; set; }
    public string? PartsReplaced { get; set; }
    public decimal Cost { get; set; }
    public int? DowntimeHours { get; set; }
    public DateOnly? NextServiceDue { get; set; }
    public string? CertificateUrl { get; set; }
    public string? Findings { get; set; }
}

public class ServiceContractDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContractorName { get; set; }
    public string? AddressOneLine { get; set; }
    public string? SocietyName { get; set; }
    public string? Scope { get; set; }
    public decimal AnnualValue { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public RentFrequency PaymentFrequency { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int DaysToExpiry { get; set; }
    public bool IsExpiringSoon { get; set; }
    public int NoticePeriodDays { get; set; }
    public string CoverType { get; set; } = "NonComprehensive";
    public int ResponseSlaHours { get; set; }
    public int VisitsPerYear { get; set; }
    public int VisitsCompleted { get; set; }
    public string? DocumentUrl { get; set; }
    public bool AutoRenews { get; set; }
    public bool IsActive { get; set; }
    public int AssetCount { get; set; }
}

public class InspectionRoundDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid? PropertyId { get; set; }
    public string? AddressOneLine { get; set; }
    public string? SocietyName { get; set; }
    public InspectionKind Kind { get; set; }
    public DateTime InspectedAt { get; set; }
    public string? InspectorName { get; set; }
    public string? Areas { get; set; }
    public int? Score { get; set; }
    public int FindingCount { get; set; }
    public int OpenFindingCount { get; set; }
    public int SafetyIssueCount { get; set; }
    public string? Summary { get; set; }
    public string? ReportUrl { get; set; }
    public DateOnly? NextRoundDue { get; set; }
    public List<InspectionFindingDto> Findings { get; set; } = [];
}

public class InspectionFindingDto
{
    public Guid? Id { get; set; }
    public string Area { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketPriority Severity { get; set; }
    public List<string> PhotoUrls { get; set; } = [];
    public Guid? WorkOrderId { get; set; }
    public string? WorkOrderNumber { get; set; }
    public string? AssignedToName { get; set; }
    public DateOnly? TargetDate { get; set; }
    public bool IsResolved { get; set; }
    public DateOnly? ResolvedOn { get; set; }
    public bool IsSafetyIssue { get; set; }
    public bool IsOverdue { get; set; }
}

// ── Meters & utilities ───────────────────────────────────────────────────────

public class MeterDto
{
    public Guid Id { get; set; }
    public string MeterNumber { get; set; } = string.Empty;
    public MeterKind Kind { get; set; }
    public Guid? PropertyId { get; set; }
    public string? AddressOneLine { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitLabel { get; set; }
    public string? SocietyName { get; set; }
    public Guid? ParentMeterId { get; set; }
    public string? ParentMeterNumber { get; set; }
    public string? Location { get; set; }
    public string? Make { get; set; }
    public string? SerialNumber { get; set; }
    public decimal Multiplier { get; set; }
    public int DecimalPlaces { get; set; }
    public decimal? MaxReading { get; set; }
    public decimal? LastReading { get; set; }
    public DateOnly? LastReadOn { get; set; }
    public int? DaysSinceLastReading { get; set; }
    public decimal? AverageConsumption { get; set; }
    public Guid? UtilityTariffId { get; set; }
    public string? TariffName { get; set; }
    public string? UtilityAccountNumber { get; set; }
    public decimal? SecurityDeposit { get; set; }
    public bool IsCommonArea { get; set; }
    public bool IsPrepaid { get; set; }
    public decimal? PrepaidBalance { get; set; }
    public bool IsFaulty { get; set; }
    public bool IsActive { get; set; }
    public int SubMeterCount { get; set; }
}

public class MeterReadingDto
{
    public Guid Id { get; set; }
    public Guid MeterId { get; set; }
    public string MeterNumber { get; set; } = string.Empty;
    public string? UnitLabel { get; set; }
    public MeterKind Kind { get; set; }
    public DateOnly ReadingDate { get; set; }
    public decimal ReadingValue { get; set; }
    public decimal? PreviousReading { get; set; }
    public decimal Consumption { get; set; }
    public int? DaysSinceLastReading { get; set; }
    public ReadingSource Source { get; set; }
    public string? ReadByName { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsImplausible { get; set; }
    public decimal? VariancePercent { get; set; }
    public bool IsVerified { get; set; }
    public bool RolledOver { get; set; }
    public bool IsBilled { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// A round of readings taken on a phone. Posted as one batch when the reader gets back into signal,
/// with the implausible ones flagged rather than silently billed.
/// </summary>
public class MeterReadingBatchDto
{
    public Guid? SocietyId { get; set; }
    public Guid? PropertyId { get; set; }
    public DateOnly ReadingDate { get; set; }
    public List<MeterReadingEntryDto> Readings { get; set; } = [];
    public bool WasOffline { get; set; }
}

public class MeterReadingEntryDto
{
    public Guid MeterId { get; set; }
    public decimal ReadingValue { get; set; }
    public string? PhotoUrl { get; set; }
    public ReadingSource Source { get; set; } = ReadingSource.Manual;
    public string? Note { get; set; }
    public string? ClientReference { get; set; }
}

public class MeterReadingBatchResultDto
{
    public int Accepted { get; set; }
    public int Flagged { get; set; }
    public int Rejected { get; set; }
    public decimal TotalConsumption { get; set; }
    public List<MeterReadingDto> Implausible { get; set; } = [];
    public List<string> Errors { get; set; } = [];
}

public class UtilityTariffDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public MeterKind Kind { get; set; }
    public string? SocietyName { get; set; }
    public decimal RatePerUnit { get; set; }
    public decimal FixedCharge { get; set; }
    public decimal MinimumCharge { get; set; }
    public decimal FuelAdjustmentPerUnit { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal MeterRent { get; set; }
    public decimal AdministrativeMarkupPercent { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public int MeterCount { get; set; }
    public List<UtilityTariffSlabDto> Slabs { get; set; } = [];
}

public class UtilityTariffSlabDto
{
    public Guid? Id { get; set; }
    public decimal FromUnits { get; set; }
    public decimal? ToUnits { get; set; }
    public decimal RatePerUnit { get; set; }
    public bool IsRetrospective { get; set; }
    public int SortOrder { get; set; }
}

public class UtilityBillDto
{
    public Guid Id { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public Guid MeterId { get; set; }
    public string MeterNumber { get; set; } = string.Empty;
    public MeterKind Kind { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitLabel { get; set; }
    public string? PartyName { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly IssuedOn { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal OpeningReading { get; set; }
    public decimal ClosingReading { get; set; }
    public decimal Consumption { get; set; }
    public decimal CommonAreaShare { get; set; }
    public decimal EnergyCharge { get; set; }
    public decimal FixedCharge { get; set; }
    public decimal FuelAdjustment { get; set; }
    public decimal MarkupAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public InstalmentStatus Status { get; set; }
    public bool IsEstimated { get; set; }
    public bool IsDisputed { get; set; }
    public List<UtilityBillLineDto> Lines { get; set; } = [];
}

public class UtilityBillLineDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Units { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// Bulk supply against the sum of sub-meters. The gap is common-area consumption plus loss, and
/// seeing it is how a society stops paying for a leak nobody reported.
/// </summary>
public class UtilityReconciliationDto
{
    public Guid? SocietyId { get; set; }
    public Guid? PropertyId { get; set; }
    public MeterKind Kind { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public decimal BulkConsumption { get; set; }
    public decimal SubMeterTotal { get; set; }
    public decimal Difference { get; set; }
    public decimal DifferencePercent { get; set; }
    public decimal CommonAreaConsumption { get; set; }
    public decimal UnexplainedLoss { get; set; }
    public decimal BulkInvoiceAmount { get; set; }
    public decimal RecoveredAmount { get; set; }
    public decimal ShortfallAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool IsWithinTolerance { get; set; }
    public List<MeterReadingDto> OutlierMeters { get; set; } = [];
}

public class FuelLogDto
{
    public Guid Id { get; set; }
    public Guid? SocietyId { get; set; }
    public Guid? FacilityAssetId { get; set; }
    public string? AssetName { get; set; }
    public DateOnly LogDate { get; set; }
    public string EntryType { get; set; } = "Consumption";
    public decimal Litres { get; set; }
    public decimal? RatePerLitre { get; set; }
    public decimal Amount { get; set; }
    public decimal? RunHours { get; set; }
    public decimal? OpeningStock { get; set; }
    public decimal? ClosingStock { get; set; }
    public decimal? ConsumptionPerHour { get; set; }
    public decimal? ExpectedPerHour { get; set; }
    public bool IsAnomalous { get; set; }
    public string? SupplierName { get; set; }
    public string? InvoiceReference { get; set; }
    public decimal? RecoveredAmount { get; set; }
    public string? RecordedByName { get; set; }
    public string? Note { get; set; }
}

public class ParkingSlotDto
{
    public Guid Id { get; set; }
    public string SlotNumber { get; set; } = string.Empty;
    public Guid? PropertyId { get; set; }
    public string? SocietyName { get; set; }
    public string? Level { get; set; }
    public string? Zone { get; set; }
    public string SlotType { get; set; } = "Car";
    public bool IsCovered { get; set; }
    public bool HasEvCharger { get; set; }
    public AreaDto? Area { get; set; }
    public bool IsSaleable { get; set; }
    public Guid? SoldWithUnitId { get; set; }
    public string? SoldWithUnitLabel { get; set; }
    public bool IsAllotted { get; set; }
    public Guid? AllottedToUnitId { get; set; }
    public string? AllottedToUnitLabel { get; set; }
    public string? AllottedToName { get; set; }
    public string? VehicleNumber { get; set; }
    public decimal MonthlyRent { get; set; }
    public bool IsVisitorParking { get; set; }
    public bool IsActive { get; set; }
}

public class ParkingAllotmentDto
{
    public Guid? Id { get; set; }
    public Guid ParkingSlotId { get; set; }
    public string? SlotNumber { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitLabel { get; set; }
    public Guid? PartyId { get; set; }
    public string? PartyName { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? ResidentVehicleId { get; set; }
    public string? VehicleNumber { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public decimal MonthlyCharge { get; set; }
    public bool IsIncludedInRent { get; set; }
    public bool IsActive { get; set; }
    public string? Note { get; set; }
}
