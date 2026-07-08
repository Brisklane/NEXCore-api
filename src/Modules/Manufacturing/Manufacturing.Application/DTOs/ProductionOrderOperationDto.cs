namespace Manufacturing.Application.DTOs;

public class ProductionOrderOperationDto
{
    public Guid Id { get; set; }
    public Guid ProductionOrderId { get; set; }
    public Guid RoutingOperationId { get; set; }
    public Guid WorkCenterId { get; set; }
    public int SequenceNo { get; set; }
    public required string OperationName { get; set; }
    public decimal PlannedSetupHours { get; set; }
    public decimal PlannedLaborHours { get; set; }
    public decimal PlannedMachineHours { get; set; }
    public decimal ActualSetupHours { get; set; }
    public decimal ActualLaborHours { get; set; }
    public decimal ActualMachineHours { get; set; }
    public decimal ConfirmedQty { get; set; }
    public decimal ScrapQty { get; set; }
    public required string Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? ConfirmedById { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateProductionOrderOperationDto
{
    public Guid ProductionOrderId { get; set; }
    public Guid RoutingOperationId { get; set; }
    public Guid WorkCenterId { get; set; }
    public int SequenceNo { get; set; }
    public required string OperationName { get; set; }
    public decimal PlannedSetupHours { get; set; }
    public decimal PlannedLaborHours { get; set; }
    public decimal PlannedMachineHours { get; set; }
    public string? Notes { get; set; }
}

public class UpdateProductionOrderOperationDto
{
    public decimal? ActualSetupHours { get; set; }
    public decimal? ActualLaborHours { get; set; }
    public decimal? ActualMachineHours { get; set; }
    public decimal? ConfirmedQty { get; set; }
    public decimal? ScrapQty { get; set; }
    public string? Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? ConfirmedById { get; set; }
    public string? Notes { get; set; }
}
