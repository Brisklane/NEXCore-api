using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Material Planning Data - defines MRP parameters for a product.
/// Drives automatic production and procurement decisions.
/// SAP Equivalent: MRP1/MRP2/MRP3 tabs on Material Master (MM02)
/// Oracle Equivalent: Item Planning Attributes
/// </summary>
public class MaterialPlanningData : BaseEntity
{
    /// <summary>
    /// Product reference (External - from Inventory module)
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Safety stock level to maintain at all times
    /// </summary>
    public decimal SafetyStock { get; set; }

    /// <summary>
    /// Reorder point — triggers replenishment when stock falls below this
    /// </summary>
    public decimal ReorderPoint { get; set; }

    /// <summary>
    /// Maximum allowed stock level (for min/max planning strategy)
    /// </summary>
    public decimal MaximumStockLevel { get; set; }

    /// <summary>
    /// Fixed lot size for production or procurement
    /// </summary>
    public decimal LotSize { get; set; }

    /// <summary>
    /// Lead time in days for production or procurement
    /// </summary>
    public int LeadTimeDays { get; set; }

    /// <summary>
    /// Planning horizon in days — how far ahead MRP generates requirements
    /// </summary>
    public int PlanningHorizonDays { get; set; } = 90;

    /// <summary>
    /// Expected scrap/loss percentage during manufacturing or procurement
    /// Used in net requirements calculation
    /// </summary>
    public decimal ScrapPercentage { get; set; } = 0;

    /// <summary>
    /// Type of procurement — InHouse (manufactured) or External (purchased)
    /// </summary>
    public required string ProcurementType { get; set; } // InHouse, External, Both

    /// <summary>
    /// MRP planning strategy type
    /// </summary>
    public required string MRPType { get; set; } // ReorderPoint, ForecastBased, MakeToOrder, MinMax

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }
}
