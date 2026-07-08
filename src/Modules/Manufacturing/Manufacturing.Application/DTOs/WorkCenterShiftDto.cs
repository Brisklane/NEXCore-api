namespace Manufacturing.Application.DTOs;

public class WorkCenterShiftDto
{
    public Guid Id { get; set; }
    public Guid WorkCenterId { get; set; }
    public required string ShiftName { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal AvailableHours { get; set; }
    public decimal CapacityUtilizationPercent { get; set; }
    public required string WorkingDays { get; set; }
    public bool IsActive { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateWorkCenterShiftDto
{
    public Guid WorkCenterId { get; set; }
    public required string ShiftName { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal AvailableHours { get; set; }
    public decimal CapacityUtilizationPercent { get; set; } = 100;
    public required string WorkingDays { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Notes { get; set; }
}

public class UpdateWorkCenterShiftDto
{
    public string? ShiftName { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public decimal? AvailableHours { get; set; }
    public decimal? CapacityUtilizationPercent { get; set; }
    public string? WorkingDays { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Notes { get; set; }
}
