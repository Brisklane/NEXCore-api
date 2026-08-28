using Nexcore.SharedKernel;
using RealEstate.Domain.Enums;

namespace RealEstate.Domain.Entities;

/// <summary>
/// A job of work on a property.
///
/// Deliberately one engine for every source: a tenant's repair request, a resident's complaint, a
/// snag fix, a defect claim inside the liability period and a planned maintenance task. They have
/// the same lifecycle — triage, authorise, assign, attend, complete, sign off, cost — and running
/// four copies of it is how a facilities team ends up with four backlogs and no idea of the total.
/// </summary>
public class WorkOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;

    public WorkOrderSource Source { get; set; } = WorkOrderSource.TenantRequest;
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Raised;
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;

    // ── What and where ───────────────────────────────────────────────────────

    public Guid? PropertyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? SocietyId { get; set; }
    public Guid? FacilityAssetId { get; set; }
    public string? LocationDetail { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>"Plumbing", "Electrical", "Carpentry", "Painting", "Hvac", "Lift", "Civil", "Cleaning".</summary>
    public string? Trade { get; set; }

    // ── Where it came from ───────────────────────────────────────────────────

    public Guid? TenancyId { get; set; }
    public Guid? ComplaintId { get; set; }
    public Guid? SnagId { get; set; }
    public Guid? DefectClaimId { get; set; }
    public Guid? PpmTaskId { get; set; }
    public Guid? InspectionFindingId { get; set; }
    public Guid? RaisedByPartyId { get; set; }
    public Guid? RaisedByUserId { get; set; }
    public DateTime RaisedAt { get; set; } = DateTime.UtcNow;

    // ── Who does it ──────────────────────────────────────────────────────────

    public Guid? AssignedToUserId { get; set; }
    public Guid? ContractorId { get; set; }
    public DateTime? AssignedAt { get; set; }

    public DateTime? AppointmentFrom { get; set; }
    public DateTime? AppointmentTo { get; set; }
    public AccessArrangement? Access { get; set; }
    public Guid? KeySetId { get; set; }
    public Guid? AccessNoticeId { get; set; }
    public bool OccupierNotified { get; set; }

    // ── SLA ──────────────────────────────────────────────────────────────────

    public DateTime? ResponseDueAt { get; set; }
    public DateTime? CompletionDueAt { get; set; }
    public bool SlaBreached { get; set; }

    // ── Authorisation ────────────────────────────────────────────────────────

    public decimal EstimatedCost { get; set; }

    /// <summary>Over the landlord's or committee's limit, so it may not start until approved.</summary>
    public bool RequiresAuthorisation { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public bool IsAuthorised { get; set; }
    public Guid? AuthorisedByUserId { get; set; }
    public DateTime? AuthorisedAt { get; set; }

    /// <summary>Emergencies proceed without waiting. Recorded as such, and reviewed after.</summary>
    public bool IsEmergencyOverride { get; set; }

    // ── Execution ────────────────────────────────────────────────────────────

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal? LabourHours { get; set; }
    public string? WorkDone { get; set; }

    public bool OccupierSignedOff { get; set; }
    public DateTime? SignedOffAt { get; set; }
    public string? SignatureUrl { get; set; }
    public int? SatisfactionRating { get; set; }

    // ── Money ────────────────────────────────────────────────────────────────

    public decimal LabourCost { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal ContractorCost { get; set; }
    public decimal TotalCost { get; set; }
    public CostBearer CostBearer { get; set; } = CostBearer.Landlord;

    /// <summary>How the cost splits when more than one party pays — a shared drain, say.</summary>
    public string? CostSplitJson { get; set; }

    public bool IsRecharged { get; set; }
    public Guid? RechargeInvoiceId { get; set; }
    public Guid? ContractorInvoiceId { get; set; }
    public Guid? OwnerStatementLineId { get; set; }

    public Guid? CancelReasonCodeId { get; set; }
    public string? Note { get; set; }

    public ICollection<WorkOrderLine> Lines { get; set; } = [];
    public ICollection<WorkOrderPhoto> Photos { get; set; } = [];
}

public class WorkOrderLine : BaseEntity
{
    public Guid WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }

    /// <summary>"Labour", "Material", "Plant", "Contractor", "Callout", "Disposal".</summary>
    public string LineType { get; set; } = "Labour";

    public decimal Quantity { get; set; } = 1m;
    public string? Uom { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Parts drawn from the site store, so the fix depletes real stock in Inventory.</summary>
    public Guid? ItemId { get; set; }
    public Guid? WarehouseId { get; set; }
    public bool StockIssued { get; set; }

    public int SortOrder { get; set; }
}

public class WorkOrderPhoto : BaseEntity
{
    public Guid WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }

    public string Url { get; set; } = string.Empty;

    /// <summary>"Before", "During", "After". The before/after pair is what justifies a recharge.</summary>
    public string Stage { get; set; } = "Before";

    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
    public Guid? CapturedByUserId { get; set; }
    public string? Caption { get; set; }
}

public class WorkOrderCost : BaseEntity
{
    public Guid WorkOrderId { get; set; }

    public CostBearer Bearer { get; set; }
    public Guid? PartyId { get; set; }
    public decimal Amount { get; set; }
    public decimal SharePercent { get; set; }
    public string? Justification { get; set; }
    public bool IsInvoiced { get; set; }
    public Guid? InvoiceId { get; set; }
}

/// <summary>A trades firm or individual we send work to.</summary>
public class Contractor : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public Guid? PartyId { get; set; }
    public Guid? SupplierId { get; set; }

    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? EmergencyPhone { get; set; }
    public string? Email { get; set; }
    public string? AddressLine { get; set; }

    public string? RegistrationNumber { get; set; }
    public string? TaxNumber { get; set; }

    public bool IsApproved { get; set; }
    public DateOnly? ApprovedOn { get; set; }
    public bool IsSuspended { get; set; }
    public string? SuspensionReason { get; set; }

    /// <summary>Available for out-of-hours emergencies. A small flag with large operational value.</summary>
    public bool IsEmergencyContractor { get; set; }

    public int ResponseSlaHours { get; set; } = 24;

    // ── Performance, recomputed from completed jobs ──────────────────────────

    public int JobsCompleted { get; set; }
    public decimal AverageCost { get; set; }
    public decimal AverageDaysToComplete { get; set; }
    public decimal? AverageRating { get; set; }
    public int ReworkCount { get; set; }
    public int SlaBreachCount { get; set; }

    public ICollection<ContractorTrade> Trades { get; set; } = [];
    public ICollection<ContractorCompliance> Compliance { get; set; } = [];
}

