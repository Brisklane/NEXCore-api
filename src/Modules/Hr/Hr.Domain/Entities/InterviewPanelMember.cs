
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class InterviewPanelMember : BaseEntity
{
    public Guid InterviewId { get; set; }
    public Guid InterviewerEmployeeId { get; set; }
    public Guid? AlternateInterviewerEmployeeId { get; set; }
    public string Role { get; set; } = string.Empty;
    public decimal ScoreWeight { get; set; }
    public bool IsLead { get; set; }
    public bool IsMandatory { get; set; }
    public Guid InviteStatusLookupValueId { get; set; }
    public DateTime? InviteSentAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? ResponseComments { get; set; }
    public bool HasConflictOfInterest { get; set; }
    public string? ConflictOfInterestNotes { get; set; }
    public bool IsAvailabilityConfirmed { get; set; }
    public DateTime? FeedbackDeadline { get; set; }
    public DateTime? ReminderSentAt { get; set; }
    public bool DeclinedToParticipate { get; set; }
    public string? DeclineReason { get; set; }
    public string? AttendanceStatus { get; set; }
    public DateTime? JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public int PanelSequence { get; set; }

    public Interview? Interview { get; set; }
    public Employee? InterviewerEmployee { get; set; }
    public Employee? AlternateInterviewerEmployee { get; set; }
    public LookupValue? InviteStatusLookupValue { get; set; }
}
