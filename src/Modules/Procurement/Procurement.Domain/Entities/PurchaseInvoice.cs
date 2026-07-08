using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Enums;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Vendor Bill / AP Invoice — records vendor's invoice against a PO.
/// Aligned with SAP MIRO (Invoice Verification), Oracle AP Invoice,
/// Dynamics Purchase Invoice, Odoo account.move (vendor bill).
/// Triggers AP journal entry in the Accounting module.
/// </summary>
public class PurchaseInvoice : BaseEntity
{
    // ─── Identity ──────────────────────────────────────────────────────────────
    /// <summary>Our internal AP invoice number (e.g., BILL-2024-00001).</summary>
    public string InvoiceNumber { get; set; } = string.Empty;
    /// <summary>Vendor's own invoice number — mandatory for duplicate detection.</summary>
    public required string VendorInvoiceNumber { get; set; }

    // ─── Links ─────────────────────────────────────────────────────────────────
    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;
    public string? VendorName { get; set; }

    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    // ─── Dates ─────────────────────────────────────────────────────────────────
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime PostingDate { get; set; } = DateTime.UtcNow;

    // ─── Status ────────────────────────────────────────────────────────────────
    public PurchaseInvoiceStatus Status { get; set; } = PurchaseInvoiceStatus.Draft;
    public InvoiceMatchingStatus MatchingStatus { get; set; } = InvoiceMatchingStatus.NotMatched;
    public InvoicePaymentStatus PaymentStatus { get; set; } = InvoicePaymentStatus.NotPaid;

    // ─── Currency ──────────────────────────────────────────────────────────────
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;

    // ─── Financials ────────────────────────────────────────────────────────────
    public decimal SubTotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }

    /// <summary>
    /// Portion of TaxAmount that can be reclaimed as input VAT.
    /// Relevant for partially exempt businesses and mixed-use purchases.
    /// </summary>
    public decimal RecoverableTaxAmount { get; set; }
    /// <summary>TaxAmount - RecoverableTaxAmount; expensed to the cost account.</summary>
    public decimal NonRecoverableTaxAmount { get; set; }

    // ─── Payment Terms ─────────────────────────────────────────────────────────
    public PaymentTerms PaymentTerms { get; set; } = PaymentTerms.Net30;

    // ─── Accounting Integration ────────────────────────────────────────────────
    /// <summary>Cross-module reference to AP Journal Entry in Accounting. ID only.</summary>
    public Guid? AccountingJournalEntryId { get; set; }

    /// <summary>
    /// Cross-module reference to Accounting FiscalPeriod this invoice is posted into.
    /// Prevents posting to a closed period. ID only.
    /// </summary>
    public Guid? FiscalPeriodId { get; set; }

    /// <summary>
    /// Cross-module reference to an Accounting DimensionSet for analytical accounting
    /// (cost centre, project, department) at invoice header level. ID only.
    /// </summary>
    public Guid? DimensionSetId { get; set; }

    // ─── Approval ──────────────────────────────────────────────────────────────
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? PostedByUserId { get; set; }
    public DateTime? PostedAt { get; set; }

    public string? HoldReason { get; set; }
    public string? DisputeReason { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }

    // ─── Navigation ────────────────────────────────────────────────────────────
    public ICollection<PurchaseInvoiceLine> Lines { get; set; } = [];
    public ICollection<ThreeWayMatchRecord> MatchRecords { get; set; } = [];
    public ICollection<VendorPaymentLine> PaymentAllocations { get; set; } = [];
}
