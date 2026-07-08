namespace Hr.Application.DTOs;

public class OnboardingTaskTemplateDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public string TaskCategory { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DefaultAssigneeRole { get; set; } = string.Empty;
    public int DefaultDueDaysFromStart { get; set; }
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public Guid? ApplicableDepartmentId { get; set; }
    public Guid? ApplicableDesignationId { get; set; }
    public string? ApplicableEmploymentType { get; set; }
    public Guid? ApplicableLocationId { get; set; }
    public Guid? DependsOnTemplateId { get; set; }
    public decimal? EstimatedHours { get; set; }
    public bool RequiresDocumentUpload { get; set; }
    public bool RequiresManagerSignoff { get; set; }
    public bool NotifyEmployeeOnAssign { get; set; }
    public bool NotifyAssigneeOnCreate { get; set; }
    public int? EscalateAfterDays { get; set; }
    public string? EscalateToRole { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateOnboardingTaskTemplateDto
{
    public string TemplateCode { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public string TaskCategory { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DefaultAssigneeRole { get; set; } = string.Empty;
    public int DefaultDueDaysFromStart { get; set; }
    public bool IsRequired { get; set; } = true;
    public int SortOrder { get; set; }
    public Guid? ApplicableDepartmentId { get; set; }
    public Guid? ApplicableDesignationId { get; set; }
    public string? ApplicableEmploymentType { get; set; }
    public Guid? ApplicableLocationId { get; set; }
    public Guid? DependsOnTemplateId { get; set; }
    public decimal? EstimatedHours { get; set; }
    public bool RequiresDocumentUpload { get; set; }
    public bool RequiresManagerSignoff { get; set; }
    public bool NotifyEmployeeOnAssign { get; set; }
    public bool NotifyAssigneeOnCreate { get; set; }
    public int? EscalateAfterDays { get; set; }
    public string? EscalateToRole { get; set; }
}

public class UpdateOnboardingTaskTemplateDto
{
    public string? TaskName { get; set; }
    public string? TaskCategory { get; set; }
    public string? Description { get; set; }
    public string? DefaultAssigneeRole { get; set; }
    public int? DefaultDueDaysFromStart { get; set; }
    public bool? IsRequired { get; set; }
    public bool? IsActive { get; set; }
    public int? SortOrder { get; set; }
    public Guid? ApplicableDepartmentId { get; set; }
    public Guid? ApplicableDesignationId { get; set; }
    public string? ApplicableEmploymentType { get; set; }
    public Guid? ApplicableLocationId { get; set; }
    public decimal? EstimatedHours { get; set; }
    public bool? RequiresDocumentUpload { get; set; }
    public bool? RequiresManagerSignoff { get; set; }
    public int? EscalateAfterDays { get; set; }
    public string? EscalateToRole { get; set; }
}
