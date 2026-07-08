using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Rework Order - handles reprocessing of rejected/failed items from a production order.
/// Created when an Inspection result is Rejected or Rework.
/// SAP Equivalent: QM Re-work Order / PP Rework Production Order
/// Oracle Equivalent: Rework Work Order
/// </summary>
public class ReworkOrder : BaseEntity
{
    /// <summary>
    /// Original production order from which the rework originated
    /// </summary>
    public Guid ProductionOrderId { get; set; }

    /// <summary>
    /// Inspection record that triggered this rework (optional but recommended)
    /// </summary>
    public Guid? InspectionId { get; set; }

    /// <summary>
    /// Routing to follow for rework (may differ from the original production routing)
    /// </summary>
    public Guid? ReworkRoutingId { get; set; }

    /// <summary>
    /// Quantity sent for rework
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Quantity successfully completed after rework
    /// </summary>
    public decimal QuantityCompleted { get; set; } = 0;

    /// <summary>
    /// Quantity rejected again after rework (scrapped)
    /// </summary>
    public decimal QuantityRejected { get; set; } = 0;

    /// <summary>
    /// Unit of measure
    /// </summary>
    public required string UnitOfMeasure { get; set; }

    /// <summary>
    /// Root cause or reason for rework
    /// </summary>
    public required string Reason { get; set; }

    /// <summary>
    /// Current status of the rework order
    /// </summary>
    public required string Status { get; set; } // Draft, Released, InProgress, Completed, Cancelled

    /// <summary>
    /// Scheduled start date for rework
    /// </summary>
    public DateTime? ScheduledStartDate { get; set; }

    /// <summary>
    /// Scheduled completion date for rework
    /// </summary>
    public DateTime? ScheduledEndDate { get; set; }

    /// <summary>
    /// Notes and remarks
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public ProductionOrder? ProductionOrder { get; set; }
    public Inspection? Inspection { get; set; }
    public Routing? ReworkRouting { get; set; }
}
