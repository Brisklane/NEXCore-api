using Fitness.Domain.Enums;

namespace Fitness.Application.DTOs;

// ── Assessments ──────────────────────────────────────────────────────────────

public class AssessmentTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }
    public string? Purpose { get; set; }
    public int DisplayOrder { get; set; }
    public int RecommendedIntervalDays { get; set; }
    public Guid? ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public bool IsSystemTemplate { get; set; }
    public bool IsActive { get; set; }

    public List<AssessmentMeasureDto> Measures { get; set; } = [];
    public int UsageCount { get; set; }
}

public class AssessmentMeasureDto
{
    public Guid Id { get; set; }
    public Guid AssessmentTemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public MeasureType MeasureType { get; set; }
    public MeasureDirection Direction { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string? Grouping { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public decimal? NormalLow { get; set; }
    public decimal? NormalHigh { get; set; }
    public string? Instructions { get; set; }
    public bool IsCalculated { get; set; }
    public string? CalculationNote { get; set; }
    public bool IsRequired { get; set; }
    public bool IsDeviceImported { get; set; }
    public string? DeviceFieldName { get; set; }
}

public class AssessmentDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public Guid ClubId { get; set; }
    public Guid? AssessmentTemplateId { get; set; }
    public string? TemplateName { get; set; }
    public Guid? StaffId { get; set; }
    public string? StaffName { get; set; }
    public Guid? AppointmentId { get; set; }

    public DateTime PerformedOn { get; set; }
    public string? Summary { get; set; }
    public string? Recommendations { get; set; }
    public string? DeviceSource { get; set; }
    public DateTime? NextDueOn { get; set; }
    public bool SharedWithMember { get; set; }
    public string? ReportUrl { get; set; }

    public List<AssessmentValueDto> Values { get; set; } = [];

    /// <summary>The assessment this one is compared against.</summary>
    public Guid? PreviousAssessmentId { get; set; }
    public DateTime? PreviousPerformedOn { get; set; }
    public int? DaysSincePrevious { get; set; }
}

public class AssessmentValueDto
{
    public Guid Id { get; set; }
    public Guid? AssessmentMeasureId { get; set; }
    public string MeasureName { get; set; } = string.Empty;
    public MeasureType MeasureType { get; set; }
    public MeasureDirection Direction { get; set; }
    public string? Unit { get; set; }

    public decimal? NumericValue { get; set; }
    public string? TextValue { get; set; }
    public bool? BooleanValue { get; set; }

    /// <summary>Converted to the member's unit preference, so the chart reads in their language.</summary>
    public string DisplayValue { get; set; } = string.Empty;

    public decimal? PreviousValue { get; set; }
    public decimal? Change { get; set; }
    public decimal? ChangePercent { get; set; }

    /// <summary>Whether the change is an improvement, which depends on the measure's direction.</summary>
    public bool? IsImprovement { get; set; }

    public string? NormBand { get; set; }
    public int? Percentile { get; set; }
    public string? Grouping { get; set; }
    public string? Note { get; set; }
    public int DisplayOrder { get; set; }
}

public class RecordAssessmentDto
{
    public Guid MemberId { get; set; }
    public Guid ClubId { get; set; }
    public Guid? AssessmentTemplateId { get; set; }
    public Guid? StaffId { get; set; }
    public Guid? AppointmentId { get; set; }
    public DateTime? PerformedOn { get; set; }

    public string? Summary { get; set; }
    public string? Recommendations { get; set; }
    public string? DeviceSource { get; set; }
    public string? DeviceReference { get; set; }
    public bool SharedWithMember { get; set; } = true;

