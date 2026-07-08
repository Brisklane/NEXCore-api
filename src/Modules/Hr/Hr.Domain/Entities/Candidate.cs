
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class Candidate : BaseEntity
{
    public string CandidateCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? ResumeUrl { get; set; }
    public string? Source { get; set; }
    public decimal? TotalExperienceYears { get; set; }
    public decimal? CurrentSalary { get; set; }
    public decimal? ExpectedSalary { get; set; }
    public string? CandidateRating { get; set; }
    public bool IsBlacklisted { get; set; }
    public Guid? BlacklistReasonLookupValueId { get; set; }
    public DateTime? BlacklistedAt { get; set; }
    public Guid? BlacklistedByEmployeeId { get; set; }
    public bool ConsentGiven { get; set; }
    public DateTime? DataRetentionExpiryDate { get; set; }
    public Guid? DuplicateOfCandidateId { get; set; }
    public Guid? MergedIntoCandidateId { get; set; }
    public string? LegacyCandidateId { get; set; }
    public string? LegacySourceSystem { get; set; }

    public LookupValue? BlacklistReasonLookupValue { get; set; }
    public string? CurrentCompany { get; set; }
    public Guid? CurrentDesignationId { get; set; }
    public Designation? CurrentDesignation { get; set; }
    public Employee? BlacklistedByEmployee { get; set; }
    public Candidate? DuplicateOfCandidate { get; set; }
    public Candidate? MergedIntoCandidate { get; set; }
    public CandidateProfile? Profile { get; set; }
    public ICollection<Application>? Applications { get; set; }
    public ICollection<CandidateStageHistory>? StageHistories { get; set; }
    public ICollection<CallLog>? CallLogs { get; set; }
    public ICollection<CandidateSkill>? CandidateSkills { get; set; }
    public ICollection<CandidateContact>? Contacts { get; set; }
    public ICollection<CandidateAddress>? Addresses { get; set; }
    public ICollection<CandidateMediaLink>? MediaLinks { get; set; }
}
