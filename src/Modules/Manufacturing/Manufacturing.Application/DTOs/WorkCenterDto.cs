namespace Manufacturing.Application.DTOs;

public class WorkCenterDto
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public decimal CapacityPerHour { get; set; }
    public decimal? HourlyMachineCost { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateWorkCenterDto
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public decimal CapacityPerHour { get; set; }
    public decimal? HourlyMachineCost { get; set; }
    public string? Description { get; set; }
}

public class UpdateWorkCenterDto
{
    public string? Name { get; set; }
    public decimal? CapacityPerHour { get; set; }
    public decimal? HourlyMachineCost { get; set; }
    public bool? IsActive { get; set; }
    public string? Description { get; set; }
}
