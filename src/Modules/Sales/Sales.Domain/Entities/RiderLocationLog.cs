using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// GPS location breadcrumb logged during a rider's active assignment.
/// Powers the live order tracking map in the customer app.
/// </summary>
public class RiderLocationLog : BaseEntity
{
    public Guid RiderAssignmentId { get; set; }
    public RiderAssignment RiderAssignment { get; set; } = null!;

    public Guid RiderId { get; set; }
    public Rider Rider { get; set; } = null!;

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>Speed in km/h at time of log.</summary>
    public double? SpeedKmh { get; set; }

    /// <summary>Compass bearing in degrees (0–360).</summary>
    public double? Bearing { get; set; }

    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
}
