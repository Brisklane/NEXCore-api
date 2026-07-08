namespace Accounting.Contracts.Events;

/// <summary>
/// Event raised when account balance is calculated
/// </summary>
public class AccountBalanceCalculatedEvent : AccountingEvent
{
    public Guid AccountId { get; set; }
    public Guid PeriodId { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal DebitTotal { get; set; }
    public decimal CreditTotal { get; set; }
    public decimal ClosingBalance { get; set; }
}
