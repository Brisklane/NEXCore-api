using Fitness.Domain.Enums;

namespace Fitness.Application.DTOs;

// ── Class types ──────────────────────────────────────────────────────────────

public class ClassTypeDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Discipline { get; set; }
    public string? MarketingBlurb { get; set; }
    public string? ImageUrl { get; set; }
    public string? ColourHex { get; set; }
    public int DisplayOrder { get; set; }

    public int DefaultDurationMinutes { get; set; }
    public int DefaultCapacity { get; set; }
    public int Intensity { get; set; }
    public string? EquipmentNeeded { get; set; }

    public int? MinimumAge { get; set; }
    public int? MaximumAge { get; set; }
    public bool RequiresSkillClearance { get; set; }
    public Guid? RequiredSkillId { get; set; }

    public bool AllowsDropIn { get; set; }
    public decimal DropInPrice { get; set; }
    public int CreditCost { get; set; }

    public Guid? BookingPolicyId { get; set; }
    public string? BookingPolicyName { get; set; }
    public Guid? CancellationPolicyId { get; set; }
    public string? CancellationPolicyName { get; set; }

    public bool AvailableToMarketplace { get; set; }
    public bool IsBookable { get; set; }
    public bool IsActive { get; set; }

    // Roll-ups so the class-type screen shows what is worth keeping on the timetable.
    public int OccurrencesLast30Days { get; set; }
    public int AverageAttendance { get; set; }
    public int AverageFillPercent { get; set; }
}

