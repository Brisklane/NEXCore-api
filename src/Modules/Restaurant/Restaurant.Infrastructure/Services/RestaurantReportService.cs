using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Application.Services.Interfaces;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

/// <summary>
/// The dashboard and every analytical report.
///
/// Reports read from the closed record — checks and check lines — rather than from live orders,
/// because an order that is still open is a forecast, not a fact. The one exception is the
/// dashboard's "right now" block, which is explicitly about what is happening this minute.
/// </summary>
public class RestaurantReportService(RestaurantDbContext db, IRestaurantTenant tenant) : IRestaurantReportService
{
    // ── Dashboard ────────────────────────────────────────────────────────────

    public async Task<RestaurantDashboardDto> GetDashboardAsync(Guid? outletId)
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var yesterdayStart = todayStart.AddDays(-1);

        var outlet = outletId.HasValue
            ? await db.Outlets.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == outletId)
            : await db.Outlets.ForTenant(tenant).OrderBy(o => o.Name).FirstOrDefaultAsync();

        var resolvedOutletId = outlet?.Id;

        var checksToday = await db.Checks.ForTenant(tenant)
            .Where(c => !c.IsVoided && c.PaidAt >= todayStart)
            .WhereIf(resolvedOutletId.HasValue, c => c.OutletId == resolvedOutletId)
            .ToListAsync();

        var salesYesterday = await db.Checks.ForTenant(tenant)
            .Where(c => !c.IsVoided && c.PaidAt >= yesterdayStart && c.PaidAt < todayStart)
            .WhereIf(resolvedOutletId.HasValue, c => c.OutletId == resolvedOutletId)
            .SumAsync(c => (decimal?)c.TotalAmount) ?? 0m;

        var ordersToday = await db.Orders.ForTenant(tenant)
            .Where(o => o.OpenedAt >= todayStart && o.Status != RestaurantOrderStatus.Cancelled)
            .WhereIf(resolvedOutletId.HasValue, o => o.OutletId == resolvedOutletId)
            .Select(o => new { o.Id, o.GuestCount, o.CostAmount, o.Status, o.TotalAmount, o.SubTotal })
            .ToListAsync();

        var salesToday = Math.Round(checksToday.Sum(c => c.TotalAmount), 2);
        var coversToday = ordersToday.Sum(o => o.GuestCount);
        var settledCount = checksToday.Count;

        var openOrders = ordersToday
            .Where(o => o.Status is not (RestaurantOrderStatus.Closed or RestaurantOrderStatus.Cancelled))
            .ToList();

        var tables = await db.Tables.ForTenant(tenant)
            .Where(t => t.IsActive)
            .WhereIf(resolvedOutletId.HasValue, t => t.OutletId == resolvedOutletId)
            .Select(t => new { t.State, t.CurrentGuestCount, t.StateChangedAt })
            .ToListAsync();

        var occupiedStates = new[] { TableState.Seated, TableState.Ordered, TableState.Served, TableState.BillPrinted };
        var occupied = tables.Count(t => occupiedStates.Contains(t.State));

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        var seatedLimit = settings?.SeatedAttentionMinutes ?? 10;
        var servedLimit = settings?.ServedAttentionMinutes ?? 20;

        var attention = tables.Count(t => t.StateChangedAt.HasValue && t.State switch
        {
            TableState.Seated => (now - t.StateChangedAt.Value).TotalMinutes >= seatedLimit,
            TableState.Served => (now - t.StateChangedAt.Value).TotalMinutes >= servedLimit,
            TableState.NeedsCleaning => (now - t.StateChangedAt.Value).TotalMinutes >= 10,
            _ => false,
        });

        var liveTickets = await db.KitchenTickets.ForTenant(tenant)
            .Where(t => t.Status != KitchenTicketStatus.Bumped && t.Status != KitchenTicketStatus.Cancelled)
            .WhereIf(resolvedOutletId.HasValue, t => t.OutletId == resolvedOutletId)
            .Select(t => new { t.FiredAt, t.StationId })
            .ToListAsync();

        var slaByStation = await db.Stations.ForTenant(tenant)
            .WhereIf(resolvedOutletId.HasValue, s => s.OutletId == resolvedOutletId)
            .ToDictionaryAsync(s => s.Id, s => s.SlaMinutes);

        var overdue = liveTickets.Count(t =>
            (now - t.FiredAt).TotalMinutes >= slaByStation.GetValueOrDefault(t.StationId, 15));

        var prepTimes = await db.KitchenTickets.ForTenant(tenant)
            .Where(t => t.PrepSeconds != null && t.FiredAt >= todayStart)
            .WhereIf(resolvedOutletId.HasValue, t => t.OutletId == resolvedOutletId)
            .Select(t => t.PrepSeconds!.Value)
            .ToListAsync();

        var turns = await db.TableStateLogs.ForTenant(tenant)
            .Where(l => l.OccurredAt >= todayStart
                     && l.ToState == TableState.NeedsCleaning
                     && l.SecondsInPreviousState != null)
            .WhereIf(resolvedOutletId.HasValue, l => l.OutletId == resolvedOutletId)
            .Select(l => l.SecondsInPreviousState!.Value)
            .ToListAsync();

        var wastageToday = await db.WastageLogs.ForTenant(tenant)
            .Where(w => w.OccurredAt >= todayStart)
            .WhereIf(resolvedOutletId.HasValue, w => w.OutletId == resolvedOutletId)
            .SumAsync(w => (decimal?)w.TotalCost) ?? 0m;

        var items86 = await db.MenuItemAvailabilities.ForTenant(tenant)
            .CountAsync(a => !a.IsAvailable && (resolvedOutletId == null || a.OutletId == resolvedOutletId));

        var waitlistLength = await db.Waitlist.ForTenant(tenant)
            .CountAsync(w => (w.Status == WaitlistStatus.Waiting || w.Status == WaitlistStatus.Notified)
                          && (resolvedOutletId == null || w.OutletId == resolvedOutletId));

        var upcomingReservations = await db.Reservations.ForTenant(tenant)
            .CountAsync(r => (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Requested)
                          && r.ReservedFor >= now && r.ReservedFor <= now.AddHours(4)
                          && (resolvedOutletId == null || r.OutletId == resolvedOutletId));

        var staffOnShift = await db.StaffShifts.ForTenant(tenant)
            .CountAsync(s => s.ShiftDate == todayStart
                          && (s.Status == ShiftStatus.Started || s.Status == ShiftStatus.OnBreak)
                          && (resolvedOutletId == null || s.OutletId == resolvedOutletId));

        var foodCostToday = Math.Round(ordersToday.Sum(o => o.CostAmount), 2);

        var filter = new ReportFilterDto
        {
            OutletId = resolvedOutletId,
            From = todayStart,
            To = now,
            Top = 5,
        };

        var nextReservations = await db.Reservations.ForTenant(tenant)
            .Where(r => (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Requested)
                     && r.ReservedFor >= now.AddMinutes(-30))
            .WhereIf(resolvedOutletId.HasValue, r => r.OutletId == resolvedOutletId)
            .OrderBy(r => r.ReservedFor)
            .Take(5)
            .ToListAsync();

        var recentOrders = await db.Orders.ForTenant(tenant)
            .WhereIf(resolvedOutletId.HasValue, o => o.OutletId == resolvedOutletId)
            .OrderByDescending(o => o.OpenedAt)
            .Take(8)
            .Select(o => new OrderSummaryDto
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
                TotalAmount = o.TotalAmount > 0 ? o.TotalAmount : o.SubTotal,
                PaidAmount = o.PaidAmount,
                OpenedAt = o.OpenedAt,
                ClosedAt = o.ClosedAt,
            })
            .ToListAsync();

        foreach (var order in recentOrders)
            order.MinutesOpen = (int)Math.Max(0, ((order.ClosedAt ?? now) - order.OpenedAt).TotalMinutes);

        return new RestaurantDashboardDto
        {
            OutletId = resolvedOutletId,
            OutletName = outlet?.Name,
            GeneratedAt = now,
            CurrencyCode = outlet?.CurrencyCode ?? "USD",

            SalesToday = salesToday,
            SalesYesterday = Math.Round(salesYesterday, 2),
            SalesChangePercent = salesYesterday > 0
                ? Math.Round((salesToday - salesYesterday) / salesYesterday * 100m, 1)
                : 0m,
            OrdersToday = ordersToday.Count,
            CoversToday = coversToday,
            AverageCheck = settledCount == 0 ? 0m : Math.Round(salesToday / settledCount, 2),
            AveragePerCover = coversToday == 0 ? 0m : Math.Round(salesToday / coversToday, 2),
            TipsToday = Math.Round(checksToday.Sum(c => c.TipAmount), 2),
            DiscountsToday = Math.Round(checksToday.Sum(c => c.DiscountAmount), 2),
            VoidsToday = await db.Checks.ForTenant(tenant)
                .Where(c => c.IsVoided && c.VoidedAt >= todayStart)
                .WhereIf(resolvedOutletId.HasValue, c => c.OutletId == resolvedOutletId)
                .SumAsync(c => (decimal?)c.TotalAmount) ?? 0m,
            FoodCostToday = foodCostToday,
            FoodCostPercent = salesToday > 0 ? Math.Round(foodCostToday / salesToday * 100m, 1) : 0m,
            WastageToday = Math.Round(wastageToday, 2),

            OpenOrders = openOrders.Count,
            OpenOrderValue = Math.Round(openOrders.Sum(o => o.TotalAmount > 0 ? o.TotalAmount : o.SubTotal), 2),
            OccupiedTables = occupied,
            TotalTables = tables.Count,
            OccupancyPercent = tables.Count == 0 ? 0m : Math.Round(occupied * 100m / tables.Count, 1),
            SeatedGuests = tables.Sum(t => t.CurrentGuestCount),
            KitchenTicketsOpen = liveTickets.Count,
            KitchenTicketsOverdue = overdue,
            WaitlistLength = waitlistLength,
            UpcomingReservations = upcomingReservations,
            StaffOnShift = staffOnShift,
            TablesNeedingAttention = attention,
            Items86 = items86,

            AverageTableTurnMinutes = turns.Count == 0 ? 0m : Math.Round((decimal)turns.Average() / 60m, 1),
            AveragePrepMinutes = prepTimes.Count == 0 ? 0m : Math.Round((decimal)prepTimes.Average() / 60m, 1),

            SalesByHour = await GetHourlyAsync(filter),
            TopCategories = await GetCategorySalesAsync(filter),
            TopItems = (await GetItemSalesAsync(filter)).Take(5).ToList(),
            TopWaiters = (await GetWaiterPerformanceAsync(filter)).Take(5).ToList(),
            RecentOrders = recentOrders,
            NextReservations = nextReservations.Select(r => RestaurantMapper.ToDto(r, now)).ToList(),
        };
    }

    // ── Sales ────────────────────────────────────────────────────────────────

    public async Task<SalesSummaryReportDto> GetSalesSummaryAsync(ReportFilterDto filter)
    {
        var (from, to) = Range(filter);

        var checks = await SettledChecks(filter, from, to).ToListAsync();
        var orderIds = checks.Select(c => c.OrderId).Distinct().ToList();

        var orders = await db.Orders.ForTenant(tenant)
            .Where(o => orderIds.Contains(o.Id))
            .Select(o => new { o.Id, o.GuestCount, o.OrderType, o.CostAmount, o.OpenedAt })
            .ToListAsync();

        var payments = await db.CheckPayments.ForTenant(tenant)
            .Where(p => p.PaidAt >= from && p.PaidAt <= to)
            .WhereIf(filter.OutletId.HasValue, p => p.OutletId == filter.OutletId)
            .ToListAsync();

        var gross = Math.Round(checks.Sum(c => c.SubTotal), 2);
        var discounts = Math.Round(checks.Sum(c => c.DiscountAmount), 2);
        var foodCost = Math.Round(orders.Sum(o => o.CostAmount), 2);
        var net = Math.Round(gross - discounts, 2);
        var covers = orders.Sum(o => o.GuestCount);
        var tendered = payments.Where(p => !p.IsRefund).Sum(p => p.Amount);

        var outlet = filter.OutletId.HasValue
            ? await db.Outlets.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == filter.OutletId)
            : null;

        var orderTypeRows = orders
            .GroupBy(o => o.OrderType)
            .Select(g =>
            {
                var ids = g.Select(o => o.Id).ToHashSet();
                var amount = Math.Round(checks.Where(c => ids.Contains(c.OrderId)).Sum(c => c.TotalAmount), 2);
                return new OrderTypeSalesDto
                {
                    OrderType = g.Key,
                    OrderTypeName = g.Key.ToString(),
                    OrderCount = g.Count(),
                    CoverCount = g.Sum(o => o.GuestCount),
                    Amount = amount,
                    AverageCheck = g.Count() == 0 ? 0m : Math.Round(amount / g.Count(), 2),
                };
            })
            .OrderByDescending(r => r.Amount)
            .ToList();

        var totalByType = orderTypeRows.Sum(r => r.Amount);
        foreach (var row in orderTypeRows)
            row.SharePercent = totalByType > 0 ? Math.Round(row.Amount / totalByType * 100m, 2) : 0m;

        var byDay = checks
            .Where(c => c.PaidAt.HasValue)
            .GroupBy(c => c.PaidAt!.Value.Date)
            .Select(g => new DailySalesDto
            {
                Date = g.Key,
                Amount = Math.Round(g.Sum(c => c.TotalAmount), 2),
                OrderCount = g.Select(c => c.OrderId).Distinct().Count(),
                CoverCount = orders.Where(o => g.Select(c => c.OrderId).Contains(o.Id)).Sum(o => o.GuestCount),
                AverageCheck = Math.Round(g.Sum(c => c.TotalAmount) / g.Count(), 2),
            })
            .OrderBy(d => d.Date)
            .ToList();

        return new SalesSummaryReportDto
        {
            From = from,
            To = to,
            OutletId = filter.OutletId,
            CurrencyCode = outlet?.CurrencyCode ?? "USD",
            GrossSales = gross,
            Discounts = discounts,
            ServiceCharge = Math.Round(checks.Sum(c => c.ServiceChargeAmount), 2),
            PackagingCharge = Math.Round(checks.Sum(c => c.PackagingChargeAmount), 2),
            DeliveryFees = Math.Round(checks.Sum(c => c.DeliveryFeeAmount), 2),
            Tax = Math.Round(checks.Sum(c => c.TaxAmount), 2),
            NetSales = net,
            Tips = Math.Round(checks.Sum(c => c.TipAmount), 2),
            Voids = await db.Checks.ForTenant(tenant)
                .Where(c => c.IsVoided && c.VoidedAt >= from && c.VoidedAt <= to)
                .WhereIf(filter.OutletId.HasValue, c => c.OutletId == filter.OutletId)
                .SumAsync(c => (decimal?)c.TotalAmount) ?? 0m,
            Refunds = Math.Round(payments.Where(p => p.IsRefund).Sum(p => p.Amount), 2),
            FoodCost = foodCost,
            GrossMargin = Math.Round(net - foodCost, 2),
            GrossMarginPercent = net > 0 ? Math.Round((net - foodCost) / net * 100m, 2) : 0m,
            OrderCount = orderIds.Count,
            CheckCount = checks.Count,
            CoverCount = covers,
            AverageCheck = checks.Count == 0 ? 0m : Math.Round(checks.Sum(c => c.TotalAmount) / checks.Count, 2),
            AveragePerCover = covers == 0 ? 0m : Math.Round(checks.Sum(c => c.TotalAmount) / covers, 2),
            ByHour = await GetHourlyAsync(filter),
            ByCategory = await GetCategorySalesAsync(filter),
            ByItem = await GetItemSalesAsync(filter),
            ByOrderType = orderTypeRows,
            ByTender = payments.Where(p => !p.IsRefund)
                .GroupBy(p => p.TenderType)
                .Select(g => new TenderBreakdownDto
                {
                    TenderType = g.Key,
                    TenderName = g.Key.ToString(),
                    Count = g.Count(),
                    Amount = Math.Round(g.Sum(p => p.Amount), 2),
                    TipAmount = Math.Round(g.Sum(p => p.TipAmount), 2),
                    SharePercent = tendered > 0 ? Math.Round(g.Sum(p => p.Amount) / tendered * 100m, 2) : 0m,
                })
                .OrderByDescending(t => t.Amount)
                .ToList(),
            ByDay = byDay,
        };
    }

    private async Task<List<HourlySalesDto>> GetHourlyAsync(ReportFilterDto filter)
    {
        var (from, to) = Range(filter);

        var checks = await SettledChecks(filter, from, to)
            .Select(c => new { c.PaidAt, c.TotalAmount, c.OrderId })
            .ToListAsync();

        var orderCovers = await db.Orders.ForTenant(tenant)
            .Where(o => checks.Select(c => c.OrderId).Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => o.GuestCount);

        return Enumerable.Range(0, 24).Select(hour =>
        {
            var slice = checks.Where(c => c.PaidAt!.Value.Hour == hour).ToList();
            return new HourlySalesDto
            {
                Hour = hour,
                Label = $"{hour:00}:00",
                Amount = Math.Round(slice.Sum(c => c.TotalAmount), 2),
                OrderCount = slice.Select(c => c.OrderId).Distinct().Count(),
                CoverCount = slice.Select(c => c.OrderId).Distinct().Sum(id => orderCovers.GetValueOrDefault(id)),
            };
        }).ToList();
    }

    private async Task<List<CategorySalesDto>> GetCategorySalesAsync(ReportFilterDto filter)
    {
        var lines = await SoldLinesAsync(filter);
        if (lines.Count == 0) return [];

        var itemIds = lines.Select(l => l.MenuItemId).Distinct().ToList();
        var itemCategories = await db.MenuItems.ForTenant(tenant)
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.CategoryId);

        var categoryNames = await db.MenuCategories.ForTenant(tenant)
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        var total = lines.Sum(l => l.LineTotal);

        return lines
            .GroupBy(l => itemCategories.GetValueOrDefault(l.MenuItemId))
            .Select(g => new CategorySalesDto
            {
                CategoryId = g.Key,
                CategoryName = categoryNames.GetValueOrDefault(g.Key) ?? "Uncategorised",
                Quantity = Math.Round(g.Sum(l => l.Quantity), 3),
                Amount = Math.Round(g.Sum(l => l.LineTotal), 2),
                CostAmount = Math.Round(g.Sum(l => l.UnitCost * l.Quantity), 2),
                MarginAmount = Math.Round(g.Sum(l => l.LineTotal - l.UnitCost * l.Quantity), 2),
                SharePercent = total > 0 ? Math.Round(g.Sum(l => l.LineTotal) / total * 100m, 2) : 0m,
            })
            .OrderByDescending(c => c.Amount)
            .Take(Math.Max(1, filter.Top))
            .ToList();
    }

    public async Task<List<ItemSalesDto>> GetItemSalesAsync(ReportFilterDto filter)
    {
        var lines = await SoldLinesAsync(filter);
        if (lines.Count == 0) return [];

        var itemIds = lines.Select(l => l.MenuItemId).Distinct().ToList();
        var items = await db.MenuItems.ForTenant(tenant)
            .Where(i => itemIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Name, i.CategoryId })
            .ToListAsync();

        var categoryNames = await db.MenuCategories.ForTenant(tenant)
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        var total = lines.Sum(l => l.LineTotal);

        return lines
            .GroupBy(l => l.MenuItemId)
            .Select(g =>
            {
                var item = items.FirstOrDefault(i => i.Id == g.Key);
                var revenue = Math.Round(g.Sum(l => l.LineTotal), 2);
                var cost = Math.Round(g.Sum(l => l.UnitCost * l.Quantity), 2);

                return new ItemSalesDto
                {
                    MenuItemId = g.Key,
                    ItemName = item?.Name ?? g.First().ItemName,
                    CategoryName = item is null ? null : categoryNames.GetValueOrDefault(item.CategoryId),
                    Quantity = Math.Round(g.Sum(l => l.Quantity), 3),
                    Amount = revenue,
                    CostAmount = cost,
                    MarginAmount = Math.Round(revenue - cost, 2),
                    MarginPercent = revenue > 0 ? Math.Round((revenue - cost) / revenue * 100m, 2) : 0m,
                    SharePercent = total > 0 ? Math.Round(revenue / total * 100m, 2) : 0m,
                };
            })
            .OrderByDescending(i => i.Amount)
            .Take(Math.Max(1, filter.Top))
            .ToList();
    }

    // ── Menu engineering ─────────────────────────────────────────────────────

    /// <summary>
    /// The popularity × margin matrix.
    ///
    /// Two thresholds define the quadrants. Margin is compared against the menu's own average
    /// contribution margin. Popularity is compared against <c>(1 / itemCount) × 0.7</c> — the
    /// standard 70% rule: if every dish sold equally each would take <c>1/n</c> of the mix, and an
    /// item is "popular" if it reaches 70% of that equal share. A flat "top half" split would
    /// re-classify half the menu as failing every time a new dish is added.
    /// </summary>
    public async Task<MenuEngineeringReportDto> GetMenuEngineeringAsync(ReportFilterDto filter)
    {
        var (from, to) = Range(filter);
        var lines = await SoldLinesAsync(filter);

        if (lines.Count == 0)
            return new MenuEngineeringReportDto { From = from, To = to, OutletId = filter.OutletId };

        var itemIds = lines.Select(l => l.MenuItemId).Distinct().ToList();
        var items = await db.MenuItems.ForTenant(tenant)
            .Where(i => itemIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Name, i.CategoryId })
            .ToListAsync();

        var categoryNames = await db.MenuCategories.ForTenant(tenant)
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        var totalQuantity = lines.Sum(l => l.Quantity);

        var rows = lines
            .GroupBy(l => l.MenuItemId)
            .Select(g =>
            {
                var item = items.FirstOrDefault(i => i.Id == g.Key);
                var quantity = g.Sum(l => l.Quantity);
                var revenue = g.Sum(l => l.LineTotal);
                var cost = g.Sum(l => l.UnitCost * l.Quantity);
                var unitPrice = quantity > 0 ? revenue / quantity : 0m;
                var unitCost = quantity > 0 ? cost / quantity : 0m;

                return new MenuEngineeringRowDto
                {
                    MenuItemId = g.Key,
                    ItemName = item?.Name ?? g.First().ItemName,
                    CategoryName = item is null ? null : categoryNames.GetValueOrDefault(item.CategoryId),
                    QuantitySold = Math.Round(quantity, 2),
                    Revenue = Math.Round(revenue, 2),
                    UnitPrice = Math.Round(unitPrice, 2),
                    UnitCost = Math.Round(unitCost, 2),
                    ContributionMargin = Math.Round(unitPrice - unitCost, 2),
                    TotalMargin = Math.Round(revenue - cost, 2),
                    FoodCostPercent = unitPrice > 0 ? Math.Round(unitCost / unitPrice * 100m, 2) : 0m,
                    PopularityPercent = totalQuantity > 0 ? Math.Round(quantity / totalQuantity * 100m, 2) : 0m,
                };
            })
            .ToList();

        var averageMargin = rows.Count == 0 ? 0m : Math.Round(rows.Average(r => r.ContributionMargin), 2);
        var popularityThreshold = Math.Round(100m / rows.Count * 0.7m, 2);

        foreach (var row in rows)
        {
            var popular = row.PopularityPercent >= popularityThreshold;
            var profitable = row.ContributionMargin >= averageMargin;

            (row.Classification, row.Recommendation) = (popular, profitable) switch
            {
                (true, true) => (MenuEngineeringClass.Star,
                    "Protect it. Keep quality and placement exactly as they are; do not discount."),
                (true, false) => (MenuEngineeringClass.Plowhorse,
                    "Popular but thin. Re-cost the recipe, tighten the portion, or raise the price a little."),
                (false, true) => (MenuEngineeringClass.Puzzle,
                    "Profitable but overlooked. Move it up the menu, rename it, or have staff recommend it."),
                (false, false) => (MenuEngineeringClass.Dog,
                    "Neither popular nor profitable. Re-work it or take it off the menu."),
            };

            row.ClassificationName = row.Classification.ToString();
        }

        return new MenuEngineeringReportDto
        {
            From = from,
            To = to,
            OutletId = filter.OutletId,
            AverageMargin = averageMargin,
            PopularityThreshold = popularityThreshold,
            StarCount = rows.Count(r => r.Classification == MenuEngineeringClass.Star),
            PlowhorseCount = rows.Count(r => r.Classification == MenuEngineeringClass.Plowhorse),
            PuzzleCount = rows.Count(r => r.Classification == MenuEngineeringClass.Puzzle),
            DogCount = rows.Count(r => r.Classification == MenuEngineeringClass.Dog),
            Rows = rows.OrderByDescending(r => r.TotalMargin).ToList(),
        };
    }

    // ── Operational ──────────────────────────────────────────────────────────

    public async Task<List<WaiterPerformanceDto>> GetWaiterPerformanceAsync(ReportFilterDto filter)
    {
        var (from, to) = Range(filter);

        var orders = await db.Orders.ForTenant(tenant)
            .Where(o => o.WaiterId != null && o.ClosedAt >= from && o.ClosedAt <= to
                     && o.Status == RestaurantOrderStatus.Closed)
            .WhereIf(filter.OutletId.HasValue, o => o.OutletId == filter.OutletId)
            .WhereIf(filter.WaiterId.HasValue, o => o.WaiterId == filter.WaiterId)
            .Select(o => new
            {
                o.Id, o.WaiterId, o.WaiterName, o.GuestCount, o.TotalAmount,
                o.DiscountAmount, o.TipAmount, o.OpenedAt, o.ClosedAt,
            })
            .ToListAsync();

        if (orders.Count == 0) return [];

        var orderIds = orders.Select(o => o.Id).ToList();

        var voids = await db.OrderLines.ForTenant(tenant)
            .Where(l => orderIds.Contains(l.OrderId) && l.IsVoided)
            .Select(l => new { l.OrderId, Amount = l.UnitPrice * l.Quantity })
            .ToListAsync();

        var dessertItemIds = await db.MenuItems.ForTenant(tenant)
            .Where(i => i.DefaultCourse == CourseType.Dessert)
            .Select(i => i.Id)
            .ToListAsync();

        var dessertOrders = await db.OrderLines.ForTenant(tenant)
            .Where(l => orderIds.Contains(l.OrderId) && !l.IsVoided && dessertItemIds.Contains(l.MenuItemId))
            .Select(l => l.OrderId)
            .Distinct()
            .ToListAsync();

        var hours = await db.StaffShifts.ForTenant(tenant)
            .Where(s => s.ShiftDate >= from.Date && s.ShiftDate <= to.Date)
            .WhereIf(filter.OutletId.HasValue, s => s.OutletId == filter.OutletId)
            .GroupBy(s => s.StaffId)
            .Select(g => new { StaffId = g.Key, Hours = g.Sum(s => s.HoursWorked) })
            .ToListAsync();

        var turnLogs = await db.TableStateLogs.ForTenant(tenant)
            .Where(l => l.OccurredAt >= from && l.OccurredAt <= to
                     && l.ToState == TableState.NeedsCleaning && l.SecondsInPreviousState != null
                     && l.WaiterId != null)
            .Select(l => new { l.WaiterId, Seconds = l.SecondsInPreviousState!.Value })
            .ToListAsync();

        return orders
            .GroupBy(o => o.WaiterId!.Value)
            .Select(g =>
            {
                var ids = g.Select(o => o.Id).ToHashSet();
                var sales = g.Sum(o => o.TotalAmount);
                var covers = g.Sum(o => o.GuestCount);
                var tips = g.Sum(o => o.TipAmount);
                var worked = hours.FirstOrDefault(h => h.StaffId == g.Key)?.Hours ?? 0m;
                var turns = turnLogs.Where(t => t.WaiterId == g.Key).Select(t => t.Seconds).ToList();

                return new WaiterPerformanceDto
                {
                    StaffId = g.Key,
                    StaffName = g.First().WaiterName ?? "Unknown",
                    OrderCount = g.Count(),
                    CoverCount = covers,
                    SalesAmount = Math.Round(sales, 2),
                    AverageCheck = Math.Round(sales / g.Count(), 2),
                    AveragePerCover = covers == 0 ? 0m : Math.Round(sales / covers, 2),
                    TipsEarned = Math.Round(tips, 2),
                    TipPercent = sales > 0 ? Math.Round(tips / sales * 100m, 2) : 0m,
                    DiscountAmount = Math.Round(g.Sum(o => o.DiscountAmount), 2),
                    VoidAmount = Math.Round(voids.Where(v => ids.Contains(v.OrderId)).Sum(v => v.Amount), 2),
                    VoidCount = voids.Count(v => ids.Contains(v.OrderId)),
                    AverageTurnMinutes = turns.Count == 0 ? 0m : Math.Round((decimal)turns.Average() / 60m, 1),
                    HoursWorked = Math.Round(worked, 2),
                    SalesPerHour = worked > 0 ? Math.Round(sales / worked, 2) : 0m,
                    UpsellRate = g.Count() == 0
                        ? 0m
                        : Math.Round(ids.Count(dessertOrders.Contains) * 100m / g.Count(), 1),
                };
            })
            .OrderByDescending(w => w.SalesAmount)
            .ToList();
    }

    public async Task<List<TableTurnoverDto>> GetTableTurnoverAsync(ReportFilterDto filter)
    {
        var (from, to) = Range(filter);

        var logs = await db.TableStateLogs.ForTenant(tenant)
            .Where(l => l.OccurredAt >= from && l.OccurredAt <= to
                     && l.ToState == TableState.NeedsCleaning && l.SecondsInPreviousState != null)
            .WhereIf(filter.OutletId.HasValue, l => l.OutletId == filter.OutletId)
            .Select(l => new { l.TableId, l.OrderId, l.GuestCount, Seconds = l.SecondsInPreviousState!.Value })
            .ToListAsync();

        if (logs.Count == 0) return [];

        var tableIds = logs.Select(l => l.TableId).Distinct().ToList();
        var tables = await db.Tables.ForTenant(tenant)
            .Where(t => tableIds.Contains(t.Id))
            .Select(t => new { t.Id, t.TableNumber, t.Seats, t.SectionId })
            .ToListAsync();

        var sectionNames = await db.Sections.ForTenant(tenant).ToDictionaryAsync(s => s.Id, s => s.Name);

        var orderIds = logs.Where(l => l.OrderId.HasValue).Select(l => l.OrderId!.Value).Distinct().ToList();
        var revenue = await db.Orders.ForTenant(tenant)
            .Where(o => orderIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => o.TotalAmount);

        var windowHours = (decimal)(to - from).TotalHours;

        return logs
            .GroupBy(l => l.TableId)
            .Select(g =>
            {
                var table = tables.FirstOrDefault(t => t.Id == g.Key);
                var seats = table?.Seats ?? 0;
                var money = g.Where(l => l.OrderId.HasValue).Sum(l => revenue.GetValueOrDefault(l.OrderId!.Value));
                var occupiedHours = (decimal)g.Sum(l => l.Seconds) / 3600m;

                return new TableTurnoverDto
                {
                    TableId = g.Key,
                    TableNumber = table?.TableNumber ?? "?",
                    SectionName = table?.SectionId is null ? null : sectionNames.GetValueOrDefault(table.SectionId.Value),
                    Seats = seats,
                    TurnCount = g.Count(),
                    CoverCount = g.Sum(l => l.GuestCount),
                    Revenue = Math.Round(money, 2),
                    RevenuePerSeat = seats == 0 ? 0m : Math.Round(money / seats, 2),
                    AverageTurnMinutes = Math.Round((decimal)g.Average(l => l.Seconds) / 60m, 1),
                    OccupancyPercent = windowHours <= 0 ? 0m : Math.Round(occupiedHours / windowHours * 100m, 1),
                };
            })
            .OrderByDescending(t => t.Revenue)
            .ToList();
    }

    public async Task<List<KitchenPerformanceDto>> GetKitchenPerformanceAsync(ReportFilterDto filter)
    {
        var (from, to) = Range(filter);

        var tickets = await db.KitchenTickets.ForTenant(tenant)
            .Where(t => t.FiredAt >= from && t.FiredAt <= to)
            .WhereIf(filter.OutletId.HasValue, t => t.OutletId == filter.OutletId)
            .WhereIf(filter.StationId.HasValue, t => t.StationId == filter.StationId)
            .Select(t => new { t.Id, t.StationId, t.PrepSeconds, t.RecallCount, t.IsRemake })
            .ToListAsync();

        if (tickets.Count == 0) return [];

        var stations = await db.Stations.ForTenant(tenant)
            .ToDictionaryAsync(s => s.Id, s => new { s.Name, s.SlaMinutes });

        var lineCounts = await db.KitchenTicketLines.ForTenant(tenant)
            .Where(l => tickets.Select(t => t.Id).Contains(l.TicketId))
            .GroupBy(l => l.TicketId)
            .Select(g => new { TicketId = g.Key, Count = g.Count() })
            .ToListAsync();

        return tickets
            .GroupBy(t => t.StationId)
            .Select(g =>
            {
                var station = stations.GetValueOrDefault(g.Key);
                var sla = (station?.SlaMinutes ?? 15) * 60;
                var completed = g.Where(t => t.PrepSeconds.HasValue).Select(t => t.PrepSeconds!.Value).OrderBy(s => s).ToList();
                var ids = g.Select(t => t.Id).ToHashSet();
                var breaches = completed.Count(s => s > sla);

                return new KitchenPerformanceDto
                {
                    StationId = g.Key,
                    StationName = station?.Name ?? "Unknown",
                    TicketCount = g.Count(),
                    ItemCount = lineCounts.Where(l => ids.Contains(l.TicketId)).Sum(l => l.Count),
                    AveragePrepMinutes = completed.Count == 0 ? 0m : Math.Round((decimal)completed.Average() / 60m, 1),
                    MedianPrepMinutes = completed.Count == 0 ? 0m
                        : Math.Round((decimal)completed[completed.Count / 2] / 60m, 1),
                    LongestPrepMinutes = completed.Count == 0 ? 0m : Math.Round((decimal)completed.Max() / 60m, 1),
                    SlaBreachCount = breaches,
                    SlaBreachPercent = completed.Count == 0 ? 0m : Math.Round(breaches * 100m / completed.Count, 1),
                    RecallCount = g.Sum(t => t.RecallCount),
                    RemakeCount = g.Count(t => t.IsRemake),
                };
            })
            .OrderByDescending(k => k.TicketCount)
            .ToList();
    }

    public async Task<List<VoidAuditRowDto>> GetVoidAuditAsync(ReportFilterDto filter)
    {
        var (from, to) = Range(filter);

        var voids = await db.OrderLines.ForTenant(tenant)
            .Where(l => l.IsVoided && l.VoidedAt >= from && l.VoidedAt <= to)
            .Select(l => new
            {
                l.OrderId, l.VoidedAt, l.ItemName, l.Quantity, l.UnitPrice,
                l.VoidReasonId, l.VoidNote, l.VoidedByStaffId, l.WasFiredWhenVoided, l.IsComped,
            })
            .ToListAsync();

        var orderIds = voids.Select(v => v.OrderId).Distinct().ToList();
        var orders = await db.Orders.ForTenant(tenant)
            .Where(o => orderIds.Contains(o.Id))
            .WhereIf(filter.OutletId.HasValue, o => o.OutletId == filter.OutletId)
            .Select(o => new { o.Id, o.OrderNumber, o.TableNumber })
            .ToListAsync();

        var reasonNames = await db.VoidReasons.ForTenant(tenant).ToDictionaryAsync(r => r.Id, r => r.Name);
        var staffNames = await db.Staff.ForTenant(tenant).ToDictionaryAsync(s => s.Id, s => s.DisplayName ?? s.FullName);

        var rows = voids
            .Where(v => orders.Any(o => o.Id == v.OrderId))
            .Select(v =>
            {
                var order = orders.First(o => o.Id == v.OrderId);
                return new VoidAuditRowDto
                {
                    OccurredAt = v.VoidedAt ?? DateTime.MinValue,
                    OrderNumber = order.OrderNumber,
                    TableNumber = order.TableNumber,
                    ItemName = v.ItemName,
                    Quantity = v.Quantity,
                    Amount = Math.Round(v.UnitPrice * v.Quantity, 2),
                    ReasonName = v.VoidReasonId.HasValue ? reasonNames.GetValueOrDefault(v.VoidReasonId.Value) : null,
                    Note = v.VoidNote,
                    StaffName = v.VoidedByStaffId.HasValue ? staffNames.GetValueOrDefault(v.VoidedByStaffId.Value) : null,
                    WasFired = v.WasFiredWhenVoided,
                    Kind = v.IsComped ? "Comp" : "Void",
                };
            })
            .ToList();

        var discounts = await db.CheckDiscounts.ForTenant(tenant)
            .Where(d => d.AppliedAt >= from && d.AppliedAt <= to)
            .ToListAsync();

        var discountCheckIds = discounts.Select(d => d.CheckId).Distinct().ToList();
        var discountChecks = await db.Checks.ForTenant(tenant)
            .Where(c => discountCheckIds.Contains(c.Id))
            .WhereIf(filter.OutletId.HasValue, c => c.OutletId == filter.OutletId)
            .Select(c => new { c.Id, c.CheckNumber, c.OrderId })
            .ToListAsync();

        var discountOrderIds = discountChecks.Select(c => c.OrderId).Distinct().ToList();
        var discountOrders = await db.Orders.ForTenant(tenant)
            .Where(o => discountOrderIds.Contains(o.Id))
            .Select(o => new { o.Id, o.OrderNumber, o.TableNumber })
            .ToListAsync();

        // Discounts sit in the same audit as voids because a manager investigating leakage is
        // looking for both, and splitting them across two screens hides the pattern.
        rows.AddRange(discounts
            .Where(d => discountChecks.Any(c => c.Id == d.CheckId))
            .Select(d =>
            {
                var check = discountChecks.First(c => c.Id == d.CheckId);
                var order = discountOrders.FirstOrDefault(o => o.Id == check.OrderId);
                return new VoidAuditRowDto
                {
                    OccurredAt = d.AppliedAt,
                    OrderNumber = order?.OrderNumber ?? check.CheckNumber,
                    TableNumber = order?.TableNumber,
                    ItemName = d.ReasonName ?? "Check discount",
                    Quantity = 1,
                    Amount = d.Amount,
                    ReasonName = d.ReasonName,
                    Note = d.PromoCode,
                    StaffName = d.AppliedByStaffId.HasValue
                        ? staffNames.GetValueOrDefault(d.AppliedByStaffId.Value) : null,
                    ApprovedByName = d.ApprovedByStaffId.HasValue
                        ? staffNames.GetValueOrDefault(d.ApprovedByStaffId.Value) : null,
                    Kind = d.Kind == DiscountKind.Comp ? "Comp" : "Discount",
                };
            }));

        return rows.OrderByDescending(r => r.OccurredAt).ToList();
    }

    public async Task<List<WastageSummaryDto>> GetWastageSummaryAsync(ReportFilterDto filter)
    {
        var (from, to) = Range(filter);

        var rows = await db.WastageLogs.ForTenant(tenant)
            .Where(w => w.OccurredAt >= from && w.OccurredAt <= to)
            .WhereIf(filter.OutletId.HasValue, w => w.OutletId == filter.OutletId)
            .Select(w => new { w.Reason, w.Quantity, w.TotalCost })
            .ToListAsync();

        var total = rows.Sum(r => r.TotalCost);

        return rows
            .GroupBy(r => r.Reason)
            .Select(g => new WastageSummaryDto
            {
                Reason = g.Key,
                ReasonName = g.Key.ToString(),
                EntryCount = g.Count(),
                Quantity = Math.Round(g.Sum(r => r.Quantity), 3),
                TotalCost = Math.Round(g.Sum(r => r.TotalCost), 2),
                SharePercent = total > 0 ? Math.Round(g.Sum(r => r.TotalCost) / total * 100m, 2) : 0m,
            })
            .OrderByDescending(w => w.TotalCost)
            .ToList();
    }

    // ── Shared query building blocks ─────────────────────────────────────────

    private static (DateTime From, DateTime To) Range(ReportFilterDto filter)
    {
        var to = filter.To ?? DateTime.UtcNow;
        var from = filter.From ?? to.Date.AddDays(-29);
        return (from, to);
    }

    private IQueryable<Domain.Entities.RestaurantCheck> SettledChecks(ReportFilterDto filter, DateTime from, DateTime to)
        => db.Checks.ForTenant(tenant)
            .Where(c => !c.IsVoided && c.Status == CheckStatus.Paid && c.PaidAt >= from && c.PaidAt <= to)
            .WhereIf(filter.OutletId.HasValue, c => c.OutletId == filter.OutletId);

    /// <summary>
    /// Lines that were actually sold and paid for, in the window. Reporting off check lines
    /// rather than order lines means voided and unbilled food never inflates a sales figure.
    /// </summary>
    private async Task<List<CheckLineProjection>> SoldLinesAsync(ReportFilterDto filter)
    {
        var (from, to) = Range(filter);

        var checkIds = await SettledChecks(filter, from, to).Select(c => c.Id).ToListAsync();
        if (checkIds.Count == 0) return [];

        var query = db.CheckLines.ForTenant(tenant).Where(l => checkIds.Contains(l.CheckId));

        if (filter.CategoryId.HasValue)
        {
            var categoryItemIds = db.MenuItems.ForTenant(tenant)
                .Where(i => i.CategoryId == filter.CategoryId)
                .Select(i => i.Id);

            query = query.Where(l => categoryItemIds.Contains(l.MenuItemId));
        }

        return await query
            .Select(l => new CheckLineProjection(l.MenuItemId, l.ItemName, l.Quantity, l.LineTotal, l.UnitCost))
            .ToListAsync();
    }

    private record CheckLineProjection(
        Guid MenuItemId, string ItemName, decimal Quantity, decimal LineTotal, decimal UnitCost);
}