    public List<RecordMeasureValueDto> Values { get; set; } = [];
}

public class RecordMeasureValueDto
{
    public Guid? AssessmentMeasureId { get; set; }
    public string MeasureName { get; set; } = string.Empty;
    public decimal? NumericValue { get; set; }
    public string? TextValue { get; set; }
    public bool? BooleanValue { get; set; }
    public string? Unit { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// One measure charted over time, which is the shape the progress screen actually wants — not a
/// list of assessments the client has to read down a column of.
/// </summary>
public class ProgressSeriesDto
{
    public Guid MemberId { get; set; }
    public string MeasureName { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public MeasureDirection Direction { get; set; }

    public List<ProgressPointDto> Points { get; set; } = [];

    public decimal? First { get; set; }
    public decimal? Latest { get; set; }
    public decimal? Best { get; set; }
    public decimal? TotalChange { get; set; }
    public decimal? TotalChangePercent { get; set; }
    public bool? IsImproving { get; set; }

    public decimal? NormalLow { get; set; }
    public decimal? NormalHigh { get; set; }
    public decimal? GoalValue { get; set; }
    public DateTime? GoalDate { get; set; }
}

public class ProgressPointDto
{
    public DateTime On { get; set; }
    public decimal Value { get; set; }
    public Guid? AssessmentId { get; set; }
    public string? Note { get; set; }
}

public class ProgressPhotoDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public Guid? AssessmentId { get; set; }
    public DateTime TakenOn { get; set; }
    public string Pose { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public bool ConsentGiven { get; set; }
    public bool MayUseInMarketing { get; set; }
    public string? TakenByName { get; set; }
    public string? Note { get; set; }
}

public class MemberGoalDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? MeasureName { get; set; }
    public string? Unit { get; set; }
    public decimal? StartValue { get; set; }
    public decimal? TargetValue { get; set; }
    public decimal? CurrentValue { get; set; }
    public DateTime SetOn { get; set; }
    public DateTime? TargetDate { get; set; }
    public DateTime? AchievedOn { get; set; }
    public GoalStatus Status { get; set; }
    public int ProgressPercent { get; set; }
    public int? DaysRemaining { get; set; }
    public string? SetByName { get; set; }
    public string? WhyItMatters { get; set; }
    public bool IsOnTrack { get; set; }
}

// ── Coaching ─────────────────────────────────────────────────────────────────

public class NutritionPlanDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public Guid? StaffId { get; set; }
    public string? StaffName { get; set; }
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
    public string? Disclaimer { get; set; }
    public bool IsActive { get; set; }
}

public class HabitTrackerDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public Guid? NutritionPlanId { get; set; }
    public Guid? StaffId { get; set; }

