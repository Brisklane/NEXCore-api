namespace Hr.Application.DTOs;

public class InterviewFeedbackDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid InterviewId { get; set; }
    public Guid PanelMemberId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid SubmittedByEmployeeId { get; set; }
    public DateTime SubmittedAt { get; set; }
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
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateInterviewFeedbackDto
{
    public Guid InterviewId { get; set; }
    public Guid PanelMemberId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid SubmittedByEmployeeId { get; set; }
    public decimal? OverallScore { get; set; }
    public Guid? RecommendationLookupValueId { get; set; }
    public decimal? TechnicalScore { get; set; }
    public decimal? BehavioralScore { get; set; }
    public decimal? CommunicationScore { get; set; }
    public decimal? CultureFitScore { get; set; }
    public string? StrengthNotes { get; set; }
    public string? ConcernNotes { get; set; }
    public string? Comments { get; set; }
    public string CompetencyScoresJson { get; set; } = "[]";
    public string? HireReadiness { get; set; }
    public bool WouldRehire { get; set; }
}

public class UpdateInterviewFeedbackDto
{
    public decimal? OverallScore { get; set; }
    public Guid? RecommendationLookupValueId { get; set; }
    public Guid? DecisionLookupValueId { get; set; }
    public decimal? TechnicalScore { get; set; }
    public decimal? BehavioralScore { get; set; }
    public decimal? CommunicationScore { get; set; }
    public decimal? CultureFitScore { get; set; }
    public string? StrengthNotes { get; set; }
    public string? ConcernNotes { get; set; }
    public string? Comments { get; set; }
    public string? CompetencyScoresJson { get; set; }
    public string? HireReadiness { get; set; }
    public bool? WouldRehire { get; set; }
    public bool? IsSubmitted { get; set; }
}
