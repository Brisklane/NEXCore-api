using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Events;
using Restaurant.Application.DTOs;
using Restaurant.Application.Services.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

/// <summary>
/// Checks: carving an order into bills, taking money against them, and closing the order when
/// the last one is settled.
///
/// The splitting rules are the reason this is a service rather than a controller with a mapper.
/// A split must be *exhaustive and exact*: every line of the order lands on exactly one check,
/// and the checks add up to the order to the cent. Letting a client compute the split would put
/// that guarantee on the far side of the network, where a rounding difference becomes money the
/// restaurant never collects.
/// </summary>
public class CheckService(
    RestaurantDbContext db,
    IRestaurantTenant tenant,
    RestaurantNumbering numbering,
    IRestaurantStaffService staff,
    IEventPublisher events,
    ILogger<CheckService> logger) : ICheckService
{
    // ── Reads ────────────────────────────────────────────────────────────────

    public async Task<RestaurantCheckDto?> GetCheckAsync(Guid checkId)
    {
        var check = await LoadCheckAsync(checkId);
        if (check is null) return null;

        var dto = RestaurantMapper.ToDto(check);
        await DecorateAsync([dto]);
        return dto;
    }

    public async Task<List<RestaurantCheckDto>> GetChecksForOrderAsync(Guid orderId)
    {
        var checks = await db.Checks.ForTenant(tenant)
            .Where(c => c.OrderId == orderId)
            .Include(c => c.Lines.Where(l => !l.IsDeleted))
            .Include(c => c.Payments.Where(p => !p.IsDeleted))
            .Include(c => c.Discounts.Where(d => !d.IsDeleted))
            .OrderBy(c => c.SplitIndex)
            .ToListAsync();

        var dtos = checks.Select(RestaurantMapper.ToDto).ToList();
        await DecorateAsync(dtos);
        return dtos;
    }

    public async Task<PaginatedResponse<RestaurantCheckDto>> ListChecksAsync(
        Guid? outletId, CheckStatus? status, DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.Checks.ForTenant(tenant)
            .WhereIf(outletId.HasValue, c => c.OutletId == outletId)
            .WhereIf(status.HasValue, c => c.Status == status)
            .WhereIf(from.HasValue, c => c.CreatedAt >= from)
            .WhereIf(to.HasValue, c => c.CreatedAt <= to)
            .WhereIf(!string.IsNullOrWhiteSpace(pagination.SearchTerm),
                c => c.CheckNumber.ToLower().Contains(pagination.SearchTerm!.ToLower()));

        var total = await query.CountAsync();

        var checks = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Include(c => c.Payments.Where(p => !p.IsDeleted))
            .ToListAsync();

        var dtos = checks.Select(RestaurantMapper.ToDto).ToList();
        await DecorateAsync(dtos);

        return PaginatedResponse<RestaurantCheckDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    // ── Splitting ────────────────────────────────────────────────────────────

    public async Task<List<RestaurantCheckDto>> CreateChecksAsync(CreateChecksDto request, Guid userId)
    {
        var order = await db.Orders.ForTenant(tenant)
            .Include(o => o.Lines.Where(l => !l.IsDeleted))
            .ThenInclude(l => l.Modifiers.Where(m => !m.IsDeleted))
            .FirstOrDefaultAsync(o => o.Id == request.OrderId)
            ?? throw new InvalidOperationException("Order not found.");

        if (order.Status is RestaurantOrderStatus.Cancelled)
            throw new InvalidOperationException("A cancelled order cannot be billed.");

        var existing = await db.Checks.ForTenant(tenant)
            .Where(c => c.OrderId == order.Id && !c.IsVoided)
            .Include(c => c.Payments.Where(p => !p.IsDeleted))
            .Include(c => c.Lines.Where(l => !l.IsDeleted))
            .ToListAsync();

        // Re-splitting after money has changed hands would strand a payment against a check that
        // no longer exists. The guest has to be refunded first — refuse rather than orphan it.
        if (existing.Any(c => c.PaidAmount > 0))
            throw new InvalidOperationException(
                "This order already has payments against it. Refund them before re-splitting.");

        if (request.ReplaceExisting)
            foreach (var check in existing) check.StampDeleted(userId);

        var billable = order.Lines
            .Where(l => !l.IsVoided && l.ParentLineId is null)
            .OrderBy(l => l.DisplayOrder)
            .ToList();

        if (billable.Count == 0)
            throw new InvalidOperationException("There is nothing on this order to bill.");

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync() ?? new RestaurantSettings();
        var now = DateTime.UtcNow;

        var whole = new List<List<LineSlice>> { billable.Select(l => new LineSlice(l, 1m)).ToList() };

        var buckets = request.SplitMethod switch
        {
            SplitMethod.None => whole,
            SplitMethod.BySeat => SplitBySeat(billable, request.Parts),
            SplitMethod.ByItem => SplitByItem(billable, request.Parts),
            SplitMethod.Evenly => SplitEvenly(billable, Math.Max(1, request.SplitCount)),
            SplitMethod.ByAmount => SplitByProportion(billable, ProportionsFromAmounts(billable, request.Parts)),
            SplitMethod.ByPercentage => SplitByProportion(billable, ProportionsFromPercentages(request.Parts)),
            _ => whole,
        };

        buckets = buckets.Where(b => b.Count > 0).ToList();
        if (buckets.Count == 0) throw new InvalidOperationException("That split produced no billable checks.");

        var created = new List<RestaurantCheck>();

        for (var index = 0; index < buckets.Count; index++)
        {
            var bucket = buckets[index];

            var check = new RestaurantCheck
            {
                OrderId = order.Id,
                OutletId = order.OutletId,
                CheckNumber = await numbering.NextCheckNumberAsync(order.OutletId, now),
                Status = CheckStatus.Open,
                SplitMethod = request.SplitMethod,
                SplitIndex = index + 1,
                SplitCount = buckets.Count,
                SeatNumbers = string.Join(",", bucket.Select(l => l.Line.SeatNumber)
                    .Where(s => s.HasValue).Select(s => s!.Value).Distinct().OrderBy(s => s)),
                CurrencyCode = order.CurrencyCode,
                SessionId = request.SessionId ?? order.SessionId,
                CashierId = request.CashierId,
                WaiterId = order.WaiterId,
            }.StampNew(tenant, userId);

            check.Code = check.CheckNumber;

            var displayOrder = 0;
            foreach (var slice in bucket)
            {
                check.Lines.Add(new CheckLine
                {
                    CheckId = check.Id,
                    OrderLineId = slice.Line.Id,
                    MenuItemId = slice.Line.MenuItemId,
                    ItemName = slice.Line.ItemName,
                    VariantName = slice.Line.VariantName,
                    ModifierSummary = SummariseModifiers(slice.Line),
                    SeatNumber = slice.Line.SeatNumber,
                    Quantity = Math.Round(slice.Line.Quantity * slice.Share, 3),
                    UnitPrice = slice.Line.UnitPrice + PerUnitModifiers(slice.Line),
                    DiscountAmount = Math.Round(slice.Line.DiscountAmount * slice.Share, 2),
                    TaxAmount = Math.Round(slice.Line.TaxAmount * slice.Share, 2),
                    LineTotal = Math.Round(slice.Line.LineTotal * slice.Share, 2),
                    UnitCost = slice.Line.UnitCost,
                    DisplayOrder = displayOrder++,
                }.StampNew(tenant, userId));
            }

            RecalculateCheck(check, order, settings, buckets.Count);
            db.Checks.Add(check);
            created.Add(check);
        }

        // A rounding remainder has to land somewhere. Putting it on the first check is arbitrary
        // but deliberate: the alternative is checks that quietly do not sum to the order.
        ReconcileSplitRounding(created, order, settings);

        order.BilledAt ??= now;
        if (order.Status is RestaurantOrderStatus.Open or RestaurantOrderStatus.Fired
                          or RestaurantOrderStatus.PartiallyServed or RestaurantOrderStatus.Served)
        {
            db.OrderStatusHistory.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = order.Status,
                ToStatus = RestaurantOrderStatus.Billed,
                OccurredAt = now,
            }.StampNew(tenant, userId));

            order.Status = RestaurantOrderStatus.Billed;
        }
        order.StampUpdated(userId);

        if (order.TableId.HasValue)
        {
            var table = await db.Tables.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == order.TableId);
            if (table is not null && table.State is TableState.Ordered or TableState.Served)
            {
                table.State = TableState.BillPrinted;
                table.StateChangedAt = now;
                table.StampUpdated(userId);
            }
        }

        await db.SaveChangesAsync();

        var dtos = created.Select(RestaurantMapper.ToDto).ToList();
        await DecorateAsync(dtos);
        return dtos;
    }

    /// <summary>A line, and what fraction of it belongs to this check (1 for a whole line).</summary>
    private record LineSlice(RestaurantOrderLine Line, decimal Share);

    private static List<List<LineSlice>> SplitBySeat(List<RestaurantOrderLine> lines, List<CheckSplitPartDto> parts)
    {
        if (parts.Count > 0)
        {
            var buckets = parts
                .Select(p => lines.Where(l => l.SeatNumber.HasValue && p.SeatNumbers.Contains(l.SeatNumber.Value))
                                  .Select(l => new LineSlice(l, 1m)).ToList())
                .ToList();

            // Anything not attributed to a seat still has to be paid for. Shared sides and the
            // bottle of wine go on the first check rather than falling off the bill entirely.
            var assigned = buckets.SelectMany(b => b.Select(s => s.Line.Id)).ToHashSet();
            var orphans = lines.Where(l => !assigned.Contains(l.Id)).Select(l => new LineSlice(l, 1m)).ToList();

            if (orphans.Count > 0)
            {
                if (buckets.Count == 0) buckets.Add(orphans);
                else buckets[0].AddRange(orphans);
            }

            return buckets;
        }

        // No explicit parts: one check per seat that actually ordered something.
        return lines
            .GroupBy(l => l.SeatNumber ?? 0)
            .OrderBy(g => g.Key)
            .Select(g => g.Select(l => new LineSlice(l, 1m)).ToList())
            .ToList();
    }

    private static List<List<LineSlice>> SplitByItem(List<RestaurantOrderLine> lines, List<CheckSplitPartDto> parts)
    {
        var buckets = parts
            .Select(p => lines.Where(l => p.OrderLineIds.Contains(l.Id))
                              .Select(l => new LineSlice(l, 1m)).ToList())
            .ToList();

        var assigned = buckets.SelectMany(b => b.Select(s => s.Line.Id)).ToHashSet();
        var orphans = lines.Where(l => !assigned.Contains(l.Id)).Select(l => new LineSlice(l, 1m)).ToList();

        if (orphans.Count > 0)
        {
            if (buckets.Count == 0) buckets.Add(orphans);
            else buckets[0].AddRange(orphans);
        }

        return buckets;
    }

    private static List<List<LineSlice>> SplitEvenly(List<RestaurantOrderLine> lines, int ways)
        => Enumerable.Range(0, ways)
            .Select(_ => lines.Select(l => new LineSlice(l, 1m / ways)).ToList())
            .ToList();

    private static List<decimal> ProportionsFromAmounts(List<RestaurantOrderLine> lines, List<CheckSplitPartDto> parts)
    {
        var total = lines.Sum(l => l.LineTotal);
        if (total <= 0) return parts.Select(_ => 1m / Math.Max(1, parts.Count)).ToList();

        var shares = parts.Select(p => (p.Amount ?? 0m) / total).ToList();

        // Whatever the named amounts did not cover is still owed; it becomes a final check rather
        // than vanishing from the bill.
        var covered = shares.Sum();
        if (covered < 0.9999m) shares.Add(1m - covered);

        return shares;
    }

    private static List<decimal> ProportionsFromPercentages(List<CheckSplitPartDto> parts)
    {
        var shares = parts.Select(p => (p.Percentage ?? 0m) / 100m).ToList();
        var covered = shares.Sum();
        if (covered < 0.9999m) shares.Add(1m - covered);
        return shares;
    }

    private static List<List<LineSlice>> SplitByProportion(List<RestaurantOrderLine> lines, List<decimal> shares)
        => shares.Where(s => s > 0)
            .Select(share => lines.Select(l => new LineSlice(l, share)).ToList())
            .ToList();

    /// <summary>
    /// Fixes the cent that fractional splitting loses. Three-way splitting a $10.00 order gives
    /// three $3.33 checks and leaves a cent on the table; it is added to the first check so the
    /// checks always sum to the order.
    /// </summary>
    private static void ReconcileSplitRounding(List<RestaurantCheck> checks, RestaurantOrder order, RestaurantSettings settings)
    {
        if (checks.Count < 2) return;

        var difference = Math.Round(order.TotalAmount - checks.Sum(c => c.TotalAmount), 2);
        if (difference == 0m) return;

        var first = checks[0];
        first.RoundingAmount += difference;
        first.TotalAmount = Math.Round(first.TotalAmount + difference, 2);
    }

    private void RecalculateCheck(RestaurantCheck check, RestaurantOrder order, RestaurantSettings settings, int splitCount)
    {
        var lines = check.Lines.Where(l => !l.IsDeleted).ToList();

        check.SubTotal = Math.Round(lines.Sum(l => l.LineTotal), 2);
        check.TaxAmount = Math.Round(lines.Sum(l => l.TaxAmount), 2);

        // Order-level charges follow the share of the order this check represents, so a guest
        // paying a fifth of the food pays a fifth of the service charge — not all of it.
        var share = order.SubTotal > 0 ? check.SubTotal / order.SubTotal : 1m / Math.Max(1, splitCount);

        check.DiscountAmount = Math.Round(
            check.Discounts.Where(d => !d.IsDeleted).Sum(d => d.Amount) + order.DiscountAmount * share, 2);
        check.ServiceChargeAmount = Math.Round(order.ServiceChargeAmount * share, 2);
        check.PackagingChargeAmount = Math.Round(order.PackagingChargeAmount * share, 2);
        check.DeliveryFeeAmount = Math.Round(order.DeliveryFeeAmount * share, 2);

        var chargeable = check.SubTotal - check.DiscountAmount + check.ServiceChargeAmount
                       + check.PackagingChargeAmount + check.DeliveryFeeAmount;

        var gross = settings.PricesIncludeTax ? chargeable : chargeable + check.TaxAmount;

        check.RoundingAmount = settings.CashRoundingIncrement > 0
            ? Math.Round(RoundTo(gross, settings.CashRoundingIncrement) - gross, 2)
            : 0m;

        check.TotalAmount = Math.Round(gross + check.RoundingAmount, 2);
        check.PaidAmount = Math.Round(check.Payments.Where(p => !p.IsDeleted && !p.IsRefund).Sum(p => p.Amount)
                                    - check.Payments.Where(p => !p.IsDeleted && p.IsRefund).Sum(p => p.Amount), 2);
        check.TipAmount = Math.Round(check.Payments.Where(p => !p.IsDeleted).Sum(p => p.TipAmount), 2);
    }

    /// <summary>Rounds to the nearest cash increment (5c, 10c, 1 unit) for currencies without small coins.</summary>
    private static decimal RoundTo(decimal value, decimal increment)
        => increment <= 0 ? value : Math.Round(value / increment, MidpointRounding.AwayFromZero) * increment;

    // ── Money in ─────────────────────────────────────────────────────────────

    public async Task<RestaurantCheckDto> TakePaymentAsync(TakePaymentDto request, Guid userId)
    {
        var check = await LoadCheckAsync(request.CheckId)
            ?? throw new InvalidOperationException("Check not found.");

        if (check.IsVoided) throw new InvalidOperationException("This check has been voided.");
        if (check.Status == CheckStatus.Paid) throw new InvalidOperationException("This check is already settled.");

        // A retried tender must not take the money twice.
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var duplicate = await db.CheckPayments.ForTenant(tenant)
                .AnyAsync(p => p.CheckId == check.Id && p.Reference == request.IdempotencyKey);

            if (duplicate) return (await GetCheckAsync(check.Id))!;
        }

        if (request.Amount <= 0) throw new InvalidOperationException("Payment amount must be greater than zero.");

        var outstanding = Math.Round(check.TotalAmount - check.PaidAmount, 2);
        if (request.Amount > outstanding + 0.01m)
            throw new InvalidOperationException(
                $"That is more than the {outstanding:0.00} still owed on this check.");

        var now = DateTime.UtcNow;

        // Change is only ever given on cash. Handing back cash against a card tender is how a
        // till gets emptied, so the difference on a non-cash tender is simply not accepted.
        var change = request.TenderType == TenderType.Cash && request.TenderedAmount > request.Amount
            ? Math.Round(request.TenderedAmount - request.Amount, 2)
            : 0m;

        var payment = new CheckPayment
        {
            CheckId = check.Id,
            OutletId = check.OutletId,
            SessionId = request.SessionId ?? check.SessionId,
            TenderType = request.TenderType,
            Amount = Math.Round(request.Amount, 2),
            TenderedAmount = request.TenderedAmount > 0 ? request.TenderedAmount : request.Amount,
            ChangeAmount = change,
            TipAmount = Math.Max(0, request.TipAmount),
            CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? check.CurrencyCode : request.CurrencyCode,
            ExchangeRate = request.ExchangeRate <= 0 ? 1m : request.ExchangeRate,
            Reference = request.Reference ?? request.IdempotencyKey,
            CardLast4 = request.CardLast4,
            CardScheme = request.CardScheme,
            AuthCode = request.AuthCode,
            GiftCardId = request.GiftCardId,
            LoyaltyPointsUsed = request.LoyaltyPointsUsed,
            PaidAt = now,
            StaffId = request.StaffId,
        }.StampNew(tenant, userId);

        check.Payments.Add(payment);
        db.CheckPayments.Add(payment);

        if (payment.TipAmount > 0)
        {
            db.Tips.Add(new TipRecord
            {
                OutletId = check.OutletId,
                CheckId = check.Id,
                PaymentId = payment.Id,
                SessionId = payment.SessionId,
                WaiterId = check.WaiterId,
                Amount = payment.TipAmount,
                TenderType = payment.TenderType,
                IsDeclared = payment.TenderType == TenderType.Cash,
                ReceivedAt = now,
            }.StampNew(tenant, userId));
        }

        check.PaidAmount = Math.Round(check.Payments.Where(p => !p.IsDeleted && !p.IsRefund).Sum(p => p.Amount)
                                    - check.Payments.Where(p => !p.IsDeleted && p.IsRefund).Sum(p => p.Amount), 2);
        check.TipAmount = Math.Round(check.Payments.Where(p => !p.IsDeleted).Sum(p => p.TipAmount), 2);
        check.ChangeAmount = Math.Round(check.Payments.Where(p => !p.IsDeleted).Sum(p => p.ChangeAmount), 2);

        var settled = check.PaidAmount >= check.TotalAmount - 0.01m;
        check.Status = settled ? CheckStatus.Paid : CheckStatus.PartiallyPaid;
        if (settled) check.PaidAt = now;
        check.StampUpdated(userId);

        await db.SaveChangesAsync();

        if (settled) await OnCheckSettledAsync(check, userId);

        return (await GetCheckAsync(check.Id))!;
    }

    /// <summary>
    /// Everything that happens once a check is fully paid: roll the session totals, and when the
    /// order's last check is settled, close the order, free the table and deplete the stock the
    /// food consumed.
    /// </summary>
    private async Task OnCheckSettledAsync(RestaurantCheck check, Guid userId)
    {
        var now = DateTime.UtcNow;

        var order = await db.Orders.ForTenant(tenant)
            .Include(o => o.Lines.Where(l => !l.IsDeleted)).ThenInclude(l => l.Modifiers.Where(m => !m.IsDeleted))
            .FirstOrDefaultAsync(o => o.Id == check.OrderId);

        if (order is null) return;

        var siblings = await db.Checks.ForTenant(tenant)
            .Where(c => c.OrderId == order.Id && !c.IsVoided)
            .ToListAsync();

        order.PaidAmount = Math.Round(siblings.Sum(c => c.PaidAmount), 2);
        order.TipAmount = Math.Round(siblings.Sum(c => c.TipAmount), 2);
        order.StampUpdated(userId);

        await UpdateSessionTotalsAsync(check, userId);

        var allSettled = siblings.All(c => c.Status == CheckStatus.Paid);
        if (!allSettled)
        {
            await db.SaveChangesAsync();
            return;
        }

        db.OrderStatusHistory.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = order.Status,
            ToStatus = RestaurantOrderStatus.Closed,
            OccurredAt = now,
        }.StampNew(tenant, userId));

        order.Status = RestaurantOrderStatus.Closed;
        order.ClosedAt = now;

        if (order.TableId.HasValue)
        {
            var table = await db.Tables.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == order.TableId);
            if (table is not null)
            {
                db.TableStateLogs.Add(new TableStateLog
                {
                    TableId = table.Id,
                    OutletId = table.OutletId,
                    OrderId = order.Id,
                    WaiterId = table.AssignedWaiterId,
                    FromState = table.State,
                    ToState = TableState.NeedsCleaning,
                    OccurredAt = now,
                    SecondsInPreviousState = table.StateChangedAt is null
                        ? null : (int)(now - table.StateChangedAt.Value).TotalSeconds,
                    GuestCount = table.CurrentGuestCount,
                }.StampNew(tenant, userId));

                table.State = TableState.NeedsCleaning;
                table.StateChangedAt = now;
                table.CurrentOrderId = null;
                table.CurrentGuestCount = 0;
                table.SeatedAt = null;
                table.StampUpdated(userId);

                // Merged tables come apart when the party leaves.
                var merged = await db.Tables.ForTenant(tenant)
                    .Where(t => t.MergedIntoTableId == table.Id).ToListAsync();

                foreach (var member in merged)
                {
                    member.MergedIntoTableId = null;
                    member.CurrentOrderId = null;
                    member.State = TableState.NeedsCleaning;
                    member.StateChangedAt = now;
                    member.StampUpdated(userId);
                }
            }
        }

        await UpdateGuestProfileAsync(order, userId);
        await UpdateWaiterShiftAsync(order, userId);

        await db.SaveChangesAsync();
        await DepleteStockAsync(order, userId);

        logger.LogInformation("Order {OrderNumber} closed — {Total} {Currency}",
            order.OrderNumber, order.TotalAmount, order.CurrencyCode);
    }

    /// <summary>
    /// Explodes each sold line through its recipe and asks Inventory to take the ingredients out
    /// of stock. This is what makes a restaurant's stock ledger mean anything: nobody sells a
    /// "bun", they sell a burger, and the bun has to leave the shelf all the same.
    /// </summary>
    private async Task DepleteStockAsync(RestaurantOrder order, Guid userId)
    {
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        if (settings is not null && !settings.DepleteStockOnCheckClose) return;

        var outlet = await db.Outlets.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == order.OutletId);
        if (outlet?.WarehouseId is null)
        {
            logger.LogWarning("Outlet {OutletId} has no warehouse — stock was not depleted for {OrderNumber}",
                order.OutletId, order.OrderNumber);
            return;
        }

        var lines = order.Lines.Where(l => !l.IsVoided).ToList();
        if (lines.Count == 0) return;

        var itemIds = lines.Select(l => l.MenuItemId).Distinct().ToList();

        var menuItems = await db.MenuItems.ForTenant(tenant)
            .Where(i => itemIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Code, i.InventoryItemId })
            .ToListAsync();

        var recipes = await db.Recipes.ForTenant(tenant)
            .Where(r => r.MenuItemId != null && itemIds.Contains(r.MenuItemId!.Value))
            .Include(r => r.Ingredients.Where(i => !i.IsDeleted))
            .ToListAsync();

        var deductions = new Dictionary<(Guid ItemId, string Uom), (decimal Quantity, decimal Cost, string Code)>();

        void Accumulate(Guid inventoryItemId, string uom, decimal quantity, decimal unitCost, string code)
        {
            if (quantity <= 0) return;
            var key = (inventoryItemId, uom);
            var existing = deductions.GetValueOrDefault(key);
            deductions[key] = (existing.Quantity + quantity, unitCost, existing.Code ?? code);
        }

        foreach (var line in lines)
        {
            var recipe = recipes.FirstOrDefault(r => r.MenuItemId == line.MenuItemId && r.VariantId == line.VariantId)
                      ?? recipes.FirstOrDefault(r => r.MenuItemId == line.MenuItemId && r.VariantId == null);

            if (recipe is not null && recipe.Ingredients.Count > 0)
            {
                var portions = recipe.YieldQuantity > 0 ? recipe.YieldQuantity : 1m;

                foreach (var ingredient in recipe.Ingredients.Where(i => i.InventoryItemId.HasValue))
                {
                    // Yield and waste are why a recipe asking for 100g of onion consumes more
                    // than 100g of stock: some of what you buy is peel and some gets dropped.
                    var yieldFactor = ingredient.YieldPercent > 0 ? ingredient.YieldPercent / 100m : 1m;
                    var wasteFactor = 1m + ingredient.WastePercent / 100m;

                    var perPortion = ingredient.Quantity / portions / yieldFactor * wasteFactor;
                    Accumulate(ingredient.InventoryItemId!.Value, ingredient.Uom,
                               Math.Round(perPortion * line.Quantity, 6), ingredient.UnitCost, ingredient.IngredientName);
                }
            }
            else
            {
                // No recipe: a pre-packaged item (a bottled drink) maps straight to an inventory
                // item and comes off stock one for one.
                var mapped = menuItems.FirstOrDefault(i => i.Id == line.MenuItemId);
                if (mapped?.InventoryItemId is not null)
                    Accumulate(mapped.InventoryItemId.Value, "EA", line.Quantity, line.UnitCost, mapped.Code ?? line.ItemName);
            }

            // "Extra cheese" is stock too.
            foreach (var modifier in line.Modifiers.Where(m => m.InventoryItemId.HasValue && !m.IsRemoval))
                Accumulate(modifier.InventoryItemId!.Value, modifier.ConsumptionUom ?? "EA",
                           Math.Round(modifier.ConsumptionQuantity * modifier.Quantity * line.Quantity, 6),
                           modifier.CostDelta, modifier.ModifierName);
        }

        if (deductions.Count == 0) return;

        var payload = deductions.Select(kv => new StockDeductionLine
        {
            ProductId = kv.Key.ItemId,
            ProductCode = kv.Value.Code,
            WarehouseId = outlet.WarehouseId,
            Quantity = kv.Value.Quantity,
            UnitOfMeasure = kv.Key.Uom,
            UnitCost = kv.Value.Cost,
        }).ToList();

        // Reuses the platform's existing "a sale completed, take this out of stock" contract —
        // the same one the retail till publishes — so Inventory needs no restaurant-specific
        // handler and one stock ledger serves every channel.
        await events.PublishAsync(new PosTransactionCompletedEvent
        {
            PosTransactionId = order.Id,
            PosStoreId = outlet.PosStoreId ?? outlet.Id,
            WarehouseId = outlet.WarehouseId.Value,
            CompanyId = order.CompanyId,
            BranchId = order.BranchId,
            BusinessUnitId = order.BusinessUnitId,
            CreatedByUserId = userId,
            CompletedAt = DateTime.UtcNow,
            Lines = payload,
        });

        logger.LogInformation("Published stock deduction for order {OrderNumber}: {LineCount} ingredient line(s)",
            order.OrderNumber, payload.Count);
    }

    private async Task UpdateSessionTotalsAsync(RestaurantCheck check, Guid userId)
    {
        if (check.SessionId is null) return;

        var session = await db.Sessions.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == check.SessionId);
        if (session is null || session.Status != SessionStatus.Open) return;

        var payments = await db.CheckPayments.ForTenant(tenant)
            .Where(p => p.SessionId == session.Id && !p.IsRefund)
            .ToListAsync();

        var refunds = await db.CheckPayments.ForTenant(tenant)
            .Where(p => p.SessionId == session.Id && p.IsRefund)
            .ToListAsync();

        session.ExpectedCash = Math.Round(session.OpeningFloat
            + payments.Where(p => p.TenderType == TenderType.Cash).Sum(p => p.Amount)
            - payments.Where(p => p.TenderType == TenderType.Cash).Sum(p => p.ChangeAmount)
            - refunds.Where(p => p.TenderType == TenderType.Cash).Sum(p => p.Amount), 2);

        session.ExpectedCard = Math.Round(payments.Where(p => p.TenderType == TenderType.Card).Sum(p => p.Amount)
                                        - refunds.Where(p => p.TenderType == TenderType.Card).Sum(p => p.Amount), 2);

        session.ExpectedOther = Math.Round(
            payments.Where(p => p.TenderType is not (TenderType.Cash or TenderType.Card)).Sum(p => p.Amount)
          - refunds.Where(p => p.TenderType is not (TenderType.Cash or TenderType.Card)).Sum(p => p.Amount), 2);

        var checks = await db.Checks.ForTenant(tenant)
            .Where(c => c.SessionId == session.Id && !c.IsVoided)
            .ToListAsync();

        session.TotalSales = Math.Round(checks.Sum(c => c.SubTotal - c.DiscountAmount), 2);
        session.TotalDiscounts = Math.Round(checks.Sum(c => c.DiscountAmount), 2);
        session.TotalTax = Math.Round(checks.Sum(c => c.TaxAmount), 2);
        session.TotalServiceCharge = Math.Round(checks.Sum(c => c.ServiceChargeAmount), 2);
        session.TotalTips = Math.Round(payments.Sum(p => p.TipAmount), 2);
        session.TotalRefunds = Math.Round(refunds.Sum(p => p.Amount), 2);
        session.CheckCount = checks.Count(c => c.Status == CheckStatus.Paid);

        var orderIds = checks.Select(c => c.OrderId).Distinct().ToList();
        session.OrderCount = orderIds.Count;
        session.CoverCount = await db.Orders.ForTenant(tenant)
            .Where(o => orderIds.Contains(o.Id)).SumAsync(o => o.GuestCount);

        session.StampUpdated(userId);
    }

    private async Task UpdateGuestProfileAsync(RestaurantOrder order, Guid userId)
    {
        if (order.GuestProfileId is null) return;

        var guest = await db.Guests.ForTenant(tenant).FirstOrDefaultAsync(g => g.Id == order.GuestProfileId);
        if (guest is null) return;

        guest.VisitCount++;
        guest.LifetimeSpend = Math.Round(guest.LifetimeSpend + order.TotalAmount, 2);
        guest.AverageCheck = Math.Round(guest.LifetimeSpend / Math.Max(1, guest.VisitCount), 2);
        guest.FirstVisitAt ??= order.OpenedAt;
        guest.LastVisitAt = order.ClosedAt ?? DateTime.UtcNow;
        guest.StampUpdated(userId);
    }

    private async Task UpdateWaiterShiftAsync(RestaurantOrder order, Guid userId)
    {
        if (order.WaiterId is null) return;

        var today = DateTime.UtcNow.Date;
        var shift = await db.StaffShifts.ForTenant(tenant)
            .FirstOrDefaultAsync(s => s.StaffId == order.WaiterId
                                   && s.ShiftDate == today
                                   && s.Status != ShiftStatus.Ended);

        if (shift is null) return;

        shift.SalesAmount = Math.Round(shift.SalesAmount + order.TotalAmount, 2);
        shift.OrdersHandled++;
        shift.CoversServed += order.GuestCount;
        shift.TipsEarned = Math.Round(shift.TipsEarned + order.TipAmount, 2);
        shift.StampUpdated(userId);
    }

    // ── Adjustments ──────────────────────────────────────────────────────────

    public async Task<RestaurantCheckDto> ApplyDiscountAsync(ApplyDiscountDto request, Guid userId)
    {
        var check = await LoadCheckAsync(request.CheckId)
            ?? throw new InvalidOperationException("Check not found.");

        if (check.Status == CheckStatus.Paid) throw new InvalidOperationException("This check is already settled.");
        if (check.IsVoided) throw new InvalidOperationException("This check has been voided.");

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync() ?? new RestaurantSettings();

        DiscountReason? reason = null;
        if (request.DiscountReasonId.HasValue)
            reason = await db.DiscountReasons.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == request.DiscountReasonId);
        else if (settings.DiscountRequiresReason)
            throw new InvalidOperationException("A discount reason is required.");

        var basis = request.OrderLineId.HasValue
            ? check.Lines.Where(l => l.OrderLineId == request.OrderLineId).Sum(l => l.LineTotal)
            : check.SubTotal;

        var amount = request.Kind switch
        {
            DiscountKind.Percentage => Math.Round(basis * request.Value / 100m, 2),
            DiscountKind.Comp => Math.Round(basis, 2),
            _ => Math.Round(Math.Min(request.Value, basis), 2),
        };

        if (amount <= 0) throw new InvalidOperationException("That discount comes to nothing.");

        // A discount above the configured ceiling needs a supervisor. Checking it here rather
        // than in the UI is the point: the till is the thing that can be walked around.
        var threshold = reason?.MaxAmountWithoutApproval > 0
            ? reason.MaxAmountWithoutApproval
            : settings.DiscountApprovalThreshold;

        var needsApproval = (reason?.RequiresApproval ?? false) || (threshold > 0 && amount > threshold);
        Guid? approvedBy = null;

        if (needsApproval)
        {
            if (string.IsNullOrWhiteSpace(request.ApprovalPin))
                throw new UnauthorizedAccessException("A supervisor PIN is required for a discount of this size.");

            var approver = await staff.VerifyApprovalAsync(check.OutletId, request.ApprovalPin, "discount")
                ?? throw new UnauthorizedAccessException("That PIN is not authorised to approve discounts.");

            approvedBy = approver.StaffId;
        }

        var discount = new CheckDiscount
        {
            CheckId = check.Id,
            OrderLineId = request.OrderLineId,
            DiscountReasonId = request.DiscountReasonId,
            ReasonName = reason?.Name,
            Kind = request.Kind,
            Value = request.Value,
            Amount = amount,
            PromoCode = request.PromoCode,
            AppliedByStaffId = request.StaffId,
            ApprovedByStaffId = approvedBy,
            AppliedAt = DateTime.UtcNow,
        }.StampNew(tenant, userId);

        check.Discounts.Add(discount);
        db.CheckDiscounts.Add(discount);

        var order = await db.Orders.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == check.OrderId)!;
        RecalculateCheck(check, order!, settings, check.SplitCount);
        check.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetCheckAsync(check.Id))!;
    }

    public async Task<RestaurantCheckDto> RemoveDiscountAsync(Guid discountId, Guid userId)
    {
        var discount = await db.CheckDiscounts.ForTenant(tenant).FirstOrDefaultAsync(d => d.Id == discountId)
            ?? throw new InvalidOperationException("Discount not found.");

        var check = await LoadCheckAsync(discount.CheckId)
            ?? throw new InvalidOperationException("Check not found.");

        if (check.Status == CheckStatus.Paid)
            throw new InvalidOperationException("This check is already settled.");

        discount.StampDeleted(userId);
        check.Discounts.Remove(check.Discounts.First(d => d.Id == discountId));

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync() ?? new RestaurantSettings();
        var order = await db.Orders.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == check.OrderId);
        RecalculateCheck(check, order!, settings, check.SplitCount);
        check.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetCheckAsync(check.Id))!;
    }

    public async Task<RestaurantCheckDto> AddTipAsync(AddTipDto request, Guid userId)
    {
        var check = await LoadCheckAsync(request.CheckId)
            ?? throw new InvalidOperationException("Check not found.");

        if (request.Amount <= 0) throw new InvalidOperationException("A tip must be greater than zero.");

        db.Tips.Add(new TipRecord
        {
            OutletId = check.OutletId,
            CheckId = check.Id,
            SessionId = check.SessionId,
            WaiterId = request.WaiterId ?? check.WaiterId,
            Amount = Math.Round(request.Amount, 2),
            TenderType = request.TenderType,
            IsDeclared = request.IsDeclared || request.TenderType == TenderType.Cash,
            ReceivedAt = DateTime.UtcNow,
        }.StampNew(tenant, userId));

        check.TipAmount = Math.Round(check.TipAmount + request.Amount, 2);
        check.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetCheckAsync(check.Id))!;
    }

    public async Task<RestaurantCheckDto> WaiveServiceChargeAsync(WaiveServiceChargeDto request, Guid userId)
    {
        var check = await LoadCheckAsync(request.CheckId)
            ?? throw new InvalidOperationException("Check not found.");

        if (check.Status == CheckStatus.Paid) throw new InvalidOperationException("This check is already settled.");
        if (check.ServiceChargeAmount <= 0) throw new InvalidOperationException("There is no service charge to waive.");

        var order = await db.Orders.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == check.OrderId);

        var rule = await db.ServiceChargeRules.ForTenant(tenant)
            .Where(r => r.IsActive && (r.OutletId == null || r.OutletId == check.OutletId))
            .OrderBy(r => r.Priority)
            .FirstOrDefaultAsync();

        if (rule is not null && !rule.IsWaivable)
            throw new InvalidOperationException("The service charge on this check cannot be waived.");

        if (rule?.RequiresApprovalToWaive ?? false)
        {
            if (string.IsNullOrWhiteSpace(request.ApprovalPin))
                throw new UnauthorizedAccessException("A supervisor PIN is required to waive the service charge.");

            _ = await staff.VerifyApprovalAsync(check.OutletId, request.ApprovalPin, "discount")
                ?? throw new UnauthorizedAccessException("That PIN is not authorised to waive charges.");
        }

        var waived = check.ServiceChargeAmount;

        // Recorded as a discount, not simply zeroed: a waived charge that leaves no trace is
        // indistinguishable from one that was never applied, and managers need to see the pattern.
        var discount = new CheckDiscount
        {
            CheckId = check.Id,
            ReasonName = "Service charge waived",
            Kind = DiscountKind.Amount,
            Value = waived,
            Amount = waived,
            AppliedByStaffId = request.StaffId,
            AppliedAt = DateTime.UtcNow,
        }.StampNew(tenant, userId);

        check.Discounts.Add(discount);
        db.CheckDiscounts.Add(discount);

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync() ?? new RestaurantSettings();
        RecalculateCheck(check, order!, settings, check.SplitCount);
        check.ServiceChargeAmount = 0m;
        check.TotalAmount = Math.Round(check.TotalAmount - waived, 2);
        check.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetCheckAsync(check.Id))!;
    }

    public async Task<RestaurantCheckDto> VoidCheckAsync(VoidCheckDto request, Guid userId)
    {
        var check = await LoadCheckAsync(request.CheckId)
            ?? throw new InvalidOperationException("Check not found.");

        if (check.PaidAmount > 0)
            throw new InvalidOperationException("This check has payments against it. Refund them before voiding.");

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync() ?? new RestaurantSettings();

        VoidReason? reason = null;
        if (request.VoidReasonId.HasValue)
            reason = await db.VoidReasons.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == request.VoidReasonId);
        else if (settings.VoidRequiresReason)
            throw new InvalidOperationException("A void reason is required.");

        if (reason?.RequiresApproval ?? false)
        {
            if (string.IsNullOrWhiteSpace(request.ApprovalPin))
                throw new UnauthorizedAccessException("A supervisor PIN is required to void this check.");

            _ = await staff.VerifyApprovalAsync(check.OutletId, request.ApprovalPin, "void")
                ?? throw new UnauthorizedAccessException("That PIN is not authorised to void checks.");
        }

        check.IsVoided = true;
        check.Status = CheckStatus.Voided;
        check.VoidReasonId = request.VoidReasonId;
        check.VoidNote = request.Note;
        check.VoidedAt = DateTime.UtcNow;
        check.VoidedByStaffId = request.StaffId;
        check.StampUpdated(userId);

        var session = check.SessionId.HasValue
            ? await db.Sessions.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == check.SessionId)
            : null;

        if (session is not null)
        {
            session.TotalVoids = Math.Round(session.TotalVoids + check.TotalAmount, 2);
            session.StampUpdated(userId);
        }

        await db.SaveChangesAsync();
        return (await GetCheckAsync(check.Id))!;
    }

    public async Task<RestaurantCheckDto> RefundPaymentAsync(RefundPaymentDto request, Guid userId)
    {
        var original = await db.CheckPayments.ForTenant(tenant).FirstOrDefaultAsync(p => p.Id == request.PaymentId)
            ?? throw new InvalidOperationException("Payment not found.");

        if (original.IsRefund) throw new InvalidOperationException("A refund cannot itself be refunded.");

        var alreadyRefunded = await db.CheckPayments.ForTenant(tenant)
            .Where(p => p.RefundOfPaymentId == original.Id)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        var refundable = Math.Round(original.Amount - alreadyRefunded, 2);
        var amount = request.Amount <= 0 ? refundable : Math.Round(request.Amount, 2);

        if (amount > refundable)
            throw new InvalidOperationException($"Only {refundable:0.00} of that payment can still be refunded.");

        var check = await LoadCheckAsync(original.CheckId)
            ?? throw new InvalidOperationException("Check not found.");

        if (string.IsNullOrWhiteSpace(request.ApprovalPin))
            throw new UnauthorizedAccessException("A supervisor PIN is required to refund a payment.");

        _ = await staff.VerifyApprovalAsync(check.OutletId, request.ApprovalPin, "refund")
            ?? throw new UnauthorizedAccessException("That PIN is not authorised to issue refunds.");

        var refund = new CheckPayment
        {
            CheckId = check.Id,
            OutletId = check.OutletId,
            SessionId = original.SessionId,
            TenderType = original.TenderType,
            Amount = amount,
            TenderedAmount = amount,
            CurrencyCode = original.CurrencyCode,
            ExchangeRate = original.ExchangeRate,
            PaidAt = DateTime.UtcNow,
            StaffId = request.StaffId,
            IsRefund = true,
            RefundOfPaymentId = original.Id,
            RefundReason = request.Reason,
        }.StampNew(tenant, userId);

        check.Payments.Add(refund);
        db.CheckPayments.Add(refund);

        check.PaidAmount = Math.Round(check.Payments.Where(p => !p.IsDeleted && !p.IsRefund).Sum(p => p.Amount)
                                    - check.Payments.Where(p => !p.IsDeleted && p.IsRefund).Sum(p => p.Amount), 2);

        check.Status = check.PaidAmount <= 0.01m ? CheckStatus.Refunded
                     : check.PaidAmount >= check.TotalAmount - 0.01m ? CheckStatus.Paid
                     : CheckStatus.PartiallyPaid;

        check.StampUpdated(userId);

        await db.SaveChangesAsync();
        await UpdateSessionTotalsAsync(check, userId);
        await db.SaveChangesAsync();

        return (await GetCheckAsync(check.Id))!;
    }

    public async Task<RestaurantCheckDto> MarkPrintedAsync(Guid checkId, Guid userId)
    {
        var check = await db.Checks.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == checkId)
            ?? throw new InvalidOperationException("Check not found.");

        check.PrintCount++;
        check.PrintedAt = DateTime.UtcNow;
        if (check.Status == CheckStatus.Open) check.Status = CheckStatus.Printed;
        check.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetCheckAsync(check.Id))!;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Task<RestaurantCheck?> LoadCheckAsync(Guid checkId) =>
        db.Checks.ForTenant(tenant)
            .Include(c => c.Lines.Where(l => !l.IsDeleted))
            .Include(c => c.Payments.Where(p => !p.IsDeleted))
            .Include(c => c.Discounts.Where(d => !d.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == checkId);

    private async Task DecorateAsync(List<RestaurantCheckDto> checks)
    {
        if (checks.Count == 0) return;

        var orderIds = checks.Select(c => c.OrderId).Distinct().ToList();
        var orders = await db.Orders.ForTenant(tenant)
            .Where(o => orderIds.Contains(o.Id))
            .Select(o => new { o.Id, o.OrderNumber, o.TableNumber, o.WaiterName })
            .ToListAsync();

        foreach (var check in checks)
        {
            var order = orders.FirstOrDefault(o => o.Id == check.OrderId);
            check.OrderNumber = order?.OrderNumber;
            check.TableNumber = order?.TableNumber;
            check.WaiterName = order?.WaiterName;
        }
    }

    private static decimal PerUnitModifiers(RestaurantOrderLine line)
        => line.Modifiers.Where(m => !m.IsDeleted).Sum(m => m.PriceDelta * m.Quantity);

    private static string? SummariseModifiers(RestaurantOrderLine line)
    {
        var modifiers = line.Modifiers.Where(m => !m.IsDeleted).ToList();
        if (modifiers.Count == 0) return null;

        return string.Join(", ", modifiers.Select(m => m.IsRemoval ? $"no {m.ModifierName}" : m.ModifierName));
    }
}
