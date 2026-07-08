namespace Hr.Application.DTOs;

public class CostCenterDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CostCenterCode { get; set; } = string.Empty;
    public string CostCenterName { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public Guid? BudgetOwnerEmployeeId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCostCenterDto
{
    public string CostCenterCode { get; set; } = string.Empty;
    public string CostCenterName { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public Guid? BudgetOwnerEmployeeId { get; set; }
}

public class UpdateCostCenterDto
{
    public string? CostCenterName { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? BudgetOwnerEmployeeId { get; set; }
    public bool? IsActive { get; set; }
}
