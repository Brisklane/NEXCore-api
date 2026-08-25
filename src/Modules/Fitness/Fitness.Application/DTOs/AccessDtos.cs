using Fitness.Domain.Enums;

namespace Fitness.Application.DTOs;

// ── The decision ─────────────────────────────────────────────────────────────

/// <summary>
/// A credential presented at a door, a kiosk or a desk.
///
/// Deliberately small: this is the highest-traffic request in the product and it has a 300 ms
/// budget, so it carries an identifier and a context and nothing else.
/// </summary>
public class AccessRequestDto
{
    public Guid ClubId { get; set; }
    public Guid? DoorId { get; set; }

    public string CredentialIdentifier { get; set; } = string.Empty;
    public CredentialType Method { get; set; } = CredentialType.RfidFob;
    public ReaderDirection Direction { get; set; } = ReaderDirection.In;

    /// <summary>Set when the panel decided this itself during an outage and is replaying it.</summary>
    public bool WasOfflineDecision { get; set; }
    public DateTime? OccurredAt { get; set; }

    public string? ControllerReference { get; set; }
}

/// <summary>
/// What the door does, and what the person is told.
///
/// The message is the point. "Access denied" makes a member queue at reception; "Your membership
/// is frozen until 3 March — see reception to restart it" sends them to the right place already
/// knowing what to ask for.
/// </summary>
public class AccessDecisionDto
{
    public AccessDecision Decision { get; set; }
    public AccessDenialReason DenialReason { get; set; }

    /// <summary>Written for the person at the door, never for the log.</summary>
    public string Message { get; set; } = string.Empty;

    public Guid? MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? PreferredName { get; set; }
    public string? PhotoUrl { get; set; }
    public string? MemberNumber { get; set; }
    public MemberStatus? MemberStatus { get; set; }

    public Guid? CheckInId { get; set; }
    public Guid? AccessEventId { get; set; }

    /// <summary>Warnings that did not block, so the kiosk can still say something useful.</summary>
    public List<MemberAlertDto> Alerts { get; set; } = [];

    /// <summary>What the member can do about a refusal, right now.</summary>
    public string? ResolutionAction { get; set; }
    public string? ResolutionRoute { get; set; }
    public decimal? AmountDue { get; set; }

    /// <summary>Their next class or session, so the kiosk greeting is worth reading.</summary>
    public UpcomingBookingDto? NextBooking { get; set; }

    public int ClubOccupancy { get; set; }
    public int? ClubCapacity { get; set; }

    public int VisitNumber { get; set; }
    public bool IsMilestoneVisit { get; set; }
    public bool IsBirthday { get; set; }

    public int DecisionMs { get; set; }
}

// ── Doors & controllers ──────────────────────────────────────────────────────

public class DoorDto
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public string? ClubName { get; set; }
    public Guid? AreaId { get; set; }
    public string? AreaName { get; set; }

    public string Name { get; set; } = string.Empty;
    public ReaderDirection Direction { get; set; }

    public Guid? ControllerId { get; set; }
    public string? ControllerName { get; set; }
    public bool ControllerOnline { get; set; }

    public string? ReaderAddress { get; set; }
    public string? HardwareKind { get; set; }

    public bool CountsOccupancy { get; set; }
    public bool RequiresClassBooking { get; set; }
    public int ClassBookingWindowMinutes { get; set; }
    public bool StaffOnly { get; set; }
    public AntiPassbackMode? AntiPassbackOverride { get; set; }

    public bool IsActive { get; set; }
    public bool IsHeldOpen { get; set; }
    public string? HeldOpenReason { get; set; }

    // Today's traffic, so the doors screen shows which reader is actually being used.
    public int EntriesToday { get; set; }
    public int DenialsToday { get; set; }
    public DateTime? LastEventAt { get; set; }
}

