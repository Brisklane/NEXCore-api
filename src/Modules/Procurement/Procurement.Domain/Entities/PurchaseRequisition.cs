using Nexcore.SharedKernel;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Internal purchase request raised by a department before a PO is issued.
/// Aligned with SAP Purchase Requisition (ME51N), Oracle iProcurement Requisition,
/// Dynamics Purchase Requisition, Odoo purchase.order (internal request).
/// </summary>
public class PurchaseRequisition : BaseEntity
{
    // ─── Identity ──────────────────────────────────────────────────────────────
    /// <summary>Auto-generated number (e.g., PR-2024-00001).</summary>
    public string RequisitionNumber { get; set; } = string.Empty;

    public required string Title { get; set; }

    // ─── Requester ─────────────────────────────────────────────────────────────
    /// <summary>Cross-module reference to Auth/HR user who raised the request.</summary>
    public Guid RequestedByUserId { get; set; }
    public string? RequestedByName { get; set; }

    /// <summary>Cross-module reference to HR Department. ID only.</summary>
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }

    /// <summary>Cross-module reference to HR CostCenter. ID only.</summary>
    public Guid? CostCenterId { get; set; }

    // ─── Dates ─────────────────────────────────────────────────────────────────
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public DateTime? RequiredByDate { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }

    // ─── Status ────────────────────────────────────────────────────────────────
    public RequisitionStatus Status { get; set; } = RequisitionStatus.Draft;
    public RequisitionPriority Priority { get; set; } = RequisitionPriority.Normal;

    // ─── Suggested Vendor ──────────────────────────────────────────────────────
    public Guid? SuggestedVendorId { get; set; }
    public Vendor? SuggestedVendor { get; set; }

    // ─── Financials ────────────────────────────────────────────────────────────
    public string CurrencyCode { get; set; } = "USD";
    public decimal EstimatedTotalAmount { get; set; }

    // ─── Budget ────────────────────────────────────────────────────────────────
    /// <summary>Cross-module reference to Accounting Budget. ID only.</summary>
    public Guid? BudgetId { get; set; }
    public bool IsBudgetChecked { get; set; }
    public bool IsBudgetAvailable { get; set; }

    public string? RejectionReason { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }

    // ─── Navigation ────────────────────────────────────────────────────────────
    public ICollection<PurchaseRequisitionLine> Lines { get; set; } = [];
    public ICollection<ProcurementApproval> Approvals { get; set; } = [];
}
