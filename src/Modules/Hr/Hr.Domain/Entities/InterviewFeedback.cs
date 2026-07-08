
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class InterviewFeedback : BaseEntity
{
    public Guid InterviewId { get; set; }
    public Guid PanelMemberId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid SubmittedByEmployeeId { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public bool IsSubmitted { get; set; }
    public decimal? OverallScore { get; set; }
    public Guid? RecommendationLookupValueId { get; set; }
    public Guid? DecisionLookupValueId { get; set; }
    public decimal? TechnicalScore { get; set; }
    public decimal? BehavioralScore { get; set; }
    public decimal? CommunicationScore { get; set; }
    public decimal? CultureFitScore { get; set; }
    public decimal? WeightedFinalScore { get; set; }
    public string? StrengthNotes { get; set; }
    public string? ConcernNotes { get; set; }
    public string? Comments { get; set; }
    public string CompetencyScoresJson { get; set; } = "[]";
    public string? HireReadiness { get; set; }
    public bool WouldRehire { get; set; }
    public bool SubmittedLate { get; set; }
    public bool ReviewedByHR { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ConfidentialNotes { get; set; }

    public Interview? Interview { get; set; }
    public InterviewPanelMember? PanelMember { get; set; }
    public Candidate? Candidate { get; set; }
    public Application? Application { get; set; }
    public Employee? SubmittedByEmployee { get; set; }
    public LookupValue? RecommendationLookupValue { get; set; }
    public LookupValue? DecisionLookupValue { get; set; }
}
