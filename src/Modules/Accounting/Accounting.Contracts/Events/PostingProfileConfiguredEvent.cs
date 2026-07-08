namespace Accounting.Contracts.Events;

/// <summary>
/// Event raised when posting profile is configured
/// </summary>
public class PostingProfileConfiguredEvent : AccountingEvent
{
    public Guid PostingProfileId { get; set; }
    public string? ModuleName { get; set; }
    public string? TransactionType { get; set; }
    public Guid DebitAccountId { get; set; }
    public Guid CreditAccountId { get; set; }
}
