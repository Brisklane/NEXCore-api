namespace Hr.Application.DTOs;

public class JobDetailDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid JobId { get; set; }
    public Guid? JobTemplateId { get; set; }
    public Guid? JobLocationId { get; set; }
    public Guid? PositionId { get; set; }
    public string? Description { get; set; }
    public string? Requirements { get; set; }
    public decimal? MinExperienceYears { get; set; }
    public decimal? MaxExperienceYears { get; set; }
    public bool PublishExternallyFlag { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateJobDetailDto
{
    public Guid JobId { get; set; }
    public Guid? JobTemplateId { get; set; }
    public Guid? JobLocationId { get; set; }
    public Guid? PositionId { get; set; }
    public string? Description { get; set; }
    public string? Requirements { get; set; }
    public decimal? MinExperienceYears { get; set; }
    public decimal? MaxExperienceYears { get; set; }
    public bool PublishExternallyFlag { get; set; }
}

public class UpdateJobDetailDto
{
    public Guid? JobTemplateId { get; set; }
    public Guid? JobLocationId { get; set; }
    public Guid? PositionId { get; set; }
    public string? Description { get; set; }
    public string? Requirements { get; set; }
    public decimal? MinExperienceYears { get; set; }
    public decimal? MaxExperienceYears { get; set; }
    public bool? PublishExternallyFlag { get; set; }
}
