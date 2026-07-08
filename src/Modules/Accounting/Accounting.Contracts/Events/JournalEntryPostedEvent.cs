namespace Accounting.Contracts.Events;

/// <summary>
/// Event raised when journal entry is posted
/// </summary>
public class JournalEntryPostedEvent : AccountingEvent
{
    public Guid JournalEntryId { get; set; }
    public string? JournalNumber { get; set; }
    public Guid LedgerId { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public DateTime PostingDate { get; set; }
}
