
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class OnboardingTaskTemplate : BaseEntity
{
    public string TemplateCode { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public string TaskCategory { get; set; } = string.Empty;
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
    public string? LegacyTemplateId { get; set; }

    public Department? ApplicableDepartment { get; set; }
    public Designation? ApplicableDesignation { get; set; }
    public JobLocation? ApplicableLocation { get; set; }
    public OnboardingTaskTemplate? DependsOnTemplate { get; set; }
    public ICollection<OnboardingTaskTemplate>? DependentTemplates { get; set; }
}
