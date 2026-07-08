namespace Hr.Application.DTOs;

public class InterviewerAvailabilityDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
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
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateInterviewerAvailabilityDto
{
    public Guid InterviewerEmployeeId { get; set; }
    public DateTime AvailableDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public Guid SlotStatusLookupValueId { get; set; }
    public string? TimeZone { get; set; }
    public string? Notes { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrencePattern { get; set; }
}

public class UpdateInterviewerAvailabilityDto
{
    public DateTime? AvailableDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public Guid? SlotStatusLookupValueId { get; set; }
    public Guid? InterviewId { get; set; }
    public string? TimeZone { get; set; }
    public string? Notes { get; set; }
    public bool? IsBlocked { get; set; }
    public string? ReasonCode { get; set; }
}
