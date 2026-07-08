using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// Operating hours for a specific day of the week for a POS store.
/// One record per day. A missing record means the store is closed that day.
/// </summary>
public class PosStoreSchedule : BaseEntity
{
    public Guid PosStoreId { get; set; }
    public PosStore PosStore { get; set; } = null!;

    /// <summary>Uses <see cref="System.DayOfWeek"/> — Sunday=0 … Saturday=6.</summary>
    public System.DayOfWeek DayOfWeek { get; set; }

    /// <summary>Opening time in "HH:mm" 24-hour format (e.g., "09:00").</summary>
    public string OpeningTime { get; set; } = "09:00";

    /// <summary>Closing time in "HH:mm" 24-hour format (e.g., "22:00").</summary>
    public string ClosingTime { get; set; } = "22:00";

    public bool IsClosed { get; set; }
}
