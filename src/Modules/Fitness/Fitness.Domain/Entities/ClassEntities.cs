using Fitness.Domain.Enums;
using Nexcore.SharedKernel;

namespace Fitness.Domain.Entities;

/// <summary>
/// A kind of class the club runs — Spin, Reformer Pilates, BodyPump, Beginners Boxing.
///
/// The type carries everything true of every instance of it; the occurrence carries what is true
/// of Tuesday at seven. Keeping those apart is what makes "cancel Thursday's spin" a one-row
/// change instead of an edit to the class itself.
/// </summary>
public class ClassType : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Strength, cardio, mind-body, aquatic, martial arts, dance, junior.</summary>
    public string? Discipline { get; set; }

    /// <summary>Shown to members when they browse the timetable.</summary>
    public string? MarketingBlurb { get; set; }
    public string? ImageUrl { get; set; }
    public string? ColourHex { get; set; }
    public int DisplayOrder { get; set; }

    public int DefaultDurationMinutes { get; set; } = 45;
    public int DefaultCapacity { get; set; } = 20;

    /// <summary>1 (gentle) to 5 (all out), so a member can pick something they will survive.</summary>
    public int Intensity { get; set; } = 3;

    public string? EquipmentNeeded { get; set; }

    /// <summary>Minimum age to attend. Junior classes carry a maximum too.</summary>
    public int? MinimumAge { get; set; }
    public int? MaximumAge { get; set; }

    /// <summary>A coach must have signed the member off before they can book — Olympic lifting, advanced Reformer.</summary>
    public bool RequiresSkillClearance { get; set; }
    public Guid? RequiredSkillId { get; set; }

    /// <summary>Non-members may pay to drop in.</summary>
    public bool AllowsDropIn { get; set; } = true;
    public decimal DropInPrice { get; set; }

    /// <summary>Credits deducted from a pack when a holder books. Usually one.</summary>
    public int CreditCost { get; set; } = 1;

    public Guid? BookingPolicyId { get; set; }
    public Guid? CancellationPolicyId { get; set; }

    /// <summary>Off-peak spots may be sold through ClassPass-style marketplaces.</summary>
    public bool AvailableToMarketplace { get; set; }

    public bool IsBookable { get; set; } = true;
}

/// <summary>
/// A repeating slot on the timetable — "Spin, Tuesdays 07:00, Studio 2, Sarah".
///
/// The occurrences are generated from it, not stored as an infinite series, so publishing a new
/// term is one record and moving a class permanently does not mean editing fifty-two rows.
/// </summary>
public class ClassSchedule : BaseEntity
{
    public Guid ClubId { get; set; }
    public Guid ClassTypeId { get; set; }
    public ClassType? ClassType { get; set; }

    public Guid? RoomId { get; set; }
    public Room? Room { get; set; }

    public Guid? InstructorStaffId { get; set; }

    /// <summary>Bit flags, Sunday = 1 … Saturday = 64. One row can cover Mon/Wed/Fri.</summary>
    public int DaysOfWeekMask { get; set; }

    public TimeSpan StartsAt { get; set; }
    public int DurationMinutes { get; set; } = 45;

    public int Capacity { get; set; } = 20;

    /// <summary>How many places are held back for marketplace bookings on off-peak occurrences.</summary>
    public int MarketplaceCapacity { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    /// <summary>Runs every week, every second week, every fourth. One is the usual answer.</summary>
    public int RepeatEveryWeeks { get; set; } = 1;

    /// <summary>Days ahead occurrences are materialised. The generator keeps this window full.</summary>
    public int GenerateAheadDays { get; set; } = 60;
    public DateTime? GeneratedThrough { get; set; }

    /// <summary>A published schedule appears on the member-facing timetable; a draft does not.</summary>
    public bool IsPublished { get; set; }

    /// <summary>Groups a seasonal timetable so a whole term can be published or pulled at once.</summary>
    public string? SeasonCode { get; set; }
}

/// <summary>
/// One actual class, at one time, on one day. The thing members book.
///
/// Materialised from the schedule so that a substitution, a room change or a cancellation is a
/// change to *this* class and nothing else.
/// </summary>
public class ClassOccurrence : BaseEntity
{
    public Guid ClubId { get; set; }
    public Guid ClassTypeId { get; set; }
    public ClassType? ClassType { get; set; }

    public Guid? ClassScheduleId { get; set; }

    public Guid? RoomId { get; set; }
    public Room? Room { get; set; }

    public Guid? InstructorStaffId { get; set; }

    /// <summary>Set when someone is covering, so the roster and the member app both say who.</summary>
    public Guid? SubstituteStaffId { get; set; }

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }

    public ClassOccurrenceStatus Status { get; set; } = ClassOccurrenceStatus.Scheduled;

