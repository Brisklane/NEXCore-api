
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Enums;

namespace Hr.Domain.Entities;

public class Interview : BaseEntity
{
    public string InterviewCode { get; set; } = string.Empty;
    public Guid ApplicationId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid JobId { get; set; }
    public string InterviewTitle { get; set; } = string.Empty;
    public Guid InterviewTypeLookupValueId { get; set; }
    public int? InterviewSequenceNo { get; set; }
    public Guid StatusLookupValueId { get; set; }
    public Guid? InterviewFeedbackTemplateId { get; set; }
    public DateTime? ScheduledStart { get; set; }
    public DateTime? ScheduledEnd { get; set; }
    public int? DurationMinutes { get; set; }
    public InterviewFormat Format { get; set; }
    public string? Location { get; set; }
    public string? VideoLink { get; set; }
    public string? CalendarProvider { get; set; }
    public string? CalendarEventId { get; set; }
    public Guid? ProposedByEmployeeId { get; set; }
    public DateTime? ProposedAt { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public bool CandidateConfirmed { get; set; }
    public DateTime? CandidateConfirmedAt { get; set; }
    public DateTime? OriginalScheduledStart { get; set; }
    public int RescheduleCount { get; set; }
    public string? CancelReason { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }
    public int? ActualDurationMinutes { get; set; }
    public Guid? DecisionLookupValueId { get; set; }
    public decimal? WeightedPanelScore { get; set; }
    public DateTime? FeedbackDueDate { get; set; }
    public int FeedbackSubmittedCount { get; set; }
    public bool SLABreached { get; set; }
    public string? RecordingUrl { get; set; }
    public bool IsMandatoryRound { get; set; } = true;

    public Application? Application { get; set; }
    public Candidate? Candidate { get; set; }
    public Job? Job { get; set; }
    public InterviewFeedbackTemplate? InterviewFeedbackTemplate { get; set; }
    public Employee? ProposedByEmployee { get; set; }
    public LookupValue? InterviewTypeLookupValue { get; set; }
    public LookupValue? StatusLookupValue { get; set; }
    public LookupValue? DecisionLookupValue { get; set; }
    public ICollection<InterviewPanelMember>? PanelMembers { get; set; }
    public ICollection<InterviewFeedback>? Feedbacks { get; set; }
    public ICollection<InterviewNotification>? Notifications { get; set; }
    public ICollection<InterviewerAvailability>? InterviewerAvailabilities { get; set; }
}
