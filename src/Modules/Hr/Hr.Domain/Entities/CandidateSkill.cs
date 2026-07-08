
using Nexcore.SharedKernel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hr.Domain.Entities;

public class CandidateSkill : BaseEntity
{
    public Guid CandidateId { get; set; }
    public Guid SkillId { get; set; }
    public Guid ProficiencyLookupValueId { get; set; }
    public decimal? YearsExperience { get; set; }
    public bool IsVerified { get; set; }
    public Guid? VerifiedByEmployeeId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? Source { get; set; }
    public string? Notes { get; set; }

    public Candidate? Candidate { get; set; }
    public Skill? Skill { get; set; }
    public LookupValue? ProficiencyLookupValue { get; set; }
    public Employee? VerifiedByEmployee { get; set; }
}
