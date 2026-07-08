using Hr.Application.Enums;

namespace Hr.Application.DTOs;

public class EmployeeDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? DesignationId { get; set; }
    public Guid? PositionId { get; set; }
    public Guid? ReportingManagerId { get; set; }
    public Guid? JobLocationId { get; set; }
    public EmployeeStatus? Status { get; set; }
    public DateTime? JoinDate { get; set; }
    public DateTime? ExitDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateEmployeeDto
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? DesignationId { get; set; }
    public Guid? PositionId { get; set; }
    public Guid? ReportingManagerId { get; set; }
    public Guid? JobLocationId { get; set; }
    public EmployeeStatus? Status { get; set; }
    public DateTime? JoinDate { get; set; }
}

public class UpdateEmployeeDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MiddleName { get; set; }
    public string? Phone { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? DesignationId { get; set; }
    public Guid? PositionId { get; set; }
    public Guid? ReportingManagerId { get; set; }
    public Guid? JobLocationId { get; set; }
    public EmployeeStatus? Status { get; set; }
    public DateTime? ExitDate { get; set; }
    public bool? IsActive { get; set; }
}
