namespace Hr.Application.DTOs;

public class JobTemplateDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public Guid? DesignationId { get; set; }
    public string EmploymentType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Requirements { get; set; }
    public string? RequiredSkills { get; set; }
    public string? Responsibilities { get; set; }
    public bool IsActive { get; set; }
    public int VersionNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateJobTemplateDto
{
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public Guid? DesignationId { get; set; }
    public string EmploymentType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Requirements { get; set; }
    public string? RequiredSkills { get; set; }
    public string? Responsibilities { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateJobTemplateDto
{
    public string? TemplateName { get; set; }
    public string? JobTitle { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? DesignationId { get; set; }
    public string? EmploymentType { get; set; }
    public string? Description { get; set; }
    public string? Requirements { get; set; }
    public string? RequiredSkills { get; set; }
    public string? Responsibilities { get; set; }
    public bool? IsActive { get; set; }
}
