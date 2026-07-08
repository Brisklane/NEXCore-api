
using Nexcore.SharedKernel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hr.Domain.Entities;

public class InterviewerAvailability : BaseEntity
{
    public Guid InterviewerEmployeeId { get; set; }
    public DateTime AvailableDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public Guid SlotStatusLookupValueId { get; set; }
    public Guid? InterviewId { get; set; }
    public string? TimeZone { get; set; }
    public string? Notes { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrencePattern { get; set; }
    public bool IsBlocked { get; set; }
    public string? ReasonCode { get; set; }

    public Employee? InterviewerEmployee { get; set; }
    public Interview? Interview { get; set; }
    public LookupValue? SlotStatusLookupValue { get; set; }
}
