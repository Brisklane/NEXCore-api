namespace Manufacturing.Application.DTOs;

public class OverheadRuleDto
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public Guid? WorkCenterId { get; set; }
    public required string RateType { get; set; }
    public decimal Value { get; set; }
    public required string AppliesTo { get; set; }
    public bool IsActive { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateOverheadRuleDto
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public Guid? WorkCenterId { get; set; }
    public required string RateType { get; set; }
    public decimal Value { get; set; }
    public required string AppliesTo { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Notes { get; set; }
}

public class UpdateOverheadRuleDto
{
    public string? Name { get; set; }
    public string? RateType { get; set; }
    public decimal? Value { get; set; }
    public string? AppliesTo { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Notes { get; set; }
}
