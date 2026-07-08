namespace Hr.Application.DTOs;

// Read models

public class ApprovalRequestDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string ApprovalRequestCode { get; set; } = string.Empty;

    /// <summary>E.g. "Job", "OfferLetter"</summary>
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }

    public Guid WorkflowConfigId { get; set; }
    public Guid RequestedByEmployeeId { get; set; }

    public int CurrentLevel { get; set; }
    public int TotalLevels { get; set; }

    public Guid OverallStatusLookupValueId { get; set; }
    public string? OverallStatusLabel { get; set; }

    public Guid PriorityLookupValueId { get; set; }
    public string? PriorityLabel { get; set; }

    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public string? Comments { get; set; }
    public string? ApprovalSubjectCode { get; set; }
    public string? ApprovalSubjectTitle { get; set; }
    public string? ApprovalSummary { get; set; }
    public string? ApprovalDisplayName { get; set; }

    public List<ApprovalRequestStepDto> Steps { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ApprovalRequestStepDto
{
    public Guid Id { get; set; }
    public Guid ApprovalRequestId { get; set; }
    public Guid WorkflowConfigStepId { get; set; }

    public int StepLevel { get; set; }
    public string ApproverType { get; set; } = string.Empty;
    public string? ApproverValue { get; set; }
    public Guid? ApproverEmployeeId { get; set; }

    public bool Mandatory { get; set; }
    public int SLAHours { get; set; }
    public string ExecutionType { get; set; } = string.Empty;

    public Guid StatusLookupValueId { get; set; }
    public string? StatusLabel { get; set; }

    public string? Comments { get; set; }
    public string? RejectionReason { get; set; }
    public string? RejectionCategory { get; set; }
    public DateTime? ActionDate { get; set; }

    public Guid? DelegatedToEmployeeId { get; set; }
    public bool EscalatedFlag { get; set; }
    public Guid? EscalatedToEmployeeId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// Command models

/// <summary>
/// Submit a new approval request for any HR entity.
/// The service will look up the WorkflowConfig by EntityType, evaluate WorkflowConditions
/// against EntityData, build the final step chain, and persist everything.
/// </summary>
public class SubmitApprovalRequestDto
{
    /// <summary>Entity type name, e.g. "Requisition", "OfferLetter".</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Primary key of the entity being approved.</summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Workflow config to use. If omitted the service auto-resolves the active
    /// WorkflowConfig matching TransactionType == EntityType.
    /// </summary>
    public Guid? WorkflowConfigId { get; set; }

    public Guid RequestedByEmployeeId { get; set; }
    public Guid PriorityLookupValueId { get; set; }

    /// <summary>Human-readable subject shown in notifications.</summary>
    public string? ApprovalSubjectCode { get; set; }
    public string? ApprovalSubjectTitle { get; set; }
    public string? ApprovalSummary { get; set; }
    public string? ApprovalDisplayName { get; set; }
    public string? Comments { get; set; }

    /// <summary>
    /// Key/value snapshot of the entity's fields used to evaluate WorkflowConditions.
    /// Example for a Job Requisition:
    ///   { "SalaryRangeMax": "150000", "Priority": "CRITICAL", "DepartmentId": "..." }
    /// The condition engine reads these values to decide whether to add/replace steps.
    /// </summary>
    public Dictionary<string, string> EntityData { get; set; } = [];
}

/// <summary>Approve the current pending step of an approval request.</summary>
public class ApproveStepDto
{
    public Guid ApproverEmployeeId { get; set; }
    public string? Comments { get; set; }
}

/// <summary>Reject the current pending step, stopping the whole request.</summary>
public class RejectStepDto
{
    public Guid ApproverEmployeeId { get; set; }
    public string RejectionReason { get; set; } = string.Empty;
    public string? RejectionCategory { get; set; }
    public string? Comments { get; set; }
}

/// <summary>Delegate a step to another employee.</summary>
public class DelegateStepDto
{
    public Guid FromEmployeeId { get; set; }
    public Guid ToEmployeeId { get; set; }
    public string? Comments { get; set; }
}

/// <summary>Update mutable header fields of a draft/pending approval request.</summary>
public class UpdateApprovalRequestDto
{
    public Guid? PriorityLookupValueId { get; set; }
    public string? Comments { get; set; }
    public string? ApprovalSubjectTitle { get; set; }
    public string? ApprovalSummary { get; set; }
    public string? ApprovalDisplayName { get; set; }
}