    public int Capacity { get; set; }
    public int BookedCount { get; set; }
    public int WaitlistCount { get; set; }
    public int AttendedCount { get; set; }
    public int NoShowCount { get; set; }

    public int MarketplaceCapacity { get; set; }
    public int MarketplaceBookedCount { get; set; }

    /// <summary>Opens for booking at this moment. Popular classes release on a schedule so it is fair.</summary>
    public DateTime? BookingOpensAt { get; set; }
    public DateTime? BookingClosesAt { get; set; }

    /// <summary>Minutes before the start after which an unclaimed spot is released to the waitlist.</summary>
    public int SpotReleaseMinutes { get; set; } = 5;

    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledByStaffId { get; set; }

    /// <summary>Everyone booked has been told the class is off.</summary>
    public bool CancellationNotified { get; set; }

    /// <summary>The workout being run, for a box that programmes its classes.</summary>
    public Guid? WorkoutId { get; set; }

    public string? Note { get; set; }

    public ICollection<ClassBooking> Bookings { get; set; } = [];
}

/// <summary>
/// A member's place in a class.
///
/// Carries how it was paid for, because that is what decides what happens when it is cancelled
/// late: an entitlement booking costs nothing, a pack credit is forfeited, a drop-in payment is
/// gone. A booking table that does not know which it was cannot enforce a policy.
/// </summary>
public class ClassBooking : BaseEntity
{
    public Guid ClassOccurrenceId { get; set; }
    public ClassOccurrence? ClassOccurrence { get; set; }

    public Guid? MemberId { get; set; }
    public Member? Member { get; set; }

    /// <summary>Set for a non-member drop-in, where there is no member record.</summary>
    public string? GuestName { get; set; }
    public string? GuestPhone { get; set; }
    public string? GuestEmail { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Booked;
    public BookingPaymentKind PaymentKind { get; set; } = BookingPaymentKind.Entitlement;
    public BookingChannel Channel { get; set; } = BookingChannel.MemberApp;

    public DateTime BookedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CheckedInAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    /// <summary>Which spot they are on, for a room with a spot map.</summary>
    public Guid? SpotId { get; set; }
    public string? SpotLabel { get; set; }

    public Guid? AgreementId { get; set; }

    /// <summary>The session-credit movement that paid for it, so a refund knows what to give back.</summary>
    public Guid? SessionCreditMovementId { get; set; }
    public int CreditsUsed { get; set; }

    public decimal AmountPaid { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid? PaymentId { get; set; }

    /// <summary>Charged because they cancelled too late or did not turn up.</summary>
    public decimal PenaltyCharged { get; set; }
    public bool CreditForfeited { get; set; }
    public bool StrikeIssued { get; set; }

    public Guid? MarketplaceChannelId { get; set; }
    public string? MarketplaceReference { get; set; }

    /// <summary>Their position while waitlisted; cleared when they are promoted.</summary>
    public int? WaitlistPosition { get; set; }
    public DateTime? PromotedAt { get; set; }

    public string? Note { get; set; }
    public Guid? BookedByStaffId { get; set; }
}

/// <summary>
/// How far ahead members may book, and how many places they may hold.
///
/// A named policy rather than three columns on the class type, because the answer differs by plan
/// tier — the whole point of a premium membership at most clubs is that it books further ahead.
/// </summary>
public class BookingPolicy : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }

    public int BookingOpensDaysBefore { get; set; } = 14;

    /// <summary>Minutes before the start after which booking closes. Zero allows booking until it starts.</summary>
    public int BookingClosesMinutesBefore { get; set; }

    public int MaxConcurrentBookings { get; set; }
    public int MaxBookingsPerDay { get; set; }
    public int MaxBookingsPerWeek { get; set; }

    public bool WaitlistEnabled { get; set; } = true;
    public int MaxWaitlistLength { get; set; } = 20;

    /// <summary>
    /// Holds the member's credit the moment they join the waitlist. Without it, a promotion at
    /// 05:50 fails because the credit was spent elsewhere at midnight.
    /// </summary>
    public bool HoldCreditOnWaitlist { get; set; } = true;

    /// <summary>Minutes a promoted member has to confirm before the place moves on.</summary>
    public int WaitlistConfirmMinutes { get; set; } = 30;

    /// <summary>Stops booking the same class type twice in a day.</summary>
    public bool PreventDuplicateSameDay { get; set; }

    public bool RequiresPaymentUpFront { get; set; }
    public bool IsDefault { get; set; }
}

