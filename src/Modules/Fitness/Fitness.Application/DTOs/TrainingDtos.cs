using Fitness.Domain.Enums;

namespace Fitness.Application.DTOs;

// ── Bookable staff & appointments ────────────────────────────────────────────

public class BookableStaffDto
{
    public Guid Id { get; set; }
    public Guid StaffId { get; set; }
    public Guid ClubId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string? Bio { get; set; }
    public string? Specialities { get; set; }

    public decimal? HourlyRate { get; set; }
    public bool BookableOnline { get; set; }
    public int DefaultBufferMinutes { get; set; }
    public int BookingWindowDays { get; set; }
    public bool IsContractor { get; set; }
    public int MaxClientsPerDay { get; set; }
    public bool AcceptingNewClients { get; set; }
    public bool IsActive { get; set; }

    public List<Guid> ServiceIds { get; set; } = [];
    public List<StaffAvailabilityDto> Availability { get; set; } = [];

    // Utilisation, so the PT manager can see who has capacity.
    public int SessionsThisWeek { get; set; }
    public int AvailableHoursThisWeek { get; set; }
    public int BookedHoursThisWeek { get; set; }
    public int UtilisationPercent { get; set; }
    public int ActiveClients { get; set; }
}

public class StaffAvailabilityDto
{
    public Guid Id { get; set; }
    public Guid BookableStaffId { get; set; }
    public Guid? ClubId { get; set; }
    public int DayOfWeek { get; set; }
    public TimeSpan StartsAt { get; set; }
    public TimeSpan EndsAt { get; set; }
    public TimeSpan? BreakStartsAt { get; set; }
    public TimeSpan? BreakEndsAt { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class StaffTimeOffDto
{
    public Guid Id { get; set; }
    public Guid StaffId { get; set; }
    public string? StaffName { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Note { get; set; }
    public bool IsAllDay { get; set; }
    public bool IsApproved { get; set; }
    public string? ApprovedByName { get; set; }
}

public class AppointmentSummaryDto
{
    public Guid Id { get; set; }
    public string AppointmentNumber { get; set; } = string.Empty;
    public Guid ClubId { get; set; }

    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public AppointmentKind Kind { get; set; }
    public string? ColourHex { get; set; }

    public Guid StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;

    public Guid? MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? MemberPhotoUrl { get; set; }
    public string? MemberPhone { get; set; }

    public AppointmentStatus Status { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public int DurationMinutes { get; set; }

    public string? RoomName { get; set; }
    public string? ResourceName { get; set; }

    public DateTime? CheckedInAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public int ParticipantCount { get; set; }
    public bool IsFirstSession { get; set; }
    public bool IsSignedOff { get; set; }

    public int CreditsUsed { get; set; }
    public decimal AmountPaid { get; set; }
}

public class AppointmentDetailDto : AppointmentSummaryDto
{
    public BookingChannel Channel { get; set; }
    public Guid? RoomId { get; set; }
    public Guid? ResourceId { get; set; }

    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    public BookingPaymentKind PaymentKind { get; set; }
    public Guid? SessionPackagePurchaseId { get; set; }
    public Guid? InvoiceId { get; set; }
    public decimal PenaltyCharged { get; set; }

    public Guid? SeriesId { get; set; }
    public Guid? RescheduledFromId { get; set; }

    public string? SessionNotes { get; set; }
    public string? PlanForNextSession { get; set; }
    public Guid? WorkoutId { get; set; }
    public string? WorkoutName { get; set; }

    public List<AppointmentParticipantDto> Participants { get; set; } = [];
    public List<MedicalFlagDto> MedicalFlags { get; set; } = [];
    public List<MemberGoalDto> Goals { get; set; } = [];

    /// <summary>Their last three sessions with this trainer, so the coach is not starting cold.</summary>
    public List<AppointmentSummaryDto> RecentSessions { get; set; } = [];
    public int SessionsRemaining { get; set; }
}

public class AppointmentParticipantDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public AppointmentStatus Status { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public int CreditsUsed { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal PenaltyCharged { get; set; }
}

public class CreateAppointmentDto
{
    public Guid ClubId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid StaffId { get; set; }
    public Guid? MemberId { get; set; }

    public DateTime StartsAt { get; set; }
    public int? DurationMinutesOverride { get; set; }

    public Guid? RoomId { get; set; }
    public Guid? ResourceId { get; set; }

    public BookingChannel Channel { get; set; } = BookingChannel.FrontDesk;
    public BookingPaymentKind PaymentKind { get; set; } = BookingPaymentKind.PackCredit;
    public Guid? SessionPackagePurchaseId { get; set; }

    public PaymentMethod? PaymentMethod { get; set; }
    public Guid? CashSessionId { get; set; }

    /// <summary>Extra clients on a semi-private or small-group session.</summary>
    public List<Guid> AdditionalMemberIds { get; set; } = [];

    /// <summary>Creates a repeating run rather than one appointment.</summary>
    public bool IsRecurring { get; set; }
    public int RepeatEveryWeeks { get; set; } = 1;
    public int OccurrenceCount { get; set; }

    public bool OverrideConflicts { get; set; }
    public string? Note { get; set; }
}

/// <summary>Open slots for a trainer or a service, which is what a booking screen actually asks for.</summary>
public class AvailabilitySearchDto
{
    public Guid ClubId { get; set; }
    public Guid? StaffId { get; set; }
    public Guid ServiceId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public Guid? MemberId { get; set; }

    /// <summary>Searches every qualified trainer rather than one, for "first available".</summary>
    public bool AnyStaff { get; set; }
}

public class AvailabilitySlotDto
{
    public Guid StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public Guid? RoomId { get; set; }
    public string? RoomName { get; set; }
    public decimal Price { get; set; }
    public bool IsPreferredCoach { get; set; }
}

public class SignOffSessionDto
{
    public Guid AppointmentId { get; set; }
    public Guid? MemberId { get; set; }
    public string? SessionNotes { get; set; }
    public string? PlanForNextSession { get; set; }
    public string? MemberSignatureUrl { get; set; }
    public bool MemberConfirmed { get; set; }
    public int CreditsConsumed { get; set; } = 1;
}

// ── Session packages & credits ───────────────────────────────────────────────

public class SessionPackagePurchaseDto
{
    public Guid Id { get; set; }
    public string PurchaseNumber { get; set; } = string.Empty;
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public Guid ClubId { get; set; }
    public Guid? PlanId { get; set; }
    public string? PlanName { get; set; }
    public Guid? ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public Guid? StaffId { get; set; }
    public string? StaffName { get; set; }

    public DateTime PurchasedOn { get; set; }
    public int SessionsPurchased { get; set; }
    public int SessionsUsed { get; set; }
    public int SessionsRemaining { get; set; }

    public decimal TotalPrice { get; set; }
    public decimal PricePerSession { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public DateTime? ExpiresOn { get; set; }
    public bool IsExpired { get; set; }
    public bool ExpiringSoon { get; set; }
    public int? DaysToExpiry { get; set; }

    public Guid? InvoiceId { get; set; }
    public string? SoldByName { get; set; }
    public bool IsTransferable { get; set; }
    public bool IsRefundable { get; set; }

    /// <summary>Unearned value still sitting on this package — the deferred-revenue liability.</summary>
    public decimal UnearnedValue { get; set; }
}

public class SellPackageDto
{
    public Guid MemberId { get; set; }
    public Guid ClubId { get; set; }
    public Guid? PlanId { get; set; }
    public Guid? ServiceId { get; set; }
    public Guid? StaffId { get; set; }

    public int Sessions { get; set; }
    public decimal? PriceOverride { get; set; }
    public string? PriceOverrideReason { get; set; }
    public DateTime? ExpiresOn { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Card;
    public bool TakePaymentNow { get; set; } = true;
    public Guid? CashSessionId { get; set; }
    public Guid? SoldByStaffId { get; set; }
}

public class SessionCreditDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public Guid? SessionPackagePurchaseId { get; set; }
    public Guid? AgreementId { get; set; }
    public Guid? ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public Guid? ClassTypeId { get; set; }
    public string? ClassTypeName { get; set; }
    public EntitlementKind Kind { get; set; }

    public int Granted { get; set; }
    public int Used { get; set; }
    public int Held { get; set; }
    public int Remaining { get; set; }

    public DateTime? ExpiresOn { get; set; }
    public bool IsExpired { get; set; }
    public decimal UnitValue { get; set; }

    public List<SessionCreditMovementDto> Movements { get; set; } = [];
}

public class SessionCreditMovementDto
{
    public Guid Id { get; set; }
    public SessionCreditMovementKind Kind { get; set; }
    public DateTime OccurredAt { get; set; }
    public int Quantity { get; set; }
    public int BalanceAfter { get; set; }
    public Guid? AppointmentId { get; set; }
    public Guid? ClassBookingId { get; set; }
    public string? Note { get; set; }
    public string? PerformedByName { get; set; }
    public string? Context { get; set; }
}

public class AdjustCreditsDto
{
    public Guid MemberId { get; set; }
    public Guid? SessionCreditId { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime? NewExpiryOn { get; set; }
}

public class CoachAssignmentDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public Guid StaffId { get; set; }
    public string? StaffName { get; set; }
    public Guid ClubId { get; set; }
    public DateTime AssignedOn { get; set; }
    public DateTime? EndedOn { get; set; }
    public string? EndReason { get; set; }
    public bool IsPrimary { get; set; }
}

// ── Exercises & workouts ─────────────────────────────────────────────────────

public class ExerciseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ExerciseCategory Category { get; set; }
    public string? MuscleGroups { get; set; }
    public string? Equipment { get; set; }
    public string? Instructions { get; set; }
    public string? VideoUrl { get; set; }
    public string? ImageUrl { get; set; }
    public string? ScalingOptions { get; set; }
    public bool TracksPersonalRecord { get; set; }
    public ScoreType? PrScoreType { get; set; }
    public bool IsSystemExercise { get; set; }
    public bool IsActive { get; set; }
}

public class WorkoutDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }
    public string? Summary { get; set; }
    public string? CoachNotes { get; set; }
    public ScoreType? ScoreType { get; set; }
    public string? ScoreUnit { get; set; }
    public int? TimeCapSeconds { get; set; }
    public bool IsBenchmark { get; set; }
    public string? BenchmarkName { get; set; }
    public int? EstimatedMinutes { get; set; }
    public bool IsTemplate { get; set; }
    public bool IsActive { get; set; }

    public List<WorkoutSectionDto> Sections { get; set; } = [];

    public int ResultCount { get; set; }
    public DateTime? LastPerformedOn { get; set; }
}

public class WorkoutSectionDto
{
    public Guid Id { get; set; }
    public Guid WorkoutId { get; set; }
    public string Title { get; set; } = string.Empty;
    public WorkoutSectionKind Kind { get; set; }
    public int DisplayOrder { get; set; }
    public int? Rounds { get; set; }
    public int? DurationSeconds { get; set; }
    public int? RestSeconds { get; set; }
    public ScoreType? ScoreType { get; set; }
    public string? Instructions { get; set; }

    public List<WorkoutMovementDto> Movements { get; set; } = [];
}

public class WorkoutMovementDto
{
    public Guid Id { get; set; }
    public Guid WorkoutSectionId { get; set; }
    public Guid? ExerciseId { get; set; }
    public string MovementName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int? Sets { get; set; }
    public string? Reps { get; set; }
    public decimal? LoadKg { get; set; }
    public decimal? LoadPercentOfMax { get; set; }
    public decimal? DistanceMetres { get; set; }
    public int? Calories { get; set; }
    public int? DurationSeconds { get; set; }
    public int? RestSeconds { get; set; }
    public string? Tempo { get; set; }
    public string? ScalingNote { get; set; }
}

public class ProgramTrackDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }
    public string? ColourHex { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPublic { get; set; }
    public DateTime? StartsOn { get; set; }
    public DateTime? EndsOn { get; set; }
    public bool IsActive { get; set; }

    public int PublishedDays { get; set; }
    public DateTime? NextUnpublishedOn { get; set; }
}

public class ProgramDayDto
{
    public Guid Id { get; set; }
    public Guid ProgramTrackId { get; set; }
    public string? TrackName { get; set; }
    public string? TrackColour { get; set; }
    public Guid? WorkoutId { get; set; }
    public string? WorkoutName { get; set; }
    public DateTime ScheduledOn { get; set; }
    public Guid? ClubId { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishAt { get; set; }
    public string? CoachBrief { get; set; }

    public WorkoutDto? Workout { get; set; }
    public int ResultCount { get; set; }
}

/// <summary>Today's programming across every track, which is what the whiteboard screen renders.</summary>
public class WodBoardDto
{
    public Guid ClubId { get; set; }
    public string ClubName { get; set; } = string.Empty;
    public DateTime ForDate { get; set; }
    public List<ProgramDayDto> Tracks { get; set; } = [];
    public List<LeaderboardEntryDto> TodaysLeaderboard { get; set; } = [];
}

// ── Results & leaderboards ───────────────────────────────────────────────────

public class WorkoutResultDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? MemberPhotoUrl { get; set; }

    public Guid? WorkoutId { get; set; }
    public string? WorkoutName { get; set; }
    public Guid? ClassOccurrenceId { get; set; }
    public Guid? ProgramTrackId { get; set; }
    public string? TrackName { get; set; }
    public Guid ClubId { get; set; }

    public DateTime PerformedOn { get; set; }
    public ScoreType ScoreType { get; set; }

    public int? TimeSeconds { get; set; }
    public int? Rounds { get; set; }
    public int? Reps { get; set; }
    public decimal? LoadKg { get; set; }
    public decimal? DistanceMetres { get; set; }
    public int? Calories { get; set; }
    public int? Points { get; set; }
    public bool? Passed { get; set; }

    public decimal NormalisedScore { get; set; }

    /// <summary>"4:32", "12 + 8", "102.5 kg" — formatted once on the server so every screen agrees.</summary>
    public string ScoreDisplay { get; set; } = string.Empty;

    public bool WasScaled { get; set; }
    public string? ScalingNote { get; set; }
    public bool DidNotFinish { get; set; }

    public string? MemberNote { get; set; }
    public string? CoachNote { get; set; }
    public bool IsPersonalRecord { get; set; }
    public string? EnteredByName { get; set; }
}

public class LogResultDto
{
    public Guid MemberId { get; set; }
    public Guid? WorkoutId { get; set; }
    public Guid? ClassOccurrenceId { get; set; }
    public Guid? AppointmentId { get; set; }
    public Guid? ProgramTrackId { get; set; }
    public Guid ClubId { get; set; }

    public DateTime? PerformedOn { get; set; }
    public ScoreType ScoreType { get; set; }

    public int? TimeSeconds { get; set; }
    public int? Rounds { get; set; }
    public int? Reps { get; set; }
    public decimal? LoadKg { get; set; }
    public decimal? DistanceMetres { get; set; }
    public int? Calories { get; set; }
    public int? Points { get; set; }
    public bool? Passed { get; set; }

    public bool WasScaled { get; set; }
    public string? ScalingNote { get; set; }
    public bool DidNotFinish { get; set; }
    public string? MemberNote { get; set; }
}

public class PersonalRecordDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public Guid? ExerciseId { get; set; }
    public Guid? WorkoutId { get; set; }
    public string RecordName { get; set; } = string.Empty;
    public ScoreType ScoreType { get; set; }
    public decimal Value { get; set; }
    public string? Unit { get; set; }
    public string ValueDisplay { get; set; } = string.Empty;
    public int? RepMax { get; set; }
    public DateTime AchievedOn { get; set; }
    public decimal? PreviousValue { get; set; }
    public DateTime? PreviousAchievedOn { get; set; }
    public decimal? Improvement { get; set; }
    public string? ImprovementDisplay { get; set; }
}

public class LeaderboardEntryDto
{
    public Guid Id { get; set; }
    public int Rank { get; set; }
    public Guid MemberId { get; set; }
    public string MemberDisplayName { get; set; } = string.Empty;
    public string? MemberPhotoUrl { get; set; }
    public decimal Score { get; set; }
    public string ScoreDisplay { get; set; } = string.Empty;
    public bool WasScaled { get; set; }
    public string? Division { get; set; }
    public Guid? WorkoutResultId { get; set; }
    public DateTime? AchievedOn { get; set; }
}

public class LeaderboardDto
{
    public string Title { get; set; } = string.Empty;
    public Guid? WorkoutId { get; set; }
    public Guid? ClassOccurrenceId { get; set; }
    public Guid? ChallengeId { get; set; }
    public Guid ClubId { get; set; }
    public string? Division { get; set; }
    public ScoreType ScoreType { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    public List<LeaderboardEntryDto> Entries { get; set; } = [];
    public LeaderboardEntryDto? ViewerEntry { get; set; }
    public int TotalParticipants { get; set; }
    public DateTime ComputedAt { get; set; }
}

// ── Effort & streaks ─────────────────────────────────────────────────────────

public class EffortSessionDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? MemberPhotoUrl { get; set; }
    public Guid ClubId { get; set; }
    public Guid? ClassOccurrenceId { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int DurationMinutes { get; set; }

    public int EffortPoints { get; set; }
    public int GreyMinutes { get; set; }
    public int BlueMinutes { get; set; }
    public int GreenMinutes { get; set; }
    public int YellowMinutes { get; set; }
    public int RedMinutes { get; set; }

    public int? AverageHeartRate { get; set; }
    public int? PeakHeartRate { get; set; }
    public int? CaloriesBurned { get; set; }
    public EffortZone PeakZone { get; set; }
    public string? DeviceType { get; set; }
}

public class AttendanceStreakDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string Cadence { get; set; } = string.Empty;
    public int CurrentCount { get; set; }
    public int LongestCount { get; set; }
    public DateTime StartedOn { get; set; }
    public DateTime LastQualifyingOn { get; set; }
    public DateTime? BrokenOn { get; set; }
    public int RequiredPerPeriod { get; set; }

