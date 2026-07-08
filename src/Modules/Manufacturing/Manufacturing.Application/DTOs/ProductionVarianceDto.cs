namespace Manufacturing.Application.DTOs;

public class ProductionVarianceDto
{
    public Guid Id { get; set; }
    public Guid ProductionOrderId { get; set; }
    public Guid CostEntryId { get; set; }
    public decimal StandardMaterialCost { get; set; }
    public decimal ActualMaterialCost { get; set; }
    public decimal MaterialVariance { get; set; }
    public decimal StandardLaborCost { get; set; }
    public decimal ActualLaborCost { get; set; }
    public decimal LaborVariance { get; set; }
    public decimal StandardMachineCost { get; set; }
    public decimal ActualMachineCost { get; set; }
    public decimal MachineVariance { get; set; }
    public decimal StandardOverheadCost { get; set; }
    public decimal ActualOverheadCost { get; set; }
    public decimal OverheadVariance { get; set; }
    public decimal TotalVariance { get; set; }
    public required string VarianceCategory { get; set; }
    public bool IsSettled { get; set; }
    public Guid? SettlementJournalEntryId { get; set; }
    public DateTime? SettledAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateProductionVarianceDto
{
    public Guid ProductionOrderId { get; set; }
    public Guid CostEntryId { get; set; }
    public decimal StandardMaterialCost { get; set; }
    public decimal ActualMaterialCost { get; set; }
    public decimal StandardLaborCost { get; set; }
    public decimal ActualLaborCost { get; set; }
    public decimal StandardMachineCost { get; set; }
    public decimal ActualMachineCost { get; set; }
    public decimal StandardOverheadCost { get; set; }
    public decimal ActualOverheadCost { get; set; }
    public required string VarianceCategory { get; set; }
    public string? Notes { get; set; }
}

public class UpdateProductionVarianceDto
{
    public bool? IsSettled { get; set; }
    public Guid? SettlementJournalEntryId { get; set; }
    public DateTime? SettledAt { get; set; }
    public string? Notes { get; set; }
}
