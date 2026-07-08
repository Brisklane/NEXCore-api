namespace Manufacturing.Application.DTOs;

public class CostEntryDto
{
    public Guid Id { get; set; }
    public Guid ProductionOrderId { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal? MachineCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal? ScrapCost { get; set; }
    public decimal TotalCost { get; set; }
    public Guid? JournalEntryId { get; set; }
    public DateTime PostedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCostEntryDto
{
    public Guid ProductionOrderId { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal? MachineCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal? ScrapCost { get; set; }
    public DateTime PostedAt { get; set; }
    public string? Notes { get; set; }
}

public class UpdateCostEntryDto
{
    public decimal? MaterialCost { get; set; }
    public decimal? LaborCost { get; set; }
    public decimal? MachineCost { get; set; }
    public decimal? OverheadCost { get; set; }
    public decimal? ScrapCost { get; set; }
    public Guid? JournalEntryId { get; set; }
    public string? Notes { get; set; }
}
