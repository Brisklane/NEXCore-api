namespace Accounting.Contracts.Events;

/// <summary>
/// Event raised when journal entry is reversed
/// </summary>
public class JournalEntryReversedEvent : AccountingEvent
{
    public Guid OriginalJournalId { get; set; }
    public Guid ReversalJournalId { get; set; }
    public string? Reason { get; set; }
    public DateTime PostingDate { get; set; }
}
