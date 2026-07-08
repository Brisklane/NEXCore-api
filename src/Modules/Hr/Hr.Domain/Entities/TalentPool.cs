using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class TalentPool : BaseEntity
{
    public string TalentPoolCode { get; set; } = string.Empty;
    public string TalentPoolName { get; set; } = string.Empty;
    public string? CriteriaJson { get; set; }
    public ICollection<CandidateProfile>? CandidateProfiles { get; set; }
}
