namespace Hr.Application.DTOs;

public class GradeDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string GradeCode { get; set; } = string.Empty;
    public string GradeName { get; set; } = string.Empty;
    public int LevelNo { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateGradeDto
{
    public string GradeCode { get; set; } = string.Empty;
    public string GradeName { get; set; } = string.Empty;
    public int LevelNo { get; set; }
}

public class UpdateGradeDto
{
    public string? GradeName { get; set; }
    public int? LevelNo { get; set; }
    public bool? IsActive { get; set; }
}
