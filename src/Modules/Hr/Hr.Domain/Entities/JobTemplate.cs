
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class JobTemplate : BaseEntity
{
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public Guid? DesignationId { get; set; }
    public string EmploymentType { get; set; } = string.Empty;
    public string? Requirements { get; set; }
    public string? RequiredSkills { get; set; }
    public string? Responsibilities { get; set; }
    public Guid? JobFamilyId { get; set; }
    public Guid? JobFunctionId { get; set; }
    public Guid? GradeId { get; set; }
    public string? WorkerCategory { get; set; }
    public string? RemoteType { get; set; }
    public decimal? MinExperienceYears { get; set; }
    public decimal? MaxExperienceYears { get; set; }
    public string? EducationRequirements { get; set; }
    public string? PreferredSkills { get; set; }
    public string? LanguagesRequired { get; set; }
    public int VersionNumber { get; set; } = 1;
    public Guid? ParentTemplateId { get; set; }
    public string? LegacyTemplateId { get; set; }
    public string? LegacySourceSystem { get; set; }

    public ICollection<Job>? Jobs { get; set; }
}

