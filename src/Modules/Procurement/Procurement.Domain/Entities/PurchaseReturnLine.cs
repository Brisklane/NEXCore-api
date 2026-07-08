using Nexcore.SharedKernel;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Individual item line on a Purchase Return.
/// References the original GRN line so inventory reversal is traceable.
/// </summary>
public class PurchaseReturnLine : BaseEntity
{
    public Guid PurchaseReturnId { get; set; }
    public PurchaseReturn PurchaseReturn { get; set; } = null!;

    public int LineNumber { get; set; }

    // ─── Source References ─────────────────────────────────────────────────────
    public Guid GoodsReceiptLineId { get; set; }
    public GoodsReceiptLine GoodsReceiptLine { get; set; } = null!;

    public Guid PurchaseOrderLineId { get; set; }
    public PurchaseOrderLine PurchaseOrderLine { get; set; } = null!;

    // ─── Item ──────────────────────────────────────────────────────────────────
    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public required string ItemDescription { get; set; }

    // ─── Quantity & Pricing ────────────────────────────────────────────────────
    public decimal QuantityReceived { get; set; }
    public decimal QuantityReturned { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public string? UnitOfMeasureName { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalReturnAmount { get; set; }

    // ─── Return Details ────────────────────────────────────────────────────────
    public PurchaseReturnReason ReturnReason { get; set; }
    public string? QualityIssueDescription { get; set; }
    public string? LotNumber { get; set; }

    /// <summary>Cross-module reference to return storage location in Inventory. ID only.</summary>
    public Guid? ReturnStorageLocationId { get; set; }

    public string? Notes { get; set; }
}
