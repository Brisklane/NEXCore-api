namespace Manufacturing.Application.DTOs;

public class WorkInProgressDto
{
    public Guid Id { get; set; }
    public Guid ProductionOrderId { get; set; }
    public string? ProductionOrderNumber { get; set; }
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal QuantityInProgress { get; set; }
    public decimal QuantityCompleted { get; set; }
    public decimal QuantityRejected { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateWorkInProgressDto
{
    public Guid ProductionOrderId { get; set; }
    public decimal QuantityInProgress { get; set; }
    public string? Notes { get; set; }
}

public class UpdateWorkInProgressDto
{
    public decimal? QuantityInProgress { get; set; }
    public decimal? QuantityCompleted { get; set; }
    public decimal? QuantityRejected { get; set; }
    public string? Notes { get; set; }
}