public class ContractorTrade : BaseEntity
{
    public Guid ContractorId { get; set; }
    public Contractor? Contractor { get; set; }

    public string Trade { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public string? Certification { get; set; }
}

public class ContractorRate : BaseEntity
{
    public Guid ContractorId { get; set; }

    public string? Trade { get; set; }

    /// <summary>"Hourly", "Daily", "PerJob", "PerUnit", "Callout".</summary>
    public string RateType { get; set; } = "Hourly";

    public decimal Rate { get; set; }
    public decimal OutOfHoursRate { get; set; }
    public decimal CalloutCharge { get; set; }
    public string? Uom { get; set; }

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
}

/// <summary>
/// An insurance policy or licence a contractor must hold. Expiry blocks new work, because sending
/// an uninsured contractor to a client's property is the agency's liability, not the contractor's.
/// </summary>
public class ContractorCompliance : BaseEntity
{
    public Guid ContractorId { get; set; }
    public Contractor? Contractor { get; set; }

    /// <summary>"PublicLiability", "EmployersLiability", "ProfessionalIndemnity", "TradeLicence",
    /// "GasSafeRegistration", "ElectricalCertification", "HealthAndSafety".</summary>
    public string ComplianceType { get; set; } = string.Empty;

    public string? ReferenceNumber { get; set; }
    public string? Provider { get; set; }
    public decimal? CoverAmount { get; set; }
    public DateOnly? IssuedOn { get; set; }
    public DateOnly ExpiresOn { get; set; }
    public string? DocumentUrl { get; set; }

    public bool IsMandatory { get; set; } = true;
    public bool BlocksAssignmentWhenExpired { get; set; } = true;
    public bool IsVerified { get; set; }
    public DateOnly? VerifiedOn { get; set; }
}

/// <summary>
/// A recurring maintenance obligation that generates work orders by itself, so a lift service is
/// never missed because the person who remembered it left.
/// </summary>
public class PpmSchedule : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public Guid? FacilityAssetId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? SocietyId { get; set; }

    public string? Trade { get; set; }
    public string? TaskDescription { get; set; }

