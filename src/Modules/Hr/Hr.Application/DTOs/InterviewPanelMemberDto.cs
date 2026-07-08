namespace Hr.Application.DTOs;

public class InterviewPanelMemberDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid InterviewId { get; set; }
    public Guid InterviewerEmployeeId { get; set; }
    public Guid? AlternateInterviewerEmployeeId { get; set; }
    public string Role { get; set; } = string.Empty;
    public decimal ScoreWeight { get; set; }
    public bool IsLead { get; set; }
    public bool IsMandatory { get; set; }
    public Guid InviteStatusLookupValueId { get; set; }
    public DateTime? InviteSentAt { get; set; }
    public bool HasConflictOfInterest { get; set; }
    public bool IsAvailabilityConfirmed { get; set; }
    public DateTime? FeedbackDeadline { get; set; }
    public int PanelSequence { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateInterviewPanelMemberDto
{
    public Guid InterviewId { get; set; }
    public Guid InterviewerEmployeeId { get; set; }
    public Guid? AlternateInterviewerEmployeeId { get; set; }
    public string Role { get; set; } = string.Empty;
    public decimal ScoreWeight { get; set; }
    public bool IsLead { get; set; }
    public bool IsMandatory { get; set; }
    public Guid InviteStatusLookupValueId { get; set; }
    public DateTime? FeedbackDeadline { get; set; }
    public int PanelSequence { get; set; }
}

public class UpdateInterviewPanelMemberDto
{
    public Guid? AlternateInterviewerEmployeeId { get; set; }
    public string? Role { get; set; }
    public decimal? ScoreWeight { get; set; }
    public bool? IsLead { get; set; }
    public bool? IsMandatory { get; set; }
    public Guid? InviteStatusLookupValueId { get; set; }
    public bool? HasConflictOfInterest { get; set; }
    public string? ConflictOfInterestNotes { get; set; }
    public bool? IsAvailabilityConfirmed { get; set; }
    public DateTime? FeedbackDeadline { get; set; }
}
