using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Production Variance - records differences between standard/planned cost and actual cost.
/// Used for cost analysis, variance reporting, and accounting settlement.
/// SAP Equivalent: CO-PC Variance (KKBC_ORD / KKS1) | Oracle Equivalent: Work Order Cost Variance
/// </summary>
public class ProductionVariance : BaseEntity
{
    /// <summary>
    /// Production Order reference
    /// </summary>
    public Guid ProductionOrderId { get; set; }

    /// <summary>
    /// Cost Entry reference
    /// </summary>
    public Guid CostEntryId { get; set; }

    /// <summary>
    /// Standard/planned material cost from BOM
    /// </summary>
    public decimal StandardMaterialCost { get; set; }

    /// <summary>
    /// Actual material cost incurred
    /// </summary>
    public decimal ActualMaterialCost { get; set; }

    /// <summary>
    /// Variance on material cost (Actual - Standard)
    /// </summary>
    public decimal MaterialVariance => ActualMaterialCost - StandardMaterialCost;

    /// <summary>
    /// Standard/planned labor cost from routing
    /// </summary>
    public decimal StandardLaborCost { get; set; }

    /// <summary>
    /// Actual labor cost incurred
    /// </summary>
    public decimal ActualLaborCost { get; set; }

    /// <summary>
    /// Variance on labor cost (Actual - Standard)
    /// </summary>
    public decimal LaborVariance => ActualLaborCost - StandardLaborCost;

    /// <summary>
    /// Standard/planned machine cost from work center rates
    /// </summary>
    public decimal StandardMachineCost { get; set; }

    /// <summary>
    /// Actual machine cost incurred
    /// </summary>
    public decimal ActualMachineCost { get; set; }

    /// <summary>
    /// Variance on machine cost (Actual - Standard)
    /// </summary>
    public decimal MachineVariance => ActualMachineCost - StandardMachineCost;

    /// <summary>
    /// Standard overhead cost
    /// </summary>
    public decimal StandardOverheadCost { get; set; }

    /// <summary>
    /// Actual overhead cost incurred
    /// </summary>
    public decimal ActualOverheadCost { get; set; }

    /// <summary>
    /// Variance on overhead cost (Actual - Standard)
    /// </summary>
    public decimal OverheadVariance => ActualOverheadCost - StandardOverheadCost;

    /// <summary>
    /// Total variance across all cost components
    /// </summary>
    public decimal TotalVariance => MaterialVariance + LaborVariance + MachineVariance + OverheadVariance;

    /// <summary>
    /// Primary variance category classification
    /// </summary>
    public required string VarianceCategory { get; set; } // QuantityVariance, PriceVariance, UsageVariance, MixVariance, ScrapVariance

    /// <summary>
    /// Indicates if this variance has been settled to accounting
    /// </summary>
    public bool IsSettled { get; set; } = false;

    /// <summary>
    /// Settlement journal entry reference (External - from Accounting module)
    /// </summary>
    public Guid? SettlementJournalEntryId { get; set; }

    /// <summary>
    /// Timestamp when variance was settled
    /// </summary>
    public DateTime? SettledAt { get; set; }

    /// <summary>
    /// Notes and remarks
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public ProductionOrder? ProductionOrder { get; set; }
    public CostEntry? CostEntry { get; set; }
}
