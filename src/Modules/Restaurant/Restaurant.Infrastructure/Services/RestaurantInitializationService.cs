using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

/// <summary>
/// Stands a company's Restaurant app up as something that works on first open.
///
/// Two tiers, and the distinction matters:
///
/// **Essential** is the configuration without which the app cannot function at all — you cannot
/// seat a guest with no outlet, fire a ticket with no station, or void a line with no reason
/// codes. Every company gets it, always, whatever they chose at sign-up. An app that greets a new
/// customer with a blank screen and a silent save button is broken, not minimal.
///
/// **Sample** is the demonstration venue — a laid-out floor, a real menu with modifiers and
/// combos, staff with PINs, recipes. Only companies that ticked "seed sample data" at
/// registration get it, because a real restaurant wants to type in their own menu, not delete
/// someone else's.
///
/// Everything is idempotent and runs per tier, so a company that skipped sample data at sign-up
/// can ask for it later, and one that already has an outlet is never given a second one.
/// </summary>
public class RestaurantInitializationService(
    RestaurantDbContext db,
    ILogger<RestaurantInitializationService> logger)
{
    /// <summary>Called by the CompanyCreated handler. Provisions essentials, and samples if asked.</summary>
    public Task<bool> InitializeForNewCompanyAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, bool includeSampleData)
        => EnsureProvisionedAsync(companyId, branchId, businessUnitId, userId, includeSampleData);

    /// <summary>
    /// Brings a company's Restaurant app up to a working state, whenever it is called.
    ///
    /// This exists because installing an app is not the same event as creating a company: a
    /// business that has been running NexCore for a year and adds Restaurant today never saw
    /// <c>CompanyCreatedEvent</c>, and would otherwise land on a floor plan with no outlet behind
    /// it — where every save silently does nothing.
    /// </summary>
    public async Task<bool> EnsureProvisionedAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, bool includeSampleData)
    {
        var tenant = new FixedRestaurantTenant(companyId, branchId, businessUnitId, userId);

        var outlet = await db.Outlets
            .FirstOrDefaultAsync(o => o.CompanyId == companyId && o.BranchId == branchId
                                   && o.BusinessUnitId == businessUnitId && !o.IsDeleted);

        var alreadyProvisioned = outlet is not null;

        // Sample data on a venue that already has dishes would duplicate the menu, so it is only
        // laid down on a venue that is still empty.
        var wantsSample = includeSampleData
            && !await db.MenuItems.AnyAsync(i => i.CompanyId == companyId && !i.IsDeleted);

        // Trading history is gated separately from the menu. A venue seeded before this existed
        // has dishes but no orders, and would otherwise keep a blank dashboard for ever.
        var wantsHistory = includeSampleData
            && !await db.Orders.AnyAsync(o => o.CompanyId == companyId && !o.IsDeleted);

        if (alreadyProvisioned && !wantsSample && !wantsHistory) return false;

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            if (!await db.Settings.AnyAsync(s => s.CompanyId == companyId && s.BranchId == branchId
                                              && s.BusinessUnitId == businessUnitId && !s.IsDeleted))
                SeedSettings(tenant);

            outlet ??= SeedOutlet(tenant);

            if (!alreadyProvisioned)
            {
                SeedSchedules(tenant, outlet);
                SeedReasonCodes(tenant);
                var serviceCharge = SeedServiceCharge(tenant, outlet);
                outlet.ServiceChargeRuleId = serviceCharge.Id;
                SeedPrinters(tenant, outlet);
                SeedFoodSafety(tenant, outlet);
                SeedDeliveryZones(tenant, outlet);
            }

            var stations = alreadyProvisioned
                ? await db.Stations.Where(s => s.OutletId == outlet.Id && !s.IsDeleted).ToListAsync()
                : SeedStations(tenant, outlet);

            if (stations.Count == 0) stations = SeedStations(tenant, outlet);

            var floor = await db.Floors
                .FirstOrDefaultAsync(f => f.OutletId == outlet.Id && !f.IsDeleted);

            List<TableSection> sections;

            if (floor is null)
            {
                (floor, sections) = SeedFloor(tenant, outlet);
            }
            else
            {
                sections = await db.Sections
                    .Where(s => s.FloorId == floor.Id && !s.IsDeleted).ToListAsync();
            }

            await db.SaveChangesAsync();

            var menu = await db.Menus
                .FirstOrDefaultAsync(m => m.CompanyId == companyId && !m.IsDeleted && m.IsDefault)
                ?? SeedMenu(tenant, outlet);

            await db.SaveChangesAsync();

            var categories = await db.MenuCategories
                .Where(c => c.MenuId == menu.Id && !c.IsDeleted).ToListAsync();

            if (categories.Count == 0)
            {
                categories = SeedCategories(tenant, menu, stations);
                await db.SaveChangesAsync();
            }

            if (!await db.RoutingRules.AnyAsync(r => r.OutletId == outlet.Id && !r.IsDeleted))
                SeedRouting(tenant, outlet, stations, categories);

            // ── Sample venue ─────────────────────────────────────────────────
            if (wantsSample)
            {
                SeedTables(tenant, outlet, floor, sections);
                SeedFixtures(tenant, floor);
                SeedStaff(tenant, outlet, sections);

                var modifierGroups = SeedModifierGroups(tenant);
                await db.SaveChangesAsync();

                var items = SeedItems(tenant, outlet, categories, stations, modifierGroups);
                await db.SaveChangesAsync();

                SeedCombos(tenant, outlet, items);
                SeedHappyHour(tenant, outlet, categories);
                SeedRecipes(tenant, items);
                SeedPrepBatches(tenant, outlet, items);
            }

            await db.SaveChangesAsync();

            // A menu with no orders behind it leaves the dashboard, reports, kitchen screen and
            // sessions page blank, which reads as broken rather than empty. The sample venue gets a
            // fortnight of trading so every screen has something true to show.
            if (wantsHistory)
            {
                await new RestaurantOperationalSeeder(db).SeedAsync(tenant, outlet);
            }

            await db.SaveChangesAsync();

            outlet.DefaultMenuId = menu.Id;
            outlet.SeatingCapacity = await db.Tables
                .Where(t => t.OutletId == outlet.Id && !t.IsDeleted && t.IsActive)
                .SumAsync(t => t.Seats);

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            logger.LogInformation(
                "Restaurant provisioned for company {CompanyId}: outlet '{Outlet}', "
                + "sample venue {Sample}, trading history {History}",
                companyId, outlet.Name,
                wantsSample ? "included" : "skipped",
                wantsHistory ? "included" : "skipped");

            return true;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            logger.LogError(ex, "Failed to provision Restaurant data for company {CompanyId}", companyId);
            throw;
        }
    }

    // ── Settings ─────────────────────────────────────────────────────────────

    private RestaurantSettings SeedSettings(IRestaurantTenant t)
    {
        var settings = new RestaurantSettings
        {
            RequireWaiterPin = true,
            RequireGuestCountOnSeat = true,
            AutoFireOnSend = true,
            SeatedAttentionMinutes = 10,
            ServedAttentionMinutes = 20,
            TipsEnabled = true,
            TipPresetPercents = "10,15,20",
            VoidRequiresReason = true,
            DiscountRequiresReason = true,
            DiscountApprovalThreshold = 20m,
            KitchenDisplayEnabled = true,
            ExpoScreenEnabled = true,
            KdsWarningMinutes = 8,
            ShowAllergenWarnings = true,
            DepleteStockOnCheckClose = true,
            TrackWastage = true,
            ReservationsEnabled = true,
            WaitlistEnabled = true,
            DefaultReservationDuration = 90,
            LargePartyThreshold = 8,
            PrintReceiptAutomatically = true,
            ReceiptFooter = "Thank you for dining with us.",
            PackagingChargePerOrder = 0m,
        }.StampNew(t);

        db.Settings.Add(settings);
        return settings;
    }

    // ── Venue ────────────────────────────────────────────────────────────────

    private RestaurantOutlet SeedOutlet(IRestaurantTenant t)
    {
        var outlet = new RestaurantOutlet
        {
            Code = "RST-001",
            Name = "Main Restaurant",
            ServiceStyle = ServiceStyle.CasualDining,
            CuisineType = "Contemporary",
            CurrencyCode = "USD",
            AverageDiningMinutes = 75,
            AcceptsReservations = true,
            AcceptsTakeaway = true,
            AcceptsDelivery = true,
            QrOrderingEnabled = false,
            DefaultTaxPercent = 0m,
            ReceiptFooter = "Thank you for dining with us.",
            Description = "Default venue created with the Restaurant app. Rename it to your own.",
        }.StampNew(t);

        db.Outlets.Add(outlet);
        return outlet;
    }

    private void SeedSchedules(IRestaurantTenant t, RestaurantOutlet outlet)
    {
        for (var day = 0; day < 7; day++)
        {
            db.OutletSchedules.Add(new OutletSchedule
            {
                OutletId = outlet.Id,
                DayOfWeek = day,
                OpensAt = new TimeSpan(11, 0, 0),
                // Friday and Saturday run late — the default most venues would set anyway.
                ClosesAt = day is 5 or 6 ? new TimeSpan(1, 0, 0) : new TimeSpan(23, 0, 0),
            }.StampNew(t));
        }
    }

    private (Floor Floor, List<TableSection> Sections) SeedFloor(IRestaurantTenant t, RestaurantOutlet outlet)
    {
        var floor = new Floor
        {
            OutletId = outlet.Id,
            Name = "Ground Floor",
            DisplayOrder = 0,
            CanvasWidth = 1200,
            CanvasHeight = 760,
        }.StampNew(t);

        db.Floors.Add(floor);

        var sections = new List<TableSection>
        {
            new() { FloorId = floor.Id, Name = "Main Hall", DisplayOrder = 0, ColorHex = "#2b7fff" },
            new() { FloorId = floor.Id, Name = "Window", DisplayOrder = 1, ColorHex = "#22c7e6" },
            new() { FloorId = floor.Id, Name = "Bar", DisplayOrder = 2, ColorHex = "#7c5cff" },
            new() { FloorId = floor.Id, Name = "Terrace", DisplayOrder = 3, ColorHex = "#16a34a", IsOutdoor = true },
        };

        foreach (var section in sections)
        {
            section.StampNew(t);
            db.Sections.Add(section);
        }

        return (floor, sections);
    }

    private void SeedTables(IRestaurantTenant t, RestaurantOutlet outlet, Floor floor, List<TableSection> sections)
    {
        var mainHall = sections[0];
        var window = sections[1];
        var bar = sections[2];
        var terrace = sections[3];

        // Laid out on a grid that reads like a real room rather than a straight line, so the
        // floor plan is usable the moment it opens.
        var layout = new (string Number, TableShape Shape, int Seats, int X, int Y, int W, int H, TableSection Section)[]
        {
            ("1",  TableShape.Round,     2,  80,  90, 70, 70, window),
            ("2",  TableShape.Round,     2,  80, 200, 70, 70, window),
            ("3",  TableShape.Round,     2,  80, 310, 70, 70, window),
            ("4",  TableShape.Square,    4, 260,  90, 90, 90, mainHall),
            ("5",  TableShape.Square,    4, 260, 220, 90, 90, mainHall),
            ("6",  TableShape.Square,    4, 260, 350, 90, 90, mainHall),
            ("7",  TableShape.Rectangle, 6, 430,  90, 150, 90, mainHall),
            ("8",  TableShape.Rectangle, 6, 430, 220, 150, 90, mainHall),
            ("9",  TableShape.Rectangle, 8, 430, 350, 190, 90, mainHall),
            ("10", TableShape.Booth,     4, 680,  90, 120, 90, mainHall),
            ("11", TableShape.Booth,     4, 680, 220, 120, 90, mainHall),
            ("12", TableShape.Booth,     6, 680, 350, 120, 110, mainHall),
            ("B1", TableShape.BarStool,  1, 900,  90, 50, 50, bar),
            ("B2", TableShape.BarStool,  1, 900, 160, 50, 50, bar),
            ("B3", TableShape.BarStool,  1, 900, 230, 50, 50, bar),
            ("B4", TableShape.BarStool,  1, 900, 300, 50, 50, bar),
            ("T1", TableShape.Round,     4, 1010, 110, 90, 90, terrace),
            ("T2", TableShape.Round,     4, 1010, 240, 90, 90, terrace),
            ("T3", TableShape.Oval,      6, 1000, 370, 120, 90, terrace),
        };

        foreach (var (number, shape, seats, x, y, w, h, section) in layout)
        {
            db.Tables.Add(new DiningTable
            {
                OutletId = outlet.Id,
                FloorId = floor.Id,
                SectionId = section.Id,
                TableNumber = number,
                Shape = shape,
                Seats = seats,
                MaxPartySize = seats + 2,
                PositionX = x,
                PositionY = y,
                Width = w,
                Height = h,
                State = TableState.Free,
                StateChangedAt = DateTime.UtcNow,
                QrToken = Guid.NewGuid().ToString("N")[..12],
            }.StampNew(t));
        }
    }

    private void SeedFixtures(IRestaurantTenant t, Floor floor)
    {
        var fixtures = new (FloorFixtureKind Kind, string? Label, int X, int Y, int W, int H)[]
        {
            (FloorFixtureKind.BarCounter, "Bar", 870, 60, 130, 320),
            (FloorFixtureKind.KitchenPass, "Pass", 430, 500, 200, 40),
            (FloorFixtureKind.Door, "Entrance", 20, 420, 40, 90),
            (FloorFixtureKind.Restroom, "WC", 1020, 500, 90, 60),
            (FloorFixtureKind.Wall, null, 960, 60, 12, 520),
        };

        foreach (var (kind, label, x, y, w, h) in fixtures)
        {
            db.Fixtures.Add(new FloorFixture
            {
                FloorId = floor.Id,
                Kind = kind,
                Label = label,
                PositionX = x,
                PositionY = y,
                Width = w,
                Height = h,
            }.StampNew(t));
        }
    }

    // ── Kitchen ──────────────────────────────────────────────────────────────

    private List<KitchenStation> SeedStations(IRestaurantTenant t, RestaurantOutlet outlet)
    {
        var stations = new List<KitchenStation>
        {
            new() { Name = "Grill",        StationType = StationType.Grill,       DisplayOrder = 0, SlaMinutes = 14, ColorHex = "#ef4444" },
            new() { Name = "Fryer",        StationType = StationType.Fryer,       DisplayOrder = 1, SlaMinutes = 8,  ColorHex = "#f59e0b" },
            new() { Name = "Cold Station", StationType = StationType.ColdStation, DisplayOrder = 2, SlaMinutes = 6,  ColorHex = "#16a34a" },
            new() { Name = "Bar",          StationType = StationType.Bar,         DisplayOrder = 3, SlaMinutes = 5,  ColorHex = "#7c5cff" },
            new() { Name = "Dessert",      StationType = StationType.Dessert,     DisplayOrder = 4, SlaMinutes = 7,  ColorHex = "#db2777" },
            new() { Name = "Pass",         StationType = StationType.Expo,        DisplayOrder = 5, SlaMinutes = 18, ColorHex = "#2b7fff", IsExpo = true },
        };

        foreach (var station in stations)
        {
            station.OutletId = outlet.Id;
            station.StampNew(t);
            db.Stations.Add(station);
        }

        return stations;
    }

    private void SeedRouting(
        IRestaurantTenant t, RestaurantOutlet outlet,
        List<KitchenStation> stations, List<MenuCategory> categories)
    {
        MenuCategory? Category(string name) => categories.FirstOrDefault(c => c.Name == name);

        var rules = new (string StationName, string? CategoryName, RoutingMatchType Match, int Priority)[]
        {
            ("Grill",        "Mains",      RoutingMatchType.Category, 10),
            ("Grill",        "Burgers",    RoutingMatchType.Category, 10),
            ("Fryer",        "Sides",      RoutingMatchType.Category, 20),
            ("Fryer",        "Starters",   RoutingMatchType.Category, 20),
            ("Cold Station", "Salads",     RoutingMatchType.Category, 30),
            ("Bar",          "Drinks",     RoutingMatchType.Category, 40),
            ("Dessert",      "Desserts",   RoutingMatchType.Category, 50),
            // The catch-all sits last so an uncategorised new dish still reaches a cook rather
            // than silently never being made.
            ("Grill",        null,         RoutingMatchType.AllItems, 900),
        };

        foreach (var (stationName, categoryName, match, priority) in rules)
        {
            var station = stations.First(s => s.Name == stationName);
            var category = categoryName is null ? null : Category(categoryName);

            if (categoryName is not null && category is null) continue;

            db.RoutingRules.Add(new StationRoutingRule
            {
                StationId = station.Id,
                OutletId = outlet.Id,
                MatchType = match,
                CategoryId = category?.Id,
                Priority = priority,
            }.StampNew(t));
        }
    }

    private void SeedPrinters(IRestaurantTenant t, RestaurantOutlet outlet)
    {
        db.PrinterProfiles.Add(new PrinterProfile
        {
            OutletId = outlet.Id,
            Name = "Front counter receipt",
            PaperWidthMm = 80,
            IsReceiptPrinter = true,
            OpensCashDrawer = true,
        }.StampNew(t));

        db.PrinterProfiles.Add(new PrinterProfile
        {
            OutletId = outlet.Id,
            Name = "Kitchen ticket printer",
            PaperWidthMm = 80,
            IsKitchenPrinter = true,
            CopiesPerTicket = 1,
        }.StampNew(t));
    }

    // ── Reason codes & charges ───────────────────────────────────────────────

    private void SeedReasonCodes(IRestaurantTenant t)
    {
        var voidReasons = new (string Name, bool Approval, bool Wastage)[]
        {
            ("Ordered by mistake", false, false),
            ("Guest changed mind", false, false),
            ("Wrong item made", true, true),
            ("Quality issue", true, true),
            ("Took too long", true, true),
            ("Allergy — remake", true, true),
            ("Till error", true, false),
        };

        var order = 0;
        foreach (var (name, approval, wastage) in voidReasons)
        {
            db.VoidReasons.Add(new VoidReason
            {
                Name = name,
                DisplayOrder = order++,
                RequiresApproval = approval,
                CountsAsWastage = wastage,
            }.StampNew(t));
        }

        var discountReasons = new (string Name, bool Approval, decimal Ceiling, bool IsComp)[]
        {
            ("Staff meal", true, 0m, true),
            ("Service recovery", true, 0m, true),
            ("Manager comp", true, 0m, true),
            ("Loyalty member", false, 20m, false),
            ("Promotion", false, 20m, false),
            ("Happy hour", false, 20m, false),
            ("Birthday", false, 15m, false),
        };

        order = 0;
        foreach (var (name, approval, ceiling, isComp) in discountReasons)
        {
            db.DiscountReasons.Add(new DiscountReason
            {
                Name = name,
                DisplayOrder = order++,
                RequiresApproval = approval,
                MaxAmountWithoutApproval = ceiling,
                IsComp = isComp,
            }.StampNew(t));
        }
    }

    private ServiceChargeRule SeedServiceCharge(IRestaurantTenant t, RestaurantOutlet outlet)
    {
        // Off by default (Value = 0) but fully configured: a manager turns it on by typing a
        // number, rather than having to work out what a service-charge rule needs.
        var rule = new ServiceChargeRule
        {
            OutletId = outlet.Id,
            Name = "Large party service charge",
            Basis = ServiceChargeBasis.Percentage,
            Value = 0m,
            MinPartySize = 6,
            ApplicableOrderTypes = ((int)OrderType.DineIn).ToString(),
            IsTaxable = true,
            IsWaivable = true,
            RequiresApprovalToWaive = true,
            Description = "Set a percentage to apply an automatic service charge to larger parties.",
        }.StampNew(t);

        db.ServiceChargeRules.Add(rule);
        return rule;
    }

    // ── Staff ────────────────────────────────────────────────────────────────

    private void SeedStaff(IRestaurantTenant t, RestaurantOutlet outlet, List<TableSection> sections)
    {
        // PINs are set here so the till is usable immediately. They are hashed like any other
        // PIN and the Staff screen prompts to change them.
        var people = new (string Name, string Display, StaffRole Role, string Pin, int SectionIndex)[]
        {
            ("Restaurant Manager", "Manager", StaffRole.Manager,   "1379", 0),
            ("Head Chef",          "Chef",    StaffRole.Chef,      "2468", 0),
            ("Front Cashier",      "Cashier", StaffRole.Cashier,   "3690", 0),
            ("Server One",         "Alex",    StaffRole.Waiter,    "4812", 0),
            ("Server Two",         "Sam",     StaffRole.Waiter,    "5923", 1),
            ("Host",               "Host",    StaffRole.Host,      "6034", 0),
        };

        var code = 1;
        foreach (var (name, display, role, pin, sectionIndex) in people)
        {
            var isManager = role is StaffRole.Manager;
            var isSupervisory = isManager || role is StaffRole.Supervisor;

            db.Staff.Add(new RestaurantStaff
            {
                OutletId = outlet.Id,
                Code = $"ST-{code++:D3}",
                FullName = name,
                DisplayName = display,
                Role = role,
                PinHash = BCrypt.Net.BCrypt.HashPassword(pin),
                DefaultSectionId = sections.ElementAtOrDefault(sectionIndex)?.Id,

                CanTakeOrders = role is StaffRole.Waiter or StaffRole.Manager or StaffRole.Cashier or StaffRole.Host,
                CanVoidLines = isSupervisory || role is StaffRole.Cashier,
                CanApplyDiscounts = isSupervisory || role is StaffRole.Cashier,
                CanApproveDiscounts = isSupervisory,
                CanOpenCashDrawer = isSupervisory || role is StaffRole.Cashier,
                CanCloseSession = isSupervisory || role is StaffRole.Cashier,
                CanRunReports = isSupervisory,
                CanEditMenu = isManager || role is StaffRole.Chef,
                CanManageTables = isSupervisory || role is StaffRole.Host or StaffRole.Waiter,
                CanServeAlcohol = role is not StaffRole.KitchenPorter,
            }.StampNew(t));
        }
    }

    // ── Menu ─────────────────────────────────────────────────────────────────

    private MenuCard SeedMenu(IRestaurantTenant t, RestaurantOutlet outlet)
    {
        var menu = new MenuCard
        {
            OutletId = outlet.Id,
            Name = "All Day Menu",
            Daypart = MenuDaypart.AllDay,
            IsDefault = true,
            DisplayOrder = 0,
        }.StampNew(t);

        db.Menus.Add(menu);

        db.Menus.Add(new MenuCard
        {
            OutletId = outlet.Id,
            Name = "Breakfast",
            Daypart = MenuDaypart.Breakfast,
            AvailableFrom = new TimeSpan(7, 0, 0),
            AvailableTo = new TimeSpan(11, 30, 0),
            DisplayOrder = 1,
            IsActive = false,
            Description = "Enable and add items to run a separate breakfast service.",
        }.StampNew(t));

        return menu;
    }

    private List<MenuCategory> SeedCategories(IRestaurantTenant t, MenuCard menu, List<KitchenStation> stations)
    {
        Guid StationId(string name) => stations.First(s => s.Name == name).Id;

        var categories = new List<MenuCategory>
        {
            new() { Name = "Starters", DisplayOrder = 0, ColorHex = "#f59e0b", IconName = "restaurant",       DefaultStationId = StationId("Fryer") },
            new() { Name = "Salads",   DisplayOrder = 1, ColorHex = "#16a34a", IconName = "eco",              DefaultStationId = StationId("Cold Station") },
            new() { Name = "Burgers",  DisplayOrder = 2, ColorHex = "#ef4444", IconName = "lunch_dining",     DefaultStationId = StationId("Grill") },
            new() { Name = "Mains",    DisplayOrder = 3, ColorHex = "#dc2626", IconName = "dinner_dining",    DefaultStationId = StationId("Grill") },
            new() { Name = "Sides",    DisplayOrder = 4, ColorHex = "#d97706", IconName = "fastfood",         DefaultStationId = StationId("Fryer") },
            new() { Name = "Desserts", DisplayOrder = 5, ColorHex = "#db2777", IconName = "icecream",         DefaultStationId = StationId("Dessert") },
            new() { Name = "Drinks",   DisplayOrder = 6, ColorHex = "#7c5cff", IconName = "local_bar",        DefaultStationId = StationId("Bar") },
        };

        foreach (var category in categories)
        {
            category.MenuId = menu.Id;
            category.StampNew(t);
            db.MenuCategories.Add(category);
        }

        return categories;
    }

    private List<ModifierGroup> SeedModifierGroups(IRestaurantTenant t)
    {
        var groups = new List<(ModifierGroup Group, (string Name, decimal Price, bool Removal, bool Default)[] Options)>
        {
            (new ModifierGroup
            {
                Name = "Cooking preference",
                PromptText = "How would you like it cooked?",
                SelectionMode = ModifierSelectionMode.Single,
                IsRequired = true,
                MinSelections = 1,
                MaxSelections = 1,
                DisplayOrder = 0,
            },
            [
                ("Rare", 0m, false, false),
                ("Medium rare", 0m, false, false),
                ("Medium", 0m, false, true),
                ("Medium well", 0m, false, false),
                ("Well done", 0m, false, false),
            ]),

            (new ModifierGroup
            {
                Name = "Burger extras",
                PromptText = "Anything extra?",
                SelectionMode = ModifierSelectionMode.Multiple,
                MaxSelections = 6,
                DisplayOrder = 1,
            },
            [
                ("Extra cheese", 1.50m, false, false),
                ("Bacon", 2.00m, false, false),
                ("Fried egg", 1.50m, false, false),
                ("Avocado", 2.00m, false, false),
                ("Jalapeños", 0.75m, false, false),
                ("Extra patty", 4.00m, false, false),
            ]),

            (new ModifierGroup
            {
                Name = "Hold the…",
                PromptText = "Leave anything out?",
                SelectionMode = ModifierSelectionMode.Multiple,
                MaxSelections = 8,
                DisplayOrder = 2,
            },
            [
                ("Onion", 0m, true, false),
                ("Pickles", 0m, true, false),
                ("Tomato", 0m, true, false),
                ("Lettuce", 0m, true, false),
                ("Sauce", 0m, true, false),
                ("Cheese", 0m, true, false),
            ]),

            (new ModifierGroup
            {
                Name = "Side choice",
                PromptText = "Choose a side",
                SelectionMode = ModifierSelectionMode.Single,
                IsRequired = true,
                MinSelections = 1,
                MaxSelections = 1,
                DisplayOrder = 3,
            },
            [
                ("Fries", 0m, false, true),
                ("Sweet potato fries", 1.50m, false, false),
                ("Side salad", 0m, false, false),
                ("Onion rings", 1.50m, false, false),
                ("Mashed potato", 0m, false, false),
            ]),

            (new ModifierGroup
            {
                Name = "Drink size",
                PromptText = "Which size?",
                SelectionMode = ModifierSelectionMode.Single,
                IsRequired = true,
                MinSelections = 1,
                MaxSelections = 1,
                DisplayOrder = 4,
            },
            [
                ("Regular", 0m, false, true),
                ("Large", 1.00m, false, false),
            ]),

            (new ModifierGroup
            {
                Name = "Spice level",
                PromptText = "How spicy?",
                SelectionMode = ModifierSelectionMode.Single,
                MaxSelections = 1,
                DisplayOrder = 5,
            },
            [
                ("Mild", 0m, false, true),
                ("Medium", 0m, false, false),
                ("Hot", 0m, false, false),
                ("Extra hot", 0m, false, false),
            ]),
        };

        var result = new List<ModifierGroup>();

        foreach (var (group, options) in groups)
        {
            group.StampNew(t);
            db.ModifierGroups.Add(group);

            var order = 0;
            foreach (var (name, price, removal, isDefault) in options)
            {
                db.Modifiers.Add(new Modifier
                {
                    ModifierGroupId = group.Id,
                    Name = name,
                    PriceDelta = price,
                    CostDelta = Math.Round(price * 0.3m, 2),
                    DisplayOrder = order++,
                    IsDefault = isDefault,
                    IsRemoval = removal,
                }.StampNew(t));
            }

            result.Add(group);
        }

        return result;
    }

    private List<MenuItem> SeedItems(
        IRestaurantTenant t, RestaurantOutlet outlet,
        List<MenuCategory> categories, List<KitchenStation> stations, List<ModifierGroup> modifierGroups)
    {
        Guid CategoryId(string name) => categories.First(c => c.Name == name).Id;
        Guid StationId(string name) => stations.First(s => s.Name == name).Id;
        ModifierGroup Group(string name) => modifierGroups.First(g => g.Name == name);

        var definitions = new (string Category, string Name, decimal Price, decimal Cost, CourseType Course,
                               int Prep, string Station, string[] Modifiers, bool Veg, SpiceLevel Spice, int? Cals)[]
        {
            ("Starters", "Garlic Bread",         5.50m, 1.20m, CourseType.Appetizer, 8,  "Fryer", [], true,  SpiceLevel.None,   320),
            ("Starters", "Chicken Wings",        9.50m, 3.40m, CourseType.Appetizer, 12, "Fryer", ["Spice level"], false, SpiceLevel.Medium, 640),
            ("Starters", "Calamari",            10.50m, 4.10m, CourseType.Appetizer, 10, "Fryer", [], false, SpiceLevel.None,   520),
            ("Starters", "Soup of the Day",      6.50m, 1.60m, CourseType.Soup,      6,  "Cold Station", [], true, SpiceLevel.None, 240),

            ("Salads",   "Caesar Salad",         9.00m, 2.60m, CourseType.Salad,     7,  "Cold Station", [], true,  SpiceLevel.None, 380),
            ("Salads",   "Greek Salad",          8.50m, 2.40m, CourseType.Salad,     7,  "Cold Station", [], true,  SpiceLevel.None, 310),
            ("Salads",   "Grilled Chicken Salad",12.00m, 4.20m, CourseType.Salad,    10, "Grill", [], false, SpiceLevel.None, 450),

            ("Burgers",  "Classic Cheeseburger", 13.50m, 4.60m, CourseType.Main,     14, "Grill", ["Cooking preference", "Burger extras", "Hold the…", "Side choice"], false, SpiceLevel.None, 820),
            ("Burgers",  "Bacon Double",         16.50m, 6.10m, CourseType.Main,     16, "Grill", ["Cooking preference", "Burger extras", "Hold the…", "Side choice"], false, SpiceLevel.None, 1100),
            ("Burgers",  "Crispy Chicken Burger",13.00m, 4.30m, CourseType.Main,     13, "Fryer", ["Burger extras", "Hold the…", "Side choice"], false, SpiceLevel.Mild, 780),
            ("Burgers",  "Veggie Burger",        12.00m, 3.50m, CourseType.Main,     12, "Grill", ["Burger extras", "Hold the…", "Side choice"], true,  SpiceLevel.None, 620),

            ("Mains",    "Ribeye Steak",         28.00m,11.20m, CourseType.Main,     20, "Grill", ["Cooking preference", "Side choice"], false, SpiceLevel.None, 900),
            ("Mains",    "Grilled Salmon",       22.00m, 8.60m, CourseType.Main,     16, "Grill", ["Side choice"], false, SpiceLevel.None, 640),
            ("Mains",    "Roast Chicken",        18.50m, 6.20m, CourseType.Main,     18, "Grill", ["Side choice"], false, SpiceLevel.None, 720),
            ("Mains",    "Pasta Primavera",      15.00m, 4.10m, CourseType.Main,     14, "Grill", ["Spice level"], true,  SpiceLevel.None, 680),

            ("Sides",    "French Fries",          4.50m, 0.90m, CourseType.Side,     6,  "Fryer", [], true,  SpiceLevel.None, 400),
            ("Sides",    "Sweet Potato Fries",    5.50m, 1.30m, CourseType.Side,     7,  "Fryer", [], true,  SpiceLevel.None, 420),
            ("Sides",    "Onion Rings",           5.00m, 1.10m, CourseType.Side,     7,  "Fryer", [], true,  SpiceLevel.None, 450),
            ("Sides",    "Seasonal Vegetables",   5.00m, 1.40m, CourseType.Side,     6,  "Grill", [], true,  SpiceLevel.None, 160),

            ("Desserts", "Chocolate Brownie",     7.50m, 1.80m, CourseType.Dessert,  6,  "Dessert", [], true, SpiceLevel.None, 520),
            ("Desserts", "Cheesecake",            7.50m, 2.10m, CourseType.Dessert,  4,  "Dessert", [], true, SpiceLevel.None, 480),
            ("Desserts", "Ice Cream",             5.00m, 1.20m, CourseType.Dessert,  3,  "Dessert", [], true, SpiceLevel.None, 300),

            ("Drinks",   "Soft Drink",            3.50m, 0.60m, CourseType.Beverage, 2,  "Bar", ["Drink size"], true, SpiceLevel.None, 140),
            ("Drinks",   "Fresh Orange Juice",    5.00m, 1.40m, CourseType.Beverage, 3,  "Bar", ["Drink size"], true, SpiceLevel.None, 120),
            ("Drinks",   "Coffee",                3.50m, 0.50m, CourseType.Beverage, 3,  "Bar", [], true, SpiceLevel.None, 10),
            ("Drinks",   "Draft Beer",            6.50m, 1.80m, CourseType.Beverage, 2,  "Bar", ["Drink size"], true, SpiceLevel.None, 180),
            ("Drinks",   "House Wine",            8.00m, 2.60m, CourseType.Beverage, 2,  "Bar", [], true, SpiceLevel.None, 160),
        };

        var items = new List<MenuItem>();
        var code = 1;
        var displayOrder = new Dictionary<string, int>();

        foreach (var d in definitions)
        {
            displayOrder.TryGetValue(d.Category, out var order);
            displayOrder[d.Category] = order + 1;

            var item = new MenuItem
            {
                Code = $"MI-{code++:D3}",
                CategoryId = CategoryId(d.Category),
                Name = d.Name,
                DisplayOrder = order,
                BasePrice = d.Price,
                StandardCost = d.Cost,
                StationId = StationId(d.Station),
                DefaultCourse = d.Course,
                PrepTimeMinutes = d.Prep,
                IsVegetarian = d.Veg,
                SpiceLevel = d.Spice,
                Calories = d.Cals,
                IsAlcohol = d.Name is "Draft Beer" or "House Wine",
                IsAvailable = true,
            }.StampNew(t);

            db.MenuItems.Add(item);
            items.Add(item);

            var linkOrder = 0;
            foreach (var groupName in d.Modifiers)
            {
                db.MenuItemModifierGroups.Add(new MenuItemModifierGroup
                {
                    MenuItemId = item.Id,
                    ModifierGroupId = Group(groupName).Id,
                    DisplayOrder = linkOrder++,
                }.StampNew(t));
            }

            // Takeaway is priced a little under dine-in — there is no table to turn, and it is
            // the most common real-world price-scope difference.
            db.MenuItemPrices.Add(new MenuItemPrice
            {
                MenuItemId = item.Id,
                OutletId = outlet.Id,
                Scope = PriceScope.Takeaway,
                Price = Math.Round(d.Price * 0.95m, 2),
            }.StampNew(t));
        }

        // Sized drinks, so the variant path is exercised out of the box.
        var softDrink = items.First(i => i.Name == "Soft Drink");
        foreach (var (name, price, isDefault) in new (string, decimal, bool)[]
                 { ("Regular", 3.50m, true), ("Large", 4.50m, false) })
        {
            db.MenuItemVariants.Add(new MenuItemVariant
            {
                MenuItemId = softDrink.Id,
                Name = name,
                Price = price,
                StandardCost = Math.Round(price * 0.18m, 2),
                IsDefault = isDefault,
                DisplayOrder = isDefault ? 0 : 1,
            }.StampNew(t));
        }

        return items;
    }

    private void SeedCombos(IRestaurantTenant t, RestaurantOutlet outlet, List<MenuItem> items)
    {
        MenuItem Item(string name) => items.First(i => i.Name == name);

        var combo = new ComboMeal
        {
            OutletId = outlet.Id,
            Code = "CMB-001",
            Name = "Burger Meal Deal",
            Price = 18.50m,
            StandardCost = 6.10m,
            DisplayOrder = 0,
            Description = "Any burger, a side and a drink.",
        }.StampNew(t);

        db.Combos.Add(combo);

        var burgerSlot = new ComboComponent
        {
            ComboMealId = combo.Id,
            Name = "Choose a burger",
            Mode = ComboComponentMode.ChooseOne,
            DisplayOrder = 0,
        }.StampNew(t);

        var sideSlot = new ComboComponent
        {
            ComboMealId = combo.Id,
            Name = "Choose a side",
            Mode = ComboComponentMode.ChooseOne,
            DisplayOrder = 1,
        }.StampNew(t);

        var drinkSlot = new ComboComponent
        {
            ComboMealId = combo.Id,
            Name = "Choose a drink",
            Mode = ComboComponentMode.ChooseOne,
            DisplayOrder = 2,
        }.StampNew(t);

        db.ComboComponents.AddRange(burgerSlot, sideSlot, drinkSlot);

        void Option(ComboComponent slot, string itemName, decimal upcharge, bool isDefault, int order)
        {
            db.ComboComponentOptions.Add(new ComboComponentOption
            {
                ComboComponentId = slot.Id,
                MenuItemId = Item(itemName).Id,
                UpchargeAmount = upcharge,
                IsDefault = isDefault,
                DisplayOrder = order,
            }.StampNew(t));
        }

        Option(burgerSlot, "Classic Cheeseburger", 0m, true, 0);
        Option(burgerSlot, "Crispy Chicken Burger", 0m, false, 1);
        Option(burgerSlot, "Veggie Burger", 0m, false, 2);
        Option(burgerSlot, "Bacon Double", 3.00m, false, 3);

        Option(sideSlot, "French Fries", 0m, true, 0);
        Option(sideSlot, "Sweet Potato Fries", 1.00m, false, 1);
        Option(sideSlot, "Onion Rings", 1.00m, false, 2);

        Option(drinkSlot, "Soft Drink", 0m, true, 0);
        Option(drinkSlot, "Fresh Orange Juice", 1.50m, false, 1);
    }

    private void SeedHappyHour(IRestaurantTenant t, RestaurantOutlet outlet, List<MenuCategory> categories)
    {
        var drinks = categories.FirstOrDefault(c => c.Name == "Drinks");
        if (drinks is null) return;

        db.HappyHourRules.Add(new HappyHourRule
        {
            OutletId = outlet.Id,
            Name = "Happy Hour — drinks",
            StartTime = new TimeSpan(16, 0, 0),
            EndTime = new TimeSpan(18, 30, 0),
            ActiveDays = "1,2,3,4,5",
            DiscountKind = DiscountKind.Percentage,
            DiscountValue = 0m,
            CategoryId = drinks.Id,
            ApplicableOrderTypes = ((int)OrderType.DineIn).ToString(),
            Priority = 10,
            IsActive = false,
            Description = "Set a discount and enable to run weekday happy hour on drinks.",
        }.StampNew(t));
    }

    // ── Food safety ──────────────────────────────────────────────────────────

    /// <summary>
    /// Seeds the temperature checkpoints and checklists a food business is expected to keep.
    ///
    /// The ranges are the standard safe limits — chilled at or below 5°C, frozen at or below
    /// −18°C, hot held at or above 63°C, cooked core at or above 75°C — so a new venue is
    /// compliant on day one rather than being handed an empty screen and a legal obligation.
    /// A manager adjusts them to local regulation.
    /// </summary>
    private void SeedFoodSafety(IRestaurantTenant t, RestaurantOutlet outlet)
    {
        var checkpoints = new (string Name, TemperatureCheckpointKind Kind, decimal Min, decimal Max, int Hours, string? Location)[]
        {
            ("Walk-in chiller",   TemperatureCheckpointKind.Refrigerator,  0m,   5m,   6,  "Back of house"),
            ("Under-counter fridge", TemperatureCheckpointKind.Refrigerator, 0m, 5m,   12, "Line"),
            ("Freezer",           TemperatureCheckpointKind.Freezer,      -25m, -18m, 12, "Back of house"),
            ("Hot hold / bain-marie", TemperatureCheckpointKind.HotHolding, 63m, 90m, 4,  "Pass"),
            ("Cooked core probe", TemperatureCheckpointKind.CookedCore,     75m,  99m, 0,  "Kitchen"),
            ("Delivery intake",   TemperatureCheckpointKind.DeliveryIntake, 0m,   8m,  0,  "Goods in"),
        };

        var order = 0;
        foreach (var (name, kind, min, max, hours, location) in checkpoints)
        {
            db.TemperatureCheckpoints.Add(new TemperatureCheckpoint
            {
                OutletId = outlet.Id,
                Name = name,
                Kind = kind,
                MinSafeCelsius = min,
                MaxSafeCelsius = max,
                CheckIntervalHours = hours,
                Location = location,
                DisplayOrder = order++,
            }.StampNew(t));
        }

        void Checklist(string name, ChecklistFrequency frequency, TimeSpan? dueAt, StaffRole? role,
                       int displayOrder, (string Text, bool Critical)[] items)
        {
            var checklist = new ComplianceChecklist
            {
                OutletId = outlet.Id,
                Name = name,
                Frequency = frequency,
                DueAt = dueAt,
                AssignedRole = role,
                DisplayOrder = displayOrder,
            }.StampNew(t);

            db.Checklists.Add(checklist);

            var itemOrder = 0;
            foreach (var (text, critical) in items)
            {
                db.ChecklistItems.Add(new ChecklistItem
                {
                    ChecklistId = checklist.Id,
                    Text = text,
                    AnswerType = ChecklistAnswerType.YesNo,
                    IsCritical = critical,
                    DisplayOrder = itemOrder++,
                }.StampNew(t));
            }
        }

        Checklist("Opening checks", ChecklistFrequency.Opening, new TimeSpan(10, 0, 0), StaffRole.Chef, 0,
        [
            ("Fridge and freezer temperatures recorded and in range", true),
            ("Hot holding equipment switched on and up to temperature", true),
            ("Hand wash stations stocked with soap and towels", true),
            ("No damaged or out-of-date stock on the line", true),
            ("Surfaces and equipment clean and sanitised", false),
            ("Probe thermometer working and sanitised", false),
            ("Waste bins emptied and lids fitted", false),
        ]);

        Checklist("Closing checks", ChecklistFrequency.Closing, new TimeSpan(23, 30, 0), StaffRole.Chef, 1,
        [
            ("All hot food either used, chilled down or discarded", true),
            ("Open food covered, labelled and dated", true),
            ("Fridge and freezer temperatures recorded", true),
            ("All equipment cleaned and switched off", false),
            ("Floors swept and mopped", false),
            ("Waste removed and bin area clean", false),
            ("Deliveries area secure", false),
        ]);

        Checklist("Weekly deep clean", ChecklistFrequency.Weekly, null, StaffRole.KitchenPorter, 2,
        [
            ("Extraction canopy and filters cleaned", false),
            ("Behind and under all equipment cleaned", false),
            ("Fridge and freezer seals cleaned and checked", false),
            ("Dry store rotated, oldest to front", false),
            ("Pest control check — no evidence of activity", true),
            ("First aid kit checked and restocked", false),
        ]);

        Checklist("Allergen changeover", ChecklistFrequency.OnDemand, null, StaffRole.Chef, 3,
        [
            ("Work surface cleaned and sanitised before preparation", true),
            ("Separate, colour-coded boards and utensils in use", true),
            ("Hands washed and apron changed", true),
            ("Allergen information checked against the current recipe", true),
        ]);
    }

    private void SeedDeliveryZones(IRestaurantTenant t, RestaurantOutlet outlet)
    {
        var zones = new (string Name, decimal Fee, decimal Minimum, decimal Radius, int Minutes, string Color)[]
        {
            ("Local (0–3 km)",   2.00m, 10m, 3m,  25, "#16a34a"),
            ("Nearby (3–6 km)",  3.50m, 15m, 6m,  35, "#f59e0b"),
            ("Outer (6–10 km)",  5.00m, 25m, 10m, 50, "#ef4444"),
        };

        var order = 0;
        foreach (var (name, fee, minimum, radius, minutes, color) in zones)
        {
            db.DeliveryZones.Add(new DeliveryZone
            {
                OutletId = outlet.Id,
                Name = name,
                DeliveryFee = fee,
                MinimumOrderValue = minimum,
                RadiusKm = radius,
                EstimatedMinutes = minutes,
                DisplayOrder = order++,
                ColorHex = color,
                IsActive = outlet.AcceptsDelivery,
            }.StampNew(t));
        }
    }

    /// <summary>
    /// A few prepped batches so the food-safety screen shows a working label board rather than an
    /// empty one — including one already past its use-by, because seeing what an expired label
    /// looks like is the point of the screen.
    /// </summary>
    private void SeedPrepBatches(IRestaurantTenant t, RestaurantOutlet outlet, List<MenuItem> items)
    {
        var now = DateTime.UtcNow;

        var batches = new (string Name, decimal Qty, string Uom, int PreparedHoursAgo, int ShelfLifeHours, string Where)[]
        {
            ("Burger sauce",    2m,  "litre", 6,  72, "Walk-in, shelf 2"),
            ("Caesar dressing", 1.5m, "litre", 20, 48, "Walk-in, shelf 2"),
            ("Tomato sauce",    4m,  "litre", 30, 96, "Walk-in, shelf 1"),
            ("Diced onion",     3m,  "kg",    50, 24, "Prep fridge"),
        };

        var index = 1;
        foreach (var (name, qty, uom, preparedAgo, shelfLife, where) in batches)
        {
            var preparedAt = now.AddHours(-preparedAgo);

            db.PrepBatches.Add(new PrepBatch
            {
                OutletId = outlet.Id,
                BatchCode = $"PB-{preparedAt:yyMMdd}-{index++:D3}",
                ItemName = name,
                MenuItemId = items.FirstOrDefault(i => i.Name.Contains(name.Split(' ')[0], StringComparison.OrdinalIgnoreCase))?.Id,
                Quantity = qty,
                Uom = uom,
                PreparedAt = preparedAt,
                UseByAt = preparedAt.AddHours(shelfLife),
                StorageLocation = where,
            }.StampNew(t));
        }
    }

    private void SeedRecipes(IRestaurantTenant t, List<MenuItem> items)
    {
        // Ingredients are named rather than linked to Inventory items, because a brand-new
        // company has no item master yet. The Recipes screen prompts to map each line to a real
        // inventory item, at which point depletion starts working.
        var recipes = new (string Item, (string Ingredient, decimal Qty, string Uom, decimal Cost, decimal Yield)[] Lines)[]
        {
            ("Classic Cheeseburger",
            [
                ("Beef patty 150g", 1m,   "ea", 2.10m, 100m),
                ("Burger bun",      1m,   "ea", 0.45m, 100m),
                ("Cheddar slice",   1m,   "ea", 0.35m, 100m),
                ("Lettuce",         20m,  "g",  0.008m, 80m),
                ("Tomato",          30m,  "g",  0.006m, 85m),
                ("Burger sauce",    25m,  "ml", 0.010m, 100m),
            ]),
            ("French Fries",
            [
                ("Potato fries frozen", 180m, "g",  0.004m, 100m),
                ("Frying oil",          15m,  "ml", 0.003m, 100m),
                ("Sea salt",            2m,   "g",  0.002m, 100m),
            ]),
            ("Caesar Salad",
            [
                ("Romaine lettuce", 150m, "g",  0.006m, 75m),
                ("Caesar dressing", 40m,  "ml", 0.012m, 100m),
                ("Parmesan",        20m,  "g",  0.030m, 100m),
                ("Croutons",        25m,  "g",  0.008m, 100m),
            ]),
        };

        foreach (var (itemName, lines) in recipes)
        {
            var item = items.FirstOrDefault(i => i.Name == itemName);
            if (item is null) continue;

            var recipe = new Recipe
            {
                MenuItemId = item.Id,
                Name = $"{itemName} — standard",
                YieldQuantity = 1m,
                YieldUom = "portion",
            }.StampNew(t);

            db.Recipes.Add(recipe);

            var order = 0;
            var total = 0m;

            foreach (var (ingredient, qty, uom, cost, yield) in lines)
            {
                var lineCost = Math.Round(qty / (yield / 100m) * cost, 6);
                total += lineCost;

                db.RecipeIngredients.Add(new RecipeIngredient
                {
                    RecipeId = recipe.Id,
                    IngredientName = ingredient,
                    Quantity = qty,
                    Uom = uom,
                    UnitCost = cost,
                    YieldPercent = yield,
                    LineCost = lineCost,
                    DisplayOrder = order++,
                }.StampNew(t));
            }

            recipe.TotalCost = Math.Round(total, 4);
            item.StandardCost = recipe.TotalCost;
        }
    }
}
