
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class WorkflowCondition : BaseEntity
{
    public Guid WorkflowConfigId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string FieldValue { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string ActionValue { get; set; } = string.Empty;
    public int LogicalGroup { get; set; }
    public string JoinOperator { get; set; } = string.Empty;

    public WorkflowConfig? WorkflowConfig { get; set; }
}
