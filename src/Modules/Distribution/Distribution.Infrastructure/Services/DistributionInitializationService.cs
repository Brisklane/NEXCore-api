using Distribution.Domain.Entities;
using Distribution.Domain.Enums;
using Distribution.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Distribution.Infrastructure.Services;

/// <summary>
/// Stands a company's Distribution app up as something that works on first open.
///
/// Two tiers, and the distinction matters:
///
/// **Essential** is the configuration without which the app cannot function at all. A visit
/// cannot be closed without a no-order reason, a settlement cannot close without variance
/// reasons, and every threshold the field terminal enforces comes from the settings row. Every
/// company gets these, always. An app that greets a new customer with a blank screen and a save
/// button that silently fails is broken, not minimal.
///
/// **Sample** is the demonstration network — a distributor, a territory, a beat with outlets, a
/// rep, a van, a price list and a live scheme. Only companies that asked for sample data get it,
/// because a real distributor wants their own retail universe, not somebody else's.
///
/// Everything is idempotent and runs per tier, so a company that skipped samples at sign-up can
/// ask for them later, and one that already has a partner is never given a second one.
/// </summary>
public class DistributionInitializationService(
    DistributionDbContext db,
    ILogger<DistributionInitializationService> logger)
{
    /// <summary>Called by the CompanyCreated handler. Provisions essentials, and samples if asked.</summary>
    public Task<bool> InitializeForNewCompanyAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, bool includeSampleData)
        => EnsureProvisionedAsync(companyId, branchId, businessUnitId, userId, includeSampleData);

    /// <summary>
    /// Brings a company's Distribution app up to a working state, whenever it is called.
    ///
    /// Installing an app is not the same event as creating a company: a business that has run
    /// NexCore for a year and adds Distribution today never saw <c>CompanyCreatedEvent</c>, and
    /// would otherwise land on a field terminal with no reason codes behind it — where check-out
    /// refuses every visit and nobody can say why.
    /// </summary>
    public async Task<bool> EnsureProvisionedAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, bool includeSampleData)
    {
        var tenant = new FixedDistributionTenant(companyId, branchId, businessUnitId, userId);

        var hasSettings = await db.Settings
            .AnyAsync(s => s.CompanyId == companyId && s.BranchId == branchId
                           && s.BusinessUnitId == businessUnitId && !s.IsDeleted);

        var hasReasons = await db.ReasonCodes.AnyAsync(r => r.CompanyId == companyId && !r.IsDeleted);

        var provisionedEssentials = hasSettings && hasReasons;

        if (!hasSettings) SeedSettings(tenant);
        if (!hasReasons) SeedReasonCodes(tenant);

        if (!provisionedEssentials) await db.SaveChangesAsync();

        // Sample data on a network that already has a partner would duplicate it, so it is only
        // laid down on a company that is still empty.
        var wantsSample = includeSampleData
            && !await db.Partners.AnyAsync(p => p.CompanyId == companyId && !p.IsDeleted);

        if (wantsSample)
        {
            await SeedSampleNetworkAsync(tenant);
            logger.LogInformation("Distribution sample network seeded for company {CompanyId}", companyId);
        }

        return !provisionedEssentials || wantsSample;
    }

    // ═══ Essentials ══════════════════════════════════════════════════════════

    private void SeedSettings(FixedDistributionTenant tenant)
        => db.Settings.Add(new DistributionSettings().StampNew(tenant));

    /// <summary>
    /// The reason registry. Every surface that demands an explanation needs codes on day one, or
    /// the first visit of the first day cannot be closed.
    /// </summary>
    private void SeedReasonCodes(FixedDistributionTenant tenant)
    {
        void Add(ReasonSurface surface, string name, int order,
            bool note = false, bool approval = false, bool recoverable = false, bool negative = false)
            => db.ReasonCodes.Add(new ReasonCode
            {
                Surface = surface,
                Name = name,
                DisplayOrder = order,
                RequiresNote = note,
                RequiresApproval = approval,
                IsRecoverable = recoverable,
                IsNegative = negative,
                IsSystem = true,
            }.StampNew(tenant));

        // Why a call produced nothing — the denominator of strike rate, so it has to be honest.
        Add(ReasonSurface.NoOrder, "Shop closed", 1);
        Add(ReasonSurface.NoOrder, "Owner not available", 2);
        Add(ReasonSurface.NoOrder, "Stock sufficient", 3);
        Add(ReasonSurface.NoOrder, "Credit blocked", 4, negative: true);
        Add(ReasonSurface.NoOrder, "Competitor scheme", 5, note: true, negative: true);
        Add(ReasonSurface.NoOrder, "No demand", 6);
        Add(ReasonSurface.NoOrder, "Price objection", 7, note: true);
        Add(ReasonSurface.NoOrder, "Pending complaint", 8, note: true, negative: true);
        Add(ReasonSurface.NoOrder, "Other", 99, note: true);

        Add(ReasonSurface.VisitSkipped, "Market holiday", 1);
        Add(ReasonSurface.VisitSkipped, "Outlet weekly off", 2);
        Add(ReasonSurface.VisitSkipped, "Ran out of time", 3, negative: true);
        Add(ReasonSurface.VisitSkipped, "Other", 99, note: true);

        Add(ReasonSurface.OutOfFenceCheckIn, "Outlet has moved", 1, note: true);
        Add(ReasonSurface.OutOfFenceCheckIn, "GPS inaccurate", 2);
        Add(ReasonSurface.OutOfFenceCheckIn, "Met owner elsewhere", 3, note: true);
        Add(ReasonSurface.OutOfFenceCheckIn, "Coordinates never captured", 4);

        Add(ReasonSurface.OrderCancellation, "Customer cancelled", 1);
        Add(ReasonSurface.OrderCancellation, "Duplicate order", 2);
        Add(ReasonSurface.OrderCancellation, "Stock unavailable", 3);
        Add(ReasonSurface.OrderCancellation, "Credit not cleared", 4, negative: true);
        Add(ReasonSurface.OrderCancellation, "Priced incorrectly", 5, note: true);

        Add(ReasonSurface.OrderRejection, "Above credit limit", 1, negative: true);
        Add(ReasonSurface.OrderRejection, "Below minimum margin", 2, negative: true);
        Add(ReasonSurface.OrderRejection, "Unauthorised product", 3);
        Add(ReasonSurface.OrderRejection, "Other", 99, note: true);

        Add(ReasonSurface.ShortPick, "Out of stock", 1);
        Add(ReasonSurface.ShortPick, "Damaged in the bin", 2, negative: true);
        Add(ReasonSurface.ShortPick, "Below minimum shelf life", 3);
        Add(ReasonSurface.ShortPick, "Not found in location", 4, note: true, negative: true);

        Add(ReasonSurface.DeliveryFailure, "Shop closed", 1);
        Add(ReasonSurface.DeliveryFailure, "Payment refused", 2, negative: true);
        Add(ReasonSurface.DeliveryFailure, "Goods refused", 3, note: true, negative: true);
        Add(ReasonSurface.DeliveryFailure, "Wrong address", 4, note: true);
        Add(ReasonSurface.DeliveryFailure, "Customer absent", 5);
        Add(ReasonSurface.DeliveryFailure, "Vehicle breakdown", 6, note: true, negative: true);

        Add(ReasonSurface.Return, "Damaged in transit", 1, recoverable: true);
        Add(ReasonSurface.Return, "Expired", 2);
        Add(ReasonSurface.Return, "Near expiry", 3);
        Add(ReasonSurface.Return, "Wrong item supplied", 4, negative: true);
        Add(ReasonSurface.Return, "Quality complaint", 5, note: true, negative: true);
        Add(ReasonSurface.Return, "Not selling", 6);
        Add(ReasonSurface.Return, "Recall", 7);

        // The settlement gate depends on these existing.
        Add(ReasonSurface.StockVariance, "Miscount", 1);
        Add(ReasonSurface.StockVariance, "Damaged on the van", 2, recoverable: true, negative: true);
        Add(ReasonSurface.StockVariance, "Given as free sample", 3, note: true);
        Add(ReasonSurface.StockVariance, "Theft", 4, note: true, approval: true, recoverable: true, negative: true);
        Add(ReasonSurface.StockVariance, "Not loaded despite the sheet", 5, note: true, negative: true);
        Add(ReasonSurface.StockVariance, "Returned but not recorded", 6, note: true);

        Add(ReasonSurface.CashVariance, "Change rounding", 1);
        Add(ReasonSurface.CashVariance, "Unrecorded expense", 2, note: true);
        Add(ReasonSurface.CashVariance, "Collection not entered", 3, note: true, negative: true);
        Add(ReasonSurface.CashVariance, "Shortfall to recover", 4, note: true, approval: true, recoverable: true, negative: true);

        Add(ReasonSurface.CreditOverride, "Long-standing customer", 1, note: true);
        Add(ReasonSurface.CreditOverride, "Cheque already deposited", 2, note: true);
        Add(ReasonSurface.CreditOverride, "Festival stocking", 3);
        Add(ReasonSurface.CreditOverride, "Management approved", 4, note: true, approval: true);

        Add(ReasonSurface.FefoOverride, "Nominated batch not found", 1, note: true);
        Add(ReasonSurface.FefoOverride, "Customer requested a later date", 2, note: true);
        Add(ReasonSurface.FefoOverride, "Nominated batch damaged", 3, negative: true);

        Add(ReasonSurface.ClaimRejection, "No supporting document", 1);
        Add(ReasonSurface.ClaimRejection, "Outside the scheme period", 2);
        Add(ReasonSurface.ClaimRejection, "Already claimed", 3);
        Add(ReasonSurface.ClaimRejection, "Quantity does not reconcile", 4, note: true);
        Add(ReasonSurface.ClaimRejection, "Outlet not eligible", 5);

        Add(ReasonSurface.PriceOverride, "Competitive match", 1, note: true, approval: true);
        Add(ReasonSurface.PriceOverride, "Damaged stock discount", 2, note: true);
        Add(ReasonSurface.PriceOverride, "Contract price", 3);

        Add(ReasonSurface.Wastage, "Expired", 1);
        Add(ReasonSurface.Wastage, "Damaged", 2, recoverable: true);
        Add(ReasonSurface.Wastage, "Cold chain breach", 3, note: true, negative: true);
    }

    // ═══ Sample network ══════════════════════════════════════════════════════

    /// <summary>
    /// A worked example of a distribution business: one distributor, one territory, one beat with
    /// four outlets, a rep with a van, a price list and a live buy-10-get-1 scheme. Enough that
    /// every screen has something real on it the first time it is opened.
    /// </summary>
    private async Task SeedSampleNetworkAsync(FixedDistributionTenant tenant)
    {
        var region = new GeoNode
        {
            Code = "GEO-00001",
            Name = "Central Region",
            LevelName = "Region",
            Depth = 0,
        }.StampNew(tenant);

        var city = new GeoNode
        {
            Code = "GEO-00002",
            Name = "Riverside",
            LevelName = "City",
            Depth = 1,
            ParentId = region.Id,
            Path = region.Id.ToString(),
        }.StampNew(tenant);

        db.GeoNodes.AddRange(region, city);

        var territory = new DistributionTerritory
        {
            Code = "TER-00001",
            Name = "Riverside Central",
            GeoNodeId = city.Id,
            CurrencyCode = "USD",
            Description = "Demonstration territory covering the Riverside trading area.",
        }.StampNew(tenant);

        db.Territories.Add(territory);

        var partner = new ChannelPartner
        {
            Code = "DIST-00001",
            Name = "Riverside Trading Co.",
            TradeName = "Riverside Trading",
            PartnerType = PartnerType.Distributor,
            Status = PartnerStatus.Active,
            ServicingModel = ServicingModel.VanSales,
            ContactPerson = "Sample Contact",
            Phone = "+1 555 0100",
            City = "Riverside",
            CurrencyCode = "USD",
            CreditLimit = 50000,
            CreditDays = 30,
            CreditEnforcement = CreditEnforcement.Warn,
            MarginPercent = 12,
            TerritoryId = territory.Id,
            AppointedOn = DateTime.UtcNow.Date.AddYears(-1),
            SecondaryCaptureMode = SecondaryCaptureMode.Declared,
            Notes = "Sample distributor created with the demonstration data.",
        }.StampNew(tenant);

        db.Partners.Add(partner);

        var rep = new FieldRep
        {
            Code = "REP-00001",
            FullName = "Sample Field Rep",
            DisplayName = "Sample",
            Role = FieldRole.VanSalesman,
            Phone = "+1 555 0110",
            TerritoryId = territory.Id,
            PartnerId = partner.Id,
            CashHoldingLimit = 2000,
            DiscountAuthorityPercent = 5,
            JoinedOn = DateTime.UtcNow.Date.AddMonths(-6),
            // A PIN is set so the shared-device login can be demonstrated. It is hashed like any
            // other, and a real deployment changes it on first use.
            PinHash = BCrypt.Net.BCrypt.HashPassword("1234"),
        }.StampNew(tenant);

        db.FieldReps.Add(rep);

        var vehicle = new Vehicle
        {
            Code = "VEH-00001",
            RegistrationNumber = "SAMPLE-01",
            Name = "Demo Van",
            Kind = VehicleKind.Van,
            Ownership = VehicleOwnership.Owned,
            CapacityWeightKg = 1200,
            CapacityVolumeM3 = 8,
        }.StampNew(tenant);

        db.Vehicles.Add(vehicle);

        db.VehicleCompliances.Add(new VehicleCompliance
        {
            VehicleId = vehicle.Id,
            Kind = VehicleComplianceKind.Insurance,
            DocumentNumber = "INS-SAMPLE-01",
            IssuedOn = DateTime.UtcNow.Date.AddMonths(-6),
            ExpiresOn = DateTime.UtcNow.Date.AddMonths(6),
        }.StampNew(tenant));

        var driver = new Driver
        {
            Code = "DRV-00001",
            FullName = "Sample Driver",
            Phone = "+1 555 0120",
            LicenceNumber = "LIC-SAMPLE-01",
            LicenceExpiresOn = DateTime.UtcNow.Date.AddYears(2),
            DefaultVehicleId = vehicle.Id,
        }.StampNew(tenant);

        db.Drivers.Add(driver);

        var van = new VanUnit
        {
            Code = "VAN-00001",
            Name = "Riverside Van 1",
            VehicleId = vehicle.Id,
            FieldRepId = rep.Id,
            CapacityWeightKg = 1200,
            CapacityVolumeM3 = 8,
        }.StampNew(tenant);

        db.VanUnits.Add(van);

        var route = new SalesRoute
        {
            Code = "RTE-00001",
            Name = "Riverside Monday Beat",
            Kind = RouteKind.VanSales,
            Frequency = VisitFrequency.Weekly,
            TerritoryId = territory.Id,
            PartnerId = partner.Id,
            FieldRepId = rep.Id,
            VehicleId = vehicle.Id,
            VanUnitId = van.Id,
            // Monday, in the weekday mask the journey planner reads.
            ActiveDays = "1",
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            TargetCallsPerDay = 20,
            MinimumProductiveCalls = 12,
            Description = "Demonstration beat with four sample outlets.",
        }.StampNew(tenant);

        db.Routes.Add(route);

        var outlets = new[]
        {
            ("OUT-00001", "Corner Store", OutletChannel.GeneralTrade, OutletGrade.A, 5000m),
            ("OUT-00002", "Riverside Supermart", OutletChannel.ModernTrade, OutletGrade.A, 15000m),
            ("OUT-00003", "Station Kiosk", OutletChannel.KioskPanShop, OutletGrade.C, 1200m),
            ("OUT-00004", "Bridge Pharmacy", OutletChannel.Pharmacy, OutletGrade.B, 4000m),
        };

        var sequence = 0;

        foreach (var (code, name, channel, grade, limit) in outlets)
        {
            var outlet = new RetailOutlet
            {
                Code = code,
                Name = name,
                OwnerName = $"{name} Owner",
                OwnerPhone = $"+1 555 02{sequence:D2}",
                Channel = channel,
                Grade = grade,
                Status = OutletStatus.Active,
                City = "Riverside",
                Area = "Central",
                CurrencyCode = "USD",
                CreditLimit = limit,
                CreditDays = 15,
                CreditEnforcement = CreditEnforcement.Warn,
                GeofenceRadiusMetres = 150,
                PartnerId = partner.Id,
                TerritoryId = territory.Id,
                GeoNodeId = city.Id,
                OnboardedAt = DateTime.UtcNow.Date.AddMonths(-3),
                IsApproved = true,
                ApprovedAt = DateTime.UtcNow,
            }.StampNew(tenant);

            db.Outlets.Add(outlet);

            db.RouteOutlets.Add(new RouteOutlet
            {
                RouteId = route.Id,
                OutletId = outlet.Id,
                StopSequence = ++sequence,
                ServiceMinutes = 12,
                IsMustVisit = grade == OutletGrade.A,
            }.StampNew(tenant));

            db.CreditProfiles.Add(new CreditProfile
            {
                OutletId = outlet.Id,
                CurrencyCode = "USD",
                CreditLimit = limit,
                CreditDays = 15,
                Enforcement = CreditEnforcement.Warn,
                AvailableCredit = limit,
                RecalculatedAt = DateTime.UtcNow,
            }.StampNew(tenant));
        }

        route.OutletCount = sequence;

        db.CreditProfiles.Add(new CreditProfile
        {
            PartnerId = partner.Id,
            CurrencyCode = "USD",
            CreditLimit = partner.CreditLimit,
            CreditDays = partner.CreditDays,
            Enforcement = partner.CreditEnforcement,
            AvailableCredit = partner.CreditLimit,
            RecalculatedAt = DateTime.UtcNow,
        }.StampNew(tenant));

        // A general-trade price list so the catalogue has something to resolve against. It is
        // approved, because an unapproved list is invisible to pricing and the demo would show
        // an empty order pad.
        var priceList = new ChannelPriceList
        {
            Code = "PRL-00001",
            Name = "General Trade — Standard",
            Scope = PriceScope.Channel,
            Channel = OutletChannel.GeneralTrade,
            CurrencyCode = "USD",
            EffectiveFrom = DateTime.UtcNow.Date.AddMonths(-1),
            Priority = 10,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow,
            Description = "Demonstration price list. Replace the lines with your own catalogue.",
        }.StampNew(tenant);

        db.PriceLists.Add(priceList);

        var scheme = new TradeScheme
        {
            SchemeNumber = $"SCH-{DateTime.UtcNow:yy}-0001",
            Name = "Buy 10 get 1 free — demonstration",
            Kind = TradeSchemeKind.QuantityFreeGoods,
            Status = SchemeStatus.Active,
            SettlementMode = SchemeSettlementMode.OnInvoice,
            Stacking = SchemeStacking.Combinable,
            ValidFrom = DateTime.UtcNow.Date.AddDays(-7),
            ValidTo = DateTime.UtcNow.Date.AddMonths(2),
            MinQuantity = 10,
            FreeQuantity = 1,
            IsRecurringPerBlock = true,
            CurrencyCode = "USD",
            BudgetAmount = 5000,
            StopWhenBudgetExhausted = true,
            ApprovedAt = DateTime.UtcNow,
            TermsAndConditions = "Sample scheme. Applies to every SKU until products are added to it.",
        }.StampNew(tenant);

        db.Schemes.Add(scheme);

        db.SchemeScopes.Add(new TradeSchemeScope
        {
            SchemeId = scheme.Id,
            Channel = OutletChannel.GeneralTrade,
        }.StampNew(tenant));

        db.SchemeBudgetLedger.Add(new SchemeBudgetLedger
        {
            SchemeId = scheme.Id,
            OccurredAt = DateTime.UtcNow,
            Amount = scheme.BudgetAmount,
            BalanceAfter = scheme.BudgetAmount,
            Reason = "Initial budget",
        }.StampNew(tenant));

        db.ColdChainCheckpoints.Add(new ColdChainCheckpoint
        {
            Code = "CCP-00001",
            Name = "Demo Reefer Van",
            Kind = ColdChainPointKind.ReeferVehicle,
            VehicleId = vehicle.Id,
            VanUnitId = van.Id,
            MinSafeCelsius = 2,
            MaxSafeCelsius = 8,
            CheckIntervalHours = 8,
            Location = "In transit",
        }.StampNew(tenant));

        await db.SaveChangesAsync();
    }
}

/// <summary>Stamps a new sample or essential row with the fixed seeding scope.</summary>
file static class SeedExtensions
{
    public static T StampNew<T>(this T entity, FixedDistributionTenant tenant)
        where T : Nexcore.SharedKernel.BaseEntity
    {
        entity.CompanyId = tenant.CompanyId;
        entity.BranchId = tenant.BranchId;
        entity.BusinessUnitId = tenant.BusinessUnitId;
        entity.CreatedAt = DateTime.UtcNow;
        entity.CreatedByUserId = tenant.UserId;
        return entity;
    }
}
