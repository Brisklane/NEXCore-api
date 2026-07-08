using Procurement.Domain.Enums;

namespace Procurement.Application.DTOs;

// ── Purchase Requisition Read ─────────────────────────────────────────────────

public class PurchaseRequisitionDto
{
    public Guid Id { get; set; }
    public string RequisitionNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid RequestedByUserId { get; set; }
    public string? RequestedByName { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public Guid? CostCenterId { get; set; }

    public DateTime RequestDate { get; set; }
    public DateTime? RequiredByDate { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }

    public RequisitionStatus Status { get; set; }
    public RequisitionPriority Priority { get; set; }

    public Guid? SuggestedVendorId { get; set; }
    public string? SuggestedVendorName { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal EstimatedTotalAmount { get; set; }

    public Guid? BudgetId { get; set; }
    public bool IsBudgetChecked { get; set; }
    public bool IsBudgetAvailable { get; set; }

    public string? RejectionReason { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }

    /// <summary>True when a purchase order has already been created from this requisition
    /// (auto-created on approval, or converted manually) — used to hide the "Convert to PO" action.</summary>
    public bool HasPurchaseOrder { get; set; }

    public List<PurchaseRequisitionLineDto> Lines { get; set; } = [];
}

public class PurchaseRequisitionLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public string? UnitOfMeasureName { get; set; }
    public decimal? EstimatedUnitPrice { get; set; }
    public decimal? EstimatedTotalPrice { get; set; }
    public Guid? ProcurementCategoryId { get; set; }
    public string? ProcurementCategoryName { get; set; }
    public DateTime? RequiredByDate { get; set; }
    public Guid? DeliveryLocationId { get; set; }
    public RequisitionLineStatus LineStatus { get; set; }
    public decimal QuantityOrdered { get; set; }
    public decimal QuantityRemaining { get; set; }
    public Guid? SuggestedVendorId { get; set; }
    public string? SuggestedVendorName { get; set; }
    public string? Notes { get; set; }
}

// ── Purchase Requisition Create ───────────────────────────────────────────────

public class CreatePurchaseRequisitionDto
{
    public required string Title { get; set; }
    public string? Description { get; set; }

    public Guid RequestedByUserId { get; set; }
    public string? RequestedByName { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public Guid? CostCenterId { get; set; }

    public DateTime? RequiredByDate { get; set; }
    public RequisitionPriority Priority { get; set; } = RequisitionPriority.Normal;
    public Guid? SuggestedVendorId { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public Guid? BudgetId { get; set; }

    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }

    public List<CreatePurchaseRequisitionLineDto> Lines { get; set; } = [];
}

public class CreatePurchaseRequisitionLineDto
{
    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public required string ItemDescription { get; set; }
    public decimal Quantity { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public string? UnitOfMeasureName { get; set; }
    public decimal? EstimatedUnitPrice { get; set; }
    public Guid? ProcurementCategoryId { get; set; }
    public DateTime? RequiredByDate { get; set; }
    public Guid? DeliveryLocationId { get; set; }
    public Guid? SuggestedVendorId { get; set; }
    public string? Notes { get; set; }
}

public class RejectRequisitionDto
{
    public required string RejectionReason { get; set; }
}

public class UpdatePurchaseRequisitionDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? RequiredByDate { get; set; }
    public RequisitionPriority? Priority { get; set; }
    public Guid? SuggestedVendorId { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    public List<CreatePurchaseRequisitionLineDto>? Lines { get; set; }
}
