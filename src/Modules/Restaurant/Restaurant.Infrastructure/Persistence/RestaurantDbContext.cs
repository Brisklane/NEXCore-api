using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Persistence;
using Restaurant.Domain.Entities;

namespace Restaurant.Infrastructure.Persistence;

/// <summary>
/// Restaurant module DbContext. Default schema: <c>restaurant</c>.
///
/// The model is organised the way a restaurant is: the venue and its floor plan, the menu that
/// is sold from it, the kitchen that produces it, the orders that flow across all three, the
/// money that closes them, and the costing that explains whether any of it was worth doing.
///
/// Delete behaviour is deliberate throughout. Composition (an order and its lines, a ticket and
/// its lines, a recipe and its ingredients) cascades; every reference that crosses an aggregate
/// boundary is <see cref="DeleteBehavior.NoAction"/> so deleting a section can never take a
/// night's orders with it.
/// </summary>
public class RestaurantDbContext : DbContext
{
    private const string DefaultSchema = "restaurant";

    public RestaurantDbContext(DbContextOptions<RestaurantDbContext> options) : base(options) { }

    // ── Venue & layout ───────────────────────────────────────────────────────
    public DbSet<RestaurantOutlet> Outlets { get; set; } = null!;
    public DbSet<OutletSchedule> OutletSchedules { get; set; } = null!;
    public DbSet<Floor> Floors { get; set; } = null!;
    public DbSet<TableSection> Sections { get; set; } = null!;
    public DbSet<DiningTable> Tables { get; set; } = null!;
    public DbSet<FloorFixture> Fixtures { get; set; } = null!;
    public DbSet<TableStateLog> TableStateLogs { get; set; } = null!;

    // ── Menu ─────────────────────────────────────────────────────────────────
    public DbSet<MenuCard> Menus { get; set; } = null!;
    public DbSet<MenuCategory> MenuCategories { get; set; } = null!;
    public DbSet<MenuItem> MenuItems { get; set; } = null!;
    public DbSet<MenuItemVariant> MenuItemVariants { get; set; } = null!;
    public DbSet<MenuItemPrice> MenuItemPrices { get; set; } = null!;
    public DbSet<HappyHourRule> HappyHourRules { get; set; } = null!;
    public DbSet<ModifierGroup> ModifierGroups { get; set; } = null!;
    public DbSet<Modifier> Modifiers { get; set; } = null!;
    public DbSet<MenuItemModifierGroup> MenuItemModifierGroups { get; set; } = null!;
    public DbSet<ComboMeal> Combos { get; set; } = null!;
    public DbSet<ComboComponent> ComboComponents { get; set; } = null!;
    public DbSet<ComboComponentOption> ComboComponentOptions { get; set; } = null!;
    public DbSet<MenuItemAvailability> MenuItemAvailabilities { get; set; } = null!;

    // ── Kitchen ──────────────────────────────────────────────────────────────
    public DbSet<KitchenStation> Stations { get; set; } = null!;
    public DbSet<StationRoutingRule> RoutingRules { get; set; } = null!;
    public DbSet<KitchenTicket> KitchenTickets { get; set; } = null!;
    public DbSet<KitchenTicketLine> KitchenTicketLines { get; set; } = null!;
    public DbSet<PrinterProfile> PrinterProfiles { get; set; } = null!;

    // ── Orders ───────────────────────────────────────────────────────────────
    public DbSet<RestaurantOrder> Orders { get; set; } = null!;
    public DbSet<RestaurantOrderLine> OrderLines { get; set; } = null!;
    public DbSet<RestaurantOrderLineModifier> OrderLineModifiers { get; set; } = null!;
    public DbSet<OrderStatusHistory> OrderStatusHistory { get; set; } = null!;
    public DbSet<RestaurantDelivery> Deliveries { get; set; } = null!;

    // ── Money ────────────────────────────────────────────────────────────────
    public DbSet<RestaurantCheck> Checks { get; set; } = null!;
    public DbSet<CheckLine> CheckLines { get; set; } = null!;
    public DbSet<CheckPayment> CheckPayments { get; set; } = null!;
    public DbSet<CheckDiscount> CheckDiscounts { get; set; } = null!;
    public DbSet<VoidReason> VoidReasons { get; set; } = null!;
    public DbSet<DiscountReason> DiscountReasons { get; set; } = null!;
    public DbSet<ServiceChargeRule> ServiceChargeRules { get; set; } = null!;
    public DbSet<TipRecord> Tips { get; set; } = null!;
    public DbSet<TipPool> TipPools { get; set; } = null!;
    public DbSet<TipDistribution> TipDistributions { get; set; } = null!;

    // ── Front of house ───────────────────────────────────────────────────────
    public DbSet<Reservation> Reservations { get; set; } = null!;
    public DbSet<WaitlistEntry> Waitlist { get; set; } = null!;
    public DbSet<GuestProfile> Guests { get; set; } = null!;
    public DbSet<CustomerFeedback> Feedback { get; set; } = null!;

    // ── Staff & cash ─────────────────────────────────────────────────────────
    public DbSet<RestaurantStaff> Staff { get; set; } = null!;
    public DbSet<StaffShift> StaffShifts { get; set; } = null!;
    public DbSet<TimeClockEntry> TimeClockEntries { get; set; } = null!;
    public DbSet<RestaurantSession> Sessions { get; set; } = null!;
    public DbSet<SessionCashMovement> CashMovements { get; set; } = null!;

