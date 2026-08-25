using Fitness.Domain.Enums;
using Nexcore.SharedKernel;

namespace Fitness.Domain.Entities;

/// <summary>A run of lockers in one place, so a member can be told where to go rather than just a number.</summary>
public class LockerBank : BaseEntity
{
    public Guid ClubId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Men's changing, women's, accessible, poolside.</summary>
    public string? Location { get; set; }
    public Guid? AreaId { get; set; }

    public int TotalLockers { get; set; }
    public int RentedCount { get; set; }
    public int OutOfOrderCount { get; set; }

    public bool SupportsRental { get; set; } = true;
    public bool SupportsDayUse { get; set; } = true;

    public ICollection<Locker> Lockers { get; set; } = [];
}

/// <summary>One locker.</summary>
public class Locker : BaseEntity
{
    public Guid LockerBankId { get; set; }
    public LockerBank? LockerBank { get; set; }

    public Guid ClubId { get; set; }

    public string Number { get; set; } = string.Empty;
    public LockerSize Size { get; set; } = LockerSize.Medium;
    public LockerStatus Status { get; set; } = LockerStatus.Free;

    /// <summary>Padlock, key, combination, electronic — which decides what is handed over.</summary>
    public string? LockType { get; set; }
    public string? KeyNumber { get; set; }

    public decimal MonthlyRate { get; set; }
    public decimal AnnualRate { get; set; }
    public decimal Deposit { get; set; }

    public Guid? CurrentAssignmentId { get; set; }

    public string? OutOfOrderNote { get; set; }
    public DateTime? LastCleanedOn { get; set; }
}

/// <summary>
/// A locker let to a member, for a term or for a day.
///
/// The reclaim ladder matters: a locker whose rental lapsed and which nobody chased is a locker
/// the club cannot sell, and every club has a row of them.
/// </summary>
public class LockerAssignment : BaseEntity
{
    public Guid LockerId { get; set; }
    public Locker? Locker { get; set; }

    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid ClubId { get; set; }

    public DateTime StartsOn { get; set; }
    public DateTime? EndsOn { get; set; }
    public DateTime? ReleasedOn { get; set; }

    /// <summary>A day locker issued at check-in and swept overnight.</summary>
    public bool IsDayUse { get; set; }

    public decimal Rate { get; set; }
    public decimal DepositHeld { get; set; }
    public decimal DepositReturned { get; set; }

    public Guid? AgreementId { get; set; }
    public Guid? InvoiceId { get; set; }

    public bool AutoRenews { get; set; }
    public DateTime? NextBillingOn { get; set; }

    /// <summary>The chase before the locker is cut open and reclaimed.</summary>
    public bool ExpiryNoticeSent { get; set; }
    public DateTime? ExpiryNoticeSentOn { get; set; }
    public bool WasReclaimed { get; set; }
    public string? ReclaimNote { get; set; }

    public string? KeyIssued { get; set; }
    public bool KeyReturned { get; set; }
}

/// <summary>
/// Something bookable that is not a class and not a person — a court, a lane, a sauna slot, a
/// reformer, the studio when it is hired out.
/// </summary>
public class BookableResource : BaseEntity
{
    public Guid ClubId { get; set; }
    public Guid? AreaId { get; set; }

    public string Name { get; set; } = string.Empty;
    public ResourceKind Kind { get; set; } = ResourceKind.SquashCourt;

    public int Capacity { get; set; } = 1;
    public int DisplayOrder { get; set; }
    public string? ColourHex { get; set; }

    /// <summary>Length of a bookable block.</summary>
    public int SlotMinutes { get; set; } = 45;
    public int BufferMinutes { get; set; }

    public decimal MemberRate { get; set; }
    public decimal NonMemberRate { get; set; }
    public decimal PeakSurcharge { get; set; }

    /// <summary>Days ahead members may book it.</summary>
    public int BookingWindowDays { get; set; } = 7;
    public int MaxConcurrentBookingsPerMember { get; set; } = 2;

