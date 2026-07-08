namespace Hr.Application.DTOs;

public class WorkflowConfigStepDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid WorkflowConfigId { get; set; }
    public int LevelNo { get; set; }
    public string ApproverType { get; set; } = string.Empty;
    public string? ApproverValue { get; set; }
    public bool Mandatory { get; set; }
    public int SLAHours { get; set; }
    public string ExecutionType { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsConditional { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateWorkflowConfigStepDto
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
}

public class UpdateWorkflowConfigStepDto
{
    public int? LevelNo { get; set; }
    public string? ApproverType { get; set; }
    public string? ApproverValue { get; set; }
    public bool? Mandatory { get; set; }
    public int? SLAHours { get; set; }
    public string? ExecutionType { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsConditional { get; set; }
}
