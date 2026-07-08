using Manufacturing.Domain.Entities;
using Manufacturing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Helpers;

namespace Manufacturing.Infrastructure.Services;

/// <summary>
/// Seeds comprehensive sample manufacturing data.
///
/// Domain: Industrial Office Furniture — 5 products, 6 production orders
///
/// Products:
///   FG-CHAIR    Executive Ergonomic Chair
///   FG-DESK     Height-Adjustable Standing Desk
///   FG-CABINET  4-Drawer Steel Filing Cabinet
///   FG-SHELF    3-Tier Steel Bookshelf
///   FG-TABLE    6-Seater Meeting Room Table
///
/// Production Orders:
///   PO-0001  Chair    × 100  Completed  (full close-out, rework, costing)
///   PO-0002  Cabinet  ×  60  Completed  (full close-out, rework, costing)
///   PO-0003  Shelf    ×  80  Completed  (full close-out, rework, costing)
///   PO-0004  Desk     ×  50  InProgress (assembly phase, painting pending)
///   PO-0005  Table    ×  20  Released   (operations assigned, not yet started)
///   PO-0006  Chair    × 150  Planned    (second demand run)
/// </summary>
public class ManufacturingSeedDataService
{
    private readonly ManufacturingDbContext _ctx;
    private readonly ILogger<ManufacturingSeedDataService> _logger;

    public ManufacturingSeedDataService(
        ManufacturingDbContext ctx,
        ILogger<ManufacturingSeedDataService> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }

    // ── Per-company deterministic cross-module GUIDs ──────────────────────────
    // Derived at runtime from companyId using CrossModuleGuid.Derive(companyId, seq).
    // The inventory seed uses the same formula, so both modules independently
    // arrive at the same IDs for the same company — fully multi-tenant safe.
    //
    // Sequence map (must match InventoryInitializationService.Items.cs):
    //   Finished Goods  : 1001–1005
    //   Raw Materials   : 1101–1112
    //   Warehouses      : 2001–2002
    private static Guid FgChair(Guid c)       => CrossModuleGuid.Derive(c, 1001);
    private static Guid FgDesk(Guid c)        => CrossModuleGuid.Derive(c, 1002);
    private static Guid FgCabinet(Guid c)     => CrossModuleGuid.Derive(c, 1003);
    private static Guid FgBookshelf(Guid c)   => CrossModuleGuid.Derive(c, 1004);
    private static Guid FgTable(Guid c)       => CrossModuleGuid.Derive(c, 1005);

    private static Guid RmSteelTube(Guid c)    => CrossModuleGuid.Derive(c, 1101);
    private static Guid RmFabric(Guid c)       => CrossModuleGuid.Derive(c, 1102);
    private static Guid RmFoam(Guid c)         => CrossModuleGuid.Derive(c, 1103);
    private static Guid RmWoodPanel(Guid c)    => CrossModuleGuid.Derive(c, 1104);
    private static Guid RmScrews(Guid c)       => CrossModuleGuid.Derive(c, 1105);
    private static Guid RmPaint(Guid c)        => CrossModuleGuid.Derive(c, 1106);
    private static Guid RmMdfBoard(Guid c)     => CrossModuleGuid.Derive(c, 1107);
    private static Guid RmHandles(Guid c)      => CrossModuleGuid.Derive(c, 1108);
    private static Guid RmSteelSheet(Guid c)   => CrossModuleGuid.Derive(c, 1109);
    private static Guid RmGasLift(Guid c)      => CrossModuleGuid.Derive(c, 1110);
    private static Guid RmCasterWheels(Guid c) => CrossModuleGuid.Derive(c, 1111);
    private static Guid RmParticleBoard(Guid c)=> CrossModuleGuid.Derive(c, 1112);

    private static Guid WhRawMaterials(Guid c)  => CrossModuleGuid.Derive(c, 2001);
    private static Guid WhFinishedGoods(Guid c) => CrossModuleGuid.Derive(c, 2002);

    // ── Fast-food (pizza shop) cross-module GUIDs ─────────────────────────────
    // Must match InventoryInitializationService.Items.cs seq map:
    //   Food raw materials  : 1201–1230
    //   Menu finished goods : 1301–1340
    private static Guid RmDough(Guid c)        => CrossModuleGuid.Derive(c, 1201);
    private static Guid RmMozz(Guid c)         => CrossModuleGuid.Derive(c, 1202);
    private static Guid RmPizzaSauce(Guid c)   => CrossModuleGuid.Derive(c, 1203);
    private static Guid RmPepperoni(Guid c)    => CrossModuleGuid.Derive(c, 1204);
    private static Guid RmChkTikka(Guid c)     => CrossModuleGuid.Derive(c, 1205);
    private static Guid RmVegMix(Guid c)       => CrossModuleGuid.Derive(c, 1206);
    private static Guid RmBeefPatty(Guid c)    => CrossModuleGuid.Derive(c, 1207);
    private static Guid RmChkFillet(Guid c)    => CrossModuleGuid.Derive(c, 1208);
    private static Guid RmBun(Guid c)          => CrossModuleGuid.Derive(c, 1209);
    private static Guid RmCheddar(Guid c)      => CrossModuleGuid.Derive(c, 1210);
    private static Guid RmLettuce(Guid c)      => CrossModuleGuid.Derive(c, 1211);
    private static Guid RmTomato(Guid c)       => CrossModuleGuid.Derive(c, 1212);
    private static Guid RmMayo(Guid c)         => CrossModuleGuid.Derive(c, 1213);
    private static Guid RmPotato(Guid c)       => CrossModuleGuid.Derive(c, 1214);
    private static Guid RmFryOil(Guid c)       => CrossModuleGuid.Derive(c, 1215);
    private static Guid RmPizzaBox(Guid c)     => CrossModuleGuid.Derive(c, 1216);
    private static Guid RmBurgerWrap(Guid c)   => CrossModuleGuid.Derive(c, 1217);
    private static Guid RmFriesCup(Guid c)     => CrossModuleGuid.Derive(c, 1218);

    private static Guid FgPizzaMargS(Guid c)   => CrossModuleGuid.Derive(c, 1301);
    private static Guid FgPizzaMargM(Guid c)   => CrossModuleGuid.Derive(c, 1302);
    private static Guid FgPizzaMargL(Guid c)   => CrossModuleGuid.Derive(c, 1303);
    private static Guid FgPizzaPepS(Guid c)    => CrossModuleGuid.Derive(c, 1304);
    private static Guid FgPizzaPepM(Guid c)    => CrossModuleGuid.Derive(c, 1305);
    private static Guid FgPizzaPepL(Guid c)    => CrossModuleGuid.Derive(c, 1306);
    private static Guid FgPizzaTikkaS(Guid c)  => CrossModuleGuid.Derive(c, 1307);
    private static Guid FgPizzaTikkaM(Guid c)  => CrossModuleGuid.Derive(c, 1308);
    private static Guid FgPizzaTikkaL(Guid c)  => CrossModuleGuid.Derive(c, 1309);
    private static Guid FgBurgerBeef(Guid c)   => CrossModuleGuid.Derive(c, 1321);
    private static Guid FgBurgerZinger(Guid c) => CrossModuleGuid.Derive(c, 1322);
    private static Guid FgBurgerCheese(Guid c) => CrossModuleGuid.Derive(c, 1323);
    private static Guid FgFriesReg(Guid c)     => CrossModuleGuid.Derive(c, 1331);
    private static Guid FgFriesLrg(Guid c)     => CrossModuleGuid.Derive(c, 1332);

    // ── Public API ────────────────────────────────────────────────────────────

