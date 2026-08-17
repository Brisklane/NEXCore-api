using Nexcore.SharedKernel;
using Restaurant.Domain.Enums;

namespace Restaurant.Domain.Entities;

/// <summary>
/// A venue. Everything operational hangs off an outlet: floors, stations, sessions, orders.
///
/// An outlet is deliberately not the same record as a POS store. A company can run a retail
/// shop and a restaurant from the same premises with different staff, menus and kitchens;
/// <see cref="PosStoreId"/> links the two when they really are one place, so reporting can
/// roll up, without forcing them to be one row.
/// </summary>
public class RestaurantOutlet : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ServiceStyle ServiceStyle { get; set; } = ServiceStyle.CasualDining;
    public string? CuisineType { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? CountryCode { get; set; }
    public string? TimeZoneId { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>Warehouse stock is depleted from when a check closes. Null disables depletion.</summary>
    public Guid? WarehouseId { get; set; }

    /// <summary>Optional link to a Sales POS store when the venue is also a retail till.</summary>
    public Guid? PosStoreId { get; set; }

    public Guid? DefaultMenuId { get; set; }
    public Guid? DefaultTaxGroupId { get; set; }
    public Guid? ServiceChargeRuleId { get; set; }

    /// <summary>Rate applied to any dish that does not set its own. Percentage, not fraction.</summary>
    public decimal DefaultTaxPercent { get; set; }

    /// <summary>
    /// Separate rate for takeaway and delivery. Many jurisdictions tax eat-in and take-out
    /// differently, and getting that wrong is a filing problem, not a rounding one.
    /// Zero falls back to <see cref="DefaultTaxPercent"/>.
    /// </summary>
    public decimal TakeawayTaxPercent { get; set; }

    /// <summary>Total seats across every table; recomputed when the layout changes.</summary>
    public int SeatingCapacity { get; set; }

    /// <summary>Minutes a table is expected to be occupied — the reservation engine books against this.</summary>
    public int AverageDiningMinutes { get; set; } = 60;

    public bool AcceptsReservations { get; set; } = true;
    public bool AcceptsDelivery { get; set; }
    public bool AcceptsTakeaway { get; set; } = true;
    public bool HasDriveThru { get; set; }
    public bool QrOrderingEnabled { get; set; }

    /// <summary>Temporarily shut (renovation, holiday) without deactivating the record.</summary>
    public bool IsTemporarilyClosed { get; set; }
    public string? ClosureNote { get; set; }

    public string? LogoUrl { get; set; }
    public string? ReceiptFooter { get; set; }

    public ICollection<OutletSchedule> Schedules { get; set; } = [];
    public ICollection<Floor> Floors { get; set; } = [];
    public ICollection<KitchenStation> Stations { get; set; } = [];
}

/// <summary>Trading hours for one weekday, or a dated override (holiday, private event).</summary>
public class OutletSchedule : BaseEntity
{
    public Guid OutletId { get; set; }
    public RestaurantOutlet? Outlet { get; set; }

    /// <summary>0 = Sunday … 6 = Saturday. Ignored when <see cref="OverrideDate"/> is set.</summary>
    public int DayOfWeek { get; set; }

    /// <summary>Set for a one-off override that wins over the weekday row.</summary>
    public DateTime? OverrideDate { get; set; }

    public TimeSpan OpensAt { get; set; }
    public TimeSpan ClosesAt { get; set; }

    /// <summary>True when the venue does not open at all on this day/date.</summary>
    public bool IsClosed { get; set; }

    public string? Note { get; set; }
}

/// <summary>A physical level or area of the venue that carries its own plan.</summary>
public class Floor : BaseEntity
{
    public Guid OutletId { get; set; }
    public RestaurantOutlet? Outlet { get; set; }

    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    /// <summary>Plan canvas size in grid units; tables are positioned inside it.</summary>
    public int CanvasWidth { get; set; } = 1200;
    public int CanvasHeight { get; set; } = 800;

    /// <summary>Optional traced blueprint shown behind the tables.</summary>
    public string? BackgroundImageUrl { get; set; }

    public ICollection<TableSection> Sections { get; set; } = [];
    public ICollection<DiningTable> Tables { get; set; } = [];
    public ICollection<FloorFixture> Fixtures { get; set; } = [];
}

/// <summary>
/// A serving zone inside a floor (Bar, Patio, Family Hall, VIP). Sections are what waiters are
/// assigned to, so they are the unit of workload — not the floor and not the individual table.
/// </summary>
public class TableSection : BaseEntity
{
    public Guid FloorId { get; set; }
    public Floor? Floor { get; set; }

    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    /// <summary>Hex colour used to tint this section's tables on the plan.</summary>
    public string? ColorHex { get; set; }

    public bool IsSmoking { get; set; }
    public bool IsOutdoor { get; set; }
    public bool IsPrivate { get; set; }

    /// <summary>Minimum spend to hold this section, for VIP rooms and cabanas.</summary>
    public decimal? MinimumSpend { get; set; }

    public ICollection<DiningTable> Tables { get; set; } = [];
}

/// <summary>A table on the floor plan, with both its geometry and its live service state.</summary>
public class DiningTable : BaseEntity
{
    public Guid OutletId { get; set; }
    public Guid FloorId { get; set; }
    public Floor? Floor { get; set; }
    public Guid? SectionId { get; set; }
    public TableSection? Section { get; set; }

    /// <summary>What staff call it: "12", "A4", "Bar-3".</summary>
    public string TableNumber { get; set; } = string.Empty;

    public TableShape Shape { get; set; } = TableShape.Square;
    public int Seats { get; set; } = 4;
    public int? MinPartySize { get; set; }
    public int? MaxPartySize { get; set; }

    // Geometry on the floor canvas.
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int Width { get; set; } = 80;
    public int Height { get; set; } = 80;
    public int Rotation { get; set; }

    // ── Live state ───────────────────────────────────────────────────────────
    public TableState State { get; set; } = TableState.Free;
    public Guid? CurrentOrderId { get; set; }
    public Guid? AssignedWaiterId { get; set; }
    public int CurrentGuestCount { get; set; }
    public DateTime? SeatedAt { get; set; }
    public DateTime? StateChangedAt { get; set; }

    /// <summary>
    /// Set on every table of a merged party, to the id of the primary table. One check covers
    /// the group; the tables keep their own rows so the plan still shows the real room.
    /// </summary>
    public Guid? MergedIntoTableId { get; set; }

    /// <summary>Token printed on the table's QR sticker for self-ordering.</summary>
    public string? QrToken { get; set; }

    public string? Note { get; set; }
}

/// <summary>Non-table furniture and structure, so the plan reads like the actual room.</summary>
public class FloorFixture : BaseEntity
{
    public Guid FloorId { get; set; }
    public Floor? Floor { get; set; }

    public FloorFixtureKind Kind { get; set; }
    public string? Label { get; set; }

    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int Width { get; set; } = 60;
    public int Height { get; set; } = 20;
    public int Rotation { get; set; }
    public string? ColorHex { get; set; }
}

/// <summary>
/// One transition of a table's state. Written on every change so table-turn analysis is a query
/// over facts rather than a reconstruction from order timestamps.
/// </summary>
public class TableStateLog : BaseEntity
{
    public Guid TableId { get; set; }
    public DiningTable? Table { get; set; }

    public Guid OutletId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? WaiterId { get; set; }

    public TableState FromState { get; set; }
    public TableState ToState { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>Seconds the table spent in <see cref="FromState"/>.</summary>
    public int? SecondsInPreviousState { get; set; }

    public int GuestCount { get; set; }
    public string? Note { get; set; }
}
