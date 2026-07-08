namespace Manufacturing.Application.DTOs;

public class ProductionOrderComponentDto
{
    public Guid Id { get; set; }
    public Guid ProductionOrderId { get; set; }
    public Guid? BOMItemId { get; set; }
    public Guid MaterialId { get; set; }
    public decimal PlannedQty { get; set; }
    public decimal IssuedQty { get; set; }
    public decimal ReturnedQty { get; set; }
    public required string UnitOfMeasure { get; set; }
    public decimal ScrapPercentage { get; set; }
    public bool IsSubstituted { get; set; }
    public Guid? OriginalMaterialId { get; set; }
    public Guid? StorageLocationId { get; set; }
    public bool IsManuallyAdded { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateProductionOrderComponentDto
{
    public Guid ProductionOrderId { get; set; }
    public Guid? BOMItemId { get; set; }
    public Guid MaterialId { get; set; }
    public decimal PlannedQty { get; set; }
    public required string UnitOfMeasure { get; set; }
    public decimal ScrapPercentage { get; set; } = 0;
    public Guid? StorageLocationId { get; set; }
    public bool IsManuallyAdded { get; set; } = false;
    public string? Notes { get; set; }
}

public class UpdateProductionOrderComponentDto
{
    public decimal? PlannedQty { get; set; }
    public decimal? IssuedQty { get; set; }
    public decimal? ReturnedQty { get; set; }
    public string? UnitOfMeasure { get; set; }
    public decimal? ScrapPercentage { get; set; }
    public bool? IsSubstituted { get; set; }
    public Guid? OriginalMaterialId { get; set; }
    public Guid? StorageLocationId { get; set; }
    public string? Notes { get; set; }
}
