namespace Manufacturing.Application.DTOs;

public class MaterialPlanningDataDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public decimal SafetyStock { get; set; }
    public decimal ReorderPoint { get; set; }
    public decimal MaximumStockLevel { get; set; }
    public decimal LotSize { get; set; }
    public int LeadTimeDays { get; set; }
    public int PlanningHorizonDays { get; set; }
    public decimal ScrapPercentage { get; set; }
    public required string ProcurementType { get; set; }
    public required string MRPType { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateMaterialPlanningDataDto
{
    public Guid ProductId { get; set; }
    public decimal SafetyStock { get; set; }
    public decimal ReorderPoint { get; set; }
    public decimal MaximumStockLevel { get; set; }
    public decimal LotSize { get; set; }
    public int LeadTimeDays { get; set; }
    public int PlanningHorizonDays { get; set; } = 90;
    public decimal ScrapPercentage { get; set; } = 0;
    public required string ProcurementType { get; set; }
    public required string MRPType { get; set; }
    public string? Notes { get; set; }
}

public class UpdateMaterialPlanningDataDto
{
    public decimal? SafetyStock { get; set; }
    public decimal? ReorderPoint { get; set; }
    public decimal? MaximumStockLevel { get; set; }
    public decimal? LotSize { get; set; }
    public int? LeadTimeDays { get; set; }
    public int? PlanningHorizonDays { get; set; }
    public decimal? ScrapPercentage { get; set; }
    public string? ProcurementType { get; set; }
    public string? MRPType { get; set; }
    public bool? IsActive { get; set; }
    public string? Notes { get; set; }
}
