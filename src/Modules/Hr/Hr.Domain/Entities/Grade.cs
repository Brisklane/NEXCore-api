using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class Grade : BaseEntity
{
    public string GradeCode { get; set; } = string.Empty;
    public string GradeName { get; set; } = string.Empty;
    public int LevelNo { get; set; }
    public ICollection<Designation>? Designations { get; set; }
    public ICollection<Position>? Positions { get; set; }
    public ICollection<JobTemplate>? JobTemplates { get; set; }
}
