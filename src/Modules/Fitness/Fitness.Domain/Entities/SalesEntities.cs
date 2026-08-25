using Fitness.Domain.Enums;
using Nexcore.SharedKernel;

namespace Fitness.Domain.Entities;

/// <summary>
/// Someone who might join.
///
/// Carries a first-response clock because in this market that single number moves conversion more
/// than anything else on the record: a lead answered in five minutes converts several times
/// better than the same lead answered in an hour, and a board that does not show the clock
/// cannot be managed against it.
/// </summary>
public class FitnessLead : BaseEntity
{
    public Guid ClubId { get; set; }

    /// <summary>Set once they exist as a person in the system; the lead and the member are one record then.</summary>
    public Guid? MemberId { get; set; }

    /// <summary>CRM contact, when one has been created.</summary>
    public Guid? ContactId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateTime? DateOfBirth { get; set; }

    public LeadStatus Status { get; set; } = LeadStatus.New;

    public Guid? LeadSourceId { get; set; }
    public LeadSource? LeadSource { get; set; }

    public Guid? CampaignId { get; set; }
    public Guid? ReferredByMemberId { get; set; }
    public Guid? PromoCodeId { get; set; }

    /// <summary>What they said they want. The line that makes the follow-up call not sound like a script.</summary>
    public string? Goal { get; set; }
    public string? InterestedInPlanId { get; set; }
    public string? Notes { get; set; }

    // ── Speed to lead ────────────────────────────────────────────────────────

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FirstContactedAt { get; set; }

    /// <summary>Minutes from arrival to first genuine contact. The number the board sorts on.</summary>
    public int? ResponseMinutes { get; set; }
    public bool SlaBreached { get; set; }

    // ── Pipeline ─────────────────────────────────────────────────────────────

    public Guid? AssignedStaffId { get; set; }
    public DateTime? AssignedAt { get; set; }

    public DateTime? LastActivityAt { get; set; }
    public DateTime? NextFollowUpOn { get; set; }
    public int ContactAttempts { get; set; }

    public DateTime? TourBookedFor { get; set; }
    public DateTime? TouredOn { get; set; }
    public DateTime? TrialStartedOn { get; set; }
    public DateTime? TrialEndsOn { get; set; }

    // ── Outcome ──────────────────────────────────────────────────────────────

    public DateTime? WonOn { get; set; }
    public Guid? ResultingAgreementId { get; set; }
    public decimal? WonValue { get; set; }

    public DateTime? LostOn { get; set; }
    public Guid? LossReasonId { get; set; }
    public string? LossNote { get; set; }

    /// <summary>Marketing spend attributed to this lead, so cost per acquisition is a real number.</summary>
    public decimal? AttributedCost { get; set; }

    public ICollection<LeadActivity> Activities { get; set; } = [];
}

/// <summary>Where leads come from, and what that channel costs.</summary>
public class LeadSource : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public LeadSourceKind Kind { get; set; } = LeadSourceKind.Other;

    public Guid? ClubId { get; set; }
    public int DisplayOrder { get; set; }

    /// <summary>Spend against this source, entered by the marketer, used for cost per lead.</summary>
    public decimal MonthlyCost { get; set; }

    /// <summary>Where the lead form posts from, so web enquiries land on the right source without asking.</summary>
    public string? TrackingCode { get; set; }
}

/// <summary>Every touch on a lead. What was tried, when, by whom, and what came back.</summary>
public class LeadActivity : BaseEntity
{
    public Guid LeadId { get; set; }
    public FitnessLead? Lead { get; set; }

    public LeadActivityKind Kind { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public string? Summary { get; set; }
    public string? Outcome { get; set; }

    public Guid? StaffId { get; set; }
    public string? StaffName { get; set; }

    public LeadStatus? FromStatus { get; set; }
    public LeadStatus? ToStatus { get; set; }

    /// <summary>Whether this touch counted as reaching them, which is what stops the SLA clock.</summary>
    public bool WasSuccessfulContact { get; set; }

    public Guid? MessageLogId { get; set; }
    public DateTime? FollowUpOn { get; set; }
}

/// <summary>
/// A booked show-round. Kept as its own record rather than a date on the lead so no-shows are
/// countable and the tour-to-join rate is measurable.
/// </summary>
public class Tour : BaseEntity
{
    public Guid LeadId { get; set; }
    public FitnessLead? Lead { get; set; }