public class SaveDoorDto
{
    public Guid ClubId { get; set; }
    public Guid? AreaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ReaderDirection Direction { get; set; } = ReaderDirection.Bidirectional;
    public Guid? ControllerId { get; set; }
    public string? ReaderAddress { get; set; }
    public string? HardwareKind { get; set; }
    public bool CountsOccupancy { get; set; } = true;
    public bool RequiresClassBooking { get; set; }
    public int ClassBookingWindowMinutes { get; set; } = 15;
    public bool StaffOnly { get; set; }
    public AntiPassbackMode? AntiPassbackOverride { get; set; }
    public bool IsActive { get; set; } = true;
}

public class AccessControllerDto
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public string? ClubName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Vendor { get; set; }
    public string? Model { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? IpAddress { get; set; }
    public string? SerialNumber { get; set; }

    public OfflineAccessPolicy OfflinePolicy { get; set; }
    public int CacheSeconds { get; set; }

    public DateTime? LastHeartbeatAt { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public int PendingEventCount { get; set; }
    public bool IsOnline { get; set; }
    public int HeartbeatTimeoutMinutes { get; set; }
    public int? SecondsSinceHeartbeat { get; set; }
    public bool IsActive { get; set; }

    public List<DoorDto> Doors { get; set; } = [];
}

public class SaveControllerDto
{
    public Guid ClubId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Vendor { get; set; }
    public string? Model { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? IpAddress { get; set; }
    public string? SerialNumber { get; set; }
    public OfflineAccessPolicy OfflinePolicy { get; set; } = OfflineAccessPolicy.AllowKnownActive;
    public int CacheSeconds { get; set; } = 300;
    public int HeartbeatTimeoutMinutes { get; set; } = 5;
    public bool IsActive { get; set; } = true;
}

/// <summary>The entitlement list a controller caches so it can keep working offline.</summary>
public class AccessCacheDto
{
    public Guid ControllerId { get; set; }
    public DateTime GeneratedAt { get; set; }
    public OfflineAccessPolicy OfflinePolicy { get; set; }
    public int ValidSeconds { get; set; }
    public List<CachedCredentialDto> Credentials { get; set; } = [];
}

public class CachedCredentialDto
{
    public string Identifier { get; set; } = string.Empty;
    public Guid MemberId { get; set; }
    public bool IsAllowed { get; set; }

    /// <summary>Bit flags, Sunday = 1 … Saturday = 64, and the window inside those days.</summary>
    public int DaysOfWeekMask { get; set; }
    public TimeSpan? FromTime { get; set; }
    public TimeSpan? ToTime { get; set; }

    public DateTime? ValidUntil { get; set; }
    public string? DisplayName { get; set; }
}

public class AccessRuleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }
    public decimal? BalanceThreshold { get; set; }
    public bool RequiresWaiver { get; set; }
    public bool RequiresMedicalClearance { get; set; }
    public bool RespectsOccupancyCap { get; set; }
    public AntiPassbackMode AntiPassback { get; set; }
    public int? MinimumAge { get; set; }
    public bool RequiresGuardian { get; set; }
    public int MaxVisitsPerPeriod { get; set; }
    public EntitlementLimit VisitLimitBasis { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }

    public List<AccessRuleWindowDto> Windows { get; set; } = [];
}

public class AccessRuleWindowDto
{
    public Guid Id { get; set; }
    public int DaysOfWeekMask { get; set; }
    public TimeSpan StartsAt { get; set; }
    public TimeSpan EndsAt { get; set; }
    public string? Label { get; set; }
}

// ── Check-in & attendance ────────────────────────────────────────────────────

public class CheckInDto
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public string? ClubName { get; set; }
    public Guid? MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? MemberNumber { get; set; }
    public string? PhotoUrl { get; set; }

    public VisitKind Kind { get; set; }
    public DateTime CheckedInAt { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public int? DurationMinutes { get; set; }
    public bool AutoClosed { get; set; }

    public CredentialType Method { get; set; }
    public Guid? DoorId { get; set; }
    public string? DoorName { get; set; }
    public Guid? AreaId { get; set; }

    public Guid? ClassBookingId { get; set; }
    public string? ClassName { get; set; }
    public Guid? AppointmentId { get; set; }

    public Guid? HostMemberId { get; set; }
    public string? HostMemberName { get; set; }

    public bool WasManualEntry { get; set; }
    public string? CheckedInByName { get; set; }
    public decimal FeeCharged { get; set; }
}