    // ── Food safety & compliance ─────────────────────────────────────────────
    public DbSet<TemperatureCheckpoint> TemperatureCheckpoints { get; set; } = null!;
    public DbSet<TemperatureLog> TemperatureLogs { get; set; } = null!;
    public DbSet<ComplianceChecklist> Checklists { get; set; } = null!;
    public DbSet<ChecklistItem> ChecklistItems { get; set; } = null!;
    public DbSet<ChecklistRun> ChecklistRuns { get; set; } = null!;
    public DbSet<ChecklistAnswer> ChecklistAnswers { get; set; } = null!;
    public DbSet<PrepBatch> PrepBatches { get; set; } = null!;
    public DbSet<DeliveryZone> DeliveryZones { get; set; } = null!;

    // ── Costing ──────────────────────────────────────────────────────────────
    public DbSet<Recipe> Recipes { get; set; } = null!;
    public DbSet<RecipeIngredient> RecipeIngredients { get; set; } = null!;
    public DbSet<WastageLog> WastageLogs { get; set; } = null!;
    public DbSet<RestaurantSettings> Settings { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(DefaultSchema);

        // ═══ Venue ═══════════════════════════════════════════════════════════

        modelBuilder.Entity<RestaurantOutlet>(e =>
        {
            e.ToTable("Outlets", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.CuisineType).HasMaxLength(100);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.AddressLine).HasMaxLength(500);
            e.Property(x => x.City).HasMaxLength(120);
            e.Property(x => x.CountryCode).HasMaxLength(3);
            e.Property(x => x.TimeZoneId).HasMaxLength(80);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.DefaultTaxPercent).HasPrecision(9, 4);
            e.Property(x => x.TakeawayTaxPercent).HasPrecision(9, 4);
            e.Property(x => x.LogoUrl).HasMaxLength(500);
            e.Property(x => x.ReceiptFooter).HasMaxLength(1000);
            e.Property(x => x.ClosureNote).HasMaxLength(500);
            e.Property(x => x.Description).HasMaxLength(1000);

            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.Code })
                .IsUnique().HasDatabaseName("IX_Outlet_Tenant_Code");

            e.HasMany(x => x.Schedules).WithOne(s => s.Outlet)
                .HasForeignKey(s => s.OutletId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Floors).WithOne(f => f.Outlet)
                .HasForeignKey(f => f.OutletId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Stations).WithOne(s => s.Outlet)
                .HasForeignKey(s => s.OutletId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OutletSchedule>(e =>
        {
            e.ToTable("OutletSchedules", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Note).HasMaxLength(300);
            e.HasIndex(x => new { x.OutletId, x.DayOfWeek }).HasDatabaseName("IX_OutletSchedule_Outlet_Day");
        });

        modelBuilder.Entity<Floor>(e =>
        {
            e.ToTable("Floors", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(120);
            e.Property(x => x.BackgroundImageUrl).HasMaxLength(500);
            e.HasIndex(x => new { x.OutletId, x.DisplayOrder }).HasDatabaseName("IX_Floor_Outlet_Order");

            e.HasMany(x => x.Sections).WithOne(s => s.Floor)
                .HasForeignKey(s => s.FloorId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Tables).WithOne(t => t.Floor)
                .HasForeignKey(t => t.FloorId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Fixtures).WithOne(f => f.Floor)
                .HasForeignKey(f => f.FloorId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TableSection>(e =>
        {
            e.ToTable("Sections", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(120);
            e.Property(x => x.ColorHex).HasMaxLength(9);
            e.Property(x => x.MinimumSpend).HasPrecision(18, 2);

            // A table keeps its section reference when a section is retired: reassigning is a
            // decision for a manager, not a side effect of a delete.
            e.HasMany(x => x.Tables).WithOne(t => t.Section)
                .HasForeignKey(t => t.SectionId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<DiningTable>(e =>
        {
            e.ToTable("Tables", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.TableNumber).IsRequired().HasMaxLength(30);
            e.Property(x => x.QrToken).HasMaxLength(64);
            e.Property(x => x.Note).HasMaxLength(500);

            e.HasIndex(x => new { x.OutletId, x.TableNumber })
                .IsUnique().HasDatabaseName("IX_Table_Outlet_Number");
            e.HasIndex(x => new { x.OutletId, x.State }).HasDatabaseName("IX_Table_Outlet_State");
            e.HasIndex(x => x.QrToken).HasDatabaseName("IX_Table_QrToken");
        });

        modelBuilder.Entity<FloorFixture>(e =>
        {
            e.ToTable("Fixtures", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Label).HasMaxLength(120);
            e.Property(x => x.ColorHex).HasMaxLength(9);
        });

        modelBuilder.Entity<TableStateLog>(e =>
        {
            e.ToTable("TableStateLogs", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Note).HasMaxLength(300);
            e.HasIndex(x => new { x.OutletId, x.OccurredAt }).HasDatabaseName("IX_TableStateLog_Outlet_At");
            e.HasOne(x => x.Table).WithMany()
                .HasForeignKey(x => x.TableId).OnDelete(DeleteBehavior.NoAction);
        });

        // ═══ Menu ════════════════════════════════════════════════════════════

        modelBuilder.Entity<MenuCard>(e =>
        {
            e.ToTable("Menus", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.ActiveDays).HasMaxLength(30);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.HasMany(x => x.Categories).WithOne(c => c.Menu)
                .HasForeignKey(c => c.MenuId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MenuCategory>(e =>
        {
            e.ToTable("MenuCategories", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.ColorHex).HasMaxLength(9);
            e.Property(x => x.IconName).HasMaxLength(60);
            e.Property(x => x.ImageUrl).HasMaxLength(500);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.HasIndex(x => new { x.MenuId, x.DisplayOrder }).HasDatabaseName("IX_MenuCategory_Menu_Order");
            e.HasMany(x => x.Items).WithOne(i => i.Category)
                .HasForeignKey(i => i.CategoryId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MenuItem>(e =>
        {
            e.ToTable("MenuItems", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.ShortName).HasMaxLength(60);
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.ImageUrl).HasMaxLength(500);
            e.Property(x => x.Allergens).HasMaxLength(500);
            e.Property(x => x.KitchenNote).HasMaxLength(500);
            e.Property(x => x.Barcode).HasMaxLength(64);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.BasePrice).HasPrecision(18, 4);
            e.Property(x => x.StandardCost).HasPrecision(18, 4);
            e.Property(x => x.TaxPercent).HasPrecision(9, 4);

            e.HasIndex(x => new { x.CompanyId, x.Code }).HasDatabaseName("IX_MenuItem_Company_Code");
            e.HasIndex(x => new { x.CategoryId, x.DisplayOrder }).HasDatabaseName("IX_MenuItem_Category_Order");
            e.HasIndex(x => x.Barcode).HasDatabaseName("IX_MenuItem_Barcode");

            e.HasMany(x => x.Variants).WithOne(v => v.MenuItem)
                .HasForeignKey(v => v.MenuItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Prices).WithOne(p => p.MenuItem)
                .HasForeignKey(p => p.MenuItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.ModifierGroups).WithOne(m => m.MenuItem)
                .HasForeignKey(m => m.MenuItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Recipes).WithOne(r => r.MenuItem)
                .HasForeignKey(r => r.MenuItemId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<MenuItemVariant>(e =>
        {
            e.ToTable("MenuItemVariants", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Barcode).HasMaxLength(64);
            e.Property(x => x.Price).HasPrecision(18, 4);
            e.Property(x => x.StandardCost).HasPrecision(18, 4);
        });

        modelBuilder.Entity<MenuItemPrice>(e =>
        {
            e.ToTable("MenuItemPrices", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Price).HasPrecision(18, 4);
            e.HasIndex(x => new { x.MenuItemId, x.Scope, x.OutletId })
                .HasDatabaseName("IX_MenuItemPrice_Item_Scope");
        });

        modelBuilder.Entity<HappyHourRule>(e =>
        {
            e.ToTable("HappyHourRules", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.ActiveDays).HasMaxLength(30);
            e.Property(x => x.ApplicableOrderTypes).HasMaxLength(60);
            e.Property(x => x.DiscountValue).HasPrecision(18, 4);
            e.Property(x => x.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<ModifierGroup>(e =>
        {
            e.ToTable("ModifierGroups", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.PromptText).HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.HasMany(x => x.Modifiers).WithOne(m => m.ModifierGroup)
                .HasForeignKey(m => m.ModifierGroupId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Modifier>(e =>
        {
            e.ToTable("Modifiers", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.ConsumptionUom).HasMaxLength(20);
            e.Property(x => x.PriceDelta).HasPrecision(18, 4);
            e.Property(x => x.CostDelta).HasPrecision(18, 4);
            e.Property(x => x.ConsumptionQuantity).HasPrecision(18, 6);
        });

        modelBuilder.Entity<MenuItemModifierGroup>(e =>
        {
            e.ToTable("MenuItemModifierGroups", DefaultSchema);
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.MenuItemId, x.ModifierGroupId })
                .IsUnique().HasDatabaseName("IX_MenuItemModifierGroup_Pair");
            e.HasOne(x => x.ModifierGroup).WithMany()
                .HasForeignKey(x => x.ModifierGroupId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ComboMeal>(e =>
        {
            e.ToTable("Combos", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.ImageUrl).HasMaxLength(500);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Price).HasPrecision(18, 4);
            e.Property(x => x.StandardCost).HasPrecision(18, 4);
            e.HasMany(x => x.Components).WithOne(c => c.ComboMeal)
                .HasForeignKey(c => c.ComboMealId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ComboComponent>(e =>
        {
            e.ToTable("ComboComponents", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.HasMany(x => x.Options).WithOne(o => o.ComboComponent)
                .HasForeignKey(o => o.ComboComponentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ComboComponentOption>(e =>
        {
            e.ToTable("ComboComponentOptions", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.UpchargeAmount).HasPrecision(18, 4);
        });

        modelBuilder.Entity<MenuItemAvailability>(e =>
        {
            e.ToTable("MenuItemAvailabilities", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Reason).HasMaxLength(300);
            e.HasIndex(x => new { x.OutletId, x.MenuItemId, x.VariantId })
                .IsUnique().HasDatabaseName("IX_Availability_Outlet_Item");
        });

        // ═══ Kitchen ═════════════════════════════════════════════════════════

        modelBuilder.Entity<KitchenStation>(e =>
        {
            e.ToTable("Stations", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(120);
            e.Property(x => x.ColorHex).HasMaxLength(9);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.HasIndex(x => new { x.OutletId, x.DisplayOrder }).HasDatabaseName("IX_Station_Outlet_Order");
            e.HasMany(x => x.RoutingRules).WithOne(r => r.Station)
                .HasForeignKey(r => r.StationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StationRoutingRule>(e =>
        {
            e.ToTable("RoutingRules", DefaultSchema);
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.OutletId, x.Priority }).HasDatabaseName("IX_RoutingRule_Outlet_Priority");
        });

        modelBuilder.Entity<KitchenTicket>(e =>
        {
            e.ToTable("KitchenTickets", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.TicketNumber).IsRequired().HasMaxLength(30);
            e.Property(x => x.TableNumber).HasMaxLength(30);
            e.Property(x => x.WaiterName).HasMaxLength(150);
            e.Property(x => x.Note).HasMaxLength(500);

            e.HasIndex(x => new { x.OutletId, x.StationId, x.Status })
                .HasDatabaseName("IX_KitchenTicket_Station_Status");
            e.HasIndex(x => new { x.OutletId, x.FiredAt }).HasDatabaseName("IX_KitchenTicket_Outlet_Fired");

            e.HasOne(x => x.Station).WithMany()
                .HasForeignKey(x => x.StationId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Order).WithMany(o => o.Tickets)
                .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Lines).WithOne(l => l.Ticket)
                .HasForeignKey(l => l.TicketId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KitchenTicketLine>(e =>
        {
            e.ToTable("KitchenTicketLines", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
            e.Property(x => x.VariantName).HasMaxLength(100);
            e.Property(x => x.ModifierSummary).HasMaxLength(1000);
            e.Property(x => x.SpecialInstructions).HasMaxLength(500);
            e.Property(x => x.AllergenWarning).HasMaxLength(300);
            e.Property(x => x.Quantity).HasPrecision(18, 3);
        });

        modelBuilder.Entity<PrinterProfile>(e =>
        {
            e.ToTable("PrinterProfiles", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(120);
            e.Property(x => x.Target).HasMaxLength(300);
            e.Property(x => x.HeaderText).HasMaxLength(500);
            e.Property(x => x.FooterText).HasMaxLength(500);
        });

        // ═══ Orders ══════════════════════════════════════════════════════════

        modelBuilder.Entity<RestaurantOrder>(e =>
        {
            e.ToTable("Orders", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.OrderNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.TokenNumber).HasMaxLength(20);
            e.Property(x => x.TableNumber).HasMaxLength(30);
            e.Property(x => x.WaiterName).HasMaxLength(150);
            e.Property(x => x.CustomerName).HasMaxLength(200);
            e.Property(x => x.CustomerPhone).HasMaxLength(50);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.ExternalSource).HasMaxLength(80);
            e.Property(x => x.ExternalReference).HasMaxLength(120);
            e.Property(x => x.IdempotencyKey).HasMaxLength(80);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.CancelReason).HasMaxLength(500);

            foreach (var money in new[]
            {
                nameof(RestaurantOrder.SubTotal), nameof(RestaurantOrder.DiscountAmount),
                nameof(RestaurantOrder.ServiceChargeAmount), nameof(RestaurantOrder.PackagingChargeAmount),
                nameof(RestaurantOrder.DeliveryFeeAmount), nameof(RestaurantOrder.TaxAmount),
                nameof(RestaurantOrder.TipAmount), nameof(RestaurantOrder.RoundingAmount),
                nameof(RestaurantOrder.TotalAmount), nameof(RestaurantOrder.PaidAmount),
                nameof(RestaurantOrder.CostAmount),
            })
                e.Property(money).HasPrecision(18, 4);

            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.OrderNumber })
                .IsUnique().HasDatabaseName("IX_Order_Tenant_Number");
            e.HasIndex(x => new { x.OutletId, x.Status }).HasDatabaseName("IX_Order_Outlet_Status");
            e.HasIndex(x => new { x.OutletId, x.OpenedAt }).HasDatabaseName("IX_Order_Outlet_Opened");
            e.HasIndex(x => x.TableId).HasDatabaseName("IX_Order_Table");

            // Idempotency is enforced in the database, not just in the service: two concurrent
            // retries of the same submission must not both win.
            e.HasIndex(x => new { x.CompanyId, x.IdempotencyKey })
                .IsUnique().HasDatabaseName("IX_Order_Idempotency")
                .HasFilter("idempotency_key IS NOT NULL");

            e.HasOne(x => x.Table).WithMany()
                .HasForeignKey(x => x.TableId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Lines).WithOne(l => l.Order)
                .HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.StatusHistory).WithOne(h => h.Order)
                .HasForeignKey(h => h.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Checks).WithOne(c => c.Order)
                .HasForeignKey(c => c.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RestaurantOrderLine>(e =>
        {
            e.ToTable("OrderLines", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
            e.Property(x => x.VariantName).HasMaxLength(100);
            e.Property(x => x.SpecialInstructions).HasMaxLength(500);
            e.Property(x => x.VoidNote).HasMaxLength(500);
            e.Property(x => x.Quantity).HasPrecision(18, 3);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.ModifierAmount).HasPrecision(18, 4);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 4);
            e.Property(x => x.TaxAmount).HasPrecision(18, 4);
            e.Property(x => x.LineTotal).HasPrecision(18, 4);
            e.Property(x => x.UnitCost).HasPrecision(18, 4);
            e.Property(x => x.TaxPercent).HasPrecision(9, 4);

            e.HasIndex(x => new { x.OrderId, x.DisplayOrder }).HasDatabaseName("IX_OrderLine_Order_Order");
            e.HasIndex(x => new { x.OrderId, x.Status }).HasDatabaseName("IX_OrderLine_Order_Status");
            e.HasIndex(x => x.MenuItemId).HasDatabaseName("IX_OrderLine_MenuItem");

            e.HasMany(x => x.Modifiers).WithOne(m => m.OrderLine)
                .HasForeignKey(m => m.OrderLineId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RestaurantOrderLineModifier>(e =>
        {
            e.ToTable("OrderLineModifiers", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ModifierName).IsRequired().HasMaxLength(150);
            e.Property(x => x.GroupName).HasMaxLength(150);
            e.Property(x => x.ConsumptionUom).HasMaxLength(20);
            e.Property(x => x.Quantity).HasPrecision(18, 3);
            e.Property(x => x.PriceDelta).HasPrecision(18, 4);
            e.Property(x => x.CostDelta).HasPrecision(18, 4);
            e.Property(x => x.ConsumptionQuantity).HasPrecision(18, 6);
        });

        modelBuilder.Entity<OrderStatusHistory>(e =>
        {
            e.ToTable("OrderStatusHistory", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.StaffName).HasMaxLength(150);
            e.Property(x => x.Note).HasMaxLength(500);
        });

        modelBuilder.Entity<RestaurantDelivery>(e =>
        {
            e.ToTable("Deliveries", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.RecipientName).HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.AddressLine).HasMaxLength(500);
            e.Property(x => x.Landmark).HasMaxLength(200);
            e.Property(x => x.City).HasMaxLength(120);
            e.Property(x => x.ZoneName).HasMaxLength(120);
            e.Property(x => x.RiderName).HasMaxLength(150);
            e.Property(x => x.RiderPhone).HasMaxLength(50);
            e.Property(x => x.FailureReason).HasMaxLength(500);
            e.Property(x => x.DeliveryNote).HasMaxLength(500);
            e.Property(x => x.DeliveryFee).HasPrecision(18, 4);
            e.Property(x => x.DistanceKm).HasPrecision(10, 3);

            e.HasIndex(x => new { x.OutletId, x.Status }).HasDatabaseName("IX_Delivery_Outlet_Status");
            e.HasOne(x => x.Order).WithMany()
                .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        // ═══ Money ═══════════════════════════════════════════════════════════

        modelBuilder.Entity<RestaurantCheck>(e =>
        {
            e.ToTable("Checks", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.CheckNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.SeatNumbers).HasMaxLength(200);
            e.Property(x => x.CashierName).HasMaxLength(150);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.FiscalReference).HasMaxLength(500);
            e.Property(x => x.VoidNote).HasMaxLength(500);

            foreach (var money in new[]
            {
                nameof(RestaurantCheck.SubTotal), nameof(RestaurantCheck.DiscountAmount),
                nameof(RestaurantCheck.ServiceChargeAmount), nameof(RestaurantCheck.PackagingChargeAmount),
                nameof(RestaurantCheck.DeliveryFeeAmount), nameof(RestaurantCheck.TaxAmount),
                nameof(RestaurantCheck.TipAmount), nameof(RestaurantCheck.RoundingAmount),
                nameof(RestaurantCheck.TotalAmount), nameof(RestaurantCheck.PaidAmount),
                nameof(RestaurantCheck.ChangeAmount),
            })
                e.Property(money).HasPrecision(18, 4);

            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.CheckNumber })
                .IsUnique().HasDatabaseName("IX_Check_Tenant_Number");
            e.HasIndex(x => new { x.OutletId, x.Status }).HasDatabaseName("IX_Check_Outlet_Status");
            e.HasIndex(x => x.SessionId).HasDatabaseName("IX_Check_Session");

            e.HasMany(x => x.Lines).WithOne(l => l.Check)
                .HasForeignKey(l => l.CheckId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Payments).WithOne(p => p.Check)
                .HasForeignKey(p => p.CheckId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Discounts).WithOne(d => d.Check)
                .HasForeignKey(d => d.CheckId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CheckLine>(e =>
        {
            e.ToTable("CheckLines", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
            e.Property(x => x.VariantName).HasMaxLength(100);
            e.Property(x => x.ModifierSummary).HasMaxLength(1000);
            e.Property(x => x.Quantity).HasPrecision(18, 3);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 4);
            e.Property(x => x.TaxAmount).HasPrecision(18, 4);
            e.Property(x => x.LineTotal).HasPrecision(18, 4);
            e.Property(x => x.UnitCost).HasPrecision(18, 4);
        });

        modelBuilder.Entity<CheckPayment>(e =>
        {
            e.ToTable("CheckPayments", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.Reference).HasMaxLength(200);
            e.Property(x => x.CardLast4).HasMaxLength(4);
            e.Property(x => x.CardScheme).HasMaxLength(40);
            e.Property(x => x.AuthCode).HasMaxLength(60);
            e.Property(x => x.RefundReason).HasMaxLength(500);
            e.Property(x => x.Amount).HasPrecision(18, 4);
            e.Property(x => x.TenderedAmount).HasPrecision(18, 4);
            e.Property(x => x.ChangeAmount).HasPrecision(18, 4);
            e.Property(x => x.TipAmount).HasPrecision(18, 4);
            e.Property(x => x.ExchangeRate).HasPrecision(18, 8);
            e.HasIndex(x => new { x.OutletId, x.PaidAt }).HasDatabaseName("IX_CheckPayment_Outlet_At");
            e.HasIndex(x => x.SessionId).HasDatabaseName("IX_CheckPayment_Session");
        });

        modelBuilder.Entity<CheckDiscount>(e =>
        {
            e.ToTable("CheckDiscounts", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ReasonName).HasMaxLength(150);
            e.Property(x => x.PromoCode).HasMaxLength(60);
            e.Property(x => x.Value).HasPrecision(18, 4);
            e.Property(x => x.Amount).HasPrecision(18, 4);
        });

        modelBuilder.Entity<VoidReason>(e =>
        {
            e.ToTable("VoidReasons", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<DiscountReason>(e =>
        {
            e.ToTable("DiscountReasons", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.MaxAmountWithoutApproval).HasPrecision(18, 4);
        });

        modelBuilder.Entity<ServiceChargeRule>(e =>
        {
            e.ToTable("ServiceChargeRules", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.ApplicableOrderTypes).HasMaxLength(60);
            e.Property(x => x.Value).HasPrecision(18, 4);
            e.Property(x => x.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<TipRecord>(e =>
        {
            e.ToTable("Tips", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.WaiterName).HasMaxLength(150);
            e.Property(x => x.Amount).HasPrecision(18, 4);
            e.HasIndex(x => new { x.OutletId, x.ReceivedAt }).HasDatabaseName("IX_Tip_Outlet_At");
        });

        modelBuilder.Entity<TipPool>(e =>
        {
            e.ToTable("TipPools", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.TotalAmount).HasPrecision(18, 4);
            e.Property(x => x.DistributedAmount).HasPrecision(18, 4);
            e.Property(x => x.KitchenSharePercent).HasPrecision(9, 4);
            e.HasMany(x => x.Distributions).WithOne(d => d.TipPool)
                .HasForeignKey(d => d.TipPoolId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TipDistribution>(e =>
        {
            e.ToTable("TipDistributions", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.StaffName).HasMaxLength(150);
            e.Property(x => x.HoursWorked).HasPrecision(10, 3);
            e.Property(x => x.SalesAmount).HasPrecision(18, 4);
            e.Property(x => x.SharePercent).HasPrecision(9, 4);
            e.Property(x => x.Amount).HasPrecision(18, 4);
        });

        // ═══ Front of house ══════════════════════════════════════════════════

        modelBuilder.Entity<Reservation>(e =>
        {
            e.ToTable("Reservations", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ReservationNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.GuestName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Occasion).HasMaxLength(120);
            e.Property(x => x.SpecialRequests).HasMaxLength(1000);
            e.Property(x => x.AllergyNotes).HasMaxLength(500);
            e.Property(x => x.CancelReason).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.DepositAmount).HasPrecision(18, 4);

            e.HasIndex(x => new { x.OutletId, x.ReservedFor }).HasDatabaseName("IX_Reservation_Outlet_For");
            e.HasIndex(x => new { x.OutletId, x.Status }).HasDatabaseName("IX_Reservation_Outlet_Status");
            e.HasOne(x => x.Table).WithMany()
                .HasForeignKey(x => x.TableId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<WaitlistEntry>(e =>
        {
            e.ToTable("Waitlist", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.GuestName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.PagerNumber).HasMaxLength(20);
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasIndex(x => new { x.OutletId, x.Status, x.JoinedAt }).HasDatabaseName("IX_Waitlist_Outlet_Status");
        });

        modelBuilder.Entity<GuestProfile>(e =>
        {
            e.ToTable("Guests", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.DietaryPreferences).HasMaxLength(500);
            e.Property(x => x.Allergies).HasMaxLength(500);
            e.Property(x => x.FavouriteItems).HasMaxLength(1000);
            e.Property(x => x.PreferredSeating).HasMaxLength(200);
            e.Property(x => x.BlacklistReason).HasMaxLength(500);
            e.Property(x => x.LoyaltyTier).HasMaxLength(60);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.Property(x => x.LifetimeSpend).HasPrecision(18, 4);
            e.Property(x => x.AverageCheck).HasPrecision(18, 4);
            e.HasIndex(x => new { x.CompanyId, x.Phone }).HasDatabaseName("IX_Guest_Company_Phone");
        });

        modelBuilder.Entity<CustomerFeedback>(e =>
        {
            e.ToTable("Feedback", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Comment).HasMaxLength(2000);
            e.Property(x => x.GuestName).HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.ResolutionNote).HasMaxLength(1000);
            e.HasIndex(x => new { x.OutletId, x.SubmittedAt }).HasDatabaseName("IX_Feedback_Outlet_At");
        });

        // ═══ Staff & cash ════════════════════════════════════════════════════

        modelBuilder.Entity<RestaurantStaff>(e =>
        {
            e.ToTable("Staff", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            e.Property(x => x.DisplayName).HasMaxLength(80);
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.PhotoUrl).HasMaxLength(500);
            e.Property(x => x.PinHash).HasMaxLength(200);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.HourlyRate).HasPrecision(18, 4);
            e.Property(x => x.TipSharePercent).HasPrecision(9, 4);
            e.HasIndex(x => new { x.OutletId, x.Code }).HasDatabaseName("IX_Staff_Outlet_Code");
            e.HasIndex(x => new { x.OutletId, x.Role }).HasDatabaseName("IX_Staff_Outlet_Role");
        });

        modelBuilder.Entity<StaffShift>(e =>
        {
            e.ToTable("StaffShifts", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.HoursWorked).HasPrecision(10, 3);
            e.Property(x => x.SalesAmount).HasPrecision(18, 4);
            e.Property(x => x.TipsEarned).HasPrecision(18, 4);
            e.HasIndex(x => new { x.OutletId, x.ShiftDate }).HasDatabaseName("IX_StaffShift_Outlet_Date");
            e.HasOne(x => x.Staff).WithMany()
                .HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<TimeClockEntry>(e =>
        {
            e.ToTable("TimeClockEntries", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.Hours).HasPrecision(10, 3);
            e.HasIndex(x => new { x.OutletId, x.StaffId, x.ClockedInAt }).HasDatabaseName("IX_TimeClock_Staff_At");
        });

        modelBuilder.Entity<RestaurantSession>(e =>
        {
            e.ToTable("Sessions", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.SessionNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.CashierName).HasMaxLength(150);
            e.Property(x => x.TerminalName).HasMaxLength(120);
            e.Property(x => x.ClosingNote).HasMaxLength(1000);

            foreach (var money in new[]
            {
                nameof(RestaurantSession.OpeningFloat), nameof(RestaurantSession.ExpectedCash),
                nameof(RestaurantSession.ExpectedCard), nameof(RestaurantSession.ExpectedOther),
                nameof(RestaurantSession.CountedCash), nameof(RestaurantSession.CountedCard),
                nameof(RestaurantSession.CountedOther), nameof(RestaurantSession.CashVariance),
                nameof(RestaurantSession.TotalSales), nameof(RestaurantSession.TotalDiscounts),
                nameof(RestaurantSession.TotalVoids), nameof(RestaurantSession.TotalRefunds),
                nameof(RestaurantSession.TotalTips), nameof(RestaurantSession.TotalTax),
                nameof(RestaurantSession.TotalServiceCharge),
            })
                e.Property(money).HasPrecision(18, 4);

            e.HasIndex(x => new { x.OutletId, x.Status }).HasDatabaseName("IX_Session_Outlet_Status");
            e.HasMany(x => x.CashMovements).WithOne(m => m.Session)
                .HasForeignKey(m => m.SessionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SessionCashMovement>(e =>
        {
            e.ToTable("CashMovements", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Reason).HasMaxLength(300);
            e.Property(x => x.Reference).HasMaxLength(120);
            e.Property(x => x.StaffName).HasMaxLength(150);
            e.Property(x => x.Amount).HasPrecision(18, 4);
        });

        // ═══ Food safety & compliance ════════════════════════════════════════

        modelBuilder.Entity<TemperatureCheckpoint>(e =>
        {
            e.ToTable("TemperatureCheckpoints", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.Location).HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.MinSafeCelsius).HasPrecision(6, 2);
            e.Property(x => x.MaxSafeCelsius).HasPrecision(6, 2);
            e.HasIndex(x => new { x.OutletId, x.DisplayOrder }).HasDatabaseName("IX_TempCheckpoint_Outlet_Order");
            e.HasMany(x => x.Logs).WithOne(l => l.Checkpoint)
                .HasForeignKey(l => l.CheckpointId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TemperatureLog>(e =>
        {
            e.ToTable("TemperatureLogs", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ReadingCelsius).HasPrecision(6, 2);
            e.Property(x => x.StaffName).HasMaxLength(150);
            e.Property(x => x.CorrectiveAction).HasMaxLength(1000);
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasIndex(x => new { x.OutletId, x.RecordedAt }).HasDatabaseName("IX_TempLog_Outlet_At");
            e.HasIndex(x => new { x.OutletId, x.IsOutOfRange, x.IsResolved })
                .HasDatabaseName("IX_TempLog_Outlet_Breach");
        });

        modelBuilder.Entity<ComplianceChecklist>(e =>
        {
            e.ToTable("Checklists", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.ActiveDays).HasMaxLength(30);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.HasMany(x => x.Items).WithOne(i => i.Checklist)
                .HasForeignKey(i => i.ChecklistId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Runs).WithOne(r => r.Checklist)
                .HasForeignKey(r => r.ChecklistId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChecklistItem>(e =>
        {
            e.ToTable("ChecklistItems", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Text).IsRequired().HasMaxLength(500);
            e.Property(x => x.Unit).HasMaxLength(20);
            e.Property(x => x.Guidance).HasMaxLength(1000);
            e.Property(x => x.MinValue).HasPrecision(12, 3);
            e.Property(x => x.MaxValue).HasPrecision(12, 3);
        });

        modelBuilder.Entity<ChecklistRun>(e =>
        {
            e.ToTable("ChecklistRuns", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.CompletedByStaffName).HasMaxLength(150);
            e.Property(x => x.CorrectiveAction).HasMaxLength(1000);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.HasIndex(x => new { x.OutletId, x.DueOn }).HasDatabaseName("IX_ChecklistRun_Outlet_Due");
            e.HasMany(x => x.Answers).WithOne(a => a.Run)
                .HasForeignKey(a => a.RunId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChecklistAnswer>(e =>
        {
            e.ToTable("ChecklistAnswers", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemText).IsRequired().HasMaxLength(500);
            e.Property(x => x.TextValue).HasMaxLength(1000);
            e.Property(x => x.CorrectiveAction).HasMaxLength(1000);
            e.Property(x => x.NumericValue).HasPrecision(12, 3);
        });

        modelBuilder.Entity<PrepBatch>(e =>
        {
            e.ToTable("PrepBatches", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.BatchCode).IsRequired().HasMaxLength(50);
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);
            e.Property(x => x.PreparedByStaffName).HasMaxLength(150);
            e.Property(x => x.SupplierBatchRefs).HasMaxLength(500);
            e.Property(x => x.StorageLocation).HasMaxLength(200);
            e.Property(x => x.DiscardReason).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.HasIndex(x => new { x.OutletId, x.UseByAt }).HasDatabaseName("IX_PrepBatch_Outlet_UseBy");
            e.HasIndex(x => new { x.CompanyId, x.BatchCode }).HasDatabaseName("IX_PrepBatch_Company_Code");
        });

        modelBuilder.Entity<DeliveryZone>(e =>
        {
            e.ToTable("DeliveryZones", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.CoveredAreas).HasMaxLength(2000);
            e.Property(x => x.ColorHex).HasMaxLength(9);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.DeliveryFee).HasPrecision(18, 4);
            e.Property(x => x.MinimumOrderValue).HasPrecision(18, 4);
            e.Property(x => x.FreeDeliveryThreshold).HasPrecision(18, 4);
            e.Property(x => x.RadiusKm).HasPrecision(10, 3);
            e.HasIndex(x => new { x.OutletId, x.DisplayOrder }).HasDatabaseName("IX_DeliveryZone_Outlet_Order");
        });

        // ═══ Costing ═════════════════════════════════════════════════════════

        modelBuilder.Entity<Recipe>(e =>
        {
            e.ToTable("Recipes", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.YieldUom).IsRequired().HasMaxLength(20);
            e.Property(x => x.Instructions).HasMaxLength(4000);
            e.Property(x => x.PlatingNotes).HasMaxLength(1000);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.YieldQuantity).HasPrecision(18, 4);
            e.Property(x => x.TotalCost).HasPrecision(18, 4);
            e.HasIndex(x => new { x.MenuItemId, x.VariantId }).HasDatabaseName("IX_Recipe_Item_Variant");
            e.HasMany(x => x.Ingredients).WithOne(i => i.Recipe)
                .HasForeignKey(i => i.RecipeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecipeIngredient>(e =>
        {
            e.ToTable("RecipeIngredients", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.IngredientName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.Quantity).HasPrecision(18, 6);
            e.Property(x => x.YieldPercent).HasPrecision(9, 4);
            e.Property(x => x.WastePercent).HasPrecision(9, 4);
            e.Property(x => x.UnitCost).HasPrecision(18, 6);
            e.Property(x => x.LineCost).HasPrecision(18, 6);
        });

        modelBuilder.Entity<WastageLog>(e =>
        {
            e.ToTable("WastageLogs", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);
            e.Property(x => x.StaffName).HasMaxLength(150);
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.Property(x => x.UnitCost).HasPrecision(18, 4);
            e.Property(x => x.TotalCost).HasPrecision(18, 4);
            e.HasIndex(x => new { x.OutletId, x.OccurredAt }).HasDatabaseName("IX_Wastage_Outlet_At");
        });

        modelBuilder.Entity<RestaurantSettings>(e =>
        {
            e.ToTable("Settings", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.TipPresetPercents).HasMaxLength(60);
            e.Property(x => x.ReceiptHeader).HasMaxLength(1000);
            e.Property(x => x.ReceiptFooter).HasMaxLength(1000);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.KitchenTipSharePercent).HasPrecision(9, 4);
            e.Property(x => x.CashRoundingIncrement).HasPrecision(18, 4);
            e.Property(x => x.PackagingChargePerOrder).HasPrecision(18, 4);
            e.Property(x => x.DiscountApprovalThreshold).HasPrecision(18, 4);

            // One row per company — settings are a singleton, and a duplicate would silently
            // give half the tills different rules from the other half.
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId })
                .IsUnique().HasDatabaseName("IX_Settings_Tenant");
        });

        modelBuilder.ApplyNexcoreConventions();
    }
}
