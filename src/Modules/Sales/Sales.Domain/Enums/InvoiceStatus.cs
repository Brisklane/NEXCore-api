namespace Sales.Domain.Enums;

/// <summary>
/// Sales invoice lifecycle.
/// </summary>
public enum InvoiceStatus
{
    Draft = 0,
    Issued = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Overdue = 4,
    Cancelled = 5,
    CreditNote = 6
}
