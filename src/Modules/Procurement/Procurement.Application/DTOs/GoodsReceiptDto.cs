using Procurement.Domain.Enums;

namespace Procurement.Application.DTOs;

// ── Goods Receipt Read ────────────────────────────────────────────────────────

public class GoodsReceiptDto
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public string? VendorDeliveryNoteNumber { get; set; }

    public Guid PurchaseOrderId { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public Guid VendorId { get; set; }
    public string? VendorName { get; set; }

    public DateTime ReceiptDate { get; set; }
    public DateTime? PostedAt { get; set; }

    public GoodsReceiptStatus Status { get; set; }
    public ReceiptType ReceiptType { get; set; }

    public Guid? WarehouseId { get; set; }
    public Guid? StorageLocationId { get; set; }
    public Guid? PostedByUserId { get; set; }
    public Guid? OriginalReceiptId { get; set; }

    public Guid? AccountingJournalEntryId { get; set; }
    public Guid? FiscalPeriodId { get; set; }

    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }

    public List<GoodsReceiptLineDto> Lines { get; set; } = [];
}

public class GoodsReceiptLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public Guid PurchaseOrderLineId { get; set; }

    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public string ItemDescription { get; set; } = string.Empty;

    public decimal QuantityOrdered { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityAccepted { get; set; }
    public decimal QuantityRejected { get; set; }

    public Guid? UnitOfMeasureId { get; set; }
    public string? UnitOfMeasureName { get; set; }

    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? ManufacturerBatchNumber { get; set; }

    public Guid? StorageLocationId { get; set; }
    public string? StorageLocationName { get; set; }

    public QualityInspectionStatus QualityStatus { get; set; }
    public string? QualityNotes { get; set; }
    public Guid? InspectedByUserId { get; set; }
    public DateTime? InspectedAt { get; set; }

    public string? Notes { get; set; }
}

// ── Goods Receipt Create ──────────────────────────────────────────────────────

public class CreateGoodsReceiptDto
{
    public Guid PurchaseOrderId { get; set; }
    public string? VendorDeliveryNoteNumber { get; set; }
    public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;
    public ReceiptType ReceiptType { get; set; } = ReceiptType.Standard;
    public Guid? WarehouseId { get; set; }
    public Guid? StorageLocationId { get; set; }
    public Guid? OriginalReceiptId { get; set; }
    public Guid? FiscalPeriodId { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    public List<CreateGoodsReceiptLineDto> Lines { get; set; } = [];
}

public class CreateGoodsReceiptLineDto
{
    public Guid PurchaseOrderLineId { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityAccepted { get; set; }
    public decimal QuantityRejected { get; set; }
    public Guid? StorageLocationId { get; set; }
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? ManufacturerBatchNumber { get; set; }
    public string? Notes { get; set; }
}

public class InspectGoodsReceiptLineDto
{
    public Guid LineId { get; set; }
    public QualityInspectionStatus QualityStatus { get; set; }
    public string? QualityNotes { get; set; }
    public decimal QuantityAccepted { get; set; }
    public decimal QuantityRejected { get; set; }
}

public class UpdateGoodsReceiptDto
{
    public string? VendorDeliveryNoteNumber { get; set; }
    public DateTime? ReceiptDate { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? StorageLocationId { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
}
