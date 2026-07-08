namespace Manufacturing.Application.DTOs;

public class ReworkOrderDto
{
    public Guid Id { get; set; }
    public Guid ProductionOrderId { get; set; }
    public Guid? InspectionId { get; set; }
    public Guid? ReworkRoutingId { get; set; }
    public decimal Quantity { get; set; }
    public decimal QuantityCompleted { get; set; }
    public decimal QuantityRejected { get; set; }
    public required string UnitOfMeasure { get; set; }
    public required string Reason { get; set; }
    public required string Status { get; set; }
    public DateTime? ScheduledStartDate { get; set; }
    public DateTime? ScheduledEndDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateReworkOrderDto
{
    public Guid ProductionOrderId { get; set; }
    public Guid? InspectionId { get; set; }
    public Guid? ReworkRoutingId { get; set; }
    public decimal Quantity { get; set; }
    public required string UnitOfMeasure { get; set; }
    public required string Reason { get; set; }
    public DateTime? ScheduledStartDate { get; set; }
    public DateTime? ScheduledEndDate { get; set; }
    public string? Notes { get; set; }
}

public class UpdateReworkOrderDto
{
    public decimal? QuantityCompleted { get; set; }
    public decimal? QuantityRejected { get; set; }
    public string? Status { get; set; }
    public DateTime? ScheduledStartDate { get; set; }
    public DateTime? ScheduledEndDate { get; set; }
    public string? Notes { get; set; }
}
