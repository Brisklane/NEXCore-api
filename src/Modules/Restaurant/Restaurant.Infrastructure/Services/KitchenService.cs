using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Application.Services.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

/// <summary>
/// Station routing and the kitchen display.
///
/// The routing decision is the interesting part. A fired line has to end up in front of exactly
/// the cook who makes it, and one order routinely splits across three stations. Rules are scored
/// by specificity — item beats category beats order-type beats catch-all — so a kitchen can
/// configure one broad default and then override it dish by dish without the rules fighting.
/// </summary>
public class KitchenService(RestaurantDbContext db, IRestaurantTenant tenant, RestaurantNumbering numbering)
    : IKitchenService
{
    public async Task<KitchenDisplayDto> GetDisplayAsync(Guid outletId, Guid? stationId, bool includeBumped = false)
    {
        var now = DateTime.UtcNow;
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        var warningMinutes = settings?.KdsWarningMinutes ?? 8;

        var stations = await db.Stations.ForTenant(tenant)
            .Where(s => s.OutletId == outletId && s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();

        var station = stationId.HasValue ? stations.FirstOrDefault(s => s.Id == stationId) : null;
        var isExpo = station?.IsExpo ?? stationId is null;

        // The pass sees everything; a station sees only its own work.
        var query = db.KitchenTickets.ForTenant(tenant)
            .Where(t => t.OutletId == outletId)
            .WhereIf(stationId.HasValue && !isExpo, t => t.StationId == stationId);

        query = includeBumped
            ? query.Where(t => t.FiredAt >= now.AddHours(-4))
            : query.Where(t => t.Status != KitchenTicketStatus.Bumped && t.Status != KitchenTicketStatus.Cancelled);

        var tickets = await query
            .Include(t => t.Lines.Where(l => !l.IsDeleted))
            .OrderByDescending(t => t.IsPriority)
            .ThenBy(t => t.FiredAt)
            .Take(120)
            .ToListAsync();

        var stationById = stations.ToDictionary(s => s.Id);

        var dtos = tickets.Select(t =>
        {
            var sla = stationById.TryGetValue(t.StationId, out var s) ? s.SlaMinutes : 15;
            var dto = RestaurantMapper.ToDto(t, now, warningMinutes, sla);
            if (s is not null)
            {
                dto.StationName = s.Name;
                dto.StationColorHex = s.ColorHex;
            }
            return dto;
        }).ToList();

        var orderIds = tickets.Select(t => t.OrderId).Distinct().ToList();
        var orderNumbers = await db.Orders.ForTenant(tenant)
            .Where(o => orderIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => o.OrderNumber);

        foreach (var dto in dtos) dto.OrderNumber = orderNumbers.GetValueOrDefault(dto.OrderId);

        var live = dtos.Where(t => t.Status is not (KitchenTicketStatus.Bumped or KitchenTicketStatus.Cancelled)).ToList();
        var completed = tickets.Where(t => t.PrepSeconds.HasValue).Select(t => t.PrepSeconds!.Value).ToList();

        return new KitchenDisplayDto
        {
            OutletId = outletId,
            StationId = stationId,
            StationName = station?.Name ?? "All stations",
            IsExpo = isExpo,
            ServerTime = now,
            Tickets = dtos,
            AllDayCounts = BuildAllDayCounts(live),
            NewCount = live.Count(t => t.Status == KitchenTicketStatus.New),
            InProgressCount = live.Count(t => t.Status is KitchenTicketStatus.Acknowledged or KitchenTicketStatus.InProgress),
            ReadyCount = live.Count(t => t.Status == KitchenTicketStatus.Ready),
            OverdueCount = live.Count(t => t.UrgencyLevel == "overdue"),
            AveragePrepSeconds = completed.Count == 0 ? 0 : (int)completed.Average(),
        };
    }

    /// <summary>
    /// "All day": how many of each dish are still outstanding across every open ticket. A cook
    /// batching twelve portions of fries needs one number, not twelve tickets to read.
    /// </summary>
    private static List<AllDayCountDto> BuildAllDayCounts(List<KitchenTicketDto> tickets) =>
        tickets
            .SelectMany(t => t.Lines
                .Where(l => l.Status is not (OrderLineStatus.Served or OrderLineStatus.Voided))
                .Select(l => new { Ticket = t, Line = l }))
            .GroupBy(x => new { x.Line.MenuItemId, x.Line.ItemName, x.Line.VariantName })
            .Select(g => new AllDayCountDto
            {
                MenuItemId = g.Key.MenuItemId,
                ItemName = g.Key.ItemName,
                VariantName = g.Key.VariantName,
                OutstandingQuantity = g.Sum(x => x.Line.Quantity),
                TicketCount = g.Select(x => x.Ticket.Id).Distinct().Count(),
                OldestAgeSeconds = g.Max(x => x.Ticket.AgeSeconds),
            })
            .OrderByDescending(c => c.OutstandingQuantity)
            .ToList();

    public async Task<List<KitchenTicketDto>> GetTicketsForOrderAsync(Guid orderId)
    {
        var now = DateTime.UtcNow;
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        var tickets = await db.KitchenTickets.ForTenant(tenant)
            .Where(t => t.OrderId == orderId)
            .Include(t => t.Lines.Where(l => !l.IsDeleted))
            .OrderBy(t => t.FiredAt)
            .ToListAsync();

        var stations = await db.Stations.ForTenant(tenant)
            .ToDictionaryAsync(s => s.Id, s => new { s.Name, s.ColorHex, s.SlaMinutes });

        return tickets.Select(t =>
        {
            var station = stations.GetValueOrDefault(t.StationId);
            var dto = RestaurantMapper.ToDto(t, now, settings?.KdsWarningMinutes ?? 8, station?.SlaMinutes ?? 15);
            dto.StationName = station?.Name;
            dto.StationColorHex = station?.ColorHex;
            return dto;
        }).ToList();
    }

    // ── Ticket creation ──────────────────────────────────────────────────────

    public async Task<List<KitchenTicketDto>> CreateTicketsAsync(
        Guid orderId, List<Guid> orderLineIds, bool isPriority, Guid userId)
    {
        var now = DateTime.UtcNow;

        var order = await db.Orders.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new InvalidOperationException("Order not found.");

        var lines = await db.OrderLines.ForTenant(tenant)
            .Where(l => orderLineIds.Contains(l.Id) && !l.IsVoided)
            .Include(l => l.Modifiers.Where(m => !m.IsDeleted))
            .ToListAsync();

        if (lines.Count == 0) return [];

        var stations = await db.Stations.ForTenant(tenant)
            .Where(s => s.OutletId == order.OutletId && s.IsActive)
            .Include(s => s.RoutingRules.Where(r => !r.IsDeleted && r.IsActive))
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();

        if (stations.Count == 0) return [];

        var itemIds = lines.Select(l => l.MenuItemId).Distinct().ToList();
        var items = await db.MenuItems.ForTenant(tenant)
            .Where(i => itemIds.Contains(i.Id))
            .Select(i => new
            {
                i.Id, i.CategoryId, i.StationId, i.KitchenNote,
                i.Allergens, i.ContainsNuts, i.ContainsShellfish, i.ContainsDairy,
            })
            .ToListAsync();

        var categoryStations = await db.MenuCategories.ForTenant(tenant)
            .Where(c => items.Select(i => i.CategoryId).Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.DefaultStationId);

        var expo = stations.FirstOrDefault(s => s.IsExpo);
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        var expoEnabled = settings?.ExpoScreenEnabled ?? true;

        // station id → the lines that station has to cook
        var routed = new Dictionary<Guid, List<RestaurantOrderLine>>();

        foreach (var line in lines)
        {
            var item = items.FirstOrDefault(i => i.Id == line.MenuItemId);
            var categoryId = item?.CategoryId;

            var targets = ResolveStations(stations, line, item?.StationId, categoryId,
                                          categoryStations.GetValueOrDefault(categoryId ?? Guid.Empty), order.OrderType);

            // Everything also lands on the pass, so plates for one table leave together.
            if (expoEnabled && expo is not null && targets.All(s => s.Id != expo.Id))
                targets.Add(expo);

            foreach (var target in targets)
            {
                if (!routed.TryGetValue(target.Id, out var list)) routed[target.Id] = list = [];
                list.Add(line);
            }

            line.StationId ??= targets.FirstOrDefault(s => !s.IsExpo)?.Id;
        }

        var created = new List<KitchenTicket>();

        foreach (var (stationId, stationLines) in routed)
        {
            // One ticket per station *per course*: a kitchen that lumps starters and mains onto
            // one card cannot pace a meal.
            foreach (var courseGroup in stationLines.GroupBy(l => l.Course))
            {
                var ticket = new KitchenTicket
                {
                    OutletId = order.OutletId,
                    StationId = stationId,
                    OrderId = order.Id,
                    TicketNumber = await numbering.NextTicketNumberAsync(order.OutletId, now),
                    Status = KitchenTicketStatus.New,
                    Course = courseGroup.Key,
                    OrderType = order.OrderType,
                    TableNumber = order.TableNumber ?? order.TokenNumber,
                    WaiterName = order.WaiterName,
                    GuestCount = order.GuestCount,
                    IsPriority = isPriority,
                    FiredAt = now,
                }.StampNew(tenant, userId);

                ticket.Code = ticket.TicketNumber;

                var displayOrder = 0;
                foreach (var line in courseGroup.OrderBy(l => l.SeatNumber ?? 0).ThenBy(l => l.DisplayOrder))
                {
                    var item = items.FirstOrDefault(i => i.Id == line.MenuItemId);

                    var allergens = new List<string>();
                    if (item?.ContainsNuts == true) allergens.Add("NUTS");
                    if (item?.ContainsShellfish == true) allergens.Add("SHELLFISH");
                    if (item?.ContainsDairy == true) allergens.Add("DAIRY");
                    if (!string.IsNullOrWhiteSpace(item?.Allergens)) allergens.Add(item!.Allergens!.ToUpperInvariant());

                    ticket.Lines.Add(new KitchenTicketLine
                    {
                        TicketId = ticket.Id,
                        OrderLineId = line.Id,
                        MenuItemId = line.MenuItemId,
                        ItemName = line.ItemName,
                        VariantName = line.VariantName,
                        Quantity = line.Quantity,
                        ModifierSummary = SummariseModifiers(line),
                        SpecialInstructions = string.Join(" · ",
                            new[] { line.SpecialInstructions, item?.KitchenNote }.Where(s => !string.IsNullOrWhiteSpace(s))),
                        AllergenWarning = allergens.Count > 0 ? string.Join(", ", allergens) : null,
                        SeatNumber = line.SeatNumber,
                        Status = OrderLineStatus.Fired,
                        DisplayOrder = displayOrder++,
                    }.StampNew(tenant, userId));
                }

                db.KitchenTickets.Add(ticket);
                created.Add(ticket);
            }
        }

        await db.SaveChangesAsync();

        var settingsWarning = settings?.KdsWarningMinutes ?? 8;
        var stationById = stations.ToDictionary(s => s.Id);

        return created.Select(t =>
        {
            var dto = RestaurantMapper.ToDto(t, now, settingsWarning,
                stationById.TryGetValue(t.StationId, out var s) ? s.SlaMinutes : 15);
            dto.StationName = stationById.GetValueOrDefault(t.StationId)?.Name;
            dto.OrderNumber = order.OrderNumber;
            return dto;
        }).ToList();
    }

    /// <summary>
    /// Picks the station(s) for a line. Explicit wins over inferred: an item pinned to a station
    /// goes there, otherwise the most specific matching rule decides, otherwise the category's
    /// default, otherwise the first non-expo station so nothing is ever silently dropped.
    /// </summary>
    private static List<KitchenStation> ResolveStations(
        List<KitchenStation> stations, RestaurantOrderLine line,
        Guid? itemStationId, Guid? categoryId, Guid? categoryStationId, OrderType orderType)
    {
        var targets = new List<KitchenStation>();

        if (itemStationId.HasValue)
        {
            var pinned = stations.FirstOrDefault(s => s.Id == itemStationId);
            if (pinned is not null) targets.Add(pinned);
        }

        if (targets.Count == 0)
        {
            var candidates = stations
                .SelectMany(s => s.RoutingRules.Select(r => new { Station = s, Rule = r }))
                .Where(x => Matches(x.Rule, line, categoryId, orderType))
                .OrderByDescending(x => Specificity(x.Rule))
                .ThenBy(x => x.Rule.Priority)
                .ToList();

            var primary = candidates.FirstOrDefault(x => !x.Rule.IsAdditional);
            if (primary is not null) targets.Add(primary.Station);

            foreach (var additional in candidates.Where(x => x.Rule.IsAdditional))
                if (targets.All(s => s.Id != additional.Station.Id))
                    targets.Add(additional.Station);
        }

        if (targets.Count == 0 && categoryStationId.HasValue)
        {
            var fromCategory = stations.FirstOrDefault(s => s.Id == categoryStationId);
            if (fromCategory is not null) targets.Add(fromCategory);
        }

        if (targets.Count == 0)
        {
            var fallback = stations.FirstOrDefault(s => !s.IsExpo) ?? stations.FirstOrDefault();
            if (fallback is not null) targets.Add(fallback);
        }

        return targets;
    }

    private static bool Matches(StationRoutingRule rule, RestaurantOrderLine line, Guid? categoryId, OrderType orderType)
        => rule.MatchType switch
        {
            RoutingMatchType.Item => rule.MenuItemId == line.MenuItemId,
            RoutingMatchType.Category => categoryId.HasValue && rule.CategoryId == categoryId,
            RoutingMatchType.OrderType => rule.OrderType == orderType,
            RoutingMatchType.AllItems => true,
            _ => false,
        };

    private static int Specificity(StationRoutingRule rule) => rule.MatchType switch
    {
        RoutingMatchType.Item => 4,
        RoutingMatchType.Category => 3,
        RoutingMatchType.OrderType => 2,
        _ => 1,
    };

    /// <summary>
    /// Renders modifiers the way a cook reads them: removals first and in capitals, because
    /// "NO PEANUTS" missed on a busy line is the failure that actually hurts someone.
    /// </summary>
    private static string? SummariseModifiers(RestaurantOrderLine line)
    {
        var modifiers = line.Modifiers.Where(m => !m.IsDeleted).ToList();
        if (modifiers.Count == 0) return null;

        var removals = modifiers.Where(m => m.IsRemoval)
            .Select(m => $"NO {m.ModifierName.ToUpperInvariant()}");

        var additions = modifiers.Where(m => !m.IsRemoval)
            .Select(m => m.Quantity > 1 ? $"{m.Quantity:0.##}× {m.ModifierName}" : m.ModifierName);

        return string.Join(" · ", removals.Concat(additions));
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    public async Task<KitchenTicketDto> AcknowledgeAsync(Guid ticketId, Guid userId)
        => await TransitionAsync(ticketId, userId, t =>
        {
            if (t.Status == KitchenTicketStatus.New)
            {
                t.Status = KitchenTicketStatus.Acknowledged;
                t.AcknowledgedAt = DateTime.UtcNow;
            }
        });

    public async Task<KitchenTicketDto> StartAsync(Guid ticketId, Guid userId)
        => await TransitionAsync(ticketId, userId, t =>
        {
            var now = DateTime.UtcNow;
            t.AcknowledgedAt ??= now;
            t.Status = KitchenTicketStatus.InProgress;
            t.StartedAt ??= now;
        });

    public async Task<KitchenTicketDto> MarkReadyAsync(Guid ticketId, Guid? ticketLineId, Guid userId)
    {
        var now = DateTime.UtcNow;

        return await TransitionAsync(ticketId, userId, t =>
        {
            if (ticketLineId.HasValue)
            {
                var line = t.Lines.FirstOrDefault(l => l.Id == ticketLineId);
                if (line is not null)
                {
                    line.Status = OrderLineStatus.Ready;
                    line.ReadyAt = now;
                    line.StampUpdated(userId);
                }

                // The ticket is only ready once the last line on it is — a station that clears a
                // card with a dish still on the pass sends an incomplete table.
                var outstanding = t.Lines.Any(l => !l.IsDeleted
                    && l.Status is not (OrderLineStatus.Ready or OrderLineStatus.Served or OrderLineStatus.Voided));

                if (outstanding) return;
            }
            else
            {
                foreach (var line in t.Lines.Where(l => !l.IsDeleted && l.Status != OrderLineStatus.Voided))
                {
                    line.Status = OrderLineStatus.Ready;
                    line.ReadyAt = now;
                    line.StampUpdated(userId);
                }
            }

            t.Status = KitchenTicketStatus.Ready;
            t.ReadyAt = now;
            t.StartedAt ??= t.AcknowledgedAt ?? t.FiredAt;
        }, syncOrderLines: true);
    }

    public async Task<KitchenTicketDto> BumpAsync(BumpTicketDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        return await TransitionAsync(request.TicketId, userId, t =>
        {
            if (request.TicketLineId.HasValue)
            {
                var line = t.Lines.FirstOrDefault(l => l.Id == request.TicketLineId);
                if (line is not null)
                {
                    line.Status = OrderLineStatus.Served;
                    line.ReadyAt ??= now;
                    line.StampUpdated(userId);
                }

                var outstanding = t.Lines.Any(l => !l.IsDeleted
                    && l.Status is not (OrderLineStatus.Served or OrderLineStatus.Voided));

                if (outstanding) return;
            }
            else
            {
                foreach (var line in t.Lines.Where(l => !l.IsDeleted && l.Status != OrderLineStatus.Voided))
                {
                    line.Status = OrderLineStatus.Served;
                    line.ReadyAt ??= now;
                    line.StampUpdated(userId);
                }
            }

            t.Status = KitchenTicketStatus.Bumped;
            t.BumpedAt = now;
            t.ReadyAt ??= now;
            t.PrepSeconds = (int)Math.Max(0, (now - t.FiredAt).TotalSeconds);
        }, syncOrderLines: true);
    }

    public async Task<KitchenTicketDto> RecallAsync(RecallTicketDto request, Guid userId)
        => await TransitionAsync(request.TicketId, userId, t =>
        {
            t.Status = KitchenTicketStatus.InProgress;
            t.BumpedAt = null;
            t.ReadyAt = null;
            t.PrepSeconds = null;
            t.RecallCount++;
            t.Note = string.IsNullOrWhiteSpace(request.Reason) ? t.Note : request.Reason;

            foreach (var line in t.Lines.Where(l => !l.IsDeleted && l.Status != OrderLineStatus.Voided))
            {
                line.Status = OrderLineStatus.Fired;
                line.ReadyAt = null;
                line.StampUpdated(userId);
            }
        }, syncOrderLines: true);

    public async Task<KitchenTicketDto> SetPriorityAsync(Guid ticketId, bool isPriority, Guid userId)
        => await TransitionAsync(ticketId, userId, t => t.IsPriority = isPriority);

    /// <summary>
    /// Applies a change to a ticket and, when the change affects what the floor sees, pushes the
    /// same status onto the underlying order lines. The two must not drift: a waiter's screen
    /// showing "preparing" for a dish already on the pass is worse than no screen at all.
    /// </summary>
    private async Task<KitchenTicketDto> TransitionAsync(
        Guid ticketId, Guid userId, Action<KitchenTicket> mutate, bool syncOrderLines = false)
    {
        var ticket = await db.KitchenTickets.ForTenant(tenant)
            .Include(t => t.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(t => t.Id == ticketId)
            ?? throw new InvalidOperationException("Kitchen ticket not found.");

        mutate(ticket);
        ticket.StampUpdated(userId);

        if (syncOrderLines)
        {
            var orderLineIds = ticket.Lines.Select(l => l.OrderLineId).ToList();
            var orderLines = await db.OrderLines.ForTenant(tenant)
                .Where(l => orderLineIds.Contains(l.Id))
                .ToListAsync();

            foreach (var orderLine in orderLines)
            {
                var ticketLine = ticket.Lines.First(l => l.OrderLineId == orderLine.Id);
                if (orderLine.IsVoided) continue;

                // Only advance. An expo bump must not walk a dish the grill already marked served
                // back to "fired".
                if (ticketLine.Status > orderLine.Status || ticket.Status == KitchenTicketStatus.Recalled)
                {
                    orderLine.Status = ticketLine.Status;
                    if (ticketLine.Status == OrderLineStatus.Ready) orderLine.ReadyAt ??= DateTime.UtcNow;
                    orderLine.StampUpdated(userId);
                }
            }
        }

        await db.SaveChangesAsync();

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        var station = await db.Stations.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == ticket.StationId);

        var dto = RestaurantMapper.ToDto(ticket, DateTime.UtcNow,
            settings?.KdsWarningMinutes ?? 8, station?.SlaMinutes ?? 15);

        dto.StationName = station?.Name;
        dto.StationColorHex = station?.ColorHex;
        dto.OrderNumber = await db.Orders.ForTenant(tenant)
            .Where(o => o.Id == ticket.OrderId).Select(o => o.OrderNumber).FirstOrDefaultAsync();

        return dto;
    }
}
