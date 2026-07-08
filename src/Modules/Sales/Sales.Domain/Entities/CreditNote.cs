using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// Credit Note issued to reverse or partially credit a Sales Invoice.
/// Aligned with SAP Credit Memo, Oracle Credit Memo, Dynamics Credit Note.
/// </summary>
public class CreditNote : BaseEntity
{
    public string CreditNoteNumber { get; set; } = string.Empty;

    public Guid SalesInvoiceId { get; set; }
    public SalesInvoice SalesInvoice { get; set; } = null!;

    /// <summary>Cross-module reference to Crm.Contact. ID only.</summary>
    public Guid? ContactId { get; set; }
    /// <summary>Snapshot of contact name for fast reads.</summary>
    public string? ContactName { get; set; }

    public DateTime CreditNoteDate { get; set; } = DateTime.UtcNow;

    public decimal SubtotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    /// <summary>ISO 4217 currency code, e.g., "USD". No cross-module FK.</summary>
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>Reason for issuing the credit note.</summary>
    public string? Reason { get; set; }

    public string? Notes { get; set; }

    /// <summary>Accounting journal entry reference.</summary>
    public Guid? AccountingJournalEntryId { get; set; }

    public ICollection<CreditNoteLine> Lines { get; set; } = new List<CreditNoteLine>();
}
