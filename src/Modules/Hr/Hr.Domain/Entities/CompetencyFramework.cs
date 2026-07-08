
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class CompetencyFramework : BaseEntity
{
    public new string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? JobFamilyId { get; set; }
    public int VersionNumber { get; set; } = 1;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public ICollection<CompetencyFrameworkItem>? Items { get; set; }
}

