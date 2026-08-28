using Nexcore.SharedKernel;
using RealEstate.Domain.Enums;

namespace RealEstate.Domain.Entities;

/// <summary>
/// Somebody who wants something. The unit of work for every salesperson in the app.
///
/// One person may have many live enquiries — a buyer looking at two projects and also trying to
/// rent out their existing flat — so the enquiry hangs off the party rather than replacing it.
/// </summary>
public class Enquiry : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid? PartyId { get; set; }
    public Party? Party { get; set; }

    // Captured before the party exists, so a lead is never lost to a missing record.
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }

    public EnquiryStage Stage { get; set; } = EnquiryStage.New;
    public EnquiryChannel Channel { get; set; } = EnquiryChannel.Manual;

    /// <summary>The specific origin inside the channel — which portal, which campaign, which partner.</summary>
    public string? SubSource { get; set; }

    public Guid? PortalChannelId { get; set; }
    public Guid? CampaignId { get; set; }
    public Guid? ChannelPartnerId { get; set; }
    public Guid? ReferredByPartyId { get; set; }
    public Guid? MarketingEventId { get; set; }

    // ── What they want ───────────────────────────────────────────────────────

    public Guid? ListingId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? RequirementProfileId { get; set; }

    public ListingKind Interest { get; set; } = ListingKind.ForSale;
    public BuyingPurpose? Purpose { get; set; }
    public FundingKind? Funding { get; set; }
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public string? Timeline { get; set; }
    public string? Message { get; set; }

    // ── Ownership and the clock ──────────────────────────────────────────────

    public Guid? AssignedAgentId { get; set; }
    public Guid? OfficeId { get; set; }
    public Guid? TerritoryId { get; set; }
    public DateTime? AssignedAt { get; set; }

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When somebody actually made contact. The gap from <see cref="ReceivedAt"/> is speed to lead,
    /// and it predicts conversion better than almost anything else on this record.
    /// </summary>
    public DateTime? FirstContactedAt { get; set; }

    /// <summary>The deadline. Past it and unanswered, the board goes red and the manager is told.</summary>
    public DateTime? ResponseDueAt { get; set; }

    public bool SlaBreached { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public DateTime? NextFollowUpAt { get; set; }

    // ── Scoring ──────────────────────────────────────────────────────────────

    public int Score { get; set; }

    /// <summary>The components behind the score, so the screen can show its working.</summary>
    public string? ScoreBreakdown { get; set; }

    public bool IsQualified { get; set; }
    public int ViewingCount { get; set; }
    public int SiteVisitCount { get; set; }

    // ── Outcome ──────────────────────────────────────────────────────────────

    public Guid? ConvertedBookingId { get; set; }
    public Guid? ConvertedDealId { get; set; }
    public Guid? ConvertedTenancyId { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid? LossReasonCodeId { get; set; }
    public string? LossNote { get; set; }

    /// <summary>Where they went instead. The single most useful thing to know about a lost lead.</summary>
    public string? LostToCompetitor { get; set; }

    /// <summary>Set when this enquiry was folded into another as a duplicate.</summary>
    public Guid? MergedIntoEnquiryId { get; set; }

    public ICollection<EnquiryStageHistory> StageHistory { get; set; } = [];
}

public class EnquiryStageHistory : BaseEntity
{
    public Guid EnquiryId { get; set; }
    public Enquiry? Enquiry { get; set; }

    public EnquiryStage FromStage { get; set; }
    public EnquiryStage ToStage { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public Guid ChangedByUserId { get; set; }

    /// <summary>How long it sat in the previous stage. Stage velocity falls out of this column.</summary>
    public int DaysInPreviousStage { get; set; }

    public string? Note { get; set; }
}

/// <summary>
/// What a buyer or tenant is actually looking for, in a shape the matcher can query.
///
/// Held separately from the enquiry because the same person's requirement outlives any single
/// enquiry and is re-matched against every new listing for as long as it is live.
/// </summary>
public class RequirementProfile : BaseEntity
{
    public Guid PartyId { get; set; }
    public Guid? EnquiryId { get; set; }
    public string? Name { get; set; }

    public ListingKind Interest { get; set; } = ListingKind.ForSale;
    public PropertyCategory? Category { get; set; }

    /// <summary>Comma-separated sub-types. A buyer will look at a house or a townhouse, not one only.</summary>
    public string? SubTypes { get; set; }

    public BuyingPurpose? Purpose { get; set; }
    public FundingKind? Funding { get; set; }

    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public string? CurrencyCode { get; set; }

    public decimal? MinAreaSqFt { get; set; }
    public decimal? MaxAreaSqFt { get; set; }
    public int? MinBedrooms { get; set; }
    public int? MaxBedrooms { get; set; }
    public int? MinBathrooms { get; set; }
    public int? MinFloor { get; set; }
    public int? MaxFloor { get; set; }
    public Facing? PreferredFacing { get; set; }
    public FurnishingState? Furnishing { get; set; }

    /// <summary>Comma-separated feature keys that a match must have.</summary>
    public string? MustHaveFeatures { get; set; }

    /// <summary>Comma-separated feature keys that raise the score but do not gate it.</summary>
    public string? NiceToHaveFeatures { get; set; }

    /// <summary>Drawn on the map. More honest than a list of area names when a patch is irregular.</summary>
    public string? PreferredAreasGeoJson { get; set; }

    public DateOnly? AvailableFrom { get; set; }
    public string? Exclusions { get; set; }

    public bool AlertsEnabled { get; set; } = true;
    public DateTime? LastMatchedAt { get; set; }

    public ICollection<RequirementArea> Areas { get; set; } = [];
}

public class RequirementArea : BaseEntity
{
    public Guid RequirementProfileId { get; set; }
    public RequirementProfile? Profile { get; set; }
    public Guid GeoAreaId { get; set; }

    /// <summary>Preference weight, so "ideally Gulberg, would consider DHA" is expressible.</summary>
    public int Priority { get; set; } = 1;
}

/// <summary>A stored search on the portal or in the app, with alerting.</summary>
public class SavedSearch : BaseEntity
{
    public Guid? PartyId { get; set; }
    public Guid? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CriteriaJson { get; set; } = "{}";

    public bool AlertsEnabled { get; set; } = true;

    /// <summary>"Instant", "Daily", "Weekly". Instant alerts are what win a hot listing.</summary>
    public string AlertFrequency { get; set; } = "Daily";

    public DateTime? LastAlertedAt { get; set; }
    public int ResultCountAtLastRun { get; set; }
}

/// <summary>
/// One listing matched to one requirement, with the reasoning kept.
///
/// Stored rather than computed on demand so that "we sent you these six on Tuesday" is provable,
/// and so open and click tracking has something to attach to.
/// </summary>
public class MatchResult : BaseEntity
{
    public Guid RequirementProfileId { get; set; }
    public Guid? ListingId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? PropertyId { get; set; }

    /// <summary>0–100. Explainable: <see cref="ScoreBreakdown"/> says how it was reached.</summary>
    public int Score { get; set; }

    public string? ScoreBreakdown { get; set; }
    public DateTime MatchedAt { get; set; } = DateTime.UtcNow;

    public bool WasSent { get; set; }
    public DateTime? SentAt { get; set; }
    public NotificationChannel? SentVia { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClickedAt { get; set; }

    /// <summary>The agent or the client said no. Stops it being sent again.</summary>
    public bool IsDismissed { get; set; }
    public Guid? DismissReasonCodeId { get; set; }

    public bool LedToViewing { get; set; }
}

/// <summary>
/// Anything that happened with a person: a call, a message, a meeting, a document sent.
///
/// Deliberately one table across every channel and every context, because the only view that
/// matters is the single chronological timeline on the 360 — and stitching that together from six
/// tables at read time is how systems become slow and inconsistent.
/// </summary>
public class Activity : BaseEntity
{
    public ActivityKind Kind { get; set; }
    public ActivityDirection Direction { get; set; } = ActivityDirection.Outbound;

    public Guid? PartyId { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? DealId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? ListingId { get; set; }
    public Guid? ChannelPartnerId { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Guid? UserId { get; set; }
    public Guid? AgentId { get; set; }

    public string? Subject { get; set; }
    public string? Body { get; set; }

    public int? DurationSeconds { get; set; }

    /// <summary>"Answered", "NoAnswer", "Busy", "WrongNumber", "Interested", "CallBack", "NotInterested".</summary>
    public string? Outcome { get; set; }

    public Guid? OutcomeReasonCodeId { get; set; }

    // Channel specifics, kept flat because they are read together and never queried apart.
    public string? ExternalMessageId { get; set; }
    public string? RecordingUrl { get; set; }
    public string? AttachmentUrl { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool DeliveryFailed { get; set; }
    public string? FailureReason { get; set; }

    /// <summary>Generated by a job rather than typed by a person. Filtered out of "what did we say" views.</summary>
    public bool IsSystemGenerated { get; set; }

    public bool IsPinned { get; set; }
}

/// <summary>
/// The next thing somebody has to do. Drives the My Day screen, which is the only screen most
/// agents open before lunch.
/// </summary>
public class FollowUpTask : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Note { get; set; }
    public ActivityKind SuggestedAction { get; set; } = ActivityKind.Call;

    public Guid? PartyId { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? DealId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public Guid? DunningCaseId { get; set; }

    public Guid AssignedToUserId { get; set; }
    public Guid? AssignedByUserId { get; set; }

    public DateTime DueAt { get; set; }
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;
    public TaskState State { get; set; } = TaskState.Open;

    public DateTime? CompletedAt { get; set; }
    public Guid? CompletedByUserId { get; set; }
    public string? CompletionNote { get; set; }
    public Guid? ResultingActivityId { get; set; }

    /// <summary>Raised by a rule — an unanswered lead, a broken promise, a compliance date — not by a person.</summary>
    public bool IsAutoGenerated { get; set; }

    public string? SourceRuleKey { get; set; }
    public int SnoozeCount { get; set; }
}
