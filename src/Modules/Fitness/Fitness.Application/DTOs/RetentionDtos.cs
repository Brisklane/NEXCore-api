using Fitness.Domain.Enums;

namespace Fitness.Application.DTOs;

// ── Churn ────────────────────────────────────────────────────────────────────

public class ChurnScoreDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string? MemberNumber { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public Guid ClubId { get; set; }

    public DateTime ComputedOn { get; set; }
    public ChurnRiskBand Band { get; set; }
    public int Score { get; set; }
    public ChurnRiskBand? PreviousBand { get; set; }
    public int? PreviousScore { get; set; }
    public bool BandWorsened { get; set; }

    public int DaysSinceLastVisit { get; set; }
    public decimal VisitsPerWeekNow { get; set; }
    public decimal VisitsPerWeekBaseline { get; set; }
    public int TenureDays { get; set; }
    public bool HasUpcomingBooking { get; set; }
    public bool HasOutstandingBalance { get; set; }
    public bool HasFailedPayment { get; set; }
    public int DaysToContractEnd { get; set; }

    public Guid? OwnerStaffId { get; set; }
    public string? OwnerStaffName { get; set; }
    public bool IsActioned { get; set; }
    public DateTime? ActionedOn { get; set; }

    public decimal MonthlyValue { get; set; }
    public decimal LifetimeValue { get; set; }
    public string? PlanName { get; set; }

    public List<ChurnFactorDto> Factors { get; set; } = [];

    /// <summary>The single best reason to lead the call with.</summary>
    public string? HeadlineReason { get; set; }
    public string? SuggestedAction { get; set; }
}

public class ChurnFactorDto
{
    public ChurnFactorKind Kind { get; set; }
    public int Weight { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public string? SuggestedAction { get; set; }
}

/// <summary>The at-risk board: who to call today, why, and who owns them.</summary>
public class RetentionBoardDto
{
    public Guid? ClubId { get; set; }
    public string? ClubName { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastScoredAt { get; set; }

    public int HealthyCount { get; set; }
    public int WatchCount { get; set; }
    public int AtRiskCount { get; set; }
    public int CriticalCount { get; set; }

    public decimal ValueAtRisk { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>Members whose band got worse at the last run — the newly urgent.</summary>
    public int NewlyAtRisk { get; set; }

    public List<ChurnScoreDto> Members { get; set; } = [];
    public List<RetentionTaskDto> OpenTasks { get; set; } = [];

    // How well the club is actually doing at this, rather than just how many are at risk.
    public int ContactedThisWeek { get; set; }
    public int RecoveredThisMonth { get; set; }
    public int SavedThisMonth { get; set; }
    public int SaveRatePercent { get; set; }
}

public class RetentionTaskDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string? MemberPhone { get; set; }
    public string? PhotoUrl { get; set; }
    public Guid ClubId { get; set; }

    public Guid? AssignedStaffId { get; set; }
    public string? AssignedStaffName { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public string? Trigger { get; set; }
    public Guid? ChurnScoreId { get; set; }

    public DateTime DueOn { get; set; }
    public int Priority { get; set; }
    public bool IsOverdue { get; set; }

    public DateTime? CompletedAt { get; set; }
    public string? CompletedByName { get; set; }
    public string? Outcome { get; set; }
    public bool IsDismissed { get; set; }
}

public class CompleteTaskDto
{
    public Guid TaskId { get; set; }
    public string? Outcome { get; set; }

    /// <summary>Logs the call on the member's timeline as well, which is where it belongs.</summary>
    public bool LogOnMemberTimeline { get; set; } = true;

    public DateTime? FollowUpOn { get; set; }
    public bool Dismiss { get; set; }
    public string? DismissReason { get; set; }
}

// ── Journeys & campaigns ─────────────────────────────────────────────────────

public class EngagementJourneyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }
    public JourneyTrigger Trigger { get; set; }
    public int? TriggerThresholdDays { get; set; }
    public Guid? SegmentId { get; set; }
    public string? SegmentName { get; set; }

    public bool IsActive { get; set; }
    public bool PreventReEnrolment { get; set; }
    public int ReEnrolmentCooldownDays { get; set; }

    public int EnrolledCount { get; set; }
    public int CompletedCount { get; set; }
    public int CurrentlyEnrolled { get; set; }
    public string? SuccessMetric { get; set; }
    public int SuccessCount { get; set; }
    public int SuccessRatePercent { get; set; }

