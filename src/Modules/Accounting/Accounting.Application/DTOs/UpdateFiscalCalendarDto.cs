using System.ComponentModel.DataAnnotations;

namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for updating a fiscal calendar (fiscal year).
/// Only permitted while the year is open and has no transactions.
/// </summary>
public class UpdateFiscalCalendarDto
{
    [StringLength(100)]
    public string? Name { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}
