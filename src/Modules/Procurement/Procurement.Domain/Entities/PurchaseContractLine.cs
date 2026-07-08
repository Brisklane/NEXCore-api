using Nexcore.SharedKernel;

namespace Procurement.Domain.Entities;

/// <summary>
/// Line item in a purchase contract defining item, agreed price, and quantity commitments.
/// </summary>
public class PurchaseContractLine : BaseEntity
{
    public Guid ContractId { get; set; }
    public PurchaseContract Contract { get; set; } = null!;

    public int LineNumber { get; set; }

    // ─── Item ──────────────────────────────────────────────────────────────────
    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public required string ItemDescription { get; set; }

    public Guid? ProcurementCategoryId { get; set; }
    public ProcurementCategory? ProcurementCategory { get; set; }

    // ─── Quantity ──────────────────────────────────────────────────────────────
    public Guid? UnitOfMeasureId { get; set; }
    public string? UnitOfMeasureName { get; set; }
    public decimal? MinimumQuantity { get; set; }
    public decimal? MaximumQuantity { get; set; }
    public decimal? CommittedQuantity { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal RemainingQuantity => (CommittedQuantity ?? MaximumQuantity ?? 0) - OrderedQuantity;

    // ─── Pricing ───────────────────────────────────────────────────────────────
    public decimal UnitPrice { get; set; }
    public decimal? DiscountPercent { get; set; }

    // ─── Validity ──────────────────────────────────────────────────────────────
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    public string? Notes { get; set; }

    // ─── Navigation ────────────────────────────────────────────────────────────
    public ICollection<PurchaseOrderLine> OrderLines { get; set; } = [];
}