public class SaveClassTypeDto
{
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Discipline { get; set; }
    public string? MarketingBlurb { get; set; }
    public string? ImageUrl { get; set; }
    public string? ColourHex { get; set; }
    public int DisplayOrder { get; set; }
    public int DefaultDurationMinutes { get; set; } = 45;
    public int DefaultCapacity { get; set; } = 20;
    public int Intensity { get; set; } = 3;
    public string? EquipmentNeeded { get; set; }
    public int? MinimumAge { get; set; }
    public int? MaximumAge { get; set; }
    public bool RequiresSkillClearance { get; set; }
    public Guid? RequiredSkillId { get; set; }
    public bool AllowsDropIn { get; set; } = true;
    public decimal DropInPrice { get; set; }
    public int CreditCost { get; set; } = 1;
    public Guid? BookingPolicyId { get; set; }
    public Guid? CancellationPolicyId { get; set; }
    public bool AvailableToMarketplace { get; set; }
    public bool IsBookable { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

// ── Schedules ────────────────────────────────────────────────────────────────

public class ClassScheduleDto
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public Guid ClassTypeId { get; set; }
    public string ClassTypeName { get; set; } = string.Empty;
    public string? ColourHex { get; set; }

    public Guid? RoomId { get; set; }
    public string? RoomName { get; set; }
    public Guid? InstructorStaffId { get; set; }
    public string? InstructorName { get; set; }

    public int DaysOfWeekMask { get; set; }
    public TimeSpan StartsAt { get; set; }
    public int DurationMinutes { get; set; }
    public int Capacity { get; set; }
    public int MarketplaceCapacity { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int RepeatEveryWeeks { get; set; }
    public int GenerateAheadDays { get; set; }
    public DateTime? GeneratedThrough { get; set; }

    public bool IsPublished { get; set; }
    public string? SeasonCode { get; set; }
    public bool IsActive { get; set; }

    public int UpcomingOccurrences { get; set; }
}

public class SaveClassScheduleDto
{
    public Guid ClubId { get; set; }
    public Guid ClassTypeId { get; set; }
    public Guid? RoomId { get; set; }
    public Guid? InstructorStaffId { get; set; }
    public int DaysOfWeekMask { get; set; }
    public TimeSpan StartsAt { get; set; }
    public int DurationMinutes { get; set; } = 45;
    public int Capacity { get; set; } = 20;
    public int MarketplaceCapacity { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int RepeatEveryWeeks { get; set; } = 1;
    public int GenerateAheadDays { get; set; } = 60;
    public bool IsPublished { get; set; }
    public string? SeasonCode { get; set; }
}

/// <summary>
/// What is wrong with a timetable before it is published — the instructor in two rooms, the room
/// double-booked, a class inside a closure. Caught before members can see it, not after.
/// </summary>
public class ScheduleConflictDto
{
    public string ConflictType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime OccursAt { get; set; }
    public Guid? ScheduleId { get; set; }
    public Guid? OccurrenceId { get; set; }
    public Guid? ConflictsWithId { get; set; }
    public string? ConflictsWithLabel { get; set; }

    /// <summary>A blocker stops publish; a warning does not.</summary>
    public bool IsBlocking { get; set; }
}

// ── Occurrences ──────────────────────────────────────────────────────────────

public class ClassOccurrenceSummaryDto
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public Guid ClassTypeId { get; set; }
    public string ClassTypeName { get; set; } = string.Empty;
    public string? ColourHex { get; set; }
    public string? Discipline { get; set; }
    public int Intensity { get; set; }

    public Guid? RoomId { get; set; }
    public string? RoomName { get; set; }
    public Guid? InstructorStaffId { get; set; }
    public string? InstructorName { get; set; }
    public string? InstructorPhotoUrl { get; set; }
    public bool HasSubstitute { get; set; }

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public int DurationMinutes { get; set; }

    public ClassOccurrenceStatus Status { get; set; }
    public int Capacity { get; set; }
    public int BookedCount { get; set; }
    public int WaitlistCount { get; set; }
    public int AttendedCount { get; set; }
    public int NoShowCount { get; set; }
    public int SpacesLeft { get; set; }
    public int FillPercent { get; set; }

    public bool HasSpotMap { get; set; }
    public bool AllowsDropIn { get; set; }
    public decimal DropInPrice { get; set; }

    public DateTime? BookingOpensAt { get; set; }
    public DateTime? BookingClosesAt { get; set; }
    public bool BookingIsOpen { get; set; }

    public string? CancellationReason { get; set; }

    // Filled when the caller is a member, so the timetable can show their own state.
    public bool ViewerIsBooked { get; set; }
    public bool ViewerIsWaitlisted { get; set; }
    public Guid? ViewerBookingId { get; set; }
    public bool ViewerCanBook { get; set; }
    public string? ViewerBlockReason { get; set; }
}

public class ClassOccurrenceDetailDto : ClassOccurrenceSummaryDto
{
    public Guid? ClassScheduleId { get; set; }
    public Guid? SubstituteStaffId { get; set; }
    public string? SubstituteName { get; set; }

    public int MarketplaceCapacity { get; set; }
    public int MarketplaceBookedCount { get; set; }
    public int SpotReleaseMinutes { get; set; }

    public DateTime? CancelledAt { get; set; }
    public bool CancellationNotified { get; set; }

    public Guid? WorkoutId { get; set; }
    public string? WorkoutName { get; set; }
    public string? Note { get; set; }

    public List<ClassBookingDto> Bookings { get; set; } = [];
    public List<ClassBookingDto> Waitlist { get; set; } = [];
    public List<RoomSpotDto> SpotMap { get; set; } = [];
}

/// <summary>The timetable a screen renders — a date range of occurrences with its filters resolved.</summary>
public class TimetableDto
{
    public Guid ClubId { get; set; }
    public string ClubName { get; set; } = string.Empty;
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    public List<ClassOccurrenceSummaryDto> Occurrences { get; set; } = [];

    public List<ClassTypeDto> ClassTypes { get; set; } = [];
    public List<RoomDto> Rooms { get; set; } = [];
    public List<StaffSummaryDto> Instructors { get; set; } = [];

    public int TotalClasses { get; set; }
    public int TotalCapacity { get; set; }
    public int TotalBooked { get; set; }
    public int AverageFillPercent { get; set; }
}

public class UpdateOccurrenceDto
{
    public Guid OccurrenceId { get; set; }
    public Guid? RoomId { get; set; }
    public Guid? InstructorStaffId { get; set; }
    public Guid? SubstituteStaffId { get; set; }
    public DateTime? StartsAt { get; set; }
    public int? DurationMinutes { get; set; }
    public int? Capacity { get; set; }
    public Guid? WorkoutId { get; set; }
    public string? Note { get; set; }

    /// <summary>Tells everyone booked what changed. Almost always yes.</summary>
    public bool NotifyBookedMembers { get; set; } = true;
}

public class CancelOccurrenceDto
{
    public Guid OccurrenceId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool NotifyBookedMembers { get; set; } = true;

    /// <summary>Credits always go back when the club cancels; this is here to be explicit about it.</summary>
    public bool RefundCredits { get; set; } = true;
}

// ── Bookings ─────────────────────────────────────────────────────────────────

public class ClassBookingDto
{
    public Guid Id { get; set; }
    public Guid ClassOccurrenceId { get; set; }
    public string? ClassName { get; set; }
    public DateTime? ClassStartsAt { get; set; }
    public string? RoomName { get; set; }
    public string? InstructorName { get; set; }

    public Guid? MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? MemberNumber { get; set; }
    public string? PhotoUrl { get; set; }
    public string? GuestName { get; set; }

    public BookingStatus Status { get; set; }
    public BookingPaymentKind PaymentKind { get; set; }
    public BookingChannel Channel { get; set; }

    public DateTime BookedAt { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public Guid? SpotId { get; set; }
    public string? SpotLabel { get; set; }

    public int CreditsUsed { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal PenaltyCharged { get; set; }
    public bool CreditForfeited { get; set; }
    public bool StrikeIssued { get; set; }

    public int? WaitlistPosition { get; set; }
    public DateTime? PromotedAt { get; set; }

    public string? Note { get; set; }

    /// <summary>Shown on the instructor's roster so a condition is not a surprise mid-class.</summary>
    public List<MedicalFlagDto> MedicalFlags { get; set; } = [];
    public bool IsFirstVisit { get; set; }
    public int VisitCount { get; set; }
}

public class CreateBookingDto
{
    public Guid ClassOccurrenceId { get; set; }
    public Guid? MemberId { get; set; }

    /// <summary>A non-member drop-in, taken at the desk.</summary>
    public string? GuestName { get; set; }
    public string? GuestPhone { get; set; }
    public string? GuestEmail { get; set; }

    public Guid? SpotId { get; set; }
    public BookingChannel Channel { get; set; } = BookingChannel.FrontDesk;

    /// <summary>Joins the waitlist rather than failing when the class is full.</summary>
    public bool JoinWaitlistIfFull { get; set; } = true;

    /// <summary>Books past an entitlement or policy block. Manager only, always logged.</summary>
    public bool OverridePolicy { get; set; }
    public string? OverrideReason { get; set; }

    public PaymentMethod? PaymentMethod { get; set; }
    public Guid? CashSessionId { get; set; }
    public string? Note { get; set; }
}

/// <summary>Whether this member may book this class, worked out before the button is pressed.</summary>
public class BookingEligibilityDto
{
    public bool CanBook { get; set; }
    public string? BlockReason { get; set; }
    public bool CanWaitlist { get; set; }
    public int? WaitlistPosition { get; set; }

    public BookingPaymentKind PaymentKind { get; set; }
    public int CreditsRequired { get; set; }
    public int CreditsAvailable { get; set; }
    public decimal AmountPayable { get; set; }

    public bool WithinBookingWindow { get; set; }
    public DateTime? BookingOpensAt { get; set; }
    public bool HasEntitlement { get; set; }
    public bool HasSkillClearance { get; set; }
    public bool MeetsAgeRequirement { get; set; }
    public bool WithinConcurrentLimit { get; set; }
    public bool IsBookingBanned { get; set; }
    public DateTime? BanEndsOn { get; set; }

    public List<RoomSpotDto> AvailableSpots { get; set; } = [];
}

public class CancelBookingDto
{
    public Guid BookingId { get; set; }
    public string? Reason { get; set; }

    /// <summary>Waives the late-cancel penalty. Manager only, always logged.</summary>
    public bool WaivePenalty { get; set; }
    public string? WaiveReason { get; set; }
}

/// <summary>What cancelling now will cost, so the member is told before they confirm.</summary>
public class CancelBookingPreviewDto
{
    public Guid BookingId { get; set; }
    public bool IsLate { get; set; }
    public int HoursUntilStart { get; set; }
    public int FreeCancelHours { get; set; }

    public PolicyOutcome Outcome { get; set; }
    public bool CreditWillBeForfeited { get; set; }
    public decimal FeeWillBeCharged { get; set; }
    public bool StrikeWillBeIssued { get; set; }
    public int CurrentStrikes { get; set; }
    public int StrikeThreshold { get; set; }
    public bool WillTriggerBan { get; set; }

    public string Explanation { get; set; } = string.Empty;
    public int WaitlistLength { get; set; }
}

public class MarkAttendanceDto
{
    public Guid ClassOccurrenceId { get; set; }

    /// <summary>Bookings to mark attended.</summary>
    public List<Guid> AttendedBookingIds { get; set; } = [];

    /// <summary>Bookings to mark no-show, which applies the policy.</summary>
    public List<Guid> NoShowBookingIds { get; set; } = [];

    /// <summary>Members who turned up without booking.</summary>
    public List<Guid> WalkInMemberIds { get; set; } = [];

    /// <summary>Closes the class, so no-show penalties fire and the roster locks.</summary>
    public bool CompleteClass { get; set; }
}

// ── Policies ─────────────────────────────────────────────────────────────────

public class BookingPolicyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }
    public int BookingOpensDaysBefore { get; set; }
    public int BookingClosesMinutesBefore { get; set; }
    public int MaxConcurrentBookings { get; set; }
    public int MaxBookingsPerDay { get; set; }
    public int MaxBookingsPerWeek { get; set; }
    public bool WaitlistEnabled { get; set; }
    public int MaxWaitlistLength { get; set; }
    public bool HoldCreditOnWaitlist { get; set; }
    public int WaitlistConfirmMinutes { get; set; }
    public bool PreventDuplicateSameDay { get; set; }
    public bool RequiresPaymentUpFront { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

public class CancellationPolicyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }
    public int FreeCancelHours { get; set; }
    public PolicyOutcome LateCancelOutcome { get; set; }
    public decimal LateCancelFee { get; set; }
    public PolicyOutcome NoShowOutcome { get; set; }
    public decimal NoShowFee { get; set; }
    public int NoShowGraceMinutes { get; set; }
    public int StrikeThreshold { get; set; }
    public int StrikeWindowDays { get; set; }
    public int BookingBanDays { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

public class LateCancelStrikeDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public DateTime OccurredOn { get; set; }
    public bool WasNoShow { get; set; }
    public string? ClassName { get; set; }
    public decimal FeeCharged { get; set; }
    public DateTime ExpiresOn { get; set; }
    public bool IsWaived { get; set; }
    public string? WaivedReason { get; set; }
    public string? WaivedByName { get; set; }
}

// ── Courses ──────────────────────────────────────────────────────────────────

public class CourseEnrolmentDto
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public Guid ClassScheduleId { get; set; }
    public string CourseName { get; set; } = string.Empty;

    public DateTime StartsOn { get; set; }
    public DateTime EndsOn { get; set; }
    public int TotalSessions { get; set; }
    public int SessionsAttended { get; set; }
    public int AttendancePercent { get; set; }

    public decimal Price { get; set; }
    public Guid? InvoiceId { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsWithdrawn { get; set; }
    public string? WithdrawalReason { get; set; }
}

// ── Marketplace ──────────────────────────────────────────────────────────────

public class MarketplaceChannelDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }
    public decimal RatePerBooking { get; set; }
    public decimal RevenueSharePercent { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool OffPeakOnly { get; set; }
    public int DefaultCapacityPerClass { get; set; }
    public string? ApiEndpoint { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public bool IsActive { get; set; }

    public int BookingsThisMonth { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public decimal UnreconciledAmount { get; set; }
}
