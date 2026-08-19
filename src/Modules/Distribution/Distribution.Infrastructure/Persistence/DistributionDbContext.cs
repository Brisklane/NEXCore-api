using Distribution.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Persistence;

namespace Distribution.Infrastructure.Persistence;

/// <summary>
/// Distribution module DbContext. Default schema: <c>distribution</c>.
///
/// The model is organised the way a distribution business is: the network it sells through, the
/// geography and routes that reach it, the field force that walks them, the vans that carry the
/// stock, the orders and warehouse work that fulfil them, the trips that deliver them, the trade
/// instruments that price them, the money that settles them, and the secondary layer that finally
/// explains whether any of it sold through.
///
/// Two conventions run through the whole model:
///
/// **Items, warehouses, bins and batches are referenced by <c>Guid</c> only.** Distribution never
/// creates a second item master or a second stock ledger — Inventory owns both, and every
/// movement here posts against them through events.
///
/// **Delete behaviour is deliberate.** Composition (an order and its lines, a claim and its lines,
/// a trip and its stops) cascades; every reference that crosses an aggregate boundary is
/// <see cref="DeleteBehavior.NoAction"/>, so retiring a route can never take a quarter of
/// settlements with it.
/// </summary>
public class DistributionDbContext : DbContext
{
    private const string DefaultSchema = "distribution";

    public DistributionDbContext(DbContextOptions<DistributionDbContext> options) : base(options) { }

    // ── Network & channel ────────────────────────────────────────────────────
    public DbSet<ChannelPartner> Partners { get; set; } = null!;
    public DbSet<PartnerContact> PartnerContacts { get; set; } = null!;
    public DbSet<PartnerDocument> PartnerDocuments { get; set; } = null!;
    public DbSet<PartnerAuthorisation> PartnerAuthorisations { get; set; } = null!;
    public DbSet<PartnerAgreement> PartnerAgreements { get; set; } = null!;
    public DbSet<RetailOutlet> Outlets { get; set; } = null!;
    public DbSet<OutletContact> OutletContacts { get; set; } = null!;
    public DbSet<OutletAsset> OutletAssets { get; set; } = null!;
    public DbSet<OutletPhoto> OutletPhotos { get; set; } = null!;
    public DbSet<OutletNote> OutletNotes { get; set; } = null!;

    // ── Geography & route-to-market ──────────────────────────────────────────
    public DbSet<GeoNode> GeoNodes { get; set; } = null!;
    public DbSet<DistributionTerritory> Territories { get; set; } = null!;
    public DbSet<SalesRoute> Routes { get; set; } = null!;
    public DbSet<RouteOutlet> RouteOutlets { get; set; } = null!;
    public DbSet<RouteAssignment> RouteAssignments { get; set; } = null!;
    public DbSet<JourneyPlan> JourneyPlans { get; set; } = null!;
    public DbSet<JourneyPlanDay> JourneyPlanDays { get; set; } = null!;

    // ── Field force ──────────────────────────────────────────────────────────
    public DbSet<FieldRep> FieldReps { get; set; } = null!;
    public DbSet<FieldDay> FieldDays { get; set; } = null!;
    public DbSet<OutletVisit> Visits { get; set; } = null!;
    public DbSet<VisitTask> VisitTasks { get; set; } = null!;
    public DbSet<SurveyForm> SurveyForms { get; set; } = null!;
    public DbSet<SurveyQuestion> SurveyQuestions { get; set; } = null!;
    public DbSet<SurveyResponse> SurveyResponses { get; set; } = null!;
    public DbSet<SurveyAnswer> SurveyAnswers { get; set; } = null!;
    public DbSet<CompetitorObservation> CompetitorObservations { get; set; } = null!;
    public DbSet<MerchandisingAudit> MerchandisingAudits { get; set; } = null!;
    public DbSet<MerchandisingAuditLine> MerchandisingAuditLines { get; set; } = null!;
    public DbSet<PosmPlacement> PosmPlacements { get; set; } = null!;
    public DbSet<FieldDevice> FieldDevices { get; set; } = null!;

    // ── Van sales ────────────────────────────────────────────────────────────
    public DbSet<VanUnit> VanUnits { get; set; } = null!;
    public DbSet<VanLoadSheet> VanLoadSheets { get; set; } = null!;
    public DbSet<VanLoadLine> VanLoadLines { get; set; } = null!;
    public DbSet<VanStockBalance> VanStockBalances { get; set; } = null!;
    public DbSet<VanStockMovement> VanStockMovements { get; set; } = null!;
    public DbSet<VanCycleCount> VanCycleCounts { get; set; } = null!;
    public DbSet<VanCycleCountLine> VanCycleCountLines { get; set; } = null!;

    // ── Orders & fulfilment ──────────────────────────────────────────────────
    public DbSet<DistributionOrder> Orders { get; set; } = null!;
    public DbSet<DistributionOrderLine> OrderLines { get; set; } = null!;
    public DbSet<OrderStatusEvent> OrderStatusEvents { get; set; } = null!;
    public DbSet<OrderApproval> OrderApprovals { get; set; } = null!;
    public DbSet<StockAllocation> StockAllocations { get; set; } = null!;
    public DbSet<PickWave> PickWaves { get; set; } = null!;
    public DbSet<PickTask> PickTasks { get; set; } = null!;
    public DbSet<PickTaskLine> PickTaskLines { get; set; } = null!;
    public DbSet<PackageUnit> Packages { get; set; } = null!;
    public DbSet<PackageContent> PackageContents { get; set; } = null!;
    public DbSet<Dispatch> Dispatches { get; set; } = null!;
    public DbSet<DispatchLine> DispatchLines { get; set; } = null!;

    // ── Logistics ────────────────────────────────────────────────────────────
    public DbSet<Vehicle> Vehicles { get; set; } = null!;
    public DbSet<VehicleCompliance> VehicleCompliances { get; set; } = null!;
    public DbSet<Driver> Drivers { get; set; } = null!;
    public DbSet<DeliveryTrip> Trips { get; set; } = null!;
    public DbSet<TripStop> TripStops { get; set; } = null!;
    public DbSet<TripExpense> TripExpenses { get; set; } = null!;
    public DbSet<ProofOfDelivery> ProofsOfDelivery { get; set; } = null!;
    public DbSet<PodLine> PodLines { get; set; } = null!;

    // ── Returns ──────────────────────────────────────────────────────────────
    public DbSet<ReturnAuthorisation> Returns { get; set; } = null!;
    public DbSet<ReturnAuthorisationLine> ReturnLines { get; set; } = null!;
    public DbSet<ReturnReceipt> ReturnReceipts { get; set; } = null!;
    public DbSet<ReturnReceiptLine> ReturnReceiptLines { get; set; } = null!;
    public DbSet<ReturnDisposition> ReturnDispositions { get; set; } = null!;

    // ── Pricing & trade ──────────────────────────────────────────────────────
    public DbSet<ChannelPriceList> PriceLists { get; set; } = null!;
    public DbSet<ChannelPriceListLine> PriceListLines { get; set; } = null!;
    public DbSet<PriceSlab> PriceSlabs { get; set; } = null!;
    public DbSet<MarginLadder> MarginLadders { get; set; } = null!;
    public DbSet<MrpRevision> MrpRevisions { get; set; } = null!;
    public DbSet<TradeScheme> Schemes { get; set; } = null!;
    public DbSet<TradeSchemeSlab> SchemeSlabs { get; set; } = null!;
    public DbSet<TradeSchemeProduct> SchemeProducts { get; set; } = null!;
    public DbSet<TradeSchemeScope> SchemeScopes { get; set; } = null!;
    public DbSet<SchemeApplication> SchemeApplications { get; set; } = null!;
    public DbSet<SchemeBudgetLedger> SchemeBudgetLedger { get; set; } = null!;

    // ── Claims & money ───────────────────────────────────────────────────────
    public DbSet<ChannelClaim> Claims { get; set; } = null!;
    public DbSet<ChannelClaimLine> ClaimLines { get; set; } = null!;
    public DbSet<ClaimDocument> ClaimDocuments { get; set; } = null!;
    public DbSet<ClaimStatusEvent> ClaimStatusEvents { get; set; } = null!;
    public DbSet<SupplierRebateAgreement> RebateAgreements { get; set; } = null!;
    public DbSet<RebateAccrual> RebateAccruals { get; set; } = null!;
    public DbSet<Chargeback> Chargebacks { get; set; } = null!;
    public DbSet<CreditProfile> CreditProfiles { get; set; } = null!;
    public DbSet<CreditOverride> CreditOverrides { get; set; } = null!;
    public DbSet<CollectionReceipt> Collections { get; set; } = null!;
    public DbSet<ChequeRecord> Cheques { get; set; } = null!;
    public DbSet<RouteSettlement> Settlements { get; set; } = null!;
    public DbSet<SettlementVariance> SettlementVariances { get; set; } = null!;
    public DbSet<CashDeposit> CashDeposits { get; set; } = null!;

    // ── Secondary sales & DMS ────────────────────────────────────────────────
    public DbSet<PrimarySaleFact> PrimarySaleFacts { get; set; } = null!;
    public DbSet<SecondarySale> SecondarySales { get; set; } = null!;
    public DbSet<SecondarySaleLine> SecondarySaleLines { get; set; } = null!;
    public DbSet<SecondaryUploadBatch> SecondaryUploads { get; set; } = null!;
    public DbSet<SecondaryMappingProfile> MappingProfiles { get; set; } = null!;
    public DbSet<DistributorStockDeclaration> StockDeclarations { get; set; } = null!;
    public DbSet<DistributorStockLine> StockDeclarationLines { get; set; } = null!;
    public DbSet<StockNorm> StockNorms { get; set; } = null!;
    public DbSet<SellInSellOutReconciliation> Reconciliations { get; set; } = null!;

    // ── Targets & performance ────────────────────────────────────────────────
    public DbSet<SalesTarget> Targets { get; set; } = null!;
    public DbSet<TargetLine> TargetLines { get; set; } = null!;
    public DbSet<IncentiveScheme> IncentiveSchemes { get; set; } = null!;
    public DbSet<IncentiveSlab> IncentiveSlabs { get; set; } = null!;
    public DbSet<IncentivePayout> IncentivePayouts { get; set; } = null!;
    public DbSet<KpiSnapshot> KpiSnapshots { get; set; } = null!;

    // ── Planning ─────────────────────────────────────────────────────────────
    public DbSet<DemandForecast> Forecasts { get; set; } = null!;
    public DbSet<ForecastLine> ForecastLines { get; set; } = null!;
    public DbSet<ReplenishmentSuggestion> ReplenishmentSuggestions { get; set; } = null!;
    public DbSet<StockTransferRequest> TransferRequests { get; set; } = null!;

    // ── Traceability & platform ──────────────────────────────────────────────
    public DbSet<ColdChainCheckpoint> ColdChainCheckpoints { get; set; } = null!;
    public DbSet<ColdChainLog> ColdChainLogs { get; set; } = null!;
    public DbSet<BatchTraceLink> BatchTraceLinks { get; set; } = null!;
    public DbSet<ProductRecall> Recalls { get; set; } = null!;
    public DbSet<RecallOutletNotice> RecallNotices { get; set; } = null!;
    public DbSet<ReasonCode> ReasonCodes { get; set; } = null!;
    public DbSet<DistributionSettings> Settings { get; set; } = null!;
    public DbSet<DistributionNotification> Notifications { get; set; } = null!;

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

        ConfigureNetwork(modelBuilder);
        ConfigureRoutes(modelBuilder);
        ConfigureField(modelBuilder);
        ConfigureVan(modelBuilder);
        ConfigureOrders(modelBuilder);
        ConfigureLogistics(modelBuilder);
        ConfigureReturns(modelBuilder);
        ConfigureTrade(modelBuilder);
        ConfigureMoney(modelBuilder);
        ConfigureSecondary(modelBuilder);
        ConfigurePerformance(modelBuilder);
        ConfigureCompliance(modelBuilder);

