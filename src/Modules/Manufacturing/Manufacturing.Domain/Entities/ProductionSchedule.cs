using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Production Schedule - assigns production orders to work centers and time slots.
/// Supports forward and backward scheduling for capacity planning.
/// SAP Equivalent: PP/DS Production Schedule | Oracle Equivalent: Production Scheduling
/// </summary>
public class ProductionSchedule : BaseEntity
{
    /// <summary>
    /// Production Order reference
    /// </summary>
    public Guid ProductionOrderId { get; set; }

    /// <summary>
    /// Work Center assigned for this schedule slot
    /// </summary>
    public Guid WorkCenterId { get; set; }

    /// <summary>
    /// Specific order operation reference (optional - for operation-level scheduling)
    /// </summary>
    public Guid? ProductionOrderOperationId { get; set; }

    /// <summary>
    /// Scheduled start date and time
    /// </summary>
    public DateTime ScheduledStartDate { get; set; }

    /// <summary>
    /// Scheduled end date and time
    /// </summary>
    public DateTime ScheduledEndDate { get; set; }

    /// <summary>
    /// Type of scheduling used
    /// </summary>
    public required string ScheduleType { get; set; } // Forward, Backward, JustInTime

    /// <summary>
    /// Capacity required in hours for this schedule slot
    /// </summary>
    public decimal CapacityRequiredHours { get; set; }

    /// <summary>
    /// Current status of the schedule
    /// </summary>
    public required string Status { get; set; } = "Scheduled"; // Scheduled, Confirmed, InProgress, Completed, Cancelled

    /// <summary>
    /// Indicates if this schedule has a capacity conflict
    /// </summary>
    public bool HasCapacityConflict { get; set; } = false;

    /// <summary>
    /// Conflict description if HasCapacityConflict is true
    /// </summary>
    public string? ConflictDescription { get; set; }

    /// <summary>
    /// Notes and remarks
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public ProductionOrder? ProductionOrder { get; set; }
    public WorkCenter? WorkCenter { get; set; }
    public ProductionOrderOperation? ProductionOrderOperation { get; set; }
}