    /// <summary>"Daily", "Weekly", "Monthly", "Quarterly", "HalfYearly", "Yearly", "Custom".</summary>
    public string Frequency { get; set; } = "Monthly";

    public int IntervalDays { get; set; }
    public DateOnly? LastCompletedOn { get; set; }
    public DateOnly NextDueOn { get; set; }

    /// <summary>Days ahead of the due date the work order is raised, so it can be scheduled.</summary>
    public int GenerateDaysBefore { get; set; } = 7;

    public Guid? ContractorId { get; set; }
    public Guid? ServiceContractId { get; set; }
    public decimal? EstimatedCost { get; set; }
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;

    /// <summary>Statutory rather than good practice, which changes how a miss is treated.</summary>
    public bool IsStatutory { get; set; }

    public string? ChecklistJson { get; set; }
}

public class PpmTask : BaseEntity
{
    public Guid PpmScheduleId { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly? CompletedOn { get; set; }

    public Guid? WorkOrderId { get; set; }
    public Guid? ContractorId { get; set; }
    public decimal? Cost { get; set; }

    /// <summary>"Pending", "Raised", "Completed", "Missed", "Skipped".</summary>
    public string Status { get; set; } = "Pending";

    public bool IsOverdue { get; set; }
    public string? CompletionNote { get; set; }
    public string? CertificateUrl { get; set; }
    public string? SkipReason { get; set; }
}

/// <summary>Plant and equipment in a building — the things a facility manager is actually responsible for.</summary>
public class FacilityAsset : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string AssetCode { get; set; } = string.Empty;
    public AssetKind Kind { get; set; }

    public Guid? PropertyId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? SocietyId { get; set; }
    public string? Location { get; set; }

    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? Capacity { get; set; }

    public DateOnly? InstalledOn { get; set; }
    public DateOnly? WarrantyExpiresOn { get; set; }
    public int? ExpectedLifeYears { get; set; }
    public decimal? PurchaseCost { get; set; }
    public decimal? ReplacementCost { get; set; }

    public Guid? ServiceContractId { get; set; }
    public DateOnly? LastServicedOn { get; set; }
    public DateOnly? NextServiceDue { get; set; }

    /// <summary>"Operational", "Faulty", "UnderRepair", "Decommissioned", "Standby".</summary>
    public string OperationalStatus { get; set; } = "Operational";

    public int BreakdownCount { get; set; }
    public decimal LifetimeMaintenanceCost { get; set; }
    public string? ManualUrl { get; set; }
    public bool IsCritical { get; set; }
}

public class AssetServiceRecord : BaseEntity
{
    public Guid FacilityAssetId { get; set; }

    public DateOnly ServicedOn { get; set; }

    /// <summary>"Routine", "Breakdown", "Overhaul", "Inspection", "Replacement".</summary>
    public string ServiceType { get; set; } = "Routine";

    public Guid? ContractorId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public string? WorkDone { get; set; }
    public string? PartsReplaced { get; set; }
    public decimal Cost { get; set; }

    public int? DowntimeHours { get; set; }
    public DateOnly? NextServiceDue { get; set; }
    public string? CertificateUrl { get; set; }
    public string? Findings { get; set; }
}

/// <summary>An annual maintenance contract, with the renewal alert that stops it lapsing unnoticed.</summary>
public class ServiceContract : BaseEntity
{
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public Guid? ContractorId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? SocietyId { get; set; }

    public string? Scope { get; set; }
    public decimal AnnualValue { get; set; }
    public RentFrequency PaymentFrequency { get; set; } = RentFrequency.Quarterly;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int NoticePeriodDays { get; set; } = 60;
    public int AlertDaysBefore { get; set; } = 90;

    /// <summary>"Comprehensive" includes parts; "NonComprehensive" is labour only. The difference is money.</summary>
    public string CoverType { get; set; } = "NonComprehensive";

    public int ResponseSlaHours { get; set; } = 24;
    public int VisitsPerYear { get; set; }
    public int VisitsCompleted { get; set; }

    public string? DocumentUrl { get; set; }
    public bool AutoRenews { get; set; }
    public bool RenewalAlertSent { get; set; }
}

