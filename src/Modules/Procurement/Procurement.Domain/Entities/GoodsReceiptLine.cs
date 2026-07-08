using Nexcore.SharedKernel;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Individual item received on a Goods Receipt Note.
/// Captures actual vs expected quantities and quality inspection result.
/// </summary>
public class GoodsReceiptLine : BaseEntity
{
    public Guid ReceiptId { get; set; }
    public GoodsReceipt Receipt { get; set; } = null!;

    public Guid PurchaseOrderLineId { get; set; }
    public PurchaseOrderLine PurchaseOrderLine { get; set; } = null!;

    public int LineNumber { get; set; }

    // ─── Item ──────────────────────────────────────────────────────────────────
    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public required string ItemDescription { get; set; }

    // ─── Quantities ────────────────────────────────────────────────────────────
    public decimal QuantityOrdered { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityAccepted { get; set; }
    public decimal QuantityRejected { get; set; }

    public Guid? UnitOfMeasureId { get; set; }
    public string? UnitOfMeasureName { get; set; }

    // ─── Traceability ──────────────────────────────────────────────────────────
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? ManufacturerBatchNumber { get; set; }

    // ─── Storage ───────────────────────────────────────────────────────────────
    /// <summary>Cross-module reference to Inventory storage location/bin. ID only.</summary>
    public Guid? StorageLocationId { get; set; }
    public string? StorageLocationName { get; set; }

    // ─── Quality ───────────────────────────────────────────────────────────────
    public QualityInspectionStatus QualityStatus { get; set; } = QualityInspectionStatus.Pending;
    public string? QualityNotes { get; set; }
    public Guid? InspectedByUserId { get; set; }
    public DateTime? InspectedAt { get; set; }

    public string? Notes { get; set; }
}
