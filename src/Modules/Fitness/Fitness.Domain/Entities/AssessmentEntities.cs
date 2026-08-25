using Fitness.Domain.Enums;
using Nexcore.SharedKernel;

namespace Fitness.Domain.Entities;

/// <summary>
/// A named set of measures taken together — the new-member baseline, the quarterly review, a
/// sport-specific screen.
///
/// A template rather than a fixed form because clubs measure different things, and because a
/// trainer who cannot add "ankle dorsiflexion" to their own protocol will keep it in a notebook,
/// where it helps nobody.
/// </summary>
public class AssessmentTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }

    public string? Purpose { get; set; }
    public int DisplayOrder { get; set; }

    /// <summary>Suggested gap before the next one, which is what drives the re-test reminder.</summary>
    public int RecommendedIntervalDays { get; set; } = 90;

    /// <summary>Assessments can be a paid service rather than a free add-on.</summary>
    public Guid? ServiceId { get; set; }

    public bool IsSystemTemplate { get; set; }

    public ICollection<AssessmentMeasure> Measures { get; set; } = [];
}

/// <summary>
/// One thing measured, with its unit, its direction and its normal range.
///
/// The direction matters: a progress arrow that points up for body fat is worse than no arrow,
/// and a range-based measure like blood pressure is not "higher is better" in either direction.
/// </summary>
public class AssessmentMeasure : BaseEntity
{
    public Guid AssessmentTemplateId { get; set; }
    public AssessmentTemplate? AssessmentTemplate { get; set; }

    public string Name { get; set; } = string.Empty;
    public MeasureType MeasureType { get; set; } = MeasureType.Weight;
    public MeasureDirection Direction { get; set; } = MeasureDirection.Neutral;

    /// <summary>Stored unit. Display converts to the member's preference rather than re-storing.</summary>
    public string Unit { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }
    public string? Grouping { get; set; }

    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }

    /// <summary>Healthy band, when there is one, so a result can be shown in context.</summary>
    public decimal? NormalLow { get; set; }
    public decimal? NormalHigh { get; set; }

    public string? Instructions { get; set; }

    /// <summary>Derived rather than entered — BMI from height and weight, for instance.</summary>
    public bool IsCalculated { get; set; }
    public string? CalculationNote { get; set; }

    public bool IsRequired { get; set; }

    /// <summary>Imported from a body-composition scanner rather than typed in.</summary>
    public bool IsDeviceImported { get; set; }
    public string? DeviceFieldName { get; set; }
}

/// <summary>One assessment on one date, and everything measured during it.</summary>
public class Assessment : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid ClubId { get; set; }
    public Guid? AssessmentTemplateId { get; set; }
    public AssessmentTemplate? AssessmentTemplate { get; set; }

    public Guid? StaffId { get; set; }
    public Guid? AppointmentId { get; set; }

    public DateTime PerformedOn { get; set; } = DateTime.UtcNow;

    public string? Summary { get; set; }
    public string? Recommendations { get; set; }

    /// <summary>Set when values came from a scanner rather than being typed.</summary>
    public string? DeviceSource { get; set; }
    public string? DeviceReference { get; set; }

    /// <summary>Nudge for the re-test, so an assessment is a series rather than a one-off.</summary>
    public DateTime? NextDueOn { get; set; }

    public bool SharedWithMember { get; set; } = true;
    public string? ReportUrl { get; set; }

    public ICollection<AssessmentValue> Values { get; set; } = [];
}

/// <summary>One measured number, with the change since last time already worked out.</summary>
public class AssessmentValue : BaseEntity
{
    public Guid AssessmentId { get; set; }
    public Assessment? Assessment { get; set; }

    public Guid? AssessmentMeasureId { get; set; }

    /// <summary>Kept flat so a historical chart still reads correctly if the measure is renamed.</summary>
    public string MeasureName { get; set; } = string.Empty;
    public MeasureType MeasureType { get; set; }
    public string? Unit { get; set; }

    public decimal? NumericValue { get; set; }
    public string? TextValue { get; set; }
    public bool? BooleanValue { get; set; }

    /// <summary>Change against the previous assessment, stored so a list does not need a self-join per row.</summary>
    public decimal? PreviousValue { get; set; }
    public decimal? Change { get; set; }
    public decimal? ChangePercent { get; set; }

    /// <summary>Where it sits against the norm for their age and sex, when a norm exists.</summary>
    public string? NormBand { get; set; }
    public int? Percentile { get; set; }

