namespace Accounting.Contracts.Events;

/// <summary>
/// Event raised when ledger is created
/// </summary>
public class LedgerCreatedEvent : AccountingEvent
{
    public Guid LedgerId { get; set; }
    public string? LedgerName { get; set; }
    public string? BaseCurrencyCode { get; set; }
    public DateTime CreatedAt { get; set; }
}
