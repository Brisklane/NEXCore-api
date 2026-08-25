using Fitness.Domain.Enums;

namespace Fitness.Application.DTOs;

// ── Lockers ──────────────────────────────────────────────────────────────────

public class LockerBankDto
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public Guid? AreaId { get; set; }
    public int TotalLockers { get; set; }
    public int RentedCount { get; set; }
    public int FreeCount { get; set; }
    public int OutOfOrderCount { get; set; }
    public int OccupancyPercent { get; set; }
    public bool SupportsRental { get; set; }
    public bool SupportsDayUse { get; set; }
    public bool IsActive { get; set; }

    public List<LockerDto> Lockers { get; set; } = [];

    public decimal MonthlyRentalRevenue { get; set; }
    public int ExpiringThisMonth { get; set; }
}

public class LockerDto
{
    public Guid Id { get; set; }
    public Guid LockerBankId { get; set; }
    public string? BankName { get; set; }
    public Guid ClubId { get; set; }

    public string Number { get; set; } = string.Empty;
    public LockerSize Size { get; set; }
    public LockerStatus Status { get; set; }
    public string? LockType { get; set; }
    public string? KeyNumber { get; set; }

    public decimal MonthlyRate { get; set; }
    public decimal AnnualRate { get; set; }
    public decimal Deposit { get; set; }

    public string? OutOfOrderNote { get; set; }
    public DateTime? LastCleanedOn { get; set; }

    // The live tenancy, so the bank grid renders in one read.
    public Guid? CurrentAssignmentId { get; set; }
    public Guid? RentedByMemberId { get; set; }
    public string? RentedByName { get; set; }
    public DateTime? RentalEndsOn { get; set; }
    public bool RentalExpired { get; set; }
    public bool RentalExpiringSoon { get; set; }
}

public class LockerAssignmentDto
{
    public Guid Id { get; set; }
    public Guid LockerId { get; set; }
    public string? LockerNumber { get; set; }
    public string? BankName { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? MemberPhone { get; set; }
    public Guid ClubId { get; set; }

    public DateTime StartsOn { get; set; }
    public DateTime? EndsOn { get; set; }
    public DateTime? ReleasedOn { get; set; }
    public bool IsDayUse { get; set; }
    public bool IsExpired { get; set; }
    public int? DaysToExpiry { get; set; }

    public decimal Rate { get; set; }
    public decimal DepositHeld { get; set; }
    public decimal DepositReturned { get; set; }
    public bool AutoRenews { get; set; }
    public DateTime? NextBillingOn { get; set; }

    public bool ExpiryNoticeSent { get; set; }
    public bool WasReclaimed { get; set; }
    public string? KeyIssued { get; set; }
    public bool KeyReturned { get; set; }
}

public class AssignLockerDto
{
    public Guid LockerId { get; set; }
    public Guid MemberId { get; set; }
    public DateTime StartsOn { get; set; }
    public DateTime? EndsOn { get; set; }
    public bool IsDayUse { get; set; }
    public decimal? RateOverride { get; set; }
    public decimal Deposit { get; set; }
    public bool AutoRenews { get; set; }
    public string? KeyIssued { get; set; }
    public bool ChargeNow { get; set; } = true;
    public PaymentMethod? PaymentMethod { get; set; }
}

public class ReleaseLockerDto
{
    public Guid AssignmentId { get; set; }
    public bool KeyReturned { get; set; }
    public decimal DepositReturned { get; set; }
    public bool WasReclaimed { get; set; }
    public string? Note { get; set; }
}

// ── Resources ────────────────────────────────────────────────────────────────

public class BookableResourceDto
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public Guid? AreaId { get; set; }
    public string? AreaName { get; set; }

    public string Name { get; set; } = string.Empty;
    public ResourceKind Kind { get; set; }
    public int Capacity { get; set; }
    public int DisplayOrder { get; set; }
    public string? ColourHex { get; set; }

