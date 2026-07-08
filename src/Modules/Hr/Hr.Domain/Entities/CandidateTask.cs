
using Nexcore.SharedKernel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hr.Domain.Entities;

public class CandidateTask : BaseEntity
{
    public string TaskCode { get; set; } = string.Empty;
    public Guid ApplicationId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid JobId { get; set; }
    public Guid TaskTypeLookupValueId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string? ExternalProvider { get; set; }
    public string? ProviderReferenceId { get; set; }
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public Guid StatusLookupValueId { get; set; }
    public Guid AssignedByEmployeeId { get; set; }
    public string? SubmissionMethod { get; set; }
    public decimal? MaxScore { get; set; }
    public decimal? PassingScore { get; set; }
    public int AttemptAllowed { get; set; } = 1;
    public int ReminderSentCount { get; set; }
    public bool IsMandatory { get; set; } = true;
    public string? CancelReason { get; set; }

    public Application? Application { get; set; }
    public Candidate? Candidate { get; set; }
    public Job? Job { get; set; }
    public Employee? AssignedByEmployee { get; set; }
    public LookupValue? TaskTypeLookupValue { get; set; }
    public LookupValue? StatusLookupValue { get; set; }
    public ICollection<CandidateTaskSubmission>? Submissions { get; set; }
    public ICollection<CandidateTaskEvaluation>? Evaluations { get; set; }
}