    public string? Note { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// A photograph of a member's body over time.
///
/// Special-category, consent-gated and excluded from any export the member has not explicitly
/// asked for. Held separately from ordinary documents so that access can be restricted without
/// restricting their contract PDF as well.
/// </summary>
public class ProgressPhoto : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid? AssessmentId { get; set; }

    public DateTime TakenOn { get; set; } = DateTime.UtcNow;

    /// <summary>Front, side, back — so a comparison lines up like with like.</summary>
    public string Pose { get; set; } = "Front";

    public string ImageUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }

    /// <summary>Explicit consent to hold it. Absent means it should not have been taken.</summary>
    public bool ConsentGiven { get; set; }
    public DateTime? ConsentGivenOn { get; set; }

    /// <summary>Never shown outside the member and their coach without separate marketing consent.</summary>
    public bool MayUseInMarketing { get; set; }

    public Guid? TakenByStaffId { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// Something a member is trying to achieve, with a number and a date on it.
///
/// Vague goals are unmeasurable and therefore unmotivating; the model insists on a target value
/// and a target date so progress can actually be drawn.
/// </summary>
public class MemberGoal : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>What is being moved — a measure, a lift, a class count, a weight.</summary>
    public string? MeasureName { get; set; }
    public string? Unit { get; set; }

    public decimal? StartValue { get; set; }
    public decimal? TargetValue { get; set; }
    public decimal? CurrentValue { get; set; }

    public DateTime SetOn { get; set; } = DateTime.UtcNow;
    public DateTime? TargetDate { get; set; }
    public DateTime? AchievedOn { get; set; }

    public GoalStatus Status { get; set; } = GoalStatus.Active;

    /// <summary>0–100, recomputed as the current value moves.</summary>
    public int ProgressPercent { get; set; }

    public Guid? SetByStaffId { get; set; }
    public string? WhyItMatters { get; set; }
}

/// <summary>
/// A nutrition plan assigned to a member.
///
/// Coaching support, explicitly not dietetics: no diagnosis, no prescription, no clinical claims.
/// The disclaimer is part of the template rather than something a club is trusted to remember.
/// </summary>
public class NutritionPlan : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid? StaffId { get; set; }
    public Guid ClubId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime StartsOn { get; set; }
    public DateTime? EndsOn { get; set; }

    public int? DailyCalories { get; set; }
    public int? ProteinGrams { get; set; }
    public int? CarbGrams { get; set; }
    public int? FatGrams { get; set; }
    public int? FibreGrams { get; set; }
    public int? WaterMillilitres { get; set; }

    public string? MealGuidance { get; set; }
    public string? Restrictions { get; set; }
    public string? SupplementNotes { get; set; }

    /// <summary>Shown to the member with the plan. Not optional.</summary>
    public string? Disclaimer { get; set; }
}

/// <summary>
/// A habit a member is tracking — water, sleep, steps, protein, showing up.
///
/// Adherence is the metric coaching is judged on, and a streak the member can see is worth more
/// than a spreadsheet the coach can see.
/// </summary>
public class HabitTracker : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid? NutritionPlanId { get; set; }
    public Guid? StaffId { get; set; }

    public string HabitName { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public decimal? DailyTarget { get; set; }

    public DateTime StartsOn { get; set; }
    public DateTime? EndsOn { get; set; }

    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }

    /// <summary>Percentage of days hit since the tracker started.</summary>
    public int AdherencePercent { get; set; }

    public ICollection<HabitEntry> Entries { get; set; } = [];
}

/// <summary>One day's answer on a habit tracker.</summary>
public class HabitEntry : BaseEntity
{
    public Guid HabitTrackerId { get; set; }
    public HabitTracker? HabitTracker { get; set; }

    public Guid MemberId { get; set; }

    public DateTime ForDate { get; set; }
    public decimal? Value { get; set; }
    public bool Completed { get; set; }

    public string? Note { get; set; }
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A coach's periodic check-in with a client: how the week went, what the coach said back.
///
/// Held as a record rather than a message thread because the adherence score and the reply
/// together are the coaching product, and both need to be reportable.
/// </summary>
public class CoachCheckIn : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid StaffId { get; set; }
    public Guid ClubId { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime DueOn { get; set; }

    public DateTime? MemberSubmittedAt { get; set; }
    public string? MemberResponse { get; set; }

    /// <summary>1–10 self-rated, which is a surprisingly good early churn signal on its own.</summary>
    public int? EnergyRating { get; set; }
    public int? SleepRating { get; set; }
    public int? StressRating { get; set; }
    public int? AdherenceRating { get; set; }

    public DateTime? CoachRepliedAt { get; set; }
    public string? CoachResponse { get; set; }
    public string? AdjustmentsMade { get; set; }

    public bool IsComplete { get; set; }
    public bool WasMissed { get; set; }
}
