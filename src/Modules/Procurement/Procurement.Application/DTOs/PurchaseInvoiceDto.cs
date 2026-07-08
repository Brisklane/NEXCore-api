using Nexcore.SharedKernel.Enums;
using Procurement.Domain.Enums;

namespace Procurement.Application.DTOs;

// ── Purchase Invoice Read ─────────────────────────────────────────────────────

public class PurchaseInvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string VendorInvoiceNumber { get; set; } = string.Empty;

    public Guid VendorId { get; set; }
    public string? VendorName { get; set; }

    public Guid? PurchaseOrderId { get; set; }
    public string? PurchaseOrderNumber { get; set; }

    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime PostingDate { get; set; }

    public PurchaseInvoiceStatus Status { get; set; }
    public InvoiceMatchingStatus MatchingStatus { get; set; }
    public InvoicePaymentStatus PaymentStatus { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;

    public decimal SubTotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal RecoverableTaxAmount { get; set; }
    public decimal NonRecoverableTaxAmount { get; set; }

    public PaymentTerms PaymentTerms { get; set; }

    public Guid? AccountingJournalEntryId { get; set; }
    public Guid? FiscalPeriodId { get; set; }
    public Guid? DimensionSetId { get; set; }

    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? PostedByUserId { get; set; }
    public DateTime? PostedAt { get; set; }

    public string? HoldReason { get; set; }
    public string? DisputeReason { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }

    public List<PurchaseInvoiceLineDto> Lines { get; set; } = [];
}

public class PurchaseInvoiceLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }

    public Guid? PurchaseOrderLineId { get; set; }
    public Guid? GoodsReceiptLineId { get; set; }

    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public string ItemDescription { get; set; } = string.Empty;

    public decimal Quantity { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public string? UnitOfMeasureName { get; set; }

    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }

    public Guid? TaxCodeId { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal RecoverableTaxAmount { get; set; }
    public decimal NonRecoverableTaxAmount { get; set; }

    public decimal SubTotal { get; set; }
    public decimal TotalPrice { get; set; }

    public Guid? ProcurementCategoryId { get; set; }
    public string? ProcurementCategoryName { get; set; }
    public Guid? LedgerAccountId { get; set; }
    public Guid? DimensionSetId { get; set; }

    public string? Notes { get; set; }
}

// ── Purchase Invoice Create ───────────────────────────────────────────────────

public class CreatePurchaseInvoiceDto
{
    public required string VendorInvoiceNumber { get; set; }
    public Guid VendorId { get; set; }
    public Guid? PurchaseOrderId { get; set; }

    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime PostingDate { get; set; } = DateTime.UtcNow;

    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;
    public PaymentTerms PaymentTerms { get; set; } = PaymentTerms.Net30;

    public Guid? FiscalPeriodId { get; set; }
    public Guid? DimensionSetId { get; set; }

    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }

    public List<CreatePurchaseInvoiceLineDto> Lines { get; set; } = [];
}

public class CreatePurchaseInvoiceLineDto
{
    public Guid? PurchaseOrderLineId { get; set; }
    public Guid? GoodsReceiptLineId { get; set; }

    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public required string ItemDescription { get; set; }

    public decimal Quantity { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public string? UnitOfMeasureName { get; set; }

    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public Guid? TaxCodeId { get; set; }
    public decimal TaxPercent { get; set; }

    public Guid? ProcurementCategoryId { get; set; }
    public Guid? LedgerAccountId { get; set; }
    public Guid? DimensionSetId { get; set; }

    public string? Notes { get; set; }
}

public class HoldInvoiceDto
{
    public required string HoldReason { get; set; }
}

public class DisputeInvoiceDto
{
    public required string DisputeReason { get; set; }
}

public class UpdatePurchaseInvoiceDto
{
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid? FiscalPeriodId { get; set; }
    public Guid? DimensionSetId { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    public List<CreatePurchaseInvoiceLineDto>? Lines { get; set; }
}