/// <summary>Manual check-in from the desk, where staff have already identified the person.</summary>
public class ManualCheckInDto
{
    public Guid ClubId { get; set; }
    public Guid MemberId { get; set; }
    public Guid? ClassBookingId { get; set; }
    public Guid? AppointmentId { get; set; }

    /// <summary>Lets a refused member in anyway. Always logged, always with a reason.</summary>
    public bool OverrideDenial { get; set; }
    public string? OverrideReason { get; set; }
}

public class VisitHistoryDto
{
    public Guid Id { get; set; }
    public DateTime CheckedInAt { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public int? DurationMinutes { get; set; }
    public string ClubName { get; set; } = string.Empty;
    public VisitKind Kind { get; set; }
    public string? Activity { get; set; }
    public CredentialType Method { get; set; }
}

public class AccessEventDto
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public string? ClubName { get; set; }
    public Guid? DoorId { get; set; }
    public string? DoorName { get; set; }

    public DateTime OccurredAt { get; set; }

    public Guid? MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? MemberNumber { get; set; }
    public Guid? StaffId { get; set; }
    public string? StaffName { get; set; }

    public string? CredentialIdentifier { get; set; }
    public CredentialType Method { get; set; }

    public AccessDecision Decision { get; set; }
    public AccessDenialReason DenialReason { get; set; }
    public string? DecisionMessage { get; set; }
    public ReaderDirection Direction { get; set; }

    public string? ImageUrl { get; set; }
    public bool WasOfflineDecision { get; set; }
    public DateTime? ReplayedAt { get; set; }

    public string? OverriddenByName { get; set; }
    public string? OverrideReason { get; set; }
    public int DecisionMs { get; set; }
}

/// <summary>Live occupancy for the desk and for the dashboard.</summary>
public class OccupancyDto
{
    public Guid ClubId { get; set; }
    public string ClubName { get; set; } = string.Empty;
    public int Occupancy { get; set; }
    public int? SoftCapacity { get; set; }
    public int? HardCapacity { get; set; }
    public int PercentFull { get; set; }
    public bool IsOverSoftCap { get; set; }
    public bool IsAtHardCap { get; set; }
    public DateTime AsAt { get; set; }

    public List<AreaOccupancyDto> Areas { get; set; } = [];

    /// <summary>Everyone currently in the building, for a fire roll-call.</summary>
    public List<CheckInDto> InClub { get; set; } = [];
}

public class AreaOccupancyDto
{
    public Guid AreaId { get; set; }
    public string AreaName { get; set; } = string.Empty;
    public AreaKind Kind { get; set; }
    public int Occupancy { get; set; }
    public int? Capacity { get; set; }
    public int PercentFull { get; set; }
}

/// <summary>Occupancy over time, for the peak-hours heat map.</summary>
public class OccupancyTrendDto
{
    public Guid ClubId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<OccupancyPointDto> Points { get; set; } = [];
    public int PeakOccupancy { get; set; }
    public DateTime? PeakAt { get; set; }
}

public class OccupancyPointDto
{
    public DateTime At { get; set; }
    public int DayOfWeek { get; set; }
    public int Hour { get; set; }
    public int Occupancy { get; set; }
    public int CheckIns { get; set; }
}

// ── Guests & day passes ──────────────────────────────────────────────────────

public class GuestVisitDto
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public Guid HostMemberId { get; set; }
    public string? HostMemberName { get; set; }

    public string GuestName { get; set; } = string.Empty;
    public string? GuestPhone { get; set; }
    public string? GuestEmail { get; set; }
    public DateTime? GuestDateOfBirth { get; set; }

    public DateTime VisitedOn { get; set; }
    public bool WaiverSigned { get; set; }
    public bool UsedHostAllowance { get; set; }
    public decimal FeeCharged { get; set; }
    public Guid? CreatedLeadId { get; set; }
}

