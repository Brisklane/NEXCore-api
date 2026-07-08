using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Cost Entry - stores production cost posting and accounting integration
/// </summary>
public class CostEntry : BaseEntity
{
    /// <summary>
    /// Production Order reference
    /// </summary>
    public Guid ProductionOrderId { get; set; }

    /// <summary>
    /// Material cost component
    /// </summary>
    public decimal MaterialCost { get; set; }

    /// <summary>
    /// Labor cost component
    /// </summary>
    public decimal LaborCost { get; set; }

    /// <summary>
    /// Machine cost component
    /// </summary>
    public decimal? MachineCost { get; set; }

    /// <summary>
    /// Overhead cost component
    /// </summary>
    public decimal OverheadCost { get; set; }

    /// <summary>
    /// Scrap/Waste cost
    /// </summary>
    public decimal? ScrapCost { get; set; }

    /// <summary>
    /// Total production cost
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Journal Entry reference (External - from Accounting module)
    /// </summary>
    public Guid? JournalEntryId { get; set; }

    /// <summary>
    /// Timestamp when cost was posted
    /// </summary>
    public DateTime PostedAt { get; set; }

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public ProductionOrder? ProductionOrder { get; set; }
}