namespace Hr.Application.DTOs;

public class ShiftDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string ShiftCode { get; set; } = string.Empty;
    public string ShiftName { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? TimeZone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateShiftDto
{
    public string ShiftCode { get; set; } = string.Empty;
    public string ShiftName { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? TimeZone { get; set; }
}

public class UpdateShiftDto
{
    public string? ShiftName { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string? TimeZone { get; set; }
    public bool? IsActive { get; set; }
}
