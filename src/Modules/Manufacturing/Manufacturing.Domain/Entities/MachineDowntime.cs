using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Machine Downtime - records unplanned or planned stoppages at a work center.
/// Used for capacity loss tracking, OEE (Overall Equipment Effectiveness) calculation,
/// and integration with Plant Maintenance.
/// SAP Equivalent: PM Notification / Work Order | Oracle Equivalent: Resource Downtime
/// </summary>
public class MachineDowntime : BaseEntity
{
    /// <summary>
    /// Work Center affected by the downtime
    /// </summary>
    public Guid WorkCenterId { get; set; }

    /// <summary>
    /// Production Order impacted by this downtime (optional)
    /// </summary>
    public Guid? ProductionOrderId { get; set; }

    /// <summary>
    /// Start timestamp of the downtime
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// End timestamp of the downtime (null if still ongoing)
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Calculated duration in hours (set when EndTime is recorded)
    /// </summary>
    public decimal? DurationHours { get; set; }

    /// <summary>
    /// Category of downtime
    /// </summary>
    public required string Category { get; set; } // Breakdown, PlannedMaintenance, Setup, QualityIssue, MaterialShortage, Idle, UtilityFailure

    /// <summary>
    /// Detailed reason for the downtime
    /// </summary>
    public required string Reason { get; set; }

    /// <summary>
    /// Root cause analysis description
    /// </summary>
    public string? RootCause { get; set; }

    /// <summary>
    /// Resolution / corrective action taken
    /// </summary>
    public string? Resolution { get; set; }

    /// <summary>
    /// User who reported the downtime
    /// </summary>
    public Guid ReportedById { get; set; }

    /// <summary>
    /// User who resolved the downtime
    /// </summary>
    public Guid? ResolvedById { get; set; }

    /// <summary>
    /// Current status of the downtime record
    /// </summary>
    public required string Status { get; set; } = "Open"; // Open, InProgress, Resolved, Closed

    /// <summary>
    /// Maintenance work order reference if escalated to maintenance (External - from Maintenance module)
    /// </summary>
    public Guid? MaintenanceWorkOrderId { get; set; }

    /// <summary>
    /// Notes and remarks
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public WorkCenter? WorkCenter { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }
}
