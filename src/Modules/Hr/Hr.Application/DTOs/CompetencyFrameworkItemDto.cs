namespace Hr.Application.DTOs;

public class CompetencyFrameworkItemDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid CompetencyFrameworkId { get; set; }
    public Guid SkillId { get; set; }
    public decimal WeightPercent { get; set; }
    public decimal MinimumRating { get; set; }
    public bool IsMandatory { get; set; }
    public int SortOrder { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCompetencyFrameworkItemDto
{
    public Guid CompetencyFrameworkId { get; set; }
    public Guid SkillId { get; set; }
    public decimal WeightPercent { get; set; }
    public decimal MinimumRating { get; set; }
    public bool IsMandatory { get; set; }
    public int SortOrder { get; set; }
    public string? Notes { get; set; }
}

public class UpdateCompetencyFrameworkItemDto
{
    public Guid? SkillId { get; set; }
    public decimal? WeightPercent { get; set; }
    public decimal? MinimumRating { get; set; }
    public bool? IsMandatory { get; set; }
    public int? SortOrder { get; set; }
    public string? Notes { get; set; }
}
