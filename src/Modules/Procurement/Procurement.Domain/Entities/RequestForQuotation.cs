using Nexcore.SharedKernel;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Request for Quotation (RFQ) / Invitation to Bid sent to one or more vendors.
/// Aligned with SAP RFQ (ME41), Oracle Sourcing RFQ, Dynamics RFQ, Odoo purchase.rfq.
/// </summary>
public class RequestForQuotation : BaseEntity
{
    // ─── Identity ──────────────────────────────────────────────────────────────
    /// <summary>Auto-generated number (e.g., RFQ-2024-00001).</summary>
    public string RFQNumber { get; set; } = string.Empty;
    public required string Title { get; set; }

    // ─── Source ────────────────────────────────────────────────────────────────
    /// <summary>Requisition that triggered this RFQ. Null if created independently.</summary>
    public Guid? RequisitionId { get; set; }
    public PurchaseRequisition? Requisition { get; set; }

    // ─── Dates ─────────────────────────────────────────────────────────────────
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public DateTime SubmissionDeadline { get; set; }
    public DateTime? QuotationValidityDate { get; set; }
    public DateTime? AwardedAt { get; set; }

    // ─── Status ────────────────────────────────────────────────────────────────
    public RFQStatus Status { get; set; } = RFQStatus.Draft;

    // ─── Currency ──────────────────────────────────────────────────────────────
    public string CurrencyCode { get; set; } = "USD";

    // ─── Delivery ──────────────────────────────────────────────────────────────
    public Guid? DeliveryAddressId { get; set; }
    public DateTime? RequiredDeliveryDate { get; set; }

    public string? TermsAndConditions { get; set; }
    public string? EvaluationCriteria { get; set; }
    public string? Notes { get; set; }

    // ─── Navigation ────────────────────────────────────────────────────────────
    public ICollection<RFQLine> Lines { get; set; } = [];
    public ICollection<RFQVendor> InvitedVendors { get; set; } = [];
    public ICollection<VendorQuotation> VendorQuotations { get; set; } = [];
}
