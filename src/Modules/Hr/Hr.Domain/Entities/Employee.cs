using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class Employee : BaseEntity
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
    public string? Status { get; set; }
    public DateTime? JoinDate { get; set; }
    public DateTime? ExitDate { get; set; }
    public Department? Department { get; set; }
    public Designation? Designation { get; set; }
    public Position? Position { get; set; }
    public Employee? ReportingManager { get; set; }
    public ICollection<Employee>? DirectReports { get; set; }
    public JobLocation? JobLocation { get; set; }
}
