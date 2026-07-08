using Nexcore.SharedKernel;

namespace Procurement.Domain.Entities;

/// <summary>
/// Individual line on a Vendor Debit Note — mirrors the return line it credits.
/// </summary>
public class VendorDebitNoteLine : BaseEntity
{
    public Guid DebitNoteId { get; set; }
    public VendorDebitNote DebitNote { get; set; } = null!;

    public int LineNumber { get; set; }

    public Guid ReturnLineId { get; set; }
    public PurchaseReturnLine ReturnLine { get; set; } = null!;

    // ─── Item ──────────────────────────────────────────────────────────────────
    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public required string ItemDescription { get; set; }

    // ─── Amounts ───────────────────────────────────────────────────────────────
    public decimal Quantity { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TotalAmount { get; set; }

    /// <summary>GL account for this credit line. Cross-module ref to Accounting. ID only.</summary>
    public Guid? LedgerAccountId { get; set; }

    public string? Notes { get; set; }
}
