using System.ComponentModel.DataAnnotations;

namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for updating journal entry
/// </summary>
public class UpdateJournalEntryDto
{
    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? ReferenceNumber { get; set; }

    public DateTime? PostingDate { get; set; }

    /// <summary>
    /// Replacement set of journal lines (Draft only). When provided, the existing lines are
    /// replaced wholesale and totals are recomputed. Null = leave lines unchanged.
    /// </summary>
    public List<CreateJournalLineDto>? Lines { get; set; }
}
