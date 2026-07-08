
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class WorkflowEscalation : BaseEntity
{
    public Guid WorkflowConfigStepId { get; set; }
    public int AfterHours { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string ActionTarget { get; set; } = string.Empty;
    public int ReminderCount { get; set; }
    public bool AutoApproveFlag { get; set; }
    public WorkflowConfigStep? WorkflowConfigStep { get; set; }
}
