namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for creating fiscal period
/// </summary>
public class CreateFiscalPeriodDto
{
    public Guid FiscalCalendarId { get; set; }
    public required string PeriodName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Description { get; set; }
}
