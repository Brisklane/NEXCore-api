
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class CallLog : BaseEntity
{
    public Guid CandidateId { get; set; }
    public Guid? ApplicationId { get; set; }
    public Guid CalledByEmployeeId { get; set; }
    public string CallType { get; set; } = string.Empty;
    public DateTime CallDate { get; set; } = DateTime.UtcNow;
    public int? DurationMinutes { get; set; }
    public string? Notes { get; set; }
    public string? Outcome { get; set; }
    public DateTime? NextActionDate { get; set; }
    public string? NextActionType { get; set; }
    public string? ContactMethod { get; set; }
    public string? PhoneNumberUsed { get; set; }
    public string? CallDirection { get; set; }
    public string? RecordingUrl { get; set; }
    public string? TranscriptUrl { get; set; }
    public string? TelephonySessionId { get; set; }
    public string? TelephonyProvider { get; set; }
    public decimal? SentimentScore { get; set; }
    public bool IsFollowUpDone { get; set; }
    public DateTime? FollowUpCompletedAt { get; set; }
    public Guid? CommunicationTemplateId { get; set; }

    public Candidate? Candidate { get; set; }
    public Application? Application { get; set; }
    public Employee? CalledByEmployee { get; set; }
    public CommunicationTemplate? CommunicationTemplate { get; set; }
}
