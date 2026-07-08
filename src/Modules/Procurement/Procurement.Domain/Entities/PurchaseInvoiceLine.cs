using Nexcore.SharedKernel;

namespace Procurement.Domain.Entities;

/// <summary>
/// Individual line on a vendor bill / AP invoice.
/// </summary>
public class PurchaseInvoiceLine : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public PurchaseInvoice Invoice { get; set; } = null!;

    public int LineNumber { get; set; }

    // ─── Source References ─────────────────────────────────────────────────────
    public Guid? PurchaseOrderLineId { get; set; }
    public PurchaseOrderLine? PurchaseOrderLine { get; set; }

    public Guid? GoodsReceiptLineId { get; set; }
    public GoodsReceiptLine? GoodsReceiptLine { get; set; }

    // ─── Item ──────────────────────────────────────────────────────────────────
    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public required string ItemDescription { get; set; }

    // ─── Quantity & Pricing ────────────────────────────────────────────────────
    public decimal Quantity { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public string? UnitOfMeasureName { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }

    public Guid? TaxCodeId { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    /// <summary>Recoverable input VAT on this line (e.g. partially exempt businesses).</summary>
    public decimal RecoverableTaxAmount { get; set; }
    /// <summary>Non-recoverable input VAT — expensed to the cost account on this line.</summary>
    public decimal NonRecoverableTaxAmount { get; set; }

    public decimal SubTotal { get; set; }
    public decimal TotalPrice { get; set; }

    // ─── Classification & Accounting ──────────────────────────────────────────
    public Guid? ProcurementCategoryId { get; set; }
    public ProcurementCategory? ProcurementCategory { get; set; }

    /// <summary>GL account override for this line. Cross-module ref to Accounting. ID only.</summary>
    public Guid? LedgerAccountId { get; set; }

    /// <summary>
    /// Line-level analytical dimension (cost centre, project, department).
    /// Overrides the invoice header DimensionSetId when set.
    /// Cross-module ref to Accounting DimensionSet. ID only.
    /// </summary>
    public Guid? DimensionSetId { get; set; }

    public string? Notes { get; set; }
}
