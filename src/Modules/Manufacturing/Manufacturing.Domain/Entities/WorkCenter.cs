using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Work Center - represents a machine, production station, or labor unit where manufacturing operations are performed
/// </summary>
public class WorkCenter : BaseEntity
{
    /// <summary>
    /// Unique code for the work center
    /// </summary>
    public new required string Code { get; set; }

    /// <summary>
    /// Name of the work center
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Production capacity per hour
    /// </summary>
    public decimal CapacityPerHour { get; set; }

    /// <summary>
    /// Hourly machine or labor cost
    /// </summary>
    public decimal? HourlyMachineCost { get; set; }

    // Navigation properties
    public ICollection<RoutingOperation>? RoutingOperations { get; set; }
    public ICollection<WorkCenterShift>? Shifts { get; set; }
    public ICollection<MachineDowntime>? Downtimes { get; set; }
    public ICollection<CapacityLoad>? CapacityLoads { get; set; }
    public ICollection<OverheadRule>? OverheadRules { get; set; }
}