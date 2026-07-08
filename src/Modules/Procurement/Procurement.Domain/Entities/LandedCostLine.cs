using Nexcore.SharedKernel;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Individual cost component on a Landed Cost document
/// (e.g. "Ocean Freight $500", "Import Duty $120", "Insurance $30").
/// Each line defines how its amount is split across goods receipt lines.
/// </summary>
public class LandedCostLine : BaseEntity
{
    public Guid LandedCostId { get; set; }
    public LandedCost LandedCost { get; set; } = null!;

    public int LineNumber { get; set; }

    public LandedCostType CostType { get; set; }
    public new required string Description { get; set; }

    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>How this cost component is split across received goods lines.</summary>
    public LandedCostAllocationMethod AllocationMethod { get; set; } = LandedCostAllocationMethod.ByValue;

    /// <summary>Cross-module GL account for this cost type. ID only.</summary>
    public Guid? LedgerAccountId { get; set; }

    /// <summary>Cross-module Tax Code in Accounting. ID only.</summary>
    public Guid? TaxCodeId { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }

    public string? Notes { get; set; }
}
