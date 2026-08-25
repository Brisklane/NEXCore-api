using Fitness.Domain.Enums;

namespace Fitness.Application.DTOs;

// ── Club ─────────────────────────────────────────────────────────────────────

public class ClubDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public ClubType ClubType { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? PostCode { get; set; }
    public string? CountryCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? TimeZoneId { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public UnitSystem UnitSystem { get; set; }

    public Guid? WarehouseId { get; set; }
    public Guid? PosStoreId { get; set; }
    public Guid? DefaultTaxGroupId { get; set; }
    public decimal DefaultTaxPercent { get; set; }

    public int? SoftCapacity { get; set; }
    public int? HardCapacity { get; set; }
    public int CurrentOccupancy { get; set; }

    public Guid? DefaultBookingPolicyId { get; set; }
    public Guid? DefaultCancellationPolicyId { get; set; }
    public Guid? DefaultDunningPolicyId { get; set; }

    public decimal AccessBalanceThreshold { get; set; }
    public AntiPassbackMode AntiPassback { get; set; }
    public int AntiPassbackMinutes { get; set; }
    public OfflineAccessPolicy OfflinePolicy { get; set; }

    public int MinimumAge { get; set; }
    public int GuardianRequiredBelowAge { get; set; }
    public bool RequiresWaiver { get; set; }
    public bool RequiresHealthScreening { get; set; }

    public bool AllowsCrossClubVisits { get; set; }
    public decimal CrossClubVisitFee { get; set; }

    public bool IsTemporarilyClosed { get; set; }
    public string? ClosureNote { get; set; }
    public string? LogoUrl { get; set; }
    public string? ReceiptFooter { get; set; }
    public string? BrandCode { get; set; }

    public bool IsActive { get; set; }
    public string? Description { get; set; }

    public List<ClubScheduleDto> Schedules { get; set; } = [];

    // Live roll-ups, filled by the list endpoint so the picker shows state at a glance.
    public int ActiveMemberCount { get; set; }
    public int InClubNow { get; set; }
    public int ClassesToday { get; set; }
    public bool IsOpenNow { get; set; }
}

public class SaveClubDto
{
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public ClubType ClubType { get; set; } = ClubType.Gym;

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? PostCode { get; set; }
    public string? CountryCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? TimeZoneId { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public UnitSystem UnitSystem { get; set; } = UnitSystem.Metric;

    public Guid? WarehouseId { get; set; }
    public Guid? PosStoreId { get; set; }
    public Guid? DefaultTaxGroupId { get; set; }
    public decimal DefaultTaxPercent { get; set; }

    public int? SoftCapacity { get; set; }
    public int? HardCapacity { get; set; }

    public Guid? DefaultBookingPolicyId { get; set; }
    public Guid? DefaultCancellationPolicyId { get; set; }
    public Guid? DefaultDunningPolicyId { get; set; }

    public decimal AccessBalanceThreshold { get; set; }
    public AntiPassbackMode AntiPassback { get; set; } = AntiPassbackMode.Soft;
    public int AntiPassbackMinutes { get; set; } = 60;
    public OfflineAccessPolicy OfflinePolicy { get; set; } = OfflineAccessPolicy.AllowKnownActive;

    public int MinimumAge { get; set; } = 16;
    public int GuardianRequiredBelowAge { get; set; } = 14;
    public bool RequiresWaiver { get; set; } = true;
    public bool RequiresHealthScreening { get; set; } = true;

    public bool AllowsCrossClubVisits { get; set; } = true;
    public decimal CrossClubVisitFee { get; set; }

    public bool IsTemporarilyClosed { get; set; }
    public string? ClosureNote { get; set; }
    public string? LogoUrl { get; set; }
    public string? ReceiptFooter { get; set; }
    public string? BrandCode { get; set; }

    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

public class ClubScheduleDto
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public int DayOfWeek { get; set; }
    public DateTime? OverrideDate { get; set; }
    public TimeSpan OpensAt { get; set; }
    public TimeSpan ClosesAt { get; set; }
    public TimeSpan? StaffedFrom { get; set; }
    public TimeSpan? StaffedTo { get; set; }
    public bool IsClosed { get; set; }
    public string? Note { get; set; }
}

public class ClubClosureDto
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public DateTime StartsOn { get; set; }
    public DateTime EndsOn { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? MemberNotice { get; set; }
    public bool CancelsClasses { get; set; }
    public bool ExtendsAgreements { get; set; }
    public bool BlocksAccess { get; set; }

    /// <summary>Filled on save so a manager sees the blast radius before it happens.</summary>
    public int ClassesAffected { get; set; }
    public int BookingsAffected { get; set; }
}

// ── Areas & rooms ────────────────────────────────────────────────────────────

public class ClubAreaDto
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public string Name { get; set; } = string.Empty;
    public AreaKind Kind { get; set; }
    public int DisplayOrder { get; set; }
    public int? Capacity { get; set; }
    public int CurrentOccupancy { get; set; }
    public bool RequiresEntitlement { get; set; }
    public int? MinimumAge { get; set; }
    public int? MaxParticipantsPerStaff { get; set; }
    public bool IsOutOfService { get; set; }
    public string? OutOfServiceNote { get; set; }
    public bool IsActive { get; set; }

