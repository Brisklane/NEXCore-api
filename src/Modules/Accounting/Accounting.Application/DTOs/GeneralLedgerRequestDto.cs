using System.ComponentModel.DataAnnotations;

namespace Accounting.Application.DTOs;

/// <summary>
/// Request DTO for general ledger report
/// </summary>
public class GeneralLedgerRequestDto
{
    [Required]
    public Guid AccountId { get; set; }

    [Required]
    public DateTime FromDate { get; set; }

    [Required]
    public DateTime ToDate { get; set; }
}
