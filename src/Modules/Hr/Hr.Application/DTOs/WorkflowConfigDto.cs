namespace Hr.Application.DTOs;

public class WorkflowConfigDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string WorkflowCode { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public string WorkflowName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int TotalLevels { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Description { get; set; }
    public int VersionNo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateWorkflowConfigDto
{
    public string WorkflowCode { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public string WorkflowName { get; set; } = string.Empty;
    public int TotalLevels { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Description { get; set; }
}

public class UpdateWorkflowConfigDto
{
    public string? WorkflowName { get; set; }
    public string? Module { get; set; }
    public string? TransactionType { get; set; }
    public int? TotalLevels { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Description { get; set; }
}
