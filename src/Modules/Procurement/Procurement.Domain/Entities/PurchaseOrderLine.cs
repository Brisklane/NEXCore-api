using Nexcore.SharedKernel;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Individual line item on a Purchase Order.
/// Tracks ordered, received, and invoiced quantities independently for 3-way match.
/// </summary>
public class PurchaseOrderLine : BaseEntity
{
    public Guid OrderId { get; set; }
    public PurchaseOrder Order { get; set; } = null!;

    public int LineNumber { get; set; }

    // ─── Source ────────────────────────────────────────────────────────────────
    public Guid? RequisitionLineId { get; set; }
    public PurchaseRequisitionLine? RequisitionLine { get; set; }

    public Guid? ContractLineId { get; set; }
    public PurchaseContractLine? ContractLine { get; set; }

    // ─── Item ──────────────────────────────────────────────────────────────────
    /// <summary>Cross-module reference to Inventory item. Null for service/expense lines.</summary>
    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public required string ItemDescription { get; set; }

    // ─── Quantity & UoM ────────────────────────────────────────────────────────
    public decimal Quantity { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public string? UnitOfMeasureName { get; set; }

    // ─── Pricing ───────────────────────────────────────────────────────────────
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }

    /// <summary>Cross-module reference to Accounting TaxCode. ID only.</summary>
    public Guid? TaxCodeId { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }

    public decimal SubTotal { get; set; }
    public decimal TotalPrice { get; set; }

    // ─── Classification ────────────────────────────────────────────────────────
    public Guid? ProcurementCategoryId { get; set; }
    public ProcurementCategory? ProcurementCategory { get; set; }

    /// <summary>Cross-module GL account override for this line. ID only.</summary>
    public Guid? LedgerAccountId { get; set; }

    /// <summary>Cross-module cost center for budget tracking. ID only.</summary>
    public Guid? CostCenterId { get; set; }

    /// <summary>
    /// Line-level analytical dimension for commitment accounting and cost reporting.
    /// Cross-module ref to Accounting DimensionSet. ID only.
    /// </summary>
    public Guid? DimensionSetId { get; set; }

    // ─── Delivery ──────────────────────────────────────────────────────────────
    public DateTime? ExpectedDeliveryDate { get; set; }
    public Guid? DeliveryLocationId { get; set; }

    // ─── Fulfillment Tracking (updated on GR and invoice posting) ──────────────
    public decimal QuantityReceived { get; set; }
    public decimal QuantityAccepted { get; set; }
    public decimal QuantityRejected { get; set; }
    public decimal QuantityReturned { get; set; }
    public decimal QuantityInvoiced { get; set; }
    public decimal QuantityRemaining => Quantity - QuantityReceived;
    public decimal QuantityToInvoice => QuantityAccepted - QuantityInvoiced;

    public PurchaseOrderLineStatus LineStatus { get; set; } = PurchaseOrderLineStatus.Open;

    public string? Notes { get; set; }

    // ─── Navigation ────────────────────────────────────────────────────────────
    public ICollection<GoodsReceiptLine> ReceiptLines { get; set; } = [];
    public ICollection<PurchaseInvoiceLine> InvoiceLines { get; set; } = [];
}