        modelBuilder.ApplyNexcoreConventions();
    }

    // ═══ Network ═════════════════════════════════════════════════════════════

    private static void ConfigureNetwork(ModelBuilder b)
    {
        b.Entity<ChannelPartner>(e =>
        {
            e.ToTable("Partners", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(250);
            e.Property(x => x.TradeName).HasMaxLength(250);
            e.Property(x => x.ContactPerson).HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.AlternatePhone).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.AddressLine).HasMaxLength(500);
            e.Property(x => x.City).HasMaxLength(120);
            e.Property(x => x.StateName).HasMaxLength(120);
            e.Property(x => x.PostalCode).HasMaxLength(20);
            e.Property(x => x.CountryCode).HasMaxLength(3);
            e.Property(x => x.TaxRegistrationNumber).HasMaxLength(60);
            e.Property(x => x.SecondaryTaxNumber).HasMaxLength(60);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.TerminationReason).HasMaxLength(500);
            e.Property(x => x.StatusReason).HasMaxLength(500);
            e.Property(x => x.Notes).HasMaxLength(2000);

            e.Property(x => x.CreditLimit).HasPrecision(18, 4);
            e.Property(x => x.SecurityDeposit).HasPrecision(18, 4);
            e.Property(x => x.MarginPercent).HasPrecision(9, 4);
            e.Property(x => x.MinimumMonthlyOfftake).HasPrecision(18, 4);

            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.Code })
                .IsUnique().HasDatabaseName("IX_Partner_Tenant_Code");
            e.HasIndex(x => new { x.CompanyId, x.PartnerType, x.Status }).HasDatabaseName("IX_Partner_Type_Status");
            e.HasIndex(x => x.TerritoryId).HasDatabaseName("IX_Partner_Territory");

            // Retiring a parent must never delete the sub-distributors underneath it.
            e.HasMany(x => x.Children).WithOne(c => c.ParentPartner)
                .HasForeignKey(c => c.ParentPartnerId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Territory).WithMany()
                .HasForeignKey(x => x.TerritoryId).OnDelete(DeleteBehavior.NoAction);

            e.HasMany(x => x.Contacts).WithOne(c => c.Partner)
                .HasForeignKey(c => c.PartnerId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Documents).WithOne(d => d.Partner)
                .HasForeignKey(d => d.PartnerId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Authorisations).WithOne(a => a.Partner)
                .HasForeignKey(a => a.PartnerId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PartnerContact>(e =>
        {
            e.ToTable("PartnerContacts", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Designation).HasMaxLength(120);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(200);
        });

        b.Entity<PartnerDocument>(e =>
        {
            e.ToTable("PartnerDocuments", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.DocumentType).IsRequired().HasMaxLength(120);
            e.Property(x => x.DocumentNumber).HasMaxLength(120);
            e.Property(x => x.FileUrl).HasMaxLength(500);
            e.Property(x => x.VerificationNote).HasMaxLength(500);
            // Drives the "expiring in 30 days" dashboard, which is the point of the record.
            e.HasIndex(x => new { x.CompanyId, x.ExpiresOn }).HasDatabaseName("IX_PartnerDoc_Expiry");
        });

        b.Entity<PartnerAuthorisation>(e =>
        {
            e.ToTable("PartnerAuthorisations", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ScopeName).HasMaxLength(200);
            e.HasIndex(x => new { x.PartnerId, x.BrandId, x.CategoryId })
                .HasDatabaseName("IX_PartnerAuth_Partner_Scope");
        });

        b.Entity<PartnerAgreement>(e =>
        {
            e.ToTable("PartnerAgreements", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.AgreementNumber).IsRequired().HasMaxLength(60);
            e.Property(x => x.FileUrl).HasMaxLength(500);
            e.Property(x => x.Terms).HasMaxLength(4000);
            e.Property(x => x.SecurityDeposit).HasPrecision(18, 4);
            e.Property(x => x.TargetTurnover).HasPrecision(18, 4);
            e.HasOne(x => x.Partner).WithMany()
                .HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<RetailOutlet>(e =>
        {
            e.ToTable("Outlets", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(250);
            e.Property(x => x.OwnerName).HasMaxLength(200);
            e.Property(x => x.OwnerPhone).HasMaxLength(50);
            e.Property(x => x.DecisionMakerName).HasMaxLength(200);
            e.Property(x => x.AlternatePhone).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.SubChannel).HasMaxLength(120);
            e.Property(x => x.StatusReason).HasMaxLength(500);
            e.Property(x => x.ChainName).HasMaxLength(200);
            e.Property(x => x.StoreFormat).HasMaxLength(120);
            e.Property(x => x.AddressLine).HasMaxLength(500);
            e.Property(x => x.Landmark).HasMaxLength(200);
            e.Property(x => x.Area).HasMaxLength(150);
            e.Property(x => x.City).HasMaxLength(120);
            e.Property(x => x.StateName).HasMaxLength(120);
            e.Property(x => x.PostalCode).HasMaxLength(20);
            e.Property(x => x.CountryCode).HasMaxLength(3);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.TaxRegistrationNumber).HasMaxLength(60);
            e.Property(x => x.LicenceNumber).HasMaxLength(60);
            e.Property(x => x.PhotoUrl).HasMaxLength(500);
            e.Property(x => x.Notes).HasMaxLength(2000);

            foreach (var money in new[]
            {
                nameof(RetailOutlet.CreditLimit), nameof(RetailOutlet.LifetimeSales),
                nameof(RetailOutlet.AverageMonthlyOfftake), nameof(RetailOutlet.OutstandingAmount),
            })
                e.Property(money).HasPrecision(18, 4);

            e.Property(x => x.PerfectStoreScore).HasPrecision(9, 4);

            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.Code })
                .IsUnique().HasDatabaseName("IX_Outlet_Tenant_Code");
            e.HasIndex(x => new { x.CompanyId, x.Channel, x.Status }).HasDatabaseName("IX_Outlet_Channel_Status");
            e.HasIndex(x => x.PartnerId).HasDatabaseName("IX_Outlet_Partner");
            e.HasIndex(x => x.TerritoryId).HasDatabaseName("IX_Outlet_Territory");
            // Duplicate detection on onboarding runs off phone and tax id.
            e.HasIndex(x => new { x.CompanyId, x.OwnerPhone }).HasDatabaseName("IX_Outlet_Phone");
            e.HasIndex(x => new { x.CompanyId, x.TaxRegistrationNumber }).HasDatabaseName("IX_Outlet_TaxNumber");

            e.HasOne(x => x.Partner).WithMany()
                .HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.GeoNode).WithMany()
                .HasForeignKey(x => x.GeoNodeId).OnDelete(DeleteBehavior.NoAction);

            e.HasMany(x => x.Assets).WithOne(a => a.Outlet)
                .HasForeignKey(a => a.OutletId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Contacts).WithOne(c => c.Outlet)
                .HasForeignKey(c => c.OutletId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.RouteLinks).WithOne(r => r.Outlet)
                .HasForeignKey(r => r.OutletId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<OutletContact>(e =>
        {
            e.ToTable("OutletContacts", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Designation).HasMaxLength(120);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(200);
        });

        b.Entity<OutletAsset>(e =>
        {
            e.ToTable("OutletAssets", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.AssetTag).IsRequired().HasMaxLength(60);
            e.Property(x => x.SerialNumber).HasMaxLength(80);
            e.Property(x => x.Model).HasMaxLength(120);
            e.Property(x => x.Manufacturer).HasMaxLength(120);
            e.Property(x => x.PhotoUrl).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.AssetValue).HasPrecision(18, 4);
            e.Property(x => x.DepositTaken).HasPrecision(18, 4);
            e.HasIndex(x => new { x.CompanyId, x.AssetTag }).IsUnique().HasDatabaseName("IX_OutletAsset_Tag");
            e.HasIndex(x => new { x.OutletId, x.Kind }).HasDatabaseName("IX_OutletAsset_Outlet_Kind");
        });

        b.Entity<OutletPhoto>(e =>
        {
            e.ToTable("OutletPhotos", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ImageUrl).IsRequired().HasMaxLength(500);
            e.Property(x => x.Caption).HasMaxLength(300);
            e.Property(x => x.Tag).HasMaxLength(60);
            e.HasIndex(x => new { x.OutletId, x.CapturedAt }).HasDatabaseName("IX_OutletPhoto_Outlet_At");
        });

        b.Entity<OutletNote>(e =>
        {
            e.ToTable("OutletNotes", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Text).IsRequired().HasMaxLength(2000);
            e.Property(x => x.AuthorName).HasMaxLength(200);
            e.HasIndex(x => new { x.OutletId, x.NotedAt }).HasDatabaseName("IX_OutletNote_Outlet_At");
        });
    }

    // ═══ Routes ══════════════════════════════════════════════════════════════

    private static void ConfigureRoutes(ModelBuilder b)
    {
        b.Entity<GeoNode>(e =>
        {
            e.ToTable("GeoNodes", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.LevelName).IsRequired().HasMaxLength(60);
            e.Property(x => x.Path).HasMaxLength(1000);
            // Subtree queries are a prefix scan on the materialised path.
            e.HasIndex(x => new { x.CompanyId, x.Path }).HasDatabaseName("IX_GeoNode_Path");
            e.HasMany(x => x.Children).WithOne(c => c.Parent)
                .HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<DistributionTerritory>(e =>
        {
            e.ToTable("Territories", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.HasIndex(x => new { x.CompanyId, x.Code }).HasDatabaseName("IX_Territory_Code");
            e.HasMany(x => x.Children).WithOne(c => c.Parent)
                .HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.GeoNode).WithMany()
                .HasForeignKey(x => x.GeoNodeId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Routes).WithOne(r => r.Territory)
                .HasForeignKey(r => r.TerritoryId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<SalesRoute>(e =>
        {
            e.ToTable("Routes", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.ActiveDays).HasMaxLength(30);
            e.Property(x => x.ActiveWeeks).HasMaxLength(20);
            e.Property(x => x.PlannedDistanceKm).HasPrecision(10, 3);
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique().HasDatabaseName("IX_Route_Code");
            e.HasIndex(x => new { x.TerritoryId, x.Kind }).HasDatabaseName("IX_Route_Territory_Kind");
            e.HasIndex(x => x.FieldRepId).HasDatabaseName("IX_Route_FieldRep");

            e.HasOne(x => x.Partner).WithMany()
                .HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.FieldRep).WithMany()
                .HasForeignKey(x => x.FieldRepId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Outlets).WithOne(o => o.Route)
                .HasForeignKey(o => o.RouteId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Assignments).WithOne(a => a.Route)
                .HasForeignKey(a => a.RouteId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<RouteOutlet>(e =>
        {
            e.ToTable("RouteOutlets", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.DistanceFromPreviousKm).HasPrecision(10, 3);
            e.HasIndex(x => new { x.RouteId, x.StopSequence }).HasDatabaseName("IX_RouteOutlet_Route_Seq");
            e.HasIndex(x => new { x.RouteId, x.OutletId }).IsUnique().HasDatabaseName("IX_RouteOutlet_Pair");
        });

        b.Entity<RouteAssignment>(e =>
        {
            e.ToTable("RouteAssignments", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Reason).HasMaxLength(500);
            e.HasIndex(x => new { x.RouteId, x.EffectiveFrom }).HasDatabaseName("IX_RouteAssignment_Route_From");
            e.HasOne(x => x.FieldRep).WithMany()
                .HasForeignKey(x => x.FieldRepId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<JourneyPlan>(e =>
        {
            e.ToTable("JourneyPlans", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200);
            e.HasIndex(x => new { x.FieldRepId, x.PeriodStart })
                .IsUnique().HasDatabaseName("IX_JourneyPlan_Rep_Period");
            e.HasOne(x => x.FieldRep).WithMany()
                .HasForeignKey(x => x.FieldRepId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Days).WithOne(d => d.JourneyPlan)
                .HasForeignKey(d => d.JourneyPlanId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<JourneyPlanDay>(e =>
        {
            e.ToTable("JourneyPlanDays", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.SkipReason).HasMaxLength(500);
            e.HasIndex(x => new { x.JourneyPlanId, x.PlanDate }).HasDatabaseName("IX_JourneyPlanDay_Plan_Date");
            e.HasOne(x => x.Route).WithMany()
                .HasForeignKey(x => x.RouteId).OnDelete(DeleteBehavior.NoAction);
        });
    }

    // ═══ Field ═══════════════════════════════════════════════════════════════

    private static void ConfigureField(ModelBuilder b)
    {
        b.Entity<FieldRep>(e =>
        {
            e.ToTable("FieldReps", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            e.Property(x => x.DisplayName).HasMaxLength(80);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.PhotoUrl).HasMaxLength(500);
            e.Property(x => x.PinHash).HasMaxLength(200);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.CashHoldingLimit).HasPrecision(18, 4);
            e.Property(x => x.DiscountAuthorityPercent).HasPrecision(9, 4);
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique().HasDatabaseName("IX_FieldRep_Code");
            e.HasIndex(x => new { x.CompanyId, x.Role }).HasDatabaseName("IX_FieldRep_Role");
            e.HasOne(x => x.Partner).WithMany()
                .HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<FieldDay>(e =>
        {
            e.ToTable("FieldDays", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.StartSelfieUrl).HasMaxLength(500);
            e.Property(x => x.ForceCloseReason).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.DistanceCoveredKm).HasPrecision(10, 3);

            foreach (var money in new[]
            {
                nameof(FieldDay.OrderValue), nameof(FieldDay.InvoicedValue),
                nameof(FieldDay.CollectedAmount), nameof(FieldDay.ReturnValue),
                nameof(FieldDay.CashDeclared),
            })
                e.Property(money).HasPrecision(18, 4);

            // One day per rep per date: two open days is how a settlement goes missing.
            e.HasIndex(x => new { x.FieldRepId, x.WorkDate })
                .IsUnique().HasDatabaseName("IX_FieldDay_Rep_Date");
            e.HasIndex(x => new { x.CompanyId, x.WorkDate, x.Status }).HasDatabaseName("IX_FieldDay_Date_Status");

            e.HasOne(x => x.FieldRep).WithMany()
                .HasForeignKey(x => x.FieldRepId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Route).WithMany()
                .HasForeignKey(x => x.RouteId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Visits).WithOne(v => v.FieldDay)
                .HasForeignKey(v => v.FieldDayId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<OutletVisit>(e =>
        {
            e.ToTable("Visits", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.GeoExceptionReason).HasMaxLength(500);
            e.Property(x => x.NoOrderNote).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.IdempotencyKey).HasMaxLength(80);
            e.Property(x => x.DistanceFromOutletMetres).HasPrecision(12, 2);
            e.Property(x => x.OrderValue).HasPrecision(18, 4);
            e.Property(x => x.CollectedAmount).HasPrecision(18, 4);
            e.Property(x => x.ReturnValue).HasPrecision(18, 4);

            e.HasIndex(x => new { x.FieldDayId, x.StopSequence }).HasDatabaseName("IX_Visit_Day_Seq");
            e.HasIndex(x => new { x.OutletId, x.CheckedInAt }).HasDatabaseName("IX_Visit_Outlet_At");
            e.HasIndex(x => new { x.CompanyId, x.FieldRepId, x.Status }).HasDatabaseName("IX_Visit_Rep_Status");

            // Idempotency enforced in the database: two retries of one check-in must not both win.
            e.HasIndex(x => new { x.CompanyId, x.IdempotencyKey })
                .IsUnique().HasDatabaseName("IX_Visit_Idempotency")
                .HasFilter("idempotency_key IS NOT NULL");

            e.HasOne(x => x.Outlet).WithMany()
                .HasForeignKey(x => x.OutletId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<VisitTask>(e =>
        {
            e.ToTable("VisitTasks", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired().HasMaxLength(250);
            e.Property(x => x.Instructions).HasMaxLength(2000);
            e.Property(x => x.PhotoUrl).HasMaxLength(500);
            e.Property(x => x.CompletionNote).HasMaxLength(1000);
            e.HasIndex(x => new { x.CompanyId, x.DueOn }).HasDatabaseName("IX_VisitTask_Due");
            e.HasIndex(x => new { x.OutletId, x.CompletedAt }).HasDatabaseName("IX_VisitTask_Outlet_Done");
        });

        b.Entity<SurveyForm>(e =>
        {
            e.ToTable("SurveyForms", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Instructions).HasMaxLength(2000);
            e.Property(x => x.ApplicableChannels).HasMaxLength(60);
            e.HasMany(x => x.Questions).WithOne(q => q.SurveyForm)
                .HasForeignKey(q => q.SurveyFormId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<SurveyQuestion>(e =>
        {
            e.ToTable("SurveyQuestions", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Text).IsRequired().HasMaxLength(500);
            e.Property(x => x.Options).HasMaxLength(2000);
            e.Property(x => x.Unit).HasMaxLength(20);
            e.Property(x => x.Guidance).HasMaxLength(1000);
            e.Property(x => x.ShowWhenValue).HasMaxLength(200);
            e.Property(x => x.MinValue).HasPrecision(18, 4);
            e.Property(x => x.MaxValue).HasPrecision(18, 4);
            e.HasIndex(x => new { x.SurveyFormId, x.DisplayOrder }).HasDatabaseName("IX_SurveyQuestion_Form_Order");
        });

        b.Entity<SurveyResponse>(e =>
        {
            e.ToTable("SurveyResponses", DefaultSchema);
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.OutletId, x.SubmittedAt }).HasDatabaseName("IX_SurveyResponse_Outlet_At");
            e.HasOne(x => x.SurveyForm).WithMany()
                .HasForeignKey(x => x.SurveyFormId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Answers).WithOne(a => a.Response)
                .HasForeignKey(a => a.ResponseId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<SurveyAnswer>(e =>
        {
            e.ToTable("SurveyAnswers", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.QuestionText).IsRequired().HasMaxLength(500);
            e.Property(x => x.TextValue).HasMaxLength(2000);
            e.Property(x => x.PhotoUrl).HasMaxLength(500);
            e.Property(x => x.NumericValue).HasPrecision(18, 4);
        });

        b.Entity<CompetitorObservation>(e =>
        {
            e.ToTable("CompetitorObservations", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.CompetitorName).IsRequired().HasMaxLength(200);
            e.Property(x => x.ProductName).HasMaxLength(200);
            e.Property(x => x.PackSize).HasMaxLength(60);
            e.Property(x => x.SchemeDescription).HasMaxLength(1000);
            e.Property(x => x.PhotoUrl).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.ObservedPrice).HasPrecision(18, 4);
            e.Property(x => x.ObservedMrp).HasPrecision(18, 4);
            e.HasIndex(x => new { x.CompanyId, x.ObservedAt }).HasDatabaseName("IX_Competitor_At");
        });

        b.Entity<MerchandisingAudit>(e =>
        {
            e.ToTable("MerchandisingAudits", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.BeforePhotoUrl).HasMaxLength(500);
            e.Property(x => x.AfterPhotoUrl).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(1000);

            foreach (var score in new[]
            {
                nameof(MerchandisingAudit.Score), nameof(MerchandisingAudit.MaxScore),
                nameof(MerchandisingAudit.AvailabilityScore), nameof(MerchandisingAudit.VisibilityScore),
                nameof(MerchandisingAudit.PlanogramScore), nameof(MerchandisingAudit.PricingScore),
                nameof(MerchandisingAudit.PosmScore), nameof(MerchandisingAudit.ShareOfShelfPercent),
                nameof(MerchandisingAudit.OnShelfAvailabilityPercent),
            })
                e.Property(score).HasPrecision(9, 4);

            e.HasIndex(x => new { x.OutletId, x.AuditedAt }).HasDatabaseName("IX_Audit_Outlet_At");
            e.HasOne(x => x.Outlet).WithMany()
                .HasForeignKey(x => x.OutletId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Lines).WithOne(l => l.Audit)
                .HasForeignKey(l => l.AuditId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<MerchandisingAuditLine>(e =>
        {
            e.ToTable("MerchandisingAuditLines", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
            e.Property(x => x.ItemCode).HasMaxLength(60);
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.ObservedPrice).HasPrecision(18, 4);
            e.Property(x => x.MandatedPrice).HasPrecision(18, 4);
        });

        b.Entity<PosmPlacement>(e =>
        {
            e.ToTable("PosmPlacements", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.MaterialName).IsRequired().HasMaxLength(200);
            e.Property(x => x.PhotoUrl).HasMaxLength(500);
            e.Property(x => x.Position).HasMaxLength(200);
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasIndex(x => new { x.OutletId, x.PlacedOn }).HasDatabaseName("IX_Posm_Outlet_On");
            e.HasIndex(x => new { x.CompanyId, x.ExpiresOn }).HasDatabaseName("IX_Posm_Expiry");
        });

        b.Entity<FieldDevice>(e =>
        {
            e.ToTable("FieldDevices", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.DeviceIdentifier).IsRequired().HasMaxLength(200);
            e.Property(x => x.DeviceName).HasMaxLength(200);
            e.Property(x => x.Platform).HasMaxLength(40);
            e.Property(x => x.OsVersion).HasMaxLength(40);
            e.Property(x => x.AppVersion).HasMaxLength(40);
            e.Property(x => x.BlockReason).HasMaxLength(500);
            e.HasIndex(x => new { x.CompanyId, x.DeviceIdentifier })
                .IsUnique().HasDatabaseName("IX_FieldDevice_Identifier");
        });
    }

    // ═══ Van ═════════════════════════════════════════════════════════════════

    private static void ConfigureVan(ModelBuilder b)
    {
        b.Entity<VanUnit>(e =>
        {
            e.ToTable("VanUnits", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.CapacityWeightKg).HasPrecision(12, 3);
            e.Property(x => x.CapacityVolumeM3).HasPrecision(12, 3);
            e.Property(x => x.MinSafeCelsius).HasPrecision(6, 2);
            e.Property(x => x.MaxSafeCelsius).HasPrecision(6, 2);
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique().HasDatabaseName("IX_VanUnit_Code");
            e.HasOne(x => x.Vehicle).WithMany()
                .HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.FieldRep).WithMany()
                .HasForeignKey(x => x.FieldRepId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Balances).WithOne(s => s.VanUnit)
                .HasForeignKey(s => s.VanUnitId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<VanLoadSheet>(e =>
        {
            e.ToTable("VanLoadSheets", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.LoadNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.RejectionReason).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.TotalCostValue).HasPrecision(18, 4);
            e.Property(x => x.TotalSaleValue).HasPrecision(18, 4);
            e.Property(x => x.TotalWeightKg).HasPrecision(12, 3);
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.LoadNumber })
                .IsUnique().HasDatabaseName("IX_VanLoad_Tenant_Number");
            e.HasIndex(x => new { x.VanUnitId, x.LoadDate }).HasDatabaseName("IX_VanLoad_Van_Date");
            e.HasOne(x => x.VanUnit).WithMany()
                .HasForeignKey(x => x.VanUnitId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Lines).WithOne(l => l.LoadSheet)
                .HasForeignKey(l => l.LoadSheetId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<VanLoadLine>(e =>
        {
            e.ToTable("VanLoadLines", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
            e.Property(x => x.ItemCode).HasMaxLength(60);
            e.Property(x => x.BatchNumber).HasMaxLength(60);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);
            e.Property(x => x.VarianceReason).HasMaxLength(500);

            foreach (var q in new[]
            {
                nameof(VanLoadLine.UomFactor), nameof(VanLoadLine.SuggestedQuantity),
                nameof(VanLoadLine.RequestedQuantity), nameof(VanLoadLine.ApprovedQuantity),
                nameof(VanLoadLine.PickedQuantity), nameof(VanLoadLine.LoadedQuantity),
            })
                e.Property(q).HasPrecision(18, 4);

            e.Property(x => x.UnitCost).HasPrecision(18, 4);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.LineValue).HasPrecision(18, 4);
            e.HasIndex(x => new { x.LoadSheetId, x.DisplayOrder }).HasDatabaseName("IX_VanLoadLine_Sheet_Order");
        });

        b.Entity<VanStockBalance>(e =>
        {
            e.ToTable("VanStockBalances", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
            e.Property(x => x.ItemCode).HasMaxLength(60);
            e.Property(x => x.BatchNumber).HasMaxLength(60);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.Property(x => x.ReservedQuantity).HasPrecision(18, 4);
            e.Property(x => x.UnitCost).HasPrecision(18, 4);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);

            // One balance per van, item, batch and compartment — a duplicate would double the van's stock.
            e.HasIndex(x => new { x.VanUnitId, x.ItemId, x.BatchId, x.Compartment })
                .IsUnique().HasDatabaseName("IX_VanStock_Van_Item_Batch_Compartment");
        });

        b.Entity<VanStockMovement>(e =>
        {
            e.ToTable("VanStockMovements", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
            e.Property(x => x.BatchNumber).HasMaxLength(60);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);
            e.Property(x => x.ReferenceType).HasMaxLength(60);
            e.Property(x => x.ReferenceNumber).HasMaxLength(60);
            e.Property(x => x.Reason).HasMaxLength(500);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.Property(x => x.BalanceAfter).HasPrecision(18, 4);
            e.Property(x => x.UnitCost).HasPrecision(18, 4);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.Value).HasPrecision(18, 4);
            e.HasIndex(x => new { x.VanUnitId, x.OccurredAt }).HasDatabaseName("IX_VanMovement_Van_At");
            e.HasIndex(x => new { x.FieldDayId, x.Kind }).HasDatabaseName("IX_VanMovement_Day_Kind");
        });

        b.Entity<VanCycleCount>(e =>
        {
            e.ToTable("VanCycleCounts", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.CountNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.VarianceValue).HasPrecision(18, 4);
            e.HasIndex(x => new { x.VanUnitId, x.CountedAt }).HasDatabaseName("IX_VanCount_Van_At");
            e.HasMany(x => x.Lines).WithOne(l => l.Count)
                .HasForeignKey(l => l.CountId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<VanCycleCountLine>(e =>
        {
            e.ToTable("VanCycleCountLines", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
            e.Property(x => x.BatchNumber).HasMaxLength(60);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);
            e.Property(x => x.ReasonNote).HasMaxLength(500);
            e.Property(x => x.ExpectedQuantity).HasPrecision(18, 4);
            e.Property(x => x.CountedQuantity).HasPrecision(18, 4);
            e.Property(x => x.VarianceQuantity).HasPrecision(18, 4);
            e.Property(x => x.UnitCost).HasPrecision(18, 4);
            e.Property(x => x.VarianceValue).HasPrecision(18, 4);
        });
    }

    // ═══ Orders ══════════════════════════════════════════════════════════════

    private static void ConfigureOrders(ModelBuilder b)
    {
        b.Entity<DistributionOrder>(e =>
        {
            e.ToTable("Orders", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.OrderNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.CustomerName).HasMaxLength(250);
            e.Property(x => x.CustomerPhone).HasMaxLength(50);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.HoldReason).HasMaxLength(500);
            e.Property(x => x.RejectionReason).HasMaxLength(500);
            e.Property(x => x.CancelNote).HasMaxLength(500);
            e.Property(x => x.ExternalReference).HasMaxLength(120);
            e.Property(x => x.IdempotencyKey).HasMaxLength(80);
            e.Property(x => x.Note).HasMaxLength(2000);
            e.Property(x => x.ExchangeRate).HasPrecision(18, 8);

            foreach (var money in new[]
            {
                nameof(DistributionOrder.SubTotal), nameof(DistributionOrder.DiscountAmount),
                nameof(DistributionOrder.SchemeDiscountAmount), nameof(DistributionOrder.FreeGoodsValue),
                nameof(DistributionOrder.TaxAmount), nameof(DistributionOrder.FreightAmount),
                nameof(DistributionOrder.RoundingAmount), nameof(DistributionOrder.TotalAmount),
                nameof(DistributionOrder.PaidAmount), nameof(DistributionOrder.CostAmount),
                nameof(DistributionOrder.MarginAmount), nameof(DistributionOrder.TotalQuantity),
            })
                e.Property(money).HasPrecision(18, 4);

            e.Property(x => x.FillRatePercent).HasPrecision(9, 4);

            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.OrderNumber })
                .IsUnique().HasDatabaseName("IX_Order_Tenant_Number");
            e.HasIndex(x => new { x.CompanyId, x.Status, x.OrderDate }).HasDatabaseName("IX_Order_Status_Date");
            e.HasIndex(x => new { x.OutletId, x.OrderDate }).HasDatabaseName("IX_Order_Outlet_Date");
            e.HasIndex(x => new { x.PartnerId, x.OrderDate }).HasDatabaseName("IX_Order_Partner_Date");
            e.HasIndex(x => x.RouteId).HasDatabaseName("IX_Order_Route");
            e.HasIndex(x => x.TripId).HasDatabaseName("IX_Order_Trip");

            // A double-tap in the market must never produce two orders.
            e.HasIndex(x => new { x.CompanyId, x.IdempotencyKey })
                .IsUnique().HasDatabaseName("IX_Order_Idempotency")
                .HasFilter("idempotency_key IS NOT NULL");

            e.HasOne(x => x.Outlet).WithMany()
                .HasForeignKey(x => x.OutletId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Partner).WithMany()
                .HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Lines).WithOne(l => l.Order)
                .HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.StatusEvents).WithOne(s => s.Order)
                .HasForeignKey(s => s.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Approvals).WithOne(a => a.Order)
                .HasForeignKey(a => a.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<DistributionOrderLine>(e =>
        {
            e.ToTable("OrderLines", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
            e.Property(x => x.ItemCode).HasMaxLength(60);
            e.Property(x => x.BatchNumber).HasMaxLength(60);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);
            e.Property(x => x.SchemeName).HasMaxLength(200);
            e.Property(x => x.PriceOverrideReason).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(500);

            foreach (var q in new[]
            {
                nameof(DistributionOrderLine.UomFactor), nameof(DistributionOrderLine.Quantity),
                nameof(DistributionOrderLine.BaseQuantity), nameof(DistributionOrderLine.AllocatedQuantity),
                nameof(DistributionOrderLine.PickedQuantity), nameof(DistributionOrderLine.DispatchedQuantity),
                nameof(DistributionOrderLine.DeliveredQuantity), nameof(DistributionOrderLine.ReturnedQuantity),
                nameof(DistributionOrderLine.BackorderedQuantity), nameof(DistributionOrderLine.UnitPrice),
                nameof(DistributionOrderLine.Mrp), nameof(DistributionOrderLine.DiscountAmount),
                nameof(DistributionOrderLine.SchemeDiscountAmount), nameof(DistributionOrderLine.TaxAmount),
                nameof(DistributionOrderLine.LineTotal), nameof(DistributionOrderLine.UnitCost),
                nameof(DistributionOrderLine.MarginAmount),
            })
                e.Property(q).HasPrecision(18, 4);

            e.Property(x => x.DiscountPercent).HasPrecision(9, 4);
            e.Property(x => x.TaxPercent).HasPrecision(9, 4);

            e.HasIndex(x => new { x.OrderId, x.DisplayOrder }).HasDatabaseName("IX_OrderLine_Order_Order");
            e.HasIndex(x => x.ItemId).HasDatabaseName("IX_OrderLine_Item");
            e.HasIndex(x => x.SchemeId).HasDatabaseName("IX_OrderLine_Scheme");
        });

        b.Entity<OrderStatusEvent>(e =>
        {
            e.ToTable("OrderStatusEvents", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ActorName).HasMaxLength(200);
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasIndex(x => new { x.OrderId, x.OccurredAt }).HasDatabaseName("IX_OrderStatus_Order_At");
        });

        b.Entity<OrderApproval>(e =>
        {
            e.ToTable("OrderApprovals", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.StepName).IsRequired().HasMaxLength(120);
            e.Property(x => x.TriggerReason).HasMaxLength(500);
            e.Property(x => x.ApproverName).HasMaxLength(200);
            e.Property(x => x.Comment).HasMaxLength(1000);
            e.HasIndex(x => new { x.OrderId, x.StepNumber }).HasDatabaseName("IX_OrderApproval_Order_Step");
        });

        b.Entity<StockAllocation>(e =>
        {
            e.ToTable("StockAllocations", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.BatchNumber).HasMaxLength(60);
            e.Property(x => x.FefoOverrideReason).HasMaxLength(500);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.HasIndex(x => new { x.OrderLineId, x.BatchId }).HasDatabaseName("IX_Allocation_Line_Batch");
            e.HasIndex(x => new { x.ItemId, x.WarehouseId }).HasDatabaseName("IX_Allocation_Item_Warehouse");
        });

        b.Entity<PickWave>(e =>
        {
            e.ToTable("PickWaves", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.WaveNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.TotalLines).HasPrecision(18, 4);
            e.Property(x => x.PickedLines).HasPrecision(18, 4);
            e.Property(x => x.ShortLines).HasPrecision(18, 4);
            e.HasIndex(x => new { x.CompanyId, x.WaveNumber }).IsUnique().HasDatabaseName("IX_Wave_Number");
            e.HasIndex(x => new { x.WarehouseId, x.ReleasedAt }).HasDatabaseName("IX_Wave_Warehouse_At");
            e.HasMany(x => x.Tasks).WithOne(t => t.Wave)
                .HasForeignKey(t => t.WaveId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PickTask>(e =>
        {
            e.ToTable("PickTasks", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.TaskNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.AssignedToName).HasMaxLength(200);
            e.Property(x => x.ZoneName).HasMaxLength(60);
            e.HasIndex(x => new { x.WaveId, x.Status }).HasDatabaseName("IX_PickTask_Wave_Status");
            e.HasMany(x => x.Lines).WithOne(l => l.Task)
                .HasForeignKey(l => l.TaskId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PickTaskLine>(e =>
        {
            e.ToTable("PickTaskLines", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
            e.Property(x => x.ItemCode).HasMaxLength(60);
            e.Property(x => x.BinCode).HasMaxLength(60);
            e.Property(x => x.NominatedBatchNumber).HasMaxLength(60);
            e.Property(x => x.PickedBatchNumber).HasMaxLength(60);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);
            e.Property(x => x.OverrideReason).HasMaxLength(500);
            e.Property(x => x.RequiredQuantity).HasPrecision(18, 4);
            e.Property(x => x.PickedQuantity).HasPrecision(18, 4);
            e.HasIndex(x => new { x.TaskId, x.PickSequence }).HasDatabaseName("IX_PickLine_Task_Seq");
        });

        b.Entity<PackageUnit>(e =>
        {
            e.ToTable("Packages", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.LicencePlate).IsRequired().HasMaxLength(60);
            e.Property(x => x.StagingLocation).HasMaxLength(60);
            e.Property(x => x.SealNumber).HasMaxLength(60);
            e.Property(x => x.WeightKg).HasPrecision(12, 3);
            e.Property(x => x.LengthCm).HasPrecision(10, 2);
            e.Property(x => x.WidthCm).HasPrecision(10, 2);
            e.Property(x => x.HeightCm).HasPrecision(10, 2);
            e.HasIndex(x => new { x.CompanyId, x.LicencePlate })
                .IsUnique().HasDatabaseName("IX_Package_LicencePlate");
            e.HasIndex(x => x.TripId).HasDatabaseName("IX_Package_Trip");
            e.HasMany(x => x.Contents).WithOne(c => c.Package)
                .HasForeignKey(c => c.PackageId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PackageContent>(e =>
        {
            e.ToTable("PackageContents", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
            e.Property(x => x.BatchNumber).HasMaxLength(60);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);
            e.Property(x => x.SerialNumbers).HasMaxLength(2000);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
        });

        b.Entity<Dispatch>(e =>
        {
            e.ToTable("Dispatches", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.DispatchNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.GatePassNumber).HasMaxLength(60);
            e.Property(x => x.TransportDocumentNumber).HasMaxLength(80);
            e.Property(x => x.CarrierName).HasMaxLength(200);
            e.Property(x => x.AirwayBillNumber).HasMaxLength(80);
            e.Property(x => x.DriverAcknowledgement).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.FreightCost).HasPrecision(18, 4);
            e.Property(x => x.TotalWeightKg).HasPrecision(12, 3);
            e.Property(x => x.TotalValue).HasPrecision(18, 4);
            e.HasIndex(x => new { x.CompanyId, x.DispatchNumber })
                .IsUnique().HasDatabaseName("IX_Dispatch_Number");
            e.HasMany(x => x.Lines).WithOne(l => l.Dispatch)
                .HasForeignKey(l => l.DispatchId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<DispatchLine>(e =>
        {
            e.ToTable("DispatchLines", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.OrderNumber).HasMaxLength(50);
            e.Property(x => x.OutletName).HasMaxLength(250);
            e.Property(x => x.Value).HasPrecision(18, 4);
        });
    }

    // ═══ Logistics ═══════════════════════════════════════════════════════════

    private static void ConfigureLogistics(ModelBuilder b)
    {
        b.Entity<Vehicle>(e =>
        {
            e.ToTable("Vehicles", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.RegistrationNumber).IsRequired().HasMaxLength(40);
            e.Property(x => x.Name).HasMaxLength(120);
            e.Property(x => x.Make).HasMaxLength(80);
            e.Property(x => x.Model).HasMaxLength(80);
            e.Property(x => x.FuelType).HasMaxLength(40);
            e.Property(x => x.OutOfServiceReason).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.CapacityWeightKg).HasPrecision(12, 3);
            e.Property(x => x.CapacityVolumeM3).HasPrecision(12, 3);
            e.Property(x => x.MinSafeCelsius).HasPrecision(6, 2);
            e.Property(x => x.MaxSafeCelsius).HasPrecision(6, 2);
            e.Property(x => x.CurrentOdometerKm).HasPrecision(12, 2);
            e.Property(x => x.FuelEfficiencyKmPerUnit).HasPrecision(10, 3);
            e.Property(x => x.NextServiceDueAtKm).HasPrecision(12, 2);
            e.HasIndex(x => new { x.CompanyId, x.RegistrationNumber })
                .IsUnique().HasDatabaseName("IX_Vehicle_Registration");
            e.HasMany(x => x.ComplianceRecords).WithOne(c => c.Vehicle)
                .HasForeignKey(c => c.VehicleId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<VehicleCompliance>(e =>
        {
            e.ToTable("VehicleCompliances", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.DocumentNumber).HasMaxLength(80);
            e.Property(x => x.Issuer).HasMaxLength(200);
            e.Property(x => x.FileUrl).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.Cost).HasPrecision(18, 4);
            e.HasIndex(x => new { x.CompanyId, x.ExpiresOn }).HasDatabaseName("IX_VehicleCompliance_Expiry");
        });

        b.Entity<Driver>(e =>
        {
            e.ToTable("Drivers", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.PhotoUrl).HasMaxLength(500);
            e.Property(x => x.LicenceNumber).HasMaxLength(60);
            e.Property(x => x.LicenceClass).HasMaxLength(40);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.HasIndex(x => new { x.CompanyId, x.Code }).HasDatabaseName("IX_Driver_Code");
            e.HasIndex(x => new { x.CompanyId, x.LicenceExpiresOn }).HasDatabaseName("IX_Driver_LicenceExpiry");
        });

        b.Entity<DeliveryTrip>(e =>
        {
            e.ToTable("Trips", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.TripNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.HelperName).HasMaxLength(200);
            e.Property(x => x.CancelReason).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(1000);

            foreach (var m in new[]
            {
                nameof(DeliveryTrip.OdometerOutKm), nameof(DeliveryTrip.OdometerInKm),
                nameof(DeliveryTrip.DistanceKm), nameof(DeliveryTrip.PlannedDistanceKm),
                nameof(DeliveryTrip.FuelIssued),
            })
                e.Property(m).HasPrecision(12, 3);

            foreach (var money in new[]
            {
                nameof(DeliveryTrip.PlannedValue), nameof(DeliveryTrip.DeliveredValue),
                nameof(DeliveryTrip.CollectedAmount), nameof(DeliveryTrip.ReturnValue),
                nameof(DeliveryTrip.TotalExpense),
            })
                e.Property(money).HasPrecision(18, 4);

            e.Property(x => x.FillRatePercent).HasPrecision(9, 4);
            e.Property(x => x.OnTimePercent).HasPrecision(9, 4);

            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.TripNumber })
                .IsUnique().HasDatabaseName("IX_Trip_Tenant_Number");
            e.HasIndex(x => new { x.CompanyId, x.TripDate, x.Status }).HasDatabaseName("IX_Trip_Date_Status");

            e.HasOne(x => x.Vehicle).WithMany()
                .HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Driver).WithMany()
                .HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Stops).WithOne(s => s.Trip)
                .HasForeignKey(s => s.TripId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Expenses).WithOne(x2 => x2.Trip)
                .HasForeignKey(x2 => x2.TripId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<TripStop>(e =>
        {
            e.ToTable("TripStops", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.DestinationName).HasMaxLength(250);
            e.Property(x => x.AddressLine).HasMaxLength(500);
            e.Property(x => x.FailureNote).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.PlannedValue).HasPrecision(18, 4);
            e.Property(x => x.DeliveredValue).HasPrecision(18, 4);
            e.Property(x => x.CollectedAmount).HasPrecision(18, 4);
            e.Property(x => x.ReturnValue).HasPrecision(18, 4);
            e.HasIndex(x => new { x.TripId, x.StopSequence }).HasDatabaseName("IX_TripStop_Trip_Seq");
            e.HasOne(x => x.Outlet).WithMany()
                .HasForeignKey(x => x.OutletId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<TripExpense>(e =>
        {
            e.ToTable("TripExpenses", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.Reference).HasMaxLength(120);
            e.Property(x => x.ReceiptUrl).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.RejectionReason).HasMaxLength(500);
            e.Property(x => x.Amount).HasPrecision(18, 4);
            e.HasIndex(x => new { x.TripId, x.Kind }).HasDatabaseName("IX_TripExpense_Trip_Kind");
        });

        b.Entity<ProofOfDelivery>(e =>
        {
            e.ToTable("ProofsOfDelivery", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.PodNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.ReceivedByName).HasMaxLength(200);
            e.Property(x => x.ReceivedByPhone).HasMaxLength(50);
            e.Property(x => x.SignatureImageUrl).HasMaxLength(500);
            e.Property(x => x.PhotoUrl).HasMaxLength(500);
            e.Property(x => x.OtpReference).HasMaxLength(60);
            e.Property(x => x.ExceptionNote).HasMaxLength(1000);
            e.Property(x => x.ReceiptSentTo).HasMaxLength(200);
            e.Property(x => x.Note).HasMaxLength(1000);

            foreach (var money in new[]
            {
                nameof(ProofOfDelivery.DeliveredValue), nameof(ProofOfDelivery.ShortValue),
                nameof(ProofOfDelivery.DamagedValue), nameof(ProofOfDelivery.RejectedValue),
                nameof(ProofOfDelivery.CollectedAmount),
            })
                e.Property(money).HasPrecision(18, 4);

            e.HasIndex(x => new { x.CompanyId, x.PodNumber }).IsUnique().HasDatabaseName("IX_Pod_Number");
            // The exception queue: the deliveries that did not go cleanly.
            e.HasIndex(x => new { x.CompanyId, x.IsClean, x.IsExceptionResolved })
                .HasDatabaseName("IX_Pod_Exceptions");
            e.HasMany(x => x.Lines).WithOne(l => l.Pod)
                .HasForeignKey(l => l.PodId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PodLine>(e =>
        {
            e.ToTable("PodLines", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
            e.Property(x => x.BatchNumber).HasMaxLength(60);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);
            e.Property(x => x.PhotoUrl).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(500);

            foreach (var q in new[]
            {
                nameof(PodLine.DespatchedQuantity), nameof(PodLine.AcceptedQuantity),
                nameof(PodLine.ShortQuantity), nameof(PodLine.DamagedQuantity),
                nameof(PodLine.RejectedQuantity), nameof(PodLine.UnitPrice), nameof(PodLine.CreditValue),
            })
                e.Property(q).HasPrecision(18, 4);
        });
    }

    // ═══ Returns ═════════════════════════════════════════════════════════════

    private static void ConfigureReturns(ModelBuilder b)
    {
        b.Entity<ReturnAuthorisation>(e =>
        {
            e.ToTable("Returns", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ReturnNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.OriginalInvoiceNumber).HasMaxLength(50);
            e.Property(x => x.ReasonNote).HasMaxLength(500);
            e.Property(x => x.RejectionReason).HasMaxLength(500);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.PhotoUrl).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.ValuationPercent).HasPrecision(9, 4);
            e.Property(x => x.ClaimedValue).HasPrecision(18, 4);
            e.Property(x => x.ApprovedValue).HasPrecision(18, 4);
            e.Property(x => x.CreditedValue).HasPrecision(18, 4);

            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.ReturnNumber })
                .IsUnique().HasDatabaseName("IX_Return_Tenant_Number");
            e.HasIndex(x => new { x.CompanyId, x.Status, x.RequestedOn }).HasDatabaseName("IX_Return_Status_On");
            e.HasIndex(x => x.OutletId).HasDatabaseName("IX_Return_Outlet");

            e.HasOne(x => x.Outlet).WithMany()
                .HasForeignKey(x => x.OutletId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Lines).WithOne(l => l.Return)
                .HasForeignKey(l => l.ReturnId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ReturnAuthorisationLine>(e =>
        {
            e.ToTable("ReturnLines", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
            e.Property(x => x.ItemCode).HasMaxLength(60);
            e.Property(x => x.BatchNumber).HasMaxLength(60);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);
            e.Property(x => x.PhotoUrl).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(500);

            foreach (var q in new[]
            {
                nameof(ReturnAuthorisationLine.UomFactor), nameof(ReturnAuthorisationLine.RequestedQuantity),
                nameof(ReturnAuthorisationLine.ApprovedQuantity), nameof(ReturnAuthorisationLine.ReceivedQuantity),
                nameof(ReturnAuthorisationLine.UnitPrice), nameof(ReturnAuthorisationLine.UnitCost),
                nameof(ReturnAuthorisationLine.LineValue),
            })
                e.Property(q).HasPrecision(18, 4);
        });

        b.Entity<ReturnReceipt>(e =>
        {
            e.ToTable("ReturnReceipts", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ReceiptNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.DestructionCertificateNumber).HasMaxLength(80);
            e.Property(x => x.DestructionCertificateUrl).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.TotalValue).HasPrecision(18, 4);
            e.Property(x => x.RestockedValue).HasPrecision(18, 4);
            e.Property(x => x.ScrappedValue).HasPrecision(18, 4);
            e.HasIndex(x => new { x.CompanyId, x.ReceiptNumber })
                .IsUnique().HasDatabaseName("IX_ReturnReceipt_Number");
            e.HasOne(x => x.Return).WithMany()
                .HasForeignKey(x => x.ReturnId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Lines).WithOne(l => l.Receipt)
                .HasForeignKey(l => l.ReceiptId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ReturnReceiptLine>(e =>
        {
            e.ToTable("ReturnReceiptLines", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
            e.Property(x => x.BatchNumber).HasMaxLength(60);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);
            e.Property(x => x.Note).HasMaxLength(500);

            foreach (var q in new[]
            {
                nameof(ReturnReceiptLine.ExpectedQuantity), nameof(ReturnReceiptLine.ReceivedQuantity),
                nameof(ReturnReceiptLine.AcceptedQuantity), nameof(ReturnReceiptLine.RejectedQuantity),
                nameof(ReturnReceiptLine.UnitPrice), nameof(ReturnReceiptLine.UnitCost),
                nameof(ReturnReceiptLine.LineValue),
            })
                e.Property(q).HasPrecision(18, 4);
        });

        b.Entity<ReturnDisposition>(e =>
        {
            e.ToTable("ReturnDispositions", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.Property(x => x.Value).HasPrecision(18, 4);
            e.HasOne(x => x.ReceiptLine).WithMany()
                .HasForeignKey(x => x.ReceiptLineId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    // ═══ Trade ═══════════════════════════════════════════════════════════════

    private static void ConfigureTrade(ModelBuilder b)
    {
        b.Entity<ChannelPriceList>(e =>
        {
            e.ToTable("PriceLists", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.HasIndex(x => new { x.CompanyId, x.Scope, x.EffectiveFrom }).HasDatabaseName("IX_PriceList_Scope_From");
            e.HasMany(x => x.Lines).WithOne(l => l.PriceList)
                .HasForeignKey(l => l.PriceListId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ChannelPriceListLine>(e =>
        {
            e.ToTable("PriceListLines", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
            e.Property(x => x.ItemCode).HasMaxLength(60);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);
            e.Property(x => x.UomFactor).HasPrecision(18, 4);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.Mrp).HasPrecision(18, 4);
            e.Property(x => x.MinimumPrice).HasPrecision(18, 4);
            e.Property(x => x.MaxDiscountPercent).HasPrecision(9, 4);
            e.Property(x => x.TaxPercent).HasPrecision(9, 4);
            e.HasIndex(x => new { x.PriceListId, x.ItemId, x.Uom })
                .IsUnique().HasDatabaseName("IX_PriceListLine_List_Item_Uom");
            e.HasMany(x => x.Slabs).WithOne(s => s.PriceListLine)
                .HasForeignKey(s => s.PriceListLineId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PriceSlab>(e =>
        {
            e.ToTable("PriceSlabs", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.FromQuantity).HasPrecision(18, 4);
            e.Property(x => x.ToQuantity).HasPrecision(18, 4);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.DiscountPercent).HasPrecision(9, 4);
        });

        b.Entity<MarginLadder>(e =>
        {
            e.ToTable("MarginLadders", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
            e.Property(x => x.ItemCode).HasMaxLength(60);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.Note).HasMaxLength(1000);

            foreach (var money in new[]
            {
                nameof(MarginLadder.LandedCost), nameof(MarginLadder.PriceToDistributor),
                nameof(MarginLadder.PriceToWholesaler), nameof(MarginLadder.PriceToRetailer),
                nameof(MarginLadder.Mrp),
            })
                e.Property(money).HasPrecision(18, 4);

            foreach (var pct in new[]
            {
                nameof(MarginLadder.CompanyMarginPercent), nameof(MarginLadder.DistributorMarginPercent),
                nameof(MarginLadder.WholesalerMarginPercent), nameof(MarginLadder.RetailerMarginPercent),
            })
                e.Property(pct).HasPrecision(9, 4);

            e.HasIndex(x => new { x.CompanyId, x.ItemId, x.EffectiveFrom })
                .HasDatabaseName("IX_MarginLadder_Item_From");
        });

        b.Entity<MrpRevision>(e =>
        {
            e.ToTable("MrpRevisions", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
            e.Property(x => x.BatchNumber).HasMaxLength(60);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);
            e.Property(x => x.Reason).HasMaxLength(500);
            e.Property(x => x.OldMrp).HasPrecision(18, 4);
            e.Property(x => x.NewMrp).HasPrecision(18, 4);
            e.Property(x => x.ProtectionPerUnit).HasPrecision(18, 4);
            e.HasIndex(x => new { x.CompanyId, x.ItemId, x.EffectiveFrom })
                .HasDatabaseName("IX_MrpRevision_Item_From");
        });

        b.Entity<TradeScheme>(e =>
        {
            e.ToTable("Schemes", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.SchemeNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.Name).IsRequired().HasMaxLength(250);
            e.Property(x => x.StackingGroup).HasMaxLength(60);
            e.Property(x => x.ActiveDays).HasMaxLength(30);
            e.Property(x => x.FreeItemName).HasMaxLength(200);
            e.Property(x => x.FreeItemUom).HasMaxLength(20);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.RejectionReason).HasMaxLength(500);
            e.Property(x => x.CommunicationPackUrl).HasMaxLength(500);
            e.Property(x => x.TermsAndConditions).HasMaxLength(4000);
            e.Property(x => x.Note).HasMaxLength(1000);

            foreach (var money in new[]
            {
                nameof(TradeScheme.MinQuantity), nameof(TradeScheme.MinValue),
                nameof(TradeScheme.FreeQuantity), nameof(TradeScheme.DiscountAmount),
                nameof(TradeScheme.MaxBenefitPerOrder), nameof(TradeScheme.MaxBenefitPerOutlet),
                nameof(TradeScheme.DisplayPayout), nameof(TradeScheme.BudgetAmount),
                nameof(TradeScheme.ConsumedAmount), nameof(TradeScheme.QualifyingSalesValue),
            })
                e.Property(money).HasPrecision(18, 4);

            e.Property(x => x.DiscountPercent).HasPrecision(9, 4);

            e.HasIndex(x => new { x.CompanyId, x.SchemeNumber }).IsUnique().HasDatabaseName("IX_Scheme_Number");
            // The order-entry hot path: which schemes are live right now.
            e.HasIndex(x => new { x.CompanyId, x.Status, x.ValidFrom, x.ValidTo })
                .HasDatabaseName("IX_Scheme_Active_Window");

            e.HasMany(x => x.Slabs).WithOne(s => s.Scheme)
                .HasForeignKey(s => s.SchemeId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Products).WithOne(p => p.Scheme)
                .HasForeignKey(p => p.SchemeId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Scopes).WithOne(s => s.Scheme)
                .HasForeignKey(s => s.SchemeId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<TradeSchemeSlab>(e =>
        {
            e.ToTable("SchemeSlabs", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.FreeItemName).HasMaxLength(200);
            e.Property(x => x.Label).HasMaxLength(200);

            foreach (var q in new[]
            {
                nameof(TradeSchemeSlab.FromQuantity), nameof(TradeSchemeSlab.ToQuantity),
                nameof(TradeSchemeSlab.FromValue), nameof(TradeSchemeSlab.ToValue),
                nameof(TradeSchemeSlab.FreeQuantity), nameof(TradeSchemeSlab.DiscountAmount),
                nameof(TradeSchemeSlab.PayoutAmount), nameof(TradeSchemeSlab.PointsAwarded),
            })
                e.Property(q).HasPrecision(18, 4);

            e.Property(x => x.DiscountPercent).HasPrecision(9, 4);
            e.HasIndex(x => new { x.SchemeId, x.SlabNumber }).HasDatabaseName("IX_SchemeSlab_Scheme_Number");
        });

        b.Entity<TradeSchemeProduct>(e =>
        {
            e.ToTable("SchemeProducts", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).HasMaxLength(200);
            e.Property(x => x.Uom).HasMaxLength(20);
            e.Property(x => x.RequiredQuantity).HasPrecision(18, 4);
            e.Property(x => x.UomFactor).HasPrecision(18, 4);
            e.HasIndex(x => new { x.SchemeId, x.ItemId }).HasDatabaseName("IX_SchemeProduct_Scheme_Item");
        });

        b.Entity<TradeSchemeScope>(e =>
        {
            e.ToTable("SchemeScopes", DefaultSchema);
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.SchemeId, x.Channel }).HasDatabaseName("IX_SchemeScope_Scheme_Channel");
        });

        b.Entity<SchemeApplication>(e =>
        {
            e.ToTable("SchemeApplications", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.SchemeName).IsRequired().HasMaxLength(250);
            e.Property(x => x.FreeItemName).HasMaxLength(200);
            e.Property(x => x.BenefitDescription).HasMaxLength(500);
            e.Property(x => x.ReversalReason).HasMaxLength(500);

            foreach (var money in new[]
            {
                nameof(SchemeApplication.QualifyingQuantity), nameof(SchemeApplication.QualifyingValue),
                nameof(SchemeApplication.FreeQuantity), nameof(SchemeApplication.DiscountAmount),
                nameof(SchemeApplication.PayoutAmount), nameof(SchemeApplication.PointsAwarded),
                nameof(SchemeApplication.BenefitValue),
            })
                e.Property(money).HasPrecision(18, 4);

            e.HasIndex(x => new { x.SchemeId, x.AppliedAt }).HasDatabaseName("IX_SchemeApp_Scheme_At");
            e.HasIndex(x => x.OrderId).HasDatabaseName("IX_SchemeApp_Order");
            e.HasIndex(x => new { x.OutletId, x.AppliedAt }).HasDatabaseName("IX_SchemeApp_Outlet_At");
            e.HasOne(x => x.Scheme).WithMany()
                .HasForeignKey(x => x.SchemeId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<SchemeBudgetLedger>(e =>
        {
            e.ToTable("SchemeBudgetLedger", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Reason).HasMaxLength(500);
            e.Property(x => x.Amount).HasPrecision(18, 4);
            e.Property(x => x.BalanceAfter).HasPrecision(18, 4);
            e.HasIndex(x => new { x.SchemeId, x.OccurredAt }).HasDatabaseName("IX_SchemeBudget_Scheme_At");
        });
    }

    // ═══ Money ═══════════════════════════════════════════════════════════════

    private static void ConfigureMoney(ModelBuilder b)
    {
        b.Entity<ChannelClaim>(e =>
        {
            e.ToTable("Claims", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ClaimNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.QueryNote).HasMaxLength(1000);
            e.Property(x => x.RejectionNote).HasMaxLength(1000);
            e.Property(x => x.SettlementReference).HasMaxLength(120);
            e.Property(x => x.Note).HasMaxLength(2000);

            foreach (var money in new[]
            {
                nameof(ChannelClaim.ClaimedAmount), nameof(ChannelClaim.ComputedAmount),
                nameof(ChannelClaim.VarianceAmount), nameof(ChannelClaim.ApprovedAmount),
                nameof(ChannelClaim.SettledAmount),
            })
                e.Property(money).HasPrecision(18, 4);

            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.ClaimNumber })
                .IsUnique().HasDatabaseName("IX_Claim_Tenant_Number");
            e.HasIndex(x => new { x.CompanyId, x.Status, x.SubmittedOn }).HasDatabaseName("IX_Claim_Status_On");
            e.HasIndex(x => new { x.PartnerId, x.Kind }).HasDatabaseName("IX_Claim_Partner_Kind");

            e.HasOne(x => x.Partner).WithMany()
                .HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Scheme).WithMany()
                .HasForeignKey(x => x.SchemeId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Lines).WithOne(l => l.Claim)
                .HasForeignKey(l => l.ClaimId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Documents).WithOne(d => d.Claim)
                .HasForeignKey(d => d.ClaimId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.StatusEvents).WithOne(s => s.Claim)
                .HasForeignKey(s => s.ClaimId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ChannelClaimLine>(e =>
        {
            e.ToTable("ClaimLines", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).HasMaxLength(200);
            e.Property(x => x.BatchNumber).HasMaxLength(60);
            e.Property(x => x.SourceInvoiceNumber).HasMaxLength(50);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);
            e.Property(x => x.RejectionNote).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(500);

            foreach (var money in new[]
            {
                nameof(ChannelClaimLine.Quantity), nameof(ChannelClaimLine.UnitRate),
                nameof(ChannelClaimLine.ClaimedAmount), nameof(ChannelClaimLine.ComputedAmount),
                nameof(ChannelClaimLine.ApprovedAmount),
            })
                e.Property(money).HasPrecision(18, 4);

            e.HasIndex(x => new { x.ClaimId, x.DisplayOrder }).HasDatabaseName("IX_ClaimLine_Claim_Order");
        });

        b.Entity<ClaimDocument>(e =>
        {
            e.ToTable("ClaimDocuments", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.DocumentType).IsRequired().HasMaxLength(120);
            e.Property(x => x.FileUrl).IsRequired().HasMaxLength(500);
            e.Property(x => x.FileName).HasMaxLength(250);
            e.Property(x => x.Note).HasMaxLength(500);
        });

        b.Entity<ClaimStatusEvent>(e =>
        {
            e.ToTable("ClaimStatusEvents", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ActorName).HasMaxLength(200);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.HasIndex(x => new { x.ClaimId, x.OccurredAt }).HasDatabaseName("IX_ClaimStatus_Claim_At");
        });

        b.Entity<SupplierRebateAgreement>(e =>
        {
            e.ToTable("RebateAgreements", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.AgreementNumber).IsRequired().HasMaxLength(60);
            e.Property(x => x.Name).IsRequired().HasMaxLength(250);
            e.Property(x => x.SupplierName).HasMaxLength(250);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.RebateBasis).IsRequired().HasMaxLength(60);
            e.Property(x => x.Terms).HasMaxLength(4000);
            e.Property(x => x.FileUrl).HasMaxLength(500);

            foreach (var money in new[]
            {
                nameof(SupplierRebateAgreement.ThresholdQuantity), nameof(SupplierRebateAgreement.ThresholdValue),
                nameof(SupplierRebateAgreement.RebatePerUnit), nameof(SupplierRebateAgreement.BaselineValue),
                nameof(SupplierRebateAgreement.AccruedAmount), nameof(SupplierRebateAgreement.ReceivedAmount),
            })
                e.Property(money).HasPrecision(18, 4);

            e.Property(x => x.RebatePercent).HasPrecision(9, 4);
            e.HasMany(x => x.Accruals).WithOne(a => a.Agreement)
                .HasForeignKey(a => a.AgreementId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<RebateAccrual>(e =>
        {
            e.ToTable("RebateAccruals", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.SupplierCreditReference).HasMaxLength(120);
            e.Property(x => x.Note).HasMaxLength(500);

            foreach (var money in new[]
            {
                nameof(RebateAccrual.QualifyingQuantity), nameof(RebateAccrual.QualifyingValue),
                nameof(RebateAccrual.AccruedAmount), nameof(RebateAccrual.ReceivedAmount),
            })
                e.Property(money).HasPrecision(18, 4);

            e.HasIndex(x => new { x.AgreementId, x.PeriodStart }).HasDatabaseName("IX_Rebate_Agreement_Period");
        });

        b.Entity<Chargeback>(e =>
        {
            e.ToTable("Chargebacks", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ChargebackNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.SupplierName).HasMaxLength(250);
            e.Property(x => x.ContractCustomerName).HasMaxLength(250);
            e.Property(x => x.ContractReference).HasMaxLength(120);
            e.Property(x => x.ItemName).HasMaxLength(200);
            e.Property(x => x.SourceInvoiceNumber).HasMaxLength(50);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.SettlementReference).HasMaxLength(120);
            e.Property(x => x.RejectionNote).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(1000);

            foreach (var money in new[]
            {
                nameof(Chargeback.Quantity), nameof(Chargeback.AcquisitionPrice),
                nameof(Chargeback.ContractPrice), nameof(Chargeback.ChargebackAmount),
                nameof(Chargeback.ApprovedAmount), nameof(Chargeback.SettledAmount),
            })
                e.Property(money).HasPrecision(18, 4);

            e.HasIndex(x => new { x.CompanyId, x.ChargebackNumber })
                .IsUnique().HasDatabaseName("IX_Chargeback_Number");
        });

        b.Entity<CreditProfile>(e =>
        {
            e.ToTable("CreditProfiles", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.BlockReason).HasMaxLength(500);

            foreach (var money in new[]
            {
                nameof(CreditProfile.CreditLimit), nameof(CreditProfile.TemporaryLimit),
                nameof(CreditProfile.OutstandingAmount), nameof(CreditProfile.OverdueAmount),
                nameof(CreditProfile.UnbilledOrderValue), nameof(CreditProfile.AvailableCredit),
                nameof(CreditProfile.Bucket0To30), nameof(CreditProfile.Bucket31To60),
                nameof(CreditProfile.Bucket61To90), nameof(CreditProfile.Bucket90Plus),
                nameof(CreditProfile.LastPaymentAmount), nameof(CreditProfile.SecurityDeposit),
                nameof(CreditProfile.AdvanceHeld), nameof(CreditProfile.ProvisionedAmount),
            })
                e.Property(money).HasPrecision(18, 4);

            // One live profile per outlet and per partner; two would let a block be bypassed.
            e.HasIndex(x => x.OutletId).IsUnique().HasDatabaseName("IX_CreditProfile_Outlet")
                .HasFilter("outlet_id IS NOT NULL");
            e.HasIndex(x => x.PartnerId).IsUnique().HasDatabaseName("IX_CreditProfile_Partner")
                .HasFilter("partner_id IS NOT NULL");

            e.HasOne(x => x.Outlet).WithMany()
                .HasForeignKey(x => x.OutletId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Partner).WithMany()
                .HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<CreditOverride>(e =>
        {
            e.ToTable("CreditOverrides", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.Justification).HasMaxLength(1000);
            e.Property(x => x.ApproverName).HasMaxLength(200);
            e.Property(x => x.DecisionNote).HasMaxLength(1000);
            e.Property(x => x.RequestedAmount).HasPrecision(18, 4);
            e.Property(x => x.ApprovedAmount).HasPrecision(18, 4);
            e.HasIndex(x => new { x.CompanyId, x.RequestedAt }).HasDatabaseName("IX_CreditOverride_At");
        });

        b.Entity<CollectionReceipt>(e =>
        {
            e.ToTable("Collections", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ReceiptNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.Reference).HasMaxLength(200);
            e.Property(x => x.BankName).HasMaxLength(200);
            e.Property(x => x.CardLast4).HasMaxLength(4);
            e.Property(x => x.CardScheme).HasMaxLength(40);
            e.Property(x => x.ReversalReason).HasMaxLength(500);
            e.Property(x => x.ReceiptSentTo).HasMaxLength(200);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.IdempotencyKey).HasMaxLength(80);
            e.Property(x => x.ExchangeRate).HasPrecision(18, 8);

            foreach (var money in new[]
            {
                nameof(CollectionReceipt.Amount), nameof(CollectionReceipt.CashDiscountAmount),
                nameof(CollectionReceipt.AllocatedAmount), nameof(CollectionReceipt.UnallocatedAmount),
            })
                e.Property(money).HasPrecision(18, 4);

            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.ReceiptNumber })
                .IsUnique().HasDatabaseName("IX_Collection_Tenant_Number");
            e.HasIndex(x => new { x.OutletId, x.CollectedAt }).HasDatabaseName("IX_Collection_Outlet_At");
            e.HasIndex(x => new { x.FieldDayId, x.Tender }).HasDatabaseName("IX_Collection_Day_Tender");

            // A retried sync must not double-credit an outlet.
            e.HasIndex(x => new { x.CompanyId, x.IdempotencyKey })
                .IsUnique().HasDatabaseName("IX_Collection_Idempotency")
                .HasFilter("idempotency_key IS NOT NULL");

            e.HasOne(x => x.Outlet).WithMany()
                .HasForeignKey(x => x.OutletId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<ChequeRecord>(e =>
        {
            e.ToTable("Cheques", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ChequeNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.BankName).HasMaxLength(200);
            e.Property(x => x.BranchName).HasMaxLength(200);
            e.Property(x => x.AccountName).HasMaxLength(200);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.DepositBankAccount).HasMaxLength(80);
            e.Property(x => x.BounceReason).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.Amount).HasPrecision(18, 4);
            e.Property(x => x.BounceCharges).HasPrecision(18, 4);
            e.HasIndex(x => new { x.CompanyId, x.Status, x.ChequeDate }).HasDatabaseName("IX_Cheque_Status_Date");
            e.HasIndex(x => x.OutletId).HasDatabaseName("IX_Cheque_Outlet");
        });

        b.Entity<RouteSettlement>(e =>
        {
            e.ToTable("Settlements", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.SettlementNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.ReversalReason).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(2000);

            foreach (var money in new[]
            {
                nameof(RouteSettlement.CashSalesValue), nameof(RouteSettlement.CreditSalesValue),
                nameof(RouteSettlement.TotalSalesValue), nameof(RouteSettlement.TaxValue),
                nameof(RouteSettlement.DiscountValue), nameof(RouteSettlement.SchemeValue),
                nameof(RouteSettlement.FreeGoodsValue), nameof(RouteSettlement.ReturnValue),
                nameof(RouteSettlement.CashCollected), nameof(RouteSettlement.ChequeCollected),
                nameof(RouteSettlement.DigitalCollected), nameof(RouteSettlement.TotalCollected),
                nameof(RouteSettlement.OpeningFloat), nameof(RouteSettlement.ExpectedCash),
                nameof(RouteSettlement.DeclaredCash), nameof(RouteSettlement.CashVariance),
                nameof(RouteSettlement.ExpenseAmount), nameof(RouteSettlement.CashToDeposit),
                nameof(RouteSettlement.OpeningStockValue), nameof(RouteSettlement.LoadedStockValue),
                nameof(RouteSettlement.SoldStockValue), nameof(RouteSettlement.ReturnedStockValue),
                nameof(RouteSettlement.ExpectedClosingStockValue), nameof(RouteSettlement.CountedClosingStockValue),
                nameof(RouteSettlement.StockVarianceValue),
            })
                e.Property(money).HasPrecision(18, 4);

            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.SettlementNumber })
                .IsUnique().HasDatabaseName("IX_Settlement_Tenant_Number");
            e.HasIndex(x => new { x.CompanyId, x.SettlementDate, x.Status })
                .HasDatabaseName("IX_Settlement_Date_Status");
            e.HasIndex(x => x.FieldDayId).HasDatabaseName("IX_Settlement_Day");

            e.HasOne(x => x.FieldRep).WithMany()
                .HasForeignKey(x => x.FieldRepId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Variances).WithOne(v => v.Settlement)
                .HasForeignKey(v => v.SettlementId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<SettlementVariance>(e =>
        {
            e.ToTable("SettlementVariances", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).HasMaxLength(200);
            e.Property(x => x.BatchNumber).HasMaxLength(60);
            e.Property(x => x.Uom).HasMaxLength(20);
            e.Property(x => x.ReasonNote).HasMaxLength(500);

            foreach (var q in new[]
            {
                nameof(SettlementVariance.ExpectedQuantity), nameof(SettlementVariance.ActualQuantity),
                nameof(SettlementVariance.VarianceQuantity), nameof(SettlementVariance.ExpectedAmount),
                nameof(SettlementVariance.ActualAmount), nameof(SettlementVariance.VarianceAmount),
                nameof(SettlementVariance.RecoveredAmount),
            })
                e.Property(q).HasPrecision(18, 4);

            e.HasIndex(x => new { x.SettlementId, x.Kind }).HasDatabaseName("IX_Variance_Settlement_Kind");
        });

        b.Entity<CashDeposit>(e =>
        {
            e.ToTable("CashDeposits", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.DepositNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.BankName).HasMaxLength(200);
            e.Property(x => x.BankAccount).HasMaxLength(80);
            e.Property(x => x.SlipReference).HasMaxLength(120);
            e.Property(x => x.SlipImageUrl).HasMaxLength(500);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.Amount).HasPrecision(18, 4);
            e.HasIndex(x => new { x.CompanyId, x.DepositedOn }).HasDatabaseName("IX_Deposit_On");
            e.HasIndex(x => new { x.CompanyId, x.IsReconciled }).HasDatabaseName("IX_Deposit_Reconciled");
        });
    }

    // ═══ Secondary sales ═════════════════════════════════════════════════════

    private static void ConfigureSecondary(ModelBuilder b)
    {
        b.Entity<PrimarySaleFact>(e =>
        {
            e.ToTable("PrimarySaleFacts", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
            e.Property(x => x.ItemCode).HasMaxLength(60);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);

            foreach (var q in new[]
            {
                nameof(PrimarySaleFact.Quantity), nameof(PrimarySaleFact.BaseQuantity),
                nameof(PrimarySaleFact.Value), nameof(PrimarySaleFact.FreeQuantity),
                nameof(PrimarySaleFact.ReturnQuantity), nameof(PrimarySaleFact.ReturnValue),
            })
                e.Property(q).HasPrecision(18, 4);

            e.HasIndex(x => new { x.PartnerId, x.PeriodStart, x.ItemId })
                .IsUnique().HasDatabaseName("IX_PrimaryFact_Partner_Period_Item");
        });

        b.Entity<SecondarySale>(e =>
        {
            e.ToTable("SecondarySales", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.DocumentNumber).IsRequired().HasMaxLength(60);
            e.Property(x => x.OutletNameRaw).HasMaxLength(250);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.MappingNote).HasMaxLength(500);

            foreach (var money in new[]
            {
                nameof(SecondarySale.SubTotal), nameof(SecondarySale.DiscountAmount),
                nameof(SecondarySale.SchemeAmount), nameof(SecondarySale.TaxAmount),
                nameof(SecondarySale.TotalAmount), nameof(SecondarySale.ReturnAmount),
                nameof(SecondarySale.TotalQuantity),
            })
                e.Property(money).HasPrecision(18, 4);

            e.HasIndex(x => new { x.PartnerId, x.SaleDate }).HasDatabaseName("IX_Secondary_Partner_Date");
            e.HasIndex(x => new { x.OutletId, x.SaleDate }).HasDatabaseName("IX_Secondary_Outlet_Date");
            // The exception queue: rows whose outlet or items did not resolve.
            e.HasIndex(x => new { x.CompanyId, x.IsMapped }).HasDatabaseName("IX_Secondary_Mapped");

            e.HasOne(x => x.Partner).WithMany()
                .HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Outlet).WithMany()
                .HasForeignKey(x => x.OutletId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Lines).WithOne(l => l.SecondarySale)
                .HasForeignKey(l => l.SecondarySaleId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<SecondarySaleLine>(e =>
        {
            e.ToTable("SecondarySaleLines", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).HasMaxLength(200);
            e.Property(x => x.ItemCodeRaw).HasMaxLength(120);
            e.Property(x => x.BatchNumber).HasMaxLength(60);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);
            e.Property(x => x.MappingNote).HasMaxLength(500);

            foreach (var q in new[]
            {
                nameof(SecondarySaleLine.UomFactor), nameof(SecondarySaleLine.Quantity),
                nameof(SecondarySaleLine.BaseQuantity), nameof(SecondarySaleLine.FreeQuantity),
                nameof(SecondarySaleLine.UnitPrice), nameof(SecondarySaleLine.DiscountAmount),
                nameof(SecondarySaleLine.SchemeAmount), nameof(SecondarySaleLine.TaxAmount),
                nameof(SecondarySaleLine.LineTotal),
            })
                e.Property(q).HasPrecision(18, 4);

            e.HasIndex(x => x.ItemId).HasDatabaseName("IX_SecondaryLine_Item");
        });

        b.Entity<SecondaryUploadBatch>(e =>
        {
            e.ToTable("SecondaryUploads", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.BatchNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.FileName).HasMaxLength(250);
            e.Property(x => x.FileUrl).HasMaxLength(500);
            e.Property(x => x.RejectionReason).HasMaxLength(1000);
            e.Property(x => x.ValidationSummary).HasMaxLength(4000);
            e.Property(x => x.TotalValue).HasPrecision(18, 4);
            e.Property(x => x.MappedValue).HasPrecision(18, 4);
            e.Property(x => x.MappingAccuracyPercent).HasPrecision(9, 4);
            e.HasIndex(x => new { x.PartnerId, x.PeriodStart }).HasDatabaseName("IX_Upload_Partner_Period");
        });

        b.Entity<SecondaryMappingProfile>(e =>
        {
            e.ToTable("MappingProfiles", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.FileFormat).IsRequired().HasMaxLength(20);
            e.Property(x => x.OutletColumn).HasMaxLength(120);
            e.Property(x => x.ItemColumn).HasMaxLength(120);
            e.Property(x => x.QuantityColumn).HasMaxLength(120);
            e.Property(x => x.ValueColumn).HasMaxLength(120);
            e.Property(x => x.DateColumn).HasMaxLength(120);
            e.Property(x => x.UomColumn).HasMaxLength(120);
            e.Property(x => x.BatchColumn).HasMaxLength(120);
            e.Property(x => x.InvoiceColumn).HasMaxLength(120);
            e.Property(x => x.DateFormat).HasMaxLength(40);
            e.Property(x => x.ItemCodeMap).HasColumnType("jsonb");
            e.Property(x => x.OutletCodeMap).HasColumnType("jsonb");
            e.Property(x => x.UomMap).HasColumnType("jsonb");
            e.HasIndex(x => x.PartnerId).HasDatabaseName("IX_MappingProfile_Partner");
        });

        b.Entity<DistributorStockDeclaration>(e =>
        {
            e.ToTable("StockDeclarations", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.DeclarationNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.Note).HasMaxLength(1000);

            foreach (var money in new[]
            {
                nameof(DistributorStockDeclaration.TotalValue), nameof(DistributorStockDeclaration.NearExpiryValue),
                nameof(DistributorStockDeclaration.ExpiredValue), nameof(DistributorStockDeclaration.DamagedValue),
                nameof(DistributorStockDeclaration.DaysOfCover),
            })
                e.Property(money).HasPrecision(18, 4);

            e.HasIndex(x => new { x.PartnerId, x.AsOfDate })
                .IsUnique().HasDatabaseName("IX_StockDeclaration_Partner_AsOf");
            e.HasMany(x => x.Lines).WithOne(l => l.Declaration)
                .HasForeignKey(l => l.DeclarationId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<DistributorStockLine>(e =>
        {
            e.ToTable("StockDeclarationLines", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).HasMaxLength(200);
            e.Property(x => x.ItemCodeRaw).HasMaxLength(120);
            e.Property(x => x.BatchNumber).HasMaxLength(60);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);

            foreach (var q in new[]
            {
                nameof(DistributorStockLine.UomFactor), nameof(DistributorStockLine.Quantity),
                nameof(DistributorStockLine.BaseQuantity), nameof(DistributorStockLine.UnitValue),
                nameof(DistributorStockLine.TotalValue), nameof(DistributorStockLine.Age0To30),
                nameof(DistributorStockLine.Age31To60), nameof(DistributorStockLine.Age61To90),
                nameof(DistributorStockLine.Age90Plus), nameof(DistributorStockLine.NearExpiryQuantity),
                nameof(DistributorStockLine.ExpiredQuantity), nameof(DistributorStockLine.DamagedQuantity),
                nameof(DistributorStockLine.DaysOfCover),
            })
                e.Property(q).HasPrecision(18, 4);

            e.HasIndex(x => new { x.DeclarationId, x.ItemId }).HasDatabaseName("IX_StockDeclLine_Decl_Item");
        });

        b.Entity<StockNorm>(e =>
        {
            e.ToTable("StockNorms", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);

            foreach (var q in new[]
            {
                nameof(StockNorm.TargetDaysOfCover), nameof(StockNorm.MinQuantity),
                nameof(StockNorm.MaxQuantity), nameof(StockNorm.ReorderQuantity),
                nameof(StockNorm.CurrentQuantity), nameof(StockNorm.CurrentDaysOfCover),
            })
                e.Property(q).HasPrecision(18, 4);

            e.HasIndex(x => new { x.PartnerId, x.ItemId })
                .IsUnique().HasDatabaseName("IX_StockNorm_Partner_Item");
        });

        b.Entity<SellInSellOutReconciliation>(e =>
        {
            e.ToTable("Reconciliations", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).HasMaxLength(200);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);
            e.Property(x => x.ExplanationNote).HasMaxLength(1000);

            foreach (var q in new[]
            {
                nameof(SellInSellOutReconciliation.OpeningQuantity), nameof(SellInSellOutReconciliation.PrimaryQuantity),
                nameof(SellInSellOutReconciliation.SecondaryQuantity), nameof(SellInSellOutReconciliation.ReturnQuantity),
                nameof(SellInSellOutReconciliation.DeclaredClosingQuantity),
                nameof(SellInSellOutReconciliation.ComputedClosingQuantity),
                nameof(SellInSellOutReconciliation.VarianceQuantity), nameof(SellInSellOutReconciliation.VarianceValue),
            })
                e.Property(q).HasPrecision(18, 4);

            e.Property(x => x.VariancePercent).HasPrecision(9, 4);

            e.HasIndex(x => new { x.PartnerId, x.PeriodStart, x.ItemId })
                .HasDatabaseName("IX_Reconciliation_Partner_Period_Item");
            e.HasIndex(x => new { x.CompanyId, x.Outcome, x.IsExplained })
                .HasDatabaseName("IX_Reconciliation_Exceptions");
        });
    }

    // ═══ Performance ═════════════════════════════════════════════════════════

    private static void ConfigurePerformance(ModelBuilder b)
    {
        b.Entity<SalesTarget>(e =>
        {
            e.ToTable("Targets", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.Note).HasMaxLength(1000);

            foreach (var v in new[]
            {
                nameof(SalesTarget.TargetValue), nameof(SalesTarget.AchievedValue),
                nameof(SalesTarget.ProRataTarget), nameof(SalesTarget.ProjectedValue),
                nameof(SalesTarget.LastPeriodValue), nameof(SalesTarget.SamePeriodLastYearValue),
            })
                e.Property(v).HasPrecision(18, 4);

            e.Property(x => x.AchievementPercent).HasPrecision(9, 4);
            e.HasIndex(x => new { x.CompanyId, x.Scope, x.PeriodStart }).HasDatabaseName("IX_Target_Scope_Period");
            e.HasIndex(x => new { x.FieldRepId, x.PeriodStart }).HasDatabaseName("IX_Target_Rep_Period");
            e.HasMany(x => x.Lines).WithOne(l => l.Target)
                .HasForeignKey(l => l.TargetId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<TargetLine>(e =>
        {
            e.ToTable("TargetLines", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).HasMaxLength(200);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);
            e.Property(x => x.TargetValue).HasPrecision(18, 4);
            e.Property(x => x.AchievedValue).HasPrecision(18, 4);
            e.Property(x => x.AchievementPercent).HasPrecision(9, 4);
        });

        b.Entity<IncentiveScheme>(e =>
        {
            e.ToTable("IncentiveSchemes", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.ApplicableRoles).HasMaxLength(60);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.Terms).HasMaxLength(4000);
            e.Property(x => x.GateThresholdPercent).HasPrecision(9, 4);
            e.Property(x => x.MinimumAchievementPercent).HasPrecision(9, 4);
            e.Property(x => x.LinearRatePercent).HasPrecision(9, 4);
            e.Property(x => x.MaxPayout).HasPrecision(18, 4);
            e.Property(x => x.SpiffRatePerUnit).HasPrecision(18, 4);
            e.HasMany(x => x.Slabs).WithOne(s => s.Scheme)
                .HasForeignKey(s => s.SchemeId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<IncentiveSlab>(e =>
        {
            e.ToTable("IncentiveSlabs", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Label).HasMaxLength(200);
            e.Property(x => x.FromAchievementPercent).HasPrecision(9, 4);
            e.Property(x => x.ToAchievementPercent).HasPrecision(9, 4);
            e.Property(x => x.PayoutAmount).HasPrecision(18, 4);
            e.Property(x => x.PayoutPercent).HasPrecision(9, 4);
        });

        b.Entity<IncentivePayout>(e =>
        {
            e.ToTable("IncentivePayouts", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.GateFailureReason).HasMaxLength(500);
            e.Property(x => x.PayrollReference).HasMaxLength(120);
            e.Property(x => x.Note).HasMaxLength(1000);

            foreach (var v in new[]
            {
                nameof(IncentivePayout.TargetValue), nameof(IncentivePayout.AchievedValue),
                nameof(IncentivePayout.PayoutAmount), nameof(IncentivePayout.SpiffAmount),
                nameof(IncentivePayout.DeductionAmount), nameof(IncentivePayout.NetPayout),
            })
                e.Property(v).HasPrecision(18, 4);

            e.Property(x => x.AchievementPercent).HasPrecision(9, 4);
            e.HasIndex(x => new { x.FieldRepId, x.PeriodStart }).HasDatabaseName("IX_Payout_Rep_Period");
            e.HasOne(x => x.FieldRep).WithMany()
                .HasForeignKey(x => x.FieldRepId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<KpiSnapshot>(e =>
        {
            e.ToTable("KpiSnapshots", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);

            foreach (var pct in new[]
            {
                nameof(KpiSnapshot.CoveragePercent), nameof(KpiSnapshot.StrikeRatePercent),
                nameof(KpiSnapshot.LinesPerCall), nameof(KpiSnapshot.MustSellCompliancePercent),
                nameof(KpiSnapshot.RangeSellingPercent), nameof(KpiSnapshot.AverageTimePerCallMinutes),
                nameof(KpiSnapshot.CollectionEfficiencyPercent),
            })
                e.Property(pct).HasPrecision(9, 4);

            foreach (var money in new[]
            {
                nameof(KpiSnapshot.AverageBillValue), nameof(KpiSnapshot.DropSize),
                nameof(KpiSnapshot.SalesValue), nameof(KpiSnapshot.CollectionValue),
                nameof(KpiSnapshot.ReturnValue), nameof(KpiSnapshot.OverdueAmount),
            })
                e.Property(money).HasPrecision(18, 4);

            e.Property(x => x.DistanceCoveredKm).HasPrecision(10, 3);

            e.HasIndex(x => new { x.CompanyId, x.SnapshotDate, x.Scope })
                .HasDatabaseName("IX_Kpi_Date_Scope");
            e.HasIndex(x => new { x.FieldRepId, x.SnapshotDate }).HasDatabaseName("IX_Kpi_Rep_Date");
        });

        b.Entity<DemandForecast>(e =>
        {
            e.ToTable("Forecasts", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.SeasonalityFactor).HasPrecision(9, 4);
            e.Property(x => x.TrendFactor).HasPrecision(9, 4);
            e.Property(x => x.TotalForecastQuantity).HasPrecision(18, 4);
            e.Property(x => x.TotalForecastValue).HasPrecision(18, 4);
            e.Property(x => x.TotalActualQuantity).HasPrecision(18, 4);
            e.Property(x => x.AccuracyPercent).HasPrecision(9, 4);
            e.HasIndex(x => new { x.CompanyId, x.PeriodStart }).HasDatabaseName("IX_Forecast_Period");
            e.HasMany(x => x.Lines).WithOne(l => l.Forecast)
                .HasForeignKey(l => l.ForecastId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ForecastLine>(e =>
        {
            e.ToTable("ForecastLines", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);
            e.Property(x => x.OverrideReason).HasMaxLength(500);

            foreach (var q in new[]
            {
                nameof(ForecastLine.HistoricAverage), nameof(ForecastLine.ComputedQuantity),
                nameof(ForecastLine.OverrideQuantity), nameof(ForecastLine.FinalQuantity),
                nameof(ForecastLine.ForecastValue), nameof(ForecastLine.ActualQuantity),
                nameof(ForecastLine.DemandStdDeviation),
            })
                e.Property(q).HasPrecision(18, 4);

            e.Property(x => x.AccuracyPercent).HasPrecision(9, 4);
        });

        b.Entity<ReplenishmentSuggestion>(e =>
        {
            e.ToTable("ReplenishmentSuggestions", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);
            e.Property(x => x.DismissReason).HasMaxLength(500);

            foreach (var q in new[]
            {
                nameof(ReplenishmentSuggestion.CurrentStock), nameof(ReplenishmentSuggestion.InTransitStock),
                nameof(ReplenishmentSuggestion.ReorderPoint), nameof(ReplenishmentSuggestion.SafetyStock),
                nameof(ReplenishmentSuggestion.TargetStock), nameof(ReplenishmentSuggestion.SuggestedQuantity),
                nameof(ReplenishmentSuggestion.AverageDailyDemand), nameof(ReplenishmentSuggestion.DaysOfCover),
                nameof(ReplenishmentSuggestion.EstimatedValue),
            })
                e.Property(q).HasPrecision(18, 4);

            e.HasIndex(x => new { x.CompanyId, x.IsActioned, x.UrgencyScore })
                .HasDatabaseName("IX_Replenishment_Queue");
        });

        b.Entity<StockTransferRequest>(e =>
        {
            e.ToTable("TransferRequests", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.TransferNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.RejectionReason).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.TotalQuantity).HasPrecision(18, 4);
            e.Property(x => x.TotalValue).HasPrecision(18, 4);
            e.HasIndex(x => new { x.CompanyId, x.TransferNumber })
                .IsUnique().HasDatabaseName("IX_Transfer_Number");
        });
    }

    // ═══ Compliance ══════════════════════════════════════════════════════════

    private static void ConfigureCompliance(ModelBuilder b)
    {
        b.Entity<ColdChainCheckpoint>(e =>
        {
            e.ToTable("ColdChainCheckpoints", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Location).HasMaxLength(200);
            e.Property(x => x.SensorIdentifier).HasMaxLength(120);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.MinSafeCelsius).HasPrecision(6, 2);
            e.Property(x => x.MaxSafeCelsius).HasPrecision(6, 2);
            e.Property(x => x.LastReadingCelsius).HasPrecision(6, 2);
            e.HasIndex(x => new { x.CompanyId, x.IsInBreach }).HasDatabaseName("IX_ColdChain_Breach");
            e.HasMany(x => x.Logs).WithOne(l => l.Checkpoint)
                .HasForeignKey(l => l.CheckpointId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ColdChainLog>(e =>
        {
            e.ToTable("ColdChainLogs", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.RecordedByName).HasMaxLength(200);
            e.Property(x => x.CorrectiveAction).HasMaxLength(1000);
            e.Property(x => x.PhotoUrl).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.ReadingCelsius).HasPrecision(6, 2);
            e.Property(x => x.AffectedStockValue).HasPrecision(18, 4);
            e.HasIndex(x => new { x.CheckpointId, x.RecordedAt }).HasDatabaseName("IX_ColdChainLog_Point_At");
            // Unresolved excursions: what an inspector asks to see.
            e.HasIndex(x => new { x.CompanyId, x.IsOutOfRange, x.IsResolved })
                .HasDatabaseName("IX_ColdChainLog_Unresolved");
        });

        b.Entity<BatchTraceLink>(e =>
        {
            e.ToTable("BatchTraceLinks", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
            e.Property(x => x.BatchNumber).IsRequired().HasMaxLength(60);
            e.Property(x => x.SupplierLotReference).HasMaxLength(120);
            e.Property(x => x.OutletName).HasMaxLength(250);
            e.Property(x => x.DocumentNumber).HasMaxLength(60);
            e.Property(x => x.MovementType).IsRequired().HasMaxLength(40);
            e.Property(x => x.Uom).IsRequired().HasMaxLength(20);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.Property(x => x.Value).HasPrecision(18, 4);

            // Forward trace: batch → every outlet that received it. One indexed scan during a recall.
            e.HasIndex(x => new { x.CompanyId, x.BatchNumber, x.MovedAt })
                .HasDatabaseName("IX_Trace_Batch_At");
            // Backward trace: outlet complaint → the batch and the supplier receipt behind it.
            e.HasIndex(x => new { x.OutletId, x.MovedAt }).HasDatabaseName("IX_Trace_Outlet_At");
            e.HasIndex(x => new { x.ItemId, x.BatchId }).HasDatabaseName("IX_Trace_Item_Batch");
        });

        b.Entity<ProductRecall>(e =>
        {
            e.ToTable("Recalls", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.RecallNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.Title).IsRequired().HasMaxLength(250);
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
            e.Property(x => x.BatchNumber).HasMaxLength(60);
            e.Property(x => x.Reason).HasMaxLength(2000);
            e.Property(x => x.RegulatoryReference).HasMaxLength(120);
            e.Property(x => x.PublicNotice).HasMaxLength(4000);
            e.Property(x => x.ClosureReport).HasMaxLength(4000);
            e.Property(x => x.Note).HasMaxLength(1000);

            foreach (var q in new[]
            {
                nameof(ProductRecall.DespatchedQuantity), nameof(ProductRecall.RecoveredQuantity),
                nameof(ProductRecall.DestroyedQuantity), nameof(ProductRecall.EstimatedValue),
                nameof(ProductRecall.RecoveredValue),
            })
                e.Property(q).HasPrecision(18, 4);

            e.Property(x => x.RecoveryPercent).HasPrecision(9, 4);
            e.HasIndex(x => new { x.CompanyId, x.RecallNumber }).IsUnique().HasDatabaseName("IX_Recall_Number");
            e.HasMany(x => x.Notices).WithOne(n => n.Recall)
                .HasForeignKey(n => n.RecallId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<RecallOutletNotice>(e =>
        {
            e.ToTable("RecallNotices", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.DestinationName).HasMaxLength(250);
            e.Property(x => x.NotificationChannel).HasMaxLength(40);
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.SuppliedQuantity).HasPrecision(18, 4);
            e.Property(x => x.ReturnedQuantity).HasPrecision(18, 4);
            e.Property(x => x.Value).HasPrecision(18, 4);
            e.HasIndex(x => new { x.RecallId, x.IsClosed }).HasDatabaseName("IX_RecallNotice_Recall_Closed");
            e.HasOne(x => x.Outlet).WithMany()
                .HasForeignKey(x => x.OutletId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<ReasonCode>(e =>
        {
            e.ToTable("ReasonCodes", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.ColorHex).HasMaxLength(9);
            e.Property(x => x.IconName).HasMaxLength(60);
            e.HasIndex(x => new { x.CompanyId, x.Surface, x.DisplayOrder })
                .HasDatabaseName("IX_ReasonCode_Surface_Order");
        });

        b.Entity<DistributionSettings>(e =>
        {
            e.ToTable("Settings", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.BaseCurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.InvoiceFooter).HasMaxLength(1000);

            foreach (var money in new[]
            {
                nameof(DistributionSettings.MinimumOrderValue),
                nameof(DistributionSettings.DiscountApprovalThreshold),
                nameof(DistributionSettings.ChequeBounceCharge),
                nameof(DistributionSettings.CashVarianceTolerance),
                nameof(DistributionSettings.VarianceApprovalThreshold),
            })
                e.Property(money).HasPrecision(18, 4);

            foreach (var pct in new[]
            {
                nameof(DistributionSettings.MarginFloorPercent),
                nameof(DistributionSettings.MinimumShelfLifePercentOnDespatch),
                nameof(DistributionSettings.StockVarianceTolerancePercent),
                nameof(DistributionSettings.MinimumMappingAccuracyPercent),
                nameof(DistributionSettings.ReconciliationTolerancePercent),
            })
                e.Property(pct).HasPrecision(9, 4);

            // One row per tenant — a duplicate would give half the branches different rules.
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId })
                .IsUnique().HasDatabaseName("IX_Settings_Tenant");
        });

        b.Entity<DistributionNotification>(e =>
        {
            e.ToTable("Notifications", DefaultSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired().HasMaxLength(250);
            e.Property(x => x.Body).HasMaxLength(2000);
            e.Property(x => x.ReferenceType).HasMaxLength(60);
            e.Property(x => x.ActionRoute).HasMaxLength(250);
            e.Property(x => x.DeliveredChannels).HasMaxLength(120);
            e.Property(x => x.DeliveryError).HasMaxLength(1000);
            e.HasIndex(x => new { x.TargetUserId, x.ReadAt }).HasDatabaseName("IX_Notification_User_Read");
            e.HasIndex(x => new { x.CompanyId, x.RaisedAt }).HasDatabaseName("IX_Notification_At");
        });
    }
}
