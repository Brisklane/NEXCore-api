namespace Hr.Application.DTOs;

public class WorkflowConditionDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid WorkflowConfigId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string FieldValue { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string ActionValue { get; set; } = string.Empty;
    public int LogicalGroup { get; set; }
    public string JoinOperator { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateWorkflowConditionDto
{
    public Guid WorkflowConfigId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string FieldValue { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string ActionValue { get; set; } = string.Empty;
    public int LogicalGroup { get; set; }
    public string JoinOperator { get; set; } = string.Empty;
}

public class UpdateWorkflowConditionDto
{
    public string? FieldName { get; set; }
    public string? Operator { get; set; }
    public string? FieldValue { get; set; }
    public string? ActionType { get; set; }
    public string? ActionValue { get; set; }
    public int? LogicalGroup { get; set; }
    public string? JoinOperator { get; set; }
}
