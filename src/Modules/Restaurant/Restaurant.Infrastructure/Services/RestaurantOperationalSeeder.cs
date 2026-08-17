using Microsoft.EntityFrameworkCore;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

/// <summary>
/// Lays down a fortnight of trading on top of the sample venue.
///
/// The master-data seed alone leaves most of the app looking broken: a menu with no orders means an
/// empty dashboard, empty reports, an empty kitchen screen and a sessions page that has never been
/// opened. Someone evaluating the app cannot tell the difference between "no data yet" and "this
/// screen does not work", so the sample venue is given a history it could plausibly have had.
///
/// Three properties make the data useful rather than merely present:
///
/// **It is shaped like a real week.** Covers peak at lunch and dinner, weekends run heavier than
/// Tuesdays, and average check moves with party size. Reports built on a flat random spread show
/// nothing worth looking at.
///
/// **It contains the awkward cases on purpose.** An unresolved fridge breach, a checklist with a
/// critical failure, a cash session that came up short, a voided line, a comped dessert. These are
/// what the exception-handling screens exist for, and a seed of nothing but clean days leaves them
/// permanently blank.
///
/// **It is deterministic.** The same company id always produces the same fortnight, so a bug found
/// on a seeded database can be reproduced rather than re-rolled.
/// </summary>
internal sealed class RestaurantOperationalSeeder(RestaurantDbContext db)
{
    private const int HistoryDays = 14;

    private Random rng = new(0);

    public async Task SeedAsync(IRestaurantTenant t, RestaurantOutlet outlet)
    {
        // Same company, same fortnight — so a defect found here can be reproduced.
        rng = new Random(outlet.CompanyId.GetHashCode());

        var tables = await db.Tables.Where(x => x.OutletId == outlet.Id && !x.IsDeleted).ToListAsync();
        var staff = await db.Staff.Where(x => x.OutletId == outlet.Id && !x.IsDeleted).ToListAsync();
        var items = await db.MenuItems.Where(x => x.CompanyId == t.CompanyId && !x.IsDeleted).ToListAsync();
        var stations = await db.Stations.Where(x => x.OutletId == outlet.Id && !x.IsDeleted).ToListAsync();
        var zones = await db.DeliveryZones.Where(x => x.OutletId == outlet.Id && !x.IsDeleted).ToListAsync();

        if (tables.Count == 0 || staff.Count == 0 || items.Count == 0) return;

        var waiters = staff.Where(s => s.Role is StaffRole.Waiter or StaffRole.Supervisor).ToList();
        if (waiters.Count == 0) waiters = staff.Take(2).ToList();

        var cashier = staff.FirstOrDefault(s => s.Role is StaffRole.Cashier or StaffRole.Manager) ?? staff[0];

        var guests = SeedGuests(t);
        await db.SaveChangesAsync();

        var today = DateTime.UtcNow.Date;

        // ── Closed trading days ──────────────────────────────────────────────
        for (var back = HistoryDays; back >= 1; back--)
        {
            var day = today.AddDays(-back);
            var session = SeedSession(t, outlet, cashier, day, isOpen: false);

            var covers = 0;
            var checkCount = 0;
            decimal sales = 0m, tax = 0m, discounts = 0m, tips = 0m, service = 0m;

            foreach (var openedAt in ServiceTimes(day))
            {
                var order = SeedClosedOrder(t, outlet, session, tables, waiters, items, guests, openedAt);
                covers += order.GuestCount;
                checkCount++;
                sales += order.SubTotal;
                tax += order.TaxAmount;
                discounts += order.DiscountAmount;
                tips += order.TipAmount;
                service += order.ServiceChargeAmount;
            }

            session.TotalSales = sales;
            session.TotalTax = tax;
            session.TotalDiscounts = discounts;
            session.TotalTips = tips;
            session.TotalServiceCharge = service;
            session.OrderCount = checkCount;
            session.CheckCount = checkCount;
            session.CoverCount = covers;

            SeedOffPremise(t, outlet, session, items, zones, day);

            CloseSession(t, session, day);
        }

        await db.SaveChangesAsync();

        // ── Today ────────────────────────────────────────────────────────────
        var live = SeedSession(t, outlet, cashier, today, isOpen: true);
        await db.SaveChangesAsync();

        SeedLiveService(t, outlet, live, tables, waiters, items, stations);
        SeedReservations(t, outlet, tables, guests, today);
        SeedWaitlist(t, outlet, today);

        await db.SaveChangesAsync();

        await SeedTemperatureHistoryAsync(t, outlet, staff, today);
        await SeedChecklistHistoryAsync(t, outlet, staff, today);

        SeedWastage(t, outlet, items, staff, today);
        SeedShifts(t, outlet, staff, today);
        SeedTipPool(t, outlet, staff, today);
        SeedFeedback(t, outlet, guests, waiters, today);

        await db.SaveChangesAsync();
    }

    // ── Guests ───────────────────────────────────────────────────────────────

    private List<GuestProfile> SeedGuests(IRestaurantTenant t)
    {
        var seed = new (string Name, string Phone, bool Vip, string? Allergies, string? Seating)[]
        {
            ("Aisha Rahman",    "+92 300 1234567", true,  "Peanuts",        "Window"),
            ("Daniel Okafor",   "+92 301 2345678", false, null,             "Booth"),
            ("Mei Lin Chen",    "+92 302 3456789", true,  "Shellfish",      "Quiet corner"),
            ("Omar Haddad",     "+92 303 4567890", false, null,             null),
            ("Priya Nair",      "+92 304 5678901", false, "Gluten",         "Terrace"),
            ("Tomás Herrera",   "+92 305 6789012", false, null,             null),
            ("Yuki Tanaka",     "+92 306 7890123", true,  null,             "Bar"),
            ("Sofia Almeida",   "+92 307 8901234", false, "Dairy",          "Window"),
        };

        var list = new List<GuestProfile>();

        for (var i = 0; i < seed.Length; i++)
        {
            var (name, phone, vip, allergies, seating) = seed[i];
            var visits = vip ? rng.Next(12, 30) : rng.Next(1, 9);
            var avg = 1800m + rng.Next(0, 2600);

            var g = new GuestProfile
            {
                Id = Guid.NewGuid(),
                FullName = name,
                Phone = phone,
                Email = $"{name.Split(' ')[0].ToLowerInvariant()}@example.com",
                Allergies = allergies,
                PreferredSeating = seating,
                VisitCount = visits,
                AverageCheck = avg,
                LifetimeSpend = avg * visits,
                FirstVisitAt = DateTime.UtcNow.AddDays(-rng.Next(120, 700)),
                LastVisitAt = DateTime.UtcNow.AddDays(-rng.Next(1, 30)),
                IsVip = vip,
                LoyaltyPoints = visits * 40,
                LoyaltyTier = vip ? "Gold" : visits > 5 ? "Silver" : null,
                NoShowCount = i == 3 ? 2 : 0,
                Notes = vip ? "Recognise on arrival." : null,
                IsActive = true,
            }.StampNew(t);

            list.Add(g);
            db.Guests.Add(g);
        }

        return list;
    }

