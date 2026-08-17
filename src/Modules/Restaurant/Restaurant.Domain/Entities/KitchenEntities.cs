using Nexcore.SharedKernel;
using Restaurant.Domain.Enums;

namespace Restaurant.Domain.Entities;

/// <summary>
/// A place in the kitchen that cooks. One order becomes one ticket per station, which is what
/// lets the grill and the bar work the same order in parallel without either seeing the other's
/// lines.
/// </summary>
public class KitchenStation : BaseEntity
{
    public Guid OutletId { get; set; }
    public RestaurantOutlet? Outlet { get; set; }

    public string Name { get; set; } = string.Empty;
    public StationType StationType { get; set; } = StationType.Grill;
    public int DisplayOrder { get; set; }
    public string? ColorHex { get; set; }

    /// <summary>
    /// The pass. An expo station sees every other station's lines for a table so plates go out
    /// together; exactly one station per outlet should carry this.
    /// </summary>
    public bool IsExpo { get; set; }

    /// <summary>Tickets older than this turn red on the display.</summary>
    public int SlaMinutes { get; set; } = 15;

    /// <summary>Tickets in progress before the station is considered overloaded.</summary>
    public int MaxConcurrentTickets { get; set; } = 12;

    /// <summary>Print a paper KOT as well as showing the ticket on screen.</summary>
    public bool PrintsTickets { get; set; }
    public Guid? PrinterProfileId { get; set; }

    public ICollection<StationRoutingRule> RoutingRules { get; set; } = [];
}

/// <summary>
/// Decides which station cooks a line. Rules are evaluated most-specific first — item, then
/// category, then order type, then the catch-all — so a single "everything to the grill" rule
/// can sit underneath a handful of precise ones without fighting them.
/// </summary>
public class StationRoutingRule : BaseEntity
{
    public Guid StationId { get; set; }
    public KitchenStation? Station { get; set; }

    public Guid OutletId { get; set; }

    public RoutingMatchType MatchType { get; set; } = RoutingMatchType.AllItems;
    public Guid? CategoryId { get; set; }
    public Guid? MenuItemId { get; set; }
    public OrderType? OrderType { get; set; }

    /// <summary>Lower runs first. Ties break on match specificity.</summary>
    public int Priority { get; set; }

    /// <summary>Route a copy here as well as to the primary station (drinks to bar and expo).</summary>
    public bool IsAdditional { get; set; }
}

/// <summary>
/// A KOT: the slice of an order that one station is responsible for. Created when a course is
/// fired, never before — a held course must not reach the kitchen.
/// </summary>
public class KitchenTicket : BaseEntity
{
    public Guid OutletId { get; set; }
    public Guid StationId { get; set; }
    public KitchenStation? Station { get; set; }

    public Guid OrderId { get; set; }
    public RestaurantOrder? Order { get; set; }

    /// <summary>Sequence within the service day — what staff shout across the pass.</summary>
    public string TicketNumber { get; set; } = string.Empty;

    public KitchenTicketStatus Status { get; set; } = KitchenTicketStatus.New;
    public CourseType Course { get; set; } = CourseType.Main;
    public OrderType OrderType { get; set; } = Enums.OrderType.DineIn;

    // Denormalised so the KDS renders without joining — it refreshes every few seconds.
    public string? TableNumber { get; set; }
    public string? WaiterName { get; set; }
    public int GuestCount { get; set; }

    /// <summary>Jump the queue: allergy remakes and comped remakes go first.</summary>
    public bool IsPriority { get; set; }
    public bool IsRemake { get; set; }

    public DateTime FiredAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? BumpedAt { get; set; }

    /// <summary>Wall-clock seconds from fire to bump — the kitchen's headline metric.</summary>
    public int? PrepSeconds { get; set; }

    public int RecallCount { get; set; }
    public string? Note { get; set; }

    public ICollection<KitchenTicketLine> Lines { get; set; } = [];
}

/// <summary>One dish on a ticket. Bumpable on its own so a station can clear as it plates.</summary>
public class KitchenTicketLine : BaseEntity
{
    public Guid TicketId { get; set; }
    public KitchenTicket? Ticket { get; set; }

    public Guid OrderLineId { get; set; }
    public Guid MenuItemId { get; set; }

    public string ItemName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public decimal Quantity { get; set; } = 1;

    /// <summary>Chosen modifiers rendered for the cook: "no onion, extra cheese".</summary>
    public string? ModifierSummary { get; set; }

    public string? SpecialInstructions { get; set; }
    public string? AllergenWarning { get; set; }
    public int? SeatNumber { get; set; }

    public OrderLineStatus Status { get; set; } = OrderLineStatus.Fired;
    public DateTime? ReadyAt { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Where a document physically prints. Held as data rather than client config so a replaced
/// till picks up the same routing without being set up again.
/// </summary>
public class PrinterProfile : BaseEntity
{
    public Guid OutletId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Network address or OS printer name the client agent resolves.</summary>
    public string? Target { get; set; }

    /// <summary>58 or 80 (mm).</summary>
    public int PaperWidthMm { get; set; } = 80;

    public bool IsReceiptPrinter { get; set; }
    public bool IsKitchenPrinter { get; set; }
    public bool IsLabelPrinter { get; set; }
    public bool OpensCashDrawer { get; set; }

    public int CopiesPerTicket { get; set; } = 1;
    public string? HeaderText { get; set; }
    public string? FooterText { get; set; }
}
