
using Nexcore.SharedKernel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hr.Domain.Entities;

public class ApprovalRequest : BaseEntity
{
    public string ApprovalRequestCode { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public Guid WorkflowConfigId { get; set; }
    public Guid RequestedByEmployeeId { get; set; }
    public int CurrentLevel { get; set; }
    public int TotalLevels { get; set; }
    public Guid OverallStatusLookupValueId { get; set; }
    public Guid PriorityLookupValueId { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? Comments { get; set; }
    public string? ReferenceNotes { get; set; }
    public string? ApprovalSubjectCode { get; set; }
    public string? ApprovalSubjectTitle { get; set; }
    public string? ApprovalSummary { get; set; }
    public string? ApprovalDisplayName { get; set; }

    public WorkflowConfig? WorkflowConfig { get; set; }
    public Employee? RequestedByEmployee { get; set; }
    public LookupValue? OverallStatusLookupValue { get; set; }
    public LookupValue? PriorityLookupValue { get; set; }
    public ICollection<ApprovalRequestStep>? Steps { get; set; }
}
