namespace Manufacturing.Application.DTOs;

public class CapacityLoadDto
{
    public Guid Id { get; set; }
    public Guid WorkCenterId { get; set; }
    public Guid? WorkCenterShiftId { get; set; }
    public DateTime Date { get; set; }
    public decimal RequiredHours { get; set; }
    public decimal AvailableHours { get; set; }
    public decimal LoadPercentage { get; set; }
    public int ProductionOrderCount { get; set; }
    public bool IsOverloaded { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCapacityLoadDto
{
    public Guid WorkCenterId { get; set; }
    public Guid? WorkCenterShiftId { get; set; }
    public DateTime Date { get; set; }
    public decimal RequiredHours { get; set; }
    public decimal AvailableHours { get; set; }
    public int ProductionOrderCount { get; set; } = 0;
    public string? Notes { get; set; }
}

public class UpdateCapacityLoadDto
{
    public decimal? RequiredHours { get; set; }
    public decimal? AvailableHours { get; set; }
    public int? ProductionOrderCount { get; set; }
    public string? Notes { get; set; }
}
