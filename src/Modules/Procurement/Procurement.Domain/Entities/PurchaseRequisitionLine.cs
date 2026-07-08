using Nexcore.SharedKernel;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Individual line item on a purchase requisition.
/// </summary>
public class PurchaseRequisitionLine : BaseEntity
{
    public Guid RequisitionId { get; set; }
    public PurchaseRequisition Requisition { get; set; } = null!;

    public int LineNumber { get; set; }

    // ─── Item ──────────────────────────────────────────────────────────────────
    /// <summary>Cross-module reference to Inventory item. Null for free-text (service) lines.</summary>
    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public required string ItemDescription { get; set; }

    // ─── Quantity & Price ──────────────────────────────────────────────────────
    public decimal Quantity { get; set; }
    /// <summary>Cross-module reference to Inventory UoM. ID only.</summary>
    public Guid? UnitOfMeasureId { get; set; }
    public string? UnitOfMeasureName { get; set; }
    public decimal? EstimatedUnitPrice { get; set; }
    public decimal? EstimatedTotalPrice { get; set; }

    // ─── Classification ────────────────────────────────────────────────────────
    public Guid? ProcurementCategoryId { get; set; }
    public ProcurementCategory? ProcurementCategory { get; set; }

    // ─── Delivery ──────────────────────────────────────────────────────────────
    public DateTime? RequiredByDate { get; set; }
    /// <summary>Preferred delivery location. Cross-module ref to Core.Branch.</summary>
    public Guid? DeliveryLocationId { get; set; }

    // ─── Fulfillment Tracking ──────────────────────────────────────────────────
    public RequisitionLineStatus LineStatus { get; set; } = RequisitionLineStatus.Open;
    public decimal QuantityOrdered { get; set; }
    public decimal QuantityRemaining => Quantity - QuantityOrdered;

    // ─── Suggested Vendor ──────────────────────────────────────────────────────
    public Guid? SuggestedVendorId { get; set; }
    public Vendor? SuggestedVendor { get; set; }

    public string? Notes { get; set; }
}