    public int DoorCount { get; set; }
}

public class RoomDto
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public Guid? AreaId { get; set; }
    public string? AreaName { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int DisplayOrder { get; set; }
    public bool HasSpotMap { get; set; }
    public int GridColumns { get; set; }
    public int GridRows { get; set; }
    public string? EquipmentNote { get; set; }
    public bool IsOutOfService { get; set; }
    public bool IsActive { get; set; }

    public List<RoomSpotDto> Spots { get; set; } = [];
}

public class RoomSpotDto
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public string Label { get; set; } = string.Empty;
    public int GridColumn { get; set; }
    public int GridRow { get; set; }
    public Guid? EquipmentAssetId { get; set; }
    public bool IsReserved { get; set; }
    public string? ReservedNote { get; set; }
    public bool IsOutOfService { get; set; }

    /// <summary>Filled by the booking screen: who has this spot for the class being viewed.</summary>
    public Guid? BookedByMemberId { get; set; }
    public string? BookedByName { get; set; }
}

/// <summary>Saves a room and its whole spot map in one call, the way the designer edits it.</summary>
public class SaveRoomLayoutDto
{
    public Guid? RoomId { get; set; }
    public Guid ClubId { get; set; }
    public Guid? AreaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public bool HasSpotMap { get; set; }
    public int GridColumns { get; set; } = 6;
    public int GridRows { get; set; } = 4;
    public string? EquipmentNote { get; set; }

    public List<RoomSpotDto> Spots { get; set; } = [];
}

// ── Settings ─────────────────────────────────────────────────────────────────

public class FitnessSettingsDto
{
    public Guid Id { get; set; }

    public string MemberNumberPrefix { get; set; } = "MEM";
    public int DefaultNoticePeriodDays { get; set; }
    public int DefaultCoolingOffDays { get; set; }
    public int MaxFreezeDaysPerYear { get; set; }
    public decimal DefaultFreezeFeePerMonth { get; set; }

    public BillingAnchor DefaultBillingAnchor { get; set; }
    public int FixedBillingDayOfMonth { get; set; }
    public ProrationRule DefaultProration { get; set; }
    public int InvoiceGraceDays { get; set; }
    public decimal DefaultLateFee { get; set; }
    public bool AutoRunBilling { get; set; }
    public TimeSpan BillingRunTime { get; set; }

    public int AccessCacheSeconds { get; set; }
    public bool CaptureImageOnDenial { get; set; }

    public int AbsenceRiskDays { get; set; }
    public int CriticalAbsenceDays { get; set; }
    public bool AutoScoreChurn { get; set; }

    public TimeSpan QuietHoursFrom { get; set; }
    public TimeSpan QuietHoursTo { get; set; }
    public bool RespectQuietHours { get; set; }
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
    public string? SmsSenderId { get; set; }

    public int LeadResponseSlaMinutes { get; set; }

    public decimal DiscountApprovalThresholdPercent { get; set; }
    public decimal RefundApprovalThreshold { get; set; }
    public decimal WriteOffApprovalThreshold { get; set; }
    public bool RequirePinForOverrides { get; set; }
}
