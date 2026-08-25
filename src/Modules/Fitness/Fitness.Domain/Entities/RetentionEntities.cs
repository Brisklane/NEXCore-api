using Fitness.Domain.Enums;
using Nexcore.SharedKernel;

namespace Fitness.Domain.Entities;

/// <summary>
/// How likely this member is to leave, recomputed nightly.
///
/// The band and the factors are both stored, and the factors are the point. A coach cannot act on
/// "0.71". They can act on "has not been in for 21 days, was averaging 3.4 a week, nothing
/// booked" — and the call they make off that reads as attention rather than as a retention
/// script. A score with no reasons attached is a number that gets ignored by week three.
/// </summary>
public class ChurnScore : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid ClubId { get; set; }

    public DateTime ComputedOn { get; set; } = DateTime.UtcNow;

    public ChurnRiskBand Band { get; set; } = ChurnRiskBand.Healthy;

    /// <summary>0–100. Held for sorting and trending; never shown alone.</summary>
    public int Score { get; set; }

    public ChurnRiskBand? PreviousBand { get; set; }
    public int? PreviousScore { get; set; }

    /// <summary>True when the band worsened at this run, which is what triggers a journey.</summary>
    public bool BandWorsened { get; set; }

    // ── The evidence, denormalised so the board is one read ──────────────────

    public int DaysSinceLastVisit { get; set; }
    public decimal VisitsPerWeekNow { get; set; }
    public decimal VisitsPerWeekBaseline { get; set; }
    public int TenureDays { get; set; }
    public bool HasUpcomingBooking { get; set; }
    public bool HasOutstandingBalance { get; set; }
    public bool HasFailedPayment { get; set; }
    public int DaysToContractEnd { get; set; }

    /// <summary>Who should make the call.</summary>
    public Guid? OwnerStaffId { get; set; }

    /// <summary>Someone has already actioned this; it drops off today's list without losing the history.</summary>
    public bool IsActioned { get; set; }
    public DateTime? ActionedOn { get; set; }

    public ICollection<ChurnFactor> Factors { get; set; } = [];
}

/// <summary>One reason this member is at risk, in words the person calling them can use.</summary>
public class ChurnFactor : BaseEntity
{
    public Guid ChurnScoreId { get; set; }
    public ChurnScore? ChurnScore { get; set; }

    public ChurnFactorKind Kind { get; set; }

    /// <summary>How much of the score this contributed, so the top reason can lead.</summary>
    public int Weight { get; set; }

    /// <summary>"No visit in 21 days — was averaging 3.4 a week."</summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>What the club could do about it — the suggested next action.</summary>
    public string? SuggestedAction { get; set; }
}

/// <summary>A retention job someone has to actually do, with an owner and a due date.</summary>
public class RetentionTask : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid ClubId { get; set; }
    public Guid? AssignedStaffId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }

    /// <summary>Where it came from — the risk board, a journey, a detractor NPS, a manager.</summary>
    public string? Trigger { get; set; }
    public Guid? ChurnScoreId { get; set; }
    public Guid? JourneyEnrolmentId { get; set; }

    public DateTime DueOn { get; set; }
    public int Priority { get; set; } = 2;

    public DateTime? CompletedAt { get; set; }
    public Guid? CompletedByStaffId { get; set; }
    public string? Outcome { get; set; }

    public bool IsDismissed { get; set; }
    public string? DismissReason { get; set; }
}

/// <summary>
/// An automation: when this happens, wait, then do this.
///
/// The onboarding journey alone earns the feature — the first ninety days decide the next three
/// years, and no club reliably runs a manual welcome sequence past the first busy week.
///
/// A new journey is created with <c>IsActive = false</c>: an automation that starts messaging
/// members the moment somebody saves a draft is how a club sends four hundred wrong emails.
/// </summary>
public class EngagementJourney : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }

    public JourneyTrigger Trigger { get; set; }

    /// <summary>Days, for a day-based trigger like "no visit for 14 days".</summary>
    public int? TriggerThresholdDays { get; set; }

    /// <summary>Extra conditions on entry, as a saved segment.</summary>
    public Guid? SegmentId { get; set; }

    /// <summary>Stops a member being enrolled twice while one run is still going.</summary>
    public bool PreventReEnrolment { get; set; } = true;

    /// <summary>Days before the same member may enter again after finishing.</summary>
    public int ReEnrolmentCooldownDays { get; set; } = 90;

    public int EnrolledCount { get; set; }
    public int CompletedCount { get; set; }

    /// <summary>What "worked" means for this journey — a visit, a booking, a payment.</summary>
    public string? SuccessMetric { get; set; }
    public int SuccessCount { get; set; }

    public ICollection<JourneyStep> Steps { get; set; } = [];
}

