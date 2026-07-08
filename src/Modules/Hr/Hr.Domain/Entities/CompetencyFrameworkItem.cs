
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class CompetencyFrameworkItem : BaseEntity
{
    public Guid CompetencyFrameworkId { get; set; }
    public Guid SkillId { get; set; }
    public decimal WeightPercent { get; set; }
    public decimal MinimumRating { get; set; }
    public bool IsMandatory { get; set; }
    public int SortOrder { get; set; }
    public string? Notes { get; set; }

    public CompetencyFramework? CompetencyFramework { get; set; }
    public Skill? Skill { get; set; }
}

