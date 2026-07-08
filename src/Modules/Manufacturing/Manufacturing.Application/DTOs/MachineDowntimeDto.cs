namespace Manufacturing.Application.DTOs;

public class MachineDowntimeDto
{
    public Guid Id { get; set; }
    public Guid WorkCenterId { get; set; }
    public Guid? ProductionOrderId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal? DurationHours { get; set; }
    public required string Category { get; set; }
    public required string Reason { get; set; }
    public string? RootCause { get; set; }
    public string? Resolution { get; set; }
    public Guid ReportedById { get; set; }
    public Guid? ResolvedById { get; set; }
    public required string Status { get; set; }
    public Guid? MaintenanceWorkOrderId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateMachineDowntimeDto
{
    public Guid WorkCenterId { get; set; }
    public Guid? ProductionOrderId { get; set; }
    public DateTime StartTime { get; set; }
    public required string Category { get; set; }
    public required string Reason { get; set; }
    public string? Notes { get; set; }
}

public class UpdateMachineDowntimeDto
{
    public DateTime? EndTime { get; set; }
    public decimal? DurationHours { get; set; }
    public string? RootCause { get; set; }
    public string? Resolution { get; set; }
    public Guid? ResolvedById { get; set; }
    public string? Status { get; set; }
    public Guid? MaintenanceWorkOrderId { get; set; }
    public string? Notes { get; set; }
}