/// <summary>
/// What happens when someone cancels too late or does not turn up.
///
/// This is the actual policy engine boutiques run on, and getting it wrong in either direction is
/// expensive: too soft and the 06:00 class is half empty with a waitlist, too hard and members
/// stop booking at all.
/// </summary>
public class CancellationPolicy : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }

    /// <summary>Hours before the start within which a cancellation counts as late.</summary>
    public int FreeCancelHours { get; set; } = 12;

    public PolicyOutcome LateCancelOutcome { get; set; } = PolicyOutcome.ForfeitCredit;
    public decimal LateCancelFee { get; set; }

    public PolicyOutcome NoShowOutcome { get; set; } = PolicyOutcome.ForfeitCreditAndFee;
    public decimal NoShowFee { get; set; }

    /// <summary>Minutes after the start after which an unarrived booking becomes a no-show.</summary>
    public int NoShowGraceMinutes { get; set; } = 10;

    // ── Strikes ──────────────────────────────────────────────────────────────

    /// <summary>Strikes before booking rights are suspended. Zero disables strikes entirely.</summary>
    public int StrikeThreshold { get; set; } = 3;

    /// <summary>Days the strike count looks back over. A rolling window, not a lifetime tally.</summary>
    public int StrikeWindowDays { get; set; } = 30;

    /// <summary>Days a member cannot book once the threshold is hit.</summary>
    public int BookingBanDays { get; set; } = 7;

    public bool IsDefault { get; set; }
}

/// <summary>
/// One late cancellation or no-show held against a member, inside a rolling window.
///
/// Rows rather than a counter, so the member can be shown exactly which three bookings put them
/// over — which is the difference between a policy that feels fair and one that feels arbitrary.
/// </summary>
public class LateCancelStrike : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid? ClassBookingId { get; set; }
    public Guid? AppointmentId { get; set; }
    public Guid ClubId { get; set; }

    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;

    /// <summary>Late cancel, or no-show. Some clubs weight them differently.</summary>
    public bool WasNoShow { get; set; }

    public string? ClassName { get; set; }
    public decimal FeeCharged { get; set; }

    /// <summary>Falls out of the rolling window on this date.</summary>
    public DateTime ExpiresOn { get; set; }

    /// <summary>A manager forgave it. Kept rather than deleted, so the pattern is still visible.</summary>
    public bool IsWaived { get; set; }
    public string? WaivedReason { get; set; }
    public Guid? WaivedByStaffId { get; set; }
}

/// <summary>
/// A fixed block of classes bought as one thing — an eight-week beginners' course, a swim term.
///
/// Different from a pack: the dates are fixed, the roster is fixed, and missing week three does
/// not give you a credit for week nine.
/// </summary>
public class CourseEnrolment : BaseEntity
{
    public Guid ClubId { get; set; }
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid ClassScheduleId { get; set; }

    public string CourseName { get; set; } = string.Empty;

    public DateTime StartsOn { get; set; }
    public DateTime EndsOn { get; set; }
    public int TotalSessions { get; set; }
    public int SessionsAttended { get; set; }

    public decimal Price { get; set; }
    public Guid? InvoiceId { get; set; }

    public bool IsCompleted { get; set; }
    public bool IsWithdrawn { get; set; }
    public string? WithdrawalReason { get; set; }
}

/// <summary>
/// A third-party marketplace that sells into the club's off-peak capacity.
///
/// Kept as a first-class channel because the economics are completely different — a different
/// rate, a revenue split, a capacity carve-out — and mixing those bookings in with member
/// attendance makes both numbers meaningless.
/// </summary>
public class MarketplaceChannel : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }

    /// <summary>What the club is actually paid per attendance, which is not the member price.</summary>
    public decimal RatePerBooking { get; set; }
    public decimal RevenueSharePercent { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>Only classes in these windows are offered. Marketplaces fill troughs, not peaks.</summary>
    public bool OffPeakOnly { get; set; } = true;

    /// <summary>Default places carved out per occurrence.</summary>
    public int DefaultCapacityPerClass { get; set; } = 3;

    /// <summary>Provider adapter details. The adapters themselves are a later phase.</summary>
    public string? ApiEndpoint { get; set; }
    public string? ApiKeyHash { get; set; }
    public DateTime? LastSyncAt { get; set; }
}

/// <summary>A booking that arrived from a marketplace, kept alongside its own reconciliation state.</summary>
public class MarketplaceBooking : BaseEntity
{
    public Guid MarketplaceChannelId { get; set; }
    public MarketplaceChannel? MarketplaceChannel { get; set; }

    public Guid ClassOccurrenceId { get; set; }
    public Guid? ClassBookingId { get; set; }

    public string ExternalReference { get; set; } = string.Empty;
    public string AttendeeName { get; set; } = string.Empty;
    public string? AttendeeEmail { get; set; }

    public DateTime BookedAt { get; set; } = DateTime.UtcNow;
    public bool Attended { get; set; }

    public decimal AmountDue { get; set; }
    public bool IsReconciled { get; set; }
    public DateTime? ReconciledOn { get; set; }
}
