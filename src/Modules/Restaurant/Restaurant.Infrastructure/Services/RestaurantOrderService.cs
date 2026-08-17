using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;
using Restaurant.Application.DTOs;
using Restaurant.Application.Services.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

/// <summary>
/// The order lifecycle. An order is opened when guests sit, added to over the course of a meal,
/// fired course by course, and only then billed.
///
/// Two rules run through everything here:
///
/// **The server prices the order.** A client may say what was ordered; it never says what it
/// costs. Every line is priced through <see cref="MenuService.ResolvePriceAsync"/>, so a stale
/// tablet, a tampered request and a happy-hour rule that started thirty seconds ago all produce
/// the same, correct number.
///
/// **Held lines never reach the kitchen.** Coursing only works if a held line is invisible to
/// the kitchen until it is fired, so ticket creation is driven strictly off the fire action —
/// never off adding a line.
/// </summary>
public class RestaurantOrderService(
    RestaurantDbContext db,
    IRestaurantTenant tenant,
    MenuService menu,
    RestaurantNumbering numbering,
    IKitchenService kitchen,
    ILogger<RestaurantOrderService> logger) : IRestaurantOrderService
{
    // ── Reads ────────────────────────────────────────────────────────────────

    public async Task<RestaurantOrderDto?> GetOrderAsync(Guid orderId)
    {
        var order = await LoadOrderAsync(orderId);
        if (order is null) return null;

        var dto = RestaurantMapper.ToDto(order, DateTime.UtcNow);
        await DecorateAsync(dto);
        return dto;
    }

    public async Task<RestaurantOrderDto?> GetOrderByTableAsync(Guid tableId)
    {
        var orderId = await db.Orders.ForTenant(tenant)
            .Where(o => o.TableId == tableId
                     && o.Status != RestaurantOrderStatus.Closed
                     && o.Status != RestaurantOrderStatus.Cancelled)
            .OrderByDescending(o => o.OpenedAt)
            .Select(o => (Guid?)o.Id)
            .FirstOrDefaultAsync();

        return orderId is null ? null : await GetOrderAsync(orderId.Value);
    }

    public async Task<PaginatedResponse<OrderSummaryDto>> ListOrdersAsync(
        Guid? outletId, RestaurantOrderStatus? status, OrderType? orderType,
        DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.Orders.ForTenant(tenant)
            .WhereIf(outletId.HasValue, o => o.OutletId == outletId)
            .WhereIf(status.HasValue, o => o.Status == status)
            .WhereIf(orderType.HasValue, o => o.OrderType == orderType)
            .WhereIf(from.HasValue, o => o.OpenedAt >= from)
            .WhereIf(to.HasValue, o => o.OpenedAt <= to)
            .WhereIf(!string.IsNullOrWhiteSpace(pagination.SearchTerm),
                o => o.OrderNumber.ToLower().Contains(pagination.SearchTerm!.ToLower())
                  || (o.CustomerName != null && o.CustomerName.ToLower().Contains(pagination.SearchTerm!.ToLower()))
                  || (o.TableNumber != null && o.TableNumber.ToLower().Contains(pagination.SearchTerm!.ToLower())));

        var total = await query.CountAsync();

        var rows = await query
            .OrderByDescending(o => o.OpenedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(o => new
            {
                o.Id, o.OrderNumber, o.TokenNumber, o.OrderType, o.Channel, o.Status,
                o.TableNumber, o.WaiterName, o.CustomerName, o.GuestCount,
                o.TotalAmount, o.PaidAmount, o.SubTotal, o.OpenedAt, o.ClosedAt,
                LineCount = o.Lines.Count(l => !l.IsDeleted && !l.IsVoided),
                HeldCount = o.Lines.Count(l => !l.IsDeleted && !l.IsVoided && l.IsHeld),
            })
            .ToListAsync();

        var now = DateTime.UtcNow;
        var dtos = rows.Select(o => new OrderSummaryDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            TokenNumber = o.TokenNumber,
            OrderType = o.OrderType,
            Channel = o.Channel,
            Status = o.Status,
            TableNumber = o.TableNumber,
            WaiterName = o.WaiterName,
            CustomerName = o.CustomerName,
            GuestCount = o.GuestCount,
            LineCount = o.LineCount,
            TotalAmount = o.TotalAmount > 0 ? o.TotalAmount : o.SubTotal,
            PaidAmount = o.PaidAmount,
            OpenedAt = o.OpenedAt,
            ClosedAt = o.ClosedAt,
            MinutesOpen = (int)Math.Max(0, ((o.ClosedAt ?? now) - o.OpenedAt).TotalMinutes),
            HeldLineCount = o.HeldCount,
        }).ToList();

        return PaginatedResponse<OrderSummaryDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<List<OrderSummaryDto>> GetActiveOrdersAsync(Guid outletId, Guid? waiterId = null)
    {
        var now = DateTime.UtcNow;

        var rows = await db.Orders.ForTenant(tenant)
            .Where(o => o.OutletId == outletId
                     && o.Status != RestaurantOrderStatus.Closed
                     && o.Status != RestaurantOrderStatus.Cancelled)
            .WhereIf(waiterId.HasValue, o => o.WaiterId == waiterId)
            .OrderBy(o => o.OpenedAt)
            .Select(o => new
            {
                o.Id, o.OrderNumber, o.TokenNumber, o.OrderType, o.Channel, o.Status,
                o.TableNumber, o.WaiterName, o.CustomerName, o.GuestCount,
                o.TotalAmount, o.PaidAmount, o.SubTotal, o.OpenedAt,
                LineCount = o.Lines.Count(l => !l.IsDeleted && !l.IsVoided),
                HeldCount = o.Lines.Count(l => !l.IsDeleted && !l.IsVoided && l.IsHeld),
            })
            .ToListAsync();

        return rows.Select(o => new OrderSummaryDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            TokenNumber = o.TokenNumber,
            OrderType = o.OrderType,
            Channel = o.Channel,
            Status = o.Status,
            TableNumber = o.TableNumber,
            WaiterName = o.WaiterName,
            CustomerName = o.CustomerName,
            GuestCount = o.GuestCount,
            LineCount = o.LineCount,
            TotalAmount = o.TotalAmount > 0 ? o.TotalAmount : o.SubTotal,
            PaidAmount = o.PaidAmount,
            OpenedAt = o.OpenedAt,
            MinutesOpen = (int)Math.Max(0, (now - o.OpenedAt).TotalMinutes),
            HeldLineCount = o.HeldCount,
        }).ToList();
    }

    public Task<OrderPadCatalogDto> GetOrderPadCatalogAsync(Guid outletId, OrderType orderType)
        => menu.BuildCatalogAsync(outletId, orderType);

    // ── Open ─────────────────────────────────────────────────────────────────

    public async Task<RestaurantOrderDto> OpenOrderAsync(OpenOrderDto request, Guid userId)
    {
        // Idempotency first: a retry after a timeout must return the original order, not a second
        // one. Losing this check means a flaky handheld double-charges a table.
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existingId = await db.Orders.ForTenant(tenant)
                .Where(o => o.IdempotencyKey == request.IdempotencyKey)
                .Select(o => (Guid?)o.Id)
                .FirstOrDefaultAsync();

            if (existingId is not null) return (await GetOrderAsync(existingId.Value))!;
        }

        var now = DateTime.UtcNow;
        var outlet = await db.Outlets.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == request.OutletId)
            ?? throw new InvalidOperationException("Outlet not found.");

        var settings = await GetSettingsAsync();

        DiningTable? table = null;
        if (request.TableId.HasValue)
        {
            table = await db.Tables.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == request.TableId)
                ?? throw new InvalidOperationException("Table not found.");

            var busy = await db.Orders.ForTenant(tenant).AnyAsync(o =>
                o.TableId == table.Id &&
                o.Status != RestaurantOrderStatus.Closed &&
                o.Status != RestaurantOrderStatus.Cancelled);

            if (busy) throw new InvalidOperationException($"Table {table.TableNumber} already has an open order.");
        }

        var waiter = request.WaiterId.HasValue
            ? await db.Staff.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == request.WaiterId)
            : null;

        var order = new RestaurantOrder
        {
            OutletId = request.OutletId,
            OrderNumber = await numbering.NextOrderNumberAsync(request.OutletId, now),
            OrderType = request.OrderType,
            Channel = request.Channel,
            Status = RestaurantOrderStatus.Open,
            TableId = table?.Id,
            TableNumber = table?.TableNumber,
            SectionId = table?.SectionId,
            GuestCount = Math.Max(1, request.GuestCount),
            WaiterId = request.WaiterId,
            WaiterName = waiter?.DisplayName ?? waiter?.FullName,
            SessionId = request.SessionId,
            GuestProfileId = request.GuestProfileId,
            CustomerId = request.CustomerId,
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            CurrencyCode = outlet.CurrencyCode,
            OpenedAt = now,
            Note = request.Note,
            IdempotencyKey = request.IdempotencyKey,
        }.StampNew(tenant, userId);

        // Counter and drive-thru guests have no table to be found at, so they get a spoken token.
        if (request.OrderType is OrderType.Counter or OrderType.DriveThru or OrderType.Takeaway)
            order.TokenNumber = await numbering.NextTokenAsync(request.OutletId, now);

        order.Code = order.OrderNumber;
        db.Orders.Add(order);

        db.OrderStatusHistory.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = RestaurantOrderStatus.Draft,
            ToStatus = RestaurantOrderStatus.Open,
            OccurredAt = now,
            StaffId = request.WaiterId,
            StaffName = order.WaiterName,
        }.StampNew(tenant, userId));

        if (request.Delivery is not null)
        {
            db.Deliveries.Add(new RestaurantDelivery
            {
                OrderId = order.Id,
                OutletId = order.OutletId,
                Status = DeliveryStatus.Pending,
                RecipientName = request.Delivery.RecipientName ?? request.CustomerName,
                Phone = request.Delivery.Phone ?? request.CustomerPhone,
                AddressLine = request.Delivery.AddressLine,
                Landmark = request.Delivery.Landmark,
                City = request.Delivery.City,
                Latitude = request.Delivery.Latitude,
                Longitude = request.Delivery.Longitude,
                ZoneName = request.Delivery.ZoneName,
                DeliveryFee = request.Delivery.DeliveryFee,
                DistanceKm = request.Delivery.DistanceKm,
                DeliveryNote = request.Delivery.DeliveryNote,
            }.StampNew(tenant, userId));

            order.DeliveryFeeAmount = request.Delivery.DeliveryFee;
        }

        if (table is not null)
        {
            table.CurrentOrderId = order.Id;
            table.CurrentGuestCount = order.GuestCount;
            table.AssignedWaiterId ??= request.WaiterId;
            if (table.State == TableState.Free || table.State == TableState.Reserved)
            {
                table.State = TableState.Seated;
                table.SeatedAt = now;
                table.StateChangedAt = now;
            }
            table.StampUpdated(userId);
        }

        await db.SaveChangesAsync();

        if (request.Lines.Count > 0)
        {
            return await AddLinesAsync(new AddLinesDto
            {
                OrderId = order.Id,
                WaiterId = request.WaiterId,
                Lines = request.Lines,
                FireImmediately = settings.AutoFireOnSend,
            }, userId);
        }

        return (await GetOrderAsync(order.Id))!;
    }

    // ── Lines ────────────────────────────────────────────────────────────────

    public async Task<RestaurantOrderDto> AddLinesAsync(AddLinesDto request, Guid userId)
    {
        var order = await LoadOrderAsync(request.OrderId)
            ?? throw new InvalidOperationException("Order not found.");

        AssertEditable(order);

        var now = DateTime.UtcNow;
        var settings = await GetSettingsAsync();
        var nextDisplayOrder = order.Lines.Count == 0 ? 0 : order.Lines.Max(l => l.DisplayOrder) + 1;
        var created = new List<RestaurantOrderLine>();

        foreach (var request_line in request.Lines)
        {
            if (request_line.ComboMealId.HasValue)
            {
                created.AddRange(await BuildComboLinesAsync(order, request_line, nextDisplayOrder, userId, now));
                nextDisplayOrder += 1 + request_line.ComboSelections.Count;
                continue;
            }

            var line = await BuildLineAsync(order, request_line, nextDisplayOrder++, userId, now);
            created.Add(line);
        }

        foreach (var line in created) db.OrderLines.Add(line);

        await RecalculateOrderAsync(order, created);
        await db.SaveChangesAsync();

        var toFire = created.Where(l => !l.IsHeld).Select(l => l.Id).ToList();
        if (request.FireImmediately && settings.AutoFireOnSend && toFire.Count > 0)
            await FireLinesAsync(order.Id, toFire, false, request.WaiterId, userId);

        return (await GetOrderAsync(order.Id))!;
    }

    /// <summary>
    /// Turns a requested line into a priced order line. Every money field is computed here from
    /// server-held data; nothing from the request touches price except an open-price override,
    /// and that is only honoured for items explicitly configured as open-price.
    /// </summary>
    private async Task<RestaurantOrderLine> BuildLineAsync(
        RestaurantOrder order, AddOrderLineDto request, int displayOrder, Guid userId, DateTime now)
    {
        var item = await db.MenuItems.ForTenant(tenant)
            .Include(i => i.Variants)
            .FirstOrDefaultAsync(i => i.Id == request.MenuItemId)
            ?? throw new InvalidOperationException("Menu item not found.");

        if (!item.IsAvailable)
            throw new InvalidOperationException($"{item.Name} is currently unavailable (86'd).");

        var variant = request.VariantId.HasValue
            ? item.Variants.FirstOrDefault(v => v.Id == request.VariantId && !v.IsDeleted)
            : null;

        if (request.VariantId.HasValue && variant is null)
            throw new InvalidOperationException("Selected size is not available for this item.");

        var unitPrice = item.IsOpenPrice && request.OverridePrice.HasValue
            ? Math.Max(0, request.OverridePrice.Value)
            : await menu.ResolvePriceAsync(item.Id, variant?.Id, order.OutletId, order.OrderType, now);

        var line = new RestaurantOrderLine
        {
            OrderId = order.Id,
            MenuItemId = item.Id,
            VariantId = variant?.Id,
            ItemName = item.Name,
            VariantName = variant?.Name,
            Quantity = request.Quantity <= 0 ? 1 : request.Quantity,
            UnitPrice = unitPrice,
            UnitCost = variant?.StandardCost > 0 ? variant.StandardCost : item.StandardCost,
            TaxGroupId = item.TaxGroupId,
            TaxPercent = await ResolveTaxPercentAsync(order, item.TaxPercent),
            SeatNumber = request.SeatNumber,
            Course = request.Course ?? item.DefaultCourse,
            CourseSequence = request.CourseSequence ?? CourseSequenceFor(request.Course ?? item.DefaultCourse),
            Status = request.IsHeld ? OrderLineStatus.Held : OrderLineStatus.New,
            IsHeld = request.IsHeld,
            StationId = item.StationId,
            SpecialInstructions = request.SpecialInstructions,
            DisplayOrder = displayOrder,
        }.StampNew(tenant, userId);

        await AttachModifiersAsync(line, request.Modifiers, userId);
        PriceLine(line, await GetSettingsAsync());
        return line;
    }

    /// <summary>
    /// The rate a line is charged at: the dish's own rate if it sets one, otherwise the outlet's
    /// default — and the outlet's separate takeaway rate when the order is not eaten in, because
    /// eat-in and take-out are taxed differently in most jurisdictions that tax food at all.
    /// </summary>
    private async Task<decimal> ResolveTaxPercentAsync(RestaurantOrder order, decimal itemRate)
    {
        if (itemRate > 0) return itemRate;

        var outlet = await db.Outlets.ForTenant(tenant)
            .Where(o => o.Id == order.OutletId)
            .Select(o => new { o.DefaultTaxPercent, o.TakeawayTaxPercent })
            .FirstOrDefaultAsync();

        if (outlet is null) return 0m;

        var eatingIn = order.OrderType is OrderType.DineIn or OrderType.BarTab or OrderType.RoomService;
        if (eatingIn) return outlet.DefaultTaxPercent;

        return outlet.TakeawayTaxPercent > 0 ? outlet.TakeawayTaxPercent : outlet.DefaultTaxPercent;
    }

    /// <summary>
    /// A combo becomes a priced parent line plus zero-priced component lines. The components are
    /// real lines because the kitchen has to cook them and the stock has to be depleted; keeping
    /// the money on the parent is what stops a combo being counted twice in revenue.
    /// </summary>
    private async Task<List<RestaurantOrderLine>> BuildComboLinesAsync(
        RestaurantOrder order, AddOrderLineDto request, int displayOrder, Guid userId, DateTime now)
    {
        var combo = await db.Combos.ForTenant(tenant)
            .Include(c => c.Components).ThenInclude(c => c.Options)
            .FirstOrDefaultAsync(c => c.Id == request.ComboMealId)
            ?? throw new InvalidOperationException("Combo not found.");

        if (!combo.IsAvailable) throw new InvalidOperationException($"{combo.Name} is currently unavailable.");

        var quantity = request.Quantity <= 0 ? 1 : request.Quantity;
        var upcharges = 0m;
        var lines = new List<RestaurantOrderLine>();
        var settings = await GetSettingsAsync();

        var parent = new RestaurantOrderLine
        {
            OrderId = order.Id,
            MenuItemId = combo.MenuItemId ?? combo.Id,
            ComboMealId = combo.Id,
            ItemName = combo.Name,
            Quantity = quantity,
            UnitPrice = combo.Price,
            UnitCost = combo.StandardCost,
            TaxGroupId = combo.TaxGroupId,
            SeatNumber = request.SeatNumber,
            Course = request.Course ?? CourseType.Main,
            CourseSequence = request.CourseSequence ?? CourseSequenceFor(request.Course ?? CourseType.Main),
            Status = request.IsHeld ? OrderLineStatus.Held : OrderLineStatus.New,
            IsHeld = request.IsHeld,
            SpecialInstructions = request.SpecialInstructions,
            DisplayOrder = displayOrder,
        }.StampNew(tenant, userId);

        lines.Add(parent);

        var childOrder = displayOrder + 1;
        foreach (var selection in request.ComboSelections)
        {
            var component = combo.Components.FirstOrDefault(c => c.Id == selection.ComboComponentId)
                ?? throw new InvalidOperationException("Combo component not found.");

            var option = component.Options.FirstOrDefault(o => o.MenuItemId == selection.MenuItemId
                                                            && o.VariantId == selection.VariantId)
                ?? throw new InvalidOperationException($"'{component.Name}' cannot be filled with that item.");

            var item = await db.MenuItems.ForTenant(tenant)
                .Include(i => i.Variants)
                .FirstOrDefaultAsync(i => i.Id == selection.MenuItemId)
                ?? throw new InvalidOperationException("Menu item not found.");

            if (!item.IsAvailable)
                throw new InvalidOperationException($"{item.Name} is currently unavailable (86'd).");

            var variant = selection.VariantId.HasValue
                ? item.Variants.FirstOrDefault(v => v.Id == selection.VariantId && !v.IsDeleted)
                : null;

            upcharges += option.UpchargeAmount * selection.Quantity;

            var child = new RestaurantOrderLine
            {
                OrderId = order.Id,
                MenuItemId = item.Id,
                VariantId = variant?.Id,
                ComboMealId = combo.Id,
                ParentLineId = parent.Id,
                ItemName = item.Name,
                VariantName = variant?.Name,
                Quantity = selection.Quantity * quantity,
                UnitPrice = 0m,                                   // paid for by the parent line
                UnitCost = variant?.StandardCost > 0 ? variant.StandardCost : item.StandardCost,
                SeatNumber = request.SeatNumber,
                Course = item.DefaultCourse,
                CourseSequence = CourseSequenceFor(item.DefaultCourse),
                Status = parent.Status,
                IsHeld = parent.IsHeld,
                StationId = item.StationId,
                DisplayOrder = childOrder++,
            }.StampNew(tenant, userId);

            await AttachModifiersAsync(child, selection.Modifiers, userId);
            PriceLine(child, settings);
            lines.Add(child);
        }

        parent.UnitPrice += upcharges;
        parent.UnitCost += lines.Skip(1).Sum(l => l.UnitCost * l.Quantity) / Math.Max(1, quantity);
        parent.TaxPercent = await ResolveTaxPercentAsync(order, 0m);
        PriceLine(parent, settings);

        return lines;
    }

    private async Task AttachModifiersAsync(RestaurantOrderLine line, List<SelectedModifierDto> selections, Guid userId)
    {
        if (selections.Count == 0) return;

        var ids = selections.Select(s => s.ModifierId).Distinct().ToList();
        var modifiers = await db.Modifiers.ForTenant(tenant)
            .Where(m => ids.Contains(m.Id))
            .Include(m => m.ModifierGroup)
            .ToListAsync();

        foreach (var selection in selections)
        {
            var modifier = modifiers.FirstOrDefault(m => m.Id == selection.ModifierId);
            if (modifier is null) continue;

            if (!modifier.IsAvailable)
                throw new InvalidOperationException($"'{modifier.Name}' is currently unavailable.");

            line.Modifiers.Add(new RestaurantOrderLineModifier
            {
                OrderLineId = line.Id,
                ModifierId = modifier.Id,
                ModifierGroupId = modifier.ModifierGroupId,
                ModifierName = modifier.Name,
                GroupName = modifier.ModifierGroup?.Name,
                Quantity = selection.Quantity <= 0 ? 1 : selection.Quantity,
                PriceDelta = modifier.PriceDelta,
                CostDelta = modifier.CostDelta,
                IsRemoval = modifier.IsRemoval,
                InventoryItemId = modifier.InventoryItemId,
                ConsumptionQuantity = modifier.ConsumptionQuantity,
                ConsumptionUom = modifier.ConsumptionUom,
            }.StampNew(tenant, userId));
        }
    }

    /// <summary>
    /// Recomputes a line's money from its quantity, unit price and modifiers.
    ///
    /// Tax-inclusive pricing is not the same sum run backwards: when the menu price already
    /// contains the tax, the tax is <c>gross × r/(100+r)</c> and the line total stays the menu
    /// price. Treating an inclusive price as exclusive overcharges every guest by the tax rate,
    /// so the two cases are computed separately rather than adjusted afterwards.
    /// </summary>
    private static void PriceLine(RestaurantOrderLine line, RestaurantSettings settings)
    {
        var modifierPerUnit = line.Modifiers.Where(m => !m.IsDeleted).Sum(m => m.PriceDelta * m.Quantity);
        var modifierCostPerUnit = line.Modifiers.Where(m => !m.IsDeleted).Sum(m => m.CostDelta * m.Quantity);

        line.ModifierAmount = Math.Round(modifierPerUnit * line.Quantity, 2);
        line.UnitCost = Math.Round(line.UnitCost + modifierCostPerUnit, 4);

        var gross = Math.Round((line.UnitPrice + modifierPerUnit) * line.Quantity - line.DiscountAmount, 2);
        line.LineTotal = gross;

        var rate = line.TaxPercent;
        if (rate <= 0) { line.TaxAmount = 0m; return; }

        line.TaxAmount = settings.PricesIncludeTax
            ? Math.Round(gross * rate / (100m + rate), 2)
            : Math.Round(gross * rate / 100m, 2);
    }

    /// <summary>Courses fire in this order — the sequence is what "fire the next course" walks.</summary>
    internal static int CourseSequenceFor(CourseType course) => course switch
    {
        CourseType.Beverage => 1,
        CourseType.Appetizer => 2,
        CourseType.Soup => 2,
        CourseType.Salad => 3,
        CourseType.Main => 4,
        CourseType.Side => 4,
        CourseType.Dessert => 5,
        _ => 4,
    };

    public async Task<RestaurantOrderDto> UpdateLineAsync(Guid lineId, UpdateOrderLineDto request, Guid userId)
    {
        var line = await db.OrderLines.ForTenant(tenant)
            .Include(l => l.Modifiers)
            .FirstOrDefaultAsync(l => l.Id == lineId)
            ?? throw new InvalidOperationException("Order line not found.");

        var order = await LoadOrderAsync(line.OrderId)
            ?? throw new InvalidOperationException("Order not found.");

        AssertEditable(order);

        if (line.IsVoided) throw new InvalidOperationException("This line has been voided.");

        // Once the kitchen has it, the quantity and the recipe are already committed. Changing
        // them silently would leave the pass cooking one thing and the bill charging another.
        var alreadyFired = line.Status is OrderLineStatus.Fired or OrderLineStatus.Preparing
                                       or OrderLineStatus.Ready or OrderLineStatus.Served;

        if (alreadyFired && (request.Quantity.HasValue || request.Modifiers is not null))
            throw new InvalidOperationException(
                "This line is already with the kitchen. Void it and re-enter it instead.");

        if (request.Quantity.HasValue)
        {
            if (request.Quantity.Value <= 0) throw new InvalidOperationException("Quantity must be greater than zero.");
            line.Quantity = request.Quantity.Value;
        }

        if (request.SeatNumber.HasValue) line.SeatNumber = request.SeatNumber;
        if (request.Course.HasValue)
        {
            line.Course = request.Course.Value;
            line.CourseSequence = CourseSequenceFor(request.Course.Value);
        }
        if (request.IsHeld.HasValue && !alreadyFired)
        {
            line.IsHeld = request.IsHeld.Value;
            line.Status = request.IsHeld.Value ? OrderLineStatus.Held : OrderLineStatus.New;
        }
        if (request.SpecialInstructions is not null) line.SpecialInstructions = request.SpecialInstructions;

        if (request.Modifiers is not null)
        {
            foreach (var modifier in line.Modifiers.Where(m => !m.IsDeleted))
                modifier.StampDeleted(userId);

            line.Modifiers.Clear();
            await AttachModifiersAsync(line, request.Modifiers, userId);
        }

        PriceLine(line, await GetSettingsAsync());
        line.StampUpdated(userId);

        await RecalculateOrderAsync(order, []);
        await db.SaveChangesAsync();
        return (await GetOrderAsync(order.Id))!;
    }

    public async Task<RestaurantOrderDto> VoidLineAsync(VoidLineDto request, Guid userId)
    {
        var line = await db.OrderLines.ForTenant(tenant)
            .Include(l => l.Modifiers)
            .FirstOrDefaultAsync(l => l.Id == request.OrderLineId)
            ?? throw new InvalidOperationException("Order line not found.");

        if (line.IsVoided) throw new InvalidOperationException("This line is already voided.");

        var order = await LoadOrderAsync(line.OrderId)
            ?? throw new InvalidOperationException("Order not found.");

        if (order.Status is RestaurantOrderStatus.Closed or RestaurantOrderStatus.Cancelled)
            throw new InvalidOperationException("This order is closed.");

        var settings = await GetSettingsAsync();
        VoidReason? reason = null;

        if (request.VoidReasonId.HasValue)
            reason = await db.VoidReasons.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == request.VoidReasonId);
        else if (settings.VoidRequiresReason)
            throw new InvalidOperationException("A void reason is required.");

        var wasFired = line.Status is OrderLineStatus.Fired or OrderLineStatus.Preparing
                                   or OrderLineStatus.Ready or OrderLineStatus.Served;

        line.IsVoided = true;
        line.Status = OrderLineStatus.Voided;
        line.VoidReasonId = request.VoidReasonId;
        line.VoidNote = request.Note;
        line.VoidedByStaffId = request.StaffId;
        line.VoidedAt = DateTime.UtcNow;
        line.WasFiredWhenVoided = wasFired;
        line.LineTotal = 0m;
        line.StampUpdated(userId);

        // Cancelling the line on the kitchen screens matters as much as cancelling the charge:
        // otherwise the pass keeps cooking a dish nobody is paying for.
        var ticketLines = await db.KitchenTicketLines.ForTenant(tenant)
            .Where(t => t.OrderLineId == line.Id)
            .ToListAsync();

        foreach (var ticketLine in ticketLines)
        {
            ticketLine.Status = OrderLineStatus.Voided;
            ticketLine.StampUpdated(userId);
        }

        // Food that was already cooked and is now going in the bin is a wastage event, not a
        // correction. Recording it here is what keeps theoretical-vs-actual food cost honest.
        if (wasFired && settings.TrackWastage && (reason?.CountsAsWastage ?? true))
        {
            db.WastageLogs.Add(new WastageLog
            {
                OutletId = order.OutletId,
                OccurredAt = DateTime.UtcNow,
                Reason = WastageReason.GuestReturn,
                MenuItemId = line.MenuItemId,
                ItemName = line.ItemName + (line.VariantName is null ? "" : $" ({line.VariantName})"),
                Quantity = line.Quantity,
                Uom = "portion",
                UnitCost = line.UnitCost,
                TotalCost = Math.Round(line.UnitCost * line.Quantity, 2),
                StaffId = request.StaffId,
                OrderLineId = line.Id,
                StationId = line.StationId,
                Note = request.Note ?? reason?.Name,
            }.StampNew(tenant, userId));
        }

        await RecalculateOrderAsync(order, []);
        await db.SaveChangesAsync();

        logger.LogInformation("Voided line {LineId} on order {OrderNumber} (fired: {Fired})",
            line.Id, order.OrderNumber, wasFired);

        return (await GetOrderAsync(order.Id))!;
    }

    public async Task<RestaurantOrderDto> MoveLinesAsync(MoveLinesDto request, Guid userId)
    {
        var source = await LoadOrderAsync(request.FromOrderId)
            ?? throw new InvalidOperationException("Source order not found.");
        var target = await LoadOrderAsync(request.ToOrderId)
            ?? throw new InvalidOperationException("Destination order not found.");

        AssertEditable(target);

        var lines = source.Lines.Where(l => request.LineIds.Contains(l.Id) && !l.IsVoided).ToList();
        if (lines.Count == 0) throw new InvalidOperationException("No movable lines were selected.");

        var nextDisplayOrder = target.Lines.Count == 0 ? 0 : target.Lines.Max(l => l.DisplayOrder) + 1;

        foreach (var line in lines)
        {
            line.OrderId = target.Id;
            line.DisplayOrder = nextDisplayOrder++;
            if (request.ToSeatNumber.HasValue) line.SeatNumber = request.ToSeatNumber;
            line.StampUpdated(userId);

            // The kitchen has to learn the new table, or the runner delivers to the old one.
            var ticketLines = await db.KitchenTicketLines.ForTenant(tenant)
                .Where(t => t.OrderLineId == line.Id).Include(t => t.Ticket).ToListAsync();

            foreach (var ticketLine in ticketLines.Where(t => t.Ticket is not null))
            {
                ticketLine.Ticket!.TableNumber = target.TableNumber;
                ticketLine.Ticket.StampUpdated(userId);
            }
        }

        await db.SaveChangesAsync();

        var reloadedSource = await LoadOrderAsync(source.Id);
        if (reloadedSource is not null) await RecalculateOrderAsync(reloadedSource, []);
        var reloadedTarget = await LoadOrderAsync(target.Id);
        if (reloadedTarget is not null) await RecalculateOrderAsync(reloadedTarget, []);
        await db.SaveChangesAsync();

        return (await GetOrderAsync(target.Id))!;
    }

    // ── Firing ───────────────────────────────────────────────────────────────

    public async Task<RestaurantOrderDto> FireCourseAsync(FireCourseDto request, Guid userId)
    {
        var order = await LoadOrderAsync(request.OrderId)
            ?? throw new InvalidOperationException("Order not found.");

        AssertEditable(order);

        var candidates = order.Lines
            .Where(l => !l.IsVoided && l.Status is OrderLineStatus.New or OrderLineStatus.Held)
            .ToList();

        if (request.Course.HasValue)
            candidates = candidates.Where(l => l.Course == request.Course.Value).ToList();
        else if (request.LineIds.Count > 0)
            candidates = candidates.Where(l => request.LineIds.Contains(l.Id)).ToList();

        if (candidates.Count == 0)
            throw new InvalidOperationException("There is nothing waiting to be fired.");

        await FireLinesAsync(order.Id, candidates.Select(l => l.Id).ToList(),
                             request.IsPriority, request.StaffId, userId);

        return (await GetOrderAsync(order.Id))!;
    }

    /// <summary>
    /// Marks lines fired and asks the kitchen service to build the tickets. Ticket creation is
    /// delegated because station routing is a kitchen concern; ordering only decides *when*.
    /// </summary>
    private async Task FireLinesAsync(Guid orderId, List<Guid> lineIds, bool isPriority, Guid? staffId, Guid userId)
    {
        var now = DateTime.UtcNow;

        var order = await db.Orders.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null) return;

        var lines = await db.OrderLines.ForTenant(tenant)
            .Where(l => lineIds.Contains(l.Id) && !l.IsVoided)
            .ToListAsync();

        foreach (var line in lines)
        {
            line.IsHeld = false;
            line.Status = OrderLineStatus.Fired;
            line.FiredAt = now;
            line.StampUpdated(userId);
        }

        order.FirstFiredAt ??= now;
        if (order.Status == RestaurantOrderStatus.Open) SetStatus(order, RestaurantOrderStatus.Fired, staffId, userId);
        order.StampUpdated(userId);

        // A fired course means the table has ordered; the floor plan should say so.
        if (order.TableId.HasValue)
        {
            var table = await db.Tables.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == order.TableId);
            if (table is not null && table.State is TableState.Seated)
            {
                table.State = TableState.Ordered;
                table.StateChangedAt = now;
                table.StampUpdated(userId);
            }
        }

        await db.SaveChangesAsync();
        await kitchen.CreateTicketsAsync(orderId, lines.Select(l => l.Id).ToList(), isPriority, userId);
    }

    public async Task<RestaurantOrderDto> MarkServedAsync(Guid orderId, List<Guid> lineIds, Guid userId)
    {
        var order = await LoadOrderAsync(orderId) ?? throw new InvalidOperationException("Order not found.");
        var now = DateTime.UtcNow;

        var lines = order.Lines
            .Where(l => !l.IsVoided && (lineIds.Count == 0 || lineIds.Contains(l.Id)))
            .Where(l => l.Status is OrderLineStatus.Fired or OrderLineStatus.Preparing or OrderLineStatus.Ready)
            .ToList();

        foreach (var line in lines)
        {
            line.Status = OrderLineStatus.Served;
            line.ServedAt = now;
            line.StampUpdated(userId);
        }

        var outstanding = order.Lines.Any(l => !l.IsVoided && l.Status != OrderLineStatus.Served);

        if (!outstanding && order.Status is RestaurantOrderStatus.Fired or RestaurantOrderStatus.PartiallyServed)
        {
            SetStatus(order, RestaurantOrderStatus.Served, null, userId);
            order.ServedAt = now;

            if (order.TableId.HasValue)
            {
                var table = await db.Tables.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == order.TableId);
                if (table is not null && table.State is TableState.Ordered)
                {
                    table.State = TableState.Served;
                    table.StateChangedAt = now;
                    table.StampUpdated(userId);
                }
            }
        }
        else if (outstanding && order.Status == RestaurantOrderStatus.Fired)
        {
            SetStatus(order, RestaurantOrderStatus.PartiallyServed, null, userId);
        }

        await db.SaveChangesAsync();
        return (await GetOrderAsync(order.Id))!;
    }

    // ── Order-level changes ──────────────────────────────────────────────────

    public async Task<RestaurantOrderDto> UpdateOrderAsync(Guid orderId, UpdateOrderDto request, Guid userId)
    {
        var order = await LoadOrderAsync(orderId) ?? throw new InvalidOperationException("Order not found.");
        AssertEditable(order);

        if (request.GuestCount.HasValue) order.GuestCount = Math.Max(1, request.GuestCount.Value);
        if (request.GuestProfileId.HasValue) order.GuestProfileId = request.GuestProfileId;
        if (request.CustomerId.HasValue) order.CustomerId = request.CustomerId;
        if (request.CustomerName is not null) order.CustomerName = request.CustomerName;
        if (request.CustomerPhone is not null) order.CustomerPhone = request.CustomerPhone;
        if (request.Note is not null) order.Note = request.Note;

        if (request.WaiterId.HasValue)
        {
            var waiter = await db.Staff.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == request.WaiterId);
            order.WaiterId = request.WaiterId;
            order.WaiterName = waiter?.DisplayName ?? waiter?.FullName;
        }

        // Changing order type changes the price scope, so every unfired line has to be re-priced.
        // Leaving old prices in place is how a dine-in bill ends up on a delivery order.
        if (request.OrderType.HasValue && request.OrderType.Value != order.OrderType)
        {
            order.OrderType = request.OrderType.Value;
            var now = DateTime.UtcNow;
            var settings = await GetSettingsAsync();

            foreach (var line in order.Lines.Where(l => !l.IsVoided && l.ComboMealId is null))
            {
                line.UnitPrice = await menu.ResolvePriceAsync(
                    line.MenuItemId, line.VariantId, order.OutletId, order.OrderType, now);

                // The rate moves with the order type too — an order switched from dine-in to
                // takeaway is taxed at the takeaway rate, not the one it was opened under.
                var item = await db.MenuItems.ForTenant(tenant)
                    .Where(i => i.Id == line.MenuItemId).Select(i => i.TaxPercent).FirstOrDefaultAsync();
                line.TaxPercent = await ResolveTaxPercentAsync(order, item);

                PriceLine(line, settings);
                line.StampUpdated(userId);
            }
        }

        order.StampUpdated(userId);

        if (order.TableId.HasValue)
        {
            var table = await db.Tables.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == order.TableId);
            if (table is not null)
            {
                table.CurrentGuestCount = order.GuestCount;
                if (request.WaiterId.HasValue) table.AssignedWaiterId = request.WaiterId;
                table.StampUpdated(userId);
            }
        }

        await RecalculateOrderAsync(order, []);
        await db.SaveChangesAsync();
        return (await GetOrderAsync(order.Id))!;
    }

    public async Task<RestaurantOrderDto> CancelOrderAsync(Guid orderId, CancelOrderDto request, Guid userId)
    {
        var order = await LoadOrderAsync(orderId) ?? throw new InvalidOperationException("Order not found.");

        if (order.Status is RestaurantOrderStatus.Closed)
            throw new InvalidOperationException("A closed order cannot be cancelled.");

        if (order.PaidAmount > 0)
            throw new InvalidOperationException("This order has payments against it. Refund them before cancelling.");

        var now = DateTime.UtcNow;

        foreach (var line in order.Lines.Where(l => !l.IsVoided))
        {
            line.IsVoided = true;
            line.Status = OrderLineStatus.Voided;
            line.VoidNote = request.Reason;
            line.VoidedAt = now;
            line.VoidedByStaffId = request.StaffId;
            line.LineTotal = 0m;
            line.StampUpdated(userId);
        }

        var tickets = await db.KitchenTickets.ForTenant(tenant)
            .Where(t => t.OrderId == order.Id && t.Status != KitchenTicketStatus.Bumped)
            .ToListAsync();

        foreach (var ticket in tickets)
        {
            ticket.Status = KitchenTicketStatus.Cancelled;
            ticket.StampUpdated(userId);
        }

        SetStatus(order, RestaurantOrderStatus.Cancelled, request.StaffId, userId, request.Reason);
        order.CancelReason = request.Reason;
        order.ClosedAt = now;
        order.SubTotal = order.TotalAmount = order.TaxAmount = order.ServiceChargeAmount = 0m;
        order.StampUpdated(userId);

        if (order.TableId.HasValue)
        {
            var table = await db.Tables.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == order.TableId);
            if (table is not null)
            {
                table.CurrentOrderId = null;
                table.CurrentGuestCount = 0;
                table.SeatedAt = null;
                table.State = TableState.NeedsCleaning;
                table.StateChangedAt = now;
                table.StampUpdated(userId);
            }
        }

        await db.SaveChangesAsync();
        return (await GetOrderAsync(order.Id))!;
    }

    public async Task<RestaurantDeliveryDto> UpdateDeliveryAsync(Guid orderId, UpdateDeliveryStatusDto request, Guid userId)
    {
        var delivery = await db.Deliveries.ForTenant(tenant).FirstOrDefaultAsync(d => d.OrderId == orderId)
            ?? throw new InvalidOperationException("This order has no delivery record.");

        var now = DateTime.UtcNow;
        delivery.Status = request.Status;

        if (request.RiderId.HasValue) delivery.RiderId = request.RiderId;
        if (request.RiderName is not null) delivery.RiderName = request.RiderName;
        if (request.RiderPhone is not null) delivery.RiderPhone = request.RiderPhone;
        if (request.FailureReason is not null) delivery.FailureReason = request.FailureReason;

        switch (request.Status)
        {
            case DeliveryStatus.Assigned: delivery.AssignedAt ??= now; break;
            case DeliveryStatus.PickedUp: delivery.PickedUpAt ??= now; break;
            case DeliveryStatus.Delivered: delivery.DeliveredAt ??= now; break;
        }

        delivery.StampUpdated(userId);
        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(delivery);
    }

    // ── Totals ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Recomputes the order's money from its lines: subtotal, service charge, packaging, tax and
    /// total. Called after every change so the running total on the waiter's screen is always the
    /// number the guest will be asked for.
    /// </summary>
    private async Task RecalculateOrderAsync(RestaurantOrder order, List<RestaurantOrderLine> pending)
    {
        var settings = await GetSettingsAsync();

        var lines = order.Lines.Where(l => !l.IsDeleted && !l.IsVoided)
            .Concat(pending.Where(l => !l.IsVoided))
            .Distinct()
            .ToList();

        order.SubTotal = Math.Round(lines.Sum(l => l.LineTotal), 2);
        order.CostAmount = Math.Round(lines.Sum(l => l.UnitCost * l.Quantity), 2);

        order.ServiceChargeAmount = await ResolveServiceChargeAsync(order, order.SubTotal);

        order.PackagingChargeAmount = order.OrderType is OrderType.Takeaway or OrderType.Delivery or OrderType.Curbside
            ? settings.PackagingChargePerOrder
            : 0m;

        var chargeable = order.SubTotal - order.DiscountAmount + order.ServiceChargeAmount
                       + order.PackagingChargeAmount + order.DeliveryFeeAmount;

        order.TaxAmount = Math.Round(lines.Sum(l => l.TaxAmount), 2);

        // Tax-inclusive menus have already collected the tax inside the line totals — adding it
        // again would charge every guest the rate twice. Exclusive menus add it on top.
        order.TotalAmount = settings.PricesIncludeTax
            ? Math.Round(chargeable + order.RoundingAmount, 2)
            : Math.Round(chargeable + order.TaxAmount + order.RoundingAmount, 2);
    }

    private async Task<decimal> ResolveServiceChargeAsync(RestaurantOrder order, decimal subTotal)
    {
        var rules = await db.ServiceChargeRules.ForTenant(tenant)
            .Where(r => r.IsActive && (r.OutletId == null || r.OutletId == order.OutletId))
            .OrderBy(r => r.Priority)
            .ToListAsync();

        var rule = rules.FirstOrDefault(r =>
            order.GuestCount >= r.MinPartySize && AppliesToOrderType(r.ApplicableOrderTypes, order.OrderType));

        if (rule is null) return 0m;

        return Math.Round(rule.Basis switch
        {
            ServiceChargeBasis.Percentage => subTotal * rule.Value / 100m,
            ServiceChargeBasis.FixedAmount => rule.Value,
            ServiceChargeBasis.PerCover => rule.Value * order.GuestCount,
            _ => 0m,
        }, 2);
    }

    /// <summary>
    /// An empty applicability list means dine-in only. Service charge defaulting to "everything"
    /// would quietly add it to takeaway, which guests notice and complain about.
    /// </summary>
    internal static bool AppliesToOrderType(string? csv, OrderType orderType)
    {
        if (string.IsNullOrWhiteSpace(csv)) return orderType == OrderType.DineIn;

        var values = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return values.Contains(((int)orderType).ToString());
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Task<RestaurantOrder?> LoadOrderAsync(Guid orderId) =>
        db.Orders.ForTenant(tenant)
            .Include(o => o.Lines.Where(l => !l.IsDeleted)).ThenInclude(l => l.Modifiers.Where(m => !m.IsDeleted))
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId);

    /// <summary>Fills the names and lookups the DTO carries but the entity graph does not.</summary>
    private async Task DecorateAsync(RestaurantOrderDto dto)
    {
        var stationIds = dto.Lines.Where(l => l.StationId.HasValue).Select(l => l.StationId!.Value).Distinct().ToList();
        if (stationIds.Count > 0)
        {
            var stations = await db.Stations.ForTenant(tenant)
                .Where(s => stationIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name);

            foreach (var line in dto.Lines.Where(l => l.StationId.HasValue))
                line.StationName = stations.GetValueOrDefault(line.StationId!.Value);
        }

        var reasonIds = dto.Lines.Where(l => l.VoidReasonId.HasValue).Select(l => l.VoidReasonId!.Value).Distinct().ToList();
        if (reasonIds.Count > 0)
        {
            var reasons = await db.VoidReasons.ForTenant(tenant)
                .Where(r => reasonIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.Name);

            foreach (var line in dto.Lines.Where(l => l.VoidReasonId.HasValue))
                line.VoidReasonName = reasons.GetValueOrDefault(line.VoidReasonId!.Value);
        }

        var itemIds = dto.Lines.Select(l => l.MenuItemId).Distinct().ToList();
        if (itemIds.Count > 0)
        {
            var items = await db.MenuItems.ForTenant(tenant)
                .Where(i => itemIds.Contains(i.Id))
                .Select(i => new { i.Id, i.ImageUrl, i.Allergens, i.ContainsNuts, i.ContainsShellfish })
                .ToListAsync();

            foreach (var line in dto.Lines)
            {
                var item = items.FirstOrDefault(i => i.Id == line.MenuItemId);
                if (item is null) continue;

                line.ImageUrl = item.ImageUrl;
                var warnings = new List<string>();
                if (item.ContainsNuts) warnings.Add("nuts");
                if (item.ContainsShellfish) warnings.Add("shellfish");
                if (!string.IsNullOrWhiteSpace(item.Allergens)) warnings.Add(item.Allergens!);
                if (warnings.Count > 0) line.AllergenWarning = string.Join(", ", warnings);
            }
        }

        if (dto.SectionId.HasValue)
            dto.SectionName = await db.Sections.ForTenant(tenant)
                .Where(s => s.Id == dto.SectionId).Select(s => s.Name).FirstOrDefaultAsync();

        dto.Deliveries = await db.Deliveries.ForTenant(tenant)
            .Where(d => d.OrderId == dto.Id)
            .Select(d => RestaurantMapper.ToDto(d))
            .ToListAsync();
    }

    private void SetStatus(RestaurantOrder order, RestaurantOrderStatus next, Guid? staffId, Guid userId, string? note = null)
    {
        if (order.Status == next) return;

        db.OrderStatusHistory.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = order.Status,
            ToStatus = next,
            OccurredAt = DateTime.UtcNow,
            StaffId = staffId,
            StaffName = order.WaiterName,
            Note = note,
        }.StampNew(tenant, userId));

        order.Status = next;
    }

    private static void AssertEditable(RestaurantOrder order)
    {
        if (order.Status is RestaurantOrderStatus.Closed or RestaurantOrderStatus.Cancelled)
            throw new InvalidOperationException("This order is closed and can no longer be changed.");

        if (order.Status is RestaurantOrderStatus.Paid)
            throw new InvalidOperationException("This order has been paid. Open a new order for anything further.");
    }

    private RestaurantSettings? _settings;

    private async Task<RestaurantSettings> GetSettingsAsync()
        => _settings ??= await db.Settings.ForTenant(tenant).FirstOrDefaultAsync() ?? new RestaurantSettings();
}
