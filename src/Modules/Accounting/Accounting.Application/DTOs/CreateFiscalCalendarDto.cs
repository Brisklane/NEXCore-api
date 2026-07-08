using System.ComponentModel.DataAnnotations;

namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for creating a fiscal calendar (fiscal year)
/// </summary>
public class CreateFiscalCalendarDto
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}
