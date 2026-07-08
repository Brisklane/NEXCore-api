using System.ComponentModel.DataAnnotations;

namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for creating ledger
/// </summary>
public class CreateLedgerDto
{
    [Required]
    [StringLength(255)]
    public required string Name { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public required string BaseCurrencyCode { get; set; }

    [Required]
    public Guid FiscalCalendarId { get; set; }

    public bool IsDefault { get; set; }

    public string? Description { get; set; }
}
