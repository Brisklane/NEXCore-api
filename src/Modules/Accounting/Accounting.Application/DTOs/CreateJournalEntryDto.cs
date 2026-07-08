using System.ComponentModel.DataAnnotations;

namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for creating journal entry
/// </summary>
public class CreateJournalEntryDto
{
    [Required]
    public Guid LedgerId { get; set; }

    [Required]
    [StringLength(50)]
    public required string DocumentType { get; set; }

    [Required]
    public DateTime PostingDate { get; set; }

    [Required]
    public DateTime DocumentDate { get; set; }

    [Required]
    [StringLength(500)]
    public required string Description { get; set; }

    [Required]
    [StringLength(3)]
    public required string CurrencyCode { get; set; }

    [StringLength(50)]
    public string? ReferenceNumber { get; set; }

    public IEnumerable<CreateJournalLineDto>? Lines { get; set; }
}
