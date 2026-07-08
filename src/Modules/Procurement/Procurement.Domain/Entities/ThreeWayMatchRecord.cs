using Nexcore.SharedKernel;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Three-way match record linking a Purchase Invoice line to its PO line and GR line.
/// Core AP control: prevents payment unless PO, receipt, and invoice quantities/prices agree.
/// Aligned with SAP MIRO tolerance checks, Oracle 3-way match, Dynamics matching policy.
/// </summary>
public class ThreeWayMatchRecord : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public PurchaseInvoice Invoice { get; set; } = null!;

    public Guid InvoiceLineId { get; set; }
    public PurchaseInvoiceLine InvoiceLine { get; set; } = null!;

    public Guid? PurchaseOrderLineId { get; set; }
    public PurchaseOrderLine? PurchaseOrderLine { get; set; }

    public Guid? GoodsReceiptLineId { get; set; }
    public GoodsReceiptLine? GoodsReceiptLine { get; set; }

    // ─── Matching Result ───────────────────────────────────────────────────────
    public InvoiceMatchingStatus MatchStatus { get; set; } = InvoiceMatchingStatus.NotMatched;

    public bool HasDiscrepancy { get; set; }
    public DiscrepancyType? DiscrepancyType { get; set; }

    // ─── Quantities ────────────────────────────────────────────────────────────
    public decimal POQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal InvoicedQuantity { get; set; }
    public decimal QuantityVariance { get; set; }

    // ─── Amounts ───────────────────────────────────────────────────────────────
    public decimal POUnitPrice { get; set; }
    public decimal InvoiceUnitPrice { get; set; }
    public decimal PriceVariance { get; set; }
    public decimal TotalVarianceAmount { get; set; }

    // ─── Resolution ────────────────────────────────────────────────────────────
    public string? Resolution { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public DateTime MatchedAt { get; set; } = DateTime.UtcNow;
    public Guid? MatchedByUserId { get; set; }
    public string? Notes { get; set; }
}
