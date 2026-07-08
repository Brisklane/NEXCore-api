namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for updating fiscal period
/// </summary>
public class UpdateFiscalPeriodDto
{
    public string? PeriodName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? IsClosed { get; set; }
    public string? Description { get; set; }
}
