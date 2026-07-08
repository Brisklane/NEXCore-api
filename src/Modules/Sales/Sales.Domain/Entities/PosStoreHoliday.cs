using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// A date on which a POS store is closed or has different operating hours.
/// Overrides the regular weekly schedule.
/// </summary>
public class PosStoreHoliday : BaseEntity
{
    public Guid PosStoreId { get; set; }
    public PosStore PosStore { get; set; } = null!;

    public DateTime Date { get; set; }
    /// <summary>If false the store is fully closed on this date.</summary>
    public bool IsPartiallyOpen { get; set; }

    /// <summary>Only used when IsPartiallyOpen = true.</summary>
    public string? OpeningTime { get; set; }
    public string? ClosingTime { get; set; }
}