    public async Task ClearAsync(Guid companyId)
    {
        await using var tx = await _ctx.Database.BeginTransactionAsync();
        try
        {
            await DeleteWhere(_ctx.InventoryTransactions, companyId);
            await DeleteWhere(_ctx.CapacityLoads, companyId);
            await DeleteWhere(_ctx.ReworkOrders, companyId);
            await DeleteWhere(_ctx.MachineDowntimes, companyId);
            await DeleteWhere(_ctx.ProductionBatches, companyId);
            await DeleteWhere(_ctx.ProductionOrders, companyId);
            await DeleteWhere(_ctx.PlannedOrders, companyId);
            await DeleteWhere(_ctx.Demands, companyId);
            await DeleteWhere(_ctx.Routings, companyId);
            await DeleteWhere(_ctx.BillsOfMaterial, companyId);
            await DeleteWhere(_ctx.StandardCosts, companyId);
            await DeleteWhere(_ctx.MaterialPlanningData, companyId);
            await DeleteWhere(_ctx.OverheadRules, companyId);
            await DeleteWhere(_ctx.WorkCenters, companyId);
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> SeedAsync(Guid companyId, Guid branchId, Guid businessUnitId, Guid userId)
    {
        if (await _ctx.ProductionOrders.AnyAsync(p => p.CompanyId == companyId))
        {
            _logger.LogInformation("Manufacturing seed data already exists for Company {CompanyId}", companyId);
            return false;
        }

        await using var tx = await _ctx.Database.BeginTransactionAsync();
        try
        {
            var now  = DateTime.UtcNow;
            var year = now.Year;
            var T    = (companyId, branchId, businessUnitId, userId);

            // Resolve per-company cross-module GUIDs once
            var fgChair      = FgChair(companyId);
            var fgDesk       = FgDesk(companyId);
            var fgCabinet    = FgCabinet(companyId);
            var fgBookshelf  = FgBookshelf(companyId);
            var fgTable      = FgTable(companyId);

            var rmSteelTube    = RmSteelTube(companyId);
            var rmFabric       = RmFabric(companyId);
            var rmFoam         = RmFoam(companyId);
            var rmWoodPanel    = RmWoodPanel(companyId);
            var rmScrews       = RmScrews(companyId);
            var rmPaint        = RmPaint(companyId);
            var rmMdfBoard     = RmMdfBoard(companyId);
            var rmHandles      = RmHandles(companyId);
            var rmSteelSheet   = RmSteelSheet(companyId);
            var rmGasLift      = RmGasLift(companyId);
            var rmCasterWheels = RmCasterWheels(companyId);
            var rmParticleBoard= RmParticleBoard(companyId);

            var whRawMaterials  = WhRawMaterials(companyId);
            var whFinishedGoods = WhFinishedGoods(companyId);

            var wcs         = await _ctx.WorkCenters.Where(w => w.CompanyId == companyId).ToListAsync();
            var wcAssembly  = wcs.First(w => w.Code == "WC-ASSEMBLY");
            var wcMachining = wcs.First(w => w.Code == "WC-MACHINING");
            var wcPainting  = wcs.First(w => w.Code == "WC-PAINTING");
            var wcQC        = wcs.First(w => w.Code == "WC-QC");
            var wcPackaging = wcs.First(w => w.Code == "WC-PACKAGING");
            var wcWelding   = wcs.First(w => w.Code == "WC-WELDING");
            var wcCutting   = wcs.First(w => w.Code == "WC-CUTTING");
            var wcSubCon    = wcs.First(w => w.Code == "WC-SUBCON");

            // ── WorkCenter Shifts ─────────────────────────────────────────────
            // 3 shifts per busy work center to reflect real multi-shift production
            var shiftAsmMorn = MakeShift(T, now, wcAssembly.Id,  "Morning Shift",   "Mon,Tue,Wed,Thu,Fri",     8,   0.88m);
            var shiftAsmEve  = MakeShift(T, now, wcAssembly.Id,  "Evening Shift",   "Mon,Tue,Wed,Thu,Fri",     8,   0.82m);
            var shiftAsmNight= MakeShift(T, now, wcAssembly.Id,  "Night Shift",     "Mon,Tue,Wed,Thu",         6,   0.75m);
            var shiftMchDay  = MakeShift(T, now, wcMachining.Id, "Day Shift",       "Mon,Tue,Wed,Thu,Fri",     6,   0.90m);
            var shiftMchEve  = MakeShift(T, now, wcMachining.Id, "Evening Shift",   "Mon,Tue,Wed,Thu,Fri",     6,   0.85m);
            var shiftPntDay  = MakeShift(T, now, wcPainting.Id,  "Day Shift",       "Mon,Tue,Wed,Thu,Fri",     4,   0.85m);
            var shiftPntEve  = MakeShift(T, now, wcPainting.Id,  "Evening Shift",   "Mon,Tue,Wed,Thu,Fri",     4,   0.80m);
            var shiftQcFull  = MakeShift(T, now, wcQC.Id,        "Full Day",        "Mon,Tue,Wed,Thu,Fri,Sat", 8,   0.95m);
            var shiftPkgDay  = MakeShift(T, now, wcPackaging.Id, "Day Shift",       "Mon,Tue,Wed,Thu,Fri",     8,   0.88m);
            var shiftWldDay  = MakeShift(T, now, wcWelding.Id,   "Day Shift",       "Mon,Tue,Wed,Thu,Fri",     6,   0.86m);
            var shiftWldEve  = MakeShift(T, now, wcWelding.Id,   "Evening Shift",   "Mon,Tue,Wed,Thu",         6,   0.80m);
            var shiftCutDay  = MakeShift(T, now, wcCutting.Id,   "Day Shift",       "Mon,Tue,Wed,Thu,Fri",     8,   0.90m);

            _ctx.WorkCenterShifts.AddRange(shiftAsmMorn, shiftAsmEve, shiftAsmNight,
                shiftMchDay, shiftMchEve, shiftPntDay, shiftPntEve,
                shiftQcFull, shiftPkgDay, shiftWldDay, shiftWldEve, shiftCutDay);
            await _ctx.SaveChangesAsync();

            // ── Standard Costs ────────────────────────────────────────────────
            var scChair    = MakeStdCost(T, now, fgChair,    1, "USD",  95,  36, 24, 18);
            var scDesk     = MakeStdCost(T, now, fgDesk,     1, "USD", 145,  55, 42, 30);
            var scCabinet  = MakeStdCost(T, now, fgCabinet,  1, "USD", 110,  40, 32, 22);
            var scBookshelf= MakeStdCost(T, now, fgBookshelf,1, "USD",  68,  25, 20, 15);
            var scTable    = MakeStdCost(T, now, fgTable,    1, "USD", 220,  80, 55, 40);
            _ctx.StandardCosts.AddRange(scChair, scDesk, scCabinet, scBookshelf, scTable);
            await _ctx.SaveChangesAsync();

            // ── Material Planning Data ────────────────────────────────────────
            _ctx.MaterialPlanningData.AddRange(
                // Finished goods — MRP Make
                MakeMPD(T, now, fgChair,         60,  40, 600, 100, 12, "Make", "MRP"),
                MakeMPD(T, now, fgDesk,          25,  15, 200,  50, 14, "Make", "MRP"),
                MakeMPD(T, now, fgCabinet,       30,  20, 250,  60, 12, "Make", "MRP"),
                MakeMPD(T, now, fgBookshelf,     40,  25, 400,  80, 10, "Make", "MRP"),
                MakeMPD(T, now, fgTable,         10,   5, 100,  20, 18, "Make", "MRP"),
                // Key raw materials — Buy ReorderPoint
                MakeMPD(T, now, rmSteelTube,    500, 250,4000,1000,  7, "Buy", "ReorderPoint"),
                MakeMPD(T, now, rmFabric,       200, 100,1500, 300,  5, "Buy", "ReorderPoint"),
                MakeMPD(T, now, rmSteelSheet,   300, 150,2500, 600,  7, "Buy", "ReorderPoint"),
                MakeMPD(T, now, rmWoodPanel,    150,  80,1200, 250,  7, "Buy", "ReorderPoint"),
                MakeMPD(T, now, rmGasLift,      120,  60, 600, 150,  5, "Buy", "ReorderPoint"));
            await _ctx.SaveChangesAsync();

            // ── BOM: Executive Ergonomic Chair ────────────────────────────────
            var bomChair = MakeBOM(T, now, fgChair, 1);
            _ctx.BillsOfMaterial.Add(bomChair);
            await _ctx.SaveChangesAsync();

            var bomChairItems = new[]
            {
                MakeBOMItem(T, now, bomChair.Id, rmSteelTube,   "PCS",  4,    2),
                MakeBOMItem(T, now, bomChair.Id, rmFabric,      "MTR",  1.5m, 1),
                MakeBOMItem(T, now, bomChair.Id, rmFoam,        "KG",   0.8m, 0),
                MakeBOMItem(T, now, bomChair.Id, rmScrews,      "PKT",  1,    0),
                MakeBOMItem(T, now, bomChair.Id, rmPaint,       "LTR",  0.3m, 0),
                MakeBOMItem(T, now, bomChair.Id, rmGasLift,     "PCS",  1,    0),
                MakeBOMItem(T, now, bomChair.Id, rmCasterWheels,"PCS",  1,    0),
            };
            _ctx.BOMItems.AddRange(bomChairItems);
            _ctx.BOMByProducts.Add(MakeBOMByProduct(T, now, bomChair.Id, rmSteelTube, "Scrap", "KG", 0.25m, 5));
            await _ctx.SaveChangesAsync();

            // ── BOM: 4-Drawer Filing Cabinet ──────────────────────────────────
            var bomCabinet = MakeBOM(T, now, fgCabinet, 1);
            _ctx.BillsOfMaterial.Add(bomCabinet);
            await _ctx.SaveChangesAsync();

            var bomCabinetItems = new[]
            {
                MakeBOMItem(T, now, bomCabinet.Id, rmSteelSheet, "PCS",  8,    2),
                MakeBOMItem(T, now, bomCabinet.Id, rmMdfBoard,   "PCS",  3,    1),
                MakeBOMItem(T, now, bomCabinet.Id, rmHandles,    "PCS",  4,    0),
                MakeBOMItem(T, now, bomCabinet.Id, rmScrews,     "PKT",  2,    0),
                MakeBOMItem(T, now, bomCabinet.Id, rmPaint,      "LTR",  0.5m, 0),
            };
            _ctx.BOMItems.AddRange(bomCabinetItems);
            _ctx.BOMByProducts.Add(MakeBOMByProduct(T, now, bomCabinet.Id, rmSteelSheet, "Scrap", "KG", 0.5m, 4));
            await _ctx.SaveChangesAsync();

            // ── BOM: 3-Tier Steel Bookshelf ───────────────────────────────────
            var bomBookshelf = MakeBOM(T, now, fgBookshelf, 1);
            _ctx.BillsOfMaterial.Add(bomBookshelf);
            await _ctx.SaveChangesAsync();

            var bomBookshelfItems = new[]
            {
                MakeBOMItem(T, now, bomBookshelf.Id, rmSteelSheet,    "PCS",  4,    2),
                MakeBOMItem(T, now, bomBookshelf.Id, rmParticleBoard, "PCS",  5,    1),
                MakeBOMItem(T, now, bomBookshelf.Id, rmScrews,        "PKT",  1,    0),
                MakeBOMItem(T, now, bomBookshelf.Id, rmPaint,         "LTR",  0.2m, 0),
            };
            _ctx.BOMItems.AddRange(bomBookshelfItems);
            _ctx.BOMByProducts.Add(MakeBOMByProduct(T, now, bomBookshelf.Id, rmSteelSheet, "Scrap", "KG", 0.3m, 4));
            await _ctx.SaveChangesAsync();

            // ── BOM: Height-Adjustable Desk ───────────────────────────────────
            var bomDesk = MakeBOM(T, now, fgDesk, 1);
            _ctx.BillsOfMaterial.Add(bomDesk);
            await _ctx.SaveChangesAsync();

            var bomDeskItems = new[]
            {
                MakeBOMItem(T, now, bomDesk.Id, rmSteelTube,  "PCS",  2,    2),
                MakeBOMItem(T, now, bomDesk.Id, rmWoodPanel,  "PCS",  4,    1),
                MakeBOMItem(T, now, bomDesk.Id, rmScrews,     "PKT",  2,    0),
                MakeBOMItem(T, now, bomDesk.Id, rmPaint,      "LTR",  0.5m, 0),
                MakeBOMItem(T, now, bomDesk.Id, rmGasLift,    "PCS",  1,    0),
            };
            _ctx.BOMItems.AddRange(bomDeskItems);
            await _ctx.SaveChangesAsync();

            // ── BOM: 6-Seater Meeting Table ───────────────────────────────────
            var bomTable = MakeBOM(T, now, fgTable, 1);
            _ctx.BillsOfMaterial.Add(bomTable);
            await _ctx.SaveChangesAsync();

            var bomTableItems = new[]
            {
                MakeBOMItem(T, now, bomTable.Id, rmSteelTube, "PCS",  6,    1),
                MakeBOMItem(T, now, bomTable.Id, rmWoodPanel, "PCS",  8,    1),
                MakeBOMItem(T, now, bomTable.Id, rmScrews,    "PKT",  3,    0),
                MakeBOMItem(T, now, bomTable.Id, rmPaint,     "LTR",  1.0m, 0),
                MakeBOMItem(T, now, bomTable.Id, rmMdfBoard,  "PCS",  2,    1),
            };
            _ctx.BOMItems.AddRange(bomTableItems);
            await _ctx.SaveChangesAsync();

            // ── Routing: Executive Ergonomic Chair ────────────────────────────
            var routingChair = MakeRouting(T, now, fgChair, "Executive Chair Routing", 1);
            _ctx.Routings.Add(routingChair);
            await _ctx.SaveChangesAsync();

            var routingChairOps = new[]
            {
                MakeRoutingOp(T, now, routingChair.Id, wcCutting.Id,  10, "Cut & Shape Steel Tubes",      0.50m, 0.25m, 0,     1),
                MakeRoutingOp(T, now, routingChair.Id, wcWelding.Id,  20, "Weld Chair Frame",              0.50m, 0,     0.50m, 2),
                MakeRoutingOp(T, now, routingChair.Id, wcPainting.Id, 30, "Powder Coat Frame",             0.25m, 0,     0.25m, 3),
                MakeRoutingOp(T, now, routingChair.Id, wcAssembly.Id, 40, "Assemble Seat, Back & Casters", 0.75m, 0.50m, 0,     4),
                MakeRoutingOp(T, now, routingChair.Id, wcQC.Id,       50, "Final Quality Inspection",      0.25m, 0,     0,     5),
                MakeRoutingOp(T, now, routingChair.Id, wcPackaging.Id,60, "Pack & Label",                  0.25m, 0,     0,     6),
            };
            _ctx.RoutingOperations.AddRange(routingChairOps);
            await _ctx.SaveChangesAsync();

            // ── Routing: 4-Drawer Filing Cabinet ─────────────────────────────
            var routingCabinet = MakeRouting(T, now, fgCabinet, "Filing Cabinet Routing", 1);
            _ctx.Routings.Add(routingCabinet);
            await _ctx.SaveChangesAsync();

            var routingCabinetOps = new[]
            {
                MakeRoutingOp(T, now, routingCabinet.Id, wcMachining.Id, 10, "Cut & Press Steel Sheets",   0.75m, 0.50m, 0,     1),
                MakeRoutingOp(T, now, routingCabinet.Id, wcWelding.Id,   20, "Weld Cabinet Frame & Body",  1.00m, 0,     1.00m, 2),
                MakeRoutingOp(T, now, routingCabinet.Id, wcMachining.Id, 30, "Cut MDF Drawer Bases",       0.50m, 0.25m, 0,     3),
                MakeRoutingOp(T, now, routingCabinet.Id, wcAssembly.Id,  40, "Assemble Drawers & Rails",   1.00m, 0.75m, 0,     4),
                MakeRoutingOp(T, now, routingCabinet.Id, wcPainting.Id,  50, "Powder Coat Cabinet",        0.75m, 0,     0.75m, 5),
                MakeRoutingOp(T, now, routingCabinet.Id, wcAssembly.Id,  60, "Fit Handles & Lock",         0.25m, 0.25m, 0,     6),
                MakeRoutingOp(T, now, routingCabinet.Id, wcQC.Id,        70, "Quality Inspection",         0.50m, 0,     0,     7),
                MakeRoutingOp(T, now, routingCabinet.Id, wcPackaging.Id, 80, "Pack for Delivery",          0.50m, 0,     0,     8),
            };
            _ctx.RoutingOperations.AddRange(routingCabinetOps);
            await _ctx.SaveChangesAsync();

            // ── Routing: 3-Tier Steel Bookshelf ──────────────────────────────
            var routingBookshelf = MakeRouting(T, now, fgBookshelf, "Steel Bookshelf Routing", 1);
            _ctx.Routings.Add(routingBookshelf);
            await _ctx.SaveChangesAsync();

            var routingBookshelfOps = new[]
            {
                MakeRoutingOp(T, now, routingBookshelf.Id, wcCutting.Id,  10, "Cut Steel Sheet to Profile",  0.50m, 0.30m, 0,     1),
                MakeRoutingOp(T, now, routingBookshelf.Id, wcWelding.Id,  20, "Weld Steel Frame",             0.75m, 0,     0.75m, 2),
                MakeRoutingOp(T, now, routingBookshelf.Id, wcMachining.Id,30, "Cut Particle Board Shelves",   0.40m, 0.25m, 0,     3),
                MakeRoutingOp(T, now, routingBookshelf.Id, wcPainting.Id, 40, "Paint Frame",                  0.25m, 0,     0.25m, 4),
                MakeRoutingOp(T, now, routingBookshelf.Id, wcAssembly.Id, 50, "Assemble Shelves to Frame",    0.50m, 0.25m, 0,     5),
                MakeRoutingOp(T, now, routingBookshelf.Id, wcQC.Id,       60, "Quality Inspection",           0.25m, 0,     0,     6),
                MakeRoutingOp(T, now, routingBookshelf.Id, wcPackaging.Id,70, "Pack & Label",                 0.25m, 0,     0,     7),
            };
            _ctx.RoutingOperations.AddRange(routingBookshelfOps);
            await _ctx.SaveChangesAsync();

            // ── Routing: Height-Adjustable Desk ──────────────────────────────
            var routingDesk = MakeRouting(T, now, fgDesk, "Height-Adjustable Desk Routing", 1);
            _ctx.Routings.Add(routingDesk);
            await _ctx.SaveChangesAsync();

            var routingDeskOps = new[]
            {
                MakeRoutingOp(T, now, routingDesk.Id, wcMachining.Id, 10, "Cut & Shape Wood Panels",    0.50m, 0.25m, 0,     1),
                MakeRoutingOp(T, now, routingDesk.Id, wcCutting.Id,   20, "Cut Steel Frame Tubes",      0.40m, 0.20m, 0,     2),
                MakeRoutingOp(T, now, routingDesk.Id, wcWelding.Id,   30, "Weld Steel Frame",            0.60m, 0,     0.60m, 3),
                MakeRoutingOp(T, now, routingDesk.Id, wcAssembly.Id,  40, "Assemble Desk & Gas Lift",   0.75m, 0.50m, 0,     4),
                MakeRoutingOp(T, now, routingDesk.Id, wcPainting.Id,  50, "Paint Frame & Lacquer Top",  0.50m, 0,     0.50m, 5),
                MakeRoutingOp(T, now, routingDesk.Id, wcQC.Id,        60, "Load & Height Test",          0.25m, 0,     0,     6),
                MakeRoutingOp(T, now, routingDesk.Id, wcPackaging.Id, 70, "Pack & Secure for Shipping", 0.30m, 0,     0,     7),
            };
            _ctx.RoutingOperations.AddRange(routingDeskOps);
            await _ctx.SaveChangesAsync();

            // ── Routing: 6-Seater Meeting Table ──────────────────────────────
            var routingTable = MakeRouting(T, now, fgTable, "Meeting Table Routing", 1);
            _ctx.Routings.Add(routingTable);
            await _ctx.SaveChangesAsync();

            var routingTableOps = new[]
            {
                MakeRoutingOp(T, now, routingTable.Id, wcMachining.Id, 10, "Machine Wood Top & MDF Trim", 1.00m, 0.75m, 0,     1),
                MakeRoutingOp(T, now, routingTable.Id, wcCutting.Id,   20, "Cut Steel Leg Tubes",         0.50m, 0.30m, 0,     2),
                MakeRoutingOp(T, now, routingTable.Id, wcWelding.Id,   30, "Weld Table Base Frame",        1.00m, 0,     1.00m, 3),
                MakeRoutingOp(T, now, routingTable.Id, wcPainting.Id,  40, "Powder Coat Steel Base",       0.75m, 0,     0.75m, 4),
                MakeRoutingOp(T, now, routingTable.Id, wcAssembly.Id,  50, "Assemble Top to Base",         1.00m, 0.50m, 0,     5),
                MakeRoutingOp(T, now, routingTable.Id, wcAssembly.Id,  60, "Sand, Polish & Cable Grommets",0.50m, 0.25m, 0,     6),
                MakeRoutingOp(T, now, routingTable.Id, wcQC.Id,        70, "Final Quality Inspection",     0.50m, 0,     0,     7),
                MakeRoutingOp(T, now, routingTable.Id, wcPackaging.Id, 80, "Pack & Pallet for Delivery",   0.75m, 0,     0,     8),
            };
            _ctx.RoutingOperations.AddRange(routingTableOps);
            await _ctx.SaveChangesAsync();

            // ── Demands ───────────────────────────────────────────────────────
            var demandChair1   = MakeDemand(T, now, fgChair,     120, now.AddDays(-30), "SalesOrder", "Fulfilled");
            var demandCabinet  = MakeDemand(T, now, fgCabinet,    60, now.AddDays(-20), "SalesOrder", "Fulfilled");
            var demandBookshelf= MakeDemand(T, now, fgBookshelf,  80, now.AddDays(-15), "SalesOrder", "Fulfilled");
            var demandDesk     = MakeDemand(T, now, fgDesk,       50, now.AddDays( 10), "SalesOrder", "Open");
            var demandTable    = MakeDemand(T, now, fgTable,      20, now.AddDays( 20), "SalesOrder", "Open");
            var demandChair2   = MakeDemand(T, now, fgChair,     150, now.AddDays( 35), "SalesOrder", "Open");
            _ctx.Demands.AddRange(demandChair1, demandCabinet, demandBookshelf, demandDesk, demandTable, demandChair2);
            await _ctx.SaveChangesAsync();

            // ── Planned Orders ────────────────────────────────────────────────
            var poPlannedChair1   = MakePlannedOrder(T, now, fgChair,    100, now.AddDays(-35), now.AddDays(-20), "Converted");
            var poPlannedCabinet  = MakePlannedOrder(T, now, fgCabinet,   60, now.AddDays(-25), now.AddDays(-15), "Converted");
            var poPlannedBookshelf= MakePlannedOrder(T, now, fgBookshelf,  80, now.AddDays(-20), now.AddDays(-10), "Converted");
            var poPlannedDesk     = MakePlannedOrder(T, now, fgDesk,       50, now.AddDays( -4), now.AddDays( 10), "Converted");
            var poPlannedTable    = MakePlannedOrder(T, now, fgTable,      20, now,              now.AddDays( 20), "Planned");
            var poPlannedChair2   = MakePlannedOrder(T, now, fgChair,    150, now.AddDays(  5), now.AddDays( 30), "Planned");
            _ctx.PlannedOrders.AddRange(poPlannedChair1, poPlannedCabinet, poPlannedBookshelf,
                poPlannedDesk, poPlannedTable, poPlannedChair2);
            await _ctx.SaveChangesAsync();

            // ══════════════════════════════════════════════════════════════════
            // PO-0001: Executive Ergonomic Chair × 100 — COMPLETED
            // ══════════════════════════════════════════════════════════════════
            var po1 = MakePO(T, now, $"PO-{year}-0001", fgChair,
                bomChair.Id, routingChair.Id, poPlannedChair1.Id,
                100, 97, 3, "Completed",
                now.AddDays(-30), now.AddDays(-22), now.AddDays(-20),
                "Completed on time. 3 units rejected at final QC due to gas lift preload variance.");
            _ctx.ProductionOrders.Add(po1);
            await _ctx.SaveChangesAsync();

            var po1Ops = new[]
            {
                MakePOOp(T, now, po1.Id, routingChairOps[0].Id, wcCutting.Id,  10, "Cut & Shape Steel Tubes",      0.50m, 0.25m, 0.48m, 0.24m, "Completed"),
                MakePOOp(T, now, po1.Id, routingChairOps[1].Id, wcWelding.Id,  20, "Weld Chair Frame",              0.50m, 0,     0.52m, 0,     "Completed"),
                MakePOOp(T, now, po1.Id, routingChairOps[2].Id, wcPainting.Id, 30, "Powder Coat Frame",             0.25m, 0,     0.25m, 0,     "Completed"),
                MakePOOp(T, now, po1.Id, routingChairOps[3].Id, wcAssembly.Id, 40, "Assemble Seat, Back & Casters", 0.75m, 0.50m, 0.78m, 0.52m, "Completed"),
                MakePOOp(T, now, po1.Id, routingChairOps[4].Id, wcQC.Id,       50, "Final Quality Inspection",      0.25m, 0,     0.28m, 0,     "Completed"),
                MakePOOp(T, now, po1.Id, routingChairOps[5].Id, wcPackaging.Id,60, "Pack & Label",                  0.25m, 0,     0.25m, 0,     "Completed"),
            };
            _ctx.ProductionOrderOperations.AddRange(po1Ops);

            var po1Comps = new[]
            {
                MakePOComp(T, now, po1.Id, bomChairItems[0].Id, rmSteelTube,    "PCS",  400, 396, 0, 2),
                MakePOComp(T, now, po1.Id, bomChairItems[1].Id, rmFabric,       "MTR",  150, 150, 2, 1),
                MakePOComp(T, now, po1.Id, bomChairItems[2].Id, rmFoam,         "KG",    80,  79, 0, 0),
                MakePOComp(T, now, po1.Id, bomChairItems[3].Id, rmScrews,       "PKT",  100, 100, 0, 0),
                MakePOComp(T, now, po1.Id, bomChairItems[4].Id, rmPaint,        "LTR",   30,  29, 0, 0),
                MakePOComp(T, now, po1.Id, bomChairItems[5].Id, rmGasLift,      "PCS",  100,  97, 0, 0),
                MakePOComp(T, now, po1.Id, bomChairItems[6].Id, rmCasterWheels, "PCS",  100, 100, 0, 0),
            };
            _ctx.ProductionOrderComponents.AddRange(po1Comps);
            await _ctx.SaveChangesAsync();

            _ctx.ProductionSchedules.AddRange(
                MakeSchedule(T, now, po1.Id, wcCutting.Id,  po1Ops[0].Id, "Forward", now.AddDays(-30), now.AddDays(-29), 0.50m, "Completed"),
                MakeSchedule(T, now, po1.Id, wcWelding.Id,  po1Ops[1].Id, "Forward", now.AddDays(-29), now.AddDays(-27), 0.50m, "Completed"),
                MakeSchedule(T, now, po1.Id, wcPainting.Id, po1Ops[2].Id, "Forward", now.AddDays(-27), now.AddDays(-26), 0.25m, "Completed"),
                MakeSchedule(T, now, po1.Id, wcAssembly.Id, po1Ops[3].Id, "Forward", now.AddDays(-26), now.AddDays(-24), 0.75m, "Completed"),
                MakeSchedule(T, now, po1.Id, wcQC.Id,       po1Ops[4].Id, "Forward", now.AddDays(-24), now.AddDays(-23), 0.25m, "Completed"),
                MakeSchedule(T, now, po1.Id, wcPackaging.Id,po1Ops[5].Id, "Forward", now.AddDays(-23), now.AddDays(-22), 0.25m, "Completed"));
            await _ctx.SaveChangesAsync();

            _ctx.MaterialIssues.AddRange(
                MakeMatIssue(T, now, po1.Id, rmSteelTube,    396, now.AddDays(-30), userId),
                MakeMatIssue(T, now, po1.Id, rmFabric,       150, now.AddDays(-29), userId),
                MakeMatIssue(T, now, po1.Id, rmFoam,          79, now.AddDays(-29), userId),
                MakeMatIssue(T, now, po1.Id, rmScrews,       100, now.AddDays(-28), userId),
                MakeMatIssue(T, now, po1.Id, rmPaint,         29, now.AddDays(-27), userId),
                MakeMatIssue(T, now, po1.Id, rmGasLift,       97, now.AddDays(-26), userId),
                MakeMatIssue(T, now, po1.Id, rmCasterWheels, 100, now.AddDays(-26), userId));

            _ctx.WorkInProgress.Add(MakeWIP(T, now, po1.Id, 0, 97, 3,
                "All 97 accepted units transferred to finished goods. 3 rejected units queued for rework — gas lift preload out of tolerance."));
            await _ctx.SaveChangesAsync();

            var po1Insp = MakeInspection(T, now, po1.Id, 100, 97, 3, userId, now.AddDays(-23),
                "Passed", "97/100 passed. 3 units failed gas lift height-lock test — cylinder preload below minimum.");
            _ctx.Inspections.Add(po1Insp);
            await _ctx.SaveChangesAsync();

            _ctx.InspectionCharacteristics.AddRange(
                MakeInspChar(T, now, po1Insp.Id, "Seat Height Adjustment Range", "Quantitative", "MM",  460, 440, 510, 468,  null,   "Pass", true),
                MakeInspChar(T, now, po1Insp.Id, "Backrest Lumbar Angle",        "Quantitative", "DEG", 110, 100, 120, 112,  null,   "Pass", false),
                MakeInspChar(T, now, po1Insp.Id, "Load Capacity 135kg",          "Qualitative",  null,  null,null,null, null,"Pass", "Pass", true),
                MakeInspChar(T, now, po1Insp.Id, "Gas Lift Height-Lock Torque",  "Quantitative", "NM",   18,  16,  22,  15m, null,   "Fail", true),
                MakeInspChar(T, now, po1Insp.Id, "Caster Wheel Roll Resistance", "Quantitative", "N",    8,   5,  12,   8,  null,   "Pass", false),
                MakeInspChar(T, now, po1Insp.Id, "Powder Coat Adhesion",         "Qualitative",  null,  null,null,null, null,"Pass", "Pass", true));
            await _ctx.SaveChangesAsync();

            var po1FGR = MakeFGR(T, now, po1.Id, fgChair, 97, whFinishedGoods, $"BTH-{year}-001", now.AddDays(-22));
            _ctx.FinishedGoodsReceipts.Add(po1FGR);
            await _ctx.SaveChangesAsync();

            _ctx.ProductionBatches.Add(MakeBatch(T, now, po1.Id, po1Insp.Id, fgChair,
                $"BTH-{year}-001", now.AddDays(-22), null, null,
                97, "PCS", whFinishedGoods, true, $"COA-{year}-001"));
            await _ctx.SaveChangesAsync();

            // Sub-contract: powder coating outsourced to specialist vendor for PO-1
            _ctx.SubContractOrders.Add(MakeSubCon(T, now, po1.Id, po1Ops[2].Id,
                whRawMaterials, null,
                100, now.AddDays(-27), now.AddDays(-26), now.AddDays(-26),
                4.5m, 450, 0, "Completed"));
            await _ctx.SaveChangesAsync();

            var po1Cost = MakeCostEntry(T, now, po1.Id, 9600, 3650, 2900, 1890, 220, now.AddDays(-22));
            _ctx.CostEntries.Add(po1Cost);
            await _ctx.SaveChangesAsync();

            _ctx.ProductionVariances.Add(MakeVariance(T, now, po1.Id, po1Cost.Id,
                scChair.MaterialCost * 100, po1Cost.MaterialCost,
                scChair.LaborCost    * 100, po1Cost.LaborCost,
                scChair.MachineCost  * 100, po1Cost.MachineCost ?? 0,
                scChair.OverheadCost * 100, po1Cost.OverheadCost,
                "Material", "Favorable: steel tube yield improved by pre-cutting to exact lengths, reducing scrap by 0.8%"));

            _ctx.ReworkOrders.Add(MakeRework(T, now, po1.Id, po1Insp.Id, routingChair.Id,
                3, "PCS", "Gas lift cylinder preload below 16 Nm — replace cylinder and retest",
                now.AddDays(-21), now.AddDays(-19)));

            _ctx.MachineDowntimes.Add(MakeDowntime(T, now, wcWelding.Id, po1.Id,
                now.AddDays(-29).AddHours(10), now.AddDays(-29).AddHours(11.5),
                1.5m, "Mechanical", "MIG welder wire-feed motor stall — burn-back on contact tip",
                "Replaced contact tip and wire liner; ran test beads to verify arc stability",
                userId, "Resolved"));
            await _ctx.SaveChangesAsync();

            // ══════════════════════════════════════════════════════════════════
            // PO-0002: 4-Drawer Filing Cabinet × 60 — COMPLETED
            // ══════════════════════════════════════════════════════════════════
            var po2 = MakePO(T, now, $"PO-{year}-0002", fgCabinet,
                bomCabinet.Id, routingCabinet.Id, poPlannedCabinet.Id,
                60, 58, 2, "Completed",
                now.AddDays(-22), now.AddDays(-14), now.AddDays(-12),
                "Completed. 2 units rejected at QC — drawer slide misalignment.");
            _ctx.ProductionOrders.Add(po2);
            await _ctx.SaveChangesAsync();

            var po2Ops = new[]
            {
                MakePOOp(T, now, po2.Id, routingCabinetOps[0].Id, wcMachining.Id, 10, "Cut & Press Steel Sheets",  0.75m, 0.50m, 0.72m, 0.48m, "Completed"),
                MakePOOp(T, now, po2.Id, routingCabinetOps[1].Id, wcWelding.Id,   20, "Weld Cabinet Frame & Body", 1.00m, 0,     1.05m, 0,     "Completed"),
                MakePOOp(T, now, po2.Id, routingCabinetOps[2].Id, wcMachining.Id, 30, "Cut MDF Drawer Bases",      0.50m, 0.25m, 0.48m, 0.24m, "Completed"),
                MakePOOp(T, now, po2.Id, routingCabinetOps[3].Id, wcAssembly.Id,  40, "Assemble Drawers & Rails",  1.00m, 0.75m, 1.05m, 0.78m, "Completed"),
                MakePOOp(T, now, po2.Id, routingCabinetOps[4].Id, wcPainting.Id,  50, "Powder Coat Cabinet",       0.75m, 0,     0.75m, 0,     "Completed"),
                MakePOOp(T, now, po2.Id, routingCabinetOps[5].Id, wcAssembly.Id,  60, "Fit Handles & Lock",        0.25m, 0.25m, 0.26m, 0.26m, "Completed"),
                MakePOOp(T, now, po2.Id, routingCabinetOps[6].Id, wcQC.Id,        70, "Quality Inspection",        0.50m, 0,     0.55m, 0,     "Completed"),
                MakePOOp(T, now, po2.Id, routingCabinetOps[7].Id, wcPackaging.Id, 80, "Pack for Delivery",         0.50m, 0,     0.50m, 0,     "Completed"),
            };
            _ctx.ProductionOrderOperations.AddRange(po2Ops);

            var po2Comps = new[]
            {
                MakePOComp(T, now, po2.Id, bomCabinetItems[0].Id, rmSteelSheet, "PCS", 480, 476, 0, 2),
                MakePOComp(T, now, po2.Id, bomCabinetItems[1].Id, rmMdfBoard,   "PCS", 180, 178, 0, 1),
                MakePOComp(T, now, po2.Id, bomCabinetItems[2].Id, rmHandles,    "PCS", 240, 240, 0, 0),
                MakePOComp(T, now, po2.Id, bomCabinetItems[3].Id, rmScrews,     "PKT", 120, 118, 5, 0),
                MakePOComp(T, now, po2.Id, bomCabinetItems[4].Id, rmPaint,      "LTR",  30,  29, 0, 0),
            };
            _ctx.ProductionOrderComponents.AddRange(po2Comps);
            await _ctx.SaveChangesAsync();

            _ctx.ProductionSchedules.AddRange(
                MakeSchedule(T, now, po2.Id, wcMachining.Id, po2Ops[0].Id, "Forward", now.AddDays(-22), now.AddDays(-21), 0.75m, "Completed"),
                MakeSchedule(T, now, po2.Id, wcWelding.Id,   po2Ops[1].Id, "Forward", now.AddDays(-21), now.AddDays(-19), 1.00m, "Completed"),
                MakeSchedule(T, now, po2.Id, wcMachining.Id, po2Ops[2].Id, "Forward", now.AddDays(-19), now.AddDays(-18), 0.50m, "Completed"),
                MakeSchedule(T, now, po2.Id, wcAssembly.Id,  po2Ops[3].Id, "Forward", now.AddDays(-18), now.AddDays(-16), 1.00m, "Completed"),
                MakeSchedule(T, now, po2.Id, wcPainting.Id,  po2Ops[4].Id, "Forward", now.AddDays(-16), now.AddDays(-15), 0.75m, "Completed"),
                MakeSchedule(T, now, po2.Id, wcAssembly.Id,  po2Ops[5].Id, "Forward", now.AddDays(-15), now.AddDays(-15), 0.25m, "Completed"),
                MakeSchedule(T, now, po2.Id, wcQC.Id,        po2Ops[6].Id, "Forward", now.AddDays(-15), now.AddDays(-14), 0.50m, "Completed"),
                MakeSchedule(T, now, po2.Id, wcPackaging.Id, po2Ops[7].Id, "Forward", now.AddDays(-14), now.AddDays(-14), 0.50m, "Completed"));
            await _ctx.SaveChangesAsync();

            _ctx.MaterialIssues.AddRange(
                MakeMatIssue(T, now, po2.Id, rmSteelSheet, 476, now.AddDays(-22), userId),
                MakeMatIssue(T, now, po2.Id, rmMdfBoard,   178, now.AddDays(-21), userId),
                MakeMatIssue(T, now, po2.Id, rmHandles,    240, now.AddDays(-20), userId),
                MakeMatIssue(T, now, po2.Id, rmScrews,     118, now.AddDays(-19), userId),
                MakeMatIssue(T, now, po2.Id, rmPaint,       29, now.AddDays(-17), userId));

            _ctx.WorkInProgress.Add(MakeWIP(T, now, po2.Id, 0, 58, 2,
                "58 units accepted and transferred. 2 rejected for drawer slide misalignment — sent for rework."));
            await _ctx.SaveChangesAsync();

            var po2Insp = MakeInspection(T, now, po2.Id, 60, 58, 2, userId, now.AddDays(-14),
                "Passed", "58/60 passed. 2 units failed drawer slide binding test — tolerance deviation at third-drawer rail mount.");
            _ctx.Inspections.Add(po2Insp);
            await _ctx.SaveChangesAsync();

            _ctx.InspectionCharacteristics.AddRange(
                MakeInspChar(T, now, po2Insp.Id, "Drawer Slide Binding Test",    "Qualitative",  null,  null,null,null, null,"Pass", "Pass", true),
                MakeInspChar(T, now, po2Insp.Id, "Cabinet Squareness",           "Quantitative", "MM",   0,   0, 1.5m, 0.6m,null,   "Pass", true),
                MakeInspChar(T, now, po2Insp.Id, "Lock Mechanism Cycle Test",    "Qualitative",  null,  null,null,null, null,"Pass", "Pass", true),
                MakeInspChar(T, now, po2Insp.Id, "Powder Coat Thickness",        "Quantitative", "UM",   70,  60,  90,   74, null,   "Pass", false),
                MakeInspChar(T, now, po2Insp.Id, "Load per Drawer (15kg)",       "Qualitative",  null,  null,null,null, null,"Pass", "Pass", true));
            await _ctx.SaveChangesAsync();

            var po2FGR = MakeFGR(T, now, po2.Id, fgCabinet, 58, whFinishedGoods, $"BTH-{year}-002", now.AddDays(-14));
            _ctx.FinishedGoodsReceipts.Add(po2FGR);
            await _ctx.SaveChangesAsync();

            _ctx.ProductionBatches.Add(MakeBatch(T, now, po2.Id, po2Insp.Id, fgCabinet,
                $"BTH-{year}-002", now.AddDays(-14), null, null,
                58, "PCS", whFinishedGoods, true, $"COA-{year}-002"));
            await _ctx.SaveChangesAsync();

            // Sub-contract: welding frame outsourced for 20 units to meet deadline
            _ctx.SubContractOrders.Add(MakeSubCon(T, now, po2.Id, po2Ops[1].Id,
                whRawMaterials, null,
                20, now.AddDays(-21), now.AddDays(-19), now.AddDays(-19),
                18, 360, 0, "Completed"));
            await _ctx.SaveChangesAsync();

            var po2Cost = MakeCostEntry(T, now, po2.Id, 6480, 2620, 2160, 1480, 140, now.AddDays(-14));
            _ctx.CostEntries.Add(po2Cost);
            await _ctx.SaveChangesAsync();

            _ctx.ProductionVariances.Add(MakeVariance(T, now, po2.Id, po2Cost.Id,
                scCabinet.MaterialCost * 60, po2Cost.MaterialCost,
                scCabinet.LaborCost    * 60, po2Cost.LaborCost,
                scCabinet.MachineCost  * 60, po2Cost.MachineCost ?? 0,
                scCabinet.OverheadCost * 60, po2Cost.OverheadCost,
                "Labor", "Unfavorable: drawer assembly time exceeded standard by 5% due to tighter rail tolerances on new steel supplier batch"));

            _ctx.ReworkOrders.Add(MakeRework(T, now, po2.Id, po2Insp.Id, routingCabinet.Id,
                2, "PCS", "Drawer slide rail mount off by 1.8mm — disassemble, redrill, reassemble and retest",
                now.AddDays(-13), now.AddDays(-11)));

            _ctx.MachineDowntimes.Add(MakeDowntime(T, now, wcMachining.Id, po2.Id,
                now.AddDays(-20).AddHours(9), now.AddDays(-20).AddHours(10.5),
                1.5m, "Tooling", "CNC punch press die misalignment — burr on punched holes",
                "Re-seated die set; recalibrated press tonnage; ran 5 test pieces",
                userId, "Resolved"));
            await _ctx.SaveChangesAsync();

            // ══════════════════════════════════════════════════════════════════
            // PO-0003: 3-Tier Steel Bookshelf × 80 — COMPLETED
            // ══════════════════════════════════════════════════════════════════
            var po3 = MakePO(T, now, $"PO-{year}-0003", fgBookshelf,
                bomBookshelf.Id, routingBookshelf.Id, poPlannedBookshelf.Id,
                80, 79, 1, "Completed",
                now.AddDays(-18), now.AddDays(-9), now.AddDays(-8),
                "Near-perfect run. 1 unit rejected — particle board delamination on top shelf.");
            _ctx.ProductionOrders.Add(po3);
            await _ctx.SaveChangesAsync();

            var po3Ops = new[]
            {
                MakePOOp(T, now, po3.Id, routingBookshelfOps[0].Id, wcCutting.Id,  10, "Cut Steel Sheet to Profile", 0.50m, 0.30m, 0.49m, 0.29m, "Completed"),
                MakePOOp(T, now, po3.Id, routingBookshelfOps[1].Id, wcWelding.Id,  20, "Weld Steel Frame",            0.75m, 0,     0.76m, 0,     "Completed"),
                MakePOOp(T, now, po3.Id, routingBookshelfOps[2].Id, wcMachining.Id,30, "Cut Particle Board Shelves",  0.40m, 0.25m, 0.40m, 0.24m, "Completed"),
                MakePOOp(T, now, po3.Id, routingBookshelfOps[3].Id, wcPainting.Id, 40, "Paint Frame",                 0.25m, 0,     0.25m, 0,     "Completed"),
                MakePOOp(T, now, po3.Id, routingBookshelfOps[4].Id, wcAssembly.Id, 50, "Assemble Shelves to Frame",   0.50m, 0.25m, 0.50m, 0.25m, "Completed"),
                MakePOOp(T, now, po3.Id, routingBookshelfOps[5].Id, wcQC.Id,       60, "Quality Inspection",          0.25m, 0,     0.25m, 0,     "Completed"),
                MakePOOp(T, now, po3.Id, routingBookshelfOps[6].Id, wcPackaging.Id,70, "Pack & Label",                0.25m, 0,     0.25m, 0,     "Completed"),
            };
            _ctx.ProductionOrderOperations.AddRange(po3Ops);

            var po3Comps = new[]
            {
                MakePOComp(T, now, po3.Id, bomBookshelfItems[0].Id, rmSteelSheet,    "PCS", 320, 318, 0, 2),
                MakePOComp(T, now, po3.Id, bomBookshelfItems[1].Id, rmParticleBoard, "PCS", 400, 399, 2, 1),
                MakePOComp(T, now, po3.Id, bomBookshelfItems[2].Id, rmScrews,        "PKT",  80,  79, 0, 0),
                MakePOComp(T, now, po3.Id, bomBookshelfItems[3].Id, rmPaint,         "LTR",  16,  15, 0, 0),
            };
            _ctx.ProductionOrderComponents.AddRange(po3Comps);
            await _ctx.SaveChangesAsync();

            _ctx.ProductionSchedules.AddRange(
                MakeSchedule(T, now, po3.Id, wcCutting.Id,  po3Ops[0].Id, "Forward", now.AddDays(-18), now.AddDays(-17), 0.50m, "Completed"),
                MakeSchedule(T, now, po3.Id, wcWelding.Id,  po3Ops[1].Id, "Forward", now.AddDays(-17), now.AddDays(-15), 0.75m, "Completed"),
                MakeSchedule(T, now, po3.Id, wcMachining.Id,po3Ops[2].Id, "Forward", now.AddDays(-15), now.AddDays(-14), 0.40m, "Completed"),
                MakeSchedule(T, now, po3.Id, wcPainting.Id, po3Ops[3].Id, "Forward", now.AddDays(-14), now.AddDays(-13), 0.25m, "Completed"),
                MakeSchedule(T, now, po3.Id, wcAssembly.Id, po3Ops[4].Id, "Forward", now.AddDays(-13), now.AddDays(-11), 0.50m, "Completed"),
                MakeSchedule(T, now, po3.Id, wcQC.Id,       po3Ops[5].Id, "Forward", now.AddDays(-11), now.AddDays(-10), 0.25m, "Completed"),
                MakeSchedule(T, now, po3.Id, wcPackaging.Id,po3Ops[6].Id, "Forward", now.AddDays(-10), now.AddDays( -9), 0.25m, "Completed"));
            await _ctx.SaveChangesAsync();

            _ctx.MaterialIssues.AddRange(
                MakeMatIssue(T, now, po3.Id, rmSteelSheet,    318, now.AddDays(-18), userId),
                MakeMatIssue(T, now, po3.Id, rmParticleBoard, 399, now.AddDays(-17), userId),
                MakeMatIssue(T, now, po3.Id, rmScrews,         79, now.AddDays(-16), userId),
                MakeMatIssue(T, now, po3.Id, rmPaint,          15, now.AddDays(-15), userId));

            _ctx.WorkInProgress.Add(MakeWIP(T, now, po3.Id, 0, 79, 1,
                "79 units accepted and transferred to finished goods. 1 unit rejected — particle board delamination."));
            await _ctx.SaveChangesAsync();

            var po3Insp = MakeInspection(T, now, po3.Id, 80, 79, 1, userId, now.AddDays(-10),
                "Passed", "79/80 passed. 1 unit failed — visible delamination on top particle board shelf, traced to moisture exposure in raw material storage.");
            _ctx.Inspections.Add(po3Insp);
            await _ctx.SaveChangesAsync();

            _ctx.InspectionCharacteristics.AddRange(
                MakeInspChar(T, now, po3Insp.Id, "Shelf Load 30kg each",         "Qualitative",  null,  null,null,null, null,"Pass", "Pass", true),
                MakeInspChar(T, now, po3Insp.Id, "Frame Squareness",             "Quantitative", "MM",   0,   0,   2, 0.8m, null,   "Pass", true),
                MakeInspChar(T, now, po3Insp.Id, "Particle Board Surface Finish","Qualitative",  null,  null,null,null, null,"Pass", "Pass", true),
                MakeInspChar(T, now, po3Insp.Id, "Paint Coat Thickness",         "Quantitative", "UM",   65,  55,  80,   68, null,   "Pass", false),
                MakeInspChar(T, now, po3Insp.Id, "Overall Height Tolerance",     "Quantitative", "MM",1800,1795,1805,1801, null,   "Pass", true));
            await _ctx.SaveChangesAsync();

            var po3FGR = MakeFGR(T, now, po3.Id, fgBookshelf, 79, whFinishedGoods, $"BTH-{year}-003", now.AddDays(-9));
            _ctx.FinishedGoodsReceipts.Add(po3FGR);
            await _ctx.SaveChangesAsync();

            _ctx.ProductionBatches.Add(MakeBatch(T, now, po3.Id, po3Insp.Id, fgBookshelf,
                $"BTH-{year}-003", now.AddDays(-9), null, null,
                79, "PCS", whFinishedGoods, true, $"COA-{year}-003"));
            await _ctx.SaveChangesAsync();

            // Sub-contract: frame painting outsourced (paint booth fully booked)
            _ctx.SubContractOrders.Add(MakeSubCon(T, now, po3.Id, po3Ops[3].Id,
                whRawMaterials, null,
                80, now.AddDays(-14), now.AddDays(-13), now.AddDays(-13),
                3.8m, 304, 0, "Completed"));
            await _ctx.SaveChangesAsync();

            var po3Cost = MakeCostEntry(T, now, po3.Id, 5440, 2100, 1720, 1100,  80, now.AddDays(-9));
            _ctx.CostEntries.Add(po3Cost);
            await _ctx.SaveChangesAsync();

            _ctx.ProductionVariances.Add(MakeVariance(T, now, po3.Id, po3Cost.Id,
                scBookshelf.MaterialCost * 80, po3Cost.MaterialCost,
                scBookshelf.LaborCost    * 80, po3Cost.LaborCost,
                scBookshelf.MachineCost  * 80, po3Cost.MachineCost ?? 0,
                scBookshelf.OverheadCost * 80, po3Cost.OverheadCost,
                "Material", "Favorable: steel sheet blanks pre-cut by supplier reduced cutting time and material waste"));

            _ctx.ReworkOrders.Add(MakeRework(T, now, po3.Id, po3Insp.Id, routingBookshelf.Id,
                1, "PCS", "Particle board delamination — replace top shelf board and refinish",
                now.AddDays(-8), now.AddDays(-7)));

            _ctx.MachineDowntimes.Add(MakeDowntime(T, now, wcCutting.Id, po3.Id,
                now.AddDays(-17).AddHours(13), now.AddDays(-17).AddHours(14),
                1, "Electrical", "Plasma cutter HF ignition failure — arc start intermittent",
                "Replaced HF ignition board; tested on scrap material before resuming",
                userId, "Resolved"));
            await _ctx.SaveChangesAsync();

            // ══════════════════════════════════════════════════════════════════
            // PO-0004: Height-Adjustable Standing Desk × 50 — IN PROGRESS
            // ══════════════════════════════════════════════════════════════════
            var po4 = MakePO(T, now, $"PO-{year}-0004", fgDesk,
                bomDesk.Id, routingDesk.Id, poPlannedDesk.Id,
                50, 30, 0, "InProgress",
                now.AddDays(-4), now.AddDays(10), now.AddDays(12),
                "Assembly phase in progress. Painting and QC pending. Gas lift fit-out begins tomorrow.");
            _ctx.ProductionOrders.Add(po4);
            await _ctx.SaveChangesAsync();

            var po4Ops = new[]
            {
                MakePOOp(T, now, po4.Id, routingDeskOps[0].Id, wcMachining.Id, 10, "Cut & Shape Wood Panels",   0.50m, 0.25m, 0.50m, 0.25m, "Completed"),
                MakePOOp(T, now, po4.Id, routingDeskOps[1].Id, wcCutting.Id,   20, "Cut Steel Frame Tubes",     0.40m, 0.20m, 0.40m, 0.20m, "Completed"),
                MakePOOp(T, now, po4.Id, routingDeskOps[2].Id, wcWelding.Id,   30, "Weld Steel Frame",           0.60m, 0,     0.60m, 0,     "Completed"),
                MakePOOp(T, now, po4.Id, routingDeskOps[3].Id, wcAssembly.Id,  40, "Assemble Desk & Gas Lift",  0.75m, 0.50m, 0.60m, 0.40m, "InProgress"),
                MakePOOp(T, now, po4.Id, routingDeskOps[4].Id, wcPainting.Id,  50, "Paint Frame & Lacquer Top", 0.50m, 0,     0,     0,     "Pending"),
                MakePOOp(T, now, po4.Id, routingDeskOps[5].Id, wcQC.Id,        60, "Load & Height Test",         0.25m, 0,     0,     0,     "Pending"),
                MakePOOp(T, now, po4.Id, routingDeskOps[6].Id, wcPackaging.Id, 70, "Pack & Secure for Shipping",0.30m, 0,     0,     0,     "Pending"),
            };
            _ctx.ProductionOrderOperations.AddRange(po4Ops);

            var po4Comps = new[]
            {
                MakePOComp(T, now, po4.Id, bomDeskItems[0].Id, rmSteelTube,  "PCS", 100,  98, 0, 2),
                MakePOComp(T, now, po4.Id, bomDeskItems[1].Id, rmWoodPanel,  "PCS", 200, 198, 0, 1),
                MakePOComp(T, now, po4.Id, bomDeskItems[2].Id, rmScrews,     "PKT", 100,  92, 0, 0),
                MakePOComp(T, now, po4.Id, bomDeskItems[3].Id, rmPaint,      "LTR",  25,   0, 0, 0),
                MakePOComp(T, now, po4.Id, bomDeskItems[4].Id, rmGasLift,    "PCS",  50,  30, 0, 0),
            };
            _ctx.ProductionOrderComponents.AddRange(po4Comps);
            await _ctx.SaveChangesAsync();

            _ctx.ProductionSchedules.AddRange(
                MakeSchedule(T, now, po4.Id, wcMachining.Id, po4Ops[0].Id, "Forward", now.AddDays(-4), now.AddDays(-3), 0.50m, "Completed"),
                MakeSchedule(T, now, po4.Id, wcCutting.Id,   po4Ops[1].Id, "Forward", now.AddDays(-3), now.AddDays(-2), 0.40m, "Completed"),
                MakeSchedule(T, now, po4.Id, wcWelding.Id,   po4Ops[2].Id, "Forward", now.AddDays(-2), now.AddDays(-1), 0.60m, "Completed"),
                MakeSchedule(T, now, po4.Id, wcAssembly.Id,  po4Ops[3].Id, "Forward", now.AddDays(-1), now.AddDays( 2), 0.75m, "InProgress"),
                MakeSchedule(T, now, po4.Id, wcPainting.Id,  po4Ops[4].Id, "Forward", now.AddDays( 3), now.AddDays( 5), 0.50m, "Scheduled"),
                MakeSchedule(T, now, po4.Id, wcQC.Id,        po4Ops[5].Id, "Forward", now.AddDays( 6), now.AddDays( 7), 0.25m, "Scheduled"),
                MakeSchedule(T, now, po4.Id, wcPackaging.Id, po4Ops[6].Id, "Forward", now.AddDays( 8), now.AddDays(10), 0.30m, "Scheduled"));
            await _ctx.SaveChangesAsync();

            _ctx.MaterialIssues.AddRange(
                MakeMatIssue(T, now, po4.Id, rmSteelTube,  98, now.AddDays(-4), userId),
                MakeMatIssue(T, now, po4.Id, rmWoodPanel, 198, now.AddDays(-3), userId),
                MakeMatIssue(T, now, po4.Id, rmScrews,     92, now.AddDays(-2), userId),
                MakeMatIssue(T, now, po4.Id, rmGasLift,    30, now.AddDays(-1), userId));

            _ctx.WorkInProgress.Add(MakeWIP(T, now, po4.Id, 30, 0, 0,
                "30 units assembled with gas lifts fitted. 20 units still pending assembly. Painting scheduled for Day +3."));
            await _ctx.SaveChangesAsync();

            // Sub-contract: welding frame subbed out to accelerate critical path
            _ctx.SubContractOrders.Add(MakeSubCon(T, now, po4.Id, po4Ops[2].Id,
                whRawMaterials, null,
                15, now.AddDays(-2), now.AddDays(-1), now.AddDays(-1),
                22, 330, 0, "Completed"));
            await _ctx.SaveChangesAsync();

            _ctx.MachineDowntimes.Add(MakeDowntime(T, now, wcAssembly.Id, po4.Id,
                now.AddDays(-1).AddHours(14), now.AddDays(-1).AddHours(16),
                2, "Tooling", "Torque wrench calibration failure — under-torquing gas lift collar clamp",
                "Recalibrated torque wrench to 22 Nm; retorqued 30 assembled units; spot-checked 10",
                userId, "Resolved"));
            await _ctx.SaveChangesAsync();

            // ══════════════════════════════════════════════════════════════════
            // PO-0005: 6-Seater Meeting Table × 20 — RELEASED
            // ══════════════════════════════════════════════════════════════════
            var po5 = MakePO(T, now, $"PO-{year}-0005", fgTable,
                bomTable.Id, routingTable.Id, poPlannedTable.Id,
                20, 0, 0, "Released",
                now.AddDays(1), now.AddDays(18), now.AddDays(20),
                "Released. Materials staged in raw materials warehouse. Machining starts tomorrow.");
            _ctx.ProductionOrders.Add(po5);
            await _ctx.SaveChangesAsync();

            var po5Ops = new[]
            {
                MakePOOp(T, now, po5.Id, routingTableOps[0].Id, wcMachining.Id, 10, "Machine Wood Top & MDF Trim", 1.00m, 0.75m, 0, 0, "Pending"),
                MakePOOp(T, now, po5.Id, routingTableOps[1].Id, wcCutting.Id,   20, "Cut Steel Leg Tubes",         0.50m, 0.30m, 0, 0, "Pending"),
                MakePOOp(T, now, po5.Id, routingTableOps[2].Id, wcWelding.Id,   30, "Weld Table Base Frame",        1.00m, 0,     0, 0, "Pending"),
                MakePOOp(T, now, po5.Id, routingTableOps[3].Id, wcPainting.Id,  40, "Powder Coat Steel Base",       0.75m, 0,     0, 0, "Pending"),
                MakePOOp(T, now, po5.Id, routingTableOps[4].Id, wcAssembly.Id,  50, "Assemble Top to Base",         1.00m, 0.50m, 0, 0, "Pending"),
                MakePOOp(T, now, po5.Id, routingTableOps[5].Id, wcAssembly.Id,  60, "Sand, Polish & Cable Grommets",0.50m, 0.25m, 0, 0, "Pending"),
                MakePOOp(T, now, po5.Id, routingTableOps[6].Id, wcQC.Id,        70, "Final Quality Inspection",     0.50m, 0,     0, 0, "Pending"),
                MakePOOp(T, now, po5.Id, routingTableOps[7].Id, wcPackaging.Id, 80, "Pack & Pallet for Delivery",   0.75m, 0,     0, 0, "Pending"),
            };
            _ctx.ProductionOrderOperations.AddRange(po5Ops);

            _ctx.ProductionOrderComponents.AddRange(
                MakePOComp(T, now, po5.Id, bomTableItems[0].Id, rmSteelTube,  "PCS", 120, 0, 0, 1),
                MakePOComp(T, now, po5.Id, bomTableItems[1].Id, rmWoodPanel,  "PCS", 160, 0, 0, 1),
                MakePOComp(T, now, po5.Id, bomTableItems[2].Id, rmScrews,     "PKT",  60, 0, 0, 0),
                MakePOComp(T, now, po5.Id, bomTableItems[3].Id, rmPaint,      "LTR",  20, 0, 0, 0),
                MakePOComp(T, now, po5.Id, bomTableItems[4].Id, rmMdfBoard,   "PCS",  40, 0, 0, 1));

            _ctx.ProductionSchedules.AddRange(
                MakeSchedule(T, now, po5.Id, wcMachining.Id, po5Ops[0].Id, "Forward", now.AddDays( 1), now.AddDays( 3), 1.00m, "Scheduled"),
                MakeSchedule(T, now, po5.Id, wcCutting.Id,   po5Ops[1].Id, "Forward", now.AddDays( 3), now.AddDays( 4), 0.50m, "Scheduled"),
                MakeSchedule(T, now, po5.Id, wcWelding.Id,   po5Ops[2].Id, "Forward", now.AddDays( 4), now.AddDays( 7), 1.00m, "Scheduled"),
                MakeSchedule(T, now, po5.Id, wcPainting.Id,  po5Ops[3].Id, "Forward", now.AddDays( 7), now.AddDays(10), 0.75m, "Scheduled"),
                MakeSchedule(T, now, po5.Id, wcAssembly.Id,  po5Ops[4].Id, "Forward", now.AddDays(10), now.AddDays(14), 1.00m, "Scheduled"),
                MakeSchedule(T, now, po5.Id, wcAssembly.Id,  po5Ops[5].Id, "Forward", now.AddDays(14), now.AddDays(15), 0.50m, "Scheduled"),
                MakeSchedule(T, now, po5.Id, wcQC.Id,        po5Ops[6].Id, "Forward", now.AddDays(15), now.AddDays(17), 0.50m, "Scheduled"),
                MakeSchedule(T, now, po5.Id, wcPackaging.Id, po5Ops[7].Id, "Forward", now.AddDays(17), now.AddDays(18), 0.75m, "Scheduled"));
            await _ctx.SaveChangesAsync();

            // ══════════════════════════════════════════════════════════════════
            // PO-0006: Executive Ergonomic Chair × 150 — PLANNED
            // ══════════════════════════════════════════════════════════════════
            var po6 = MakePO(T, now, $"PO-{year}-0006", fgChair,
                bomChair.Id, routingChair.Id, poPlannedChair2.Id,
                150, 0, 0, "Planned",
                now.AddDays(8), now.AddDays(28), now.AddDays(30),
                "Second batch for Q3 demand. Gas lift cylinders on order from supplier, ETA Day +6. Fabric already in stock.");
            _ctx.ProductionOrders.Add(po6);
            await _ctx.SaveChangesAsync();

            var po6Ops = new[]
            {
                MakePOOp(T, now, po6.Id, routingChairOps[0].Id, wcCutting.Id,  10, "Cut & Shape Steel Tubes",      0.50m, 0.25m, 0, 0, "Pending"),
                MakePOOp(T, now, po6.Id, routingChairOps[1].Id, wcWelding.Id,  20, "Weld Chair Frame",              0.50m, 0,     0, 0, "Pending"),
                MakePOOp(T, now, po6.Id, routingChairOps[2].Id, wcPainting.Id, 30, "Powder Coat Frame",             0.25m, 0,     0, 0, "Pending"),
                MakePOOp(T, now, po6.Id, routingChairOps[3].Id, wcAssembly.Id, 40, "Assemble Seat, Back & Casters", 0.75m, 0.50m, 0, 0, "Pending"),
                MakePOOp(T, now, po6.Id, routingChairOps[4].Id, wcQC.Id,       50, "Final Quality Inspection",      0.25m, 0,     0, 0, "Pending"),
                MakePOOp(T, now, po6.Id, routingChairOps[5].Id, wcPackaging.Id,60, "Pack & Label",                  0.25m, 0,     0, 0, "Pending"),
            };
            _ctx.ProductionOrderOperations.AddRange(po6Ops);

            _ctx.ProductionOrderComponents.AddRange(
                MakePOComp(T, now, po6.Id, bomChairItems[0].Id, rmSteelTube,    "PCS",  600, 0, 0, 2),
                MakePOComp(T, now, po6.Id, bomChairItems[1].Id, rmFabric,       "MTR",  225, 0, 0, 1),
                MakePOComp(T, now, po6.Id, bomChairItems[2].Id, rmFoam,         "KG",   120, 0, 0, 0),
                MakePOComp(T, now, po6.Id, bomChairItems[3].Id, rmScrews,       "PKT",  150, 0, 0, 0),
                MakePOComp(T, now, po6.Id, bomChairItems[4].Id, rmPaint,        "LTR",   45, 0, 0, 0),
                MakePOComp(T, now, po6.Id, bomChairItems[5].Id, rmGasLift,      "PCS",  150, 0, 0, 0),
                MakePOComp(T, now, po6.Id, bomChairItems[6].Id, rmCasterWheels, "PCS",  150, 0, 0, 0));

            _ctx.ProductionSchedules.AddRange(
                MakeSchedule(T, now, po6.Id, wcCutting.Id,  po6Ops[0].Id, "Forward", now.AddDays( 8), now.AddDays(10), 0.50m, "Scheduled"),
                MakeSchedule(T, now, po6.Id, wcWelding.Id,  po6Ops[1].Id, "Forward", now.AddDays(10), now.AddDays(13), 0.50m, "Scheduled"),
                MakeSchedule(T, now, po6.Id, wcPainting.Id, po6Ops[2].Id, "Forward", now.AddDays(13), now.AddDays(15), 0.25m, "Scheduled"),
                MakeSchedule(T, now, po6.Id, wcAssembly.Id, po6Ops[3].Id, "Forward", now.AddDays(15), now.AddDays(22), 0.75m, "Scheduled"),
                MakeSchedule(T, now, po6.Id, wcQC.Id,       po6Ops[4].Id, "Forward", now.AddDays(22), now.AddDays(25), 0.25m, "Scheduled"),
                MakeSchedule(T, now, po6.Id, wcPackaging.Id,po6Ops[5].Id, "Forward", now.AddDays(25), now.AddDays(28), 0.25m, "Scheduled"));
            await _ctx.SaveChangesAsync();

            // ── Capacity Loads (historical + current + forward) ────────────────
            _ctx.CapacityLoads.AddRange(
                // PO-1 historical
                MakeCapLoad(T, now, wcCutting.Id,  shiftCutDay.Id,  now.AddDays(-30), 4.0m, 8,   50),
                MakeCapLoad(T, now, wcWelding.Id,  shiftWldDay.Id,  now.AddDays(-28), 5.5m, 6,   92),
                MakeCapLoad(T, now, wcPainting.Id, shiftPntDay.Id,  now.AddDays(-27), 3.8m, 4,   95),
                MakeCapLoad(T, now, wcAssembly.Id, shiftAsmMorn.Id, now.AddDays(-25), 7.5m, 8,   94),
                MakeCapLoad(T, now, wcQC.Id,       shiftQcFull.Id,  now.AddDays(-24), 7.0m, 8,   88),
                // PO-2 historical
                MakeCapLoad(T, now, wcMachining.Id,shiftMchDay.Id,  now.AddDays(-22), 5.8m, 6,   97),
                MakeCapLoad(T, now, wcWelding.Id,  shiftWldEve.Id,  now.AddDays(-20), 6.0m, 6,  100),
                MakeCapLoad(T, now, wcAssembly.Id, shiftAsmEve.Id,  now.AddDays(-17), 8.0m, 8,  100),
                // PO-3 historical
                MakeCapLoad(T, now, wcCutting.Id,  shiftCutDay.Id,  now.AddDays(-18), 5.5m, 8,   69),
                MakeCapLoad(T, now, wcWelding.Id,  shiftWldDay.Id,  now.AddDays(-16), 4.5m, 6,   75),
                // PO-4 current
                MakeCapLoad(T, now, wcAssembly.Id, shiftAsmMorn.Id, now,              8.0m, 8,  100),
                MakeCapLoad(T, now, wcAssembly.Id, shiftAsmNight.Id,now,              5.0m, 6,   83));
            await _ctx.SaveChangesAsync();

            // ── Inventory Transactions ─────────────────────────────────────────
            _ctx.InventoryTransactions.AddRange(
                // PO-1 goods issues
                MakeInvTxn(T, now, rmSteelTube,    396, "GoodsIssue",   po1.Id, "ProductionOrder", now.AddDays(-30)),
                MakeInvTxn(T, now, rmFabric,       150, "GoodsIssue",   po1.Id, "ProductionOrder", now.AddDays(-29)),
                MakeInvTxn(T, now, rmFoam,          79, "GoodsIssue",   po1.Id, "ProductionOrder", now.AddDays(-29)),
                MakeInvTxn(T, now, rmGasLift,       97, "GoodsIssue",   po1.Id, "ProductionOrder", now.AddDays(-26)),
                MakeInvTxn(T, now, rmSteelTube,     25, "ScrapReceipt", po1.Id, "ProductionOrder", now.AddDays(-23)),
                MakeInvTxn(T, now, fgChair,         97, "GoodsReceipt", po1.Id, "ProductionOrder", now.AddDays(-22)),
                // PO-2 goods issues
                MakeInvTxn(T, now, rmSteelSheet,   476, "GoodsIssue",   po2.Id, "ProductionOrder", now.AddDays(-22)),
                MakeInvTxn(T, now, rmMdfBoard,     178, "GoodsIssue",   po2.Id, "ProductionOrder", now.AddDays(-21)),
                MakeInvTxn(T, now, rmHandles,      240, "GoodsIssue",   po2.Id, "ProductionOrder", now.AddDays(-20)),
                MakeInvTxn(T, now, rmSteelSheet,    30, "ScrapReceipt", po2.Id, "ProductionOrder", now.AddDays(-15)),
                MakeInvTxn(T, now, fgCabinet,       58, "GoodsReceipt", po2.Id, "ProductionOrder", now.AddDays(-14)),
                // PO-3 goods issues
                MakeInvTxn(T, now, rmSteelSheet,   318, "GoodsIssue",   po3.Id, "ProductionOrder", now.AddDays(-18)),
                MakeInvTxn(T, now, rmParticleBoard, 399,"GoodsIssue",   po3.Id, "ProductionOrder", now.AddDays(-17)),
                MakeInvTxn(T, now, rmSteelSheet,    24, "ScrapReceipt", po3.Id, "ProductionOrder", now.AddDays(-10)),
                MakeInvTxn(T, now, fgBookshelf,     79, "GoodsReceipt", po3.Id, "ProductionOrder", now.AddDays( -9)),
                // PO-4 goods issues (partial)
                MakeInvTxn(T, now, rmSteelTube,     98, "GoodsIssue",   po4.Id, "ProductionOrder", now.AddDays(-4)),
                MakeInvTxn(T, now, rmWoodPanel,    198, "GoodsIssue",   po4.Id, "ProductionOrder", now.AddDays(-3)),
                MakeInvTxn(T, now, rmGasLift,       30, "GoodsIssue",   po4.Id, "ProductionOrder", now.AddDays(-1)));
            await _ctx.SaveChangesAsync();

            // ══════════════════════════════════════════════════════════════════
            // FAST-FOOD (Pizza shop): recipes (BOMs), kitchen routings &
            // completed production runs that consume ingredients → produce menu.
            // ══════════════════════════════════════════════════════════════════
            var wcDough = wcs.First(w => w.Code == "WC-DOUGH");
            var wcOven  = wcs.First(w => w.Code == "WC-OVEN");
            var wcGrill = wcs.First(w => w.Code == "WC-GRILL");
            var wcFryer = wcs.First(w => w.Code == "WC-FRYER");

            // Food item GUIDs (same formula as Inventory)
            var rmDough     = RmDough(companyId);
            var rmMozz      = RmMozz(companyId);
            var rmSauce     = RmPizzaSauce(companyId);
            var rmPepperoni = RmPepperoni(companyId);
            var rmTikka     = RmChkTikka(companyId);
            var rmVeg       = RmVegMix(companyId);
            var rmBeef      = RmBeefPatty(companyId);
            var rmFillet    = RmChkFillet(companyId);
            var rmBun       = RmBun(companyId);
            var rmCheddar   = RmCheddar(companyId);
            var rmLettuce   = RmLettuce(companyId);
            var rmTomato    = RmTomato(companyId);
            var rmMayo      = RmMayo(companyId);
            var rmPotato    = RmPotato(companyId);
            var rmFryOil    = RmFryOil(companyId);
            var rmPizzaBox  = RmPizzaBox(companyId);
            var rmBurgWrap  = RmBurgerWrap(companyId);
            var rmFriesCup  = RmFriesCup(companyId);

            // Local helper: create a BOM + its items, return both.
            async Task<(BillOfMaterial bom, BOMItem[] items)> AddFoodBom(
                Guid fgId, (Guid mat, string uom, decimal qty, decimal scrap)[] comps)
            {
                var bom = MakeBOM(T, now, fgId, 1);
                _ctx.BillsOfMaterial.Add(bom);
                await _ctx.SaveChangesAsync();
                var its = comps.Select(x => MakeBOMItem(T, now, bom.Id, x.mat, x.uom, x.qty, x.scrap)).ToArray();
                _ctx.BOMItems.AddRange(its);
                await _ctx.SaveChangesAsync();
                return (bom, its);
            }

            // ── Pizza recipes (dough + mozzarella + sauce + topping + box) ──────
            await AddFoodBom(FgPizzaMargS(companyId), new[] { (rmDough,"PCS",1m,1m), (rmMozz,"KG",0.10m,2m), (rmSauce,"L",0.06m,1m), (rmVeg,"KG",0.03m,2m), (rmPizzaBox,"PCS",1m,0m) });
            await AddFoodBom(FgPizzaMargM(companyId), new[] { (rmDough,"PCS",1m,1m), (rmMozz,"KG",0.15m,2m), (rmSauce,"L",0.09m,1m), (rmVeg,"KG",0.05m,2m), (rmPizzaBox,"PCS",1m,0m) });
            await AddFoodBom(FgPizzaMargL(companyId), new[] { (rmDough,"PCS",2m,1m), (rmMozz,"KG",0.22m,2m), (rmSauce,"L",0.13m,1m), (rmVeg,"KG",0.08m,2m), (rmPizzaBox,"PCS",1m,0m) });
            await AddFoodBom(FgPizzaPepS(companyId),  new[] { (rmDough,"PCS",1m,1m), (rmMozz,"KG",0.10m,2m), (rmSauce,"L",0.06m,1m), (rmPepperoni,"KG",0.06m,2m), (rmPizzaBox,"PCS",1m,0m) });
            var pepM = await AddFoodBom(FgPizzaPepM(companyId), new[] { (rmDough,"PCS",1m,1m), (rmMozz,"KG",0.15m,2m), (rmSauce,"L",0.09m,1m), (rmPepperoni,"KG",0.09m,2m), (rmPizzaBox,"PCS",1m,0m) });
            await AddFoodBom(FgPizzaPepL(companyId),  new[] { (rmDough,"PCS",2m,1m), (rmMozz,"KG",0.22m,2m), (rmSauce,"L",0.13m,1m), (rmPepperoni,"KG",0.14m,2m), (rmPizzaBox,"PCS",1m,0m) });
            await AddFoodBom(FgPizzaTikkaS(companyId),new[] { (rmDough,"PCS",1m,1m), (rmMozz,"KG",0.10m,2m), (rmSauce,"L",0.06m,1m), (rmTikka,"KG",0.07m,2m), (rmPizzaBox,"PCS",1m,0m) });
            await AddFoodBom(FgPizzaTikkaM(companyId),new[] { (rmDough,"PCS",1m,1m), (rmMozz,"KG",0.15m,2m), (rmSauce,"L",0.09m,1m), (rmTikka,"KG",0.10m,2m), (rmPizzaBox,"PCS",1m,0m) });
            await AddFoodBom(FgPizzaTikkaL(companyId),new[] { (rmDough,"PCS",2m,1m), (rmMozz,"KG",0.22m,2m), (rmSauce,"L",0.13m,1m), (rmTikka,"KG",0.15m,2m), (rmPizzaBox,"PCS",1m,0m) });

            // ── Burger recipes ─────────────────────────────────────────────────
            var beefBom = await AddFoodBom(FgBurgerBeef(companyId), new[] { (rmBeef,"PCS",1m,1m), (rmBun,"PCS",1m,1m), (rmCheddar,"PCS",1m,1m), (rmLettuce,"KG",0.02m,3m), (rmTomato,"KG",0.02m,3m), (rmMayo,"L",0.02m,1m), (rmBurgWrap,"PCS",1m,0m) });
            await AddFoodBom(FgBurgerZinger(companyId), new[] { (rmFillet,"PCS",1m,1m), (rmBun,"PCS",1m,1m), (rmLettuce,"KG",0.02m,3m), (rmMayo,"L",0.03m,1m), (rmBurgWrap,"PCS",1m,0m) });
            await AddFoodBom(FgBurgerCheese(companyId), new[] { (rmBeef,"PCS",1m,1m), (rmBun,"PCS",1m,1m), (rmCheddar,"PCS",2m,1m), (rmMayo,"L",0.02m,1m), (rmBurgWrap,"PCS",1m,0m) });

            // ── Fries recipes ──────────────────────────────────────────────────
            var friesBom = await AddFoodBom(FgFriesReg(companyId), new[] { (rmPotato,"KG",0.20m,3m), (rmFryOil,"L",0.05m,2m), (rmFriesCup,"PCS",1m,0m) });
            await AddFoodBom(FgFriesLrg(companyId), new[] { (rmPotato,"KG",0.32m,3m), (rmFryOil,"L",0.07m,2m), (rmFriesCup,"PCS",1m,0m) });

            // ── Kitchen routings (for the production runs below) ────────────────
            var rtgPizza = MakeRouting(T, now, FgPizzaPepM(companyId), "Pizza Kitchen Routing", 1);
            _ctx.Routings.Add(rtgPizza);
            await _ctx.SaveChangesAsync();
            var rtgPizzaOps = new[]
            {
                MakeRoutingOp(T, now, rtgPizza.Id, wcDough.Id,     10, "Prep & Stretch Dough Base", 0.05m, 0.05m, 0,     1),
                MakeRoutingOp(T, now, rtgPizza.Id, wcOven.Id,      20, "Top & Bake Pizza",          0.20m, 0.02m, 0.20m, 2),
                MakeRoutingOp(T, now, rtgPizza.Id, wcQC.Id,        30, "Quality Check",             0.03m, 0.03m, 0,     3),
                MakeRoutingOp(T, now, rtgPizza.Id, wcPackaging.Id, 40, "Box & Label",               0.03m, 0.03m, 0,     4),
            };
            _ctx.RoutingOperations.AddRange(rtgPizzaOps);
            await _ctx.SaveChangesAsync();

            var rtgBurger = MakeRouting(T, now, FgBurgerBeef(companyId), "Burger Kitchen Routing", 1);
            _ctx.Routings.Add(rtgBurger);
            await _ctx.SaveChangesAsync();
            var rtgBurgerOps = new[]
            {
                MakeRoutingOp(T, now, rtgBurger.Id, wcGrill.Id,     10, "Grill Patty & Assemble", 0.08m, 0.05m, 0.05m, 1),
                MakeRoutingOp(T, now, rtgBurger.Id, wcPackaging.Id, 20, "Wrap & Bag",             0.03m, 0.03m, 0,     2),
            };
            _ctx.RoutingOperations.AddRange(rtgBurgerOps);
            await _ctx.SaveChangesAsync();

            var rtgFries = MakeRouting(T, now, FgFriesReg(companyId), "Fryer Routing", 1);
            _ctx.Routings.Add(rtgFries);
            await _ctx.SaveChangesAsync();
            var rtgFriesOps = new[]
            {
                MakeRoutingOp(T, now, rtgFries.Id, wcFryer.Id,     10, "Fry Potatoes",  0.06m, 0.02m, 0.06m, 1),
                MakeRoutingOp(T, now, rtgFries.Id, wcPackaging.Id, 20, "Salt & Cup",    0.02m, 0.02m, 0,     2),
            };
            _ctx.RoutingOperations.AddRange(rtgFriesOps);
            await _ctx.SaveChangesAsync();

            // ── Demands & Planned Orders ────────────────────────────────────────
            var demPizza  = MakeDemand(T, now, FgPizzaPepM(companyId), 60,  now.AddDays(-3), "SalesForecast", "Fulfilled");
            var demBurger = MakeDemand(T, now, FgBurgerBeef(companyId), 100, now.AddDays(-2), "SalesForecast", "Fulfilled");
            var demFries  = MakeDemand(T, now, FgFriesReg(companyId),  150, now.AddDays(-2), "SalesForecast", "Fulfilled");
            _ctx.Demands.AddRange(demPizza, demBurger, demFries);
            await _ctx.SaveChangesAsync();

            var ppPizza  = MakePlannedOrder(T, now, FgPizzaPepM(companyId), 60,  now.AddDays(-3), now.AddDays(-3), "Converted");
            var ppBurger = MakePlannedOrder(T, now, FgBurgerBeef(companyId), 100, now.AddDays(-2), now.AddDays(-2), "Converted");
            var ppFries  = MakePlannedOrder(T, now, FgFriesReg(companyId),  150, now.AddDays(-2), now.AddDays(-2), "Converted");
            _ctx.PlannedOrders.AddRange(ppPizza, ppBurger, ppFries);
            await _ctx.SaveChangesAsync();

            // Local helper: a completed kitchen production run with the full chain.
            async Task AddFoodRun(
                string orderNo, Guid fgId, BillOfMaterial bom, BOMItem[] bomItems, Guid routingId,
                Guid plannedId, int qty, int produced, int rejected, string batchNo,
                (Guid mat, string uom, decimal qty)[] consume,
                (decimal mat, decimal labor, decimal machine, decimal overhead) cost)
            {
                var po = MakePO(T, now, orderNo, fgId, bom.Id, routingId, plannedId,
                    qty, produced, rejected, "Completed",
                    now.AddHours(-6), now.AddHours(-1), now.AddHours(-1),
                    $"Kitchen production run — {produced} produced, {rejected} rejected.");
                _ctx.ProductionOrders.Add(po);
                await _ctx.SaveChangesAsync();

                // Components (planned == issued for a completed run)
                var comps = new List<ProductionOrderComponent>();
                for (int i = 0; i < consume.Length && i < bomItems.Length; i++)
                    comps.Add(MakePOComp(T, now, po.Id, bomItems[i].Id, consume[i].mat,
                        consume[i].uom, consume[i].qty, consume[i].qty, 0, 0));
                _ctx.ProductionOrderComponents.AddRange(comps);

                // Material issues + inventory goods-issue per ingredient
                foreach (var c in consume)
                {
                    _ctx.MaterialIssues.Add(MakeMatIssue(T, now, po.Id, c.mat, c.qty, now.AddHours(-6), userId));
                    _ctx.InventoryTransactions.Add(MakeInvTxn(T, now, c.mat, c.qty, "GoodsIssue", po.Id, "ProductionOrder", now.AddHours(-6)));
                }
                _ctx.WorkInProgress.Add(MakeWIP(T, now, po.Id, 0, produced, rejected,
                    $"{produced} units transferred to finished goods."));
                await _ctx.SaveChangesAsync();

                // Finished-goods receipt + batch + finished goods inventory receipt
                _ctx.FinishedGoodsReceipts.Add(MakeFGR(T, now, po.Id, fgId, produced, whFinishedGoods, batchNo, now.AddHours(-1)));
                _ctx.ProductionBatches.Add(MakeBatch(T, now, po.Id, null, fgId,
                    batchNo, now.AddHours(-1), now.AddDays(3), null,
                    produced, "PCS", whFinishedGoods, true, null));
                _ctx.InventoryTransactions.Add(MakeInvTxn(T, now, fgId, produced, "GoodsReceipt", po.Id, "ProductionOrder", now.AddHours(-1)));

                var ce = MakeCostEntry(T, now, po.Id, cost.mat, cost.labor, cost.machine, cost.overhead, 0, now.AddHours(-1));
                _ctx.CostEntries.Add(ce);
                await _ctx.SaveChangesAsync();
            }

            await AddFoodRun($"PO-{year}-0007", FgPizzaPepM(companyId), pepM.bom, pepM.items, rtgPizza.Id, ppPizza.Id,
                60, 58, 2, $"BTH-{year}-PIZ-001",
                new[] { (rmDough,"PCS",60m), (rmMozz,"KG",9m), (rmSauce,"L",5.4m), (rmPepperoni,"KG",5.4m), (rmPizzaBox,"PCS",60m) },
                (18000m, 4000m, 2500m, 1500m));

            await AddFoodRun($"PO-{year}-0008", FgBurgerBeef(companyId), beefBom.bom, beefBom.items, rtgBurger.Id, ppBurger.Id,
                100, 100, 0, $"BTH-{year}-BRG-001",
                new[] { (rmBeef,"PCS",100m), (rmBun,"PCS",100m), (rmCheddar,"PCS",100m), (rmLettuce,"KG",2m), (rmTomato,"KG",2m), (rmMayo,"L",2m), (rmBurgWrap,"PCS",100m) },
                (14000m, 3000m, 1500m, 1200m));

            await AddFoodRun($"PO-{year}-0009", FgFriesReg(companyId), friesBom.bom, friesBom.items, rtgFries.Id, ppFries.Id,
                150, 150, 0, $"BTH-{year}-FRY-001",
                new[] { (rmPotato,"KG",30m), (rmFryOil,"L",7.5m), (rmFriesCup,"PCS",150m) },
                (6000m, 1500m, 1000m, 600m));

            await tx.CommitAsync();

            _logger.LogInformation(
                "Manufacturing seed data created for Company {CompanyId}. " +
                "5 furniture products + 14 fast-food menu recipes (BOMs), " +
                "8 Routings, 9 PlannedOrders, 9 ProductionOrders " +
                "(6 Completed, 1 InProgress, 1 Released, 1 Planned).",
                companyId);

            return true;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "Failed to seed manufacturing data for Company {CompanyId}", companyId);
            throw;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task DeleteWhere<T>(DbSet<T> set, Guid companyId)
        where T : Nexcore.SharedKernel.BaseEntity
    {
        var rows = await set.Where(e => e.CompanyId == companyId).ToListAsync();
        if (rows.Count > 0)
        {
            set.RemoveRange(rows);
            await _ctx.SaveChangesAsync();
        }
    }

    private static WorkCenterShift MakeShift(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        Guid workCenterId, string name, string workingDays,
        decimal availableHours, decimal utilPct) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        WorkCenterId = workCenterId, ShiftName = name,
        WorkingDays = workingDays, AvailableHours = availableHours,
        CapacityUtilizationPercent = utilPct, IsActive = true
    };

    private static StandardCost MakeStdCost(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        Guid productId, int version, string currency,
        decimal total, decimal mat, decimal labor, decimal machine) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        ProductId = productId, Version = version, CurrencyCode = currency,
        MaterialCost = mat, LaborCost = labor, MachineCost = machine,
        OverheadCost = total - mat - labor - machine,
        TotalCost = total,
        IsActive = true, EffectiveFrom = now
    };

    private static MaterialPlanningData MakeMPD(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        Guid productId, decimal safety, decimal reorder, decimal max,
        decimal lot, int leadDays, string procType, string mrpType) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        ProductId = productId, SafetyStock = safety, ReorderPoint = reorder,
        MaximumStockLevel = max, LotSize = lot, LeadTimeDays = leadDays,
        PlanningHorizonDays = 90, ScrapPercentage = 2,
        ProcurementType = procType, MRPType = mrpType, IsActive = true
    };