    /// <summary>How many more visits this period keeps the streak alive.</summary>
    public int VisitsNeededThisPeriod { get; set; }
    public bool AtRiskOfBreaking { get; set; }
}

// ── Ranks ────────────────────────────────────────────────────────────────────

public class RankLadderDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }
    public string? Discipline { get; set; }
    public bool IsActive { get; set; }
    public List<RankLevelDto> Levels { get; set; } = [];
}

public class RankLevelDto
{
    public Guid Id { get; set; }
    public Guid RankLadderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Ordinal { get; set; }
    public string? ColourHex { get; set; }
    public string? BadgeUrl { get; set; }
    public int RequiredAttendances { get; set; }
    public int MinimumMonthsAtPrevious { get; set; }
    public string? RequirementsNote { get; set; }
    public decimal GradingFee { get; set; }
    public int? MinimumAge { get; set; }

    public int MembersAtThisRank { get; set; }
}

public class MemberRankDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? MemberPhotoUrl { get; set; }
    public Guid RankLadderId { get; set; }
    public string? LadderName { get; set; }
    public Guid RankLevelId { get; set; }
    public string RankName { get; set; } = string.Empty;
    public string? ColourHex { get; set; }
    public string? BadgeUrl { get; set; }

    public DateTime AwardedOn { get; set; }
    public string? AwardedByName { get; set; }
    public RankAwardStatus Status { get; set; }
    public bool IsCurrent { get; set; }
    public int AttendancesAtRank { get; set; }

    // Progress toward the next rung, which is the number a student actually wants.
    public string? NextRankName { get; set; }
    public int AttendancesRequired { get; set; }
    public int MonthsAtRank { get; set; }
    public int MonthsRequired { get; set; }
    public int ProgressPercent { get; set; }
    public bool IsEligibleForGrading { get; set; }

    public string? CertificateUrl { get; set; }
    public string? Note { get; set; }
}

public class GradingEventDto
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public Guid RankLadderId { get; set; }
    public string? LadderName { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime HeldOn { get; set; }
    public Guid? ExaminerStaffId { get; set; }
    public string? ExaminerName { get; set; }
    public string? ExternalExaminerName { get; set; }
    public int CandidateCount { get; set; }
    public int PassCount { get; set; }
    public decimal FeePerCandidate { get; set; }
    public string? Notes { get; set; }
    public bool IsCompleted { get; set; }

    public List<MemberRankDto> Candidates { get; set; } = [];
}

public class AwardRankDto
{
    public Guid MemberId { get; set; }
    public Guid RankLadderId { get; set; }
    public Guid RankLevelId { get; set; }
    public Guid? GradingEventId { get; set; }
    public DateTime? AwardedOn { get; set; }
    public string? Note { get; set; }
    public bool ChargeGradingFee { get; set; }
}

public class SkillClearanceDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public Guid ClubId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public DateTime ClearedOn { get; set; }
    public Guid ClearedByStaffId { get; set; }
    public string? ClearedByName { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public bool IsExpired { get; set; }
    public bool IsRevoked { get; set; }
    public string? RevokedReason { get; set; }
    public string? Note { get; set; }
}
