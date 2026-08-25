using Fitness.Domain.Enums;
using Nexcore.SharedKernel;

namespace Fitness.Domain.Entities;

/// <summary>
/// A physical way in or out, and the rules that govern it.
///
/// Doors are modelled separately from areas because one area can have three doors with different
/// rules — the pool has a members' door, a swim-school door that only opens for a booked lesson,
/// and a staff door — and because a door is the thing that goes offline.
/// </summary>
public class Door : BaseEntity
{
    public Guid ClubId { get; set; }
    public FitnessClub? Club { get; set; }

    /// <summary>The area entering this door puts you in. Null for the club's main entrance.</summary>
    public Guid? AreaId { get; set; }
    public ClubArea? Area { get; set; }

    public string Name { get; set; } = string.Empty;
    public ReaderDirection Direction { get; set; } = ReaderDirection.Bidirectional;

    public Guid? ControllerId { get; set; }
    public AccessController? Controller { get; set; }

    /// <summary>The reader's address on the controller — relay number, port, Wiegand channel.</summary>
    public string? ReaderAddress { get; set; }

    /// <summary>Turnstile, speed gate, mag-lock, barrier, or a reader with no barrier at all.</summary>
    public string? HardwareKind { get; set; }

    /// <summary>Counts people in and out. A door with no barrier cannot be trusted to.</summary>
    public bool CountsOccupancy { get; set; } = true;

    /// <summary>
    /// Only opens for someone booked into a class starting soon. Studio-only memberships and
    /// hotel clubs run on this, and it is the difference between selling one product and two.
    /// </summary>
    public bool RequiresClassBooking { get; set; }
    public int ClassBookingWindowMinutes { get; set; } = 15;

    public bool StaffOnly { get; set; }

    /// <summary>Overrides the club's mode for this door — a fire exit is never anti-passback.</summary>
    public AntiPassbackMode? AntiPassbackOverride { get; set; }

    public bool IsHeldOpen { get; set; }
    public string? HeldOpenReason { get; set; }
}

/// <summary>
/// A panel that makes the actual open/refuse decision at the door.
///
/// It holds a cached copy of who may enter, because the one thing a turnstile must not do is stop
/// working when the network does. The heartbeat is what turns "the door is broken" from a phone
/// call at 6am into an alert at 3am.
/// </summary>
public class AccessController : BaseEntity
{
    public Guid ClubId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Vendor and model, so a support call starts from something useful.</summary>
    public string? Vendor { get; set; }
    public string? Model { get; set; }
    public string? FirmwareVersion { get; set; }

    public string? IpAddress { get; set; }
    public string? SerialNumber { get; set; }

    /// <summary>Shared secret this controller signs its event webhooks with.</summary>
    public string? ApiKeyHash { get; set; }

    public OfflineAccessPolicy OfflinePolicy { get; set; } = OfflineAccessPolicy.AllowKnownActive;

    /// <summary>How long a cached decision stays good on the panel.</summary>
    public int CacheSeconds { get; set; } = 300;

    public DateTime? LastHeartbeatAt { get; set; }
    public DateTime? LastSyncAt { get; set; }

    /// <summary>Events the panel buffered while offline and has not yet replayed.</summary>
    public int PendingEventCount { get; set; }

    public bool IsOnline { get; set; }

    /// <summary>Minutes without a heartbeat before an alert is raised.</summary>
    public int HeartbeatTimeoutMinutes { get; set; } = 5;

    public ICollection<Door> Doors { get; set; } = [];
}

/// <summary>
/// A named set of access conditions that can be attached to a plan, an area or a door.
///
/// Separate from plan entitlements so a club can express "staff hours", "24/7", "off-peak" once
/// and reuse it, rather than restating the same six time bands on eleven plans.
/// </summary>
public class AccessRule : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }

    /// <summary>Overrides the club's balance threshold for holders of this rule.</summary>
    public decimal? BalanceThreshold { get; set; }

    public bool RequiresWaiver { get; set; } = true;
    public bool RequiresMedicalClearance { get; set; }

    /// <summary>Refuses entry once the club is at its hard cap.</summary>
    public bool RespectsOccupancyCap { get; set; } = true;

    public AntiPassbackMode AntiPassback { get; set; } = AntiPassbackMode.Soft;

    public int? MinimumAge { get; set; }
    public bool RequiresGuardian { get; set; }

    /// <summary>Visits allowed in the window. Zero is unlimited.</summary>
    public int MaxVisitsPerPeriod { get; set; }
    public EntitlementLimit VisitLimitBasis { get; set; } = EntitlementLimit.Unlimited;

    public bool IsDefault { get; set; }

    public ICollection<AccessRuleWindow> Windows { get; set; } = [];
}

