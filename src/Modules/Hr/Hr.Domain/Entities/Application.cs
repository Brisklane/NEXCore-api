
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class Application : BaseEntity
{
    public string ApplicationCode { get; set; } = string.Empty;
    public Guid JobId { get; set; }
    public Guid JobPostingChannelId { get; set; }
    public Guid CandidateId { get; set; }
    public DateTime AppliedDate { get; set; } = DateTime.UtcNow;
    public Guid CurrentStageLookupValueId { get; set; }
    public Guid StatusLookupValueId { get; set; }
    public Guid? PriorityLookupValueId { get; set; }
    public bool IsShortlisted { get; set; }
    public decimal? ScreeningScore { get; set; }
    public decimal? InternalScore { get; set; }
    public Guid? AssignedRecruiterEmployeeId { get; set; }
    public Guid? ConvertedToEmployeeId { get; set; }
    public Guid? OfferId { get; set; }
    public DateTime? HiredDate { get; set; }
    public DateTime? StageChangedAt { get; set; }

    public Job? Job { get; set; }
    public JobPostingChannel? JobPostingChannel { get; set; }
    public Candidate? Candidate { get; set; }
    public OfferLetter? Offer { get; set; }
    public Employee? AssignedRecruiterEmployee { get; set; }
    public Employee? ConvertedToEmployee { get; set; }
    public LookupValue? CurrentStageLookupValue { get; set; }
    public LookupValue? StatusLookupValue { get; set; }
    public LookupValue? PriorityLookupValue { get; set; }
    public ApplicationDetail? Detail { get; set; }
    public ApplicationCompliance? Compliance { get; set; }
    public ICollection<CandidateStageHistory>? StageHistories { get; set; }
    public ICollection<CallLog>? CallLogs { get; set; }
    public ICollection<CandidateTask>? Tasks { get; set; }
    public ICollection<Interview>? Interviews { get; set; }
    public ICollection<OnboardingTask>? OnboardingTasks { get; set; }
}
