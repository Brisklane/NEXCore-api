using Hr.Application.Enums;
using Nexcore.SharedKernel.Enums;

namespace Hr.Application.DTOs;

public class JobDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string JobCode { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public JobRecordType RecordType { get; set; } = JobRecordType.Job;
    public Guid? ParentJobId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid DesignationId { get; set; }
    public int Headcount { get; set; }
    public int FilledCount { get; set; }
    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;
    public Guid? PriorityLookupValueId { get; set; }
    public Guid? StatusLookupValueId { get; set; }
    public Guid? HiringManagerEmployeeId { get; set; }
    public Guid? RecruiterEmployeeId { get; set; }
    public Guid CurrencyId { get; set; }
    public decimal SalaryRangeMin { get; set; }
    public decimal SalaryRangeMax { get; set; }
    public DateTime? TargetStartDate { get; set; }
    public DateTime? PostingStartDate { get; set; }
    public DateTime? PostingCloseDate { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public Guid? ClosedByEmployeeId { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? CancelReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateJobDto
{
    public string JobCode { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public JobRecordType RecordType { get; set; } = JobRecordType.Job;
    public Guid? ParentJobId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid DesignationId { get; set; }
    public int Headcount { get; set; }
    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;
    public Guid? PriorityLookupValueId { get; set; }
    public Guid? StatusLookupValueId { get; set; }
    public Guid? HiringManagerEmployeeId { get; set; }
    public Guid? RecruiterEmployeeId { get; set; }
    public Guid CurrencyId { get; set; }
    public decimal SalaryRangeMin { get; set; }
    public decimal SalaryRangeMax { get; set; }
    public DateTime? TargetStartDate { get; set; }
    public DateTime? PostingStartDate { get; set; }
    public DateTime? PostingCloseDate { get; set; }

    /// <summary>
    /// Required when RecordType is Requisition.
    /// Identifies the employee submitting the approval request.
    /// </summary>
    public Guid? RequestedByEmployeeId { get; set; }
}

public class UpdateJobDto
{
    public string? JobTitle { get; set; }
    public EmploymentType? EmploymentType { get; set; }
    public int? Headcount { get; set; }
    public Guid? PriorityLookupValueId { get; set; }
    public Guid? StatusLookupValueId { get; set; }
    public Guid? RecruiterEmployeeId { get; set; }
    public decimal? SalaryRangeMin { get; set; }
    public decimal? SalaryRangeMax { get; set; }
    public DateTime? TargetStartDate { get; set; }
    public DateTime? PostingStartDate { get; set; }
    public DateTime? PostingCloseDate { get; set; }
}
