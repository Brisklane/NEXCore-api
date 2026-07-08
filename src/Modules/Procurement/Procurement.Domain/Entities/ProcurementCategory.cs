using Nexcore.SharedKernel;

namespace Procurement.Domain.Entities;

/// <summary>
/// Hierarchical commodity / spend category used to classify purchase lines.
/// Aligned with SAP Material Group, Oracle Category (UNSPSC), Odoo Product Category.
/// Links to Accounting for GL account defaulting.
/// </summary>
public class ProcurementCategory : BaseEntity
{
    public required string Name { get; set; }
    public new required string Code { get; set; }

    public Guid? ParentCategoryId { get; set; }
    public ProcurementCategory? ParentCategory { get; set; }
    public ICollection<ProcurementCategory> SubCategories { get; set; } = [];

    /// <summary>Cross-module reference to default Accounting GL account. ID only.</summary>
    public Guid? DefaultLedgerAccountId { get; set; }

    /// <summary>Cross-module reference to default Tax Code in Accounting. ID only.</summary>
    public Guid? DefaultTaxCodeId { get; set; }

    /// <summary>When true, any PO line in this category requires a requisition.</summary>
    public bool RequireRequisition { get; set; }

    /// <summary>Amount threshold above which an RFQ is mandatory.</summary>
    public decimal? RFQThresholdAmount { get; set; }

    public int SortOrder { get; set; }
}
