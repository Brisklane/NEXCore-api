using Nexcore.SharedKernel.Enums;
using Procurement.Domain.Enums;

namespace Procurement.Application.DTOs;

// ── Purchase Order Read ───────────────────────────────────────────────────────

public class PurchaseOrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;

    public Guid VendorId { get; set; }
    public string? VendorName { get; set; }
    public string? VendorReference { get; set; }

    public Guid? QuotationId { get; set; }
    public string? QuotationNumber { get; set; }
    public Guid? RequisitionId { get; set; }
    public string? RequisitionNumber { get; set; }
    public Guid? ContractId { get; set; }
    public string? ContractNumber { get; set; }

    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime? ConfirmedDeliveryDate { get; set; }
    public DateTime? SentToVendorAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public PurchaseOrderStatus Status { get; set; }

    public Guid? DeliveryAddressId { get; set; }
    public string? DeliveryStreet { get; set; }
    public string? DeliveryCity { get; set; }
    public string? DeliveryState { get; set; }
    public string? DeliveryPostalCode { get; set; }
    public string? DeliveryCountry { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;
    public PaymentTerms PaymentTerms { get; set; }
    public Incoterm? Incoterm { get; set; }
    public string? IncotermLocation { get; set; }

    public decimal SubTotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public decimal InvoicedAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }

    public Guid? BudgetId { get; set; }
    public Guid? AccountingCommitmentEntryId { get; set; }

    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public string? TermsAndConditions { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    public string? CancellationReason { get; set; }

    public List<PurchaseOrderLineDto> Lines { get; set; } = [];
}

public class PurchaseOrderLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }

    public Guid? RequisitionLineId { get; set; }
    public Guid? ContractLineId { get; set; }

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
    public decimal SubTotal { get; set; }
    public decimal TotalPrice { get; set; }

    public Guid? ProcurementCategoryId { get; set; }
    public string? ProcurementCategoryName { get; set; }
    public Guid? LedgerAccountId { get; set; }
    public Guid? CostCenterId { get; set; }
    public Guid? DimensionSetId { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; }
    public Guid? DeliveryLocationId { get; set; }

    public decimal QuantityReceived { get; set; }
    public decimal QuantityAccepted { get; set; }
    public decimal QuantityRejected { get; set; }
    public decimal QuantityReturned { get; set; }
    public decimal QuantityInvoiced { get; set; }
    public decimal QuantityRemaining { get; set; }
    public decimal QuantityToInvoice { get; set; }

    public PurchaseOrderLineStatus LineStatus { get; set; }
    public string? Notes { get; set; }
}

// ── Purchase Order Create ─────────────────────────────────────────────────────

public class CreatePurchaseOrderDto
{
    public Guid VendorId { get; set; }
    public string? VendorReference { get; set; }

    public Guid? QuotationId { get; set; }
    public Guid? RequisitionId { get; set; }
    public Guid? ContractId { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedDeliveryDate { get; set; }

    public Guid? DeliveryAddressId { get; set; }
    public string? DeliveryStreet { get; set; }
    public string? DeliveryCity { get; set; }
    public string? DeliveryState { get; set; }
    public string? DeliveryPostalCode { get; set; }
    public string? DeliveryCountry { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;
    public PaymentTerms PaymentTerms { get; set; } = PaymentTerms.Net30;
    public Incoterm? Incoterm { get; set; }
    public string? IncotermLocation { get; set; }

    public decimal ShippingAmount { get; set; }

    public Guid? BudgetId { get; set; }

    public string? TermsAndConditions { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }

    public List<CreatePurchaseOrderLineDto> Lines { get; set; } = [];
}

public class CreatePurchaseOrderLineDto
{
    public Guid? RequisitionLineId { get; set; }
    public Guid? ContractLineId { get; set; }

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
    public Guid? CostCenterId { get; set; }
    public Guid? DimensionSetId { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; }
    public Guid? DeliveryLocationId { get; set; }
    public string? Notes { get; set; }
}

public class CancelPurchaseOrderDto
{
    public required string CancellationReason { get; set; }
}

public class UpdatePurchaseOrderDto
{
    public string? VendorReference { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string? DeliveryStreet { get; set; }
    public string? DeliveryCity { get; set; }
    public string? DeliveryState { get; set; }
    public string? DeliveryPostalCode { get; set; }
    public string? DeliveryCountry { get; set; }
    public string? TermsAndConditions { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    public List<CreatePurchaseOrderLineDto>? Lines { get; set; }
}