/// <summary>One step in a journey. Ordered, and each one either waits, acts or branches.</summary>
public class JourneyStep : BaseEntity
{
    public Guid EngagementJourneyId { get; set; }
    public EngagementJourney? EngagementJourney { get; set; }

    public int StepNumber { get; set; }
    public JourneyStepKind Kind { get; set; }

    public int DelayHours { get; set; }

    public MessageChannel? Channel { get; set; }
    public Guid? MessageTemplateId { get; set; }

    /// <summary>For a condition step: the expression evaluated to decide whether to carry on.</summary>
    public string? ConditionExpression { get; set; }

    /// <summary>Step to jump to when a condition is false. Null exits the journey.</summary>
    public int? OnFalseStepNumber { get; set; }

    public string? TagToApply { get; set; }
    public Guid? OfferPromotionRuleId { get; set; }
    public int LoyaltyPointsToGrant { get; set; }

    public string? TaskTitle { get; set; }
    public Guid? TaskAssignStaffId { get; set; }
}

/// <summary>One member's run through one journey, and where they got to.</summary>
public class JourneyEnrolment : BaseEntity
{
    public Guid EngagementJourneyId { get; set; }
    public EngagementJourney? EngagementJourney { get; set; }

    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public int CurrentStep { get; set; }
    public DateTime? NextStepDueAt { get; set; }

    public DateTime? CompletedAt { get; set; }
    public DateTime? ExitedAt { get; set; }
    public string? ExitReason { get; set; }

    /// <summary>The success metric fired while they were enrolled — they came back, they paid, they booked.</summary>
    public bool WasSuccessful { get; set; }
    public DateTime? SuccessAt { get; set; }

    public bool IsPaused { get; set; }
}

/// <summary>A one-off send to a segment, as opposed to an ongoing journey.</summary>
public class Campaign : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }

    public MessageChannel Channel { get; set; } = MessageChannel.Email;
    public Guid? MessageTemplateId { get; set; }
    public Guid? SegmentId { get; set; }

    public DateTime? ScheduledFor { get; set; }
    public DateTime? SentAt { get; set; }

    public int RecipientCount { get; set; }
    public int SentCount { get; set; }
    public int DeliveredCount { get; set; }
    public int OpenedCount { get; set; }
    public int ClickedCount { get; set; }
    public int FailedCount { get; set; }
    public int SuppressedCount { get; set; }

    /// <summary>Spend on this campaign, so cost per lead and per join are real numbers.</summary>
    public decimal Cost { get; set; }

    public int LeadsGenerated { get; set; }
    public int JoinsAttributed { get; set; }
    public decimal RevenueAttributed { get; set; }

    public Guid? PromotionRuleId { get; set; }
    public bool IsSent { get; set; }
    public bool IsCancelled { get; set; }
}

/// <summary>A reusable message body, per channel, with merge fields.</summary>
public class MessageTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }

    public MessageChannel Channel { get; set; } = MessageChannel.Email;

    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;

    /// <summary>Plain-text alternative, which is what actually gets delivered on SMS and read on email.</summary>
    public string? PlainTextBody { get; set; }

    public string? LanguageCode { get; set; }

    /// <summary>What it is for — welcome, payment failed, class reminder, win-back.</summary>
    public string? Purpose { get; set; }

    /// <summary>Service messages send regardless of marketing consent; marketing does not.</summary>
    public bool IsTransactional { get; set; }

    public bool IsSystemTemplate { get; set; }
}