    // ── Sessions ─────────────────────────────────────────────────────────────

    private RestaurantSession SeedSession(
        IRestaurantTenant t, RestaurantOutlet outlet, RestaurantStaff cashier, DateTime day, bool isOpen)
    {
        var session = new RestaurantSession
        {
            Id = Guid.NewGuid(),
            OutletId = outlet.Id,
            SessionNumber = $"SES{day:yyyyMMdd}01",
            Status = isOpen ? SessionStatus.Open : SessionStatus.Closed,
            CashierId = cashier.Id,
            CashierName = cashier.FullName,
            TerminalName = "Front till",
            OpenedAt = day.AddHours(10),
            OpeningFloat = 5000m,
            IsBlindClose = false,
            IsActive = true,
        }.StampNew(t);

        db.Sessions.Add(session);

        db.CashMovements.Add(new SessionCashMovement
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            OutletId = outlet.Id,
            MovementType = CashMovementType.OpeningFloat,
            Amount = 5000m,
            Reason = "Opening float",
            StaffId = cashier.Id,
            StaffName = cashier.FullName,
            OccurredAt = session.OpenedAt,
            IsActive = true,
        }.StampNew(t));

        return session;
    }

    private void CloseSession(IRestaurantTenant t, RestaurantSession session, DateTime day)
    {
        var expectedCash = session.OpeningFloat + (session.TotalSales * 0.42m);

        // Roughly one night in four does not balance — the variance column is the whole reason
        // the close screen exists, and it is meaningless if every night is perfect.
        var drift = rng.Next(0, 4) == 0 ? (decimal)(rng.Next(-450, 260)) : 0m;

        session.ExpectedCash = decimal.Round(expectedCash, 2);
        session.ExpectedCard = decimal.Round(session.TotalSales * 0.48m, 2);
        session.ExpectedOther = decimal.Round(session.TotalSales * 0.10m, 2);
        session.CountedCash = decimal.Round(expectedCash + drift, 2);
        session.CountedCard = session.ExpectedCard;
        session.CountedOther = session.ExpectedOther;
        session.CashVariance = decimal.Round(drift, 2);
        session.Status = SessionStatus.Closed;
        session.ClosedAt = day.AddHours(23).AddMinutes(40);
        session.ZReadAt = session.ClosedAt;
        session.ClosingNote = drift < 0 ? "Short — recount done, drawer verified." : null;

        if (session.TotalSales > 0)
        {
            db.CashMovements.Add(new SessionCashMovement
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                OutletId = session.OutletId,
                MovementType = CashMovementType.Drop,
                Amount = decimal.Round(session.TotalSales * 0.20m, 2),
                Reason = "Mid-service safe drop",
                StaffId = session.CashierId,
                StaffName = session.CashierName,
                OccurredAt = day.AddHours(17),
                IsActive = true,
            }.StampNew(t));
        }
    }

    // ── Closed orders ────────────────────────────────────────────────────────

    /// <summary>Service times for a day, busier at the weekend and clustered around the two peaks.</summary>
    private IEnumerable<DateTime> ServiceTimes(DateTime day)
    {
        var weekend = day.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday or DayOfWeek.Sunday;
        var lunch = weekend ? rng.Next(4, 7) : rng.Next(2, 5);
        var dinner = weekend ? rng.Next(6, 10) : rng.Next(4, 7);

        for (var i = 0; i < lunch; i++)
            yield return day.AddHours(12).AddMinutes(rng.Next(0, 120));

        for (var i = 0; i < dinner; i++)
            yield return day.AddHours(19).AddMinutes(rng.Next(0, 150));
    }

    private RestaurantOrder SeedClosedOrder(
        IRestaurantTenant t, RestaurantOutlet outlet, RestaurantSession session,
        List<DiningTable> tables, List<RestaurantStaff> waiters, List<MenuItem> items,
        List<GuestProfile> guests, DateTime openedAt)
    {
        var table = tables[rng.Next(tables.Count)];
        var waiter = waiters[rng.Next(waiters.Count)];
        var partySize = Math.Min(table.Seats, rng.Next(1, 6));
        var guest = rng.Next(0, 3) == 0 ? guests[rng.Next(guests.Count)] : null;

        var order = new RestaurantOrder
        {
            Id = Guid.NewGuid(),
            OutletId = outlet.Id,
            OrderNumber = $"ORD{openedAt:yyyyMMddHHmmss}{rng.Next(10, 99)}",
            OrderType = OrderType.DineIn,
            Channel = OrderChannel.InHouse,
            Status = RestaurantOrderStatus.Closed,
            TableId = table.Id,
            TableNumber = table.TableNumber,
            SectionId = table.SectionId,
            GuestCount = partySize,
            WaiterId = waiter.Id,
            WaiterName = waiter.FullName,
            SessionId = session.Id,
            GuestProfileId = guest?.Id,
            CustomerName = guest?.FullName,
            CustomerPhone = guest?.Phone,
            CurrencyCode = outlet.CurrencyCode,
            OpenedAt = openedAt,
            FirstFiredAt = openedAt.AddMinutes(4),
            ServedAt = openedAt.AddMinutes(rng.Next(18, 32)),
            BilledAt = openedAt.AddMinutes(rng.Next(55, 80)),
            ClosedAt = openedAt.AddMinutes(rng.Next(82, 95)),
            IsActive = true,
        }.StampNew(t);

        db.Orders.Add(order);

        decimal sub = 0m, tax = 0m, cost = 0m, discount = 0m;
        var lineCount = Math.Max(2, partySize + rng.Next(-1, 3));

        for (var i = 0; i < lineCount; i++)
        {
            var item = items[rng.Next(items.Count)];
            var qty = rng.Next(1, 3);
            var unit = item.BasePrice;
            var lineTotal = unit * qty;
            var lineTax = decimal.Round(lineTotal * item.TaxPercent / 100m, 2);

            // One dessert in twenty goes back comped — the discount reports need something real.
            var comped = rng.Next(0, 20) == 0;
            var lineDiscount = comped ? lineTotal : 0m;

            var line = new RestaurantOrderLine
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                MenuItemId = item.Id,
                ItemName = item.Name,
                Quantity = qty,
                UnitPrice = unit,
                LineTotal = lineTotal - lineDiscount,
                DiscountAmount = lineDiscount,
                TaxAmount = comped ? 0m : lineTax,
                TaxPercent = item.TaxPercent,
                UnitCost = item.StandardCost,
                Course = item.DefaultCourse,
                CourseSequence = (int)item.DefaultCourse,
                SeatNumber = partySize > 1 ? rng.Next(1, partySize + 1) : null,
                Status = OrderLineStatus.Served,
                IsComped = comped,
                FiredAt = openedAt.AddMinutes(4),
                ReadyAt = openedAt.AddMinutes(rng.Next(12, 24)),
                ServedAt = order.ServedAt,
                DisplayOrder = i,
                IsActive = true,
            }.StampNew(t);

            db.OrderLines.Add(line);

            sub += lineTotal;
            discount += lineDiscount;
            tax += comped ? 0m : lineTax;
            cost += item.StandardCost * qty;
        }

        var serviceCharge = partySize >= 6 ? decimal.Round(sub * 0.10m, 2) : 0m;
        var tip = rng.Next(0, 3) == 0 ? decimal.Round(sub * 0.08m, 2) : 0m;
        var total = sub - discount + tax + serviceCharge;

        order.SubTotal = sub;
        order.DiscountAmount = discount;
        order.TaxAmount = tax;
        order.ServiceChargeAmount = serviceCharge;
        order.TipAmount = tip;
        order.CostAmount = cost;
        order.TotalAmount = total;
        order.PaidAmount = total + tip;

        SeedCheckFor(t, outlet, session, order, tip);
        SeedStatusTrail(t, order);

        return order;
    }

    /// <summary>
    /// The audit trail behind a closed order. Without it an order's history panel is blank and
    /// there is no way to see how long a table sat between courses.
    /// </summary>
    private void SeedStatusTrail(IRestaurantTenant t, RestaurantOrder order)
    {
        var steps = new (RestaurantOrderStatus From, RestaurantOrderStatus To, DateTime? At)[]
        {
            (RestaurantOrderStatus.Draft,  RestaurantOrderStatus.Open,   order.OpenedAt),
            (RestaurantOrderStatus.Open,   RestaurantOrderStatus.Fired,  order.FirstFiredAt),
            (RestaurantOrderStatus.Fired,  RestaurantOrderStatus.Served, order.ServedAt),
            (RestaurantOrderStatus.Served, RestaurantOrderStatus.Billed, order.BilledAt),
            (RestaurantOrderStatus.Billed, RestaurantOrderStatus.Closed, order.ClosedAt),
        };

        foreach (var (from, to, at) in steps)
        {
            if (at is null) continue;

            db.OrderStatusHistory.Add(new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                FromStatus = from,
                ToStatus = to,
                OccurredAt = at.Value,
                StaffId = order.WaiterId,
                StaffName = order.WaiterName,
                IsActive = true,
            }.StampNew(t));
        }
    }

    /// <summary>
    /// Takeaway and delivery alongside the dine-in trade, so the order-type filters, the delivery
    /// zones and the channel split on the dashboard are not all showing the same one thing.
    /// </summary>
    private void SeedOffPremise(
        IRestaurantTenant t, RestaurantOutlet outlet, RestaurantSession session,
        List<MenuItem> items, List<DeliveryZone> zones, DateTime day)
    {
        var riders = new[] { "Bilal", "Hamza", "Junaid" };

        for (var i = 0; i < rng.Next(2, 5); i++)
        {
            var isDelivery = zones.Count > 0 && rng.Next(0, 2) == 0;
            var placedAt = day.AddHours(rng.Next(12, 22)).AddMinutes(rng.Next(0, 59));
            var zone = isDelivery ? zones[rng.Next(zones.Count)] : null;

            var order = new RestaurantOrder
            {
                Id = Guid.NewGuid(),
                OutletId = outlet.Id,
                OrderNumber = $"ORD{placedAt:yyyyMMddHHmmss}{i:D2}",
                TokenNumber = $"T{rng.Next(10, 99)}",
                OrderType = isDelivery ? OrderType.Delivery : OrderType.Takeaway,
                Channel = isDelivery ? OrderChannel.Online : OrderChannel.Phone,
                Status = RestaurantOrderStatus.Closed,
                GuestCount = 1,
                SessionId = session.Id,
                CustomerName = $"Guest {rng.Next(100, 999)}",
                CustomerPhone = $"+92 33{rng.Next(0, 9)} {rng.Next(1000000, 9999999)}",
                CurrencyCode = outlet.CurrencyCode,
                OpenedAt = placedAt,
                FirstFiredAt = placedAt.AddMinutes(2),
                ServedAt = placedAt.AddMinutes(18),
                BilledAt = placedAt.AddMinutes(19),
                ClosedAt = placedAt.AddMinutes(20),
                PromisedAt = placedAt.AddMinutes(isDelivery ? 45 : 25),
                IsActive = true,
            }.StampNew(t);

            db.Orders.Add(order);

            decimal sub = 0m, tax = 0m;

            for (var j = 0; j < rng.Next(1, 4); j++)
            {
                var item = items[rng.Next(items.Count)];
                var qty = rng.Next(1, 3);
                var lineTotal = item.BasePrice * qty;
                var lineTax = decimal.Round(lineTotal * item.TaxPercent / 100m, 2);

                db.OrderLines.Add(new RestaurantOrderLine
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    MenuItemId = item.Id,
                    ItemName = item.Name,
                    Quantity = qty,
                    UnitPrice = item.BasePrice,
                    LineTotal = lineTotal,
                    TaxAmount = lineTax,
                    TaxPercent = item.TaxPercent,
                    UnitCost = item.StandardCost,
                    Course = item.DefaultCourse,
                    CourseSequence = (int)item.DefaultCourse,
                    Status = OrderLineStatus.Served,
                    FiredAt = order.FirstFiredAt,
                    ServedAt = order.ServedAt,
                    DisplayOrder = j,
                    IsActive = true,
                }.StampNew(t));

                sub += lineTotal;
                tax += lineTax;
            }

            var packaging = isDelivery ? 0m : 50m;
            var fee = zone?.DeliveryFee ?? 0m;

            order.SubTotal = sub;
            order.TaxAmount = tax;
            order.PackagingChargeAmount = packaging;
            order.DeliveryFeeAmount = fee;
            order.TotalAmount = sub + tax + packaging + fee;
            order.PaidAmount = order.TotalAmount;

            SeedCheckFor(t, outlet, session, order, 0m);
            SeedStatusTrail(t, order);

            if (!isDelivery || zone is null) continue;

            var rider = riders[rng.Next(riders.Length)];

            db.Deliveries.Add(new RestaurantDelivery
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                OutletId = outlet.Id,
                Status = DeliveryStatus.Delivered,
                RecipientName = order.CustomerName,
                Phone = order.CustomerPhone,
                AddressLine = $"House {rng.Next(1, 400)}, {zone.Name}",
                ZoneName = zone.Name,
                DeliveryFee = fee,
                DistanceKm = decimal.Round((decimal)rng.NextDouble() * 6m + 1m, 1),
                RiderName = rider,
                RiderPhone = $"+92 34{rng.Next(0, 9)} {rng.Next(1000000, 9999999)}",
                AssignedAt = placedAt.AddMinutes(19),
                PickedUpAt = placedAt.AddMinutes(22),
                DeliveredAt = placedAt.AddMinutes(rng.Next(38, 58)),
                EstimatedArrivalAt = order.PromisedAt,
                IsActive = true,
            }.StampNew(t));
        }
    }

    private void SeedCheckFor(
        IRestaurantTenant t, RestaurantOutlet outlet, RestaurantSession session,
        RestaurantOrder order, decimal tip)
    {
        var check = new RestaurantCheck
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            OutletId = outlet.Id,
            CheckNumber = order.OrderNumber.Replace("ORD", "CHK"),
            Status = CheckStatus.Paid,
            SplitMethod = SplitMethod.None,
            SplitIndex = 1,
            SplitCount = 1,
            SubTotal = order.SubTotal,
            DiscountAmount = order.DiscountAmount,
            ServiceChargeAmount = order.ServiceChargeAmount,
            TaxAmount = order.TaxAmount,
            TipAmount = tip,
            TotalAmount = order.TotalAmount,
            PaidAmount = order.TotalAmount + tip,
            CurrencyCode = order.CurrencyCode,
            SessionId = session.Id,
            CashierId = session.CashierId,
            CashierName = session.CashierName,
            IsActive = true,
        }.StampNew(t);

        db.Checks.Add(check);

        // Cash a little under half the time, which is what the close screen assumes.
        var tender = rng.Next(0, 100) switch
        {
            < 42 => TenderType.Cash,
            < 88 => TenderType.Card,
            _ => TenderType.Wallet,
        };

        var due = check.TotalAmount + tip;
        var tendered = tender == TenderType.Cash ? Math.Ceiling(due / 100m) * 100m : due;

        db.CheckPayments.Add(new CheckPayment
        {
            Id = Guid.NewGuid(),
            CheckId = check.Id,
            OutletId = outlet.Id,
            SessionId = session.Id,
            TenderType = tender,
            Amount = due,
            TenderedAmount = tendered,
            ChangeAmount = tendered - due,
            CurrencyCode = order.CurrencyCode,
            ExchangeRate = 1m,
            TipAmount = tip,
            CardLast4 = tender == TenderType.Card ? rng.Next(1000, 9999).ToString() : null,
            CardScheme = tender == TenderType.Card ? (rng.Next(0, 2) == 0 ? "Visa" : "Mastercard") : null,
            PaidAt = order.ClosedAt ?? order.OpenedAt,
            StaffId = session.CashierId,
            IsActive = true,
        }.StampNew(t));

        if (tip > 0)
        {
            db.Tips.Add(new TipRecord
            {
                Id = Guid.NewGuid(),
                OutletId = outlet.Id,
                CheckId = check.Id,
                SessionId = session.Id,
                WaiterId = order.WaiterId,
                WaiterName = order.WaiterName,
                Amount = tip,
                TenderType = tender,
                IsDeclared = true,
                ReceivedAt = order.ClosedAt ?? order.OpenedAt,
                IsActive = true,
            }.StampNew(t));
        }
    }

    // ── Today's live service ─────────────────────────────────────────────────

    /// <summary>
    /// Four tables mid-service, deliberately at different stages: one just seated, one waiting on
    /// the kitchen, one part-served, one waiting to pay. The floor plan, kitchen screen and order
    /// terminal each need a different one of those to look like anything.
    /// </summary>
    private void SeedLiveService(
        IRestaurantTenant t, RestaurantOutlet outlet, RestaurantSession session,
        List<DiningTable> tables, List<RestaurantStaff> waiters, List<MenuItem> items,
        List<KitchenStation> stations)
    {
        var now = DateTime.UtcNow;
        var open = tables.Take(4).ToList();
        if (open.Count == 0) return;

        var stages = new[]
        {
            (Status: RestaurantOrderStatus.Open,             MinutesAgo: 6,  Line: OrderLineStatus.Held,     Ticket: (KitchenTicketStatus?)null),
            (Status: RestaurantOrderStatus.Fired,            MinutesAgo: 14, Line: OrderLineStatus.Fired,    Ticket: (KitchenTicketStatus?)KitchenTicketStatus.InProgress),
            (Status: RestaurantOrderStatus.PartiallyServed,  MinutesAgo: 34, Line: OrderLineStatus.Ready,    Ticket: (KitchenTicketStatus?)KitchenTicketStatus.Ready),
            (Status: RestaurantOrderStatus.Served,           MinutesAgo: 58, Line: OrderLineStatus.Served,   Ticket: (KitchenTicketStatus?)KitchenTicketStatus.Bumped),
        };

        for (var i = 0; i < open.Count && i < stages.Length; i++)
        {
            var table = open[i];
            var stage = stages[i];
            var waiter = waiters[i % waiters.Count];
            var openedAt = now.AddMinutes(-stage.MinutesAgo);
            var partySize = Math.Min(table.Seats, rng.Next(2, 5));

            var order = new RestaurantOrder
            {
                Id = Guid.NewGuid(),
                OutletId = outlet.Id,
                OrderNumber = $"ORD{openedAt:yyyyMMddHHmmss}{i:D2}",
                OrderType = OrderType.DineIn,
                Channel = OrderChannel.InHouse,
                Status = stage.Status,
                TableId = table.Id,
                TableNumber = table.TableNumber,
                SectionId = table.SectionId,
                GuestCount = partySize,
                WaiterId = waiter.Id,
                WaiterName = waiter.FullName,
                SessionId = session.Id,
                CurrencyCode = outlet.CurrencyCode,
                OpenedAt = openedAt,
                FirstFiredAt = stage.Ticket is null ? null : openedAt.AddMinutes(3),
                IsActive = true,
            }.StampNew(t);

            db.Orders.Add(order);

            table.State = stage.Status switch
            {
                RestaurantOrderStatus.Open => TableState.Seated,
                RestaurantOrderStatus.Fired => TableState.Ordered,
                RestaurantOrderStatus.PartiallyServed => TableState.Served,
                _ => TableState.BillPrinted,
            };
            table.CurrentOrderId = order.Id;
            table.CurrentGuestCount = partySize;
            table.SeatedAt = openedAt;
            table.AssignedWaiterId = waiter.Id;

            db.TableStateLogs.Add(new TableStateLog
            {
                Id = Guid.NewGuid(),
                TableId = table.Id,
                OutletId = outlet.Id,
                FromState = TableState.Free,
                ToState = table.State,
                OccurredAt = openedAt,
                WaiterId = waiter.Id,
                OrderId = order.Id,
                GuestCount = partySize,
                IsActive = true,
            }.StampNew(t));

            decimal sub = 0m, tax = 0m;
            var lines = new List<RestaurantOrderLine>();

            for (var j = 0; j < rng.Next(2, 5); j++)
            {
                var item = items[rng.Next(items.Count)];
                var qty = rng.Next(1, 3);
                var lineTotal = item.BasePrice * qty;
                var lineTax = decimal.Round(lineTotal * item.TaxPercent / 100m, 2);

                var line = new RestaurantOrderLine
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    MenuItemId = item.Id,
                    ItemName = item.Name,
                    Quantity = qty,
                    UnitPrice = item.BasePrice,
                    LineTotal = lineTotal,
                    TaxAmount = lineTax,
                    TaxPercent = item.TaxPercent,
                    UnitCost = item.StandardCost,
                    Course = item.DefaultCourse,
                    CourseSequence = (int)item.DefaultCourse,
                    SeatNumber = rng.Next(1, partySize + 1),
                    Status = stage.Line,
                    IsHeld = stage.Line == OrderLineStatus.Held,
                    StationId = item.StationId,
                    FiredAt = stage.Ticket is null ? null : openedAt.AddMinutes(3),
                    ReadyAt = stage.Line is OrderLineStatus.Ready or OrderLineStatus.Served
                        ? openedAt.AddMinutes(16) : null,
                    ServedAt = stage.Line == OrderLineStatus.Served ? openedAt.AddMinutes(22) : null,
                    SpecialInstructions = j == 0 && i == 1 ? "No coriander" : null,
                    DisplayOrder = j,
                    IsActive = true,
                }.StampNew(t);

                db.OrderLines.Add(line);
                lines.Add(line);

                sub += lineTotal;
                tax += lineTax;
            }

            order.SubTotal = sub;
            order.TaxAmount = tax;
            order.TotalAmount = sub + tax;

            if (stage.Ticket is { } ticketStatus && stations.Count > 0)
                SeedTicket(t, outlet, order, lines, stations, ticketStatus, openedAt);

            // The last table has asked for the bill — the payment screen needs an open check.
            if (stage.Status == RestaurantOrderStatus.Served)
            {
                order.Status = RestaurantOrderStatus.Billed;
                order.BilledAt = now.AddMinutes(-3);

                db.Checks.Add(new RestaurantCheck
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    OutletId = outlet.Id,
                    CheckNumber = order.OrderNumber.Replace("ORD", "CHK"),
                    Status = CheckStatus.Open,
                    SplitMethod = SplitMethod.None,
                    SplitIndex = 1,
                    SplitCount = 1,
                    SubTotal = sub,
                    TaxAmount = tax,
                    TotalAmount = sub + tax,
                    CurrencyCode = outlet.CurrencyCode,
                    SessionId = session.Id,
                    IsActive = true,
                }.StampNew(t));
            }
        }
    }

    private void SeedTicket(
        IRestaurantTenant t, RestaurantOutlet outlet, RestaurantOrder order,
        List<RestaurantOrderLine> lines, List<KitchenStation> stations,
        KitchenTicketStatus status, DateTime openedAt)
    {
        var station = stations[rng.Next(stations.Count)];
        var firedAt = openedAt.AddMinutes(3);

        var ticket = new KitchenTicket
        {
            Id = Guid.NewGuid(),
            OutletId = outlet.Id,
            StationId = station.Id,
            OrderId = order.Id,
            TicketNumber = $"KOT{firedAt:HHmmss}",
            Status = status,
            Course = CourseType.Main,
            OrderType = order.OrderType,
            TableNumber = order.TableNumber,
            WaiterName = order.WaiterName,
            GuestCount = order.GuestCount,
            FiredAt = firedAt,
            AcknowledgedAt = firedAt.AddSeconds(40),
            StartedAt = status >= KitchenTicketStatus.InProgress ? firedAt.AddMinutes(1) : null,
            ReadyAt = status >= KitchenTicketStatus.Ready ? firedAt.AddMinutes(13) : null,
            BumpedAt = status == KitchenTicketStatus.Bumped ? firedAt.AddMinutes(15) : null,
            PrepSeconds = status >= KitchenTicketStatus.Ready ? 13 * 60 : null,
            IsPriority = order.GuestCount >= 4,
            IsActive = true,
        }.StampNew(t);

        db.KitchenTickets.Add(ticket);

        for (var i = 0; i < lines.Count; i++)
        {
            db.KitchenTicketLines.Add(new KitchenTicketLine
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                OrderLineId = lines[i].Id,
                MenuItemId = lines[i].MenuItemId,
                ItemName = lines[i].ItemName,
                Quantity = lines[i].Quantity,
                SpecialInstructions = lines[i].SpecialInstructions,
                SeatNumber = lines[i].SeatNumber,
                Status = lines[i].Status,
                ReadyAt = lines[i].ReadyAt,
                DisplayOrder = i,
                IsActive = true,
            }.StampNew(t));
        }
    }

    // ── Front of house ───────────────────────────────────────────────────────

    private void SeedReservations(
        IRestaurantTenant t, RestaurantOutlet outlet, List<DiningTable> tables,
        List<GuestProfile> guests, DateTime today)
    {
        var plan = new (int DayOffset, int Hour, int Party, ReservationStatus Status, string? Occasion)[]
        {
            (0, 13, 2, ReservationStatus.Completed, null),
            (0, 19, 4, ReservationStatus.Confirmed, "Birthday"),
            (0, 20, 6, ReservationStatus.Confirmed, "Business dinner"),
            (0, 21, 2, ReservationStatus.Requested, null),
            (1, 13, 3, ReservationStatus.Confirmed, null),
            (1, 20, 8, ReservationStatus.Confirmed, "Anniversary"),
            (2, 19, 2, ReservationStatus.Requested, null),
            (3, 20, 5, ReservationStatus.Confirmed, null),
            (-1, 20, 4, ReservationStatus.NoShow, null),
        };

        for (var i = 0; i < plan.Length; i++)
        {
            var (offset, hour, party, status, occasion) = plan[i];
            var guest = guests[i % guests.Count];
            var when = today.AddDays(offset).AddHours(hour);
            var table = tables.FirstOrDefault(x => x.Seats >= party);

            db.Reservations.Add(new Reservation
            {
                Id = Guid.NewGuid(),
                OutletId = outlet.Id,
                ReservationNumber = $"RSV{when:yyyyMMdd}{i:D2}",
                Status = status,
                GuestProfileId = guest.Id,
                GuestName = guest.FullName,
                Phone = guest.Phone,
                Email = guest.Email,
                PartySize = party,
                ReservedFor = when,
                DurationMinutes = party >= 6 ? 120 : 90,
                TableId = status is ReservationStatus.Confirmed or ReservationStatus.Completed
                    ? table?.Id : null,
                SectionId = table?.SectionId,
                FloorId = table?.FloorId,
                Occasion = occasion,
                AllergyNotes = guest.Allergies,
                SpecialRequests = occasion == "Birthday" ? "Cake at the end, candles please." : null,
                IsHighChairNeeded = party >= 4 && i % 3 == 0,
                DepositAmount = party >= 6 ? 2000m : 0m,
                IsDepositPaid = party >= 6,
                DepositPaidAt = party >= 6 ? when.AddDays(-2) : null,
                ConfirmedAt = status >= ReservationStatus.Confirmed ? when.AddDays(-1) : null,
                CompletedAt = status == ReservationStatus.Completed ? when.AddHours(2) : null,
                Source = i % 4 == 0 ? OrderChannel.Phone : OrderChannel.Online,
                IsActive = true,
            }.StampNew(t));
        }
    }

    private void SeedWaitlist(IRestaurantTenant t, RestaurantOutlet outlet, DateTime today)
    {
        var now = DateTime.UtcNow;

        var waiting = new (string Name, string Phone, int Party, int MinutesAgo, int Quoted, WaitlistStatus Status)[]
        {
            ("Walk-in — Farhan",  "+92 311 1112223", 2, 12, 20, WaitlistStatus.Waiting),
            ("Walk-in — Beatriz", "+92 311 2223334", 4, 25, 35, WaitlistStatus.Notified),
            ("Walk-in — Kwame",   "+92 311 3334445", 3, 48, 30, WaitlistStatus.Seated),
        };

        foreach (var (name, phone, party, ago, quoted, status) in waiting)
        {
            db.Waitlist.Add(new WaitlistEntry
            {
                Id = Guid.NewGuid(),
                OutletId = outlet.Id,
                GuestName = name,
                Phone = phone,
                PartySize = party,
                Status = status,
                JoinedAt = now.AddMinutes(-ago),
                QuotedWaitMinutes = quoted,
                NotifiedAt = status >= WaitlistStatus.Notified ? now.AddMinutes(-ago + quoted) : null,
                SeatedAt = status == WaitlistStatus.Seated ? now.AddMinutes(-ago + quoted + 4) : null,
                PagerNumber = $"P{rng.Next(1, 20):D2}",
                IsActive = true,
            }.StampNew(t));
        }
    }

    // ── Food safety ──────────────────────────────────────────────────────────

    private async Task SeedTemperatureHistoryAsync(
        IRestaurantTenant t, RestaurantOutlet outlet, List<RestaurantStaff> staff, DateTime today)
    {
        var checkpoints = await db.TemperatureCheckpoints
            .Where(c => c.OutletId == outlet.Id && !c.IsDeleted).ToListAsync();

        if (checkpoints.Count == 0) return;

        var chef = staff.FirstOrDefault(s => s.Role is StaffRole.Chef) ?? staff[0];

        foreach (var cp in checkpoints)
        {
            var mid = (cp.MinSafeCelsius + cp.MaxSafeCelsius) / 2m;

            for (var back = HistoryDays; back >= 0; back--)
            {
                foreach (var hour in new[] { 9, 17 })
                {
                    // One breach, three days ago, left unresolved — the compliance board needs
                    // something outstanding or its whole exception path is invisible.
                    var breach = back == 3 && hour == 17 && cp == checkpoints[0];
                    var reading = breach
                        ? cp.MaxSafeCelsius + 3.4m
                        : mid + (decimal)(rng.NextDouble() - 0.5) * 1.4m;

                    db.TemperatureLogs.Add(new TemperatureLog
                    {
                        Id = Guid.NewGuid(),
                        CheckpointId = cp.Id,
                        OutletId = outlet.Id,
                        ReadingCelsius = decimal.Round(reading, 1),
                        RecordedAt = today.AddDays(-back).AddHours(hour),
                        StaffId = chef.Id,
                        StaffName = chef.FullName,
                        IsOutOfRange = breach,
                        IsResolved = false,
                        Note = breach ? "Door found ajar on the walk-in." : null,
                        IsActive = true,
                    }.StampNew(t));
                }
            }
        }
    }

    private async Task SeedChecklistHistoryAsync(
        IRestaurantTenant t, RestaurantOutlet outlet, List<RestaurantStaff> staff, DateTime today)
    {
        var checklists = await db.Checklists
            .Include(c => c.Items)
            .Where(c => c.OutletId == outlet.Id && !c.IsDeleted)
            .ToListAsync();

        if (checklists.Count == 0) return;

        var manager = staff.FirstOrDefault(s => s.Role is StaffRole.Manager or StaffRole.Supervisor) ?? staff[0];

        foreach (var list in checklists)
        {
            var items = list.Items.Where(i => !i.IsDeleted).OrderBy(i => i.DisplayOrder).ToList();
            if (items.Count == 0) continue;

            for (var back = 7; back >= 0; back--)
            {
                var dueOn = today.AddDays(-back);

                // Today's run is left open on purpose so the board has something to do.
                var isOpen = back == 0;

                // One failed run last week, so the corrective-action trail is not empty.
                var failDay = back == 4;

                var run = new ChecklistRun
                {
                    Id = Guid.NewGuid(),
                    ChecklistId = list.Id,
                    OutletId = outlet.Id,
                    DueOn = dueOn,
                    StartedAt = isOpen ? null : dueOn.AddHours(9),
                    CompletedAt = isOpen ? null : dueOn.AddHours(9).AddMinutes(12),
                    CompletedByStaffId = isOpen ? null : manager.Id,
                    CompletedByStaffName = isOpen ? null : manager.FullName,
                    HasCriticalFailure = failDay,
                    CorrectiveAction = failDay
                        ? "Probe recalibrated and re-checked; unit signed off by the duty manager."
                        : null,
                    IsActive = true,
                }.StampNew(t);

                db.Checklists.Attach(list);
                db.ChecklistRuns.Add(run);

                if (isOpen) continue;

                var pass = 0;
                var fail = 0;

                for (var i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    var failed = failDay && item.IsCritical && i == 0;

                    if (failed) fail++; else pass++;

                    db.ChecklistAnswers.Add(new ChecklistAnswer
                    {
                        Id = Guid.NewGuid(),
                        RunId = run.Id,
                        ChecklistItemId = item.Id,
                        ItemText = item.Text,
                        YesNoValue = item.AnswerType == ChecklistAnswerType.YesNo ? !failed : null,
                        NumericValue = item.AnswerType == ChecklistAnswerType.Numeric
                            ? decimal.Round((item.MinValue ?? 0m) + 1.5m, 1) : null,
                        TextValue = item.AnswerType == ChecklistAnswerType.Text ? "Checked, all clear." : null,
                        IsPass = !failed,
                        IsCritical = item.IsCritical,
                        CorrectiveAction = failed ? "Probe recalibrated before service." : null,
                        AnsweredAt = dueOn.AddHours(9).AddMinutes(i),
                        DisplayOrder = item.DisplayOrder,
                        IsActive = true,
                    }.StampNew(t));
                }

                run.PassCount = pass;
                run.FailCount = fail;
            }
        }
    }

    // ── Wastage, shifts, tips, feedback ──────────────────────────────────────

    private void SeedWastage(
        IRestaurantTenant t, RestaurantOutlet outlet, List<MenuItem> items,
        List<RestaurantStaff> staff, DateTime today)
    {
        var reasons = new[]
        {
            WastageReason.Spoilage, WastageReason.Burnt, WastageReason.Dropped,
            WastageReason.GuestReturn, WastageReason.Expired, WastageReason.StaffMeal,
        };

        var chef = staff.FirstOrDefault(s => s.Role is StaffRole.Chef) ?? staff[0];

        for (var i = 0; i < 12; i++)
        {
            var item = items[rng.Next(items.Count)];
            var qty = rng.Next(1, 4);

            db.WastageLogs.Add(new WastageLog
            {
                Id = Guid.NewGuid(),
                OutletId = outlet.Id,
                OccurredAt = today.AddDays(-rng.Next(0, HistoryDays)).AddHours(rng.Next(11, 22)),
                Reason = reasons[rng.Next(reasons.Length)],
                MenuItemId = item.Id,
                ItemName = item.Name,
                Quantity = qty,
                Uom = "portion",
                UnitCost = item.StandardCost,
                TotalCost = item.StandardCost * qty,
                StaffId = chef.Id,
                StaffName = chef.FullName,
                IsActive = true,
            }.StampNew(t));
        }
    }

    private void SeedShifts(
        IRestaurantTenant t, RestaurantOutlet outlet, List<RestaurantStaff> staff, DateTime today)
    {
        for (var back = 6; back >= 0; back--)
        {
            var day = today.AddDays(-back);

            foreach (var person in staff)
            {
                var isToday = back == 0;
                var start = TimeSpan.FromHours(person.Role == StaffRole.Chef ? 9 : 11);
                var end = TimeSpan.FromHours(person.Role == StaffRole.Chef ? 18 : 23);

                var shift = new StaffShift
                {
                    Id = Guid.NewGuid(),
                    OutletId = outlet.Id,
                    StaffId = person.Id,
                    ShiftDate = day,
                    ScheduledStart = start,
                    ScheduledEnd = end,
                    Status = isToday ? ShiftStatus.Started : ShiftStatus.Ended,
                    ActualStart = day.Add(start).AddMinutes(rng.Next(-5, 10)),
                    ActualEnd = isToday ? null : day.Add(end).AddMinutes(rng.Next(0, 25)),
                    BreakMinutes = 30,
                    HoursWorked = isToday ? 0m : decimal.Round((decimal)(end - start).TotalHours - 0.5m, 2),
                    SectionId = person.DefaultSectionId,
                    Role = person.Role,
                    SalesAmount = person.Role == StaffRole.Waiter ? rng.Next(18000, 46000) : 0m,
                    OrdersHandled = person.Role == StaffRole.Waiter ? rng.Next(8, 22) : 0,
                    CoversServed = person.Role == StaffRole.Waiter ? rng.Next(20, 60) : 0,
                    TipsEarned = person.Role == StaffRole.Waiter ? rng.Next(600, 2600) : 0m,
                    IsActive = true,
                }.StampNew(t);

                db.StaffShifts.Add(shift);

                db.TimeClockEntries.Add(new TimeClockEntry
                {
                    Id = Guid.NewGuid(),
                    OutletId = outlet.Id,
                    StaffId = person.Id,
                    ShiftId = shift.Id,
                    ClockedInAt = shift.ActualStart!.Value,
                    ClockedOutAt = shift.ActualEnd,
                    Hours = shift.HoursWorked,
                    IsActive = true,
                }.StampNew(t));
            }
        }
    }

    private void SeedTipPool(
        IRestaurantTenant t, RestaurantOutlet outlet, List<RestaurantStaff> staff, DateTime today)
    {
        var start = today.AddDays(-7);
        var end = today.AddDays(-1);
        var total = 18_400m;

        var pool = new TipPool
        {
            Id = Guid.NewGuid(),
            OutletId = outlet.Id,
            Name = $"Week of {start:d MMM}",
            PeriodStart = start,
            PeriodEnd = end,
            Basis = TipDistributionBasis.ByHoursWorked,
            TotalAmount = total,
            KitchenSharePercent = 20m,
            IsFinalised = true,
            FinalisedAt = end.AddHours(23),
            IsActive = true,
        }.StampNew(t);

        db.TipPools.Add(pool);

        var share = staff.Count == 0 ? 0m : decimal.Round(total / staff.Count, 2);
        var distributed = 0m;

        for (var i = 0; i < staff.Count; i++)
        {
            // The last person absorbs the rounding remainder so the pool balances exactly.
            var amount = i == staff.Count - 1 ? total - distributed : share;
            distributed += share;

            db.TipDistributions.Add(new TipDistribution
            {
                Id = Guid.NewGuid(),
                TipPoolId = pool.Id,
                StaffId = staff[i].Id,
                StaffName = staff[i].FullName,
                Role = staff[i].Role,
                HoursWorked = 38m,
                SharePercent = decimal.Round(100m / staff.Count, 2),
                Amount = amount,
                IsPaidOut = true,
                PaidOutAt = end.AddHours(23),
                IsActive = true,
            }.StampNew(t));
        }

        pool.DistributedAmount = total;
    }

    private void SeedFeedback(
        IRestaurantTenant t, RestaurantOutlet outlet, List<GuestProfile> guests,
        List<RestaurantStaff> waiters, DateTime today)
    {
        var reviews = new (int Overall, int Food, int Service, string Comment, bool Resolved)[]
        {
            (5, 5, 5, "The lamb was outstanding and the service never felt rushed.", true),
            (4, 5, 3, "Food excellent, but we waited a while for the bill.", true),
            (2, 3, 2, "Starters arrived after the mains. Nobody checked on us.", false),
            (5, 5, 5, "Booked for an anniversary — they remembered. Lovely evening.", true),
            (3, 3, 4, "Fine, though the terrace was cold and the heaters were off.", false),
            (4, 4, 4, "Good value at lunch. Will come back.", true),
        };

        for (var i = 0; i < reviews.Length; i++)
        {
            var (overall, food, service, comment, resolved) = reviews[i];
            var guest = guests[i % guests.Count];

            db.Feedback.Add(new CustomerFeedback
            {
                Id = Guid.NewGuid(),
                OutletId = outlet.Id,
                GuestProfileId = guest.Id,
                WaiterId = waiters[i % waiters.Count].Id,
                OverallRating = overall,
                FoodRating = food,
                ServiceRating = service,
                AmbienceRating = Math.Min(5, overall + 1),
                ValueRating = overall,
                Comment = comment,
                GuestName = guest.FullName,
                Phone = guest.Phone,
                SubmittedAt = today.AddDays(-rng.Next(0, HistoryDays)).AddHours(rng.Next(14, 23)),
                IsResolved = resolved,
                ResolutionNote = resolved ? "Thanked the guest; passed to the floor manager." : null,
                ResolvedAt = resolved ? today.AddDays(-1) : null,
                IsActive = true,
            }.StampNew(t));
        }
    }
}
