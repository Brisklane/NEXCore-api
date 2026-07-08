
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class WorkflowConfigStep : BaseEntity
{
    public Guid WorkflowConfigId { get; set; }
    public int LevelNo { get; set; }
    public string ApproverType { get; set; } = string.Empty;
    public string? ApproverValue { get; set; }
    public bool Mandatory { get; set; } = true;
    public int SLAHours { get; set; }
    public string ExecutionType { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsConditional { get; set; }

    public WorkflowConfig? WorkflowConfig { get; set; }
    public ICollection<WorkflowEscalation>? Escalations { get; set; }
}
