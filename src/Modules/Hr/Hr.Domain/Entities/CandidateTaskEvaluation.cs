
using Nexcore.SharedKernel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hr.Domain.Entities;

public class CandidateTaskEvaluation : BaseEntity
{
    public Guid CandidateTaskId { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid EvaluatedByEmployeeId { get; set; }
    public DateTime EvaluationDate { get; set; } = DateTime.UtcNow;
    public decimal? Score { get; set; }
    public Guid ResultLookupValueId { get; set; }
    public string? Comments { get; set; }
    public decimal? TechnicalScore { get; set; }
    public decimal? QualityScore { get; set; }
    public decimal? CreativityScore { get; set; }
    public decimal? CommunicationScore { get; set; }
    public decimal? WeightedFinalScore { get; set; }
    public string? Recommendation { get; set; }
    public bool FeedbackVisibleToCandidate { get; set; }
    public int? ReviewedDurationMinutes { get; set; }

    public CandidateTask? CandidateTask { get; set; }
    public CandidateTaskSubmission? Submission { get; set; }
    public Employee? EvaluatedByEmployee { get; set; }
    public LookupValue? ResultLookupValue { get; set; }
}
