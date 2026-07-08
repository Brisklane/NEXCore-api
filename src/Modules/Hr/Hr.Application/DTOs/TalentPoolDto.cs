namespace Hr.Application.DTOs;

public class TalentPoolDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string TalentPoolCode { get; set; } = string.Empty;
    public string TalentPoolName { get; set; } = string.Empty;
    public string? CriteriaJson { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateTalentPoolDto
{
    public string TalentPoolCode { get; set; } = string.Empty;
    public string TalentPoolName { get; set; } = string.Empty;
    public string? CriteriaJson { get; set; }
}

public class UpdateTalentPoolDto
{
    public string? TalentPoolName { get; set; }
    public string? CriteriaJson { get; set; }
    public bool? IsActive { get; set; }
}
