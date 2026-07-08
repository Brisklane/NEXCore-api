namespace Hr.Application.DTOs;

public class WorkflowEscalationDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid WorkflowConfigStepId { get; set; }
    public int AfterHours { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string ActionTarget { get; set; } = string.Empty;
    public int ReminderCount { get; set; }
    public bool AutoApproveFlag { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateWorkflowEscalationDto
{
    public Guid WorkflowConfigStepId { get; set; }
    public int AfterHours { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string ActionTarget { get; set; } = string.Empty;
    public int ReminderCount { get; set; }
    public bool AutoApproveFlag { get; set; }
}

public class UpdateWorkflowEscalationDto
{
    public int? AfterHours { get; set; }
    public string? ActionType { get; set; }
    public string? ActionTarget { get; set; }
    public int? ReminderCount { get; set; }
    public bool? AutoApproveFlag { get; set; }
    public bool? IsActive { get; set; }
}