    public List<JourneyStepDto> Steps { get; set; } = [];
}

public class JourneyStepDto
{
    public Guid Id { get; set; }
    public Guid EngagementJourneyId { get; set; }
    public int StepNumber { get; set; }
    public JourneyStepKind Kind { get; set; }
    public int DelayHours { get; set; }
    public MessageChannel? Channel { get; set; }
    public Guid? MessageTemplateId { get; set; }
    public string? MessageTemplateName { get; set; }
    public string? ConditionExpression { get; set; }
    public int? OnFalseStepNumber { get; set; }
    public string? TagToApply { get; set; }
    public Guid? OfferPromotionRuleId { get; set; }
    public int LoyaltyPointsToGrant { get; set; }
    public string? TaskTitle { get; set; }
    public Guid? TaskAssignStaffId { get; set; }
}

public class JourneyEnrolmentDto
{
    public Guid Id { get; set; }
    public Guid EngagementJourneyId { get; set; }
    public string? JourneyName { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public DateTime EnrolledAt { get; set; }
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public DateTime? NextStepDueAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ExitedAt { get; set; }
    public string? ExitReason { get; set; }
    public bool WasSuccessful { get; set; }
    public bool IsPaused { get; set; }
}

public class CampaignDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }
    public MessageChannel Channel { get; set; }
    public Guid? MessageTemplateId { get; set; }
    public string? MessageTemplateName { get; set; }
    public Guid? SegmentId { get; set; }
    public string? SegmentName { get; set; }

    public DateTime? ScheduledFor { get; set; }
    public DateTime? SentAt { get; set; }

    public int RecipientCount { get; set; }
    public int SentCount { get; set; }
    public int DeliveredCount { get; set; }
    public int OpenedCount { get; set; }
    public int ClickedCount { get; set; }
    public int FailedCount { get; set; }
    public int SuppressedCount { get; set; }

    public int OpenRatePercent { get; set; }
    public int ClickRatePercent { get; set; }

    public decimal Cost { get; set; }
    public int LeadsGenerated { get; set; }
    public int JoinsAttributed { get; set; }
    public decimal RevenueAttributed { get; set; }
    public decimal? ReturnOnSpend { get; set; }

    public Guid? PromotionRuleId { get; set; }
    public bool IsSent { get; set; }
    public bool IsCancelled { get; set; }
}

public class SendCampaignDto
{
    public Guid CampaignId { get; set; }

    /// <summary>Sends to one address only, so the wording can be checked before four hundred go out.</summary>
    public bool TestSendOnly { get; set; }
    public string? TestRecipient { get; set; }

    public DateTime? ScheduleFor { get; set; }
}

public class MessageTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }
    public MessageChannel Channel { get; set; }
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? PlainTextBody { get; set; }
    public string? LanguageCode { get; set; }
    public string? Purpose { get; set; }
    public bool IsTransactional { get; set; }
    public bool IsSystemTemplate { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Merge fields this channel supports, so the editor can offer them.</summary>
    public List<string> AvailableMergeFields { get; set; } = [];
    public int UseCount { get; set; }
}

public class MessageLogDto
{
    public Guid Id { get; set; }
    public Guid? MemberId { get; set; }
    public string? MemberName { get; set; }
    public Guid? LeadId { get; set; }
    public Guid ClubId { get; set; }

    public MessageChannel Channel { get; set; }
    public MessageStatus Status { get; set; }
    public string? TemplateName { get; set; }
    public string? CampaignName { get; set; }

    public string? Recipient { get; set; }
    public string? Subject { get; set; }
    public string? BodyPreview { get; set; }

    public DateTime QueuedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public string? FailureReason { get; set; }
    public decimal Cost { get; set; }
}

public class SegmentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }
    public string DefinitionJson { get; set; } = "{}";
    public int LastCount { get; set; }
    public DateTime? LastCountedAt { get; set; }
    public bool IsSystemSegment { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Plain-English rendering of the filter, so a manager can read what it selects.</summary>
    public string? Explanation { get; set; }
}

public class SaveSegmentDto
{
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }
    public string DefinitionJson { get; set; } = "{}";
    public string? Description { get; set; }
}

// ── Loyalty & challenges ─────────────────────────────────────────────────────

public class LoyaltyAccountDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public Guid ClubId { get; set; }

    public int PointsBalance { get; set; }
    public int LifetimePoints { get; set; }
    public int PointsRedeemed { get; set; }
    public int PointsExpired { get; set; }

    public Guid? TierId { get; set; }
    public string? TierName { get; set; }
    public string? TierColour { get; set; }
    public DateTime? TierAchievedOn { get; set; }
    public string? NextTierName { get; set; }
    public int PointsToNextTier { get; set; }

    public DateTime? NextExpiryOn { get; set; }
    public int PointsExpiringSoon { get; set; }

    public List<LoyaltyTransactionDto> RecentTransactions { get; set; } = [];
}

public class LoyaltyTransactionDto
{
    public Guid Id { get; set; }
    public LoyaltyEventKind Kind { get; set; }
    public DateTime OccurredAt { get; set; }
    public int Points { get; set; }
    public int BalanceAfter { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal? RedemptionValue { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public string? AwardedByName { get; set; }
}

public class LoyaltyTierDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }
    public int Ordinal { get; set; }
    public int PointsRequired { get; set; }
    public string? ColourHex { get; set; }
    public string? BadgeUrl { get; set; }
    public decimal EarnMultiplier { get; set; }
    public string? Benefits { get; set; }
    public int RetentionMonths { get; set; }
    public bool IsActive { get; set; }
    public int MemberCount { get; set; }
}