/// <summary>A routine walk of a building or a common area.</summary>
public class InspectionRound : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid? PropertyId { get; set; }
    public Guid? SocietyId { get; set; }
    public Guid? ProjectId { get; set; }

    public InspectionKind Kind { get; set; } = InspectionKind.CommonArea;
    public DateTime InspectedAt { get; set; }
    public Guid? InspectorUserId { get; set; }

    public string? Areas { get; set; }
    public int? Score { get; set; }
    public int FindingCount { get; set; }
    public int OpenFindingCount { get; set; }

    public string? Summary { get; set; }
    public string? ReportUrl { get; set; }
    public DateOnly? NextRoundDue { get; set; }

    public ICollection<InspectionFinding> Findings { get; set; } = [];
}

public class InspectionFinding : BaseEntity
{
    public Guid InspectionRoundId { get; set; }
    public InspectionRound? Round { get; set; }

    public string Area { get; set; } = string.Empty;
    public TicketPriority Severity { get; set; } = TicketPriority.Normal;
    public string? PhotoUrls { get; set; }

    public Guid? WorkOrderId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public DateOnly? TargetDate { get; set; }
    public bool IsResolved { get; set; }
    public DateOnly? ResolvedOn { get; set; }

    /// <summary>Not a defect but a risk — a blocked fire exit, a missing extinguisher.</summary>
    public bool IsSafetyIssue { get; set; }
}

/// <summary>
/// A meter. Modelled as a hierarchy, because a building has a bulk supply with unit sub-meters
/// under it and the difference between the two is the common-area consumption that has to be
/// apportioned rather than absorbed.
/// </summary>
public class Meter : BaseEntity
{
    public string MeterNumber { get; set; } = string.Empty;
    public MeterKind Kind { get; set; }

    public Guid? PropertyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? SocietyId { get; set; }
    public Guid? ParentMeterId { get; set; }

    public string? Location { get; set; }
    public string? Make { get; set; }
    public string? SerialNumber { get; set; }

    /// <summary>Multiplier for a meter that reads in units of ten or a hundred. A classic billing error.</summary>
    public decimal Multiplier { get; set; } = 1m;

    public int DecimalPlaces { get; set; } = 2;

    /// <summary>The rollover point. A five-digit meter goes from 99999 back to 0 and the reading must cope.</summary>
    public decimal? MaxReading { get; set; }

    public decimal? LastReading { get; set; }
    public DateOnly? LastReadOn { get; set; }
    public decimal? AverageConsumption { get; set; }

    public Guid? UtilityTariffId { get; set; }
    public string? UtilityAccountNumber { get; set; }
    public decimal? SecurityDeposit { get; set; }

    public bool IsCommonArea { get; set; }
    public bool IsPrepaid { get; set; }
    public decimal? PrepaidBalance { get; set; }

    public bool IsFaulty { get; set; }
    public DateOnly? InstalledOn { get; set; }
    public DateOnly? ReplacedOn { get; set; }
}

/// <summary>
/// A reading, validated against history before it becomes a bill. An implausible reading caught at
/// entry costs a minute; caught after billing it costs a complaint and a credit note.
/// </summary>
public class MeterReading : BaseEntity
{
    public Guid MeterId { get; set; }

    public DateOnly ReadingDate { get; set; }
    public decimal ReadingValue { get; set; }
    public decimal? PreviousReading { get; set; }
    public decimal Consumption { get; set; }
    public int? DaysSinceLastReading { get; set; }

    public ReadingSource Source { get; set; } = ReadingSource.Manual;
    public Guid? ReadByUserId { get; set; }
    public string? PhotoUrl { get; set; }

    /// <summary>Flagged where consumption departs sharply from the average. Held back from billing.</summary>
    public bool IsImplausible { get; set; }
    public decimal? VariancePercent { get; set; }
    public bool IsVerified { get; set; }
    public Guid? VerifiedByUserId { get; set; }

    /// <summary>The meter rolled over, so consumption is computed across the wrap.</summary>
    public bool RolledOver { get; set; }

    public bool IsBilled { get; set; }
    public Guid? UtilityBillId { get; set; }
    public string? Note { get; set; }
}

/// <summary>The rate card a meter is billed on, including slabs and the surcharges every utility adds.</summary>
public class UtilityTariff : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public MeterKind Kind { get; set; }
    public Guid? SocietyId { get; set; }
    public Guid? PropertyId { get; set; }

    public decimal RatePerUnit { get; set; }
    public decimal FixedCharge { get; set; }
    public decimal MinimumCharge { get; set; }

    /// <summary>Fuel price adjustment or equivalent, which moves monthly in most markets.</summary>
    public decimal FuelAdjustmentPerUnit { get; set; }

    public decimal TaxPercent { get; set; }
    public decimal MeterRent { get; set; }

    /// <summary>Uplift over the utility's own rate to cover distribution losses.</summary>
    public decimal AdministrativeMarkupPercent { get; set; }

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    public ICollection<UtilityTariffSlab> Slabs { get; set; } = [];
}

