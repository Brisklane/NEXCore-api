using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Production Order Operation - order-specific copy of a routing operation step.
/// Tracks planned vs actual hours, yield, and confirmation per operation per order.
/// SAP Equivalent: PP Order Operation (CO03) | Oracle Equivalent: Work Order Operation
/// </summary>
public class ProductionOrderOperation : BaseEntity
{
    /// <summary>
    /// Production Order reference
    /// </summary>
    public Guid ProductionOrderId { get; set; }

    /// <summary>
    /// Source routing operation template reference
    /// </summary>
    public Guid RoutingOperationId { get; set; }

    /// <summary>
    /// Work Center where this operation is performed
    /// </summary>
    public Guid WorkCenterId { get; set; }

    /// <summary>
    /// Sequence number of the operation within the production order
    /// </summary>
    public int SequenceNo { get; set; }

    /// <summary>
    /// Operation name
    /// </summary>
    public required string OperationName { get; set; }

    /// <summary>
    /// Planned setup hours
    /// </summary>
    public decimal PlannedSetupHours { get; set; }

    /// <summary>
    /// Planned labor hours
    /// </summary>
    public decimal PlannedLaborHours { get; set; }

    /// <summary>
    /// Planned machine hours
    /// </summary>
    public decimal PlannedMachineHours { get; set; }

    /// <summary>
    /// Actual setup hours recorded
    /// </summary>
    public decimal ActualSetupHours { get; set; } = 0;

    /// <summary>
    /// Actual labor hours recorded
    /// </summary>
    public decimal ActualLaborHours { get; set; } = 0;

    /// <summary>
    /// Actual machine hours recorded
    /// </summary>
    public decimal ActualMachineHours { get; set; } = 0;

    /// <summary>
    /// Quantity confirmed (yield) for this operation
    /// </summary>
    public decimal ConfirmedQty { get; set; } = 0;

    /// <summary>
    /// Scrap quantity at this operation step
    /// </summary>
    public decimal ScrapQty { get; set; } = 0;

    /// <summary>
    /// Current status of the operation
    /// </summary>
    public required string Status { get; set; } = "Pending"; // Pending, InProgress, Confirmed, Skipped

    /// <summary>
    /// Actual start time of the operation
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Actual completion time of the operation
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// User who confirmed this operation
    /// </summary>
    public Guid? ConfirmedById { get; set; }

    /// <summary>
    /// Notes and remarks
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public ProductionOrder? ProductionOrder { get; set; }
    public RoutingOperation? RoutingOperation { get; set; }
    public WorkCenter? WorkCenter { get; set; }
}