public class AwardPointsDto
{
    public Guid MemberId { get; set; }
    public int Points { get; set; }
    public string Reason { get; set; } = string.Empty;
    public LoyaltyEventKind Kind { get; set; } = LoyaltyEventKind.ManualAward;
    public DateTime? ExpiresOn { get; set; }
}

public class RedeemPointsDto
{
    public Guid MemberId { get; set; }
    public int Points { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal RedemptionValue { get; set; }

    /// <summary>Applies the value to the member's account balance rather than to a specific sale.</summary>
    public bool ApplyToBalance { get; set; } = true;
    public Guid? SaleId { get; set; }
}

public class ChallengeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }
    public string? Blurb { get; set; }
    public string? ImageUrl { get; set; }

    public ChallengeMetric Metric { get; set; }
    public string? CustomMetricName { get; set; }
    public string? Unit { get; set; }

    public DateTime StartsOn { get; set; }
    public DateTime EndsOn { get; set; }
    public bool IsRunning { get; set; }
    public int? DaysRemaining { get; set; }

    public decimal? TargetValue { get; set; }
    public bool IsTeamBased { get; set; }
    public bool IsOpenToAll { get; set; }
    public Guid? SegmentId { get; set; }

    public decimal? EntryFee { get; set; }
    public string? Prize { get; set; }
    public int PointsForCompletion { get; set; }

    public int ParticipantCount { get; set; }
    public int CompletedCount { get; set; }
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; }

    public List<ChallengeParticipantDto> TopParticipants { get; set; } = [];
    public ChallengeParticipantDto? ViewerEntry { get; set; }
}

public class ChallengeParticipantDto
{
    public Guid Id { get; set; }
    public Guid ChallengeId { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public DateTime JoinedAt { get; set; }
    public decimal CurrentValue { get; set; }
    public string? ValueDisplay { get; set; }
    public int? Rank { get; set; }
    public string? TeamName { get; set; }
    public bool HasCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int ProgressPercent { get; set; }
    public DateTime? LastProgressAt { get; set; }
}

public class BadgeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }
    public string? Blurb { get; set; }
    public string? IconUrl { get; set; }
    public string? ColourHex { get; set; }
    public string? CriteriaDescription { get; set; }
    public int PointsAwarded { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsAutomatic { get; set; }
    public bool IsActive { get; set; }
    public int AwardedCount { get; set; }
}

public class MemberBadgeDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public Guid BadgeId { get; set; }
    public string BadgeName { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public string? ColourHex { get; set; }
    public DateTime EarnedOn { get; set; }
    public string? Context { get; set; }
    public int TimesEarned { get; set; }
}

// ── Feedback ─────────────────────────────────────────────────────────────────

public class NpsResponseDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? MemberPhone { get; set; }
    public Guid ClubId { get; set; }

    public int Score { get; set; }
    public string Band { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public string? Trigger { get; set; }
    public Guid? ClassOccurrenceId { get; set; }
    public string? ClassName { get; set; }
    public Guid? StaffId { get; set; }
    public string? StaffName { get; set; }

    public DateTime RespondedAt { get; set; }
    public bool FollowedUp { get; set; }
    public DateTime? FollowedUpAt { get; set; }
    public string? FollowedUpByName { get; set; }
    public string? FollowUpNote { get; set; }
    public bool NeedsFollowUp { get; set; }
}

public class NpsSummaryDto
{
    public Guid? ClubId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    public int ResponseCount { get; set; }
    public int Promoters { get; set; }
    public int Passives { get; set; }
    public int Detractors { get; set; }

    /// <summary>Promoter percentage less detractor percentage, −100 to +100.</summary>
    public int Nps { get; set; }
    public int? PreviousNps { get; set; }
    public int? Change { get; set; }
    public decimal AverageScore { get; set; }
    public int ResponseRatePercent { get; set; }

    public int DetractorsAwaitingFollowUp { get; set; }
    public List<NpsResponseDto> RecentDetractors { get; set; } = [];
    public List<NpsTrendPointDto> Trend { get; set; } = [];
}

public class NpsTrendPointDto
{
    public DateTime Period { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Nps { get; set; }
    public int ResponseCount { get; set; }
}

public class FeedbackDto
{
    public Guid Id { get; set; }
    public Guid? MemberId { get; set; }
    public string? MemberName { get; set; }
    public Guid ClubId { get; set; }
    public string? Category { get; set; }
    public string Body { get; set; } = string.Empty;
    public int? Rating { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string? Channel { get; set; }
    public bool IsAnonymous { get; set; }
    public bool IsActioned { get; set; }
    public string? ActionNote { get; set; }
    public string? ActionedByName { get; set; }
}

public class AnnouncementDto
{
    public Guid Id { get; set; }
    public Guid? ClubId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime ShowFrom { get; set; }
    public DateTime? ShowUntil { get; set; }
    public bool ShowOnKiosk { get; set; }
    public bool ShowInApp { get; set; }
    public bool ShowOnClubScreens { get; set; }
    public bool IsUrgent { get; set; }
    public Guid? SegmentId { get; set; }
    public bool IsPublished { get; set; }
    public bool IsLive { get; set; }
}
