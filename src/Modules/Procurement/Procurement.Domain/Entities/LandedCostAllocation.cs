using Nexcore.SharedKernel;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Computed allocation of a landed cost line to a specific goods receipt line.
/// Created when the landed cost is validated; drives the inventory value adjustment
/// and the AP journal entry posted to Accounting.
/// </summary>
public class LandedCostAllocation : BaseEntity
{
    public Guid LandedCostId { get; set; }
    public LandedCost LandedCost { get; set; } = null!;

    public Guid LandedCostLineId { get; set; }
    public LandedCostLine LandedCostLine { get; set; } = null!;

    public Guid GoodsReceiptLineId { get; set; }
    public GoodsReceiptLine GoodsReceiptLine { get; set; } = null!;

    // ─── Allocation Basis ──────────────────────────────────────────────────────
    public LandedCostAllocationMethod AllocationMethod { get; set; }

    /// <summary>The basis value used to compute this line's share (quantity, line value, weight, etc.).</summary>
    public decimal AllocationBasisValue { get; set; }

    /// <summary>Total basis value across all GRN lines in this allocation run (for percentage calculation).</summary>
    public decimal TotalBasisValue { get; set; }

    public decimal AllocationPercent { get; set; }

    // ─── Amounts ───────────────────────────────────────────────────────────────
    public decimal AllocatedAmount { get; set; }

    /// <summary>Per-unit cost addition for inventory valuation update.</summary>
    public decimal AllocatedAmountPerUnit { get; set; }
}
