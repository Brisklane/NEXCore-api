namespace Manufacturing.Application.DTOs;

public class ProductionScheduleDto
{
    public Guid Id { get; set; }
    public Guid ProductionOrderId { get; set; }
    public Guid WorkCenterId { get; set; }
    public Guid? ProductionOrderOperationId { get; set; }
    public DateTime ScheduledStartDate { get; set; }
    public DateTime ScheduledEndDate { get; set; }
    public required string ScheduleType { get; set; }
    public decimal CapacityRequiredHours { get; set; }
    public required string Status { get; set; }
    public bool HasCapacityConflict { get; set; }
    public string? ConflictDescription { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateProductionScheduleDto
{
    public Guid ProductionOrderId { get; set; }
    public Guid WorkCenterId { get; set; }
    public Guid? ProductionOrderOperationId { get; set; }
    public DateTime ScheduledStartDate { get; set; }
    public DateTime ScheduledEndDate { get; set; }
    public required string ScheduleType { get; set; }
    public decimal CapacityRequiredHours { get; set; }
    public string? Notes { get; set; }
}

public class UpdateProductionScheduleDto
{
    public DateTime? ScheduledStartDate { get; set; }
    public DateTime? ScheduledEndDate { get; set; }
    public string? ScheduleType { get; set; }
    public decimal? CapacityRequiredHours { get; set; }
    public string? Status { get; set; }
    public bool? HasCapacityConflict { get; set; }
    public string? ConflictDescription { get; set; }
    public string? Notes { get; set; }
}
