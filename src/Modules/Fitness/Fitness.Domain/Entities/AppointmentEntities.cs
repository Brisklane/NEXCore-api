using Fitness.Domain.Enums;
using Nexcore.SharedKernel;

namespace Fitness.Domain.Entities;

/// <summary>
/// A member of staff who can be booked, and the services they are cleared to deliver.
///
/// Separate from <see cref="FitnessStaff"/> because being bookable is a different thing from
/// being employed: a self-employed trainer who rents floor space is bookable and not on payroll,
/// and a duty manager is on payroll and not bookable.
/// </summary>
public class BookableStaff : BaseEntity
{
    public Guid StaffId { get; set; }
    public FitnessStaff? Staff { get; set; }

    public Guid ClubId { get; set; }

    public string DisplayName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }

    /// <summary>Shown on the member-facing trainer profile.</summary>
    public string? Bio { get; set; }
    public string? Specialities { get; set; }

    /// <summary>Comma-separated service ids they may deliver. Empty means all of them.</summary>
    public string? ServiceIds { get; set; }

    /// <summary>What the club charges for their hour, when it differs from the service's price.</summary>
    public decimal? HourlyRate { get; set; }

    public bool BookableOnline { get; set; } = true;

    /// <summary>Minutes between the end of one booking and the start of the next.</summary>
    public int DefaultBufferMinutes { get; set; }

    /// <summary>How far ahead members may book them.</summary>
    public int BookingWindowDays { get; set; } = 30;

    /// <summary>Self-employed and renting space, rather than employed and on payroll.</summary>
    public bool IsContractor { get; set; }

    public int MaxClientsPerDay { get; set; }
    public bool AcceptingNewClients { get; set; } = true;

    public ICollection<StaffAvailability> Availability { get; set; } = [];
}

/// <summary>When a bookable member of staff works, as a repeating weekly pattern.</summary>
public class StaffAvailability : BaseEntity
{
    public Guid BookableStaffId { get; set; }
    public BookableStaff? BookableStaff { get; set; }

    public Guid? ClubId { get; set; }

    /// <summary>0 = Sunday … 6 = Saturday.</summary>
    public int DayOfWeek { get; set; }

    public TimeSpan StartsAt { get; set; }
    public TimeSpan EndsAt { get; set; }

    /// <summary>A break inside the window — lunch, a standing meeting.</summary>
    public TimeSpan? BreakStartsAt { get; set; }
    public TimeSpan? BreakEndsAt { get; set; }

    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

/// <summary>Holiday, sickness, training, or an hour blocked out. Beats the weekly pattern.</summary>
public class StaffTimeOff : BaseEntity
{
    public Guid StaffId { get; set; }

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }

    /// <summary>Holiday, sick, training, personal, blocked.</summary>
    public string Reason { get; set; } = string.Empty;
    public string? Note { get; set; }

    public bool IsAllDay { get; set; }
    public bool IsApproved { get; set; }
    public Guid? ApprovedByUserId { get; set; }
}

/// <summary>
/// One booked session with a trainer, physio, coach or nutritionist.
///
/// Sign-off matters more than the booking: the session becoming <see cref="AppointmentStatus.Completed"/>
/// is what consumes a credit and what accrues the trainer's commission, so it is a deliberate act
/// rather than something the clock does at the end of the hour.
/// </summary>
public class Appointment : BaseEntity
{
    public string AppointmentNumber { get; set; } = string.Empty;

    public Guid ClubId { get; set; }

    public Guid ServiceId { get; set; }
    public AppointmentService? Service { get; set; }

    public Guid StaffId { get; set; }

    /// <summary>Null for a blocked-out slot with no client.</summary>
    public Guid? MemberId { get; set; }
    public Member? Member { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Confirmed;
    public BookingChannel Channel { get; set; } = BookingChannel.FrontDesk;

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }

    public Guid? RoomId { get; set; }
    public Guid? ResourceId { get; set; }

    public DateTime? CheckedInAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    // ── Payment ──────────────────────────────────────────────────────────────

    public BookingPaymentKind PaymentKind { get; set; } = BookingPaymentKind.PackCredit;
    public Guid? SessionPackagePurchaseId { get; set; }
    public Guid? SessionCreditMovementId { get; set; }
    public int CreditsUsed { get; set; } = 1;

    public decimal AmountPaid { get; set; }
    public Guid? InvoiceId { get; set; }
    public decimal PenaltyCharged { get; set; }

    /// <summary>Set when the appointment belongs to a repeating series, so the whole run can be moved.</summary>
    public Guid? SeriesId { get; set; }

    /// <summary>The appointment this one replaced when it was moved, keeping the history.</summary>
    public Guid? RescheduledFromId { get; set; }

    // ── Delivery ─────────────────────────────────────────────────────────────

    /// <summary>What was done. The trainer's record and the member's, on the same row.</summary>
    public string? SessionNotes { get; set; }
    public string? PlanForNextSession { get; set; }

    /// <summary>The workout delivered, when the trainer programmed one.</summary>
    public Guid? WorkoutId { get; set; }

    public bool IsFirstSession { get; set; }

    /// <summary>Semi-private and small-group sessions carry several members on one appointment.</summary>
    public ICollection<AppointmentParticipant> Participants { get; set; } = [];
}

/// <summary>An extra client on a semi-private or small-group session.</summary>
public class AppointmentParticipant : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Confirmed;
    public DateTime? CheckedInAt { get; set; }

    public int CreditsUsed { get; set; } = 1;
    public Guid? SessionCreditMovementId { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal PenaltyCharged { get; set; }
}

