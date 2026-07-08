using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hr.Domain.Entities;

public class Job : BaseEntity
{
    public string JobCode { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public JobRecordType RecordType { get; set; } = JobRecordType.Job;
    public Guid? ParentJobId { get; set; }

    public Guid DepartmentId { get; set; }
    public Guid DesignationId { get; set; }
    public int Headcount { get; set; }
    public int FilledCount { get; set; }
    public EmploymentType EmploymentType { get; set; }
    public decimal SalaryRangeMin { get; set; }
    public decimal SalaryRangeMax { get; set; }
    public DateTime? TargetStartDate { get; set; }
    public Guid? PriorityLookupValueId { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public Guid? StatusLookupValueId { get; set; }
    public Guid CurrencyId { get; set; }
    public Guid? HiringManagerEmployeeId { get; set; }
    public Guid? RecruiterEmployeeId { get; set; }
    public DateTime? PostingStartDate { get; set; }
    public DateTime? PostingCloseDate { get; set; }
    public Guid? ClosedByEmployeeId { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? CancelReason { get; set; }

    [NotMapped]
    public int OpenVacancies => Math.Max(0, Headcount - FilledCount);

    // Navigation
    public Department? Department { get; set; }
    public Designation? Designation { get; set; }
    public Currency? Currency { get; set; }
    public Employee? HiringManagerEmployee { get; set; }
    public Employee? RecruiterEmployee { get; set; }
    public Employee? ClosedByEmployee { get; set; }
    public Job? ParentJob { get; set; }
    public ApprovalRequest? ApprovalRequest { get; set; }
    public LookupValue? PriorityLookupValue { get; set; }
    public LookupValue? StatusLookupValue { get; set; }
    public ICollection<Job>? ChildJobs { get; set; }
    public ICollection<JobPostingChannel>? PostingChannels { get; set; }
    public ICollection<Application>? Applications { get; set; }
    public JobDetail? Detail { get; set; }
}
