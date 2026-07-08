using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Planned Order - represents MRP-generated production planning demand
/// </summary>
public class PlannedOrder : BaseEntity
{
    /// <summary>
    /// Product reference (External - from Inventory module)
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Planned quantity to produce
    /// </summary>
    public decimal PlannedQty { get; set; }

    /// <summary>
    /// Required/Due date for production
    /// </summary>
    public DateTime RequiredDate { get; set; }

    /// <summary>
    /// Source type of the planned order
    /// </summary>
    public required string SourceType { get; set; } // Forecast, SalesOrder, Reorder

    /// <summary>
    /// Current status of the planned order
    /// </summary>
    public required string Status { get; set; } = "Draft"; // Draft, Planned, Converted, Cancelled

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public ICollection<ProductionOrder>? ProductionOrders { get; set; }
}