namespace Manufacturing.Application.DTOs;

public class BillOfMaterialDto
{
    public Guid Id { get; set; }
    public Guid FinishedProductId { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Notes { get; set; }
    public List<BOMItemDto> Items { get; set; } = [];
    public List<BOMByProductDto> ByProducts { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateBillOfMaterialDto
{
    public Guid FinishedProductId { get; set; }
    public int Version { get; set; } = 1;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Notes { get; set; }
    public List<CreateBOMItemDto> Items { get; set; } = [];
    public List<CreateBOMByProductDto> ByProducts { get; set; } = [];
}

public class UpdateBillOfMaterialDto
{
    public int? Version { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Notes { get; set; }
}

// ---- BOM Item ----
public class BOMItemDto
{
    public Guid Id { get; set; }
    public Guid BillOfMaterialId { get; set; }
    public Guid MaterialId { get; set; }
    public decimal QuantityRequired { get; set; }
    public decimal ScrapPercentage { get; set; }
    public required string UnitOfMeasure { get; set; }
    public string? Notes { get; set; }
}

public class CreateBOMItemDto
{
    public Guid MaterialId { get; set; }
    public decimal QuantityRequired { get; set; }
    public decimal ScrapPercentage { get; set; } = 0;
    public required string UnitOfMeasure { get; set; }
    public string? Notes { get; set; }
}

public class UpdateBOMItemDto
{
    public decimal? QuantityRequired { get; set; }
    public decimal? ScrapPercentage { get; set; }
    public string? UnitOfMeasure { get; set; }
    public string? Notes { get; set; }
}

// ---- BOM ByProduct ----
public class BOMByProductDto
{
    public Guid Id { get; set; }
    public Guid BillOfMaterialId { get; set; }
    public Guid ProductId { get; set; }
    public required string Type { get; set; }
    public decimal Quantity { get; set; }
    public required string UnitOfMeasure { get; set; }
    public decimal? CostAllocationPercent { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? Notes { get; set; }
}

public class CreateBOMByProductDto
{
    public Guid ProductId { get; set; }
    public required string Type { get; set; }
    public decimal Quantity { get; set; }
    public required string UnitOfMeasure { get; set; }
    public decimal? CostAllocationPercent { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? Notes { get; set; }
}

public class UpdateBOMByProductDto
{
    public decimal? Quantity { get; set; }
    public string? UnitOfMeasure { get; set; }
    public decimal? CostAllocationPercent { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? Notes { get; set; }
}
