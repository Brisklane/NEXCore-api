using Nexcore.SharedKernel;

namespace Accounting.Domain.Entities;

/// <summary>
/// Fiscal period - monthly or quarterly periods within a fiscal year
/// </summary>
public class FiscalPeriod : BaseEntity
{
    /// <summary>
    /// Fiscal calendar reference
    /// </summary>
    public Guid FiscalCalendarId { get; set; }

    /// <summary>
    /// Period name (e.g., "Jan-2024", "Q1-2024")
    /// </summary>
    public required string PeriodName { get; set; }

    /// <summary>
    /// Period start date
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Period end date
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Indicates if period is closed
    /// </summary>
    public bool IsClosed { get; set; }

    /// <summary>
    /// User who closed the period
    /// </summary>
    public Guid? ClosedByUserId { get; set; }

    /// <summary>
    /// When period was closed
    /// </summary>
    public DateTime? ClosedAt { get; set; }

    // Navigation properties
    public FiscalCalendar? FiscalCalendar { get; set; }
}