/// <summary>A repeating appointment run — "every Tuesday at 07:00 for twelve weeks".</summary>
public class AppointmentSeries : BaseEntity
{
    public Guid ClubId { get; set; }
    public Guid StaffId { get; set; }
    public Guid MemberId { get; set; }
    public Guid ServiceId { get; set; }

    /// <summary>Bit flags, Sunday = 1 … Saturday = 64.</summary>
    public int DaysOfWeekMask { get; set; }
    public TimeSpan StartsAt { get; set; }
    public int DurationMinutes { get; set; }

    public DateTime SeriesStart { get; set; }
    public DateTime? SeriesEnd { get; set; }
    public int OccurrenceCount { get; set; }
    public int RepeatEveryWeeks { get; set; } = 1;

    public bool IsCancelled { get; set; }
}

/// <summary>
/// A block of sessions bought up front.
///
/// The purchase and the credits are separate records because the money and the entitlement have
/// different lifetimes: the money is earned as the sessions are taken, and the credits can expire
/// with the money already collected. That gap is exactly the deferred-revenue liability.
/// </summary>
public class SessionPackagePurchase : BaseEntity
{
    public string PurchaseNumber { get; set; } = string.Empty;

    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid ClubId { get; set; }
    public Guid? PlanId { get; set; }
    public Guid? ServiceId { get; set; }

    /// <summary>The trainer the block was sold for, when it is tied to one.</summary>
    public Guid? StaffId { get; set; }

    public DateTime PurchasedOn { get; set; } = DateTime.UtcNow;

    public int SessionsPurchased { get; set; }
    public int SessionsUsed { get; set; }
    public int SessionsRemaining { get; set; }

    public decimal TotalPrice { get; set; }

    /// <summary>Total divided by sessions. What a single unused session is worth on a refund.</summary>
    public decimal PricePerSession { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public DateTime? ExpiresOn { get; set; }
    public bool IsExpired { get; set; }

    public Guid? InvoiceId { get; set; }
    public Guid? DeferredRevenueScheduleId { get; set; }
    public Guid? SoldByStaffId { get; set; }

    public bool IsTransferable { get; set; }
    public bool IsRefundable { get; set; }
}

/// <summary>
/// The running balance of what a member may still claim — PT sessions, class credits, creche
/// hours. One row per entitlement pool, with the movements underneath it.
/// </summary>
public class SessionCredit : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid? SessionPackagePurchaseId { get; set; }
    public Guid? AgreementId { get; set; }

    /// <summary>What the credit can be spent on. Null means anything of that kind.</summary>
    public Guid? ServiceId { get; set; }
    public Guid? ClassTypeId { get; set; }
    public EntitlementKind Kind { get; set; } = EntitlementKind.PersonalTraining;

    public int Granted { get; set; }
    public int Used { get; set; }
    public int Remaining { get; set; }

    /// <summary>Reserved by a waitlist entry. Held, not spent — so the balance shown is honest.</summary>
    public int Held { get; set; }

    public DateTime? ExpiresOn { get; set; }
    public bool IsExpired { get; set; }

    /// <summary>Value of one credit, so an expiry or a refund can be costed.</summary>
    public decimal UnitValue { get; set; }

    public ICollection<SessionCreditMovement> Movements { get; set; } = [];
}

/// <summary>
/// Every change to a credit balance, in order. The ledger that ends "how many do I have left?"
/// arguments, because the answer can always be shown as a list rather than asserted as a number.
/// </summary>
public class SessionCreditMovement : BaseEntity
{
    public Guid SessionCreditId { get; set; }
    public SessionCredit? SessionCredit { get; set; }

    public Guid MemberId { get; set; }

    public SessionCreditMovementKind Kind { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>Negative consumes, positive grants.</summary>
    public int Quantity { get; set; }
    public int BalanceAfter { get; set; }

    public Guid? AppointmentId { get; set; }
    public Guid? ClassBookingId { get; set; }
    public Guid? InvoiceId { get; set; }

    /// <summary>Set when a hold from a waitlist entry was released rather than spent.</summary>
    public bool WasHeld { get; set; }

    public string? Note { get; set; }
    public Guid? PerformedByStaffId { get; set; }
}

/// <summary>
/// The trainer marking a session delivered, optionally with the member's own confirmation.
///
/// A separate record from the appointment because it is the commercial event: it is what
/// consumes the credit, accrues the commission, and — if a member ever disputes being charged for
/// a session they say they did not have — is the evidence.
/// </summary>
public class SessionSignOff : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public Guid MemberId { get; set; }
    public Guid StaffId { get; set; }

    public DateTime SignedOffAt { get; set; } = DateTime.UtcNow;

    /// <summary>Some clubs require the member to sign or enter a PIN. Many do not.</summary>
    public bool MemberConfirmed { get; set; }
    public string? MemberSignatureUrl { get; set; }

    public int CreditsConsumed { get; set; } = 1;
    public decimal SessionValue { get; set; }

    /// <summary>Set once the commission engine has picked it up, so it is never counted twice.</summary>
    public bool CommissionAccrued { get; set; }
    public Guid? CommissionAccrualId { get; set; }

    public string? Note { get; set; }
}

/// <summary>
/// Which coach owns a member's outcome.
///
/// Not decoration: an assigned coach is the single strongest retention lever a club has, and the
/// at-risk board routes to this person. A member with no coach belongs to the club, which means
/// they belong to nobody.
/// </summary>
public class CoachAssignment : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid StaffId { get; set; }
    public Guid ClubId { get; set; }

    public DateTime AssignedOn { get; set; } = DateTime.UtcNow;
    public DateTime? EndedOn { get; set; }
    public string? EndReason { get; set; }

    /// <summary>The main coach, as opposed to a second specialist the member also sees.</summary>
    public bool IsPrimary { get; set; } = true;

    public Guid? AssignedByUserId { get; set; }
}
