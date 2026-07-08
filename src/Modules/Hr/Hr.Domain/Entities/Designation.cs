using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class Designation : BaseEntity
{
    public string DesignationCode { get; set; } = string.Empty;
    public string DesignationName { get; set; } = string.Empty;
    public Guid? JobFamilyId { get; set; }
    public Guid? JobFunctionId { get; set; }
    public Guid? GradeId { get; set; }
    public JobFamily? JobFamily { get; set; }
    public JobFunction? JobFunction { get; set; }
    public Grade? Grade { get; set; }
    public ICollection<Employee>? Employees { get; set; }
    public ICollection<Position>? Positions { get; set; }
    public ICollection<JobTemplate>? JobTemplates { get; set; }
}
