using Nexcore.SharedKernel;
using Restaurant.Domain.Enums;

namespace Restaurant.Domain.Entities;

/// <summary>
/// Someone who works the floor or the kitchen. Deliberately not the same record as an ERP user:
/// most waiters never sign into the ERP, they tap a PIN on a shared tablet. <see cref="UserId"/>
/// links the two for the minority who are both.
/// </summary>
public class RestaurantStaff : BaseEntity
{
    public Guid OutletId { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public StaffRole Role { get; set; } = StaffRole.Waiter;

    /// <summary>ERP user, when this person also signs into NexCore proper.</summary>
    public Guid? UserId { get; set; }

    /// <summary>HR employee, so hours worked reach payroll without re-keying.</summary>
    public Guid? EmployeeId { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? PhotoUrl { get; set; }

    /// <summary>BCrypt hash of the till PIN. The PIN itself is never stored or logged.</summary>
    public string? PinHash { get; set; }

    /// <summary>Locks the account after repeated bad PINs, so a shared tablet is not a free door.</summary>
    public int FailedPinAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }

    // ── What this person may do on the till ──────────────────────────────────
    public bool CanTakeOrders { get; set; } = true;
    public bool CanVoidLines { get; set; }
    public bool CanApplyDiscounts { get; set; }
    public bool CanApproveDiscounts { get; set; }
    public bool CanOpenCashDrawer { get; set; }
    public bool CanCloseSession { get; set; }
    public bool CanRunReports { get; set; }
    public bool CanEditMenu { get; set; }
    public bool CanManageTables { get; set; }
    public bool CanServeAlcohol { get; set; }

    /// <summary>Default section on the floor plan. Overridden per shift by the roster.</summary>
    public Guid? DefaultSectionId { get; set; }

    public decimal? HourlyRate { get; set; }

    /// <summary>Fixed share of the tip pool for roles that are paid out on a set percentage.</summary>
    public decimal? TipSharePercent { get; set; }

    public DateTime? HiredOn { get; set; }
    public string? Note { get; set; }
}

/// <summary>A rostered shift: who works where, when, and how it actually went.</summary>
public class StaffShift : BaseEntity
{
    public Guid OutletId { get; set; }
    public Guid StaffId { get; set; }
    public RestaurantStaff? Staff { get; set; }

    public DateTime ShiftDate { get; set; }
    public TimeSpan ScheduledStart { get; set; }
    public TimeSpan ScheduledEnd { get; set; }

    public ShiftStatus Status { get; set; } = ShiftStatus.Scheduled;

    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }

    /// <summary>Unpaid break minutes, subtracted from hours worked before tip and payroll maths.</summary>
    public int BreakMinutes { get; set; }
    public decimal HoursWorked { get; set; }

    /// <summary>Section assigned for this shift; overrides the staff member's default.</summary>
    public Guid? SectionId { get; set; }
    public StaffRole Role { get; set; } = StaffRole.Waiter;

    // Rolled up when the shift ends, so the performance report needs no aggregation.
    public decimal SalesAmount { get; set; }
    public int OrdersHandled { get; set; }
    public int CoversServed { get; set; }
    public decimal TipsEarned { get; set; }

    public string? Note { get; set; }
}

/// <summary>A single clock-in/clock-out pair. Several can belong to one shift (split shifts, breaks).</summary>
public class TimeClockEntry : BaseEntity
{
    public Guid OutletId { get; set; }
    public Guid StaffId { get; set; }
    public Guid? ShiftId { get; set; }

    public DateTime ClockedInAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClockedOutAt { get; set; }

    /// <summary>True for a break rather than working time, so the two never get added together.</summary>
    public bool IsBreak { get; set; }

    public decimal Hours { get; set; }
    public string? Note { get; set; }

    /// <summary>Set when a manager corrected a forgotten clock-out; the original stays visible.</summary>
    public bool IsAdjusted { get; set; }
    public Guid? AdjustedByStaffId { get; set; }
}

/// <summary>
/// A till session: one drawer, one cashier, one stretch of trading. Every payment is stamped
/// with the session, which is what makes the closing count attributable to a person.
/// </summary>
public class RestaurantSession : BaseEntity
{
    public Guid OutletId { get; set; }

    public string SessionNumber { get; set; } = string.Empty;
    public SessionStatus Status { get; set; } = SessionStatus.Open;

    public Guid? CashierId { get; set; }
    public string? CashierName { get; set; }

    /// <summary>Physical device this session runs on, so two drawers never share a session.</summary>
    public string? TerminalName { get; set; }

    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }

    public decimal OpeningFloat { get; set; }

    // ── Expected, computed from the transactions ─────────────────────────────
    public decimal ExpectedCash { get; set; }
    public decimal ExpectedCard { get; set; }
    public decimal ExpectedOther { get; set; }

    // ── Counted, entered at close ────────────────────────────────────────────
    public decimal CountedCash { get; set; }
    public decimal CountedCard { get; set; }
    public decimal CountedOther { get; set; }

    /// <summary>Counted minus expected. Negative is a shortage.</summary>
    public decimal CashVariance { get; set; }

    /// <summary>
    /// Blind close: the cashier counts without seeing the expected figure. Standard control —
    /// showing the target first makes a shortage easy to hide.
    /// </summary>
    public bool IsBlindClose { get; set; } = true;

    public decimal TotalSales { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal TotalVoids { get; set; }
    public decimal TotalRefunds { get; set; }
    public decimal TotalTips { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalServiceCharge { get; set; }
    public int OrderCount { get; set; }
    public int CoverCount { get; set; }
    public int CheckCount { get; set; }

    /// <summary>Number of mid-shift X-reads taken. Z-read closes the session and can only run once.</summary>
    public int XReadCount { get; set; }
    public DateTime? ZReadAt { get; set; }

    public string? ClosingNote { get; set; }

    public ICollection<SessionCashMovement> CashMovements { get; set; } = [];
}

/// <summary>Any cash in or out of the drawer that is not a sale — floats, drops, payouts, counts.</summary>
public class SessionCashMovement : BaseEntity
{
    public Guid SessionId { get; set; }
    public RestaurantSession? Session { get; set; }

    public Guid OutletId { get; set; }

    public CashMovementType MovementType { get; set; }

    /// <summary>Signed: positive adds to the drawer, negative takes out.</summary>
    public decimal Amount { get; set; }

    public string? Reason { get; set; }
    public string? Reference { get; set; }
    public Guid? StaffId { get; set; }
    public string? StaffName { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>Set when the movement needed a supervisor to authorise it.</summary>
    public Guid? ApprovedByStaffId { get; set; }
}
