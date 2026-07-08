
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class WorkflowConfig : BaseEntity
{
    public string WorkflowCode { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public string WorkflowName { get; set; } = string.Empty;
    public int TotalLevels { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int VersionNo { get; set; } = 1;

    public ICollection<WorkflowConfigStep>? Steps { get; set; }
    public ICollection<WorkflowCondition>? Conditions { get; set; }
    public ICollection<ApprovalRequest>? ApprovalRequests { get; set; }
}
