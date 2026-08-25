using Fitness.Domain.Enums;
using Nexcore.SharedKernel;

namespace Fitness.Domain.Entities;

/// <summary>
/// A site. Everything operational hangs off a club: areas, doors, classes, staff, sessions.
///
/// Deliberately called a *club* rather than an outlet or a branch. "Outlet" is Restaurant's and
/// Distribution's word for a different thing, and a branch is a platform-level tenancy concept —
/// a company can run three clubs inside one branch, and the two must not be confused.
/// </summary>
public class FitnessClub : BaseEntity
{
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

    /// <summary>Warehouse the pro shop depletes from. Null disables retail stock movement.</summary>
    public Guid? WarehouseId { get; set; }

    /// <summary>Optional link to a Sales POS store when the pro shop is also a retail till.</summary>
    public Guid? PosStoreId { get; set; }

    public Guid? DefaultTaxGroupId { get; set; }

    /// <summary>Rate applied to anything that does not set its own. Percentage, not fraction.</summary>
    public decimal DefaultTaxPercent { get; set; }

    /// <summary>
    /// Total people the club may hold at once. The soft cap warns the desk; the hard cap is what
    /// the access engine refuses on, and is usually a fire-safety number.
    /// </summary>
    public int? SoftCapacity { get; set; }
    public int? HardCapacity { get; set; }

    /// <summary>Live count maintained by check-in and check-out, reconciled by the nightly sweep.</summary>
    public int CurrentOccupancy { get; set; }

    // ── Policy defaults ──────────────────────────────────────────────────────

    public Guid? DefaultBookingPolicyId { get; set; }
    public Guid? DefaultCancellationPolicyId { get; set; }
    public Guid? DefaultDunningPolicyId { get; set; }

    /// <summary>Balance above which the door refuses entry. Zero means any debt blocks.</summary>
    public decimal AccessBalanceThreshold { get; set; }

    public AntiPassbackMode AntiPassback { get; set; } = AntiPassbackMode.Soft;

    /// <summary>Minutes an entry is remembered for <see cref="AntiPassbackMode.Timed"/>.</summary>
    public int AntiPassbackMinutes { get; set; } = 60;

    public OfflineAccessPolicy OfflinePolicy { get; set; } = OfflineAccessPolicy.AllowKnownActive;

    /// <summary>Minimum age to hold a membership here without a guardian.</summary>
    public int MinimumAge { get; set; } = 16;

    /// <summary>Age below which a guardian must be present in the building.</summary>
    public int GuardianRequiredBelowAge { get; set; } = 14;

    public bool RequiresWaiver { get; set; } = true;
    public bool RequiresHealthScreening { get; set; } = true;

    /// <summary>Cross-club visits from other clubs in the same company.</summary>
    public bool AllowsCrossClubVisits { get; set; } = true;
    public decimal CrossClubVisitFee { get; set; }

    public bool IsTemporarilyClosed { get; set; }
    public string? ClosureNote { get; set; }

    public string? LogoUrl { get; set; }
    public string? ReceiptFooter { get; set; }

    /// <summary>Groups clubs under one brand for consolidated reporting.</summary>
    public string? BrandCode { get; set; }

    public ICollection<ClubSchedule> Schedules { get; set; } = [];
    public ICollection<ClubArea> Areas { get; set; } = [];
    public ICollection<Room> Rooms { get; set; } = [];
}

/// <summary>
/// Opening hours for one weekday, or a dated override.
///
/// Staffed hours and access hours are separate on purpose: a 24/7 gym has a person on the desk
/// from 06:00 to 22:00 and an open door around the clock, and conflating the two either locks
/// members out at night or claims the club is staffed when it is not.
/// </summary>
public class ClubSchedule : BaseEntity
{
    public Guid ClubId { get; set; }
    public FitnessClub? Club { get; set; }

    /// <summary>0 = Sunday … 6 = Saturday. Ignored when <see cref="OverrideDate"/> is set.</summary>
    public int DayOfWeek { get; set; }

    /// <summary>Set for a one-off override (a bank holiday, a private event) that wins over the weekday row.</summary>
    public DateTime? OverrideDate { get; set; }

    public TimeSpan OpensAt { get; set; }
    public TimeSpan ClosesAt { get; set; }

    /// <summary>When reception is actually staffed. Null means the same as the access window.</summary>
    public TimeSpan? StaffedFrom { get; set; }
    public TimeSpan? StaffedTo { get; set; }

    public bool IsClosed { get; set; }
    public string? Note { get; set; }
}

/// <summary>A dated closure — refurbishment, a public holiday, a flood — with a member-facing notice.</summary>
public class ClubClosure : BaseEntity
{
    public Guid ClubId { get; set; }
    public FitnessClub? Club { get; set; }

    public DateTime StartsOn { get; set; }
    public DateTime EndsOn { get; set; }
    public string Reason { get; set; } = string.Empty;

    /// <summary>Shown to members in the app and on the kiosk while the closure is live.</summary>
    public string? MemberNotice { get; set; }

    /// <summary>Cancels every class in the window and notifies everyone booked.</summary>
    public bool CancelsClasses { get; set; } = true;

    /// <summary>Extends every active agreement by the closed days, which most markets require.</summary>
    public bool ExtendsAgreements { get; set; }

    public bool BlocksAccess { get; set; } = true;
}

/// <summary>
/// A part of the club that can be entered, booked or restricted separately — the pool, the studio,
/// the creche, the VIP floor. Areas are what plan entitlements grant, so an off-peak plan that
/// excludes the pool is a data question rather than a special case in code.
/// </summary>
public class ClubArea : BaseEntity
{
    public Guid ClubId { get; set; }
    public FitnessClub? Club { get; set; }