public class UtilityTariffSlab : BaseEntity
{
    public Guid UtilityTariffId { get; set; }
    public UtilityTariff? Tariff { get; set; }

    public decimal FromUnits { get; set; }
    public decimal? ToUnits { get; set; }
    public decimal RatePerUnit { get; set; }

    /// <summary>Slab pricing applies to the whole consumption once the band is reached, in some tariffs.</summary>
    public bool IsRetrospective { get; set; }

    public int SortOrder { get; set; }
}

/// <summary>What one meter's consumption costs the occupier for one period.</summary>
public class UtilityBill : BaseEntity
{
    public string BillNumber { get; set; } = string.Empty;

    public Guid MeterId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? PartyId { get; set; }
    public Guid? UtilityTariffId { get; set; }

    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly IssuedOn { get; set; }
    public DateOnly DueDate { get; set; }

    public decimal OpeningReading { get; set; }
    public decimal ClosingReading { get; set; }
    public decimal Consumption { get; set; }

    /// <summary>This unit's share of the bulk-versus-submeter gap.</summary>
    public decimal CommonAreaShare { get; set; }

    public decimal EnergyCharge { get; set; }
    public decimal FixedCharge { get; set; }
    public decimal FuelAdjustment { get; set; }
    public decimal MarkupAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }

    public InstalmentStatus Status { get; set; } = InstalmentStatus.Due;
    public Guid? MaintenanceBillId { get; set; }
    public bool IsEstimated { get; set; }
    public bool IsDisputed { get; set; }

    public ICollection<UtilityBillLine> Lines { get; set; } = [];
}

public class UtilityBillLine : BaseEntity
{
    public Guid UtilityBillId { get; set; }
    public UtilityBill? Bill { get; set; }

    public decimal Units { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// Generator fuel bought and burnt. Reconciling this against what was recovered from residents is
/// where a society either controls its costs or quietly loses money every month.
/// </summary>
public class FuelLog : BaseEntity
{
    public Guid? SocietyId { get; set; }
    public Guid? FacilityAssetId { get; set; }

    public DateOnly LogDate { get; set; }

    /// <summary>"Purchase", "Consumption", "Adjustment".</summary>
    public string EntryType { get; set; } = "Consumption";

    public decimal Litres { get; set; }
    public decimal? RatePerLitre { get; set; }
    public decimal Amount { get; set; }

    public decimal? RunHours { get; set; }
    public decimal? OpeningStock { get; set; }
    public decimal? ClosingStock { get; set; }

    /// <summary>Litres per running hour. Departures from it are the first sign of a leak or a theft.</summary>
    public decimal? ConsumptionPerHour { get; set; }

    public Guid? SupplierPartyId { get; set; }
    public string? InvoiceReference { get; set; }
    public decimal? RecoveredAmount { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public string? Note { get; set; }
}

public class ParkingSlot : BaseEntity
{
    public string SlotNumber { get; set; } = string.Empty;

    public Guid? PropertyId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? SocietyId { get; set; }

    public string? Level { get; set; }
    public string? Zone { get; set; }

    /// <summary>"Car", "Bike", "Suv", "Visitor", "Disabled", "Ev", "Loading".</summary>
    public string SlotType { get; set; } = "Car";

    public bool IsCovered { get; set; }
    public bool HasEvCharger { get; set; }
    public decimal? AreaSqFt { get; set; }

    /// <summary>Sold with a unit rather than allotted. Then it is inventory, not a facility.</summary>
    public bool IsSaleable { get; set; }
    public Guid? SoldWithUnitId { get; set; }

    public bool IsAllotted { get; set; }
    public Guid? AllottedToUnitId { get; set; }
    public decimal MonthlyRent { get; set; }

    public bool IsVisitorParking { get; set; }
}

public class ParkingAllotment : BaseEntity
{
    public Guid ParkingSlotId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? PartyId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? ResidentVehicleId { get; set; }

    public DateOnly FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public decimal MonthlyCharge { get; set; }

    /// <summary>Included in the unit's price or rent, so it is not billed again.</summary>
    public bool IsIncludedInRent { get; set; }

    public string? Note { get; set; }
}
