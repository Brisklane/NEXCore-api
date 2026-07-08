using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// Rider work shift - tracks when a rider clocks in/out.
/// Used for attendance, capacity planning, and earnings calculation.
/// </summary>
public class RiderShift : BaseEntity
{
    public Guid RiderId { get; set; }
    public Rider Rider { get; set; } = null!;

    public DateTime ShiftStart { get; set; } = DateTime.UtcNow;
    public DateTime? ShiftEnd { get; set; }

    public double? StartLatitude { get; set; }
    public double? StartLongitude { get; set; }
    public double? EndLatitude { get; set; }
    public double? EndLongitude { get; set; }

    public int DeliveriesCompleted { get; set; }
    public double? TotalDistanceKm { get; set; }

    public string? Notes { get; set; }
}
