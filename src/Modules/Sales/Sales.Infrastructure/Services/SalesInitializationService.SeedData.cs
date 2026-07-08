using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Constants;
using Nexcore.SharedKernel.Helpers;
using Sales.Domain.Entities;
using Sales.Domain.Enums;
using BC = BCrypt.Net.BCrypt;

namespace Sales.Infrastructure.Services;

/// <summary>
/// Seed-data factory methods - all property names verified against actual domain entities.
/// Cross-module Guids use deterministic well-known values consistent with
/// Inventory / CRM / HR module seeders.
/// </summary>
public partial class SalesInitializationService
{
    // Tenant stamp helper

    private static T E<T>(T entity,
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) t)
        where T : BaseEntity
    {
        entity.CompanyId       = t.companyId;
        entity.BranchId        = t.branchId;
        entity.BusinessUnitId  = t.businessUnitId;
        entity.CreatedByUserId = t.userId;
        entity.CreatedAt       = DateTime.UtcNow;
        return entity;
    }

    // Well-known cross-module IDs
    private static readonly Guid ContactId1 = new("10000001-0000-0000-0000-000000000001");
    private static readonly Guid ContactId2 = new("10000001-0000-0000-0000-000000000002");
    private static readonly Guid ContactId3 = new("10000001-0000-0000-0000-000000000003");
    private static readonly Guid ContactId4 = new("10000001-0000-0000-0000-000000000004");

    private static readonly Guid ItemId1 = new("20000001-0000-0000-0000-000000000001"); // Coca-Cola 330ml
    private static readonly Guid ItemId2 = new("20000001-0000-0000-0000-000000000002"); // Lay's Chips
    private static readonly Guid ItemId3 = new("20000001-0000-0000-0000-000000000003"); // Samsung Galaxy A54
    private static readonly Guid ItemId4 = new("20000001-0000-0000-0000-000000000004"); // Nike Running Shoes
    private static readonly Guid ItemId5 = new("20000001-0000-0000-0000-000000000005"); // Whole Milk 1L

    private static readonly Guid EmpId1 = new("30000001-0000-0000-0000-000000000001");
    private static readonly Guid EmpId2 = new("30000001-0000-0000-0000-000000000002");
    private static readonly Guid EmpId3 = new("30000001-0000-0000-0000-000000000003");

    /// <summary>
    /// Per-company retail warehouse ID, shared with the Inventory module's RETAIL-WH seed
    /// via <see cref="CrossModuleIds"/>. POS stock deduction targets this warehouse, so the
    /// value MUST match what Inventory seeds. (Previously a hardcoded literal that did not
    /// match any Inventory warehouse, so POS sales never decremented real stock.)
    /// </summary>
    private static Guid RetailWarehouseId(Guid companyId) => CrossModuleIds.RetailWarehouseId(companyId);

    // 0. Document Sequences

    private static DocumentSequence[] SeedDocumentSequences(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T)
    {
        static DocumentSequence Seq(
            (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) t,
            DocumentType documentType,
            string prefix,
            string description,
            string separator       = "-",
            bool includeYear       = true,
            SequenceYearFormat yearFormat = SequenceYearFormat.Full,
            bool includeMonth      = false,
            int padding            = 5,
            SequenceResetPeriod resetOn = SequenceResetPeriod.Yearly)
            => E(new DocumentSequence
            {
                DocumentType       = documentType,
                Description        = description,
                Prefix             = prefix,
                Separator          = separator,
                IncludeYear        = includeYear,
                YearFormat         = yearFormat,
                IncludeMonth       = includeMonth,
                SequencePadding    = padding,
                ResetOn            = resetOn,
                NextSequenceNumber = 1,
                IsActive           = true,
            }, t);

        return
        [
            Seq(T, DocumentType.SalesOrder,    "SO",   "Sales Order numbers",           padding: 5),
            Seq(T, DocumentType.Quotation,     "QT",   "Quotation numbers",             padding: 5),
            Seq(T, DocumentType.Invoice,       "INV",  "Sales Invoice numbers",         padding: 5),
            Seq(T, DocumentType.CreditNote,    "CN",   "Credit Note numbers",           padding: 5),
            Seq(T, DocumentType.Payment,       "PAY",  "Payment receipt numbers",       padding: 5),
            Seq(T, DocumentType.Delivery,      "DLV",  "Delivery order numbers",        padding: 5),
            Seq(T, DocumentType.SalesReturn,   "RET",  "Sales Return numbers",          padding: 5),
            Seq(T, DocumentType.PosSession,      "POSS", "POS Session numbers",           padding: 5, resetOn: SequenceResetPeriod.Never),
            Seq(T, DocumentType.PosTransaction,  "POST", "POS Transaction numbers",       padding: 6, resetOn: SequenceResetPeriod.Never),
            Seq(T, DocumentType.RiderAssignment, "RA",   "Rider Assignment numbers",      padding: 5, resetOn: SequenceResetPeriod.Never),
        ];
    }

    // 1. Receipt Template

    private static PosReceiptTemplate[] SeedReceiptTemplates(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
        string? companyName = null)
    {
        // Real company name when available. Address/phone/email/website/logo/tax# are left blank
        // for the company to fill in via the receipt-template admin — no fake placeholders on
        // real customer-facing documents.
        var business = string.IsNullOrWhiteSpace(companyName) ? "My Store" : companyName;

        PosReceiptTemplate Make(string name, string paperSize, bool isDefault) =>
            E(new PosReceiptTemplate
            {
                TemplateName       = name,
                HeaderBusinessName = business,
                HeaderMessage      = "Welcome! Thank you for shopping with us.",
                FooterMessage      = "Thank you for your purchase! Visit again soon.",
                ReturnPolicy       = "Returns accepted within 30 days with receipt.",
                ShowBarcode        = true,
                ShowQrCode         = true,
                ShowCashierName    = true,
                ShowCustomerName   = true,
                ShowDiscountLine   = true,
                ShowTaxBreakdown   = true,
                ShowLoyaltyPoints  = true,
                ShowSavingsAmount  = true,
                BarcodeSymbology   = "Code128",
                PaperSize          = paperSize,
                IsDefault          = isDefault,
            }, T);

        // A thermal receipt (the branch default) and a full-page A4 receipt/invoice template.
        return
        [
            Make("Thermal Receipt (80mm)", "Thermal80mm", isDefault: true),
            Make("A4 Receipt / Invoice",   "A4",          isDefault: false),
        ];
    }

    // 1b. Barcode Label (Price Tag) Template

    private static PosBarcodeLabelTemplate SeedBarcodeLabelTemplate(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
        string? companyName = null) =>
        E(new PosBarcodeLabelTemplate
        {
            TemplateName       = "Default Price Tag (50x30mm)",
            HeaderText         = string.IsNullOrWhiteSpace(companyName) ? null : companyName,
            LabelWidthMm       = 50,
            LabelHeightMm      = 30,
            BarcodeSymbology   = "Code128",
            ShowProductName    = true,
            ShowPrice          = true,
            ShowSku            = true,
            ShowBarcodeValue   = true,
            ProductNameFontPt  = 8,
            PriceFontPt        = 11,
            BarcodeHeightPt    = 12,
            ShowBorders        = true,
            RollPaper          = false,
            IsDefault          = true,
        }, T);

    // 2. Price Lists

    private static (PriceList[] priceLists, PriceListItem[] priceListItems) SeedPriceLists(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T)
    {
        var retail = E(new PriceList
        {
            Code = "RETAIL", Name = "Retail Price List", Description = "Standard walk-in customer prices",
            Type = PriceListType.Retail, CurrencyCode = "USD", IsActive = true, ValidFrom = DateTime.UtcNow.AddYears(-1),
        }, T);

        var wholesale = E(new PriceList
        {
            Code = "WHOLESALE", Name = "Wholesale Price List", Description = "B2B bulk pricing - 15% below retail",
            Type = PriceListType.Wholesale, CurrencyCode = "USD", IsActive = true, ValidFrom = DateTime.UtcNow.AddYears(-1),
        }, T);

        var vip = E(new PriceList
        {
            Code = "VIP", Name = "VIP Customer Price List", Description = "Preferred customer pricing - 10% below retail",
            Type = PriceListType.CustomerSpecific, CurrencyCode = "USD", IsActive = true, ValidFrom = DateTime.UtcNow.AddYears(-1),
        }, T);

        var priceLists = new[] { retail, wholesale, vip };

        PriceListItem PLI(PriceList pl, Guid itemId, decimal price) =>
            E(new PriceListItem { PriceListId = pl.Id, ProductId = itemId, UnitPrice = price, ValidFrom = DateTime.UtcNow.AddYears(-1) }, T);

        var items = new[]
        {
            PLI(retail,    ItemId1, 1.50m),   PLI(retail,    ItemId2, 2.00m),   PLI(retail,    ItemId3, 499.00m), PLI(retail,    ItemId4, 120.00m), PLI(retail,    ItemId5, 1.80m),
            PLI(wholesale, ItemId1, 1.20m),   PLI(wholesale, ItemId2, 1.60m),   PLI(wholesale, ItemId3, 425.00m), PLI(wholesale, ItemId4, 102.00m), PLI(wholesale, ItemId5, 1.40m),
            PLI(vip,       ItemId1, 1.35m),   PLI(vip,       ItemId2, 1.80m),   PLI(vip,       ItemId3, 449.00m), PLI(vip,       ItemId4, 108.00m), PLI(vip,       ItemId5, 1.60m),
        };

        return (priceLists, items);
    }

    // 3. Customer Groups + Sales Territories

    private static CustomerGroup[] SeedCustomerGroups(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T) =>
    [
        E(new CustomerGroup { Code = "RETAIL",    Name = "Retail Customers",    Description = "Walk-in & app customers",           AllowCreditSales = true }, T),
        E(new CustomerGroup { Code = "WHOLESALE", Name = "Wholesale Customers", Description = "B2B / bulk buyers",                 AllowCreditSales = true, DefaultCreditLimit = 50000m }, T),
        E(new CustomerGroup { Code = "VIP",       Name = "VIP Customers",       Description = "High-value loyal customers",        AllowCreditSales = true, DefaultCreditLimit = 20000m }, T),
        E(new CustomerGroup { Code = "GOVT",      Name = "Government",          Description = "Government & institutional buyers", AllowCreditSales = true, DefaultCreditLimit = 100000m }, T),
    ];

    private static SalesTerritory[] SeedSalesTerritories(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T) =>
    [
        E(new SalesTerritory { Code = "NORTH", Name = "North Region", Region = "North" }, T),
        E(new SalesTerritory { Code = "SOUTH", Name = "South Region", Region = "South" }, T),
        E(new SalesTerritory { Code = "EAST",  Name = "East Region",  Region = "East"  }, T),
        E(new SalesTerritory { Code = "WEST",  Name = "West Region",  Region = "West"  }, T),
    ];

    // 4. Tax Groups + Rules

    private static (TaxGroup[] taxGroups, TaxGroupRate[] rates, TaxRule[] rules) SeedTaxGroups(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T)
    {
        var vatStdId  = new Guid("50000001-0000-0000-0000-000000000001");
        var vatZeroId = new Guid("50000001-0000-0000-0000-000000000002");
        var exemptId  = new Guid("50000001-0000-0000-0000-000000000003");

        var stdGroup  = E(new TaxGroup { Code = "STD-VAT",  Name = "Standard VAT Group",  Description = "15% VAT",       IsActive = true }, T);
        var zeroGroup = E(new TaxGroup { Code = "ZERO-VAT", Name = "Zero-Rated VAT Group", Description = "0% VAT - food", IsActive = true }, T);
        var exGroup   = E(new TaxGroup { Code = "EXEMPT",   Name = "Tax Exempt Group",     Description = "No VAT",        IsActive = true }, T);

        var rates = new[]
        {
            E(new TaxGroupRate { TaxGroupId = stdGroup.Id,  TaxDefinitionId = vatStdId,  SnapshotCode = "VAT-STD",  SnapshotRate = 15m, SnapshotTaxType = "VAT", SnapshotInclusionType = "Exclusive" }, T),
            E(new TaxGroupRate { TaxGroupId = zeroGroup.Id, TaxDefinitionId = vatZeroId, SnapshotCode = "VAT-ZERO", SnapshotRate = 0m,  SnapshotTaxType = "VAT", SnapshotInclusionType = "Exclusive" }, T),
            E(new TaxGroupRate { TaxGroupId = exGroup.Id,   TaxDefinitionId = exemptId,  SnapshotCode = "EXEMPT",   SnapshotRate = 0m,  SnapshotTaxType = "VAT", SnapshotInclusionType = "Inclusive" }, T),
        };

        var rules = new[]
        {
            E(new TaxRule { Name = "Standard Goods Rule",            TaxGroupId = stdGroup.Id,  Priority = 10, IsActive = true }, T),
            E(new TaxRule { Name = "Food & Beverage Zero-Rate Rule", TaxGroupId = zeroGroup.Id, Priority = 20, IsActive = true }, T),
        };

        return ([stdGroup, zeroGroup, exGroup], rates, rules);
    }

    // 5. Approval Policies

    private static (ApprovalPolicy[] policies, ApprovalPolicyCondition[] conditions, ApprovalPolicyStep[] steps)
        SeedApprovalPolicies(
            (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T)
    {
        var policy = E(new ApprovalPolicy
        {
            Name = "High-Value Order Approval", Description = "Orders above $5,000 require manager approval", IsActive = true,
        }, T);

        var conditions = new[]
        {
            E(new ApprovalPolicyCondition { ApprovalPolicyId = policy.Id, Field = ApprovalConditionField.OrderTotalAmount, Operator = ApprovalConditionOperator.GreaterThan, Value = "5000" }, T),
        };

        var steps = new[]
        {
            E(new ApprovalPolicyStep { ApprovalPolicyId = policy.Id, StepOrder = 1, StepName = "Sales Manager Approval",    ApproverRole = "SalesManager"    }, T),
            E(new ApprovalPolicyStep { ApprovalPolicyId = policy.Id, StepOrder = 2, StepName = "Finance Director Approval", ApproverRole = "FinanceDirector" }, T),
        };

        return ([policy], conditions, steps);
    }

    // 6. Commission Rules + Sales Targets

    private static CommissionRule[] SeedCommissionRules(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T) =>
    [
        E(new CommissionRule { Name = "Standard Sales Commission", Description = "5% on all confirmed orders",    Basis = CommissionBasis.PercentageOfNet, Rate = 0.05m, IsActive = true, RecognitionEvent = "OnInvoice", ValidFrom = DateTime.UtcNow.AddYears(-1) }, T),
        E(new CommissionRule { Name = "Electronics Bonus",         Description = "8% commission on electronics", Basis = CommissionBasis.PercentageOfNet, Rate = 0.08m, IsActive = true, RecognitionEvent = "OnInvoice", ValidFrom = DateTime.UtcNow.AddYears(-1) }, T),
    ];

    private static SalesTarget[] SeedSalesTargets(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T) =>
    [
        E(new SalesTarget
        {
            SalesRepId = EmpId1, Period = TargetPeriod.Monthly,
            PeriodStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1),
            PeriodEnd   = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.DaysInMonth(DateTime.UtcNow.Year, DateTime.UtcNow.Month)),
            TargetAmount = 50000m, ActualAmount = 32500m, CurrencyCode = "USD",
        }, T),
        E(new SalesTarget
        {
            SalesRepId = EmpId2, Period = TargetPeriod.Monthly,
            PeriodStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1),
            PeriodEnd   = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.DaysInMonth(DateTime.UtcNow.Year, DateTime.UtcNow.Month)),
            TargetAmount = 40000m, ActualAmount = 41200m, CurrencyCode = "USD",
        }, T),
    ];

    // 7. Discount Schemes + Coupons

    private static DiscountScheme[] SeedDiscountSchemes(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T) =>
    [
        E(new DiscountScheme { Code = "SUMMER-SALE", Name = "Summer Sale 10%",   DiscountType = DiscountType.Percentage,  DiscountValue = 10m, ValidFrom = DateTime.UtcNow.AddDays(-30),  ValidTo = DateTime.UtcNow.AddDays(30),  IsActive = true }, T),
        E(new DiscountScheme { Code = "BULK-5",      Name = "Bulk Buy Discount", DiscountType = DiscountType.Percentage,  DiscountValue = 5m,  MinOrderAmount = 200m, ValidFrom = DateTime.UtcNow.AddMonths(-3),                               IsActive = true }, T),
        E(new DiscountScheme { Code = "FIXED-20",    Name = "Fixed $20 Off",     DiscountType = DiscountType.FixedAmount, DiscountValue = 20m, MinOrderAmount = 100m, ValidFrom = DateTime.UtcNow.AddMonths(-1),                               IsActive = true }, T),
    ];

    private static Coupon[] SeedCoupons(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T) =>
    [
        E(new Coupon { Code = "WELCOME10", Name = "Welcome 10% Off", Status = CouponStatus.Active, DiscountType = DiscountType.Percentage,  DiscountValue = 10m, MaxDiscountAmount = 50m, ValidFrom = DateTime.UtcNow.AddMonths(-6), ValidTo = DateTime.UtcNow.AddMonths(6),  MaxUsageCount = 500, MaxUsagePerCustomer = 1, IsFirstOrderOnly = true }, T),
        E(new Coupon { Code = "SAVE25",    Name = "$25 Off $150+",   Status = CouponStatus.Active, DiscountType = DiscountType.FixedAmount, DiscountValue = 25m, MinOrderAmount = 150m,   ValidFrom = DateTime.UtcNow.AddMonths(-2), ValidTo = DateTime.UtcNow.AddMonths(2),  MaxUsageCount = 200, MaxUsagePerCustomer = 2 }, T),
        E(new Coupon { Code = "FREEDEL",   Name = "Free Delivery",   Status = CouponStatus.Active, DiscountType = DiscountType.Percentage,  DiscountValue = 0m,                          ValidFrom = DateTime.UtcNow.AddMonths(-1), ValidTo = DateTime.UtcNow.AddDays(14),   MaxUsagePerCustomer = 1, IsFreeDelivery = true }, T),
    ];

    // 7b. Promotions (auto-applied combo deal)
    // Targets the Inventory FAST-FOOD category by its deterministic cross-module
    // ID (CrossModuleGuid seq 1400) so the live pricing engine matches pizza /
    // burger / fries lines. Gated by MinOrderAmount so a single item isn't
    // discounted, but a real combo (pizza + burger + fries + drink) is.
    private static (Promotion[] promotions, PromotionItem[] items) SeedPromotions(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T)
    {
        var fastFoodCategoryId = CrossModuleGuid.Derive(T.companyId, 1400);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var combo = E(new Promotion
        {
            Name                = "Family Combo — 15% Off",
            PromotionCode       = null,                 // auto-applied, no code needed
            IsAutoApplied       = true,
            Status              = PromotionStatus.Active,
            Priority            = 20,
            StartDate           = today.AddDays(-1),
            EndDate             = today.AddYears(1),
            ScheduledDays       = ScheduledDays.EveryDay,
            MinOrderAmount      = 1500m,                // kicks in for a real combo basket
            TargetType          = PromotionTargetType.AllCustomers,
            IsStackable         = false,
            Notes               = "15% off pizzas, burgers & fries when the basket qualifies as a combo.",
        }, T);

        var items = new[]
        {
            E(new PromotionItem
            {
                PromotionId    = combo.Id,
                ItemCategoryId = fastFoodCategoryId,     // all FAST-FOOD menu items
                DiscountType   = PromotionDiscountType.PercentageOff,
                Value          = 15m,
                PriceTarget    = PromotionPriceTarget.AnyPrice,
                IsConditional  = false,
                ConditionType  = PromotionConditionType.None,
            }, T),
        };

        return ([combo], items);
    }

    // 8. Loyalty Program

    private static LoyaltyProgram SeedLoyaltyProgram(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T) =>
        E(new LoyaltyProgram
        {
            ProgramName             = "Nexcore Rewards",
            Description             = "Earn 1 point per $1 spent. Redeem 100 points for $1.",
            IsActive                = true,
            PointsPerCurrencyUnit   = 1m,
            MinOrderAmountToEarn    = 5m,
            PointValueInCurrency    = 0.01m,
            MinPointsToRedeem       = 100m,
            MaxRedemptionPercentage = 30m,
            PointsExpiryDays        = 365,
            BronzeThreshold         = 0m,
            SilverThreshold         = 500m,
            GoldThreshold           = 2000m,
            PlatinumThreshold       = 5000m,
            DiamondThreshold        = 15000m,
        }, T);

    // 9. POS Store + Schedule + Holidays + Delivery Zones

    private static (PosStore store, PosStoreSchedule[] schedule, PosStoreHoliday[] holidays,
        DeliveryZone[] zones) SeedPosStore(
            (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
            PriceList[] priceLists,
            PosReceiptTemplate receiptTemplate)
    {
        var retailPl = priceLists.First(p => p.Code == "RETAIL");

        var store = E(new PosStore
        {
            Code                     = "POS-GEN-000001",
            CodeInt                  = 1,
            TradingName              = "Nexcore Main Store",
            StoreType                = PosStoreType.Retail,
            DefaultWarehouseId       = CrossModuleIds.MainWarehouseId(T.companyId),
            DefaultPriceListId       = retailPl.Id,
            ReceiptTemplateId        = receiptTemplate.Id,
            AcceptsOnlinePickup      = true,
            HasDelivery              = true,
            IsOnlineOrderingEnabled  = true,
            OnlineStatus             = StoreOnlineStatus.Open,
            EstimatedPrepTimeMinutes = 15,
            MinOnlineOrderAmount     = 10m,
            MaxDeliveryRadiusKm      = 10.0,
            OnlineLogoUrl            = "https://cdn.nexcore.com/store-logo.png",
            OnlineBannerUrl          = "https://cdn.nexcore.com/store-banner.png",
            IsActive                 = true,
        }, T);

        var schedule = new (System.DayOfWeek day, string open, string close)[]
        {
            (System.DayOfWeek.Monday,    "08:00", "22:00"),
            (System.DayOfWeek.Tuesday,   "08:00", "22:00"),
            (System.DayOfWeek.Wednesday, "08:00", "22:00"),
            (System.DayOfWeek.Thursday,  "08:00", "22:00"),
            (System.DayOfWeek.Friday,    "08:00", "23:00"),
            (System.DayOfWeek.Saturday,  "09:00", "23:00"),
            (System.DayOfWeek.Sunday,    "10:00", "21:00"),
        }.Select(d => E(new PosStoreSchedule
        {
            PosStoreId = store.Id, DayOfWeek = d.day, OpeningTime = d.open, ClosingTime = d.close, IsClosed = false,
        }, T)).ToArray();

        var holidays = new[]
        {
            E(new PosStoreHoliday { PosStoreId = store.Id, Date = new DateTime(DateTime.UtcNow.Year, 12, 25), Description = "Christmas Day",  IsPartiallyOpen = false }, T),
            E(new PosStoreHoliday { PosStoreId = store.Id, Date = new DateTime(DateTime.UtcNow.Year,  1,  1), Description = "New Year's Day", IsPartiallyOpen = false }, T),
        };

        var zones = new[]
        {
            E(new DeliveryZone { ZoneCode = "ZONE-A", ZoneName = "City Centre (Zone A)", DeliveryFee = 2.50m, FreeDeliveryAboveAmount = 50m, RadiusKm = 3.0, EstimatedDeliveryMinutes = 20, IsActive = true, SortOrder = 1 }, T),
            E(new DeliveryZone { ZoneCode = "ZONE-B", ZoneName = "Suburbs (Zone B)",     DeliveryFee = 4.99m, FreeDeliveryAboveAmount = 80m, RadiusKm = 8.0, EstimatedDeliveryMinutes = 40, IsActive = true, SortOrder = 2 }, T),
        };

        return (store, schedule, holidays, zones);
    }

    // 10. POS Terminals + Cash Drawers

    private static (PosTerminal[] terminals, PosCashDrawer[] drawers) SeedPosTerminals(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
        PosStore store)
    {
        var drawer1 = E(new PosCashDrawer { DrawerCode = "DRW-001", DrawerLabel = "Main Cash Drawer",    IsActive = true }, T);
        var drawer2 = E(new PosCashDrawer { DrawerCode = "DRW-002", DrawerLabel = "Express Lane Drawer", IsActive = true }, T);

        var terminal1 = E(new PosTerminal { TerminalCode = "TRM-001", TerminalName = "Main Checkout",       PosStoreId = store.Id, CashDrawerId = drawer1.Id, IsActive = true, IsOnline = true  }, T);
        var terminal2 = E(new PosTerminal { TerminalCode = "TRM-002", TerminalName = "Express Checkout",    PosStoreId = store.Id, CashDrawerId = drawer2.Id, IsActive = true, IsOnline = true  }, T);
        var terminal3 = E(new PosTerminal { TerminalCode = "TRM-003", TerminalName = "Mobile POS (Tablet)", PosStoreId = store.Id,                            IsActive = true, IsOnline = false }, T);

        return ([terminal1, terminal2, terminal3], [drawer1, drawer2]);
    }

    // 11. POS Cashiers

    private static PosCashier[] SeedPosCashiers(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
        PosStore store) =>
    [
        E(new PosCashier { EmployeeId = EmpId1, DisplayName = "Alice Johnson",  PinCode = BC.HashPassword("1234"), BadgeNumber = "BADGE-001", PosStoreId = store.Id, CanApplyManualDiscount = true,  MaxManualDiscountPercentage = 15m, CanVoidTransaction = true,  CanIssueRefund = true,  CanOpenDrawer = true, CanOverridePrices = false, CanApplyCoupons = true, CanAccessReports = true,  IsActive = true }, T),
        E(new PosCashier { EmployeeId = EmpId2, DisplayName = "Bob Smith",      PinCode = BC.HashPassword("5678"), BadgeNumber = "BADGE-002", PosStoreId = store.Id, CanApplyManualDiscount = true,  MaxManualDiscountPercentage = 10m, CanVoidTransaction = false, CanIssueRefund = false, CanOpenDrawer = true, CanOverridePrices = false, CanApplyCoupons = true, CanAccessReports = false, IsActive = true }, T),
        E(new PosCashier { EmployeeId = EmpId3, DisplayName = "Carol Williams", PinCode = BC.HashPassword("9012"), BadgeNumber = "BADGE-003", PosStoreId = store.Id, CanApplyManualDiscount = false, MaxManualDiscountPercentage = 0m,  CanVoidTransaction = false, CanIssueRefund = false, CanOpenDrawer = true, CanOverridePrices = false, CanApplyCoupons = true, CanAccessReports = false, IsActive = true }, T),
    ];

    // 12. POS Session + Cash Movements + Drawer Events

    private static (PosSession session, PosCashMovement[] movements, PosCashDrawerEvent[] drawerEvents)
        SeedPosSession(
            (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
            PosTerminal terminal,
            PosCashier cashier)
    {
        var now = DateTime.UtcNow;

        var session = E(new PosSession
        {
            SessionNumber        = "SES-20240001",
            PosTerminalId        = terminal.Id,
            PosCashierId         = cashier.Id,
            Status               = PosSessionStatus.Closed,
            OpenedAt             = now.AddHours(-8),
            ClosedAt             = now.AddHours(-0.5),
            OpeningFloat         = 200m,
            ClosingFloat         = 874.50m,
            ExpectedClosingFloat = 882.30m,
            FloatVariance        = -7.80m,
            TotalSalesAmount     = 1285.60m,
            TotalRefundsAmount   = 45.00m,
            TotalDiscountsAmount = 62.30m,
            TotalTaxAmount       = 119.25m,
            NetSalesAmount       = 1178.05m,
            CashCollected        = 482.30m,
            CardCollected        = 698.75m,
            WalletCollected      = 104.55m,
            TransactionCount     = 18,
            ClosingNotes         = "Slight variance - $5 coin mismatch, $2.80 rounding",
        }, T);

        var movements = new[]
        {
            E(new PosCashMovement { PosSessionId = session.Id, PosCashierId = cashier.Id, MovementType = PosCashMovementType.CashIn,  Amount = 100m, Reason = "Additional float top-up at 10:00 AM", MovementDate = now.AddHours(-6) }, T),
            E(new PosCashMovement { PosSessionId = session.Id, PosCashierId = cashier.Id, MovementType = PosCashMovementType.CashOut, Amount = 250m, Reason = "Safe drop - mid-shift cash removal",   MovementDate = now.AddHours(-3) }, T),
        };

        var drawerEvents = new[]
        {
            E(new PosCashDrawerEvent { PosCashDrawerId = terminal.CashDrawerId ?? Guid.Empty, PosSessionId = session.Id, CashierId = cashier.Id, OpenReason = "SessionOpen", OpenedAt = now.AddHours(-8) }, T),
            E(new PosCashDrawerEvent { PosCashDrawerId = terminal.CashDrawerId ?? Guid.Empty, PosSessionId = session.Id, CashierId = cashier.Id, OpenReason = "SafeDrop",    OpenedAt = now.AddHours(-3) }, T),
        };

        return (session, movements, drawerEvents);
    }

    // 13. Quotation

    private static (Quotation quotation, QuotationLine[] lines, QuotationApproval[] approvals)
        SeedQuotation(
            (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
            PriceList[] priceLists)
    {
        var wholesale = priceLists.First(p => p.Code == "WHOLESALE");
        var now       = DateTime.UtcNow;

        var quotation = E(new Quotation
        {
            QuotationNumber = "QUO-2024-0001",
            ContactId = ContactId2, PriceListId = wholesale.Id,
            Status = QuotationStatus.Sent, QuotationDate = now.AddDays(-5), ValidUntil = now.AddDays(25),
            CurrencyCode = "USD", TotalAmount = 2390.00m, SalesRepId = EmpId1,
            Notes = "Bulk order quote for wholesale customer.",
        }, T);

        var lines = new[]
        {
            E(new QuotationLine
            {
                QuotationId = quotation.Id, LineNumber = 1, ProductId = ItemId3,
                ProductCode = "ELEC-SAM-A54", ProductName = "Samsung Galaxy A54",
                Quantity = 5m, UnitOfMeasure = "PCS", UnitPrice = 425.00m,
                DiscountPercentage = 2m, DiscountAmount = 42.50m,
                TaxRate = 15m, TaxAmount = 304.50m, TotalAmount = 2390.00m,
            }, T),
        };

        var approvals = new[]
        {
            E(new QuotationApproval { QuotationId = quotation.Id, ApproverId = T.userId, Status = "Approved", DecisionDate = now.AddDays(-4), Comments = "Approved - standard wholesale terms applied." }, T),
        };

        return (quotation, lines, approvals);
    }

    // 14. Sales Agreement

    private static (SalesAgreement agreement, SalesAgreementLine[] lines) SeedSalesAgreement(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
        PriceList[] priceLists)
    {
        var wholesale = priceLists.First(p => p.Code == "WHOLESALE");
        var now       = DateTime.UtcNow;

        var agreement = E(new SalesAgreement
        {
            AgreementNumber = "SA-2024-0001",
            ContactId = ContactId2, PriceListId = wholesale.Id,
            Status = SalesAgreementStatus.Active,
            StartDate = now.AddMonths(-3), EndDate = now.AddMonths(9),
            CommittedAmount = 50000m, ReleasedAmount = 12500m, CurrencyCode = "USD",
            Notes = "Annual blanket order - 50,000 USD committed.",
        }, T);

        var lines = new[]
        {
            E(new SalesAgreementLine { SalesAgreementId = agreement.Id, LineNumber = 1, ProductId = ItemId3, ProductCode = "ELEC-SAM-A54", ProductName = "Samsung Galaxy A54", CommittedQuantity = 100m, ReleasedQuantity = 25m, RemainingQuantity = 75m, AgreedUnitPrice = 425.00m, UnitOfMeasure = "PCS"  }, T),
            E(new SalesAgreementLine { SalesAgreementId = agreement.Id, LineNumber = 2, ProductId = ItemId4, ProductCode = "SPT-NIKE-001", ProductName = "Nike Running Shoes",  CommittedQuantity = 50m,  ReleasedQuantity = 10m, RemainingQuantity = 40m, AgreedUnitPrice = 102.00m, UnitOfMeasure = "PAIR" }, T),
        };

        return (agreement, lines);
    }

    // 15. Sales Orders

    private static (
        SalesOrder[] orders,
        SalesOrderLine[] lines,
        SalesOrderLineAddon[] addons,
        SalesOrderStatusHistory[] statusHistory,
        SalesOrderApproval[] approvals,
        SalesOrderAttachment[] attachments)
        SeedSalesOrders(
            (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
            PriceList[] priceLists,
            PosStore store,
            PosTerminal terminal,
            PosCashier cashier,
            PosSession session,
            Quotation quotation,
            SalesAgreement agreement,
            Coupon[] coupons)
    {
        var retail    = priceLists.First(p => p.Code == "RETAIL");
        var wholesale = priceLists.First(p => p.Code == "WHOLESALE");
        var now       = DateTime.UtcNow;

        // Order 1: POS Walk-In - Paid & Closed
        var order1 = E(new SalesOrder
        {
            OrderNumber        = "SO-2024-0001",
            ContactId          = ContactId1, ContactName = "Alice Brown",
            Status             = SalesOrderStatus.PaidAndClosed,
            SalesChannel       = SalesChannel.PosWalkIn, FulfillmentType = FulfillmentType.Immediate,
            OriginBranchId   = store.Id, OriginPosTerminalId = terminal.Id,
            OriginPosCashierId = cashier.Id, OriginPosSessionId = session.Id,
            PriceListId        = retail.Id, CurrencyCode = "USD",
            SubtotalAmount = 5.30m, TaxAmount = 0.53m, TotalAmount = 5.83m,
            PaidAmount = 5.83m, BalanceDue = 0m,
            OrderDate = now.AddHours(-5), PlacedAt = now.AddHours(-5), ClosedDate = now.AddHours(-5),
            PaymentTerms = PaymentTerms.Immediate,
            Notes = "Walk-in customer - cash payment",
        }, T);

        // Order 2: B2B Wholesale - Confirmed
        var order2 = E(new SalesOrder
        {
            OrderNumber        = "SO-2024-0002",
            ContactId          = ContactId2, ContactName = "TechMart Ltd",
            Status             = SalesOrderStatus.Confirmed,
            SalesChannel       = SalesChannel.DirectSales, FulfillmentType = FulfillmentType.Delivery,
            PriceListId        = wholesale.Id, CurrencyCode = "USD",
            QuotationId        = quotation.Id, SalesAgreementId = agreement.Id,
            SalesRepId         = EmpId1, CustomerPONumber = "PO-TECHMART-0045",
            SubtotalAmount = 2125.00m, DiscountAmount = 42.50m, TaxAmount = 304.50m,
            TotalAmount = 2387.00m, PaidAmount = 0m, BalanceDue = 2387.00m,
            OrderDate = now.AddDays(-3), PlacedAt = now.AddDays(-3),
            PaymentTerms = PaymentTerms.Net30,
            ShipToAddressId = null, // full address is on the linked Delivery record
            CreditCheckPassed = true,
            Notes = "Bulk order per SA-2024-0001",
        }, T);

        // Order 3: Online App - Preparing
        var order3 = E(new SalesOrder
        {
            OrderNumber          = "SO-2024-0003",
            ContactId            = ContactId3, ContactName = "Maria Garcia",
            Status               = SalesOrderStatus.Preparing,
            SalesChannel         = SalesChannel.OnlineApp, FulfillmentType = FulfillmentType.Delivery,
            OriginBranchId     = store.Id,
            PriceListId          = retail.Id, CurrencyCode = "USD",
            CouponCode           = "WELCOME10", CouponDiscountAmount = 0.38m,
            LoyaltyPointsEarned  = 6m,
            SubtotalAmount = 3.80m, DiscountAmount = 0.38m, TaxAmount = 0.51m,
            ShippingAmount = 2.50m, TotalAmount = 6.43m,
            PaidAmount = 6.43m, BalanceDue = 0m,
            OrderDate = now.AddHours(-2), PlacedAt = now.AddHours(-2),
            ShipToAddressId = null, // full address is on the linked Delivery record
            PaymentTerms = PaymentTerms.Immediate,
        }, T);

        // Order 4: POS Parked - electronics, awaiting payment
        var order4 = E(new SalesOrder
        {
            OrderNumber        = "SO-2024-0004",
            ContactId          = ContactId4, ContactName = "John Doe",
            Status             = SalesOrderStatus.PosParked,
            SalesChannel       = SalesChannel.PosWalkIn, FulfillmentType = FulfillmentType.Immediate,
            OriginBranchId   = store.Id, OriginPosTerminalId = terminal.Id,
            OriginPosCashierId = cashier.Id, OriginPosSessionId = session.Id,
            PriceListId        = retail.Id, CurrencyCode = "USD",
            SubtotalAmount = 499.00m, TaxAmount = 74.85m, TotalAmount = 573.85m,
            PaidAmount = 0m, BalanceDue = 573.85m,
            OrderDate = now.AddHours(-1),
            PaymentTerms = PaymentTerms.Immediate,
            Notes = "Customer stepped out - order parked",
        }, T);

        var orders = new[] { order1, order2, order3, order4 };

        // Lines
        var lines = new[]
        {
            // Order1 - Coke
            E(new SalesOrderLine { SalesOrderId = order1.Id, LineNumber = 1, ProductId = ItemId1, ProductCode = "BEV-COKE-330", ProductName = "Coca-Cola 330ml",    OrderedQuantity = 2m, UnitOfMeasure = "PCS",  UnitPrice = 1.50m,   NetUnitPrice = 1.50m,   LineAmount = 3.00m,   TaxRate = 0m,  TaxAmount = 0m,     TotalAmount = 3.00m,   LineStatus = "Closed", WarehouseId = RetailWarehouseId(T.companyId) }, T),
            // Order1 - Chips
            E(new SalesOrderLine { SalesOrderId = order1.Id, LineNumber = 2, ProductId = ItemId2, ProductCode = "SNK-LAYS-STD", ProductName = "Lay's Classic Chips", OrderedQuantity = 1m, UnitOfMeasure = "PCS",  UnitPrice = 2.30m,   NetUnitPrice = 2.30m,   LineAmount = 2.30m,   TaxRate = 15m, TaxAmount = 0.53m,  TotalAmount = 2.83m,   LineStatus = "Closed", WarehouseId = RetailWarehouseId(T.companyId) }, T),
            // Order2 - Samsung x5
            E(new SalesOrderLine { SalesOrderId = order2.Id, LineNumber = 1, ProductId = ItemId3, ProductCode = "ELEC-SAM-A54", ProductName = "Samsung Galaxy A54",  OrderedQuantity = 5m, UnitOfMeasure = "PCS",  UnitPrice = 425.00m, DiscountPercentage = 2m, DiscountAmount = 42.50m, NetUnitPrice = 416.50m, LineAmount = 2082.50m, TaxRate = 15m, TaxAmount = 312.38m, TotalAmount = 2394.88m, LineStatus = "Open",   WarehouseId = RetailWarehouseId(T.companyId) }, T),
            // Order3 - Milk
            E(new SalesOrderLine { SalesOrderId = order3.Id, LineNumber = 1, ProductId = ItemId5, ProductCode = "DAI-MILK-1L",  ProductName = "Whole Milk 1L",       OrderedQuantity = 2m, UnitOfMeasure = "PCS",  UnitPrice = 1.80m,   NetUnitPrice = 1.80m,   LineAmount = 3.60m,   TaxRate = 0m,  TaxAmount = 0m,     TotalAmount = 3.60m,   LineStatus = "Open",   WarehouseId = RetailWarehouseId(T.companyId) }, T),
            // Order3 - Chips
            E(new SalesOrderLine { SalesOrderId = order3.Id, LineNumber = 2, ProductId = ItemId2, ProductCode = "SNK-LAYS-STD", ProductName = "Lay's Classic Chips", OrderedQuantity = 1m, UnitOfMeasure = "PCS",  UnitPrice = 2.00m,   NetUnitPrice = 2.00m,   LineAmount = 2.00m,   TaxRate = 15m, TaxAmount = 0.51m,  TotalAmount = 2.51m,   LineStatus = "Open",   WarehouseId = RetailWarehouseId(T.companyId) }, T),
            // Order4 - Samsung (parked)
            E(new SalesOrderLine { SalesOrderId = order4.Id, LineNumber = 1, ProductId = ItemId3, ProductCode = "ELEC-SAM-A54", ProductName = "Samsung Galaxy A54",  OrderedQuantity = 1m, UnitOfMeasure = "PCS",  UnitPrice = 499.00m, NetUnitPrice = 499.00m, LineAmount = 499.00m, TaxRate = 15m, TaxAmount = 74.85m, TotalAmount = 573.85m, LineStatus = "Open",   WarehouseId = RetailWarehouseId(T.companyId) }, T),
        };

        // Add-ons (modifier on order3's chips)
        var addons = new[]
        {
            E(new SalesOrderLineAddon { SalesOrderLineId = lines[4].Id, AddonName = "Extra Spicy", AddonProductId = null, Quantity = 1m, UnitPrice = 0m, TotalPrice = 0m }, T),
        };

        // Status Histories
        var statusHistory = new[]
        {
            E(new SalesOrderStatusHistory { SalesOrderId = order1.Id, Status = SalesOrderStatus.PaidAndClosed, ChangedAt = order1.ClosedDate!.Value, ChangedBy = cashier.DisplayName, Note = "POS walk-in payment completed" }, T),
            E(new SalesOrderStatusHistory { SalesOrderId = order2.Id, Status = SalesOrderStatus.Confirmed,    ChangedAt = order2.PlacedAt!.Value,    ChangedBy = "SalesRep",          Note = "Confirmed per customer PO"   }, T),
            E(new SalesOrderStatusHistory { SalesOrderId = order3.Id, Status = SalesOrderStatus.Preparing,    ChangedAt = order3.PlacedAt!.Value,    ChangedBy = "System",            Note = "Online payment confirmed"    }, T),
        };

        // Approvals
        var approvals = new[]
        {
            E(new SalesOrderApproval
            {
                SalesOrderId = order2.Id,
                ApproverId   = T.userId,
                Status       = "Approved",
                DecisionDate = now.AddDays(-2),
                Comments     = "Approved - credit check passed, within agreement limit",
            }, T),
        };

        // Attachments
        var attachments = new[]
        {
            E(new SalesOrderAttachment
            {
                SalesOrderId  = order2.Id,
                FileName      = "PO-TECHMART-0045.pdf",
                FileUrl       = "https://cdn.nexcore.com/attachments/PO-TECHMART-0045.pdf",
                FileSizeBytes = 124500L,
                ContentType   = "application/pdf",
                Description   = "Customer Purchase Order document",
            }, T),
        };

        return (orders, lines, addons, statusHistory, approvals, attachments);
    }

    // 16. POS Transactions

    private static (PosTransaction[] txns, PosTransactionLine[] txnLines, PosPayment[] payments)
        SeedPosTransactions(
            (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
            PosStore store,
            PosTerminal terminal,
            PosCashier cashier,
            PosSession session,
            SalesOrder[] salesOrders)
    {
        var posOrder = salesOrders[0];
        var now      = DateTime.UtcNow;

        var txn1 = E(new PosTransaction
        {
            TransactionNumber = "TXN-2024-00001", ReceiptNumber = "RCP-00001",
            PosSessionId = session.Id, PosTerminalId = terminal.Id, PosStoreId = store.Id, PosCashierId = cashier.Id,
            ContactId = posOrder.ContactId, SalesOrderId = posOrder.Id,
            TransactionType = PosTransactionType.Sale, Status = PosTransactionStatus.Completed,
            TransactionDate = posOrder.OrderDate,
            SubtotalAmount = 5.30m, TaxAmount = 0.53m, TotalAmount = 5.83m,
            TenderedAmount = 10.00m, ChangeAmount = 4.17m, LoyaltyPointsEarned = 5m,
        }, T);

        var txn2 = E(new PosTransaction
        {
            TransactionNumber = "TXN-2024-00002", ReceiptNumber = "RCP-00002",
            PosSessionId = session.Id, PosTerminalId = terminal.Id, PosStoreId = store.Id, PosCashierId = cashier.Id,
            ContactId = null,
            TransactionType = PosTransactionType.Sale, Status = PosTransactionStatus.Completed,
            TransactionDate = now.AddHours(-4),
            SubtotalAmount = 120.00m, TaxAmount = 18.00m, TotalAmount = 138.00m,
            TenderedAmount = 140.00m, ChangeAmount = 2.00m,
        }, T);

        var txn3 = E(new PosTransaction
        {
            TransactionNumber = "TXN-2024-00003", ReceiptNumber = "RCP-00003",
            PosSessionId = session.Id, PosTerminalId = terminal.Id, PosStoreId = store.Id, PosCashierId = cashier.Id,
            ContactId = posOrder.ContactId, OriginalTransactionId = txn1.Id,
            TransactionType = PosTransactionType.Refund, Status = PosTransactionStatus.Completed,
            TransactionDate = now.AddHours(-2),
            SubtotalAmount = -2.30m, TaxAmount = -0.53m, TotalAmount = -2.83m,
            TenderedAmount = -2.83m, Notes = "Customer returned Lay's (damaged bag)",
        }, T);

        var txns = new[] { txn1, txn2, txn3 };

        var txnLines = new[]
        {
            E(new PosTransactionLine { PosTransactionId = txn1.Id, LineNumber = 1, ProductId = ItemId1, ProductCode = "BEV-COKE-330", ProductName = "Coca-Cola 330ml",    Quantity = 2m,  UnitOfMeasure = "PCS",  UnitPrice = 1.50m,   NetUnitPrice = 1.50m,   LineAmount = 3.00m,   TaxRate = 0m,  TaxAmount = 0m,     TotalAmount = 3.00m   }, T),
            E(new PosTransactionLine { PosTransactionId = txn1.Id, LineNumber = 2, ProductId = ItemId2, ProductCode = "SNK-LAYS-STD", ProductName = "Lay's Classic Chips", Quantity = 1m,  UnitOfMeasure = "PCS",  UnitPrice = 2.30m,   NetUnitPrice = 2.30m,   LineAmount = 2.30m,   TaxRate = 15m, TaxAmount = 0.53m,  TotalAmount = 2.83m   }, T),
            E(new PosTransactionLine { PosTransactionId = txn2.Id, LineNumber = 1, ProductId = ItemId4, ProductCode = "SPT-NIKE-001", ProductName = "Nike Running Shoes",   Quantity = 1m,  UnitOfMeasure = "PAIR", UnitPrice = 120.00m, NetUnitPrice = 120.00m, LineAmount = 120.00m, TaxRate = 15m, TaxAmount = 18.00m, TotalAmount = 138.00m }, T),
            E(new PosTransactionLine { PosTransactionId = txn3.Id, LineNumber = 1, ProductId = ItemId2, ProductCode = "SNK-LAYS-STD", ProductName = "Lay's Classic Chips", Quantity = -1m, UnitOfMeasure = "PCS",  UnitPrice = 2.30m,   NetUnitPrice = 2.30m,   LineAmount = -2.30m,  TaxRate = 15m, TaxAmount = -0.53m, TotalAmount = -2.83m, IsRefunded = true, RefundedQuantity = 1m }, T),
        };

        var payments = new[]
        {
            E(new PosPayment { PosTransactionId = txn1.Id, TenderType = PosTenderType.Cash,       Amount = 10.00m,  IsApproved = true }, T),
            E(new PosPayment { PosTransactionId = txn2.Id, TenderType = PosTenderType.CreditCard, Amount = 138.00m, IsApproved = true, CardScheme = "VISA", CardLast4 = "4242", AuthorizationCode = "AUTH-88821", ReferenceNumber = "REF-TXN-00002" }, T),
            E(new PosPayment { PosTransactionId = txn3.Id, TenderType = PosTenderType.Cash,       Amount = -2.83m,  IsApproved = true, ReferenceNumber = "REFUND-TXN-00001" }, T),
        };

        return (txns, txnLines, payments);
    }

    // 17. Deliveries

    private static (Delivery[] deliveries, DeliveryLine[] lines) SeedDeliveries(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
        SalesOrder[] orders,
        SalesOrderLine[] orderLines)
    {
        var order2   = orders[1];
        var order3   = orders[2];
        var b2bLines = orderLines.Where(l => l.SalesOrderId == order2.Id).ToArray();
        var appLines = orderLines.Where(l => l.SalesOrderId == order3.Id).ToArray();
        var now      = DateTime.UtcNow;

        var del1 = E(new Delivery
        {
            DeliveryNumber = "DLV-2024-0001", SalesOrderId = order2.Id, ContactId = order2.ContactId,
            WarehouseId = RetailWarehouseId(T.companyId), Status = DeliveryStatus.Shipped,
            PlannedDeliveryDate = now.AddDays(2), ActualShipDate = now.AddDays(-1),
            Carrier = "FedEx", TrackingNumber = "FX-123456789",
            RecipientName = "TechMart Ltd Warehouse", Street = "99 Industrial Blvd",
            City = "Metropolis", Country = "US",
        }, T);

        var del2 = E(new Delivery
        {
            DeliveryNumber = "DLV-2024-0002", SalesOrderId = order3.Id, ContactId = order3.ContactId,
            WarehouseId = RetailWarehouseId(T.companyId), Status = DeliveryStatus.Draft,
            PlannedDeliveryDate = now.AddHours(1),
            RecipientName = "Maria Garcia", RecipientPhone = "+1-555-0199",
            Street = "45 Maple Avenue", City = "Riverside", Country = "US",
            DeliveryLatitude = 40.7128, DeliveryLongitude = -74.0060,
            DeliveryNotes = "Leave at front door",
        }, T);

        var deliveries = new[] { del1, del2 };

        var deliveryLines = new[]
        {
            E(new DeliveryLine { DeliveryId = del1.Id, SalesOrderLineId = b2bLines[0].Id, LineNumber = 1, DeliveredQuantity = 5m }, T),
            E(new DeliveryLine { DeliveryId = del2.Id, SalesOrderLineId = appLines[0].Id, LineNumber = 1, DeliveredQuantity = 0m }, T),
            E(new DeliveryLine { DeliveryId = del2.Id, SalesOrderLineId = appLines[1].Id, LineNumber = 2, DeliveredQuantity = 0m }, T),
        };

        return (deliveries, deliveryLines);
    }

    // 18. Invoices + Payments + Credit Notes

    private static (
        SalesInvoice[] invoices,
        SalesInvoiceLine[] invoiceLines,
        SalesPayment[] payments,
        CreditNote[] creditNotes,
        CreditNoteLine[] creditNoteLines)
        SeedInvoicesAndPayments(
            (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
            SalesOrder[] orders,
            SalesOrderLine[] orderLines,
            Delivery[] deliveries)
    {
        var order2   = orders[1];
        var del1     = deliveries[0];
        var b2bLines = orderLines.Where(l => l.SalesOrderId == order2.Id).ToArray();
        var now      = DateTime.UtcNow;

        var invoice = E(new SalesInvoice
        {
            InvoiceNumber = "INV-2024-0001",
            SalesOrderId  = order2.Id, ContactId = order2.ContactId, DeliveryId = del1.Id,
            Status        = InvoiceStatus.Issued, InvoiceDate = now.AddDays(-1), DueDate = now.AddDays(29),
            CurrencyCode  = "USD", TotalAmount = 2387.00m, PaidAmount = 0m, BalanceDue = 2387.00m,
            PaymentTerms  = PaymentTerms.Net30,
            Notes         = "Invoice for SO-2024-0002 per PO-TECHMART-0045",
        }, T);

        var invoiceLines = new[]
        {
            E(new SalesInvoiceLine
            {
                SalesInvoiceId = invoice.Id, SalesOrderLineId = b2bLines[0].Id, LineNumber = 1,
                ProductId = ItemId3, ProductCode = "ELEC-SAM-A54", ProductName = "Samsung Galaxy A54",
                Quantity = 5m, UnitOfMeasure = "PCS", UnitPrice = 416.50m,
                DiscountAmount = 42.50m, TaxRate = 15m, TaxAmount = 312.38m, TotalAmount = 2394.88m,
            }, T),
        };

        var salesPayments = new[]
        {
            E(new SalesPayment { PaymentNumber = "PMT-2024-0001", SalesOrderId = orders[0].Id, PaymentMethod = "Cash", Amount = 5.83m, PaymentDate = orders[0].OrderDate, CurrencyCode = "USD" }, T),
        };

        var creditNote = E(new CreditNote
        {
            CreditNoteNumber = "CN-2024-0001",
            SalesInvoiceId   = invoice.Id, ContactId = orders[0].ContactId,
            CreditNoteDate   = now.AddHours(-2), CurrencyCode = "USD",
            TotalAmount      = 2.83m, Reason = "Returned damaged product - Lay's chips",
        }, T);

        var creditNoteLines = new[]
        {
            E(new CreditNoteLine
            {
                CreditNoteId = creditNote.Id, LineNumber = 1,
                ProductId = ItemId2, ProductCode = "SNK-LAYS-STD", ProductName = "Lay's Classic Chips",
                Quantity = 1m, UnitOfMeasure = "PCS", UnitPrice = 2.30m, LineAmount = 2.30m,
                TaxRate = 15m, TaxAmount = 0.53m, TotalAmount = 2.83m, Reason = "Damaged bag",
            }, T),
        };

        return ([invoice], invoiceLines, salesPayments, [creditNote], creditNoteLines);
    }

    // 19. Returns

    private static (SalesReturn[] returns, SalesReturnLine[] lines) SeedReturns(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
        SalesOrder[] orders,
        SalesOrderLine[] orderLines,
        SalesInvoice[] invoices)
    {
        var now       = DateTime.UtcNow;
        var chipsLine = orderLines.First(l => l.SalesOrderId == orders[0].Id && l.ProductId == ItemId2);

        var ret = E(new SalesReturn
        {
            ReturnNumber      = "RTN-2024-0001",
            SalesOrderId      = orders[0].Id, SalesInvoiceId = invoices[0].Id,
            ContactId         = orders[0].ContactId,
            Status            = ReturnStatus.GoodsReceived,
            RequestDate       = now.AddHours(-2), ReceivedDate = now.AddHours(-1),
            ReturnReason      = "Damaged packaging",
            TotalRefundAmount = 2.83m,
        }, T);

        var returnLines = new[]
        {
            E(new SalesReturnLine
            {
                SalesReturnId    = ret.Id, SalesOrderLineId = chipsLine.Id, LineNumber = 1,
                ProductId = ItemId2, ProductCode = "SNK-LAYS-STD", ProductName = "Lay's Classic Chips",
                ReturnedQuantity = 1m, UnitOfMeasure = "PCS", UnitPrice = 2.30m,
                RefundAmount = 2.83m, Reason = "Damaged bag", ConditionOnReturn = "Damaged",
            }, T),
        };

        return ([ret], returnLines);
    }

    // 20. Riders

    private static (Rider[] riders, RiderShift[] shifts, RiderAssignment[] assignments,
        RiderRating[] ratings, RiderLocationLog[] locationLogs)
        SeedRiders(
            (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
            PosStore store,
            SalesOrder[] orders)
    {
        var now    = DateTime.UtcNow;
        var order3 = orders[2];

        var rider1 = E(new Rider
        {
            RiderCode = "RDR-001", FirstName = "Miguel", LastName = "Santos",
            Phone = "+1-555-0101", Email = "miguel.santos@nexcore.com",
            ContractType = "Employee", HomeBranchId = store.Id,
            VehicleType = VehicleType.Motorcycle, VehiclePlateNumber = "ABC-1234",
            VehicleModel = "Honda CB150", VehicleColor = "Red",
            Status = RiderStatus.Busy, CurrentLatitude = 40.7150, CurrentLongitude = -74.0040,
            TotalDeliveries = 248, SuccessfulDeliveries = 245, FailedDeliveries = 3,
            AverageRating = 4.8m, IsActive = true, IsVerified = true,
        }, T);

        var rider2 = E(new Rider
        {
            RiderCode = "RDR-002", FirstName = "Priya", LastName = "Patel",
            Phone = "+1-555-0202", Email = "priya.patel@nexcore.com",
            ContractType = "Employee", HomeBranchId = store.Id, VehicleType = VehicleType.Bicycle,
            Status = RiderStatus.Available, TotalDeliveries = 89, SuccessfulDeliveries = 89,
            AverageRating = 4.9m, IsActive = true, IsVerified = true,
        }, T);

        var shifts = new[]
        {
            E(new RiderShift { RiderId = rider1.Id, ShiftStart = now.AddHours(-8) }, T),
            E(new RiderShift { RiderId = rider2.Id, ShiftStart = now.AddHours(-4) }, T),
        };

        var assignment = E(new RiderAssignment
        {
            AssignmentNumber = "ASN-2024-0001", SalesOrderId = order3.Id, RiderId = rider1.Id,
            PickupBranchId = store.Id, Status = RiderAssignmentStatus.PickedUp,
            AssignedAt = now.AddHours(-1), PickedUpAt = now.AddMinutes(-45),
            EstimatedDurationMinutes = 15,
        }, T);

        var ratings = new[]
        {
            E(new RiderRating { RiderId = rider1.Id, SalesOrderId = order3.Id, ContactId = ContactId3, Rating = 5, Comment = "Very fast delivery! Friendly.",    RatedAt = now.AddDays(-1) }, T),
            E(new RiderRating { RiderId = rider1.Id, SalesOrderId = order3.Id, ContactId = ContactId1, Rating = 5, Comment = "On time, handled items carefully.", RatedAt = now.AddDays(-3) }, T),
        };

        var locationLogs = new[]
        {
            E(new RiderLocationLog { RiderAssignmentId = assignment.Id, RiderId = rider1.Id, Latitude = 40.7128, Longitude = -74.0060, LoggedAt = now.AddMinutes(-45) }, T),
            E(new RiderLocationLog { RiderAssignmentId = assignment.Id, RiderId = rider1.Id, Latitude = 40.7140, Longitude = -74.0050, LoggedAt = now.AddMinutes(-20) }, T),
            E(new RiderLocationLog { RiderAssignmentId = assignment.Id, RiderId = rider1.Id, Latitude = 40.7150, Longitude = -74.0040, LoggedAt = now.AddMinutes(-5)  }, T),
        };

        return ([rider1, rider2], shifts, [assignment], ratings, locationLogs);
    }

    // 21. Gift Cards

    private static (PosGiftCard[] cards, PosGiftCardTransaction[] txns) SeedGiftCards(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T)
    {
        var now = DateTime.UtcNow;

        var gc1 = E(new PosGiftCard
        {
            CardNumber = "GC-0001-2024", OriginalBalance = 50m, CurrentBalance = 32.50m,
            CurrencyCode = "USD", Status = GiftCardStatus.Active,
            IssuedDate = now.AddMonths(-2), ExpiryDate = now.AddYears(1),
            ContactId = ContactId4, RecipientName = "John Doe",
            RecipientEmail = "john.doe@example.com", Message = "Happy Birthday!",
        }, T);

        var gc2 = E(new PosGiftCard
        {
            CardNumber = "GC-0002-2024", OriginalBalance = 100m, CurrentBalance = 100m,
            CurrencyCode = "USD", Status = GiftCardStatus.Active,
            IssuedDate = now.AddDays(-7), ExpiryDate = now.AddYears(1),
            RecipientName = "Anonymous Gift",
        }, T);

        var txns = new[]
        {
            E(new PosGiftCardTransaction { PosGiftCardId = gc1.Id, TransactionType = "Issuance",   Amount = 50m,     BalanceAfter = 50m,    TransactionDate = gc1.IssuedDate,    Notes = "Gift card issued at POS" }, T),
            E(new PosGiftCardTransaction { PosGiftCardId = gc1.Id, TransactionType = "Redemption", Amount = -17.50m, BalanceAfter = 32.50m, TransactionDate = now.AddMonths(-1), Notes = "Partial redemption"      }, T),
        };

        return ([gc1, gc2], txns);
    }

    // 22. Loyalty Accounts + Transactions

    private static (LoyaltyAccount[] accounts, LoyaltyTransaction[] txns) SeedLoyaltyAccounts(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
        LoyaltyProgram program)
    {
        var now = DateTime.UtcNow;

        var acc1 = E(new LoyaltyAccount
        {
            ContactId = ContactId1, Tier = LoyaltyTier.Silver,
            PointsBalance = 1250m, LifetimePointsEarned = 1450m, LifetimePointsRedeemed = 200m,
            LastActivityDate = now.AddHours(-5),
        }, T);

        var acc2 = E(new LoyaltyAccount
        {
            ContactId = ContactId3, Tier = LoyaltyTier.Bronze,
            PointsBalance = 6m, LifetimePointsEarned = 6m, LifetimePointsRedeemed = 0m,
            LastActivityDate = now.AddHours(-2),
        }, T);

        var txns = new[]
        {
            E(new LoyaltyTransaction { LoyaltyAccountId = acc1.Id, TransactionType = LoyaltyTransactionType.Earned, Points = 5m, BalanceAfter = 1250m, Description = "Points earned on SO-2024-0001", TransactionDate = now.AddHours(-5) }, T),
            E(new LoyaltyTransaction { LoyaltyAccountId = acc2.Id, TransactionType = LoyaltyTransactionType.Earned, Points = 6m, BalanceAfter = 6m,    Description = "Points earned on SO-2024-0003", TransactionDate = now.AddHours(-2) }, T),
        };

        return ([acc1, acc2], txns);
    }

    // 23. Product Reviews

    private static (ProductReview[] reviews, ProductReviewImage[] images) SeedProductReviews(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T)
    {
        var now = DateTime.UtcNow;

        var rev1 = E(new ProductReview { ProductId = ItemId3, ContactId = ContactId2, ProductName = "Samsung Galaxy A54", Rating = 5, Title = "Excellent smartphone!",    Body = "Great camera, snappy performance.",            IsVerifiedPurchase = true, IsApproved = true, ReviewDate = now.AddDays(-2) }, T);
        var rev2 = E(new ProductReview { ProductId = ItemId4, ContactId = ContactId1, ProductName = "Nike Running Shoes",  Rating = 4, Title = "Comfortable and durable", Body = "Great for daily running. Slightly narrow fit.", IsVerifiedPurchase = true, IsApproved = true, ReviewDate = now.AddDays(-5) }, T);
        var rev3 = E(new ProductReview { ProductId = ItemId1, ContactId = ContactId3, ProductName = "Coca-Cola 330ml",     Rating = 5, Title = "Always refreshing",        Body = "Cold Coke on a hot day - perfect!",            IsVerifiedPurchase = true, IsApproved = true, ReviewDate = now.AddDays(-1) }, T);

        var images = new[]
        {
            E(new ProductReviewImage { ProductReviewId = rev1.Id, ImageUrl = "https://cdn.nexcore.com/reviews/samsung-a54-front.jpg", SortOrder = 1 }, T),
        };

        return ([rev1, rev2, rev3], images);
    }

    // 24. Store Menu + Sections

    private static (StoreMenu[] menus, StoreMenuSection[] sections) SeedStoreMenus(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
        PosStore store)
    {
        var beveragesCatId   = new Guid("60000001-0000-0000-0000-000000000001");
        var snacksCatId      = new Guid("60000001-0000-0000-0000-000000000002");
        var dairyCatId       = new Guid("60000001-0000-0000-0000-000000000003");
        var electronicsCatId = new Guid("60000001-0000-0000-0000-000000000004");

        var mainMenu = E(new StoreMenu
        {
            Name = "Main Store Menu",
            Description = "Full product catalogue for online ordering",
            IsActive = true, DisplayOrder = 1,
        }, T);

        var sections = new[]
        {
            E(new StoreMenuSection { StoreMenuId = mainMenu.Id, ItemCategoryId = beveragesCatId,   DisplayOrder = 1, IsActive = true }, T),
            E(new StoreMenuSection { StoreMenuId = mainMenu.Id, ItemCategoryId = snacksCatId,      DisplayOrder = 2, IsActive = true }, T),
            E(new StoreMenuSection { StoreMenuId = mainMenu.Id, ItemCategoryId = dairyCatId,       DisplayOrder = 3, IsActive = true }, T),
            E(new StoreMenuSection { StoreMenuId = mainMenu.Id, ItemCategoryId = electronicsCatId, DisplayOrder = 4, IsActive = true }, T),
        };

        return ([mainMenu], sections);
    }

    // 25. Wishlist + App Notifications

    private static WishlistItem[] SeedWishlistItems(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T) =>
    [
        E(new WishlistItem { ContactId = ContactId1, ProductId = ItemId3, ProductCode = "ELEC-SAM-A54", ProductName = "Samsung Galaxy A54", AddedAt = DateTime.UtcNow.AddDays(-3)  }, T),
        E(new WishlistItem { ContactId = ContactId3, ProductId = ItemId4, ProductCode = "SPT-NIKE-001", ProductName = "Nike Running Shoes",  AddedAt = DateTime.UtcNow.AddDays(-1)  }, T),
        E(new WishlistItem { ContactId = ContactId4, ProductId = ItemId1, ProductCode = "BEV-COKE-330", ProductName = "Coca-Cola 330ml",     AddedAt = DateTime.UtcNow.AddHours(-6) }, T),
    ];

    private static AppNotification[] SeedAppNotifications(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T)
    {
        var now = DateTime.UtcNow;
        return
        [
            E(new AppNotification { ContactId = ContactId3, Type = NotificationType.OrderStatusUpdate, Title = "Your order is on the way!",               Body = "Miguel is heading to you. ETA: 15 min.",        Channel = NotificationChannel.Push,  IsRead = false, IsSent = true, SentAt = now.AddMinutes(-30)                       }, T),
            E(new AppNotification { ContactId = ContactId1, Type = NotificationType.PromotionalOffer,  Title = "Weekend Flash Sale - 20% Off Electronics!", Body = "Shop now and save big on phones & more.",       Channel = NotificationChannel.Push,  IsRead = true,  IsSent = true, SentAt = now.AddDays(-1), ReadAt = now.AddHours(-20) }, T),
            E(new AppNotification { ContactId = ContactId2, Type = NotificationType.PaymentReceived,   Title = "Invoice INV-2024-0001 is ready",           Body = "Your invoice for $2,387.00 is due in 29 days.", Channel = NotificationChannel.InApp, IsRead = false, IsSent = true, SentAt = now.AddHours(-2)                          }, T),
        ];
    }
}
