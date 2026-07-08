using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Capacity Load - tracks workload vs available capacity per work center per date/shift.
/// Used for capacity requirements planning (CRP) and scheduling conflict detection.
/// SAP Equivalent: CM Capacity Load (CM01 / CM50)
/// Oracle Equivalent: Resource Load
/// </summary>
public class CapacityLoad : BaseEntity
{
    /// <summary>
    /// Work Center this load entry belongs to
    /// </summary>
    public Guid WorkCenterId { get; set; }

    /// <summary>
    /// Specific shift this load applies to (optional — null means full day)
    /// </summary>
    public Guid? WorkCenterShiftId { get; set; }

    /// <summary>
    /// The date for this capacity load record
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Total required hours loaded from production orders and schedules on this date
    /// </summary>
    public decimal RequiredHours { get; set; }

    /// <summary>
    /// Total available capacity hours on this date (from shift definition)
    /// </summary>
    public decimal AvailableHours { get; set; }

    /// <summary>
    /// Load percentage = (RequiredHours / AvailableHours) * 100
    /// Maintained in sync when RequiredHours or AvailableHours changes
    /// </summary>
    public decimal LoadPercentage { get; set; }

    /// <summary>
    /// Number of production orders contributing to the load on this date
    /// </summary>
    public int ProductionOrderCount { get; set; } = 0;

    /// <summary>
    /// Indicates if this date has an overload (LoadPercentage > 100)
    /// </summary>
    public bool IsOverloaded { get; set; } = false;

    /// <summary>
    /// Notes and remarks
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public WorkCenter? WorkCenter { get; set; }
    public WorkCenterShift? WorkCenterShift { get; set; }
}