/// <summary>
/// Every message the system tried to send, including the ones it deliberately did not.
///
/// Suppressed sends are logged as loudly as delivered ones: "why did they not get the reminder?"
/// is answered by a row saying the member withdrew SMS consent in March, not by silence.
/// </summary>
public class MessageLog : BaseEntity
{
    public Guid? MemberId { get; set; }
    public Guid? LeadId { get; set; }
    public Guid? StaffId { get; set; }
    public Guid ClubId { get; set; }

    public MessageChannel Channel { get; set; }
    public MessageStatus Status { get; set; } = MessageStatus.Queued;

    public Guid? MessageTemplateId { get; set; }
    public Guid? CampaignId { get; set; }
    public Guid? JourneyEnrolmentId { get; set; }
    public Guid? DunningCaseId { get; set; }

    public string? Recipient { get; set; }
    public string? Subject { get; set; }
    public string? BodyPreview { get; set; }

    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClickedAt { get; set; }

    public string? FailureReason { get; set; }
    public string? ProviderReference { get; set; }

    /// <summary>Cost of the send, for SMS and WhatsApp where it is not free.</summary>
    public decimal Cost { get; set; }
}

/// <summary>
/// A saved filter over the member base.
///
/// Stored as a definition rather than a materialised list so "members who have not been in for
/// three weeks" means the same thing tomorrow as it does today.
/// </summary>
public class Segment : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }

    /// <summary>Serialised filter the segment engine evaluates.</summary>
    public string DefinitionJson { get; set; } = "{}";

    /// <summary>Cached size and when it was counted, so a list of segments is not N queries.</summary>
    public int LastCount { get; set; }
    public DateTime? LastCountedAt { get; set; }

    public bool IsSystemSegment { get; set; }
}

/// <summary>A member's points balance and tier.</summary>
public class LoyaltyAccount : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid ClubId { get; set; }

    public int PointsBalance { get; set; }
    public int LifetimePoints { get; set; }
    public int PointsRedeemed { get; set; }
    public int PointsExpired { get; set; }

    public Guid? TierId { get; set; }
    public LoyaltyTier? Tier { get; set; }
    public DateTime? TierAchievedOn { get; set; }

    /// <summary>Points needed for the next tier, held so the app can show a progress bar cheaply.</summary>
    public int PointsToNextTier { get; set; }

    public DateTime? NextExpiryOn { get; set; }
    public int PointsExpiringSoon { get; set; }
}

/// <summary>Every points movement, so a balance can always be explained.</summary>
public class LoyaltyTransaction : BaseEntity
{
    public Guid LoyaltyAccountId { get; set; }
    public LoyaltyAccount? LoyaltyAccount { get; set; }

    public Guid MemberId { get; set; }

    public LoyaltyEventKind Kind { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>Negative for a redemption or expiry.</summary>
    public int Points { get; set; }
    public int BalanceAfter { get; set; }

    public string Reason { get; set; } = string.Empty;

    public Guid? SourceEntityId { get; set; }
    public string? SourceEntityType { get; set; }

    /// <summary>What the points bought, for a redemption.</summary>
    public decimal? RedemptionValue { get; set; }

    public DateTime? ExpiresOn { get; set; }
    public Guid? AwardedByStaffId { get; set; }
}

/// <summary>A loyalty tier, and what it is worth.</summary>
public class LoyaltyTier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }

    public int Ordinal { get; set; }
    public int PointsRequired { get; set; }
    public string? ColourHex { get; set; }
    public string? BadgeUrl { get; set; }

    /// <summary>Bonus rate on points earned at this tier — 1.25 pays a quarter more.</summary>
    public decimal EarnMultiplier { get; set; } = 1;

    public string? Benefits { get; set; }

    /// <summary>Months of inactivity before a tier is lost. Zero means it is kept for good.</summary>
    public int RetentionMonths { get; set; }
}

