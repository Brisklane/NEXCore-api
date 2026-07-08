namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for fiscal calendar
/// </summary>
public class FiscalCalendarDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }

    /// <summary>Whether the fiscal year is closed (read-only).</summary>
    public bool IsClosed { get; set; }

    /// <summary>True when journal entries exist within this year's date range — blocks edit/delete.</summary>
    public bool HasTransactions { get; set; }
}
