using Nexcore.SharedKernel;

namespace Procurement.Domain.Entities;

/// <summary>
/// Approved Vendor List (AVL) / Source List entry.
/// Defines which vendors are authorised to supply a specific item or category,
/// with optional exclusivity, preferred routing, and date-range validity.
///
/// Aligned with:
///   SAP  — Source List (ME01) + Purchasing Info Record (ME11)
///   Oracle — Approved Supplier List (ASL)
///   Dynamics — Approved vendor per item
///   Odoo — Vendor routes on product (supplierinfo)
/// </summary>
public class ApprovedVendorList : BaseEntity
{
    // ─── Scope ─────────────────────────────────────────────────────────────────
    /// <summary>
    /// Specific inventory item this entry covers.
    /// Cross-module reference to Inventory. ID only.
    /// Null when the entry covers an entire ProcurementCategory.
    /// </summary>
    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemDescription { get; set; }

    /// <summary>
    /// Procurement category this entry covers.
    /// Used when no specific item is set (category-level sourcing rule).
    /// </summary>
    public Guid? ProcurementCategoryId { get; set; }
    public ProcurementCategory? ProcurementCategory { get; set; }

    // ─── Vendor ────────────────────────────────────────────────────────────────
    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    // ─── Validity ──────────────────────────────────────────────────────────────
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime? ValidTo { get; set; }

    // ─── Routing Rules ─────────────────────────────────────────────────────────
    /// <summary>
    /// When true, this vendor is the default selection for requisition auto-routing.
    /// Only one entry per item/category per company should be IsPreferred = true.
    /// </summary>
    public bool IsPreferred { get; set; }

    /// <summary>
    /// When true, only this vendor may supply the item/category.
    /// Any PO raised to another vendor triggers a validation warning.
    /// </summary>
    public bool IsExclusive { get; set; }

    /// <summary>
    /// When true, this vendor is explicitly blocked from supplying the item/category.
    /// Used to override a previously approved entry.
    /// </summary>
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }

    // ─── Commercial Defaults (from Info Record) ────────────────────────────────
    public decimal? DefaultUnitPrice { get; set; }
    public string? CurrencyCode { get; set; }
    public int LeadTimeDays { get; set; }
    public decimal? MinimumOrderQuantity { get; set; }
    public Guid? UnitOfMeasureId { get; set; }

    // ─── Quality ───────────────────────────────────────────────────────────────
    public bool RequiresQualityInspection { get; set; }

    // ─── Approval ──────────────────────────────────────────────────────────────
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public string? Notes { get; set; }
}
