using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class Shift : BaseEntity
{
    public string ShiftCode { get; set; } = string.Empty;
    public string ShiftName { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? TimeZone { get; set; }
    public ICollection<Employee>? Employees { get; set; }
    public ICollection<JobTemplate>? JobTemplates { get; set; }
    public ICollection<Position>? Positions { get; set; }
}