    public string Name { get; set; } = string.Empty;
    public AreaKind Kind { get; set; } = AreaKind.GymFloor;
    public int DisplayOrder { get; set; }

    public int? Capacity { get; set; }
    public int CurrentOccupancy { get; set; }

    /// <summary>Some areas need their own entitlement even for a full member (pool, spa, creche).</summary>
    public bool RequiresEntitlement { get; set; }

    /// <summary>Minimum age to enter unaccompanied.</summary>
    public int? MinimumAge { get; set; }

    /// <summary>Staff-to-participant ratio the booking engine enforces (creche, junior classes).</summary>
    public int? MaxParticipantsPerStaff { get; set; }

    public bool IsOutOfService { get; set; }
    public string? OutOfServiceNote { get; set; }
}

/// <summary>
/// A room classes run in. Separate from <see cref="ClubArea"/> because a single studio area can
/// hold two rooms, and because a room carries the thing an area does not: a spot map.
/// </summary>
public class Room : BaseEntity
{
    public Guid ClubId { get; set; }
    public FitnessClub? Club { get; set; }

    public Guid? AreaId { get; set; }
    public ClubArea? Area { get; set; }

    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether members pick a specific position. Cycle and reformer studios book bike 14, not
    /// "a place" — and a member who cannot choose their usual bike notices immediately.
    /// </summary>
    public bool HasSpotMap { get; set; }

    /// <summary>Grid the spot map is laid out on, so the room renders like the room.</summary>
    public int GridColumns { get; set; } = 6;
    public int GridRows { get; set; } = 4;

    public string? EquipmentNote { get; set; }
    public bool IsOutOfService { get; set; }

    public ICollection<RoomSpot> Spots { get; set; } = [];
}

/// <summary>
/// One bookable position in a room — a bike, a reformer, a mat, a rower.
///
/// Links to an equipment asset where there is one, so a bike marked out of order on the
/// maintenance screen stops being bookable without anyone touching the timetable.
/// </summary>
public class RoomSpot : BaseEntity
{
    public Guid RoomId { get; set; }
    public Room? Room { get; set; }

    /// <summary>What the member sees: "14", "B7", "Front row left".</summary>
    public string Label { get; set; } = string.Empty;

    public int GridColumn { get; set; }
    public int GridRow { get; set; }

    /// <summary>The physical machine, when this spot is one.</summary>
    public Guid? EquipmentAssetId { get; set; }

    /// <summary>Held back for staff, instructors or accessibility.</summary>
    public bool IsReserved { get; set; }
    public string? ReservedNote { get; set; }

    public bool IsOutOfService { get; set; }
}

/// <summary>
/// Company-wide Fitness configuration — one row per tenant. Anything that varies by site lives on
/// <see cref="FitnessClub"/> instead.
/// </summary>
public class FitnessSettings : BaseEntity
{
    // ── Membership ───────────────────────────────────────────────────────────

    /// <summary>Prefix for member numbers: MEM-000123.</summary>
    public string MemberNumberPrefix { get; set; } = "MEM";

    public int DefaultNoticePeriodDays { get; set; } = 30;
    public int DefaultCoolingOffDays { get; set; }

    /// <summary>Maximum freeze days a member may take in a rolling year.</summary>
    public int MaxFreezeDaysPerYear { get; set; } = 90;
    public decimal DefaultFreezeFeePerMonth { get; set; }

    // ── Billing ──────────────────────────────────────────────────────────────

    public BillingAnchor DefaultBillingAnchor { get; set; } = BillingAnchor.JoinAnniversary;
    public int FixedBillingDayOfMonth { get; set; } = 1;
    public ProrationRule DefaultProration { get; set; } = ProrationRule.Daily;

    /// <summary>Days after the due date before an invoice counts as overdue.</summary>
    public int InvoiceGraceDays { get; set; } = 3;

    public decimal DefaultLateFee { get; set; }

    /// <summary>Runs the billing job automatically each night rather than waiting for a human.</summary>
    public bool AutoRunBilling { get; set; } = true;
    public TimeSpan BillingRunTime { get; set; } = new(2, 0, 0);

    // ── Access ───────────────────────────────────────────────────────────────

    /// <summary>Seconds a granted decision stays cached on the controller.</summary>
    public int AccessCacheSeconds { get; set; } = 300;

    /// <summary>Photographs a denied entry so a disputed refusal can be settled.</summary>
    public bool CaptureImageOnDenial { get; set; } = true;

    // ── Retention ────────────────────────────────────────────────────────────

    /// <summary>Days without a visit before a member is flagged as at risk.</summary>
    public int AbsenceRiskDays { get; set; } = 14;
    public int CriticalAbsenceDays { get; set; } = 30;

    /// <summary>Recomputes the risk board overnight so the morning call list is ready.</summary>
    public bool AutoScoreChurn { get; set; } = true;

    // ── Messaging ────────────────────────────────────────────────────────────

    public TimeSpan QuietHoursFrom { get; set; } = new(21, 0, 0);
    public TimeSpan QuietHoursTo { get; set; } = new(8, 0, 0);
    public bool RespectQuietHours { get; set; } = true;

    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
    public string? SmsSenderId { get; set; }

    // ── Sales ────────────────────────────────────────────────────────────────

    /// <summary>Minutes a new lead may sit before the speed-to-lead SLA is breached.</summary>
    public int LeadResponseSlaMinutes { get; set; } = 15;

    // ── Approvals ────────────────────────────────────────────────────────────

    /// <summary>Discount above this percentage needs a manager PIN.</summary>
    public decimal DiscountApprovalThresholdPercent { get; set; } = 20;
    public decimal RefundApprovalThreshold { get; set; } = 100;
    public decimal WriteOffApprovalThreshold { get; set; } = 50;

    public bool RequirePinForOverrides { get; set; } = true;
}
