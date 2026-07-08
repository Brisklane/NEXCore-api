namespace Hr.Application.DTOs;

public class BenefitsPlanDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string BenefitsPlanCode { get; set; } = string.Empty;
    public string BenefitsPlanName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateBenefitsPlanDto
{
    public string BenefitsPlanCode { get; set; } = string.Empty;
    public string BenefitsPlanName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateBenefitsPlanDto
{
    public string? BenefitsPlanName { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}
