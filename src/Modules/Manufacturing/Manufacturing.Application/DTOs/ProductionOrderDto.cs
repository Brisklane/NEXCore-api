namespace Manufacturing.Application.DTOs;

public class ProductionOrderDto
{
    public Guid Id { get; set; }
    public required string OrderNumber { get; set; }
    public Guid ProductId { get; set; }
    public Guid BillOfMaterialId { get; set; }
    public Guid RoutingId { get; set; }
    public Guid? PlannedOrderId { get; set; }
    public decimal QuantityPlanned { get; set; }
    public decimal QuantityProduced { get; set; }
    public decimal QuantityRejected { get; set; }
    public required string Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateProductionOrderDto
{
    public required string OrderNumber { get; set; }
    public Guid ProductId { get; set; }
    public Guid BillOfMaterialId { get; set; }
    public Guid RoutingId { get; set; }
    public Guid? PlannedOrderId { get; set; }
    public decimal QuantityPlanned { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }
}

public class UpdateProductionOrderDto
{
    public decimal? QuantityPlanned { get; set; }
    public decimal? QuantityProduced { get; set; }
    public decimal? QuantityRejected { get; set; }
    public string? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }
}