    public Guid ClubId { get; set; }
    public Guid? StaffId { get; set; }

    public DateTime ScheduledFor { get; set; }
    public int DurationMinutes { get; set; } = 30;

    public DateTime? ArrivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public bool WasNoShow { get; set; }
    public bool WasCancelled { get; set; }
    public string? CancellationReason { get; set; }

    /// <summary>Whether it turned into a sale on the day, which is the number a sales manager watches.</summary>
    public bool ConvertedOnDay { get; set; }

    public string? Notes { get; set; }
    public bool ReminderSent { get; set; }
}

/// <summary>
/// A trial issued to a lead.
///
/// A real membership with a hard expiry rather than a note in a diary — the door has to honour it,
/// and the conversion sequence has to fire off its end date.
/// </summary>
public class TrialPass : BaseEntity
{
    public Guid LeadId { get; set; }
    public FitnessLead? Lead { get; set; }

    public Guid? MemberId { get; set; }
    public Guid ClubId { get; set; }
    public Guid? PlanId { get; set; }

    public DateTime StartsOn { get; set; }
    public DateTime EndsOn { get; set; }

    public int VisitsAllowed { get; set; }
    public int VisitsUsed { get; set; }

    public decimal Price { get; set; }
    public Guid? PaymentId { get; set; }

    public Guid? CredentialId { get; set; }
    public Guid? DayPassId { get; set; }

    public bool Converted { get; set; }
    public DateTime? ConvertedOn { get; set; }
    public Guid? ResultingAgreementId { get; set; }

    /// <summary>The nudge sequence has been started; stops it being started twice.</summary>
    public bool ConversionSequenceStarted { get; set; }

    public Guid? IssuedByStaffId { get; set; }
}

/// <summary>
/// A member introducing someone, tracked all the way to the reward.
///
/// Both sides are rewarded automatically on conversion, because a referral scheme that depends on
/// somebody remembering to apply a credit is a referral scheme that quietly stops working.
/// </summary>
public class Referral : BaseEntity
{
    public Guid ReferrerMemberId { get; set; }
    public Member? ReferrerMember { get; set; }

    public Guid ClubId { get; set; }

    public string ReferredName { get; set; } = string.Empty;
    public string? ReferredPhone { get; set; }
    public string? ReferredEmail { get; set; }

    public Guid? LeadId { get; set; }
    public Guid? ReferredMemberId { get; set; }

    public DateTime ReferredOn { get; set; } = DateTime.UtcNow;

    /// <summary>The trackable link or code the referrer shared.</summary>
    public string? ReferralCode { get; set; }

    public bool Converted { get; set; }
    public DateTime? ConvertedOn { get; set; }

    // ── Rewards ──────────────────────────────────────────────────────────────

    public decimal ReferrerRewardValue { get; set; }
    public int ReferrerRewardPoints { get; set; }
    public bool ReferrerRewarded { get; set; }
    public DateTime? ReferrerRewardedOn { get; set; }

    public decimal ReferredRewardValue { get; set; }
    public bool ReferredRewarded { get; set; }

    public Guid? CampaignId { get; set; }
}

/// <summary>A sales target for a person, a club or a period, and how it is tracking.</summary>
public class SalesTarget : BaseEntity
{
    public Guid ClubId { get; set; }
    public Guid? StaffId { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    /// <summary>Joins, revenue, tours, trials, PT packages — whatever this club manages against.</summary>
    public string MetricName { get; set; } = "Joins";

    public decimal TargetValue { get; set; }
    public decimal ActualValue { get; set; }

    /// <summary>Recomputed on the nightly job so a leaderboard is a read, not a calculation.</summary>
    public int AchievementPercent { get; set; }

    public decimal? BonusOnAchievement { get; set; }
    public bool IsAchieved { get; set; }
}

/// <summary>Why deals are lost, as a countable list rather than a free-text field nobody reads.</summary>
public class LossReason : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    /// <summary>Price, timing, location, competitor, facility, personal — for grouping in the report.</summary>
    public string? Category { get; set; }

    /// <summary>Prompts the consultant for a note, for reasons worth understanding in detail.</summary>
    public bool RequiresNote { get; set; }
}
