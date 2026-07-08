
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class Skill : BaseEntity
{
    public Guid SkillCategoryId { get; set; }
    public new string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? SkillTypeLookupValueId { get; set; }
    public bool IsCoreSkill { get; set; }

    public SkillCategory? SkillCategory { get; set; }
    public LookupValue? SkillTypeLookupValue { get; set; }
    public ICollection<CompetencyFrameworkItem>? CompetencyFrameworkItems { get; set; }
    public ICollection<CandidateSkill>? CandidateSkills { get; set; }
}

