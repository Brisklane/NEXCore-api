using Restaurant.Domain.Enums;

namespace Restaurant.Application.DTOs;

// ── Outlet ───────────────────────────────────────────────────────────────────

public class OutletDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public ServiceStyle ServiceStyle { get; set; }
    public string? CuisineType { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? CountryCode { get; set; }
    public string? TimeZoneId { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public Guid? WarehouseId { get; set; }
    public Guid? PosStoreId { get; set; }
    public Guid? DefaultMenuId { get; set; }
    public Guid? DefaultTaxGroupId { get; set; }
    public decimal DefaultTaxPercent { get; set; }
    public decimal TakeawayTaxPercent { get; set; }
    public Guid? ServiceChargeRuleId { get; set; }
    public int SeatingCapacity { get; set; }
    public int AverageDiningMinutes { get; set; }
    public bool AcceptsReservations { get; set; }
    public bool AcceptsDelivery { get; set; }
    public bool AcceptsTakeaway { get; set; }
    public bool HasDriveThru { get; set; }
    public bool QrOrderingEnabled { get; set; }
    public bool IsTemporarilyClosed { get; set; }
    public string? ClosureNote { get; set; }
    public string? LogoUrl { get; set; }
    public string? ReceiptFooter { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }

    public List<OutletScheduleDto> Schedules { get; set; } = [];

    // Live roll-ups, filled by the list endpoint so the picker can show status at a glance.
    public int TableCount { get; set; }
    public int OpenOrderCount { get; set; }
    public int OccupiedTableCount { get; set; }
}

public class SaveOutletDto
{
    public string? Code { get; set; }
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
    public Guid? WarehouseId { get; set; }
    public Guid? PosStoreId { get; set; }
    public Guid? DefaultMenuId { get; set; }
    public Guid? DefaultTaxGroupId { get; set; }
    public decimal DefaultTaxPercent { get; set; }
    public decimal TakeawayTaxPercent { get; set; }
    public Guid? ServiceChargeRuleId { get; set; }
    public int AverageDiningMinutes { get; set; } = 60;
    public bool AcceptsReservations { get; set; } = true;
    public bool AcceptsDelivery { get; set; }
    public bool AcceptsTakeaway { get; set; } = true;
    public bool HasDriveThru { get; set; }
    public bool QrOrderingEnabled { get; set; }
    public bool IsTemporarilyClosed { get; set; }
    public string? ClosureNote { get; set; }
    public string? LogoUrl { get; set; }
    public string? ReceiptFooter { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

public class OutletScheduleDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public int DayOfWeek { get; set; }
    public DateTime? OverrideDate { get; set; }
    public TimeSpan OpensAt { get; set; }
    public TimeSpan ClosesAt { get; set; }
    public bool IsClosed { get; set; }
    public string? Note { get; set; }
}

// ── Layout ───────────────────────────────────────────────────────────────────

public class FloorDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int CanvasWidth { get; set; }
    public int CanvasHeight { get; set; }
    public string? BackgroundImageUrl { get; set; }
    public bool IsActive { get; set; }

    public List<SectionDto> Sections { get; set; } = [];
    public List<TableDto> Tables { get; set; } = [];
    public List<FixtureDto> Fixtures { get; set; } = [];
}

public class SaveFloorDto
{
    public Guid OutletId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int CanvasWidth { get; set; } = 1200;
    public int CanvasHeight { get; set; } = 800;
    public string? BackgroundImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SectionDto
{
    public Guid Id { get; set; }
    public Guid FloorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string? ColorHex { get; set; }
    public bool IsSmoking { get; set; }
    public bool IsOutdoor { get; set; }
    public bool IsPrivate { get; set; }
    public decimal? MinimumSpend { get; set; }
    public bool IsActive { get; set; }
    public int TableCount { get; set; }
    public int SeatCount { get; set; }
}

public class SaveSectionDto
{
    public Guid FloorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string? ColorHex { get; set; }
    public bool IsSmoking { get; set; }
    public bool IsOutdoor { get; set; }
    public bool IsPrivate { get; set; }
    public decimal? MinimumSpend { get; set; }
    public bool IsActive { get; set; } = true;
}

public class TableDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public Guid FloorId { get; set; }
    public Guid? SectionId { get; set; }
    public string? SectionName { get; set; }
    public string? SectionColorHex { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public TableShape Shape { get; set; }
    public int Seats { get; set; }
    public int? MinPartySize { get; set; }
    public int? MaxPartySize { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Rotation { get; set; }

    public TableState State { get; set; }
    public Guid? CurrentOrderId { get; set; }
    public string? CurrentOrderNumber { get; set; }
    public Guid? AssignedWaiterId { get; set; }
    public string? AssignedWaiterName { get; set; }
    public int CurrentGuestCount { get; set; }
    public DateTime? SeatedAt { get; set; }
    public DateTime? StateChangedAt { get; set; }
    public Guid? MergedIntoTableId { get; set; }
    public string? QrToken { get; set; }
    public string? Note { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Running total of the open order, so the plan can show spend without a second call.</summary>
    public decimal CurrentOrderTotal { get; set; }

    /// <summary>Minutes since the table entered its current state.</summary>
    public int MinutesInState { get; set; }

    /// <summary>
    /// True when the table has been in its state longer than the settings allow — a seated
    /// table with no order, or a served table nobody has offered the bill to.
    /// </summary>
    public bool NeedsAttention { get; set; }

    /// <summary>Next reservation on this table, so the host does not over-seat it.</summary>
    public DateTime? NextReservationAt { get; set; }
}

public class SaveTableDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public Guid FloorId { get; set; }
    public Guid? SectionId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public TableShape Shape { get; set; } = TableShape.Square;
    public int Seats { get; set; } = 4;
    public int? MinPartySize { get; set; }
    public int? MaxPartySize { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int Width { get; set; } = 80;
    public int Height { get; set; } = 80;
    public int Rotation { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }
}

public class FixtureDto
{
    public Guid Id { get; set; }
    public Guid FloorId { get; set; }
    public FloorFixtureKind Kind { get; set; }
    public string? Label { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Rotation { get; set; }
    public string? ColorHex { get; set; }
}

/// <summary>
/// The whole plan in one round trip. The designer drags dozens of objects at once and saving
/// each one individually would be both slow and non-atomic — a half-saved layout is nonsense.
/// </summary>
public class SaveLayoutDto
{
    public Guid FloorId { get; set; }
    public int CanvasWidth { get; set; }
    public int CanvasHeight { get; set; }
    public string? BackgroundImageUrl { get; set; }
    public List<SaveTableDto> Tables { get; set; } = [];
    public List<FixtureDto> Fixtures { get; set; } = [];

    /// <summary>Ids removed in the designer. Soft-deleted, and refused if the table is in use.</summary>
    public List<Guid> DeletedTableIds { get; set; } = [];
    public List<Guid> DeletedFixtureIds { get; set; } = [];
}

/// <summary>Everything the live floor screen renders, for one outlet, in one call.</summary>
public class FloorPlanViewDto
{
    public Guid OutletId { get; set; }
    public string OutletName { get; set; } = string.Empty;
    public List<FloorDto> Floors { get; set; } = [];

    public int TotalTables { get; set; }
    public int FreeTables { get; set; }
    public int OccupiedTables { get; set; }
    public int ReservedTables { get; set; }
    public int NeedsCleaning { get; set; }
    public int TotalSeats { get; set; }
    public int SeatedGuests { get; set; }
    public decimal OpenOrderValue { get; set; }
    public int AttentionCount { get; set; }
}

// ── Table operations ─────────────────────────────────────────────────────────

public class SeatGuestsDto
{
    public Guid TableId { get; set; }
    public int GuestCount { get; set; } = 2;
    public Guid? WaiterId { get; set; }
    public Guid? ReservationId { get; set; }
    public Guid? WaitlistEntryId { get; set; }
    public Guid? GuestProfileId { get; set; }

    /// <summary>Open the order immediately as well as seating, which is what waiters expect.</summary>
    public bool CreateOrder { get; set; } = true;
}

public class TransferTableDto
{
    public Guid FromTableId { get; set; }
    public Guid ToTableId { get; set; }
    public string? Reason { get; set; }
}

public class MergeTablesDto
{
    public Guid PrimaryTableId { get; set; }
    public List<Guid> TableIds { get; set; } = [];
    public int? GuestCount { get; set; }
}

public class ChangeTableStateDto
{
    public Guid TableId { get; set; }
    public TableState State { get; set; }
    public string? Note { get; set; }
}

public class AssignWaiterDto
{
    public Guid TableId { get; set; }
    public Guid WaiterId { get; set; }
}
