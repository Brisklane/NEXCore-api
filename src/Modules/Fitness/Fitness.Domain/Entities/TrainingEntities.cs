using Fitness.Domain.Enums;
using Nexcore.SharedKernel;

namespace Fitness.Domain.Entities;

/// <summary>
/// A movement. Seeded with the standard set every gym uses, and extended by the tenant with their
/// own — which is why it is a table rather than an enum.
/// </summary>
public class Exercise : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ExerciseCategory Category { get; set; } = ExerciseCategory.Barbell;

    /// <summary>Comma-separated: chest, triceps, anterior delts.</summary>
    public string? MuscleGroups { get; set; }
    public string? Equipment { get; set; }

    public string? Instructions { get; set; }
    public string? VideoUrl { get; set; }
    public string? ImageUrl { get; set; }

    /// <summary>Easier versions a coach can prescribe — box squat for squat, ring row for pull-up.</summary>
    public string? ScalingOptions { get; set; }

    /// <summary>Named lifts and benchmark movements a personal record is tracked against.</summary>
    public bool TracksPersonalRecord { get; set; }
    public ScoreType? PrScoreType { get; set; }

    /// <summary>Seeded by NexCore rather than created by the club, so an update can refresh it safely.</summary>
    public bool IsSystemExercise { get; set; }
}

/// <summary>
/// A session's programming — what the class or the client is actually doing.
///
/// One model covers a box's daily WOD and a trainer's twelve-week block, because structurally
/// they are the same thing: an ordered set of sections, each with movements and a way of being
/// scored.
/// </summary>
public class Workout : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }

    public string? Summary { get; set; }
    public string? CoachNotes { get; set; }

    /// <summary>How the whole workout is scored, when it has a single score.</summary>
    public ScoreType? ScoreType { get; set; }
    public string? ScoreUnit { get; set; }

    /// <summary>Time cap in seconds, for a for-time piece.</summary>
    public int? TimeCapSeconds { get; set; }

    /// <summary>
    /// A named benchmark — Fran, Murph, a 1RM test — so results are comparable across years
    /// rather than across a workout somebody wrote once.
    /// </summary>
    public bool IsBenchmark { get; set; }
    public string? BenchmarkName { get; set; }

    /// <summary>Estimated minutes, so a coach can see it fits the class.</summary>
    public int? EstimatedMinutes { get; set; }

    /// <summary>A reusable template rather than a specific day's work.</summary>
    public bool IsTemplate { get; set; }

    public ICollection<WorkoutSection> Sections { get; set; } = [];
}

/// <summary>A block within a workout — the strength piece, the metcon, the cool-down.</summary>
public class WorkoutSection : BaseEntity
{
    public Guid WorkoutId { get; set; }
    public Workout? Workout { get; set; }

    public string Title { get; set; } = string.Empty;
    public WorkoutSectionKind Kind { get; set; } = WorkoutSectionKind.Strength;
    public int DisplayOrder { get; set; }

    public int? Rounds { get; set; }

    /// <summary>Duration for an EMOM, AMRAP, Tabata or interval block.</summary>
    public int? DurationSeconds { get; set; }
    public int? RestSeconds { get; set; }

    public ScoreType? ScoreType { get; set; }
    public string? Instructions { get; set; }

    public ICollection<WorkoutMovement> Movements { get; set; } = [];
}

/// <summary>One prescribed movement inside a section, with the load and volume it is done at.</summary>
public class WorkoutMovement : BaseEntity
{
    public Guid WorkoutSectionId { get; set; }
    public WorkoutSection? WorkoutSection { get; set; }

    public Guid? ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }

    /// <summary>Kept even when linked, so a workout still reads correctly if the exercise is renamed.</summary>
    public string MovementName { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public int? Sets { get; set; }
    public string? Reps { get; set; }

    public decimal? LoadKg { get; set; }

    /// <summary>Percentage of a one-rep max, which is how most strength work is actually written.</summary>
    public decimal? LoadPercentOfMax { get; set; }

    public decimal? DistanceMetres { get; set; }
    public int? Calories { get; set; }
    public int? DurationSeconds { get; set; }
    public int? RestSeconds { get; set; }

    /// <summary>3-1-2-0 style tempo prescription.</summary>
    public string? Tempo { get; set; }

    public string? ScalingNote { get; set; }
}

/// <summary>
/// A stream of programming a club or a client follows — "RX", "Scaled", "Beginner strength".
///
/// A box publishes two or three tracks off one day's programming, and every athlete follows one.
/// A leaderboard that mixes them is worse than no leaderboard.
/// </summary>
public class ProgramTrack : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }

    public string? ColourHex { get; set; }
    public int DisplayOrder { get; set; }

    /// <summary>Members can see this track's programming without being enrolled on it.</summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>A finite block (a twelve-week cycle) rather than an ongoing track.</summary>
    public DateTime? StartsOn { get; set; }
    public DateTime? EndsOn { get; set; }

    public ICollection<ProgramDay> Days { get; set; } = [];
}

