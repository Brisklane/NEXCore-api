using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class Department : BaseEntity
{
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public Guid? ParentDepartmentId { get; set; }
    public Guid? DepartmentHeadEmployeeId { get; set; }
    public Guid? CostCenterId { get; set; }
    public Department? ParentDepartment { get; set; }
    public Employee? DepartmentHeadEmployee { get; set; }
    public CostCenter? CostCenter { get; set; }
    public ICollection<Department>? ChildDepartments { get; set; }
    public ICollection<Employee>? Employees { get; set; }
    public ICollection<Designation>? Designations { get; set; }
    public ICollection<Position>? Positions { get; set; }
}
