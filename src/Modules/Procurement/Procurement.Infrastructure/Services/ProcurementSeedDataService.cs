using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Enums;
using Nexcore.SharedKernel.Helpers;
using Procurement.Domain.Entities;
using Procurement.Domain.Enums;
using Procurement.Infrastructure.Persistence;

namespace Procurement.Infrastructure.Services;

/// <summary>
/// Seeds comprehensive, interconnected sample Procurement data for a newly onboarded company.
///
/// Domain: Industrial Office Furniture manufacturer — purchases raw materials, IT and services.
/// The raw-material item IDs are derived with <see cref="CrossModuleGuid"/> (seq 1101–1112) so they
/// line up with the Inventory/Manufacturing seeds for the same company.
///
/// Vendors (7), each fully chained:
///   Requisition → RFQ (+ invited vendors + quotations) → Contract → Purchase Order
///   → Goods Receipt → Purchase Invoice (+ 3-way match) → Vendor Payment
///   → Purchase Return → Vendor Debit Note → Landed Cost
///
/// Every record is given distinct, realistic values (status, dates, amounts) so the data set
/// reads like a live tenant rather than copy-paste rows.
///
/// Runs AFTER <see cref="ProcurementInitializationService"/> (which seeds the vendor/procurement
/// categories, document sequences, approval workflows and settings this data references).
/// </summary>
public class ProcurementSeedDataService
{
    private readonly ProcurementDbContext _ctx;
    private readonly ILogger<ProcurementSeedDataService> _logger;

    public ProcurementSeedDataService(
        ProcurementDbContext ctx,
        ILogger<ProcurementSeedDataService> logger)
    {
        _ctx    = ctx;
        _logger = logger;
    }

    private Guid _c, _b, _bu, _u;
    private DateTime _now;

