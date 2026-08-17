using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Application.Services.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

/// <summary>
/// The floor: the plan, the table state machine, and moving parties around the room.
///
/// Table state is only ever changed through <see cref="ApplyStateAsync"/>. Every transition
/// writes a <see cref="TableStateLog"/> row with how long the table sat in the previous state,
/// which is what makes turn-time reporting a straight query instead of an archaeology exercise
/// over order timestamps.
/// </summary>
public class FloorService(
    RestaurantDbContext db,
    IRestaurantTenant tenant,
    IRestaurantOrderService orders) : IFloorService
{
    public async Task<FloorPlanViewDto> GetFloorPlanAsync(Guid outletId)
    {
        var now = DateTime.UtcNow;
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        var seatedLimit = settings?.SeatedAttentionMinutes ?? 10;
        var servedLimit = settings?.ServedAttentionMinutes ?? 20;

        var outlet = await db.Outlets.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == outletId);

        var floors = await db.Floors.ForTenant(tenant)
            .Where(f => f.OutletId == outletId && f.IsActive)
            .OrderBy(f => f.DisplayOrder).ThenBy(f => f.Name)
            .ToListAsync();

        var floorIds = floors.Select(f => f.Id).ToList();

        var sections = await db.Sections.ForTenant(tenant)
            .Where(s => floorIds.Contains(s.FloorId))
            .OrderBy(s => s.DisplayOrder).ToListAsync();

        var tables = await db.Tables.ForTenant(tenant)
            .Where(t => t.OutletId == outletId && t.IsActive)
            .OrderBy(t => t.TableNumber).ToListAsync();

        var fixtures = await db.Fixtures.ForTenant(tenant)
            .Where(f => floorIds.Contains(f.FloorId)).ToListAsync();

        // One query for every open order rather than one per table: a busy floor is 60 tables and
        // the plan refreshes every few seconds.
        var openOrderIds = tables.Where(t => t.CurrentOrderId.HasValue).Select(t => t.CurrentOrderId!.Value).ToList();
        var openOrders = await db.Orders.ForTenant(tenant)
            .Where(o => openOrderIds.Contains(o.Id))
            .Select(o => new { o.Id, o.OrderNumber, o.TotalAmount, o.SubTotal })
            .ToListAsync();

        var waiterNames = await db.Staff.ForTenant(tenant)
            .Where(s => s.OutletId == outletId)
            .ToDictionaryAsync(s => s.Id, s => s.DisplayName ?? s.FullName);

        var upcoming = await db.Reservations.ForTenant(tenant)
            .Where(r => r.OutletId == outletId
                     && r.TableId != null
                     && (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Requested)
                     && r.ReservedFor >= now && r.ReservedFor <= now.AddHours(4))
            .Select(r => new { TableId = r.TableId!.Value, r.ReservedFor })
            .ToListAsync();

        var sectionById = sections.ToDictionary(s => s.Id);

        var tableDtos = tables.Select(t =>
        {
            var dto = RestaurantMapper.ToDto(t, now);

            if (t.SectionId.HasValue && sectionById.TryGetValue(t.SectionId.Value, out var section))
            {
                dto.SectionName = section.Name;
                dto.SectionColorHex = section.ColorHex;
            }

            if (t.AssignedWaiterId.HasValue)
                dto.AssignedWaiterName = waiterNames.GetValueOrDefault(t.AssignedWaiterId.Value);

            if (t.CurrentOrderId.HasValue)
            {
                var order = openOrders.FirstOrDefault(o => o.Id == t.CurrentOrderId);
                dto.CurrentOrderNumber = order?.OrderNumber;
                // Before a check is raised the order total is still zero, so fall back to the
                // running subtotal — the floor needs to show spend, not a row of zeroes.
                dto.CurrentOrderTotal = order is null ? 0m : order.TotalAmount > 0 ? order.TotalAmount : order.SubTotal;
            }

            dto.NextReservationAt = upcoming.Where(r => r.TableId == t.Id)
                .Select(r => (DateTime?)r.ReservedFor).Min();

            dto.NeedsAttention = t.State switch
            {
                TableState.Seated => dto.MinutesInState >= seatedLimit,
                TableState.Served => dto.MinutesInState >= servedLimit,
                TableState.NeedsCleaning => dto.MinutesInState >= 10,
                _ => false,
            };

            return dto;
        }).ToList();

        var sectionDtos = sections.Select(s =>
        {
            var dto = RestaurantMapper.ToDto(s);
            var own = tables.Where(t => t.SectionId == s.Id).ToList();
            dto.TableCount = own.Count;
            dto.SeatCount = own.Sum(t => t.Seats);
            return dto;
        }).ToList();

        var floorDtos = floors.Select(f => new FloorDto
        {
            Id = f.Id,
            OutletId = f.OutletId,
            Name = f.Name,
            DisplayOrder = f.DisplayOrder,
            CanvasWidth = f.CanvasWidth,
            CanvasHeight = f.CanvasHeight,
            BackgroundImageUrl = f.BackgroundImageUrl,
            IsActive = f.IsActive,
            Sections = sectionDtos.Where(s => s.FloorId == f.Id).ToList(),
            Tables = tableDtos.Where(t => t.FloorId == f.Id).ToList(),
            Fixtures = fixtures.Where(x => x.FloorId == f.Id).Select(RestaurantMapper.ToDto).ToList(),
        }).ToList();

        var occupiedStates = new[] { TableState.Seated, TableState.Ordered, TableState.Served, TableState.BillPrinted };

        return new FloorPlanViewDto
        {
            OutletId = outletId,
            OutletName = outlet?.Name ?? string.Empty,
            Floors = floorDtos,
            TotalTables = tableDtos.Count,
            FreeTables = tableDtos.Count(t => t.State == TableState.Free),
            OccupiedTables = tableDtos.Count(t => occupiedStates.Contains(t.State)),
            ReservedTables = tableDtos.Count(t => t.State == TableState.Reserved),
            NeedsCleaning = tableDtos.Count(t => t.State == TableState.NeedsCleaning),
            TotalSeats = tableDtos.Sum(t => t.Seats),
            SeatedGuests = tableDtos.Sum(t => t.CurrentGuestCount),
            OpenOrderValue = tableDtos.Sum(t => t.CurrentOrderTotal),
            AttentionCount = tableDtos.Count(t => t.NeedsAttention),
        };
    }

    public async Task<List<TableDto>> GetTablesAsync(Guid outletId, Guid? floorId = null)
    {
        var now = DateTime.UtcNow;
        var tables = await db.Tables.ForTenant(tenant)
            .Where(t => t.OutletId == outletId)
            .WhereIf(floorId.HasValue, t => t.FloorId == floorId)
            .OrderBy(t => t.TableNumber)
            .ToListAsync();

        return tables.Select(t => RestaurantMapper.ToDto(t, now)).ToList();
    }

    public async Task<TableDto?> GetTableAsync(Guid tableId)
    {
        var table = await db.Tables.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == tableId);
        return table is null ? null : RestaurantMapper.ToDto(table, DateTime.UtcNow);
    }

    // ── Designer ─────────────────────────────────────────────────────────────

    public async Task<FloorDto> SaveLayoutAsync(SaveLayoutDto request, Guid userId)
    {
        var floor = await db.Floors.ForTenant(tenant).FirstOrDefaultAsync(f => f.Id == request.FloorId)
            ?? throw new InvalidOperationException("Floor not found.");

        floor.CanvasWidth = request.CanvasWidth > 0 ? request.CanvasWidth : floor.CanvasWidth;
        floor.CanvasHeight = request.CanvasHeight > 0 ? request.CanvasHeight : floor.CanvasHeight;
        if (request.BackgroundImageUrl is not null) floor.BackgroundImageUrl = request.BackgroundImageUrl;
        floor.StampUpdated(userId);

        var existingTables = await db.Tables.ForTenant(tenant)
            .Where(t => t.FloorId == floor.Id).ToListAsync();

        // A table with guests on it cannot be deleted mid-service. Refusing loudly is right:
        // silently keeping it would leave the designer showing a layout the floor does not have.
        var busy = existingTables
            .Where(t => request.DeletedTableIds.Contains(t.Id) && t.State != TableState.Free && t.State != TableState.Blocked)
            .Select(t => t.TableNumber)
            .ToList();

        if (busy.Count > 0)
            throw new InvalidOperationException(
                $"Cannot remove table(s) {string.Join(", ", busy)} — they are in service. Clear them first.");

        foreach (var table in existingTables.Where(t => request.DeletedTableIds.Contains(t.Id)))
            table.StampDeleted(userId);

        foreach (var dto in request.Tables)
        {
            var table = dto.Id != Guid.Empty ? existingTables.FirstOrDefault(t => t.Id == dto.Id) : null;

            if (table is null)
            {
                table = new DiningTable
                {
                    Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
                    OutletId = floor.OutletId,
                    FloorId = floor.Id,
                    State = TableState.Free,
                    StateChangedAt = DateTime.UtcNow,
                    QrToken = Guid.NewGuid().ToString("N")[..12],
                }.StampNew(tenant, userId);
                db.Tables.Add(table);
            }
            else table.StampUpdated(userId);

            table.SectionId = dto.SectionId;
            table.TableNumber = dto.TableNumber;
            table.Shape = dto.Shape;
            table.Seats = dto.Seats;
            table.MinPartySize = dto.MinPartySize;
            table.MaxPartySize = dto.MaxPartySize;
            table.PositionX = dto.PositionX;
            table.PositionY = dto.PositionY;
            table.Width = dto.Width;
            table.Height = dto.Height;
            table.Rotation = dto.Rotation;
            table.IsActive = dto.IsActive;
            table.Note = dto.Note;
        }

        var existingFixtures = await db.Fixtures.ForTenant(tenant)
            .Where(f => f.FloorId == floor.Id).ToListAsync();

        foreach (var fixture in existingFixtures.Where(f => request.DeletedFixtureIds.Contains(f.Id)))
            fixture.StampDeleted(userId);

        foreach (var dto in request.Fixtures)
        {
            var fixture = dto.Id != Guid.Empty ? existingFixtures.FirstOrDefault(f => f.Id == dto.Id) : null;

            if (fixture is null)
            {
                fixture = new FloorFixture
                {
                    Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
                    FloorId = floor.Id,
                }.StampNew(tenant, userId);
                db.Fixtures.Add(fixture);
            }
            else fixture.StampUpdated(userId);

            fixture.Kind = dto.Kind;
            fixture.Label = dto.Label;
            fixture.PositionX = dto.PositionX;
            fixture.PositionY = dto.PositionY;
            fixture.Width = dto.Width;
            fixture.Height = dto.Height;
            fixture.Rotation = dto.Rotation;
            fixture.ColorHex = dto.ColorHex;
        }

        await db.SaveChangesAsync();
        await RefreshSeatingCapacityAsync(floor.OutletId, userId);

        var plan = await GetFloorPlanAsync(floor.OutletId);
        return plan.Floors.First(f => f.Id == floor.Id);
    }

    private async Task RefreshSeatingCapacityAsync(Guid outletId, Guid userId)
    {
        var outlet = await db.Outlets.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == outletId);
        if (outlet is null) return;

        outlet.SeatingCapacity = await db.Tables.ForTenant(tenant)
            .Where(t => t.OutletId == outletId && t.IsActive)
            .SumAsync(t => t.Seats);

        outlet.StampUpdated(userId);
        await db.SaveChangesAsync();
    }

    // ── Service operations ───────────────────────────────────────────────────

    public async Task<RestaurantOrderDto?> SeatGuestsAsync(SeatGuestsDto request, Guid userId)
    {
        var table = await db.Tables.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == request.TableId)
            ?? throw new InvalidOperationException("Table not found.");

        if (table.State is TableState.Seated or TableState.Ordered or TableState.Served or TableState.BillPrinted)
            throw new InvalidOperationException($"Table {table.TableNumber} is already in service.");

        if (table.State == TableState.Blocked)
            throw new InvalidOperationException($"Table {table.TableNumber} is out of service.");

        table.CurrentGuestCount = Math.Max(1, request.GuestCount);
        table.AssignedWaiterId = request.WaiterId ?? table.AssignedWaiterId;
        table.SeatedAt = DateTime.UtcNow;
        await ApplyStateAsync(table, TableState.Seated, userId, null, null);

        if (request.ReservationId.HasValue)
        {
            var reservation = await db.Reservations.ForTenant(tenant)
                .FirstOrDefaultAsync(r => r.Id == request.ReservationId);
            if (reservation is not null)
            {
                reservation.Status = ReservationStatus.Seated;
                reservation.SeatedAt = DateTime.UtcNow;
                reservation.TableId = table.Id;
                reservation.StampUpdated(userId);
            }
        }

        if (request.WaitlistEntryId.HasValue)
        {
            var entry = await db.Waitlist.ForTenant(tenant)
                .FirstOrDefaultAsync(w => w.Id == request.WaitlistEntryId);
            if (entry is not null)
            {
                entry.Status = WaitlistStatus.Seated;
                entry.SeatedAt = DateTime.UtcNow;
                entry.TableId = table.Id;
                entry.StampUpdated(userId);
            }
        }

        await db.SaveChangesAsync();

        if (!request.CreateOrder) return null;

        var order = await orders.OpenOrderAsync(new OpenOrderDto
        {
            OutletId = table.OutletId,
            OrderType = OrderType.DineIn,
            Channel = OrderChannel.InHouse,
            TableId = table.Id,
            GuestCount = table.CurrentGuestCount,
            WaiterId = table.AssignedWaiterId,
            GuestProfileId = request.GuestProfileId,
        }, userId);

        return order;
    }

    public async Task<TableDto> ChangeStateAsync(ChangeTableStateDto request, Guid userId)
    {
        var table = await db.Tables.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == request.TableId)
            ?? throw new InvalidOperationException("Table not found.");

        await ApplyStateAsync(table, request.State, userId, null, request.Note);

        if (request.State is TableState.Free)
        {
            table.CurrentOrderId = null;
            table.CurrentGuestCount = 0;
            table.SeatedAt = null;
            table.MergedIntoTableId = null;
        }

        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(table, DateTime.UtcNow);
    }

    public async Task<TableDto> AssignWaiterAsync(AssignWaiterDto request, Guid userId)
    {
        var table = await db.Tables.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == request.TableId)
            ?? throw new InvalidOperationException("Table not found.");

        table.AssignedWaiterId = request.WaiterId;
        table.StampUpdated(userId);

        // The open order follows the table: reassigning a section mid-service must not leave
        // last hour's orders crediting the waiter who went home.
        if (table.CurrentOrderId.HasValue)
        {
            var order = await db.Orders.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == table.CurrentOrderId);
            var waiter = await db.Staff.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == request.WaiterId);
            if (order is not null)
            {
                order.WaiterId = request.WaiterId;
                order.WaiterName = waiter?.DisplayName ?? waiter?.FullName;
                order.StampUpdated(userId);
            }
        }

        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(table, DateTime.UtcNow);
    }

    public async Task<TableDto> TransferTableAsync(TransferTableDto request, Guid userId)
    {
        var from = await db.Tables.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == request.FromTableId)
            ?? throw new InvalidOperationException("Source table not found.");
        var to = await db.Tables.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == request.ToTableId)
            ?? throw new InvalidOperationException("Destination table not found.");

        if (from.CurrentOrderId is null)
            throw new InvalidOperationException($"Table {from.TableNumber} has no open order to move.");

        if (to.CurrentOrderId is not null)
            throw new InvalidOperationException($"Table {to.TableNumber} already has an open order.");

        if (to.State == TableState.Blocked)
            throw new InvalidOperationException($"Table {to.TableNumber} is out of service.");

        var order = await db.Orders.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == from.CurrentOrderId);
        if (order is not null)
        {
            order.TableId = to.Id;
            order.TableNumber = to.TableNumber;
            order.SectionId = to.SectionId;
            order.StampUpdated(userId);

            // Tickets already on the kitchen screens must show the new table, or the food goes
            // to the wrong place.
            var tickets = await db.KitchenTickets.ForTenant(tenant)
                .Where(t => t.OrderId == order.Id && t.Status != KitchenTicketStatus.Bumped)
                .ToListAsync();
            foreach (var ticket in tickets)
            {
                ticket.TableNumber = to.TableNumber;
                ticket.StampUpdated(userId);
            }
        }

        to.CurrentOrderId = from.CurrentOrderId;
        to.CurrentGuestCount = from.CurrentGuestCount;
        to.AssignedWaiterId = from.AssignedWaiterId;
        to.SeatedAt = from.SeatedAt;
        await ApplyStateAsync(to, from.State, userId, order?.Id, request.Reason);

        from.CurrentOrderId = null;
        from.CurrentGuestCount = 0;
        from.SeatedAt = null;
        await ApplyStateAsync(from, TableState.NeedsCleaning, userId, null, request.Reason);

        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(to, DateTime.UtcNow);
    }

    public async Task<List<TableDto>> MergeTablesAsync(MergeTablesDto request, Guid userId)
    {
        var ids = request.TableIds.Append(request.PrimaryTableId).Distinct().ToList();

        var tables = await db.Tables.ForTenant(tenant).Where(t => ids.Contains(t.Id)).ToListAsync();
        var primary = tables.FirstOrDefault(t => t.Id == request.PrimaryTableId)
            ?? throw new InvalidOperationException("Primary table not found.");

        // Merging two tables that each already have an order would mean silently discarding one
        // party's food. The waiter has to decide which order survives, so refuse.
        var withOrders = tables.Where(t => t.CurrentOrderId.HasValue && t.Id != primary.Id).ToList();
        if (withOrders.Count > 0)
            throw new InvalidOperationException(
                $"Table(s) {string.Join(", ", withOrders.Select(t => t.TableNumber))} already have open orders. " +
                "Transfer or close them before merging.");

        var guestCount = request.GuestCount ?? tables.Sum(t => t.CurrentGuestCount);
        primary.CurrentGuestCount = guestCount > 0 ? guestCount : tables.Sum(t => t.Seats);
        primary.MergedIntoTableId = null;
        primary.StampUpdated(userId);

        foreach (var table in tables.Where(t => t.Id != primary.Id))
        {
            table.MergedIntoTableId = primary.Id;
            table.CurrentOrderId = primary.CurrentOrderId;
            table.AssignedWaiterId = primary.AssignedWaiterId;
            table.CurrentGuestCount = 0;
            await ApplyStateAsync(table, primary.State == TableState.Free ? TableState.Seated : primary.State,
                                  userId, primary.CurrentOrderId, "Merged");
        }

        if (primary.State == TableState.Free)
        {
            primary.SeatedAt = DateTime.UtcNow;
            await ApplyStateAsync(primary, TableState.Seated, userId, null, "Merged");
        }

        if (primary.CurrentOrderId.HasValue)
        {
            var order = await db.Orders.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == primary.CurrentOrderId);
            if (order is not null)
            {
                order.GuestCount = primary.CurrentGuestCount;
                order.StampUpdated(userId);
            }
        }

        await db.SaveChangesAsync();
        var now = DateTime.UtcNow;
        return tables.Select(t => RestaurantMapper.ToDto(t, now)).ToList();
    }

    public async Task<List<TableDto>> UnmergeTableAsync(Guid primaryTableId, Guid userId)
    {
        var members = await db.Tables.ForTenant(tenant)
            .Where(t => t.MergedIntoTableId == primaryTableId).ToListAsync();

        foreach (var table in members)
        {
            table.MergedIntoTableId = null;
            table.CurrentOrderId = null;
            table.CurrentGuestCount = 0;
            await ApplyStateAsync(table, TableState.NeedsCleaning, userId, null, "Unmerged");
        }

        await db.SaveChangesAsync();
        var now = DateTime.UtcNow;
        return members.Select(t => RestaurantMapper.ToDto(t, now)).ToList();
    }

    public async Task<TableDto> ClearTableAsync(Guid tableId, Guid userId)
    {
        var table = await db.Tables.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == tableId)
            ?? throw new InvalidOperationException("Table not found.");

        table.CurrentOrderId = null;
        table.CurrentGuestCount = 0;
        table.SeatedAt = null;
        table.MergedIntoTableId = null;
        await ApplyStateAsync(table, TableState.Free, userId, null, null);

        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(table, DateTime.UtcNow);
    }

    /// <summary>
    /// The single place a table's state changes. Writes the transition log — including how long
    /// the table held the previous state — and leaves the row for the caller to save.
    /// </summary>
    private async Task ApplyStateAsync(DiningTable table, TableState next, Guid userId, Guid? orderId, string? note)
    {
        var now = DateTime.UtcNow;
        var previous = table.State;

        if (previous == next)
        {
            table.StampUpdated(userId);
            return;
        }

        var seconds = table.StateChangedAt is null
            ? (int?)null
            : (int)Math.Max(0, (now - table.StateChangedAt.Value).TotalSeconds);

        db.TableStateLogs.Add(new TableStateLog
        {
            TableId = table.Id,
            OutletId = table.OutletId,
            OrderId = orderId ?? table.CurrentOrderId,
            WaiterId = table.AssignedWaiterId,
            FromState = previous,
            ToState = next,
            OccurredAt = now,
            SecondsInPreviousState = seconds,
            GuestCount = table.CurrentGuestCount,
            Note = note,
        }.StampNew(tenant, userId));

        table.State = next;
        table.StateChangedAt = now;
        table.StampUpdated(userId);

        await Task.CompletedTask;
    }
}
