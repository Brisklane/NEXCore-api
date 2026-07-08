namespace Hr.Application.DTOs;

public class DepartmentDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public Guid? ParentDepartmentId { get; set; }
    public Guid? DepartmentHeadEmployeeId { get; set; }
    public Guid? CostCenterId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateDepartmentDto
{
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public Guid? ParentDepartmentId { get; set; }
    public Guid? DepartmentHeadEmployeeId { get; set; }
    public Guid? CostCenterId { get; set; }
}

public class UpdateDepartmentDto
{
    public string? DepartmentName { get; set; }
    public Guid? ParentDepartmentId { get; set; }
    public Guid? DepartmentHeadEmployeeId { get; set; }
    public Guid? CostCenterId { get; set; }
    public bool? IsActive { get; set; }
}
