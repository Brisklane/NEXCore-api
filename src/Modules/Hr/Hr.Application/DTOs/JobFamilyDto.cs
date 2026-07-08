namespace Hr.Application.DTOs;

public class JobFamilyDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string JobFamilyCode { get; set; } = string.Empty;
    public string JobFamilyName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateJobFamilyDto
{
    public string JobFamilyCode { get; set; } = string.Empty;
    public string JobFamilyName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateJobFamilyDto
{
    public string? JobFamilyName { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}