/// <summary>What a track is doing on a given day — the WOD.</summary>
public class ProgramDay : BaseEntity
{
    public Guid ProgramTrackId { get; set; }
    public ProgramTrack? ProgramTrack { get; set; }

    public Guid? WorkoutId { get; set; }
    public Workout? Workout { get; set; }

    public DateTime ScheduledOn { get; set; }
    public Guid? ClubId { get; set; }

    /// <summary>Members can only see it once it is published, so tomorrow's WOD stays a surprise.</summary>
    public bool IsPublished { get; set; }
    public DateTime? PublishAt { get; set; }

    public string? CoachBrief { get; set; }
}

/// <summary>
/// What a member actually did, and how it went.
///
/// The scoring fields are all present rather than one polymorphic value column because a
/// leaderboard has to sort them, and sorting a string that sometimes holds "4:32" and sometimes
/// holds "142" is how leaderboards end up wrong.
/// </summary>
public class WorkoutResult : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid? WorkoutId { get; set; }
    public Workout? Workout { get; set; }

    public Guid? ClassOccurrenceId { get; set; }
    public Guid? AppointmentId { get; set; }
    public Guid? ProgramTrackId { get; set; }
    public Guid ClubId { get; set; }

    public DateTime PerformedOn { get; set; } = DateTime.UtcNow;

    public ScoreType ScoreType { get; set; } = ScoreType.ForTime;

    /// <summary>Seconds, for a for-time result.</summary>
    public int? TimeSeconds { get; set; }
    public int? Rounds { get; set; }
    public int? Reps { get; set; }
    public decimal? LoadKg { get; set; }
    public decimal? DistanceMetres { get; set; }
    public int? Calories { get; set; }
    public int? Points { get; set; }
    public bool? Passed { get; set; }

    /// <summary>
    /// A single sortable number derived from whichever field applies, so one index serves every
    /// leaderboard. Lower is better for time; higher for everything else, and the score type says which.
    /// </summary>
    public decimal NormalisedScore { get; set; }

    /// <summary>They scaled it. A scaled result must never outrank an RX one.</summary>
    public bool WasScaled { get; set; }
    public string? ScalingNote { get; set; }

    /// <summary>Did not finish inside the cap.</summary>
    public bool DidNotFinish { get; set; }

    public string? MemberNote { get; set; }
    public string? CoachNote { get; set; }

    /// <summary>Set by the PR detector, so the celebration fires exactly once.</summary>
    public bool IsPersonalRecord { get; set; }

    /// <summary>Entered by a coach on the member's behalf, rather than by the member.</summary>
    public Guid? EnteredByStaffId { get; set; }
}

/// <summary>
/// A member's best, per movement or per benchmark.
///
/// Held rather than computed because it is read on every workout submission to decide whether to
/// celebrate, and because the history of *when* each PR landed is the progress story members
/// actually care about.
/// </summary>
public class PersonalRecord : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid? ExerciseId { get; set; }
    public Guid? WorkoutId { get; set; }

    /// <summary>"Back squat", "Fran" — kept flat so a PR survives its source being renamed.</summary>
    public string RecordName { get; set; } = string.Empty;

    public ScoreType ScoreType { get; set; }
    public decimal Value { get; set; }
    public string? Unit { get; set; }

    /// <summary>Reps the record was set at — a 5RM and a 1RM are different records.</summary>
    public int? RepMax { get; set; }

    public DateTime AchievedOn { get; set; }
    public Guid? WorkoutResultId { get; set; }

    /// <summary>The previous best, so the app can show "+5 kg" rather than just a number.</summary>
    public decimal? PreviousValue { get; set; }
    public DateTime? PreviousAchievedOn { get; set; }
}

/// <summary>
/// A cached leaderboard position.
///
/// Materialised because a class leaderboard is rendered on a wall screen that refreshes every few
/// seconds, and recomputing a ranking over every result in the club each time is the wrong shape
/// of query to put behind a television.
/// </summary>
public class LeaderboardEntry : BaseEntity
{
    public Guid ClubId { get; set; }
    public Guid? WorkoutId { get; set; }
    public Guid? ClassOccurrenceId { get; set; }
    public Guid? ChallengeId { get; set; }

    public Guid MemberId { get; set; }
    public string MemberDisplayName { get; set; } = string.Empty;
    public string? MemberPhotoUrl { get; set; }

    public int Rank { get; set; }
    public decimal Score { get; set; }
    public string? ScoreDisplay { get; set; }

    public bool WasScaled { get; set; }

    /// <summary>Division the ranking is within — gender, age band, RX/scaled.</summary>
    public string? Division { get; set; }

    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
    public Guid? WorkoutResultId { get; set; }
}

