namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for fiscal period
/// </summary>
public class FiscalPeriodDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid FiscalCalendarId { get; set; }
    public required string PeriodName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsClosed { get; set; }
    public string? Description { get; set; }
}
