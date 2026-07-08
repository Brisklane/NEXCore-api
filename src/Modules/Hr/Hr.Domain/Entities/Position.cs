using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class Position : BaseEntity
{
    public string PositionCode { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public Guid DesignationId { get; set; }
    public Guid? JobFamilyId { get; set; }
    public Guid? JobFunctionId { get; set; }
    public Guid? GradeId { get; set; }
    public Guid? PayScaleId { get; set; }
    public Guid? ReportsToPositionId { get; set; }
    public bool IsVacant { get; set; }
    public Department? Department { get; set; }
    public Designation? Designation { get; set; }
    public JobFamily? JobFamily { get; set; }
    public JobFunction? JobFunction { get; set; }
    public Grade? Grade { get; set; }
    public PayScale? PayScale { get; set; }
    public Position? ReportsToPosition { get; set; }
    public ICollection<Position>? ChildPositions { get; set; }
    public ICollection<Employee>? Employees { get; set; }
    public ICollection<JobTemplate>? JobTemplates { get; set; }
}