    public int SlotMinutes { get; set; }
    public int BufferMinutes { get; set; }
    public decimal MemberRate { get; set; }
    public decimal NonMemberRate { get; set; }
    public decimal PeakSurcharge { get; set; }

    public int BookingWindowDays { get; set; }
    public int MaxConcurrentBookingsPerMember { get; set; }
    public int FreeCancelHours { get; set; }
    public PolicyOutcome LateCancelOutcome { get; set; }
    public PolicyOutcome NoShowOutcome { get; set; }

    public Guid? LinkedDoorId { get; set; }
    public Guid? EquipmentAssetId { get; set; }
    public bool BookableOnline { get; set; }
    public bool IsOutOfService { get; set; }
    public string? OutOfServiceNote { get; set; }
    public bool IsActive { get; set; }

    public List<ResourceSlotRuleDto> SlotRules { get; set; } = [];

    public int BookingsThisWeek { get; set; }
    public int UtilisationPercent { get; set; }
    public decimal RevenueThisMonth { get; set; }
}

public class ResourceSlotRuleDto
{
    public Guid Id { get; set; }
    public Guid BookableResourceId { get; set; }
    public int DaysOfWeekMask { get; set; }
    public TimeSpan StartsAt { get; set; }
    public TimeSpan EndsAt { get; set; }
    public bool IsPeak { get; set; }
    public decimal? RateOverride { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class ResourceBookingDto
{
    public Guid Id { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public Guid BookableResourceId { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public ResourceKind ResourceKind { get; set; }
    public string? ColourHex { get; set; }
    public Guid ClubId { get; set; }

    public Guid? MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? MemberPhone { get; set; }
    public string? GuestName { get; set; }

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public int DurationMinutes { get; set; }

    public ResourceBookingStatus Status { get; set; }
    public BookingChannel Channel { get; set; }
    public int ParticipantCount { get; set; }
    public List<string> ParticipantNames { get; set; } = [];

    public decimal Amount { get; set; }
    public decimal PenaltyCharged { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public string? BookedByName { get; set; }
    public string? Note { get; set; }
}

public class CreateResourceBookingDto
{
    public Guid BookableResourceId { get; set; }
    public Guid ClubId { get; set; }
    public Guid? MemberId { get; set; }
    public string? GuestName { get; set; }
    public string? GuestPhone { get; set; }

    public DateTime StartsAt { get; set; }
    public int? DurationMinutes { get; set; }
    public int ParticipantCount { get; set; } = 1;
    public List<Guid> ParticipantMemberIds { get; set; } = [];

    public BookingChannel Channel { get; set; } = BookingChannel.FrontDesk;
    public PaymentMethod? PaymentMethod { get; set; }
    public Guid? CashSessionId { get; set; }
    public decimal? PriceOverride { get; set; }
    public bool OverrideConflicts { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// The court grid: one row per resource, one column per slot, for one day. Exactly the
/// interaction a leisure-centre desk expects, and nothing like a list of bookings.
/// </summary>
public class ResourceGridDto
{
    public Guid ClubId { get; set; }
    public string ClubName { get; set; } = string.Empty;
    public DateTime ForDate { get; set; }
    public ResourceKind? FilterKind { get; set; }

    public TimeSpan OpensAt { get; set; }
    public TimeSpan ClosesAt { get; set; }
    public int SlotMinutes { get; set; }

    public List<ResourceGridRowDto> Rows { get; set; } = [];

    public int TotalSlots { get; set; }
    public int BookedSlots { get; set; }
    public int UtilisationPercent { get; set; }
    public decimal RevenueToday { get; set; }
}

public class ResourceGridRowDto
{
    public Guid ResourceId { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public ResourceKind Kind { get; set; }
    public string? ColourHex { get; set; }
    public bool IsOutOfService { get; set; }
    public List<ResourceGridSlotDto> Slots { get; set; } = [];
}

public class ResourceGridSlotDto
{
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }

    /// <summary>Free, booked, blocked, peak or past — which is what colours the cell.</summary>
    public string State { get; set; } = "Free";

    public Guid? BookingId { get; set; }
    public string? BookedByName { get; set; }
    public bool IsPeak { get; set; }
    public decimal Rate { get; set; }
    public string? BlockReason { get; set; }
}

// ── Equipment ────────────────────────────────────────────────────────────────

public class EquipmentAssetDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public Guid ClubId { get; set; }
    public string? ClubName { get; set; }
    public Guid? AreaId { get; set; }
    public string? AreaName { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? AssetTag { get; set; }
    public AssetStatus Status { get; set; }

    public DateTime? PurchasedOn { get; set; }
    public decimal PurchaseCost { get; set; }
    public Guid? SupplierId { get; set; }
    public DateTime? WarrantyEndsOn { get; set; }
    public bool InWarranty { get; set; }
    public string? ServiceContractReference { get; set; }
    public DateTime? ServiceContractEndsOn { get; set; }

    public DateTime? InstalledOn { get; set; }
    public DateTime? RetiredOn { get; set; }
    public int? AgeMonths { get; set; }

    public decimal UsageHours { get; set; }
    public DateTime? UsageReadOn { get; set; }

    public DateTime? LastServicedOn { get; set; }
    public DateTime? NextServiceDueOn { get; set; }
    public bool ServiceOverdue { get; set; }

    public decimal TotalMaintenanceCost { get; set; }
    public int TotalDowntimeHours { get; set; }

    /// <summary>Purchase plus lifetime maintenance, which is the number that decides replacement.</summary>
    public decimal CostOfOwnership { get; set; }

    public string? QrCode { get; set; }
    public string? OutOfServiceNote { get; set; }
    public DateTime? OutOfServiceSince { get; set; }
    public int? DaysOutOfService { get; set; }
    public bool IsActive { get; set; }

    public int OpenWorkOrders { get; set; }
    public int FaultsLast90Days { get; set; }
}

public class SaveEquipmentDto
{
    public string? Code { get; set; }
    public Guid ClubId { get; set; }
    public Guid? AreaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? AssetTag { get; set; }
    public AssetStatus Status { get; set; } = AssetStatus.InService;
    public DateTime? PurchasedOn { get; set; }
    public decimal PurchaseCost { get; set; }
    public Guid? SupplierId { get; set; }
    public DateTime? WarrantyEndsOn { get; set; }
    public string? ServiceContractReference { get; set; }
    public DateTime? ServiceContractEndsOn { get; set; }
    public DateTime? InstalledOn { get; set; }
    public decimal UsageHours { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class MaintenanceScheduleDto
{
    public Guid Id { get; set; }
    public Guid? EquipmentAssetId { get; set; }
    public string? EquipmentName { get; set; }
    public Guid ClubId { get; set; }
    public string? AppliesToCategory { get; set; }

    public string TaskName { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public MaintenanceTrigger Trigger { get; set; }
    public int IntervalDays { get; set; }
    public decimal IntervalUsageHours { get; set; }

    public DateTime? LastPerformedOn { get; set; }
    public DateTime? NextDueOn { get; set; }
    public bool IsOverdue { get; set; }
    public int? DaysUntilDue { get; set; }

    public int EstimatedMinutes { get; set; }
    public Guid? DefaultAssigneeStaffId { get; set; }
    public string? DefaultAssigneeName { get; set; }
    public bool AutoCreateWorkOrder { get; set; }
    public bool IsActive { get; set; }
}

public class WorkOrderDto
{
    public Guid Id { get; set; }
    public string WorkOrderNumber { get; set; } = string.Empty;
    public Guid ClubId { get; set; }
    public string? ClubName { get; set; }
    public Guid? EquipmentAssetId { get; set; }
    public string? EquipmentName { get; set; }
    public string? AreaName { get; set; }

    public Guid? MaintenanceScheduleId { get; set; }
    public Guid? FaultReportId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public WorkOrderStatus Status { get; set; }
    public WorkOrderPriority Priority { get; set; }

    public DateTime RaisedOn { get; set; }
    public DateTime? DueOn { get; set; }
    public DateTime? StartedOn { get; set; }
    public DateTime? CompletedOn { get; set; }
    public bool IsOverdue { get; set; }
    public int AgeDays { get; set; }

    public Guid? AssignedStaffId { get; set; }
    public string? AssignedStaffName { get; set; }
    public string? ContractorName { get; set; }
    public string? ContractorReference { get; set; }

    public decimal LabourCost { get; set; }
    public decimal PartsCost { get; set; }
    public decimal TotalCost { get; set; }
    public string? PartsUsed { get; set; }
    public int DowntimeHours { get; set; }

    public string? ResolutionNote { get; set; }
    public List<string> PhotoUrls { get; set; } = [];
    public Guid? PurchaseRequestId { get; set; }
}

public class SaveWorkOrderDto
{
    public Guid ClubId { get; set; }
    public Guid? EquipmentAssetId { get; set; }
    public Guid? FaultReportId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Normal;
    public DateTime? DueOn { get; set; }
    public Guid? AssignedStaffId { get; set; }
    public string? ContractorName { get; set; }
    public string? ContractorReference { get; set; }
}

public class CompleteWorkOrderDto
{
    public Guid WorkOrderId { get; set; }
    public string? ResolutionNote { get; set; }
    public decimal LabourCost { get; set; }
    public decimal PartsCost { get; set; }
    public string? PartsUsed { get; set; }
    public int DowntimeHours { get; set; }

    /// <summary>Puts the machine back into service, and back into the spot map.</summary>
    public bool ReturnToService { get; set; } = true;

    public List<string> PhotoUrls { get; set; } = [];
}

public class FaultReportDto
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public Guid? EquipmentAssetId { get; set; }
    public string? EquipmentName { get; set; }
    public Guid? AreaId { get; set; }
    public string? AreaName { get; set; }

    public string FaultDescription { get; set; } = string.Empty;
    public WorkOrderPriority Severity { get; set; }
    public DateTime ReportedAt { get; set; }
    public Guid? ReportedByStaffId { get; set; }
    public string? ReportedByName { get; set; }
    public Guid? ReportedByMemberId { get; set; }
    public string? PhotoUrl { get; set; }

    public bool TakenOutOfService { get; set; }
    public bool SignPrinted { get; set; }
    public Guid? WorkOrderId { get; set; }
    public string? WorkOrderNumber { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedOn { get; set; }
}

public class ReportFaultDto
{
    public Guid ClubId { get; set; }
    public Guid? EquipmentAssetId { get; set; }

    /// <summary>Scanned from the sticker on the machine, which is how staff actually report faults.</summary>
    public string? QrCode { get; set; }

    public Guid? AreaId { get; set; }
    public string FaultDescription { get; set; } = string.Empty;
    public WorkOrderPriority Severity { get; set; } = WorkOrderPriority.Normal;
    public string? PhotoUrl { get; set; }

    /// <summary>Marks the machine unavailable immediately, including in every spot map that uses it.</summary>
    public bool TakeOutOfService { get; set; } = true;

    public bool CreateWorkOrder { get; set; } = true;
    public Guid? ReportedByMemberId { get; set; }
}

public class EquipmentUsageLogDto
{
    public Guid Id { get; set; }
    public Guid EquipmentAssetId { get; set; }
    public string? EquipmentName { get; set; }
    public DateTime ReadOn { get; set; }
    public decimal CumulativeHours { get; set; }
    public decimal HoursSinceLastRead { get; set; }
    public decimal? DistanceKm { get; set; }
    public int? SessionCount { get; set; }
    public string? Source { get; set; }
}