/// <summary>When an access rule permits entry. Empty means whenever the club is open.</summary>
public class AccessRuleWindow : BaseEntity
{
    public Guid AccessRuleId { get; set; }
    public AccessRule? AccessRule { get; set; }

    /// <summary>Bit flags, Sunday = 1 … Saturday = 64.</summary>
    public int DaysOfWeekMask { get; set; } = 127;

    public TimeSpan StartsAt { get; set; }
    public TimeSpan EndsAt { get; set; }

    public string? Label { get; set; }
}

/// <summary>
/// Someone arriving. The row the whole attendance, occupancy and churn story is built from, so it
/// is written on every entry regardless of what else happens.
/// </summary>
public class CheckIn : BaseEntity
{
    public Guid ClubId { get; set; }

    /// <summary>Null for a guest or a day-pass visitor who is not a member.</summary>
    public Guid? MemberId { get; set; }
    public Member? Member { get; set; }

    public VisitKind Kind { get; set; } = VisitKind.Member;

    public DateTime CheckedInAt { get; set; } = DateTime.UtcNow;
    public DateTime? CheckedOutAt { get; set; }

    /// <summary>Filled on check-out, or estimated by the overnight sweep for anyone who never scanned out.</summary>
    public int? DurationMinutes { get; set; }

    /// <summary>True when the nightly sweep closed this rather than the member scanning out.</summary>
    public bool AutoClosed { get; set; }

    public CredentialType Method { get; set; } = CredentialType.RfidFob;
    public Guid? CredentialId { get; set; }
    public Guid? DoorId { get; set; }
    public Guid? AreaId { get; set; }

    /// <summary>The booking this visit was for, when they came in for a class or a session.</summary>
    public Guid? ClassBookingId { get; set; }
    public Guid? AppointmentId { get; set; }

    /// <summary>Set for a guest, pointing at the member whose allowance paid for them.</summary>
    public Guid? HostMemberId { get; set; }
    public Guid? DayPassId { get; set; }

    /// <summary>Staff let them in by hand; who and why is on the access event.</summary>
    public bool WasManualEntry { get; set; }
    public Guid? CheckedInByStaffId { get; set; }

    /// <summary>Entitlement allowance this visit consumed, for a capped plan.</summary>
    public Guid? AgreementId { get; set; }
    public bool ConsumedVisitAllowance { get; set; }

    /// <summary>Charged for a cross-club visit or an over-allowance entry.</summary>
    public decimal FeeCharged { get; set; }
}

/// <summary>
/// Every read at every reader, granted or refused, kept and searchable.
///
/// This is the record that settles arguments — "the door would not let me in on Tuesday" is
/// answerable in one query, with the reason and, on a refusal, the photograph. It is written even
/// when nothing else is, which is why it is separate from <see cref="CheckIn"/>: a denied read
/// is not a visit, but it is very much an event.
/// </summary>
public class AccessEvent : BaseEntity
{
    public Guid ClubId { get; set; }
    public Guid? DoorId { get; set; }
    public Guid? ControllerId { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public Guid? MemberId { get; set; }
    public Guid? StaffId { get; set; }
    public Guid? CredentialId { get; set; }

    /// <summary>What was presented, kept verbatim even when it matched nothing.</summary>
    public string? CredentialIdentifier { get; set; }
    public CredentialType Method { get; set; }

    public AccessDecision Decision { get; set; }
    public AccessDenialReason DenialReason { get; set; } = AccessDenialReason.None;

    /// <summary>The sentence shown to the person at the door. Written for them, not for the log.</summary>
    public string? DecisionMessage { get; set; }

    public ReaderDirection Direction { get; set; } = ReaderDirection.In;

    /// <summary>Captured on refusal, so a disputed denial has evidence.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>True when the panel decided this on its own cache during an outage.</summary>
    public bool WasOfflineDecision { get; set; }

    /// <summary>Set when the event was buffered on the panel and replayed later.</summary>
    public DateTime? ReplayedAt { get; set; }

    /// <summary>Staff overrode a refusal. Always logged, always with a reason.</summary>
    public Guid? OverriddenByStaffId { get; set; }
    public string? OverrideReason { get; set; }

    public Guid? CheckInId { get; set; }

    /// <summary>Milliseconds the decision took. Watched, because a slow turnstile is a queue.</summary>
    public int DecisionMs { get; set; }
}

/// <summary>
/// A point-in-time count of how many people are in a club or an area.
///
/// Snapshotted on a schedule rather than derived on demand: the peak-hour heat map is one of the
/// most-read reports in the product, and reconstructing it by replaying two years of check-ins
/// every time somebody opens it is not a plan.
/// </summary>
public class OccupancySnapshot : BaseEntity
{
    public Guid ClubId { get; set; }
    public Guid? AreaId { get; set; }