    public int FreeCancelHours { get; set; } = 12;
    public PolicyOutcome LateCancelOutcome { get; set; } = PolicyOutcome.ChargeFee;
    public PolicyOutcome NoShowOutcome { get; set; } = PolicyOutcome.ChargeFee;

    /// <summary>The door that opens for a booking, when court-side access is controlled.</summary>
    public Guid? LinkedDoorId { get; set; }

    /// <summary>The machine, when the resource is a specific piece of equipment.</summary>
    public Guid? EquipmentAssetId { get; set; }

    public bool BookableOnline { get; set; } = true;
    public bool IsOutOfService { get; set; }
    public string? OutOfServiceNote { get; set; }

    public ICollection<ResourceSlotRule> SlotRules { get; set; } = [];
}

/// <summary>When a resource can be booked, and what it costs at that time.</summary>
public class ResourceSlotRule : BaseEntity
{
    public Guid BookableResourceId { get; set; }
    public BookableResource? BookableResource { get; set; }

    /// <summary>Bit flags, Sunday = 1 … Saturday = 64.</summary>
    public int DaysOfWeekMask { get; set; } = 127;

    public TimeSpan StartsAt { get; set; }
    public TimeSpan EndsAt { get; set; }

    /// <summary>Peak windows carry the surcharge and often a shorter booking horizon.</summary>
    public bool IsPeak { get; set; }

    public decimal? RateOverride { get; set; }

    /// <summary>Blocked out entirely — club-night, coaching, maintenance.</summary>
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }

    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

/// <summary>One booking of a resource.</summary>
public class ResourceBooking : BaseEntity
{
    public string BookingNumber { get; set; } = string.Empty;

    public Guid BookableResourceId { get; set; }
    public BookableResource? BookableResource { get; set; }

    public Guid ClubId { get; set; }

    public Guid? MemberId { get; set; }
    public Member? Member { get; set; }