/// <summary>
/// A club challenge — most visits in March, a 100 km month, a team step count.
///
/// Cheap to run and disproportionately effective: a leaderboard and a deadline change behaviour
/// in a way that a discount does not.
/// </summary>
public class Challenge : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }

    public string? Blurb { get; set; }
    public string? ImageUrl { get; set; }

    public ChallengeMetric Metric { get; set; } = ChallengeMetric.Visits;
    public string? CustomMetricName { get; set; }
    public string? Unit { get; set; }

    public DateTime StartsOn { get; set; }
    public DateTime EndsOn { get; set; }

    /// <summary>The number to beat, for a target challenge rather than a ranking one.</summary>
    public decimal? TargetValue { get; set; }

    /// <summary>Team-based rather than individual, which is what gets the quiet members involved.</summary>
    public bool IsTeamBased { get; set; }

    public bool IsOpenToAll { get; set; } = true;
    public Guid? SegmentId { get; set; }

    public decimal? EntryFee { get; set; }
    public string? Prize { get; set; }
    public int PointsForCompletion { get; set; }

    public int ParticipantCount { get; set; }
    public int CompletedCount { get; set; }

    public bool IsPublished { get; set; }

    public ICollection<ChallengeParticipant> Participants { get; set; } = [];
}

/// <summary>One member's entry in a challenge, and how they are doing.</summary>
public class ChallengeParticipant : BaseEntity
{
    public Guid ChallengeId { get; set; }
    public Challenge? Challenge { get; set; }

    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public decimal CurrentValue { get; set; }
    public int? Rank { get; set; }

    public string? TeamName { get; set; }

    public bool HasCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool PointsAwarded { get; set; }

    public DateTime? LastProgressAt { get; set; }
}

/// <summary>An award a member can earn. Cheap to award, disproportionately kept.</summary>
public class Badge : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }

    public string? Blurb { get; set; }
    public string? IconUrl { get; set; }
    public string? ColourHex { get; set; }

    /// <summary>What earns it — a visit count, a streak, a PR, a challenge, a class count.</summary>
    public string? CriteriaDescription { get; set; }
    public string? CriteriaExpression { get; set; }

    public int PointsAwarded { get; set; }
    public int DisplayOrder { get; set; }

    /// <summary>Awarded by the nightly job rather than by a person.</summary>
    public bool IsAutomatic { get; set; } = true;
}

/// <summary>A badge a member has earned.</summary>
public class MemberBadge : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid BadgeId { get; set; }
    public Badge? Badge { get; set; }

    public DateTime EarnedOn { get; set; } = DateTime.UtcNow;
    public string? Context { get; set; }

    /// <summary>Awarded once and only once; a repeatable badge carries a count instead.</summary>
    public int TimesEarned { get; set; } = 1;

    public bool NotificationSent { get; set; }
    public Guid? AwardedByStaffId { get; set; }
}

/// <summary>
/// A survey answer, with the score and the words. The words are what a manager acts on; the score
/// is what the trend is drawn from.
/// </summary>
public class NpsResponse : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid ClubId { get; set; }

    /// <summary>0–10.</summary>
    public int Score { get; set; }

    /// <summary>Promoter, passive or detractor — derived, stored so reports do not re-band on every read.</summary>
    public string Band { get; set; } = string.Empty;

    public string? Comment { get; set; }

    /// <summary>What prompted it — joining, a class, a cancellation, a periodic sweep.</summary>
    public string? Trigger { get; set; }
    public Guid? ClassOccurrenceId { get; set; }
    public Guid? AppointmentId { get; set; }
    public Guid? StaffId { get; set; }

    public DateTime RespondedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Detractors raise a task automatically; this records that someone actually replied.</summary>
    public bool FollowedUp { get; set; }
    public DateTime? FollowedUpAt { get; set; }
    public Guid? FollowedUpByStaffId { get; set; }
    public string? FollowUpNote { get; set; }
}

/// <summary>General feedback that is not an NPS response — a suggestion, a compliment, a grumble.</summary>
public class Feedback : BaseEntity
{
    public Guid? MemberId { get; set; }
    public Guid ClubId { get; set; }

    public string? Category { get; set; }
    public string Body { get; set; } = string.Empty;

    /// <summary>1–5, where the club asks for one.</summary>
    public int? Rating { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string? Channel { get; set; }

    public bool IsAnonymous { get; set; }

    public bool IsActioned { get; set; }
    public string? ActionNote { get; set; }
    public Guid? ActionedByStaffId { get; set; }
}
