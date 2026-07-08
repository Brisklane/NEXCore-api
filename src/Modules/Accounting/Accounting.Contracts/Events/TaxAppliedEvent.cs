namespace Accounting.Contracts.Events;

/// <summary>
/// Event raised when tax is applied
/// </summary>
public class TaxAppliedEvent : AccountingEvent
{
    public Guid TaxCodeId { get; set; }
    public string? TaxCode { get; set; }
    public decimal TaxAmount { get; set; }
    public Guid LedgerAccountId { get; set; }
}
