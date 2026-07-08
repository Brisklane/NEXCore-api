namespace Accounting.Contracts.Events;

/// <summary>
/// Base event for accounting domain events
/// </summary>
public abstract class AccountingEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Guid CompanyId { get; set; }
}
