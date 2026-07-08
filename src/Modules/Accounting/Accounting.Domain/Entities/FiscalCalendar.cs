using Nexcore.SharedKernel;

namespace Accounting.Domain.Entities;

/// <summary>
/// Fiscal calendar - defines fiscal periods for the company
/// </summary>
public class FiscalCalendar : BaseEntity
{
    /// <summary>
    /// Calendar name (e.g., "FY 2024")
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Fiscal year start date
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Fiscal year end date
    /// </summary>
    public DateTime EndDate { get; set; }

    // Open/closed state is represented by the inherited IsActive flag:
    // IsActive == true  => open (editable)
    // IsActive == false => closed (read-only)

    // Navigation properties
    public ICollection<FiscalPeriod> Periods { get; set; } = [];
}
