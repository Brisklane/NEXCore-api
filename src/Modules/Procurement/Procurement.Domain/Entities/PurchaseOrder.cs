using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Enums;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Purchase Order — legally binding commitment to buy from a vendor.
/// Aligned with SAP PO (ME21N), Oracle Purchase Order, Dynamics PO, Odoo purchase.order.
/// Central document in the Procure-to-Pay cycle.
/// </summary>
public class PurchaseOrder : BaseEntity
{
    // ─── Identity ──────────────────────────────────────────────────────────────
    /// <summary>Auto-generated order number (e.g., PO-2024-00001).</summary>
    public string OrderNumber { get; set; } = string.Empty;

    // ─── Vendor ────────────────────────────────────────────────────────────────
    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;
    public string? VendorName { get; set; }
    public string? VendorReference { get; set; }

    // ─── Source Documents ──────────────────────────────────────────────────────
    public Guid? QuotationId { get; set; }
    public VendorQuotation? Quotation { get; set; }

    public Guid? RequisitionId { get; set; }
    public PurchaseRequisition? Requisition { get; set; }

    public Guid? ContractId { get; set; }
    public PurchaseContract? Contract { get; set; }

    // ─── Dates ─────────────────────────────────────────────────────────────────
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime? ConfirmedDeliveryDate { get; set; }
    public DateTime? SentToVendorAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    // ─── Status ────────────────────────────────────────────────────────────────
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    // ─── Delivery ──────────────────────────────────────────────────────────────
    public Guid? DeliveryAddressId { get; set; }
    public string? DeliveryStreet { get; set; }
    public string? DeliveryCity { get; set; }
    public string? DeliveryState { get; set; }
    public string? DeliveryPostalCode { get; set; }
    public string? DeliveryCountry { get; set; }

    // ─── Currency & Terms ──────────────────────────────────────────────────────
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;
    public PaymentTerms PaymentTerms { get; set; } = PaymentTerms.Net30;
    public Incoterm? Incoterm { get; set; }
    public string? IncotermLocation { get; set; }

    // ─── Financials ────────────────────────────────────────────────────────────
    public decimal SubTotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal TotalAmount { get; set; }

    // ─── Invoicing Tracking ────────────────────────────────────────────────────
    public decimal InvoicedAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }

    // ─── Accounting Integration ────────────────────────────────────────────────
    /// <summary>
    /// Cross-module reference to a budget in Accounting for budget availability checking.
    /// ID only — checked on PO confirmation, not stored as FK navigation.
    /// </summary>
    public Guid? BudgetId { get; set; }

    /// <summary>
    /// Cross-module reference to the encumbrance/commitment Journal Entry created when
    /// this PO is approved (if commitment accounting is enabled in Accounting settings).
    /// Entry: Dr Budget Commitment, Cr Budget Available.
    /// Reversed automatically when the matching AP invoice is posted.
    /// ID only.
    /// </summary>
    public Guid? AccountingCommitmentEntryId { get; set; }

    // ─── Approval ──────────────────────────────────────────────────────────────
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public string? TermsAndConditions { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    public string? CancellationReason { get; set; }

    // ─── Navigation ────────────────────────────────────────────────────────────
    public ICollection<PurchaseOrderLine> Lines { get; set; } = [];
    public ICollection<GoodsReceipt> GoodsReceipts { get; set; } = [];
    public ICollection<PurchaseInvoice> PurchaseInvoices { get; set; } = [];
    public ICollection<PurchaseOrderApproval> Approvals { get; set; } = [];
    public ICollection<PurchaseOrderAmendment> Amendments { get; set; } = [];
}
