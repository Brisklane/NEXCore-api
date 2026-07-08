namespace Hr.Application.DTOs;

public class JobFunctionDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string JobFunctionCode { get; set; } = string.Empty;
    public string JobFunctionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateJobFunctionDto
{
    public string JobFunctionCode { get; set; } = string.Empty;
    public string JobFunctionName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateJobFunctionDto
{
    public string? JobFunctionName { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}
