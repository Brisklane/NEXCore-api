
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class CandidateTaskSubmission : BaseEntity
{
    public Guid CandidateTaskId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid ApplicationId { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string? FileUrl { get; set; }
    public string? SubmissionText { get; set; }
    public string? ExternalLink { get; set; }
    public Guid StatusLookupValueId { get; set; }
    public int AttemptNo { get; set; } = 1;
    public bool IsLateSubmission { get; set; }
    public int? TimeSpentMinutes { get; set; }
    public string? BrowserMetadata { get; set; }
    public string? IPAddress { get; set; }
    public decimal? IntegrityScore { get; set; }
    public decimal? AutoScore { get; set; }
    public string? ParsedOutput { get; set; }

    public CandidateTask? CandidateTask { get; set; }
    public Candidate? Candidate { get; set; }
    public Application? Application { get; set; }
    public LookupValue? StatusLookupValue { get; set; }
    public ICollection<CandidateTaskEvaluation>? Evaluations { get; set; }
}