    public string HabitName { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public decimal? DailyTarget { get; set; }

    public DateTime StartsOn { get; set; }
    public DateTime? EndsOn { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int AdherencePercent { get; set; }
    public bool IsActive { get; set; }

    public List<HabitEntryDto> RecentEntries { get; set; } = [];
    public bool LoggedToday { get; set; }
}

public class HabitEntryDto
{
    public Guid Id { get; set; }
    public Guid HabitTrackerId { get; set; }
    public DateTime ForDate { get; set; }
    public decimal? Value { get; set; }
    public bool Completed { get; set; }
    public string? Note { get; set; }
    public DateTime LoggedAt { get; set; }
}

public class CoachCheckInDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? MemberPhotoUrl { get; set; }
    public Guid StaffId { get; set; }
    public string? StaffName { get; set; }
    public Guid ClubId { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime DueOn { get; set; }

    public DateTime? MemberSubmittedAt { get; set; }
    public string? MemberResponse { get; set; }
    public int? EnergyRating { get; set; }
    public int? SleepRating { get; set; }
    public int? StressRating { get; set; }
    public int? AdherenceRating { get; set; }

    public DateTime? CoachRepliedAt { get; set; }
    public string? CoachResponse { get; set; }
    public string? AdjustmentsMade { get; set; }

    public bool IsComplete { get; set; }
    public bool WasMissed { get; set; }
    public bool AwaitingCoach { get; set; }
    public bool AwaitingMember { get; set; }
    public bool IsOverdue { get; set; }
}

// ── Compliance ───────────────────────────────────────────────────────────────

public class WaiverTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public Guid? ClubId { get; set; }
    public string? CountryCode { get; set; }
    public string? LanguageCode { get; set; }
    public Guid? ClassTypeId { get; set; }
    public string? ActivityScope { get; set; }

    public string BodyHtml { get; set; } = string.Empty;
    public string? ConsentClausesJson { get; set; }

    public bool RequiresGuardianSignature { get; set; }
    public int? GuardianRequiredBelowAge { get; set; }
    public int ValidForDays { get; set; }
    public bool BlocksAccess { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsPublished { get; set; }
    public bool RequiresResignOnNewVersion { get; set; }
    public bool IsActive { get; set; }

    public int SignedCount { get; set; }
    public int OutstandingCount { get; set; }
}

public class WaiverSignatureDto
{
    public Guid Id { get; set; }
    public Guid WaiverTemplateId { get; set; }
    public string? TemplateName { get; set; }
    public int TemplateVersion { get; set; }

    public Guid? MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? SignerName { get; set; }
    public string? SignerEmail { get; set; }

    public Guid ClubId { get; set; }
    public SignatureStatus Status { get; set; }
    public DateTime? SignedAt { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public bool IsExpired { get; set; }

    public string? GuardianName { get; set; }
    public string? GuardianRelationship { get; set; }
    public string? SignatureImageUrl { get; set; }
    public string? DocumentUrl { get; set; }
    public string? CapturedVia { get; set; }
}

public class SignWaiverDto
{
    public Guid WaiverTemplateId { get; set; }
    public Guid ClubId { get; set; }

    public Guid? MemberId { get; set; }
    public string? SignerName { get; set; }
    public string? SignerEmail { get; set; }
    public string? SignerPhone { get; set; }
    public DateTime? SignerDateOfBirth { get; set; }

    public Guid? GuestVisitId { get; set; }
    public Guid? DayPassId { get; set; }

    public string? SignatureImageUrl { get; set; }
    public string? GuardianName { get; set; }
    public string? GuardianRelationship { get; set; }
    public string? GuardianSignatureUrl { get; set; }

    public string? ConsentAnswersJson { get; set; }
    public string? CapturedVia { get; set; }
}

public class HealthScreeningDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public Guid ClubId { get; set; }

    public string TemplateName { get; set; } = "PAR-Q+";
    public int TemplateVersion { get; set; }
    public DateTime CompletedAt { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public bool IsExpired { get; set; }

    public bool RequiresClearance { get; set; }
    public ClearanceStatus ClearanceStatus { get; set; }
    public string? RiskSummary { get; set; }

    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
    public string? CapturedVia { get; set; }

    public List<HealthScreeningAnswerDto> Answers { get; set; } = [];
}

public class HealthScreeningAnswerDto
{
    public Guid Id { get; set; }
    public int QuestionNumber { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public ScreeningAnswerKind AnswerKind { get; set; }
    public bool? BooleanAnswer { get; set; }
    public string? TextAnswer { get; set; }
    public decimal? NumericAnswer { get; set; }
    public DateTime? DateAnswer { get; set; }
    public bool IsGatingQuestion { get; set; }
    public string? FollowUpAnswer { get; set; }
}

/// <summary>The blank form the join wizard and the app render, so the questions live server-side.</summary>
public class HealthScreeningFormDto
{
    public string TemplateName { get; set; } = string.Empty;
    public int TemplateVersion { get; set; }
    public string? Introduction { get; set; }
    public string? GatingMessage { get; set; }
    public List<HealthScreeningAnswerDto> Questions { get; set; } = [];
}

public class MedicalClearanceDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public Guid ClubId { get; set; }
    public Guid? HealthScreeningId { get; set; }

    public ClearanceStatus Status { get; set; }
    public DateTime RequestedOn { get; set; }
    public DateTime? SubmittedOn { get; set; }
    public DateTime? ApprovedOn { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public bool IsExpired { get; set; }

    public string? PractitionerName { get; set; }
    public string? PractitionerRegistration { get; set; }
    public string? PracticeName { get; set; }
    public string? Restrictions { get; set; }

    public Guid? DocumentId { get; set; }
    public string? DocumentUrl { get; set; }
    public string? ApprovedByName { get; set; }
    public string? RejectionReason { get; set; }
    public bool BlocksParticipation { get; set; }
}

public class SubmitClearanceDto
{
    public Guid MemberId { get; set; }
    public Guid? ClearanceId { get; set; }
    public string? PractitionerName { get; set; }
    public string? PractitionerRegistration { get; set; }
    public string? PracticeName { get; set; }
    public string? Restrictions { get; set; }
    public Guid? DocumentId { get; set; }
    public DateTime? ExpiresOn { get; set; }
}

public class ReviewClearanceDto
{
    public Guid ClearanceId { get; set; }
    public bool Approve { get; set; }
    public string? Note { get; set; }
    public string? Restrictions { get; set; }
    public DateTime? ExpiresOn { get; set; }
}