public class RegisterGuestDto
{
    public Guid ClubId { get; set; }
    public Guid HostMemberId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string? GuestPhone { get; set; }
    public string? GuestEmail { get; set; }
    public DateTime? GuestDateOfBirth { get; set; }

    public Guid? WaiverTemplateId { get; set; }
    public string? SignatureImageUrl { get; set; }

    /// <summary>Take it from the host's allowance where there is one, otherwise charge.</summary>
    public bool UseHostAllowance { get; set; } = true;
    public decimal? FeeOverride { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Card;
    public Guid? CashSessionId { get; set; }

    /// <summary>Creates a lead so the guest is followed up, which is the entire point of guest passes.</summary>
    public bool CreateLead { get; set; } = true;
}

public class DayPassDto
{
    public Guid Id { get; set; }
    public string PassNumber { get; set; } = string.Empty;
    public Guid ClubId { get; set; }
    public string? ClubName { get; set; }
    public Guid? MemberId { get; set; }

    public string VisitorName { get; set; } = string.Empty;
    public string? VisitorPhone { get; set; }
    public string? VisitorEmail { get; set; }

    public Guid? PlanId { get; set; }
    public string? PlanName { get; set; }

    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public int MaxEntries { get; set; }
    public int EntriesUsed { get; set; }
    public bool IsExpired { get; set; }
    public bool IsExhausted { get; set; }

    public decimal AmountPaid { get; set; }
    public bool WaiverSigned { get; set; }
    public bool IsTrial { get; set; }

    public Guid? CreatedLeadId { get; set; }
    public string? IssuedByName { get; set; }
    public string? TemporaryCredential { get; set; }
}

public class IssueDayPassDto
{
    public Guid ClubId { get; set; }
    public Guid? PlanId { get; set; }
    public Guid? MemberId { get; set; }

    public string VisitorName { get; set; } = string.Empty;
    public string? VisitorPhone { get; set; }
    public string? VisitorEmail { get; set; }
    public DateTime? VisitorDateOfBirth { get; set; }

    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public int MaxEntries { get; set; } = 1;

    public decimal? PriceOverride { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Card;
    public Guid? CashSessionId { get; set; }

    public Guid? WaiverTemplateId { get; set; }
    public string? SignatureImageUrl { get; set; }

    public bool IsTrial { get; set; }
    public bool CreateLead { get; set; } = true;
    public bool IssueTemporaryCredential { get; set; } = true;
}

// ── Front desk ───────────────────────────────────────────────────────────────

/// <summary>
/// Everything the front-desk screen renders, in one call.
///
/// The receptionist has a queue; the screen cannot be six requests deep. Search, who is in, what
/// is on, and what needs doing all arrive together.
/// </summary>
public class FrontDeskDto
{
    public Guid ClubId { get; set; }
    public string ClubName { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public OccupancyDto Occupancy { get; set; } = new();

    public List<CheckInDto> RecentCheckIns { get; set; } = [];
    public List<ClassOccurrenceSummaryDto> ClassesToday { get; set; } = [];
    public List<AppointmentSummaryDto> AppointmentsToday { get; set; } = [];
    public List<RetentionTaskDto> MyTasks { get; set; } = [];
    public List<MemberAlertDto> UrgentAlerts { get; set; } = [];
    public List<AnnouncementDto> Announcements { get; set; } = [];

    public int OpenLeads { get; set; }
    public int LeadsBreachingSla { get; set; }
    public int OverdueBalances { get; set; }
    public decimal OverdueAmount { get; set; }
    public int WaiversOutstanding { get; set; }

    public Guid? OpenCashSessionId { get; set; }
    public decimal CashSessionTakings { get; set; }

    public bool IsOpenNow { get; set; }
    public bool IsStaffedNow { get; set; }
    public TimeSpan? ClosesAt { get; set; }
}

/// <summary>
/// A quick lookup at the desk: type three letters of a name, a phone number, or scan a fob.
/// </summary>
public class MemberSearchDto
{
    public string Query { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }
    public bool IncludeInactive { get; set; }
    public int Limit { get; set; } = 10;
}
