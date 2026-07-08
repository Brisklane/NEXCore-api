using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Demand - normalized demand input for MRP from sales orders or forecasts.
/// MRP consumes demand records to generate PlannedOrders.
/// SAP Equivalent: Independent Requirements (MD61) / Sales Order Requirements
/// Oracle Equivalent: Demand Schedule / Sales Forecast
/// </summary>
public class Demand : BaseEntity
{
    /// <summary>
    /// Product reference (External - from Inventory module)
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Required quantity from this demand record
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Quantity already covered by PlannedOrders or ProductionOrders
    /// </summary>
    public decimal FulfilledQty { get; set; } = 0;

    /// <summary>
    /// Due date by which this demand must be fulfilled
    /// </summary>
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Source of demand
    /// </summary>
    public required string SourceType { get; set; } // SalesOrder, Forecast, ServiceOrder, MinStock

    /// <summary>
    /// Reference document ID (Sales Order ID, Forecast ID, etc.)
    /// </summary>
    public Guid? ReferenceId { get; set; }

    /// <summary>
    /// Current fulfillment status of this demand record
    /// </summary>
    public required string Status { get; set; } // Open, PartiallyFulfilled, Fulfilled, Cancelled

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }
}