    public string? GuestName { get; set; }
    public string? GuestPhone { get; set; }

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }

    public ResourceBookingStatus Status { get; set; } = ResourceBookingStatus.Booked;
    public BookingChannel Channel { get; set; } = BookingChannel.FrontDesk;

    public int ParticipantCount { get; set; } = 1;

    /// <summary>Who else is playing, so a court booking can be attributed to everyone on it.</summary>
    public string? ParticipantMemberIds { get; set; }

    public decimal Amount { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid? PaymentId { get; set; }
    public decimal PenaltyCharged { get; set; }

    public DateTime? CheckedInAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    public Guid? BookedByStaffId { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// A piece of equipment, with everything a service call needs to start from something useful.
///
/// The QR sticker is the point: staff on the floor report a fault by scanning the machine, and
/// the machine comes out of service in the same moment — including in the spot map, so nobody
/// books bike 14 while it is broken.
/// </summary>
public class EquipmentAsset : BaseEntity
{
    public Guid ClubId { get; set; }
    public Guid? AreaId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Cardio, resistance, free weight, functional, studio, pool, ancillary.</summary>
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
    public DateTime? RetiredOn { get; set; }
    public decimal? DisposalValue { get; set; }

    /// <summary>Hours the machine reports, where it does — the honest basis for replacement planning.</summary>
    public decimal UsageHours { get; set; }
    public DateTime? UsageReadOn { get; set; }

    public DateTime? LastServicedOn { get; set; }
    public DateTime? NextServiceDueOn { get; set; }

    /// <summary>Lifetime maintenance spend, so cost of ownership is one read.</summary>
    public decimal TotalMaintenanceCost { get; set; }
    public int TotalDowntimeHours { get; set; }

    /// <summary>Encoded into the sticker on the machine.</summary>
    public string? QrCode { get; set; }

    public string? OutOfServiceNote { get; set; }
    public DateTime? OutOfServiceSince { get; set; }
}

/// <summary>Preventive maintenance: do this every N days, or every N hours of use.</summary>
public class MaintenanceSchedule : BaseEntity
{
    public Guid? EquipmentAssetId { get; set; }
    public EquipmentAsset? EquipmentAsset { get; set; }

    public Guid ClubId { get; set; }

    /// <summary>Applies to every asset in a category, rather than to one machine.</summary>
    public string? AppliesToCategory { get; set; }

    public string TaskName { get; set; } = string.Empty;
    public string? Instructions { get; set; }

    public MaintenanceTrigger Trigger { get; set; } = MaintenanceTrigger.Interval;
    public int IntervalDays { get; set; }
    public decimal IntervalUsageHours { get; set; }

    public DateTime? LastPerformedOn { get; set; }
    public DateTime? NextDueOn { get; set; }

    public int EstimatedMinutes { get; set; }
    public Guid? DefaultAssigneeStaffId { get; set; }

    /// <summary>Raises the work order automatically rather than waiting for someone to notice.</summary>
    public bool AutoCreateWorkOrder { get; set; } = true;
}

/// <summary>A job on a piece of equipment or on the building, and what it cost.</summary>
public class WorkOrder : BaseEntity
{
    public string WorkOrderNumber { get; set; } = string.Empty;

    public Guid ClubId { get; set; }
    public Guid? EquipmentAssetId { get; set; }
    public EquipmentAsset? EquipmentAsset { get; set; }

    public Guid? MaintenanceScheduleId { get; set; }
    public Guid? FaultReportId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }

    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Open;
    public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Normal;

    public DateTime RaisedOn { get; set; } = DateTime.UtcNow;
    public DateTime? DueOn { get; set; }
    public DateTime? StartedOn { get; set; }
    public DateTime? CompletedOn { get; set; }

    public Guid? AssignedStaffId { get; set; }
    public string? ContractorName { get; set; }
    public string? ContractorReference { get; set; }

    public decimal LabourCost { get; set; }
    public decimal PartsCost { get; set; }
    public decimal TotalCost { get; set; }

    public string? PartsUsed { get; set; }

    /// <summary>Hours the machine was unavailable, which is what utilisation reporting needs.</summary>
    public int DowntimeHours { get; set; }

    public string? ResolutionNote { get; set; }
    public string? PhotoUrls { get; set; }

    /// <summary>Purchase request raised for parts, when Procurement is installed.</summary>
    public Guid? PurchaseRequestId { get; set; }
}

/// <summary>
/// Somebody reporting that something is broken — staff on the floor, or a member through the app.
///
/// The value is in how cheap it is to report: a QR scan and two taps means faults get reported at
/// all, which is the difference between a maintenance system and an empty table.
/// </summary>
public class FaultReport : BaseEntity
{
    public Guid ClubId { get; set; }
    public Guid? EquipmentAssetId { get; set; }
    public Guid? AreaId { get; set; }

    public string FaultDescription { get; set; } = string.Empty;
    public WorkOrderPriority Severity { get; set; } = WorkOrderPriority.Normal;

    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
    public Guid? ReportedByStaffId { get; set; }
    public Guid? ReportedByMemberId { get; set; }

    public string? PhotoUrl { get; set; }

    /// <summary>The machine was taken out of service on the spot.</summary>
    public bool TakenOutOfService { get; set; }

    /// <summary>An out-of-order notice was printed for the machine.</summary>
    public bool SignPrinted { get; set; }

    public Guid? WorkOrderId { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedOn { get; set; }
}

/// <summary>A periodic usage-hours read from a cardio machine, feeding maintenance and replacement planning.</summary>
public class EquipmentUsageLog : BaseEntity
{
    public Guid EquipmentAssetId { get; set; }
    public EquipmentAsset? EquipmentAsset { get; set; }

    public DateTime ReadOn { get; set; } = DateTime.UtcNow;

    public decimal CumulativeHours { get; set; }

    /// <summary>Hours since the previous read, so a trend does not need a self-join.</summary>
    public decimal HoursSinceLastRead { get; set; }

    public decimal? DistanceKm { get; set; }
    public int? SessionCount { get; set; }

    public string? Source { get; set; }
}
