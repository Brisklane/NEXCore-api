using Procurement.Domain.Enums;

namespace Procurement.Application.DTOs;

// ── Purchase Return (Return-to-Vendor) Read ─────────────────────────────────────

public class PurchaseReturnDto
{
    public Guid Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;

    public Guid PurchaseOrderId { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public Guid GoodsReceiptId { get; set; }
    public string? GoodsReceiptNumber { get; set; }
    public Guid VendorId { get; set; }
    public string? VendorName { get; set; }

    public DateTime ReturnDate { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PostedAt { get; set; }

    public PurchaseReturnStatus Status { get; set; }
    public PurchaseReturnReason ReturnReason { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal TotalReturnAmount { get; set; }

    public string? VendorReturnAuthorisationNumber { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }

    public Guid? DebitNoteId { get; set; }
    public string? DebitNoteNumber { get; set; }

    public List<PurchaseReturnLineDto> Lines { get; set; } = [];
}

public class PurchaseReturnLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public Guid GoodsReceiptLineId { get; set; }
    public Guid PurchaseOrderLineId { get; set; }
    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public decimal QuantityReceived { get; set; }
    public decimal QuantityReturned { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public string? UnitOfMeasureName { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalReturnAmount { get; set; }
    public PurchaseReturnReason ReturnReason { get; set; }
    public string? QualityIssueDescription { get; set; }
    public string? LotNumber { get; set; }
    public string? Notes { get; set; }
}

// ── Create ──────────────────────────────────────────────────────────────────────

public class CreatePurchaseReturnDto
{
    public required Guid GoodsReceiptId { get; set; }
    public DateTime ReturnDate { get; set; } = DateTime.UtcNow;
    public PurchaseReturnReason ReturnReason { get; set; }
    public string? VendorReturnAuthorisationNumber { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public List<CreatePurchaseReturnLineDto> Lines { get; set; } = [];
}

public class CreatePurchaseReturnLineDto
{
    public required Guid GoodsReceiptLineId { get; set; }
    public decimal QuantityReturned { get; set; }
    public PurchaseReturnReason ReturnReason { get; set; }
    public string? QualityIssueDescription { get; set; }
    public string? LotNumber { get; set; }
    public string? Notes { get; set; }
}
