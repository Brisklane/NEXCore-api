using Nexcore.SharedKernel;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Goods Receipt Note (GRN) — records physical arrival of goods against a PO.
/// Aligned with SAP MIGO (Goods Receipt), Oracle Receipt, Dynamics Product Receipt,
/// Odoo stock.picking (incoming). The GR is the trigger for 3-way match.
/// </summary>
public class GoodsReceipt : BaseEntity
{
    // ─── Identity ──────────────────────────────────────────────────────────────
    /// <summary>Auto-generated receipt number (e.g., GRN-2024-00001).</summary>
    public string ReceiptNumber { get; set; } = string.Empty;

    /// <summary>Vendor's delivery note / packing slip number.</summary>
    public string? VendorDeliveryNoteNumber { get; set; }

    // ─── Links ─────────────────────────────────────────────────────────────────
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    // ─── Dates ─────────────────────────────────────────────────────────────────
    public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;
    public DateTime? PostedAt { get; set; }

    // ─── Status ────────────────────────────────────────────────────────────────
    public GoodsReceiptStatus Status { get; set; } = GoodsReceiptStatus.Draft;
    public ReceiptType ReceiptType { get; set; } = ReceiptType.Standard;

    // ─── Warehouse ─────────────────────────────────────────────────────────────
    /// <summary>Cross-module reference to Inventory Warehouse. ID only.</summary>
    public Guid? WarehouseId { get; set; }
    /// <summary>Cross-module reference to Inventory Location. ID only.</summary>
    public Guid? StorageLocationId { get; set; }

    public Guid? PostedByUserId { get; set; }

    /// <summary>If this is a return receipt, references the original receipt.</summary>
    public Guid? OriginalReceiptId { get; set; }
    public GoodsReceipt? OriginalReceipt { get; set; }

    // ─── Accounting Integration ────────────────────────────────────────────────
    /// <summary>
    /// Cross-module reference to the GR/IR clearing Journal Entry created on posting.
    /// Entry: Dr Inventory/Stock Account, Cr GR-IR Clearing Account.
    /// ID only — never navigate across module boundaries.
    /// </summary>
    public Guid? AccountingJournalEntryId { get; set; }

    /// <summary>
    /// Cross-module reference to the Accounting FiscalPeriod this receipt is posted into.
    /// Prevents posting to a closed period. ID only.
    /// </summary>
    public Guid? FiscalPeriodId { get; set; }

    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }

    // ─── Navigation ────────────────────────────────────────────────────────────
    public ICollection<GoodsReceiptLine> Lines { get; set; } = [];
}