/// <summary>
/// One session's worth of heart-rate effort, from a wearable or a smartwatch.
///
/// Zone minutes and effort points are what challenges, streaks and the in-club display all run
/// on, so they are stored even when no raw heart-rate trace is kept.
/// </summary>
public class EffortSession : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid ClubId { get; set; }
    public Guid? ClassOccurrenceId { get; set; }
    public Guid? AppointmentId { get; set; }
    public Guid? CheckInId { get; set; }

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

    public EffortZone PeakZone { get; set; } = EffortZone.Green;

    /// <summary>Which device it came from, for support and for de-duplication.</summary>
    public string? DeviceType { get; set; }
    public string? ExternalReference { get; set; }
}

/// <summary>
/// A run of consecutive periods a member has attended in. The thing members will not break, which
/// makes it one of the cheapest retention mechanics there is.
/// </summary>
public class AttendanceStreak : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    /// <summary>Daily, weekly or monthly, so "four weeks running" and "nine days running" both work.</summary>
    public string Cadence { get; set; } = "Weekly";

    public int CurrentCount { get; set; }
    public int LongestCount { get; set; }

    public DateTime StartedOn { get; set; }
    public DateTime LastQualifyingOn { get; set; }
    public DateTime? BrokenOn { get; set; }

    /// <summary>Visits needed in a period for it to count. A weekly streak usually needs two or three.</summary>
    public int RequiredPerPeriod { get; set; } = 1;
}

/// <summary>
/// A grading ladder, for martial-arts academies and any club that awards levels.
///
/// A small model that wins a whole vertical: a karate or BJJ academy will not buy software that
/// cannot track belts, no matter how good the billing is.
/// </summary>
public class RankLadder : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }

    /// <summary>The discipline it belongs to — BJJ, Karate, Taekwondo, swimming stages.</summary>
    public string? Discipline { get; set; }

    public ICollection<RankLevel> Levels { get; set; } = [];
}

/// <summary>One rung — White belt, Blue belt, Stage 4 — and what it takes to earn it.</summary>
public class RankLevel : BaseEntity
{
    public Guid RankLadderId { get; set; }
    public RankLadder? RankLadder { get; set; }

    public string Name { get; set; } = string.Empty;
    public int Ordinal { get; set; }
    public string? ColourHex { get; set; }
    public string? BadgeUrl { get; set; }

    /// <summary>Classes that must be attended at the previous rank before grading.</summary>
    public int RequiredAttendances { get; set; }

    /// <summary>Months that must be served at the previous rank.</summary>
    public int MinimumMonthsAtPrevious { get; set; }

    /// <summary>Skills the coach must sign off before grading is allowed.</summary>
    public string? RequirementsNote { get; set; }

    public decimal GradingFee { get; set; }
    public int? MinimumAge { get; set; }
}

/// <summary>Where a member currently is on a ladder, and how they got there.</summary>
public class MemberRank : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid RankLadderId { get; set; }
    public Guid RankLevelId { get; set; }
    public RankLevel? RankLevel { get; set; }

    public DateTime AwardedOn { get; set; }
    public Guid? AwardedByStaffId { get; set; }
    public Guid? GradingEventId { get; set; }

    public RankAwardStatus Status { get; set; } = RankAwardStatus.Awarded;

    /// <summary>False once they are graded past it; the row stays as history.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Attendances logged at this rank, counting toward the next.</summary>
    public int AttendancesAtRank { get; set; }

    public string? CertificateUrl { get; set; }
    public string? Note { get; set; }
}

/// <summary>A grading day: who was examined, by whom, and who passed.</summary>
public class GradingEvent : BaseEntity
{
    public Guid ClubId { get; set; }
    public Guid RankLadderId { get; set; }

    public string Name { get; set; } = string.Empty;
    public DateTime HeldOn { get; set; }

    public Guid? ExaminerStaffId { get; set; }
    public string? ExternalExaminerName { get; set; }

    public int CandidateCount { get; set; }
    public int PassCount { get; set; }

    public decimal FeePerCandidate { get; set; }
    public string? Notes { get; set; }

    public bool IsCompleted { get; set; }
}

/// <summary>
/// A coach signing off that a member is cleared to do something.
///
/// The booking engine reads it: a class type that requires clearance simply will not accept a
/// booking from someone who has not got it, which is a safety control rather than a formality.
/// </summary>
public class SkillClearance : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid ClubId { get; set; }

    /// <summary>"Barbell snatch", "Reformer intermediate", "Deep-water competent".</summary>
    public string SkillName { get; set; } = string.Empty;

    public DateTime ClearedOn { get; set; } = DateTime.UtcNow;
    public Guid ClearedByStaffId { get; set; }

    /// <summary>Some clearances lapse and must be re-assessed.</summary>
    public DateTime? ExpiresOn { get; set; }

    public bool IsRevoked { get; set; }
    public string? RevokedReason { get; set; }

    public string? Note { get; set; }
}
