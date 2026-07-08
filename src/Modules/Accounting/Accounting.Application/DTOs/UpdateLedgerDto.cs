using System.ComponentModel.DataAnnotations;

namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for updating ledger
/// </summary>
public class UpdateLedgerDto
{
    [StringLength(255)]
    public string? Name { get; set; }

    [StringLength(3, MinimumLength = 3)]
    public string? BaseCurrencyCode { get; set; }

    public Guid? FiscalCalendarId { get; set; }

    public bool? IsDefault { get; set; }

    public bool? IsActive { get; set; }

    public string? Description { get; set; }
}
