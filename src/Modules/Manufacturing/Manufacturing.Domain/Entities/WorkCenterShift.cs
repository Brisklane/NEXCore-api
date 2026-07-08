using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Work Center Shift - defines shift-based availability and capacity for scheduling.
/// Required for finite/infinite capacity planning and production scheduling.
/// SAP Equivalent: CR Work Center Capacity (CR11) | Oracle Equivalent: Resource Availability
/// </summary>
public class WorkCenterShift : BaseEntity
{
    /// <summary>
    /// Work Center reference
    /// </summary>
    public Guid WorkCenterId { get; set; }

    /// <summary>
    /// Name of the shift
    /// </summary>
    public required string ShiftName { get; set; } // e.g. Morning, Evening, Night, General

    /// <summary>
    /// Shift start time (time of day)
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// Shift end time (time of day)
    /// </summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// Available working hours per shift (can differ from StartTime-EndTime due to breaks)
    /// </summary>
    public decimal AvailableHours { get; set; }

    /// <summary>
    /// Capacity utilization percentage (default 100%)
    /// </summary>
    public decimal CapacityUtilizationPercent { get; set; } = 100;

    /// <summary>
    /// Days of week this shift is active (stored as comma-separated values e.g. "Monday,Tuesday,Wednesday")
    /// </summary>
    public required string WorkingDays { get; set; }

    /// <summary>
    /// Effective from date
    /// </summary>
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>
    /// Effective to date
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    /// Notes and remarks
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public WorkCenter? WorkCenter { get; set; }
}
