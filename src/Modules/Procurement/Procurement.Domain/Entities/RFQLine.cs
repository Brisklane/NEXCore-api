using Nexcore.SharedKernel;

namespace Procurement.Domain.Entities;

/// <summary>
/// Line item on a Request for Quotation.
/// </summary>
public class RFQLine : BaseEntity
{
    public Guid RFQId { get; set; }
    public RequestForQuotation RFQ { get; set; } = null!;

    public int LineNumber { get; set; }

    /// <summary>Source requisition line. Null if RFQ created independently.</summary>
    public Guid? RequisitionLineId { get; set; }
    public PurchaseRequisitionLine? RequisitionLine { get; set; }

    // ─── Item ──────────────────────────────────────────────────────────────────
    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public required string ItemDescription { get; set; }

    public decimal Quantity { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public string? UnitOfMeasureName { get; set; }
    public decimal? EstimatedUnitPrice { get; set; }

    public Guid? ProcurementCategoryId { get; set; }
    public ProcurementCategory? ProcurementCategory { get; set; }

    public DateTime? RequiredDeliveryDate { get; set; }
    public string? Specifications { get; set; }
    public string? Notes { get; set; }

    // ─── Navigation ────────────────────────────────────────────────────────────
    public ICollection<VendorQuotationLine> QuotationLines { get; set; } = [];
}
