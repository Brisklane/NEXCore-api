using Nexcore.SharedKernel;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Vendor Debit Note — formal AP credit claim issued to vendor after a purchase return.
/// Reduces the outstanding AP balance; when settled it offsets against a future invoice
/// payment or is received as a refund.
///
/// Aligned with:
///   SAP  — Credit Memo posted against vendor (MIRO / FB65)
///   Oracle — Debit Memo (AP)
///   Dynamics — Purchase Credit Note
///   Odoo — Vendor Credit Note (account.move, move_type=in_refund)
/// </summary>
public class VendorDebitNote : BaseEntity
{
    // ─── Identity ──────────────────────────────────────────────────────────────
    /// <summary>Auto-generated debit note number (e.g., DN-2024-00001).</summary>
    public string DebitNoteNumber { get; set; } = string.Empty;

    // ─── Links ─────────────────────────────────────────────────────────────────
    public Guid PurchaseReturnId { get; set; }
    public PurchaseReturn PurchaseReturn { get; set; } = null!;

    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;
    public string? VendorName { get; set; }

    /// <summary>The original vendor invoice being credited. Null if return is before invoice.</summary>
    public Guid? OriginalInvoiceId { get; set; }
    public PurchaseInvoice? OriginalInvoice { get; set; }

    // ─── Dates ─────────────────────────────────────────────────────────────────
    public DateTime DebitNoteDate { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? SettledAt { get; set; }

    // ─── Status ────────────────────────────────────────────────────────────────
    public DebitNoteStatus Status { get; set; } = DebitNoteStatus.Draft;

    // ─── Currency & Amounts ────────────────────────────────────────────────────
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;
    public decimal SubTotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal SettledAmount { get; set; }
    public decimal OutstandingAmount { get; set; }

    // ─── Accounting Integration ────────────────────────────────────────────────
    /// <summary>Cross-module reference to AP reversal Journal Entry. ID only.</summary>
    public Guid? AccountingJournalEntryId { get; set; }

    public Guid? PostedByUserId { get; set; }
    public DateTime? PostedAt { get; set; }

    public string? Notes { get; set; }

    // ─── Navigation ────────────────────────────────────────────────────────────
    public ICollection<VendorDebitNoteLine> Lines { get; set; } = [];
}