    public DateTime TakenAt { get; set; } = DateTime.UtcNow;

    public int Occupancy { get; set; }
    public int? Capacity { get; set; }

    public int CheckInsInInterval { get; set; }
    public int CheckOutsInInterval { get; set; }
}

/// <summary>
/// A guest brought in by a member, consuming that member's guest allowance.
///
/// A guest signs their own waiver before entry — the host's signature covers the host. That is
/// not a nicety; it is the whole reason the club can let a stranger onto a squat rack.
/// </summary>
public class GuestVisit : BaseEntity
{
    public Guid ClubId { get; set; }
    public Guid HostMemberId { get; set; }
    public Member? HostMember { get; set; }

    public string GuestName { get; set; } = string.Empty;
    public string? GuestPhone { get; set; }
    public string? GuestEmail { get; set; }
    public DateTime? GuestDateOfBirth { get; set; }

    public DateTime VisitedOn { get; set; } = DateTime.UtcNow;

    public bool WaiverSigned { get; set; }
    public Guid? WaiverSignatureId { get; set; }

    /// <summary>Free from the host's allowance, or paid for.</summary>
    public bool UsedHostAllowance { get; set; }
    public decimal FeeCharged { get; set; }

    public Guid? CheckInId { get; set; }
    public Guid? TemporaryCredentialId { get; set; }

    /// <summary>Guests are prospects. Converting them into leads automatically is free pipeline.</summary>
    public Guid? CreatedLeadId { get; set; }
}

/// <summary>
/// A non-member paying to come in once, or for a short window.
///
/// Modelled with a real start and end rather than as a single visit because a week pass on
/// holiday is the same product as a day pass, and because it is the shape a trial takes.
/// </summary>
public class DayPass : BaseEntity
{
    public string PassNumber { get; set; } = string.Empty;

    public Guid ClubId { get; set; }

    /// <summary>Set when the visitor is (or becomes) a member record.</summary>
    public Guid? MemberId { get; set; }

    public string VisitorName { get; set; } = string.Empty;
    public string? VisitorPhone { get; set; }
    public string? VisitorEmail { get; set; }
    public DateTime? VisitorDateOfBirth { get; set; }

    public Guid? PlanId { get; set; }

    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }

    /// <summary>Entries allowed. One for a day pass, unlimited within the window for a week pass.</summary>
    public int MaxEntries { get; set; } = 1;
    public int EntriesUsed { get; set; }

    public decimal AmountPaid { get; set; }
    public Guid? PaymentId { get; set; }

    public bool WaiverSigned { get; set; }
    public Guid? WaiverSignatureId { get; set; }

    public Guid? TemporaryCredentialId { get; set; }

    /// <summary>Every day-pass visitor is a lead. Not creating one is leaving money on the counter.</summary>
    public Guid? CreatedLeadId { get; set; }

    public bool IsTrial { get; set; }
    public Guid? IssuedByStaffId { get; set; }
}

/// <summary>
/// How much of a capped entitlement a member has used in the current window.
///
/// Held per agreement per period so "eight visits a month" is a counter that resets rather than a
/// query over the whole visit history — which is what makes the door decision fast enough.
/// </summary>
public class VisitAllowanceUsage : BaseEntity
{
    public Guid MemberId { get; set; }
    public Guid AgreementId { get; set; }
    public Guid? EntitlementId { get; set; }

    public EntitlementKind Kind { get; set; } = EntitlementKind.ClubAccess;

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public int Allowance { get; set; }
    public int Used { get; set; }

    /// <summary>Uses beyond the allowance, when the plan permits paying for extras.</summary>
    public int Overage { get; set; }
    public decimal OverageCharged { get; set; }

    public DateTime? LastUsedAt { get; set; }
}
