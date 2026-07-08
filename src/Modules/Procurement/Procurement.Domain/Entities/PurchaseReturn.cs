using Nexcore.SharedKernel;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Return-to-Vendor (RTV) document — records goods being sent back to the vendor
/// after receipt, triggering a reversal of the inventory movement and a Vendor Debit Note.
///
/// Aligned with:
///   SAP  — Return Purchase Order (MIGO / movement type 122) or Return Delivery
///   Oracle — Return to Receiving (RTR) / Return to Vendor (RTV)
///   Dynamics — Return Order
///   Odoo — Reverse Transfer (stock.picking) + vendor credit note
///
/// Flow: GRN Posted → Quality Fail / Wrong Item → Create PurchaseReturn →
///       Approve → Post (reverses inventory) → System creates VendorDebitNote.
/// </summary>
public class PurchaseReturn : BaseEntity
{
    // ─── Identity ──────────────────────────────────────────────────────────────
    /// <summary>Auto-generated return number (e.g., RTN-2024-00001).</summary>
    public string ReturnNumber { get; set; } = string.Empty;

    // ─── Source Documents ──────────────────────────────────────────────────────
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public Guid GoodsReceiptId { get; set; }
    public GoodsReceipt GoodsReceipt { get; set; } = null!;

    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    // ─── Dates ─────────────────────────────────────────────────────────────────
    public DateTime ReturnDate { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PostedAt { get; set; }

    // ─── Status ────────────────────────────────────────────────────────────────
    public PurchaseReturnStatus Status { get; set; } = PurchaseReturnStatus.Draft;
    public PurchaseReturnReason ReturnReason { get; set; }

    // ─── Financials ────────────────────────────────────────────────────────────
    public string CurrencyCode { get; set; } = "USD";
    public decimal TotalReturnAmount { get; set; }

    // ─── Accounting Integration ────────────────────────────────────────────────
    /// <summary>
    /// Cross-module reference to the GR/IR reversal Journal Entry created on posting.
    /// Entry: Dr GR-IR Clearing Account, Cr Inventory/Stock Account.
    /// ID only.
    /// </summary>
    public Guid? AccountingJournalEntryId { get; set; }

    /// <summary>Cross-module reference to Accounting FiscalPeriod. ID only.</summary>
    public Guid? FiscalPeriodId { get; set; }

    // ─── Approval ──────────────────────────────────────────────────────────────
    public Guid? ApprovedByUserId { get; set; }
    public Guid? PostedByUserId { get; set; }

    /// <summary>Vendor's RMA / Return Authorisation number if required by vendor.</summary>
    public string? VendorReturnAuthorisationNumber { get; set; }

    public string? Notes { get; set; }

    // ─── Navigation ────────────────────────────────────────────────────────────
    public ICollection<PurchaseReturnLine> Lines { get; set; } = [];
    public VendorDebitNote? DebitNote { get; set; }
}
