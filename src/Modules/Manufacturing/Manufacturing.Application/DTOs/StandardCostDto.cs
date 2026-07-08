namespace Manufacturing.Application.DTOs;

public class StandardCostDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public int Version { get; set; }
    public required string CurrencyCode { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal MachineCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal TotalCost { get; set; }
    public bool IsActive { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateStandardCostDto
{
    public Guid ProductId { get; set; }
    public int Version { get; set; } = 1;
    public required string CurrencyCode { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal MachineCost { get; set; }
    public decimal OverheadCost { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Notes { get; set; }
}

public class UpdateStandardCostDto
{
    public decimal? MaterialCost { get; set; }
    public decimal? LaborCost { get; set; }
    public decimal? MachineCost { get; set; }
    public decimal? OverheadCost { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Notes { get; set; }
}
