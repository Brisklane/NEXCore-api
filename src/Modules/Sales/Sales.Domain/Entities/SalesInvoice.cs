using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// Sales Invoice / AR Invoice.
/// Aligned with SAP VF01 Billing Document, Oracle AR Invoice, Dynamics Sales Invoice.
/// Triggers AR entry in the Accounting module.
/// </summary>
public class SalesInvoice : BaseEntity
{
    // ??? Identity ?????????????????????????????????????????????????????????????
    /// <summary>Auto-generated invoice number (e.g., INV-2024-00001).</summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    // ??? Links ????????????????????????????????????????????????????????????????
    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;

    /// <summary>Cross-module reference to Crm.Contact. ID only.</summary>
    public Guid? ContactId { get; set; }
    /// <summary>Snapshot of contact name for fast reads.</summary>
    public string? ContactName { get; set; }

    public Guid? DeliveryId { get; set; }
    public Delivery? Delivery { get; set; }

    // ??? Status & Dates ???????????????????????????????????????????????????????
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }

    // ??? Currency ?????????????????????????????????????????????????????????????
    /// <summary>ISO 4217 currency code, e.g., "USD". No cross-module FK.</summary>
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;

    // ??? Financials ???????????????????????????????????????????????????????????
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceDue { get; set; }

    // ??? Billing Address ??????????????????????????????????????????????????????
    public string? BillToName { get; set; }
    public string? BillToStreet { get; set; }
    public string? BillToCity { get; set; }
    public string? BillToState { get; set; }
    public string? BillToPostalCode { get; set; }
    public string? BillToCountry { get; set; }

    public PaymentTerms PaymentTerms { get; set; } = PaymentTerms.Net30;

    // ── Other Info — Invoice section ──────────────────────────────────────────
    /// <summary>Cross-module reference to the salesperson (Auth/HR user). ID only.</summary>
    public Guid? SalesRepId { get; set; }
    /// <summary>Snapshot of salesperson name for fast reads.</summary>
    public string? SalesRepName { get; set; }

    /// <summary>
    /// Payment communication / standard reference printed on the invoice.
    /// Auto-set to InvoiceNumber when the invoice is confirmed (Posted).
    /// Odoo's "Payment Reference" field.
    /// </summary>
    public string? PaymentReference { get; set; }

    /// <summary>
    /// Recipient bank account details (IBAN/account number) for bank transfer invoices.
    /// Odoo's "Recipient Bank" field.
    /// </summary>
    public string? RecipientBankAccount { get; set; }

    /// <summary>
    /// Date the goods or service were delivered — appears in the "Other Info" tab.
    /// Distinct from DueDate.
    /// </summary>
    public DateTime? DeliveryDate { get; set; }

    // ── Other Info — Accounting section ──────────────────────────────────────
    /// <summary>Cross-module reference to a fiscal position rule. ID only.</summary>
    public Guid? FiscalPositionId { get; set; }

    /// <summary>Default payment method for this invoice (e.g. Manual, SEPA, Check).</summary>
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Auto-post setting — controls whether this draft is posted automatically.
    /// Drives the "This move is configured to be posted automatically" banner.
    /// Odoo's account.move.auto_post field.
    /// </summary>
    public InvoiceAutoPost AutoPost { get; set; } = InvoiceAutoPost.No;

    // ── Reminders ─────────────────────────────────────────────────────────────
    /// <summary>Date the last payment reminder was sent to the customer.</summary>
    public DateTime? LastReminderDate { get; set; }
    /// <summary>Number of reminders sent so far.</summary>
    public int ReminderCount { get; set; }

    /// <summary>Reference to AR Journal Entry in Accounting module.</summary>
    public Guid? AccountingJournalEntryId { get; set; }

    /// <summary>
    /// Payment state — updated whenever a payment is registered or reversed.
    /// Drives the "IN PAYMENT" ribbon and payment status column.
    /// Separate from Status (lifecycle) so that Posted can coexist with InPayment.
    /// </summary>
    public InvoicePaymentStatus PaymentStatus { get; set; } = InvoicePaymentStatus.NotPaid;

    public string? Notes { get; set; }
    public string? CustomerReference { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public ICollection<SalesInvoiceLine> Lines { get; set; } = [];
    /// <summary>Payments allocated to this invoice via PaymentAllocation.</summary>
    public ICollection<PaymentAllocation> Allocations { get; set; } = [];
    public ICollection<CreditNote> CreditNotes { get; set; } = [];
}