    private static BillOfMaterial MakeBOM(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        Guid finishedProductId, int version) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        FinishedProductId = finishedProductId, Version = version,
        IsActive = true, EffectiveFrom = now
    };

    private static BOMItem MakeBOMItem(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        Guid bomId, Guid materialId, string uom, decimal qty, decimal scrapPct) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        BillOfMaterialId = bomId, MaterialId = materialId,
        UnitOfMeasure = uom, QuantityRequired = qty, ScrapPercentage = scrapPct
    };

    private static BOMByProduct MakeBOMByProduct(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        Guid bomId, Guid productId, string type, string uom, decimal qty, decimal costAllocPct) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        BillOfMaterialId = bomId, ProductId = productId,
        Type = type, UnitOfMeasure = uom, Quantity = qty,
        CostAllocationPercent = costAllocPct
    };

    private static Routing MakeRouting(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        Guid productId, string name, int version) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        ProductId = productId, Name = name, Version = version, IsActive = true
    };

    private static RoutingOperation MakeRoutingOp(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        Guid routingId, Guid workCenterId, int opNo, string name,
        decimal stdHrs, decimal laborHrs, decimal machHrs, int seq) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        RoutingId = routingId, WorkCenterId = workCenterId,
        OperationName = name,
        StandardHours = stdHrs, LaborHours = laborHrs, MachineHours = machHrs,
        SequenceNo = seq
    };

    private static Demand MakeDemand(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        Guid productId, decimal qty, DateTime dueDate, string sourceType, string status) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        ProductId = productId, Quantity = qty, FulfilledQty = 0,
        DueDate = dueDate, SourceType = sourceType, Status = status
    };

    private static PlannedOrder MakePlannedOrder(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        Guid productId, decimal qty, DateTime plannedStart, DateTime plannedEnd, string status) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        ProductId = productId, PlannedQty = qty,
        SourceType = "MRP", Status = status,
        RequiredDate = plannedEnd
    };

    private static ProductionOrder MakePO(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        string orderNumber, Guid productId, Guid bomId, Guid routingId, Guid? plannedOrderId,
        decimal qtyPlanned, decimal qtyProduced, decimal qtyRejected, string status,
        DateTime? start, DateTime? end, DateTime? dueDate, string? notes) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        OrderNumber = orderNumber, ProductId = productId,
        BillOfMaterialId = bomId, RoutingId = routingId, PlannedOrderId = plannedOrderId,
        QuantityPlanned = qtyPlanned, QuantityProduced = qtyProduced, QuantityRejected = qtyRejected,
        Status = status, StartDate = start, EndDate = end, DueDate = dueDate,
        CreatedById = T.u, Notes = notes
    };

    private static ProductionOrderOperation MakePOOp(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        Guid poId, Guid routingOpId, Guid workCenterId,
        int seqNo, string name,
        decimal planLabor, decimal planMachine, decimal actLabor, decimal actMachine,
        string status) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        ProductionOrderId = poId, RoutingOperationId = routingOpId,
        WorkCenterId = workCenterId, SequenceNo = seqNo, OperationName = name,
        PlannedLaborHours = planLabor, PlannedMachineHours = planMachine,
        ActualLaborHours = actLabor, ActualMachineHours = actMachine,
        Status = status
    };

    private static ProductionOrderComponent MakePOComp(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        Guid poId, Guid bomItemId, Guid materialId, string uom,
        decimal plannedQty, decimal issuedQty, decimal returnedQty, decimal scrapPct) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        ProductionOrderId = poId, BOMItemId = bomItemId, MaterialId = materialId,
        UnitOfMeasure = uom, PlannedQty = plannedQty,
        IssuedQty = issuedQty, ReturnedQty = returnedQty, ScrapPercentage = scrapPct
    };

    private static ProductionSchedule MakeSchedule(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        Guid poId, Guid workCenterId, Guid poOpId, string schedType,
        DateTime start, DateTime end, decimal capHours, string status) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        ProductionOrderId = poId, WorkCenterId = workCenterId,
        ProductionOrderOperationId = poOpId, ScheduleType = schedType,
        ScheduledStartDate = start, ScheduledEndDate = end,
        CapacityRequiredHours = capHours, Status = status
    };

    private static MaterialIssue MakeMatIssue(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        Guid poId, Guid materialId, decimal qty, DateTime issuedAt, Guid issuedById) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        ProductionOrderId = poId, MaterialId = materialId,
        QuantityIssued = qty, QuantityReturned = 0,
        IssuedAt = issuedAt, IssuedById = issuedById
    };

    private static WorkInProgress MakeWIP(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        Guid poId, decimal inProgress, decimal completed, decimal rejected, string? notes) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        ProductionOrderId = poId, QuantityInProgress = inProgress,
        QuantityCompleted = completed, QuantityRejected = rejected,
        LastUpdatedAt = now, Notes = notes
    };

    private static Inspection MakeInspection(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        Guid poId, decimal inspected, decimal passed, decimal rejected,
        Guid inspectedById, DateTime inspectedAt, string status, string? remarks) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        ProductionOrderId = poId, InspectedQty = inspected,
        PassedQty = passed, RejectedQty = rejected,
        InspectedById = inspectedById, InspectedAt = inspectedAt,
        Status = status, Remarks = remarks
    };

    private static InspectionCharacteristic MakeInspChar(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        Guid inspectionId, string name, string type, string? uom,
        decimal? target, decimal? lower, decimal? upper, decimal? actual,
        string? qualResult, string result, bool isCritical) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        InspectionId = inspectionId, CharacteristicName = name,
        InspectionType = type, UnitOfMeasure = uom,
        TargetValue = target, LowerTolerance = lower, UpperTolerance = upper,
        ActualValue = actual, QualitativeResult = qualResult,
        Result = result, IsCritical = isCritical, SampleSize = 5
    };

    private static FinishedGoodsReceipt MakeFGR(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        Guid poId, Guid productId, decimal qty, Guid warehouseId,
        string batchNo, DateTime receivedAt) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        ProductionOrderId = poId, ProductId = productId,
        QuantityReceived = qty, WarehouseId = warehouseId,
        BatchNo = batchNo, ReceivedAt = receivedAt
    };

    private static ProductionBatch MakeBatch(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        Guid poId, Guid? inspectionId, Guid productId,
        string batchNumber, DateTime mfgDate, DateTime? expiryDate, DateTime? reTestDate,
        decimal qty, string uom, Guid warehouseId, bool qualityApproved, string? coa) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        ProductionOrderId = poId, InspectionId = inspectionId, ProductId = productId,
        BatchNumber = batchNumber, ManufacturingDate = mfgDate,
        ExpiryDate = expiryDate, ReTestDate = reTestDate,
        Quantity = qty, UnitOfMeasure = uom, Status = "Active",
        WarehouseId = warehouseId, QualityApproved = qualityApproved,
        CertificateOfAnalysis = coa
    };

    private static CostEntry MakeCostEntry(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        Guid poId, decimal mat, decimal labor, decimal machine, decimal overhead, decimal scrap,
        DateTime postedAt) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        ProductionOrderId = poId, MaterialCost = mat, LaborCost = labor,
        MachineCost = machine, OverheadCost = overhead, ScrapCost = scrap,
        TotalCost = mat + labor + machine + overhead + scrap, PostedAt = postedAt
    };

    private static ProductionVariance MakeVariance(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        Guid poId, Guid costEntryId,
        decimal stdMat, decimal actMat,
        decimal stdLabor, decimal actLabor,
        decimal stdMachine, decimal actMachine,
        decimal stdOH, decimal actOH,
        string category, string? notes) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        ProductionOrderId = poId, CostEntryId = costEntryId,
        StandardMaterialCost = stdMat, ActualMaterialCost = actMat,
        StandardLaborCost = stdLabor, ActualLaborCost = actLabor,
        StandardMachineCost = stdMachine, ActualMachineCost = actMachine,
        StandardOverheadCost = stdOH, ActualOverheadCost = actOH,
        VarianceCategory = category, IsSettled = true, Notes = notes
    };

    private static MachineDowntime MakeDowntime(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        Guid workCenterId, Guid poId,
        DateTime start, DateTime end, decimal durationHours,
        string category, string reason, string resolution,
        Guid resolvedById, string status) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        WorkCenterId = workCenterId, ProductionOrderId = poId,
        StartTime = start, EndTime = end, DurationHours = durationHours,
        Category = category, Reason = reason, RootCause = reason,
        Resolution = resolution, ResolvedById = resolvedById,
        ReportedById = T.u, Status = status
    };

    private static ReworkOrder MakeRework(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        Guid poId, Guid inspectionId, Guid reworkRoutingId,
        decimal qty, string uom, string reason,
        DateTime scheduledStart, DateTime scheduledEnd) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        ProductionOrderId = poId, InspectionId = inspectionId,
        ReworkRoutingId = reworkRoutingId,
        Quantity = qty, UnitOfMeasure = uom, Reason = reason,
        Status = "Draft", ScheduledStartDate = scheduledStart,
        ScheduledEndDate = scheduledEnd
    };

    private static SubContractOrder MakeSubCon(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        Guid poId, Guid poOpId,
        Guid vendorId, Guid? purchaseOrderId,
        decimal qtySent, DateTime sentAt, DateTime expectedReturn, DateTime? actualReturn,
        decimal unitCost, decimal totalCost, decimal qtyRejected, string status) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        ProductionOrderId = poId, ProductionOrderOperationId = poOpId,
        VendorId = vendorId, PurchaseOrderId = purchaseOrderId,
        QuantitySent = qtySent, QuantityReceived = qtySent - qtyRejected,
        QuantityRejected = qtyRejected,
        SentAt = sentAt, ExpectedReturnDate = expectedReturn, ActualReturnDate = actualReturn,
        UnitCost = unitCost, TotalCost = totalCost, Status = status
    };

    private static CapacityLoad MakeCapLoad(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        Guid workCenterId, Guid shiftId, DateTime date,
        decimal required, decimal available, decimal loadPct) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        WorkCenterId = workCenterId, WorkCenterShiftId = shiftId,
        Date = date, RequiredHours = required, AvailableHours = available,
        LoadPercentage = loadPct, ProductionOrderCount = 1,
        IsOverloaded = loadPct > 100
    };

    private static InventoryTransaction MakeInvTxn(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        Guid productId, decimal qty, string txnType,
        Guid referenceId, string referenceType, DateTime txnDate) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        ProductId = productId, Quantity = qty,
        TransactionType = txnType, ReferenceId = referenceId,
        ReferenceType = referenceType, TransactionDate = txnDate
    };
}
