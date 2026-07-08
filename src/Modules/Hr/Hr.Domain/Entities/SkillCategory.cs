
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class SkillCategory : BaseEntity
{
    public new string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ICollection<Skill>? Skills { get; set; }
}

