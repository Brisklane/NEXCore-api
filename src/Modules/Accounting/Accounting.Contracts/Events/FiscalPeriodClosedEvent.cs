namespace Accounting.Contracts.Events;

/// <summary>
/// Event raised when fiscal period is closed
/// </summary>
public class FiscalPeriodClosedEvent : AccountingEvent
{
    public Guid FiscalPeriodId { get; set; }
    public string? PeriodName { get; set; }
    public DateTime ClosedAt { get; set; }
    public Guid ClosedByUserId { get; set; }
}