    /// <summary>Returns true if sample data was seeded, false if it already existed.</summary>
    public async Task<bool> SeedAsync(Guid companyId, Guid branchId, Guid businessUnitId, Guid userId)
    {
        if (await _ctx.Vendors.AnyAsync(v => v.CompanyId == companyId))
        {
            _logger.LogInformation("Procurement sample data already exists for Company {CompanyId}", companyId);
            return false;
        }

        _c = companyId; _b = branchId; _bu = businessUnitId; _u = userId;
        _now = DateTime.UtcNow;

        // Reference data seeded by ProcurementInitializationService.
        var vendorCats = await _ctx.VendorCategories.Where(x => x.CompanyId == companyId).ToListAsync();
        var procCats   = await _ctx.ProcurementCategories.Where(x => x.CompanyId == companyId).ToListAsync();
        var workflows  = await _ctx.ApprovalWorkflows.Where(x => x.CompanyId == companyId).ToListAsync();

        Guid? VendorCat(string code) => vendorCats.FirstOrDefault(x => x.Code == code)?.Id;
        Guid? ProcCat(string code)   => procCats.FirstOrDefault(x => x.Code == code)?.Id;
        var reqWorkflowId = workflows.FirstOrDefault(w => w.DocumentType == ApprovalDocumentType.PurchaseRequisition)?.Id;

        await using var tx = await _ctx.Database.BeginTransactionAsync();
        try
        {
            // ── Raw-material catalogue (mirrors Inventory seed seq 1101–1112) ───────
            CatalogItem Rm(int seq, string code, string desc, string uom, decimal price)
                => new(CrossModuleGuid.Derive(_c, seq), code, desc, uom, price);

            var cat = new Dictionary<int, CatalogItem>
            {
                [1101] = Rm(1101, "MFG-RM-STEEL-TUBE",     "Steel Structural Tube 40×40mm",       "PCS", 12.50m),
                [1102] = Rm(1102, "MFG-RM-FABRIC",         "Upholstery Mesh Fabric",              "MTR",  8.75m),
                [1103] = Rm(1103, "MFG-RM-FOAM",           "High-Density Seat Foam",              "KG",   6.20m),
                [1104] = Rm(1104, "MFG-RM-WOOD-PANEL",     "Solid Rubberwood Panel 1200×600",     "PCS", 18.00m),
                [1105] = Rm(1105, "MFG-RM-SCREWS",         "Screws & Fasteners Pack",             "PKT",  3.40m),
                [1106] = Rm(1106, "MFG-RM-PAINT",          "Powder Coat Paint (RAL 9005)",        "LTR", 14.00m),
                [1107] = Rm(1107, "MFG-RM-MDF",            "MDF Board 18mm",                      "PCS", 22.00m),
                [1108] = Rm(1108, "MFG-RM-HANDLES",        "Drawer Handle Set",                   "PCS",  4.80m),
                [1110] = Rm(1110, "MFG-RM-GAS-LIFT",       "Pneumatic Gas Lift Cylinder Class 4", "PCS",  9.50m),
                [1109] = Rm(1109, "MFG-RM-STEEL-SHEET",    "Cold-Rolled Steel Sheet 1.5mm",       "PCS", 26.00m),
                [1112] = Rm(1112, "MFG-RM-PARTICLE-BOARD", "Particle Board 16mm (Shelving)",      "PCS", 15.00m),

                // ── Fast-food (pizza shop) ingredients — mirror Inventory seq 1201–1218 ──
                [1201] = Rm(1201, "FF-RM-DOUGH",       "Pizza Dough Ball 250g",     "PCS",  25.00m),
                [1202] = Rm(1202, "FF-RM-MOZZ",        "Mozzarella Cheese",         "KG",  900.00m),
                [1203] = Rm(1203, "FF-RM-PIZZA-SAUCE", "Pizza Tomato Sauce",        "L",   350.00m),
                [1204] = Rm(1204, "FF-RM-PEPPERONI",   "Pepperoni Topping",         "KG", 1400.00m),
                [1205] = Rm(1205, "FF-RM-CHK-TIKKA",   "Chicken Tikka Topping",     "KG", 1200.00m),
                [1206] = Rm(1206, "FF-RM-VEG-MIX",     "Veg Topping Mix",           "KG",  300.00m),
                [1207] = Rm(1207, "FF-RM-BEEF-PATTY",  "Beef Patty 150g",           "PCS",  80.00m),
                [1208] = Rm(1208, "FF-RM-CHK-FILLET",  "Chicken Fillet Patty 150g", "PCS",  90.00m),
                [1209] = Rm(1209, "FF-RM-BUN",         "Sesame Burger Bun",         "PCS",  30.00m),
                [1210] = Rm(1210, "FF-RM-CHEDDAR",     "Cheddar Cheese Slice",      "PCS",  20.00m),
                [1211] = Rm(1211, "FF-RM-LETTUCE",     "Iceberg Lettuce",           "KG",  150.00m),
                [1212] = Rm(1212, "FF-RM-TOMATO",      "Fresh Tomato",              "KG",  120.00m),
                [1213] = Rm(1213, "FF-RM-MAYO",        "Burger Sauce / Mayo",       "L",   400.00m),
                [1214] = Rm(1214, "FF-RM-POTATO",      "Potato (Fry Cut)",          "KG",  120.00m),
                [1215] = Rm(1215, "FF-RM-FRY-OIL",     "Frying Oil",                "L",   450.00m),
                [1216] = Rm(1216, "FF-RM-PIZZA-BOX",   "Pizza Box",                 "PCS",  25.00m),
                [1217] = Rm(1217, "FF-RM-BURGER-WRAP", "Burger Wrap / Box",         "PCS",   8.00m),
                [1218] = Rm(1218, "FF-RM-FRIES-CUP",   "Fries Cup",                 "PCS",   6.00m),
            };
            CatalogItem Inv(int seq) => cat[seq];

            // ── Vendors + their supplied catalogue & order lines ────────────────────
            var defs = new List<VendorDef>
            {
                new("V-00001", "SteelCo Industrial Supplies LLC", "SteelCo", VendorCat("RAW"),
                    "procurement@steelco-supplies.example", "+1-412-555-0110", "Pittsburgh", "USA", PaymentTerms.Net45, 10, 250_000m, true,
                    [ new(Inv(1101), 400, 12.50m), new(Inv(1109), 200, 26.00m), new(Inv(1112), 150, 15.00m) ]),
                new("V-00002", "Premium Upholstery Fabrics Ltd", "PremiumFab", VendorCat("RAW"),
                    "sales@premiumfabrics.example", "+44-161-555-0182", "Manchester", "UK", PaymentTerms.Net30, 14, 120_000m, true,
                    [ new(Inv(1102), 600, 8.75m), new(Inv(1103), 250, 6.20m) ]),
                new("V-00003", "TimberWorks Panels & Boards Co", "TimberWorks", VendorCat("RAW"),
                    "orders@timberworks.example", "+1-503-555-0143", "Portland", "USA", PaymentTerms.Net30, 12, 90_000m, false,
                    [ new(Inv(1104), 300, 18.00m), new(Inv(1107), 180, 22.00m) ]),
                new("V-00004", "FastFix Fasteners & Hardware Inc", "FastFix", VendorCat("GOODS"),
                    "b2b@fastfix-hardware.example", "+1-216-555-0177", "Cleveland", "USA", PaymentTerms.Net15, 5, 60_000m, false,
                    [ new(Inv(1105), 500, 3.40m), new(Inv(1108), 400, 4.80m), new(Inv(1110), 200, 9.50m) ]),
                new("V-00005", "ColorCoat Paints & Coatings", "ColorCoat", VendorCat("RAW"),
                    "supply@colorcoat.example", "+49-211-555-0166", "Düsseldorf", "Germany", PaymentTerms.Net30, 9, 75_000m, false,
                    [ new(Inv(1106), 300, 14.00m) ]),
                new("V-00006", "TechSphere IT Solutions", "TechSphere", VendorCat("IT"),
                    "accounts@techsphere-it.example", "+1-408-555-0190", "San Jose", "USA", PaymentTerms.Net30, 7, 100_000m, false,
                    [ new(new(null, "IT-LAPTOP-PRO14", "Business Laptop 14\" i7/16GB/512GB", "PCS", 1_200.00m), 12, 1_200.00m),
                      new(new(null, "IT-LIC-OFFICE365", "Microsoft 365 Business Licence (annual)", "EA", 99.00m), 30, 99.00m) ]),
                new("V-00007", "SecureGuard Facility Services", "SecureGuard", VendorCat("SERVICE"),
                    "contracts@secureguard.example", "+1-312-555-0125", "Chicago", "USA", PaymentTerms.Net30, 3, 50_000m, false,
                    [ new(new(null, "SVC-CLEAN-MONTHLY", "Monthly Office Cleaning Service", "MTH", 800.00m), 12, 800.00m),
                      new(new(null, "SVC-HVAC-MAINT", "Quarterly HVAC Preventive Maintenance", "EA", 1_500.00m), 4, 1_500.00m) ]),

                // ── Pizza-shop ingredient & beverage suppliers ──────────────────────────
                new("V-00008", "FreshFields Food Supplies", "FreshFields", VendorCat("RAW"),
                    "orders@freshfields.example", "+92-42-555-0188", "Lahore", "Pakistan", PaymentTerms.Net15, 2, 500_000m, true,
                    [ new(Inv(1201), 400, 25.00m),  new(Inv(1202), 50, 900.00m),  new(Inv(1203), 40, 350.00m),
                      new(Inv(1204), 30, 1_400.00m), new(Inv(1205), 30, 1_200.00m), new(Inv(1206), 40, 300.00m),
                      new(Inv(1207), 300, 80.00m),  new(Inv(1208), 300, 90.00m),  new(Inv(1209), 400, 30.00m),
                      new(Inv(1210), 400, 20.00m),  new(Inv(1211), 30, 150.00m),  new(Inv(1212), 30, 120.00m),
                      new(Inv(1213), 40, 400.00m),  new(Inv(1214), 200, 120.00m), new(Inv(1215), 60, 450.00m) ]),
                new("V-00009", "CoolBev Beverages & Packaging", "CoolBev", VendorCat("GOODS"),
                    "sales@coolbev.example", "+92-21-555-0199", "Karachi", "Pakistan", PaymentTerms.Net15, 2, 300_000m, false,
                    [ new(new(null, "BEV-PEPSI-500",  "Pepsi 500ml Bottle",      "PCS", 80.00m),  500, 80.00m),
                      new(new(null, "BEV-COKE-500",   "Coca-Cola 500ml Bottle",  "PCS", 85.00m),  500, 85.00m),
                      new(new(null, "BEV-PEPSI-15L",  "Pepsi 1.5 Litre",         "PCS", 130.00m), 300, 130.00m),
                      new(Inv(1216), 1_000, 25.00m), new(Inv(1217), 1_000, 8.00m), new(Inv(1218), 1_000, 6.00m) ]),
            };

            // ════════════════════════════════════════════════════════════════════════
            // 1. Vendor master (vendor + contacts + addresses + bank account + documents)
            // ════════════════════════════════════════════════════════════════════════
            var vendors = new Vendor[defs.Count];
            for (int i = 0; i < defs.Count; i++)
            {
                var d = defs[i];
                var v = Track(new Vendor
                {
                    VendorNumber          = d.Number,
                    Name                  = d.Name,
                    ShortName             = d.ShortName,
                    Type                  = VendorType.Company,
                    Status                = i == 6 ? VendorStatus.Active : VendorStatus.Active,
                    OnboardingStatus      = VendorOnboardingStatus.Approved,
                    TaxRegistrationNumber = $"TRN-{1000 + i}-{d.ShortName.ToUpperInvariant()}",
                    CompanyRegistrationNumber = $"CRN-{500000 + i * 137}",
                    VATNumber             = $"VAT-{d.Country[..2].ToUpperInvariant()}{900000 + i}",
                    Website               = $"https://www.{d.ShortName.ToLowerInvariant()}.example",
                    VendorCategoryId      = d.CategoryId,
                    PrimaryEmail          = d.Email,
                    PrimaryPhone          = d.Phone,
                    CurrencyCode          = "USD",
                    PaymentTerms          = d.PaymentTerms,
                    LeadTimeDays          = d.LeadTimeDays,
                    CreditLimit           = d.CreditLimit,
                    IsPreferredVendor     = d.Preferred,
                    SubledgerType         = SubledgerType.AccountsPayable,
                    OverallRating         = Math.Round(4.6m - i * 0.18m, 2),
                    OnTimeDeliveryRate    = Math.Round(98m - i * 2.1m, 2),
                    QualityScore          = Math.Round(97m - i * 1.7m, 2),
                    Notes                 = $"Strategic supplier for {d.ShortName} category."
                });
                _ctx.Vendors.Add(v);
                vendors[i] = v;

                // Contacts — primary purchasing contact + (for first few) an AP contact
                _ctx.VendorContacts.Add(Track(new VendorContact
                {
                    VendorId  = v.Id,
                    FirstName = ContactFirst[i % ContactFirst.Length],
                    LastName  = ContactLast[i % ContactLast.Length],
                    JobTitle  = "Account Manager",
                    Department= "Sales",
                    Email     = $"sales@{d.ShortName.ToLowerInvariant()}.example",
                    Phone     = d.Phone,
                    Mobile    = $"+1-555-01{20 + i:00}",
                    IsPrimary = true
                }));
                if (i < 4)
                    _ctx.VendorContacts.Add(Track(new VendorContact
                    {
                        VendorId  = v.Id,
                        FirstName = ContactFirst[(i + 3) % ContactFirst.Length],
                        LastName  = ContactLast[(i + 2) % ContactLast.Length],
                        JobTitle  = "Accounts Receivable",
                        Department= "Finance",
                        Email     = $"ar@{d.ShortName.ToLowerInvariant()}.example",
                        Phone     = d.Phone,
                        IsPrimary = false
                    }));

                // Addresses — registered HQ + (for first few) a dispatch warehouse
                _ctx.VendorAddresses.Add(Track(new VendorAddress
                {
                    VendorId    = v.Id,
                    AddressType = VendorAddressType.Both,
                    Street      = $"{100 + i * 23} Industrial Parkway",
                    City        = d.City,
                    State       = d.Country == "USA" ? UsState[i % UsState.Length] : null,
                    PostalCode  = $"{10000 + i * 1111}",
                    Country     = d.Country,
                    IsPrimary   = true
                }));
                if (i < 3)
                    _ctx.VendorAddresses.Add(Track(new VendorAddress
                    {
                        VendorId    = v.Id,
                        AddressType = VendorAddressType.Shipping,
                        Street      = $"{200 + i * 17} Logistics Drive, Dock {i + 1}",
                        City        = d.City,
                        Country     = d.Country,
                        IsPrimary   = false
                    }));

                // Bank account
                _ctx.VendorBankAccounts.Add(Track(new VendorBankAccount
                {
                    VendorId          = v.Id,
                    BankName          = BankNames[i % BankNames.Length],
                    BranchName        = $"{d.City} Commercial Branch",
                    AccountHolderName = d.Name,
                    AccountNumber     = $"{40010000 + i * 7321}",
                    IBAN              = $"US{20 + i}MOBI{60000000 + i * 13579:D8}",
                    SWIFTCode         = $"{d.ShortName[..4].ToUpperInvariant()}US33",
                    CurrencyCode      = "USD",
                    IsDefault         = true,
                    IsVerified        = i < 5,
                    VerifiedAt        = i < 5 ? _now.AddDays(-200 + i * 10) : null,
                    VerifiedByUserId  = i < 5 ? _u : null
                }));

                // Documents — trade licence (+ ISO for manufacturers)
                _ctx.VendorDocuments.Add(Track(new VendorDocument
                {
                    VendorId       = v.Id,
                    DocumentType   = VendorDocumentType.TradeLicense,
                    DocumentName   = "Trade Licence",
                    DocumentNumber = $"TL-{2024}-{3000 + i}",
                    IssuedBy       = $"{d.City} Chamber of Commerce",
                    IssuedAt       = _now.AddDays(-400 + i * 5),
                    ExpiryDate     = _now.AddDays(330 - i * 40),
                    Status         = VendorDocumentStatus.Active,
                    IsVerified     = true,
                    VerifiedByUserId = _u,
                    VerifiedAt     = _now.AddDays(-180)
                }));
                _ctx.VendorDocuments.Add(Track(new VendorDocument
                {
                    VendorId       = v.Id,
                    DocumentType   = i < 5 ? VendorDocumentType.ISO9001 : VendorDocumentType.InsuranceCertificate,
                    DocumentName   = i < 5 ? "ISO 9001:2015 Certificate" : "Liability Insurance Certificate",
                    DocumentNumber = $"DOC-{4000 + i}",
                    IssuedBy       = i < 5 ? "Bureau Veritas" : "AIG Insurance",
                    IssuedAt       = _now.AddDays(-300 + i * 7),
                    ExpiryDate     = _now.AddDays(i == 2 ? 20 : 300 - i * 25), // one nearly expiring (vendor #3)
                    Status         = VendorDocumentStatus.Active,
                    IsVerified     = i != 2
                }));
            }
            await _ctx.SaveChangesAsync();
            _logger.LogInformation("Seeded {N} vendors with contacts, addresses, banks and documents", vendors.Length);

            // ════════════════════════════════════════════════════════════════════════
            // 2. Pricelists (+ items), Approved Vendor List, Vendor Performance
            // ════════════════════════════════════════════════════════════════════════
            for (int i = 0; i < defs.Count; i++)
            {
                var d = defs[i];
                var pl = Track(new VendorPricelist
                {
                    VendorId     = vendors[i].Id,
                    Name         = $"{d.ShortName} Standard Pricelist {_now.Year}",
                    Description  = "Negotiated list pricing, valid for the current fiscal year.",
                    CurrencyCode = "USD",
                    ValidFrom    = new DateTime(_now.Year, 1, 1),
                    ValidTo      = new DateTime(_now.Year, 12, 31),
                    IsDefault    = true
                });
                _ctx.VendorPricelists.Add(pl);

                foreach (var line in d.Supplies)
                {
                    _ctx.VendorPricelistItems.Add(Track(new VendorPricelistItem
                    {
                        PricelistId       = pl.Id,
                        ItemId            = line.Item.ItemId,
                        ItemCode          = line.Item.Code,
                        ItemDescription   = line.Item.Desc,
                        UnitOfMeasureName = line.Item.Uom,
                        MinimumQuantity   = 10,
                        UnitPrice         = line.UnitPrice,
                        DiscountPercent   = i % 3 == 0 ? 2.5m : 0m,
                        LeadTimeDays      = d.LeadTimeDays
                    }));

                    // Approved Vendor List entry for each supplied item
                    _ctx.ApprovedVendorLists.Add(Track(new ApprovedVendorList
                    {
                        VendorId              = vendors[i].Id,
                        ItemId                = line.Item.ItemId,
                        ItemCode              = line.Item.Code,
                        ItemDescription       = line.Item.Desc,
                        ProcurementCategoryId = AvlCategory(d, ProcCat),
                        ValidFrom             = _now.AddDays(-300),
                        ValidTo               = _now.AddDays(400),
                        IsPreferred           = d.Preferred,
                        DefaultUnitPrice      = line.UnitPrice,
                        CurrencyCode          = "USD",
                        LeadTimeDays          = d.LeadTimeDays,
                        MinimumOrderQuantity  = 10,
                        RequiresQualityInspection = d.CategoryId == VendorCat("RAW"),
                        ApprovedByUserId      = _u,
                        ApprovedAt            = _now.AddDays(-290)
                    }));
                }

                // Vendor performance — last completed quarter
                _ctx.VendorPerformances.Add(Track(new VendorPerformance
                {
                    VendorId              = vendors[i].Id,
                    PeriodFrom            = _now.AddDays(-120),
                    PeriodTo              = _now.AddDays(-30),
                    OnTimeDeliveryRate    = Math.Round(98m - i * 2.1m, 2),
                    QualityScore          = Math.Round(97m - i * 1.7m, 2),
                    PriceComplianceRate   = Math.Round(99m - i * 1.2m, 2),
                    ResponsivenessScore   = Math.Round(95m - i * 2.4m, 2),
                    DocumentAccuracyScore = Math.Round(96m - i * 1.5m, 2),
                    OverallRating         = Math.Round(4.6m - i * 0.18m, 2),
                    TotalOrders           = 14 - i,
                    LateDeliveries        = i,
                    QualityRejections     = i / 2,
                    InvoiceDiscrepancies  = i % 3,
                    TotalPurchaseValue    = Math.Round(85_000m - i * 7_500m, 2),
                    EvaluatedByUserId     = _u,
                    EvaluatedAt           = _now.AddDays(-25),
                    Comments              = i == 0 ? "Top-tier supplier; consistently exceeds SLAs."
                                          : i >= 5 ? "Reliable; minor responsiveness gaps under review."
                                          : "Solid performer; meeting contractual targets."
                }));
            }
            await _ctx.SaveChangesAsync();
            _logger.LogInformation("Seeded pricelists, approved-vendor-list entries and performance records");

            // ════════════════════════════════════════════════════════════════════════
            // 3. Purchase Contracts (6 framework agreements)
            // ════════════════════════════════════════════════════════════════════════
            var contracts = new PurchaseContract[6];
            for (int i = 0; i < 6; i++)
            {
                var d = defs[i];
                var maxValue = Math.Round(180_000m - i * 18_000m, 2);
                var contract = Track(new PurchaseContract
                {
                    ContractNumber       = $"PC-{_now.Year}-{i + 1:00000}",
                    Title                = $"{d.ShortName} {_now.Year} Supply Framework Agreement",
                    Description          = $"Annual framework agreement covering {d.ShortName} catalogue items.",
                    VendorId             = vendors[i].Id,
                    VendorName           = d.Name,
                    ContractType         = i == 5 ? PurchaseContractType.ServiceAgreement : PurchaseContractType.FrameworkAgreement,
                    Status               = i == 4 ? PurchaseContractStatus.UnderReview : PurchaseContractStatus.Active,
                    StartDate            = new DateTime(_now.Year, 1, 1),
                    EndDate              = new DateTime(_now.Year, 12, 31),
                    SignedAt             = i == 4 ? null : _now.AddDays(-340 + i * 3),
                    AutoRenew            = i % 2 == 0,
                    RenewalNoticeDays    = 60,
                    RenewalDurationMonths= 12,
                    CurrencyCode         = "USD",
                    MaximumContractValue = maxValue,
                    CommittedValue       = Math.Round(maxValue * 0.6m, 2),
                    UsedValue            = Math.Round(maxValue * (0.15m + i * 0.05m), 2),
                    PaymentTerms         = d.PaymentTerms,
                    Incoterm             = i < 5 ? Incoterm.DAP : null,
                    ApprovedByUserId     = i == 4 ? null : _u,
                    ApprovedAt           = i == 4 ? null : _now.AddDays(-339 + i * 3),
                    TermsAndConditions   = "Standard MSA terms. Prices fixed for contract duration; volume rebates per Schedule B."
                });
                _ctx.PurchaseContracts.Add(contract);
                contracts[i] = contract;

                int ln = 1;
                foreach (var line in d.Supplies)
                {
                    _ctx.PurchaseContractLines.Add(Track(new PurchaseContractLine
                    {
                        ContractId            = contract.Id,
                        LineNumber            = ln++,
                        ItemId                = line.Item.ItemId,
                        ItemCode              = line.Item.Code,
                        ItemDescription       = line.Item.Desc,
                        ProcurementCategoryId = AvlCategory(d, ProcCat),
                        UnitOfMeasureName     = line.Item.Uom,
                        MinimumQuantity       = 50,
                        MaximumQuantity       = 5000,
                        CommittedQuantity     = 1000,
                        OrderedQuantity       = Math.Round(200m + i * 25m, 2),
                        UnitPrice             = line.UnitPrice,
                        DiscountPercent       = 2.5m,
                        ValidFrom             = new DateTime(_now.Year, 1, 1),
                        ValidTo               = new DateTime(_now.Year, 12, 31)
                    }));
                }
            }
            await _ctx.SaveChangesAsync();
            _logger.LogInformation("Seeded {N} purchase contracts", contracts.Length);

            // ════════════════════════════════════════════════════════════════════════
            // 4. Per-vendor sourcing → ordering → receipt → invoice → payment chain
            // ════════════════════════════════════════════════════════════════════════
            // NOTE: one entry per vendor in `defs` (indexed by i). Entries 7-8 cover the
            // two fast-food suppliers (V-00008 FreshFields, V-00009 CoolBev); the first
            // seven are unchanged so the existing vendors seed exactly as before.
            var reqStatuses = new[] { RequisitionStatus.Fulfilled, RequisitionStatus.Fulfilled, RequisitionStatus.Fulfilled,
                                      RequisitionStatus.PartiallyFulfilled, RequisitionStatus.Approved, RequisitionStatus.Submitted, RequisitionStatus.UnderApproval,
                                      RequisitionStatus.Fulfilled, RequisitionStatus.Fulfilled };
            var poStatuses  = new[] { PurchaseOrderStatus.FullyInvoiced, PurchaseOrderStatus.FullyInvoiced, PurchaseOrderStatus.FullyReceived,
                                      PurchaseOrderStatus.PartiallyReceived, PurchaseOrderStatus.FullyReceived, PurchaseOrderStatus.FullyInvoiced, PurchaseOrderStatus.FullyReceived,
                                      PurchaseOrderStatus.FullyInvoiced, PurchaseOrderStatus.FullyReceived };
            var invStatuses = new[] { PurchaseInvoiceStatus.FullyPaid, PurchaseInvoiceStatus.FullyPaid, PurchaseInvoiceStatus.Posted,
                                      PurchaseInvoiceStatus.PartiallyPaid, PurchaseInvoiceStatus.OnHold, PurchaseInvoiceStatus.Posted, PurchaseInvoiceStatus.FullyPaid,
                                      PurchaseInvoiceStatus.FullyPaid, PurchaseInvoiceStatus.Posted };
            var payStatuses = new[] { VendorPaymentStatus.Cleared, VendorPaymentStatus.Cleared, VendorPaymentStatus.Draft,
                                      VendorPaymentStatus.Processing, VendorPaymentStatus.Draft, VendorPaymentStatus.Sent, VendorPaymentStatus.Cleared,
                                      VendorPaymentStatus.Cleared, VendorPaymentStatus.Draft };
            var priorities  = new[] { RequisitionPriority.High, RequisitionPriority.Normal, RequisitionPriority.Normal,
                                      RequisitionPriority.Urgent, RequisitionPriority.Normal, RequisitionPriority.Low, RequisitionPriority.High,
                                      RequisitionPriority.High, RequisitionPriority.Normal };

            // Accumulators for downstream stages (returns / debit notes / landed costs)
            var poList     = new PurchaseOrder[defs.Count];
            var poLineList = new List<PurchaseOrderLine[]>();
            var grList     = new GoodsReceipt[defs.Count];
            var grLineList = new List<GoodsReceiptLine[]>();
            var invList    = new PurchaseInvoice[defs.Count];

            for (int i = 0; i < defs.Count; i++)
            {
                var d        = defs[i];
                var lines    = d.Supplies;
                var baseDate = _now.AddDays(-100 + i * 11);
                var vendor   = vendors[i];
                var procCatId = AvlCategory(d, ProcCat);

                // ── Requisition + lines ─────────────────────────────────────────────
                var estTotal = lines.Sum(l => Money(l.Quantity, l.UnitPrice, 5m).total);
                var req = Track(new PurchaseRequisition
                {
                    RequisitionNumber    = $"PR-{_now.Year}-{i + 1:00000}",
                    Title                = $"{d.ShortName} replenishment — {Departments[i % Departments.Length]}",
                    Description          = $"Scheduled replenishment of {d.ShortName} items for upcoming production runs.",
                    RequestedByUserId    = _u,
                    RequestedByName      = Requesters[i % Requesters.Length],
                    DepartmentName       = Departments[i % Departments.Length],
                    RequestDate          = baseDate,
                    RequiredByDate       = baseDate.AddDays(d.LeadTimeDays + 7),
                    SubmittedAt          = baseDate.AddDays(1),
                    ApprovedAt           = reqStatuses[i] is RequisitionStatus.Submitted or RequisitionStatus.UnderApproval ? null : baseDate.AddDays(2),
                    Status               = reqStatuses[i],
                    Priority             = priorities[i],
                    SuggestedVendorId    = vendor.Id,
                    CurrencyCode         = "USD",
                    EstimatedTotalAmount = Math.Round(estTotal, 2),
                    IsBudgetChecked      = true,
                    IsBudgetAvailable    = true
                });
                _ctx.PurchaseRequisitions.Add(req);

                int rln = 1;
                foreach (var l in lines)
                    _ctx.PurchaseRequisitionLines.Add(Track(new PurchaseRequisitionLine
                    {
                        RequisitionId         = req.Id,
                        LineNumber            = rln++,
                        ItemId                = l.Item.ItemId,
                        ItemCode              = l.Item.Code,
                        ItemDescription       = l.Item.Desc,
                        Quantity              = l.Quantity,
                        UnitOfMeasureName     = l.Item.Uom,
                        EstimatedUnitPrice    = l.UnitPrice,
                        EstimatedTotalPrice   = Math.Round(l.Quantity * l.UnitPrice, 2),
                        ProcurementCategoryId = procCatId,
                        RequiredByDate        = baseDate.AddDays(d.LeadTimeDays + 7),
                        LineStatus            = reqStatuses[i] == RequisitionStatus.Fulfilled ? RequisitionLineStatus.Fulfilled : RequisitionLineStatus.Open,
                        SuggestedVendorId     = vendor.Id
                    }));

                // Approval record for the requisition
                _ctx.ProcurementApprovals.Add(Track(new ProcurementApproval
                {
                    DocumentType = ApprovalDocumentType.PurchaseRequisition,
                    DocumentId   = req.Id,
                    WorkflowId   = reqWorkflowId,
                    StepNumber   = 1,
                    StepName     = "Department Head Approval",
                    ApproverId   = _u,
                    ApproverName = "Procurement Manager",
                    Status       = reqStatuses[i] is RequisitionStatus.Submitted or RequisitionStatus.UnderApproval
                                       ? ProcurementApprovalStatus.Pending : ProcurementApprovalStatus.Approved,
                    Action       = reqStatuses[i] is RequisitionStatus.Submitted or RequisitionStatus.UnderApproval
                                       ? null : ProcurementApprovalAction.Approved,
                    AssignedAt   = baseDate.AddDays(1),
                    DueDate      = baseDate.AddDays(4),
                    ActionDate   = reqStatuses[i] is RequisitionStatus.Submitted or RequisitionStatus.UnderApproval ? null : baseDate.AddDays(2),
                    Comments     = "Approved against Q-plan budget."
                }));

                // ── RFQ + lines + invited vendors + quotations ──────────────────────
                var rfq = Track(new RequestForQuotation
                {
                    RFQNumber             = $"RFQ-{_now.Year}-{i + 1:00000}",
                    Title                 = $"RFQ — {d.ShortName} supply",
                    Description           = "Request for competitive quotations against the attached specification.",
                    RequisitionId         = req.Id,
                    IssueDate             = baseDate.AddDays(2),
                    SubmissionDeadline    = baseDate.AddDays(9),
                    QuotationValidityDate = baseDate.AddDays(39),
                    AwardedAt             = baseDate.AddDays(11),
                    Status                = RFQStatus.Awarded,
                    CurrencyCode          = "USD",
                    RequiredDeliveryDate  = baseDate.AddDays(d.LeadTimeDays + 14),
                    TermsAndConditions    = "Quotations to include unit price, lead time and warranty terms.",
                    EvaluationCriteria    = "70% commercial / 30% technical."
                });
                _ctx.RequestForQuotations.Add(rfq);

                var rfqLines = new RFQLine[lines.Count];
                for (int k = 0; k < lines.Count; k++)
                {
                    var l = lines[k];
                    var rfqLine = Track(new RFQLine
                    {
                        RFQId                 = rfq.Id,
                        LineNumber            = k + 1,
                        ItemId                = l.Item.ItemId,
                        ItemCode              = l.Item.Code,
                        ItemDescription       = l.Item.Desc,
                        Quantity              = l.Quantity,
                        UnitOfMeasureName     = l.Item.Uom,
                        EstimatedUnitPrice    = l.UnitPrice,
                        ProcurementCategoryId = procCatId,
                        RequiredDeliveryDate  = baseDate.AddDays(d.LeadTimeDays + 14),
                        Specifications        = "Per attached technical datasheet rev. A."
                    });
                    _ctx.RFQLines.Add(rfqLine);
                    rfqLines[k] = rfqLine;
                }

                // Invite the winning vendor plus two competitors (next vendors round-robin)
                var invitee = new[] { i, (i + 1) % defs.Count, (i + 2) % defs.Count };
                var quotations = new List<VendorQuotation>();
                for (int q = 0; q < invitee.Length; q++)
                {
                    int vi = invitee[q];
                    bool isWinner = q == 0;
                    _ctx.RFQVendors.Add(Track(new RFQVendor
                    {
                        RFQId            = rfq.Id,
                        VendorId         = vendors[vi].Id,
                        InvitedAt        = baseDate.AddDays(2),
                        ResponseDeadline = baseDate.AddDays(9),
                        AcknowledgedAt   = baseDate.AddDays(3),
                        RespondedAt      = q < 2 ? baseDate.AddDays(6 + q) : null,
                        Status           = q < 2 ? RFQVendorStatus.Responded : RFQVendorStatus.NoResponse
                    }));

                    if (q >= 2) continue; // only first two respond with quotations

                    // Winner quotes the agreed price; competitor quotes a bit higher
                    decimal factor = isWinner ? 1.00m : 1.06m + q * 0.01m;
                    var quotation = Track(new VendorQuotation
                    {
                        QuotationNumber          = $"VQ-{_now.Year}-{i + 1:00}{q + 1}",
                        VendorQuotationReference = $"{vendors[vi].ShortNameOrNumber()}-Q{1000 + i}",
                        RFQId                    = rfq.Id,
                        VendorId                 = vendors[vi].Id,
                        SubmissionDate           = baseDate.AddDays(6 + q),
                        ValidUntil               = baseDate.AddDays(39),
                        Status                   = isWinner ? QuotationStatus.Accepted : QuotationStatus.Rejected,
                        CurrencyCode             = "USD",
                        PaymentTerms             = defs[vi].PaymentTerms,
                        Incoterm                 = Incoterm.DAP,
                        DeliveryLeadTimeDays     = defs[vi].LeadTimeDays,
                        TechnicalScore           = Math.Round(isWinner ? 92m : 84m - q, 2),
                        CommercialScore          = Math.Round(isWinner ? 95m : 80m - q * 2, 2),
                        OverallScore             = Math.Round(isWinner ? 94m : 81m - q, 2),
                        IsRecommended            = isWinner,
                        RejectionReason          = isWinner ? null : "Higher unit price than awarded supplier."
                    });
                    _ctx.VendorQuotations.Add(quotation);
                    quotations.Add(quotation);

                    decimal qSub = 0, qTax = 0, qTot = 0;
                    for (int k = 0; k < lines.Count; k++)
                    {
                        var l = lines[k];
                        var price = Math.Round(l.UnitPrice * factor, 4);
                        var m = Money(l.Quantity, price, 5m);
                        qSub += m.sub; qTax += m.tax; qTot += m.total;
                        _ctx.VendorQuotationLines.Add(Track(new VendorQuotationLine
                        {
                            QuotationId       = quotation.Id,
                            RFQLineId         = rfqLines[k].Id,
                            LineNumber        = k + 1,
                            ItemId            = l.Item.ItemId,
                            ItemCode          = l.Item.Code,
                            ItemDescription   = l.Item.Desc,
                            Quantity          = l.Quantity,
                            UnitOfMeasureName = l.Item.Uom,
                            UnitPrice         = price,
                            TaxPercent        = 5m,
                            TaxAmount         = m.tax,
                            SubTotal          = m.sub,
                            TotalPrice        = m.total,
                            PromisedDeliveryDate = baseDate.AddDays(defs[vi].LeadTimeDays + 10),
                            LeadTimeDays      = defs[vi].LeadTimeDays
                        }));
                    }
                    quotation.SubTotalAmount = Math.Round(qSub, 2);
                    quotation.TaxAmount      = Math.Round(qTax, 2);
                    quotation.TotalAmount    = Math.Round(qTot, 2);
                }
                var winningQuotation = quotations.FirstOrDefault(x => x.Status == QuotationStatus.Accepted);

                // ── Purchase Order + lines + approvals + amendment ──────────────────
                var orderDate = baseDate.AddDays(12);
                var po = Track(new PurchaseOrder
                {
                    OrderNumber          = $"PO-{_now.Year}-{i + 1:00000}",
                    VendorId             = vendor.Id,
                    VendorName           = d.Name,
                    VendorReference      = $"SO-{vendor.ShortNameOrNumber()}-{7000 + i}",
                    QuotationId          = winningQuotation?.Id,
                    RequisitionId        = req.Id,
                    ContractId           = i < 6 ? contracts[i].Id : null,
                    OrderDate            = orderDate,
                    ExpectedDeliveryDate = orderDate.AddDays(d.LeadTimeDays),
                    ConfirmedDeliveryDate= orderDate.AddDays(d.LeadTimeDays + 1),
                    SentToVendorAt       = orderDate.AddDays(1),
                    AcknowledgedAt       = orderDate.AddDays(2),
                    Status               = poStatuses[i],
                    DeliveryStreet       = "1 Production Way",
                    DeliveryCity         = "Detroit",
                    DeliveryState        = "MI",
                    DeliveryPostalCode   = "48201",
                    DeliveryCountry      = "USA",
                    CurrencyCode         = "USD",
                    PaymentTerms         = d.PaymentTerms,
                    Incoterm             = Incoterm.DAP,
                    IncotermLocation     = "Detroit Plant",
                    ApprovedByUserId     = _u,
                    ApprovedAt           = orderDate.AddDays(1),
                    TermsAndConditions   = "Delivery to plant goods-in. Pallets returnable."
                });
                _ctx.PurchaseOrders.Add(po);
                poList[i] = po;

                bool partial = poStatuses[i] == PurchaseOrderStatus.PartiallyReceived;
                bool invoiced = poStatuses[i] == PurchaseOrderStatus.FullyInvoiced;
                decimal poSub = 0, poTax = 0, poTot = 0;
                var poLines = new PurchaseOrderLine[lines.Count];
                for (int k = 0; k < lines.Count; k++)
                {
                    var l   = lines[k];
                    var m   = Money(l.Quantity, l.UnitPrice, 5m);
                    poSub += m.sub; poTax += m.tax; poTot += m.total;
                    var received = partial ? Math.Round(l.Quantity * 0.6m, 4) : l.Quantity;
                    var rejected = k == 0 && (i == 3 || i == 4) ? Math.Round(l.Quantity * 0.02m, 4) : 0m;
                    var accepted = received - rejected;
                    var poLine = Track(new PurchaseOrderLine
                    {
                        OrderId               = po.Id,
                        LineNumber            = k + 1,
                        ItemId                = l.Item.ItemId,
                        ItemCode              = l.Item.Code,
                        ItemDescription       = l.Item.Desc,
                        Quantity              = l.Quantity,
                        UnitOfMeasureName     = l.Item.Uom,
                        UnitPrice             = l.UnitPrice,
                        TaxPercent            = 5m,
                        TaxAmount             = m.tax,
                        SubTotal              = m.sub,
                        TotalPrice            = m.total,
                        ProcurementCategoryId = procCatId,
                        ExpectedDeliveryDate  = orderDate.AddDays(d.LeadTimeDays),
                        QuantityReceived      = received,
                        QuantityAccepted      = accepted,
                        QuantityRejected      = rejected,
                        QuantityInvoiced      = invoiced ? l.Quantity : 0m,
                        LineStatus            = partial ? PurchaseOrderLineStatus.PartiallyReceived : PurchaseOrderLineStatus.FullyReceived
                    });
                    _ctx.PurchaseOrderLines.Add(poLine);
                    poLines[k] = poLine;
                }
                poLineList.Add(poLines);

                po.SubTotalAmount    = Math.Round(poSub, 2);
                po.TaxAmount         = Math.Round(poTax, 2);
                po.TotalAmount       = Math.Round(poTot, 2);
                po.InvoicedAmount    = invoiced ? Math.Round(poTot, 2) : 0m;
                po.PaidAmount        = invStatuses[i] == PurchaseInvoiceStatus.FullyPaid ? Math.Round(poTot, 2) : 0m;
                po.OutstandingAmount = Math.Round(poTot - po.PaidAmount, 2);

                // Two-step PO approval trail
                _ctx.PurchaseOrderApprovals.Add(Track(new PurchaseOrderApproval
                {
                    OrderId    = po.Id, StepNumber = 1, StepName = "Purchasing Manager",
                    ApproverId = _u, ApproverName = "Procurement Manager",
                    Status     = ProcurementApprovalStatus.Approved, Action = ProcurementApprovalAction.Approved,
                    ActionDate = orderDate.AddDays(1), Comments = "Within contract pricing."
                }));
                _ctx.PurchaseOrderApprovals.Add(Track(new PurchaseOrderApproval
                {
                    OrderId    = po.Id, StepNumber = 2, StepName = "Finance Director",
                    ApproverId = _u, ApproverName = "Finance Director",
                    Status     = ProcurementApprovalStatus.Approved, Action = ProcurementApprovalAction.Approved,
                    ActionDate = orderDate.AddDays(1), Comments = "Budget confirmed."
                }));

                // Amendment (quantity uplift) on every PO for an audit trail
                _ctx.PurchaseOrderAmendments.Add(Track(new PurchaseOrderAmendment
                {
                    OrderId           = po.Id,
                    AmendmentNumber   = 1,
                    AmendmentDate     = orderDate.AddDays(3),
                    RequestedByUserId = _u,
                    RequestedByName   = "Procurement Manager",
                    Description       = "Increased line 1 quantity by 5% to cover scrap allowance.",
                    Status            = AmendmentStatus.Applied,
                    PreviousValues    = "Line 1 Qty: original",
                    NewValues         = "Line 1 Qty: +5%",
                    RequiresReApproval= false,
                    ApprovedByUserId  = _u,
                    ApprovedAt        = orderDate.AddDays(3)
                }));

                // ── Goods Receipt + lines ───────────────────────────────────────────
                var receiptDate = orderDate.AddDays(d.LeadTimeDays);
                var gr = Track(new GoodsReceipt
                {
                    ReceiptNumber            = $"GRN-{_now.Year}-{i + 1:00000}",
                    VendorDeliveryNoteNumber = $"DN-{vendor.ShortNameOrNumber()}-{8000 + i}",
                    PurchaseOrderId          = po.Id,
                    VendorId                 = vendor.Id,
                    ReceiptDate              = receiptDate,
                    PostedAt                 = receiptDate.AddHours(2),
                    Status                   = GoodsReceiptStatus.Posted,
                    ReceiptType              = ReceiptType.Standard,
                    PostedByUserId           = _u,
                    Notes                    = "Goods received at plant goods-in and inspected."
                });
                _ctx.GoodsReceipts.Add(gr);
                grList[i] = gr;

                var grLines = new GoodsReceiptLine[lines.Count];
                for (int k = 0; k < lines.Count; k++)
                {
                    var l = lines[k];
                    var poLine = poLines[k];
                    var grLine = Track(new GoodsReceiptLine
                    {
                        ReceiptId           = gr.Id,
                        PurchaseOrderLineId = poLine.Id,
                        LineNumber          = k + 1,
                        ItemId              = l.Item.ItemId,
                        ItemCode            = l.Item.Code,
                        ItemDescription     = l.Item.Desc,
                        QuantityOrdered     = l.Quantity,
                        QuantityReceived    = poLine.QuantityReceived,
                        QuantityAccepted    = poLine.QuantityAccepted,
                        QuantityRejected    = poLine.QuantityRejected,
                        UnitOfMeasureName   = l.Item.Uom,
                        LotNumber           = $"LOT-{_now.Year}{i + 1:00}{k + 1:00}",
                        ManufacturerBatchNumber = $"MB-{vendor.ShortNameOrNumber()}-{k + 1}",
                        StorageLocationName = l.Item.ItemId == null ? null : "RM-RACK-A",
                        QualityStatus       = poLine.QuantityRejected > 0 ? QualityInspectionStatus.PartiallyPassed : QualityInspectionStatus.Passed,
                        QualityNotes        = poLine.QuantityRejected > 0 ? "Minor cosmetic rejects quarantined." : "Conforms to specification.",
                        InspectedByUserId   = _u,
                        InspectedAt         = receiptDate.AddHours(1)
                    });
                    _ctx.GoodsReceiptLines.Add(grLine);
                    grLines[k] = grLine;
                }
                grLineList.Add(grLines);

                // ── Purchase Invoice + lines + 3-way match ──────────────────────────
                var invoiceDate = receiptDate.AddDays(2);
                var paid = invStatuses[i] switch
                {
                    PurchaseInvoiceStatus.FullyPaid     => Math.Round(poTot, 2),
                    PurchaseInvoiceStatus.PartiallyPaid => Math.Round(poTot * 0.5m, 2),
                    _                                   => 0m
                };
                var inv = Track(new PurchaseInvoice
                {
                    InvoiceNumber       = $"BILL-{_now.Year}-{i + 1:00000}",
                    VendorInvoiceNumber = $"INV-{vendor.ShortNameOrNumber()}-{9000 + i}",
                    VendorId            = vendor.Id,
                    VendorName          = d.Name,
                    PurchaseOrderId     = po.Id,
                    InvoiceDate         = invoiceDate,
                    DueDate             = invoiceDate.AddDays(PaymentTermDays(d.PaymentTerms)),
                    PostingDate         = invoiceDate.AddDays(1),
                    Status              = invStatuses[i],
                    MatchingStatus      = invStatuses[i] == PurchaseInvoiceStatus.OnHold ? InvoiceMatchingStatus.MatchException : InvoiceMatchingStatus.FullyMatched,
                    PaymentStatus       = invStatuses[i] switch
                    {
                        PurchaseInvoiceStatus.FullyPaid     => InvoicePaymentStatus.FullyPaid,
                        PurchaseInvoiceStatus.PartiallyPaid => InvoicePaymentStatus.PartiallyPaid,
                        _                                   => InvoicePaymentStatus.NotPaid
                    },
                    CurrencyCode            = "USD",
                    SubTotalAmount          = Math.Round(poSub, 2),
                    TaxAmount               = Math.Round(poTax, 2),
                    TotalAmount             = Math.Round(poTot, 2),
                    PaidAmount              = paid,
                    OutstandingAmount       = Math.Round(poTot - paid, 2),
                    RecoverableTaxAmount    = Math.Round(poTax, 2),
                    PaymentTerms            = d.PaymentTerms,
                    ApprovedByUserId        = invStatuses[i] == PurchaseInvoiceStatus.OnHold ? null : _u,
                    ApprovedAt              = invStatuses[i] == PurchaseInvoiceStatus.OnHold ? null : invoiceDate.AddDays(1),
                    PostedByUserId          = _u,
                    PostedAt                = invoiceDate.AddDays(1),
                    HoldReason              = invStatuses[i] == PurchaseInvoiceStatus.OnHold ? "Price variance vs PO pending vendor credit note." : null
                });
                _ctx.PurchaseInvoices.Add(inv);
                invList[i] = inv;

                var invLines = new PurchaseInvoiceLine[lines.Count];
                for (int k = 0; k < lines.Count; k++)
                {
                    var l = lines[k];
                    var m = Money(l.Quantity, l.UnitPrice, 5m);
                    var invLine = Track(new PurchaseInvoiceLine
                    {
                        InvoiceId            = inv.Id,
                        LineNumber           = k + 1,
                        PurchaseOrderLineId  = poLines[k].Id,
                        GoodsReceiptLineId   = grLines[k].Id,
                        ItemId               = l.Item.ItemId,
                        ItemCode             = l.Item.Code,
                        ItemDescription      = l.Item.Desc,
                        Quantity             = l.Quantity,
                        UnitOfMeasureName    = l.Item.Uom,
                        UnitPrice            = l.UnitPrice,
                        TaxPercent           = 5m,
                        TaxAmount            = m.tax,
                        RecoverableTaxAmount = m.tax,
                        SubTotal             = m.sub,
                        TotalPrice           = m.total,
                        ProcurementCategoryId= procCatId
                    });
                    _ctx.PurchaseInvoiceLines.Add(invLine);
                    invLines[k] = invLine;

                    // 3-way match record per line
                    bool priceVar = invStatuses[i] == PurchaseInvoiceStatus.OnHold && k == 0;
                    _ctx.ThreeWayMatchRecords.Add(Track(new ThreeWayMatchRecord
                    {
                        InvoiceId           = inv.Id,
                        InvoiceLineId       = invLine.Id,
                        PurchaseOrderLineId = poLines[k].Id,
                        GoodsReceiptLineId  = grLines[k].Id,
                        MatchStatus         = priceVar ? InvoiceMatchingStatus.MatchException : InvoiceMatchingStatus.FullyMatched,
                        HasDiscrepancy      = priceVar,
                        DiscrepancyType     = priceVar ? DiscrepancyType.UnitPrice : null,
                        POQuantity          = l.Quantity,
                        ReceivedQuantity    = grLines[k].QuantityAccepted,
                        InvoicedQuantity    = l.Quantity,
                        QuantityVariance    = Math.Round(l.Quantity - grLines[k].QuantityAccepted, 4),
                        POUnitPrice         = l.UnitPrice,
                        InvoiceUnitPrice    = priceVar ? Math.Round(l.UnitPrice * 1.04m, 4) : l.UnitPrice,
                        PriceVariance       = priceVar ? Math.Round(l.UnitPrice * 0.04m, 4) : 0m,
                        TotalVarianceAmount = priceVar ? Math.Round(l.Quantity * l.UnitPrice * 0.04m, 2) : 0m,
                        Resolution          = priceVar ? "Awaiting vendor credit note." : null,
                        MatchedByUserId     = _u
                    }));
                }

                // ── Vendor Payment + allocation ─────────────────────────────────────
                var payAmount = paid > 0 ? paid : Math.Round(poTot, 2); // planned amount for draft payments
                var payment = Track(new VendorPayment
                {
                    PaymentNumber        = $"PAY-{_now.Year}-{i + 1:00000}",
                    VendorId             = vendor.Id,
                    VendorName           = d.Name,
                    PaymentDate          = invoiceDate.AddDays(PaymentTermDays(d.PaymentTerms)),
                    ValueDate            = invoiceDate.AddDays(PaymentTermDays(d.PaymentTerms)),
                    ClearedAt            = payStatuses[i] == VendorPaymentStatus.Cleared ? invoiceDate.AddDays(PaymentTermDays(d.PaymentTerms) + 1) : null,
                    Status               = payStatuses[i],
                    PaymentMethod        = i % 2 == 0 ? VendorPaymentMethod.BankTransfer : VendorPaymentMethod.Check,
                    CurrencyCode         = "USD",
                    TotalAmount          = payAmount,
                    AllocatedAmount      = payAmount,
                    UnallocatedAmount    = 0m,
                    BankReferenceNumber  = i % 2 == 0 ? $"WIRE-{_now.Year}-{i + 1:0000}" : null,
                    CheckNumber          = i % 2 == 0 ? null : $"CHK-{50000 + i}",
                    TransactionReference = $"TXN-{_now.Year}-{i + 1:00000}",
                    WithholdingTaxAmount = 0m,
                    ApprovedByUserId     = payStatuses[i] == VendorPaymentStatus.Draft ? null : _u,
                    ApprovedAt           = payStatuses[i] == VendorPaymentStatus.Draft ? null : invoiceDate.AddDays(PaymentTermDays(d.PaymentTerms))
                });
                _ctx.VendorPayments.Add(payment);
                _ctx.VendorPaymentLines.Add(Track(new VendorPaymentLine
                {
                    PaymentId                = payment.Id,
                    InvoiceId                = inv.Id,
                    InvoiceTotalAmount       = Math.Round(poTot, 2),
                    InvoiceOutstandingAmount = inv.OutstandingAmount,
                    AllocatedAmount          = payAmount,
                    DiscountTaken            = 0m,
                    WriteOffAmount           = 0m,
                    FXGainLossAmount         = 0m
                }));
            }
            await _ctx.SaveChangesAsync();
            _logger.LogInformation("Seeded requisitions, RFQs, quotations, POs, receipts, invoices and payments");

            // ════════════════════════════════════════════════════════════════════════
            // 5. Purchase Returns + Vendor Debit Notes (first 6 vendors)
            // ════════════════════════════════════════════════════════════════════════
            var returnReasons = new[] { PurchaseReturnReason.QualityDefect, PurchaseReturnReason.Damaged, PurchaseReturnReason.WrongItem,
                                        PurchaseReturnReason.ExcessQuantity, PurchaseReturnReason.SpecificationMismatch, PurchaseReturnReason.ExpiredGoods };
            var returns = new PurchaseReturn[6];
            var returnLines = new PurchaseReturnLine[6];
            for (int i = 0; i < 6; i++)
            {
                var d        = defs[i];
                var po       = poList[i];
                var gr       = grList[i];
                var poLines  = poLineList[i];
                var grLines  = grLineList[i];
                var l        = d.Supplies[0];
                var retQty   = Math.Round(l.Quantity * 0.03m + 1m, 2);
                var m        = Money(retQty, l.UnitPrice, 5m);
                var retDate  = gr.ReceiptDate.AddDays(5);

                var ret = Track(new PurchaseReturn
                {
                    ReturnNumber                    = $"PRN-{_now.Year}-{i + 1:00000}",
                    PurchaseOrderId                 = po.Id,
                    GoodsReceiptId                  = gr.Id,
                    VendorId                        = vendors[i].Id,
                    ReturnDate                      = retDate,
                    ApprovedAt                      = retDate.AddDays(1),
                    PostedAt                        = i < 4 ? retDate.AddDays(2) : null,
                    Status                          = i < 4 ? PurchaseReturnStatus.Posted : PurchaseReturnStatus.Approved,
                    ReturnReason                    = returnReasons[i],
                    CurrencyCode                    = "USD",
                    TotalReturnAmount               = m.total,
                    ApprovedByUserId                = _u,
                    PostedByUserId                  = i < 4 ? _u : null,
                    VendorReturnAuthorisationNumber = $"RMA-{vendors[i].ShortNameOrNumber()}-{i + 1}",
                    Description                     = $"Return to {d.ShortName}: {returnReasons[i]}."
                });
                _ctx.PurchaseReturns.Add(ret);
                returns[i] = ret;

                var rl = Track(new PurchaseReturnLine
                {
                    PurchaseReturnId    = ret.Id,
                    LineNumber          = 1,
                    GoodsReceiptLineId  = grLines[0].Id,
                    PurchaseOrderLineId = poLines[0].Id,
                    ItemId              = l.Item.ItemId,
                    ItemCode            = l.Item.Code,
                    ItemDescription     = l.Item.Desc,
                    QuantityReceived    = grLines[0].QuantityReceived,
                    QuantityReturned    = retQty,
                    UnitOfMeasureName   = l.Item.Uom,
                    UnitPrice           = l.UnitPrice,
                    TaxPercent          = 5m,
                    TaxAmount           = m.tax,
                    TotalReturnAmount   = m.total,
                    ReturnReason        = returnReasons[i],
                    QualityIssueDescription = returnReasons[i] is PurchaseReturnReason.QualityDefect or PurchaseReturnReason.Damaged
                                                ? "Surface damage / out-of-tolerance units identified at inspection." : null,
                    LotNumber           = grLines[0].LotNumber
                });
                _ctx.PurchaseReturnLines.Add(rl);
                returnLines[i] = rl;
            }
            await _ctx.SaveChangesAsync();

            for (int i = 0; i < 6; i++)
            {
                var d   = defs[i];
                var ret = returns[i];
                var rl  = returnLines[i];
                var m   = Money(rl.QuantityReturned, rl.UnitPrice, 5m);
                bool settled = i < 3;
                var dn = Track(new VendorDebitNote
                {
                    DebitNoteNumber   = $"DN-{_now.Year}-{i + 1:00000}",
                    PurchaseReturnId  = ret.Id,
                    VendorId          = vendors[i].Id,
                    VendorName        = d.Name,
                    OriginalInvoiceId = invList[i].Id,
                    DebitNoteDate     = ret.ReturnDate.AddDays(1),
                    SentAt            = ret.ReturnDate.AddDays(2),
                    AcknowledgedAt    = i < 4 ? ret.ReturnDate.AddDays(3) : null,
                    SettledAt         = settled ? ret.ReturnDate.AddDays(10) : null,
                    Status            = settled ? DebitNoteStatus.FullySettled
                                       : i < 4 ? DebitNoteStatus.Acknowledged : DebitNoteStatus.Sent,
                    CurrencyCode      = "USD",
                    SubTotalAmount    = m.sub,
                    TaxAmount         = m.tax,
                    TotalAmount       = m.total,
                    SettledAmount     = settled ? m.total : 0m,
                    OutstandingAmount = settled ? 0m : m.total,
                    PostedByUserId    = _u,
                    PostedAt          = ret.ReturnDate.AddDays(1)
                });
                _ctx.VendorDebitNotes.Add(dn);
                _ctx.VendorDebitNoteLines.Add(Track(new VendorDebitNoteLine
                {
                    DebitNoteId     = dn.Id,
                    LineNumber      = 1,
                    ReturnLineId    = rl.Id,
                    ItemId          = rl.ItemId,
                    ItemCode        = rl.ItemCode,
                    ItemDescription = rl.ItemDescription,
                    Quantity        = rl.QuantityReturned,
                    UnitPrice       = rl.UnitPrice,
                    TaxPercent      = 5m,
                    TaxAmount       = m.tax,
                    SubTotal        = m.sub,
                    TotalAmount     = m.total
                }));
            }
            await _ctx.SaveChangesAsync();
            _logger.LogInformation("Seeded purchase returns and vendor debit notes");

            // ════════════════════════════════════════════════════════════════════════
            // 6. Landed Costs (imports — first 6 raw-material receipts)
            // ════════════════════════════════════════════════════════════════════════
            for (int i = 0; i < 6; i++)
            {
                var gr      = grList[i];
                var grLines = grLineList[i];
                var goodsValue = poLineList[i].Sum(pl => pl.SubTotal);

                var freight   = Math.Round(goodsValue * 0.06m, 2);
                var customs   = Math.Round(goodsValue * 0.04m, 2);
                var insurance = Math.Round(goodsValue * 0.015m, 2);
                var total     = freight + customs + insurance;

                var lc = Track(new LandedCost
                {
                    LandedCostNumber      = $"LC-{_now.Year}-{i + 1:00000}",
                    Description           = $"Import landed costs for receipt {gr.ReceiptNumber}.",
                    VendorId              = vendors[i].Id,
                    Status                = i < 4 ? LandedCostStatus.Posted : LandedCostStatus.Draft,
                    DocumentDate          = gr.ReceiptDate.AddDays(1),
                    PostedAt              = i < 4 ? gr.ReceiptDate.AddDays(2) : null,
                    PostedByUserId        = i < 4 ? _u : null,
                    CurrencyCode          = "USD",
                    TotalLandedCostAmount = total,
                    Notes                 = "Allocated across receipt lines by value."
                });
                _ctx.LandedCosts.Add(lc);

                var lcFreight = Track(new LandedCostLine
                {
                    LandedCostId = lc.Id, LineNumber = 1, CostType = LandedCostType.Freight,
                    Description = "Ocean freight & inland haulage", Amount = freight, CurrencyCode = "USD",
                    AllocationMethod = LandedCostAllocationMethod.ByValue
                });
                var lcCustoms = Track(new LandedCostLine
                {
                    LandedCostId = lc.Id, LineNumber = 2, CostType = LandedCostType.CustomsDuty,
                    Description = "Customs import duty", Amount = customs, CurrencyCode = "USD",
                    AllocationMethod = LandedCostAllocationMethod.ByValue
                });
                var lcInsurance = Track(new LandedCostLine
                {
                    LandedCostId = lc.Id, LineNumber = 3, CostType = LandedCostType.Insurance,
                    Description = "Cargo insurance", Amount = insurance, CurrencyCode = "USD",
                    AllocationMethod = LandedCostAllocationMethod.ByValue
                });
                _ctx.LandedCostLines.AddRange(lcFreight, lcCustoms, lcInsurance);

                _ctx.LandedCostGoodsReceipts.Add(Track(new LandedCostGoodsReceipt
                {
                    LandedCostId = lc.Id, GoodsReceiptId = gr.Id
                }));

                // Allocate each cost line across the receipt lines by value
                foreach (var (lcLine, amount) in new[] { (lcFreight, freight), (lcCustoms, customs), (lcInsurance, insurance) })
                {
                    foreach (var grLine in grLines)
                    {
                        var poLine = poLineList[i].First(p => p.Id == grLine.PurchaseOrderLineId);
                        var basis  = poLine.SubTotal;
                        var pct    = goodsValue == 0 ? 0m : Math.Round(basis / goodsValue * 100m, 4);
                        var alloc  = Math.Round(amount * (goodsValue == 0 ? 0m : basis / goodsValue), 2);
                        var qty    = grLine.QuantityAccepted == 0 ? 1m : grLine.QuantityAccepted;
                        _ctx.LandedCostAllocations.Add(Track(new LandedCostAllocation
                        {
                            LandedCostId           = lc.Id,
                            LandedCostLineId       = lcLine.Id,
                            GoodsReceiptLineId     = grLine.Id,
                            AllocationMethod       = LandedCostAllocationMethod.ByValue,
                            AllocationBasisValue   = basis,
                            TotalBasisValue        = goodsValue,
                            AllocationPercent      = pct,
                            AllocatedAmount        = alloc,
                            AllocatedAmountPerUnit = Math.Round(alloc / qty, 4)
                        }));
                    }
                }
            }
            await _ctx.SaveChangesAsync();
            _logger.LogInformation("Seeded landed costs with allocations");

            // ════════════════════════════════════════════════════════════════════════
            // 7. Advance document-number sequences past the seeded documents
            // ════════════════════════════════════════════════════════════════════════
            // The documents above are numbered manually (PR-2026-00001 …) but the
            // DocumentSequence counters seeded by ProcurementInitializationService still
            // sit at 1. Without this, the first user-created document of each type would
            // re-issue a seeded number and hit the unique (CompanyId, Number) index.
            await SyncSequenceAsync(ProcurementDocumentType.PurchaseRequisition,
                await _ctx.PurchaseRequisitions.CountAsync(x => x.CompanyId == _c));
            await SyncSequenceAsync(ProcurementDocumentType.RequestForQuotation,
                await _ctx.RequestForQuotations.CountAsync(x => x.CompanyId == _c));
            await SyncSequenceAsync(ProcurementDocumentType.PurchaseOrder,
                await _ctx.PurchaseOrders.CountAsync(x => x.CompanyId == _c));
            await SyncSequenceAsync(ProcurementDocumentType.GoodsReceipt,
                await _ctx.GoodsReceipts.CountAsync(x => x.CompanyId == _c));
            await SyncSequenceAsync(ProcurementDocumentType.PurchaseInvoice,
                await _ctx.PurchaseInvoices.CountAsync(x => x.CompanyId == _c));
            await SyncSequenceAsync(ProcurementDocumentType.PurchaseContract,
                await _ctx.PurchaseContracts.CountAsync(x => x.CompanyId == _c));
            await SyncSequenceAsync(ProcurementDocumentType.VendorPayment,
                await _ctx.VendorPayments.CountAsync(x => x.CompanyId == _c));
            await SyncSequenceAsync(ProcurementDocumentType.PurchaseReturn,
                await _ctx.PurchaseReturns.CountAsync(x => x.CompanyId == _c));
            await SyncSequenceAsync(ProcurementDocumentType.VendorDebitNote,
                await _ctx.VendorDebitNotes.CountAsync(x => x.CompanyId == _c));
            await _ctx.SaveChangesAsync();
            _logger.LogInformation("Advanced procurement document sequences past seeded documents");

            await tx.CommitAsync();
            _logger.LogInformation("Procurement sample data seeding complete for Company {CompanyId}", companyId);
            return true;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "Failed to seed Procurement sample data for Company {CompanyId}", companyId);
            throw;
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Advances a document-number sequence past the <paramref name="issued"/> documents just
    /// seeded so the first user-created document of that type doesn't re-issue a seeded number.
    /// Also stamps the reset markers to the current period so the yearly/monthly reset logic
    /// doesn't wind the counter back to 1 on first use.
    /// </summary>
    private async Task SyncSequenceAsync(ProcurementDocumentType type, int issued)
    {
        var seq = await _ctx.DocumentSequences
            .FirstOrDefaultAsync(s => s.CompanyId == _c && s.DocumentType == type);
        if (seq is null) return;

        seq.NextSequenceNumber = issued + 1;
        seq.LastResetYear      = _now.Year;
        seq.LastResetMonth     = _now.Month;
    }

    /// <summary>Stamps tenant/audit fields shared by every entity.</summary>
    private T Track<T>(T e) where T : BaseEntity
    {
        e.CompanyId       = _c;
        e.BranchId        = _b;
        e.BusinessUnitId  = _bu;
        e.CreatedByUserId = _u;
        e.CreatedAt       = _now;
        e.IsActive        = true;
        return e;
    }

    private static (decimal sub, decimal tax, decimal total) Money(decimal qty, decimal price, decimal taxPct, decimal discPct = 0m)
    {
        var gross = qty * price;
        var disc  = Math.Round(gross * discPct / 100m, 2);
        var sub   = Math.Round(gross - disc, 2);
        var tax   = Math.Round(sub * taxPct / 100m, 2);
        return (sub, tax, Math.Round(sub + tax, 2));
    }

    private static int PaymentTermDays(PaymentTerms terms) => terms switch
    {
        PaymentTerms.Cash   => 0,
        PaymentTerms.Net15  => 15,
        PaymentTerms.Net30  => 30,
        PaymentTerms.Net45  => 45,
        PaymentTerms.Net60  => 60,
        PaymentTerms.Net90  => 90,
        _                   => 30
    };

    private static Guid? AvlCategory(VendorDef d, Func<string, Guid?> procCat) => d.Number switch
    {
        "V-00006" => procCat("IT-HW"),
        "V-00007" => procCat("FACILITY"),
        _         => procCat("DIRECT")
    };

    private static readonly string[] ContactFirst = ["James", "Sophia", "Daniel", "Aisha", "Marco", "Elena", "Raj"];
    private static readonly string[] ContactLast  = ["Bennett", "Müller", "Carter", "Khan", "Rossi", "Novak", "Sharma"];
    private static readonly string[] BankNames    = ["First National Bank", "Barclays", "Pacific Trust Bank", "KeyBank", "Deutsche Bank", "Silicon Valley Bank", "Chase Commercial"];
    private static readonly string[] UsState      = ["PA", "OR", "OH", "MI", "IL", "CA", "TX"];
    private static readonly string[] Departments  = ["Production", "Upholstery", "Carpentry", "Assembly", "Finishing", "IT", "Facilities"];
    private static readonly string[] Requesters   = ["Olivia Grant", "Liam Foster", "Noah Reed", "Emma Hayes", "Lucas Bright", "Mia Turner", "Ethan Cole"];

    // ── Local seed-model records ──────────────────────────────────────────────────
    // ItemId is the per-company cross-module GUID (matching the Inventory seed) for stock
    // items, or null for non-inventory IT / service lines.
    private sealed record CatalogItem(Guid? ItemId, string Code, string Desc, string Uom, decimal Price);

    private sealed record OrderLineDef(CatalogItem Item, decimal Quantity, decimal UnitPrice);

    private sealed record VendorDef(
        string Number, string Name, string ShortName, Guid? CategoryId,
        string Email, string Phone, string City, string Country,
        PaymentTerms PaymentTerms, int LeadTimeDays, decimal CreditLimit, bool Preferred,
        List<OrderLineDef> Supplies);
}

internal static class VendorSeedExtensions
{
    public static string ShortNameOrNumber(this Vendor v) =>
        string.IsNullOrWhiteSpace(v.ShortName) ? v.VendorNumber : v.ShortName!.Replace(" ", "");
}
