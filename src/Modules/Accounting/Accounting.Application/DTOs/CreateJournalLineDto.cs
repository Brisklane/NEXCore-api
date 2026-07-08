using System.ComponentModel.DataAnnotations;

namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for creating journal line
/// </summary>
public class CreateJournalLineDto
{
    [Required]
    public Guid LedgerAccountId { get; set; }

    // A line is either a debit or a credit; the other side is 0. Balance is validated at the entry level.
    [Range(0, double.MaxValue)]
    public decimal DebitAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CreditAmount { get; set; }

    [Required]
    [StringLength(3)]
    public required string CurrencyCode { get; set; }

    public string? Description { get; set; }

    [Range(1, int.MaxValue)]
    public int LineNumber { get; set; }
}
