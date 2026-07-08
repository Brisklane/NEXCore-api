using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class CostCenter : BaseEntity
{
    public string CostCenterCode { get; set; } = string.Empty;
    public string CostCenterName { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public Guid? BudgetOwnerEmployeeId { get; set; }
    public Department? Department { get; set; }
    public Employee? BudgetOwnerEmployee { get; set; }
}
