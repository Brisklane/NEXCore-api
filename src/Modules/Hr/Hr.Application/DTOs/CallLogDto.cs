namespace Hr.Application.DTOs;

public class CallLogDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid? ApplicationId { get; set; }
    public Guid CalledByEmployeeId { get; set; }
    public string CallType { get; set; } = string.Empty;
    public DateTime CallDate { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Notes { get; set; }
    public string? Outcome { get; set; }
    public DateTime? NextActionDate { get; set; }
    public string? NextActionType { get; set; }
    public string? ContactMethod { get; set; }
    public string? CallDirection { get; set; }
    public bool IsFollowUpDone { get; set; }
    public Guid? CommunicationTemplateId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCallLogDto
{
    public Guid CandidateId { get; set; }
    public Guid? ApplicationId { get; set; }
    public Guid CalledByEmployeeId { get; set; }
    public string CallType { get; set; } = string.Empty;
    public DateTime CallDate { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Notes { get; set; }
    public string? Outcome { get; set; }
    public DateTime? NextActionDate { get; set; }
    public string? NextActionType { get; set; }
    public string? ContactMethod { get; set; }
    public string? CallDirection { get; set; }
    public Guid? CommunicationTemplateId { get; set; }
}

public class UpdateCallLogDto
{
    public string? Notes { get; set; }
    public string? Outcome { get; set; }
    public int? DurationMinutes { get; set; }
    public DateTime? NextActionDate { get; set; }
    public string? NextActionType { get; set; }
    public bool? IsFollowUpDone { get; set; }
    public DateTime? FollowUpCompletedAt { get; set; }
}
