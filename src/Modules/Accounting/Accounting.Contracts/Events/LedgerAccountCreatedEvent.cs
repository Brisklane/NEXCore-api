namespace Accounting.Contracts.Events;

/// <summary>
/// Event raised when account is created
/// </summary>
public class LedgerAccountCreatedEvent : AccountingEvent
{
    public Guid AccountId { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountName { get; set; }
    public Guid LedgerId { get; set; }
}
