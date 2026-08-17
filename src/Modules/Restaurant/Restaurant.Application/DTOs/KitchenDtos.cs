using Restaurant.Domain.Enums;

namespace Restaurant.Application.DTOs;

// ── Stations ─────────────────────────────────────────────────────────────────

public class KitchenStationDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public string Name { get; set; } = string.Empty;
    public StationType StationType { get; set; }
    public int DisplayOrder { get; set; }
    public string? ColorHex { get; set; }
    public bool IsExpo { get; set; }
    public int SlaMinutes { get; set; }
    public int MaxConcurrentTickets { get; set; }
    public bool PrintsTickets { get; set; }
    public Guid? PrinterProfileId { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }

    /// <summary>Live load, so the stations list doubles as a kitchen overview.</summary>
    public int OpenTicketCount { get; set; }
    public int OverdueTicketCount { get; set; }
    public int AveragePrepSeconds { get; set; }

    public List<StationRoutingRuleDto> RoutingRules { get; set; } = [];
}

public class SaveKitchenStationDto
{
    public Guid OutletId { get; set; }
    public string Name { get; set; } = string.Empty;
    public StationType StationType { get; set; } = StationType.Grill;
    public int DisplayOrder { get; set; }
    public string? ColorHex { get; set; }
    public bool IsExpo { get; set; }
    public int SlaMinutes { get; set; } = 15;
    public int MaxConcurrentTickets { get; set; } = 12;
    public bool PrintsTickets { get; set; }
    public Guid? PrinterProfileId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

public class StationRoutingRuleDto
{
    public Guid Id { get; set; }
    public Guid StationId { get; set; }
    public string? StationName { get; set; }
    public Guid OutletId { get; set; }
    public RoutingMatchType MatchType { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public Guid? MenuItemId { get; set; }
    public string? MenuItemName { get; set; }
    public OrderType? OrderType { get; set; }
    public int Priority { get; set; }
    public bool IsAdditional { get; set; }
    public bool IsActive { get; set; }
}

// ── Tickets ──────────────────────────────────────────────────────────────────

public class KitchenTicketDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public Guid StationId { get; set; }
    public string? StationName { get; set; }
    public string? StationColorHex { get; set; }
    public Guid OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public string TicketNumber { get; set; } = string.Empty;

    public KitchenTicketStatus Status { get; set; }
    public CourseType Course { get; set; }
    public OrderType OrderType { get; set; }

    public string? TableNumber { get; set; }
    public string? WaiterName { get; set; }
    public int GuestCount { get; set; }

    public bool IsPriority { get; set; }
    public bool IsRemake { get; set; }

    public DateTime FiredAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? BumpedAt { get; set; }
    public int? PrepSeconds { get; set; }
    public int RecallCount { get; set; }
    public string? Note { get; set; }

    /// <summary>Seconds since the ticket was fired. What the KDS colours cards by.</summary>
    public int AgeSeconds { get; set; }

    /// <summary>ok · warning · overdue — computed server-side so every screen agrees.</summary>
    public string UrgencyLevel { get; set; } = "ok";

    public List<KitchenTicketLineDto> Lines { get; set; } = [];
}

public class KitchenTicketLineDto
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Guid OrderLineId { get; set; }
    public Guid MenuItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public decimal Quantity { get; set; }
    public string? ModifierSummary { get; set; }
    public string? SpecialInstructions { get; set; }
    public string? AllergenWarning { get; set; }
    public int? SeatNumber { get; set; }
    public OrderLineStatus Status { get; set; }
    public DateTime? ReadyAt { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>Everything one kitchen screen renders, in one poll.</summary>
public class KitchenDisplayDto
{
    public Guid OutletId { get; set; }
    public Guid? StationId { get; set; }
    public string? StationName { get; set; }
    public bool IsExpo { get; set; }
    public DateTime ServerTime { get; set; }

    public List<KitchenTicketDto> Tickets { get; set; } = [];
    public List<AllDayCountDto> AllDayCounts { get; set; } = [];

    public int NewCount { get; set; }
    public int InProgressCount { get; set; }
    public int ReadyCount { get; set; }
    public int OverdueCount { get; set; }
    public int AveragePrepSeconds { get; set; }
}

/// <summary>
/// "All day" totals — how many of each dish are outstanding across every open ticket. The number
/// a line cook plans a batch from, rather than reading twelve tickets.
/// </summary>
public class AllDayCountDto
{
    public Guid MenuItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public decimal OutstandingQuantity { get; set; }
    public int TicketCount { get; set; }
    public int OldestAgeSeconds { get; set; }
}

public class BumpTicketDto
{
    public Guid TicketId { get; set; }
    public Guid? StaffId { get; set; }

    /// <summary>Bump only this line. Null bumps the whole ticket.</summary>
    public Guid? TicketLineId { get; set; }
}

public class RecallTicketDto
{
    public Guid TicketId { get; set; }
    public Guid? StaffId { get; set; }
    public string? Reason { get; set; }
}

public class PrinterProfileDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Target { get; set; }
    public int PaperWidthMm { get; set; }
    public bool IsReceiptPrinter { get; set; }
    public bool IsKitchenPrinter { get; set; }
    public bool IsLabelPrinter { get; set; }
    public bool OpensCashDrawer { get; set; }
    public int CopiesPerTicket { get; set; }
    public string? HeaderText { get; set; }
    public string? FooterText { get; set; }
    public bool IsActive { get; set; }
}
