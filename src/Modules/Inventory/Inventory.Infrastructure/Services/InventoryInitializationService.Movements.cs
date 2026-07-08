using Inventory.Domain.Constants;
using Inventory.Domain.Entities;

namespace Inventory.Infrastructure.Services;

/// <summary>
/// Seed data for Item Prices, Item Taxes, Inventory Documents, Transactions,
/// Balances, Cost Layers, and Valuations.
/// All movements are mutually consistent so that stock-level, valuation,
/// and ledger reports show meaningful data from day one.
/// </summary>
public partial class InventoryInitializationService
{
    // ────────────────────────────────────────────────────────────────────────
    // 11. ITEM PRICES
    //     Default + Wholesale + Retail price lists for every item
    // ────────────────────────────────────────────────────────────────────────

    private static ItemPrice[] SeedItemPrices(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
        Item[] items,
        Unit[] units,
        TaxDefinition[] taxes)
    {
        var now      = DateTime.UtcNow;
        var list     = new List<ItemPrice>();
        var unitPcs  = units.FirstOrDefault(u => u.Code == "PCS")?.Id ?? units[0].Id;
        var taxRate  = taxes.FirstOrDefault()?.Rate ?? 0m;

        // Per-item pricing table (code → (purchase, defaultSale, wholesale, retail))
        var pricing = new Dictionary<string, (decimal buy, decimal def, decimal whl, decimal ret)>
        {
            // Beverages
            ["BEV-PEPSI-250"]    = (45m,  55m,  52m,  60m),
            ["BEV-PEPSI-500"]    = (80m,  95m,  90m,  105m),
            ["BEV-PEPSI-1L"]     = (110m, 135m, 128m, 148m),
            ["BEV-COKE-250"]     = (48m,  60m,  56m,  65m),
            ["BEV-COKE-500"]     = (85m,  100m, 95m,  110m),
            ["BEV-NWL-500"]      = (30m,  40m,  38m,  45m),
            ["BEV-REDBULL-250"]  = (180m, 220m, 210m, 240m),
            ["BEV-NESCAF-200"]   = (550m, 680m, 650m, 720m),
            ["BEV-MILO-400"]     = (480m, 590m, 565m, 620m),
            // Dairy
            ["DAI-OLPERS-1L"]    = (200m, 240m, 228m, 255m),
            ["DAI-MILKPAK-1L"]   = (195m, 235m, 224m, 250m),
            ["DAI-EGGS-30"]      = (750m, 900m, 860m, 950m),
            ["DAI-EGGS-12"]      = (310m, 380m, 360m, 400m),
            // Food
            ["GROC-ATTA-10"]     = (1100m,1350m,1280m,1450m),
            ["GROC-RICE-SK5"]    = (1400m,1700m,1620m,1800m),
            ["GROC-SUGAR-1"]     = (175m, 210m, 200m, 225m),
            ["GROC-OIL-DALDA1"]  = (380m, 460m, 440m, 490m),
            ["GROC-OIL-DALDA3"]  = (1100m,1320m,1260m,1400m),
            ["GROC-OIL-DALDA5"]  = (1780m,2100m,2000m,2250m),
            // Snacks
            ["SNK-LAYS-MK26"]    = (35m,  45m,  42m,  50m),
            ["SNK-LAYS-MK65"]    = (75m,  90m,  85m,  100m),
            ["SNK-KITKAT-42"]    = (90m,  110m, 105m, 120m),
            ["SNK-CADBURY-40"]   = (110m, 135m, 128m, 145m),
            ["SNK-PRINGLES-OR"]  = (380m, 460m, 440m, 490m),
            // Personal Care
            ["PC-SHP-SNSL180"]   = (290m, 360m, 340m, 385m),
            ["PC-SHP-PANT180"]   = (320m, 390m, 370m, 415m),
            ["PC-SOAP-DOVE100"]  = (120m, 148m, 140m, 158m),
            ["PC-TOOTHP-CLG100"] = (220m, 270m, 255m, 290m),
            // Electronics
            ["ELEC-SAM-A15"]     = (55000m,65000m,62000m,68000m),
            ["ELEC-SAM-A35"]     = (85000m,99000m,95000m,105000m),
            ["ELEC-IPHONE15"]    = (225000m,265000m,258000m,275000m),
            ["ELEC-IPHONE15P"]   = (310000m,360000m,350000m,375000m),
            ["ELEC-JBL-FLIP6"]   = (22000m,27000m,25500m,28500m),
            ["ELEC-CHARGER-65W"] = (2500m, 3200m, 3000m, 3500m),
            ["ELEC-PWRBK-20K"]   = (8500m, 10500m,9800m, 11200m),
            ["ELEC-BATT-AA4"]    = (350m,  440m,  420m,  460m),
            // Health
            ["MED-PANADOL-10"]   = (38m,  48m,  45m,  52m),
            ["MED-PANADOL-30"]   = (95m,  118m, 112m, 128m),
            ["MED-BPMON"]        = (3500m, 4400m,4200m, 4700m),
            // Baby
            ["BABY-DIAP-PAMP-M"] = (1200m,1500m,1420m,1600m),
            ["BABY-NAN1-400"]    = (1800m,2250m,2150m,2400m),
            // Tools
            ["TOOL-DRILL-BSCH"]  = (12000m,15000m,14200m,16000m),
            ["TOOL-EXTNCORD3M"]  = (650m,  820m,  780m,  880m),
            // Sports
            ["SPT-PROTEIN-1KG"]  = (5500m, 6800m, 6500m, 7200m),
            ["SPT-FOOTBALL"]     = (2200m, 2800m, 2650m, 3000m),
            ["SPT-NIKE-42"]      = (18000m,22000m,21000m,23500m),
            // Kitchen
            ["KIT-KETTLE-1L7"]   = (2800m, 3500m, 3350m, 3800m),
            ["KIT-BLENDER"]      = (3500m, 4400m, 4200m, 4800m),
            // Fast-Food menu (sale prices used by POS via the catalog base price)
            ["FF-PIZZA-MARG-S"]  = (350m,  700m,  670m,  750m),
            ["FF-PIZZA-MARG-M"]  = (500m,  1050m, 1000m, 1120m),
            ["FF-PIZZA-MARG-L"]  = (700m,  1400m, 1340m, 1500m),
            ["FF-PIZZA-PEP-S"]   = (430m,  850m,  810m,  910m),
            ["FF-PIZZA-PEP-M"]   = (600m,  1200m, 1150m, 1290m),
            ["FF-PIZZA-PEP-L"]   = (820m,  1600m, 1530m, 1720m),
            ["FF-PIZZA-TIKKA-S"] = (430m,  850m,  810m,  910m),
            ["FF-PIZZA-TIKKA-M"] = (600m,  1200m, 1150m, 1290m),
            ["FF-PIZZA-TIKKA-L"] = (820m,  1600m, 1530m, 1720m),
            ["FF-BURGER-BEEF"]   = (250m,  450m,  430m,  490m),
            ["FF-BURGER-ZINGER"] = (280m,  500m,  480m,  540m),
            ["FF-BURGER-CHEESE"] = (300m,  520m,  500m,  560m),
            ["FF-FRIES-REG"]     = (90m,   220m,  210m,  240m),
            ["FF-FRIES-LRG"]     = (130m,  320m,  300m,  350m),
            ["FF-DEAL-FAMILY"]   = (1100m, 1499m, 1499m, 1499m),
        };

        foreach (var item in items)
        {
            var unitId = item.BaseUnitId;
            var (buyPrice, defSale, whlSale, retSale) = pricing.TryGetValue(item.Code, out var p)
                ? p
                : (100m, 130m, 122m, 140m);   // fallback for unlisted items

            var margin = taxRate / 100m;

            void AddPrice(string priceList, decimal sale, decimal purchase)
            {
                var saleExcl   = taxRate > 0 ? Math.Round(sale / (1 + margin), 4) : sale;
                var saleTax    = sale - saleExcl;
                var buyExcl    = taxRate > 0 ? Math.Round(purchase / (1 + margin), 4) : purchase;
                var buyTax     = purchase - buyExcl;

                list.Add(new ItemPrice
                {
                    Id                       = Guid.NewGuid(),
                    CompanyId                = T.companyId,
                    BranchId                 = T.branchId,
                    BusinessUnitId           = T.businessUnitId,
                    CreatedByUserId          = T.userId,
                    CreatedAt                = now,
                    ItemId                   = item.Id,
                    UnitId                   = unitId,
                    PriceList                = priceList,
                    PurchasePrice            = purchase,
                    SalePrice                = sale,
                    MinSalePrice             = Math.Round(sale * 0.85m, 2),
                    IsTaxInclusive           = false,
                    EffectiveTaxRate         = taxRate,
                    SalePriceExcludingTax    = saleExcl,
                    SaleTaxAmount            = saleTax,
                    SalePriceIncludingTax    = sale,
                    PurchasePriceExcludingTax= buyExcl,
                    PurchaseTaxAmount        = buyTax,
                    PurchasePriceIncludingTax= purchase,
                    CurrencyCode             = "PKR",
                    IsActive                 = true,
                    ValidFrom                = new DateTime(DateTime.UtcNow.Year, 1, 1),
                });
            }

            AddPrice(PriceList.Default,   defSale, buyPrice);
            AddPrice(PriceList.Wholesale,  whlSale, buyPrice);
            AddPrice(PriceList.Retail,     retSale, buyPrice);
        }

        return list.ToArray();
    }

    // ────────────────────────────────────────────────────────────────────────
    // 12. ITEM TAXES  (assign standard sales tax to every item)
    // ────────────────────────────────────────────────────────────────────────

    private static ItemTax[] SeedItemTaxes(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
        Item[] items,
        TaxDefinition[] taxes)
    {
        if (taxes.Length == 0) return [];

        var now        = DateTime.UtcNow;
        var salesTax   = taxes.FirstOrDefault(t => t.Code == "GST17") ?? taxes[0];

        return items.Select(item => new ItemTax
        {
            Id                  = Guid.NewGuid(),
            CompanyId           = T.companyId,
            BranchId            = T.branchId,
            BusinessUnitId      = T.businessUnitId,
            CreatedByUserId     = T.userId,
            CreatedAt           = now,
            ItemId              = item.Id,
            TaxDefinitionId     = salesTax.Id,
            OverrideRate        = null,           // use definition rate
            EffectiveRate       = salesTax.Rate,
            IsActive            = true,
        }).ToArray();
    }

    // ────────────────────────────────────────────────────────────────────────
    // 13. GRN DOCUMENTS + LINES + RECEIPT TRANSACTIONS + COST LAYERS + BALANCES
    //     Three purchase receipts across different months to produce a history
    // ────────────────────────────────────────────────────────────────────────

    private static (
        InventoryDocument[]   docs,
        InventoryDocumentLine[] lines,
        InventoryTransaction[] txns,
        InventoryCostLayer[]   costLayers,
        InventoryBalance[]     balances)
    SeedGrnMovements(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
        Item[] items,
        Warehouse[] warehouses,
        Bin[] bins,
        Unit[] units)
    {
        var now   = DateTime.UtcNow;
        var year  = now.Year;
        var docs  = new List<InventoryDocument>();
        var lines = new List<InventoryDocumentLine>();
        var txns  = new List<InventoryTransaction>();
        var cls   = new List<InventoryCostLayer>();
        var bals  = new Dictionary<(Guid item, Guid wh, Guid? bin), InventoryBalance>();

        // Main warehouse. Stock is held at WAREHOUSE level (no bin): POS, quick-adjust and the
        // sales/POS deduction all operate on the bin-null balance row, so seeding into a bin would
        // split each item's stock into two rows (a binned seed row + a bin-null operations row) that
        // never reconcile. Keep BinId null everywhere on the balance.
        var mainWh = warehouses.First(w => w.Code == "MAIN-WH");
        Guid? recvBin = null;
        Guid pcsUnit  = units.First(u => u.Code == "PCS").Id;

        // Unit cost lookup by item code prefix
        decimal UnitCost(string code) => code switch
        {
            // Fast-food (most specific first)
            var c when c.StartsWith("FF-DEAL")          => 1100m,
            var c when c.StartsWith("FF-PIZZA")         => 500m,
            var c when c.StartsWith("FF-BURGER")        => 280m,
            var c when c.StartsWith("FF-FRIES")         => 100m,
            var c when c.StartsWith("FF-RM")            => 120m,
            var c when c.StartsWith("ELEC-SAM-A15")    => 55000m,
            var c when c.StartsWith("ELEC-SAM-A35")    => 85000m,
            var c when c.StartsWith("ELEC-IPHONE15P")  => 310000m,
            var c when c.StartsWith("ELEC-IPHONE15")   => 225000m,
            var c when c.StartsWith("ELEC-JBL")        => 22000m,
            var c when c.StartsWith("ELEC-")           => 5000m,
            var c when c.StartsWith("SPT-NIKE")        => 18000m,
            var c when c.StartsWith("SPT-PROTEIN")     => 5500m,
            var c when c.StartsWith("GROC-ATTA")       => 1100m,
            var c when c.StartsWith("GROC-OIL")        => 380m,
            var c when c.StartsWith("GROC-RICE")       => 1400m,
            var c when c.StartsWith("DAI-")            => 200m,
            var c when c.StartsWith("BEV-REDBULL")     => 180m,
            var c when c.StartsWith("BEV-NESCAF")      => 550m,
            var c when c.StartsWith("BEV-")            => 60m,
            var c when c.StartsWith("SNK-PRINGLES")    => 380m,
            var c when c.StartsWith("SNK-KITKAT")      => 90m,
            var c when c.StartsWith("SNK-")            => 50m,
            var c when c.StartsWith("MED-BPMON")       => 3500m,
            var c when c.StartsWith("MED-")            => 45m,
            var c when c.StartsWith("BABY-DIAP")       => 1200m,
            var c when c.StartsWith("BABY-NAN")        => 1800m,
            var c when c.StartsWith("BABY-")           => 250m,
            var c when c.StartsWith("TOOL-DRILL")      => 12000m,
            var c when c.StartsWith("TOOL-")           => 500m,
            var c when c.StartsWith("KIT-")            => 2500m,
            _                                           => 100m,
        };

        // Receive quantities per GRN batch
        int RecvQty(string code) => code switch
        {
            var c when c.StartsWith("FF-RM")          => 200,   // ingredients in bulk
            var c when c.StartsWith("FF-")            => 120,   // prepared menu items on hand
            var c when c.StartsWith("ELEC-SAM-A15")   => 20,
            var c when c.StartsWith("ELEC-SAM-A35")   => 15,
            var c when c.StartsWith("ELEC-IPHONE")    => 10,
            var c when c.StartsWith("ELEC-")          => 30,
            var c when c.StartsWith("GROC-")          => 200,
            var c when c.StartsWith("DAI-")           => 150,
            var c when c.StartsWith("BEV-")           => 300,
            var c when c.StartsWith("SNK-")           => 250,
            var c when c.StartsWith("MED-")           => 100,
            var c when c.StartsWith("BABY-")          => 80,
            _                                          => 50,
        };

        // ── Create three GRNs spread over past 3 months ────────────────────
        var grnDates = new[]
        {
            new DateTime(year, Math.Max(now.Month - 2, 1), 5),
            new DateTime(year, Math.Max(now.Month - 1, 1), 10),
            new DateTime(year, now.Month, 2),
        };

        // Split items into three batches (1/3 each)
        var batches = new[]
        {
            items.Where((_, i) => i % 3 == 0).ToArray(),
            items.Where((_, i) => i % 3 == 1).ToArray(),
            items.Where((_, i) => i % 3 == 2).ToArray(),
        };

        for (int g = 0; g < 3; g++)
        {
            var grnDate  = grnDates[g];
            var batch    = batches[g];
            int lineNo   = 1;

            var doc = new InventoryDocument
            {
                Id             = Guid.NewGuid(),
                CompanyId      = T.companyId,
                BranchId       = T.branchId,
                BusinessUnitId = T.businessUnitId,
                CreatedByUserId= T.userId,
                CreatedAt      = grnDate,
                DocumentNumber = $"GRN-{year}-{g + 1:D4}",
                DocumentType   = InventoryDocumentType.GRN,
                DocumentDate   = grnDate,
                Status         = InventoryDocumentStatus.Posted,
                ToWarehouseId  = mainWh.Id,
                PostingDate    = grnDate,
                PostedByUserId = T.userId,
                Description    = $"Opening stock receipt batch {g + 1}",
                TotalQuantity  = batch.Sum(x => RecvQty(x.Code)),
                TotalCost      = batch.Sum(x => RecvQty(x.Code) * UnitCost(x.Code)),
            };
            docs.Add(doc);

            foreach (var item in batch)
            {
                var qty      = RecvQty(item.Code);
                var unitCost = UnitCost(item.Code);
                var total    = qty * unitCost;
                var unitId   = item.BaseUnitId;
                var key      = (item.Id, mainWh.Id, recvBin);

                var line = new InventoryDocumentLine
                {
                    Id             = Guid.NewGuid(),
                    CompanyId      = T.companyId,
                    BranchId       = T.branchId,
                    BusinessUnitId = T.businessUnitId,
                    CreatedByUserId= T.userId,
                    CreatedAt      = grnDate,
                    DocumentId     = doc.Id,
                    ItemId         = item.Id,
                    WarehouseId    = mainWh.Id,
                    BinId          = recvBin,
                    UnitId         = unitId,
                    Quantity       = qty,
                    UnitCost       = unitCost,
                    TotalCost      = total,
                    LineNumber     = lineNo++,
                    Description    = $"GRN batch {g + 1} receipt",
                };
                lines.Add(line);

                var txn = new InventoryTransaction
                {
                    Id             = Guid.NewGuid(),
                    CompanyId      = T.companyId,
                    BranchId       = T.branchId,
                    BusinessUnitId = T.businessUnitId,
                    CreatedByUserId= T.userId,
                    CreatedAt      = grnDate,
                    ItemId         = item.Id,
                    WarehouseId    = mainWh.Id,
                    BinId          = recvBin,
                    TransactionType= InventoryTransactionType.Receipt,
                    Quantity       = qty,
                    UnitId         = unitId,
                    UnitCost       = unitCost,
                    TotalCost      = total,
                    DocumentId     = doc.Id,
                    DocumentLineId = line.Id,
                    TransactionDate= grnDate,
                };
                txns.Add(txn);

                // FIFO cost layer
                cls.Add(new InventoryCostLayer
                {
                    Id                   = Guid.NewGuid(),
                    CompanyId            = T.companyId,
                    BranchId             = T.branchId,
                    BusinessUnitId       = T.businessUnitId,
                    CreatedByUserId      = T.userId,
                    CreatedAt            = grnDate,
                    ItemId               = item.Id,
                    WarehouseId          = mainWh.Id,
                    ReceiptTransactionId = txn.Id,
                    ReceiptDate          = grnDate,
                    OriginalQuantity     = qty,
                    RemainingQuantity    = qty,
                    UnitCost             = unitCost,
                    IsExhausted          = false,
                });

                // Update or create balance snapshot
                if (bals.TryGetValue(key, out var existingBal))
                {
                    var newQoh    = existingBal.QuantityOnHand + qty;
                    var newTotal  = existingBal.TotalValue + total;
                    existingBal.QuantityOnHand  = newQoh;
                    existingBal.QuantityAvailable = newQoh - existingBal.QuantityReserved;
                    existingBal.AverageCost     = Math.Round(newTotal / newQoh, 4);
                    existingBal.TotalValue      = newTotal;
                    existingBal.LastTransactionDate = grnDate;
                }
                else
                {
                    bals[key] = new InventoryBalance
                    {
                        Id                  = Guid.NewGuid(),
                        CompanyId           = T.companyId,
                        BranchId            = T.branchId,
                        BusinessUnitId      = T.businessUnitId,
                        CreatedByUserId     = T.userId,
                        CreatedAt           = grnDate,
                        ItemId              = item.Id,
                        WarehouseId         = mainWh.Id,
                        BinId               = recvBin,
                        QuantityOnHand      = qty,
                        QuantityReserved    = 0,
                        QuantityAvailable   = qty,
                        AverageCost         = unitCost,
                        TotalValue          = total,
                        CostingMethod       = CostingMethod.MovingAverage,
                        LastTransactionDate = grnDate,
                    };
                }
            }
        }

        return (docs.ToArray(), lines.ToArray(), txns.ToArray(), cls.ToArray(), bals.Values.ToArray());
    }

    // ────────────────────────────────────────────────────────────────────────
    // 14. ISSUE / SALES-OUT DOCUMENTS
    //     Simulate sold quantities to produce COGS data
    // ────────────────────────────────────────────────────────────────────────

    private static (
        InventoryDocument[]    docs,
        InventoryDocumentLine[] lines,
        InventoryTransaction[]  txns,
        InventoryBalance[]      updatedBalances)
    SeedIssueMovements(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
        Item[] items,
        Warehouse[] warehouses,
        Bin[] bins,
        Unit[] units,
        InventoryTransaction[] grnTxns,
        InventoryBalance[] balances)
    {
        var now    = DateTime.UtcNow;
        var year   = now.Year;
        var docs   = new List<InventoryDocument>();
        var lines  = new List<InventoryDocumentLine>();
        var txns   = new List<InventoryTransaction>();
        var modBal = new List<InventoryBalance>();

        var mainWh   = warehouses.First(w => w.Code == "MAIN-WH");
        Guid? issBin = null;   // warehouse-level (bin-null) — see SeedGrnMovements note

        // Two sales-issue documents (month-1 and current month)
        var issueDates = new[]
        {
            new DateTime(year, Math.Max(now.Month - 1, 1), 20),
            new DateTime(year, now.Month, now.Day > 5 ? now.Day - 3 : 1),
        };

        var itemBatches = new[]
        {
            items.Where((_, i) => i % 2 == 0).Take(60).ToArray(),
            items.Where((_, i) => i % 2 == 1).Take(60).ToArray(),
        };

        for (int d = 0; d < 2; d++)
        {
            var issDate = issueDates[d];
            var batch   = itemBatches[d];
            int lineNo  = 1;

            var issDoc = new InventoryDocument
            {
                Id              = Guid.NewGuid(),
                CompanyId       = T.companyId,
                BranchId        = T.branchId,
                BusinessUnitId  = T.businessUnitId,
                CreatedByUserId = T.userId,
                CreatedAt       = issDate,
                DocumentNumber  = $"ISSUE-{year}-{d + 1:D4}",
                DocumentType    = InventoryDocumentType.Delivery,
                DocumentDate    = issDate,
                Status          = InventoryDocumentStatus.Posted,
                FromWarehouseId = mainWh.Id,
                PostingDate     = issDate,
                PostedByUserId  = T.userId,
                Description     = $"Sales delivery batch {d + 1}",
            };

            decimal totalQty  = 0;
            decimal totalCost = 0;

            foreach (var item in batch)
            {
                // Find balance for this item
                var bal = balances.FirstOrDefault(b => b.ItemId == item.Id && b.WarehouseId == mainWh.Id);
                if (bal == null || bal.QuantityAvailable <= 0) continue;

                // Issue ~30 % of on-hand quantity
                var issueQty  = Math.Floor(bal.QuantityOnHand * 0.30m);
                if (issueQty <= 0) continue;

                var unitCost  = bal.AverageCost;
                var total     = issueQty * unitCost;
                var unitId    = item.BaseUnitId;

                var line = new InventoryDocumentLine
                {
                    Id              = Guid.NewGuid(),
                    CompanyId       = T.companyId,
                    BranchId        = T.branchId,
                    BusinessUnitId  = T.businessUnitId,
                    CreatedByUserId = T.userId,
                    CreatedAt       = issDate,
                    DocumentId      = issDoc.Id,
                    ItemId          = item.Id,
                    WarehouseId     = mainWh.Id,
                    BinId           = issBin,
                    UnitId          = unitId,
                    Quantity        = issueQty,
                    UnitCost        = unitCost,
                    TotalCost       = total,
                    LineNumber      = lineNo++,
                    Description     = "Sales issue",
                };
                lines.Add(line);

                txns.Add(new InventoryTransaction
                {
                    Id              = Guid.NewGuid(),
                    CompanyId       = T.companyId,
                    BranchId        = T.branchId,
                    BusinessUnitId  = T.businessUnitId,
                    CreatedByUserId = T.userId,
                    CreatedAt       = issDate,
                    ItemId          = item.Id,
                    WarehouseId     = mainWh.Id,
                    BinId           = issBin,
                    TransactionType = InventoryTransactionType.Issue,
                    Quantity        = -issueQty,      // negative = outbound
                    UnitId          = unitId,
                    UnitCost        = unitCost,
                    TotalCost       = total,
                    DocumentId      = issDoc.Id,
                    DocumentLineId  = line.Id,
                    TransactionDate = issDate,
                });

                // Update balance
                bal.QuantityOnHand    -= issueQty;
                bal.QuantityAvailable  = bal.QuantityOnHand - bal.QuantityReserved;
                bal.TotalValue         = bal.QuantityOnHand * bal.AverageCost;
                bal.LastTransactionDate= issDate;
                modBal.Add(bal);

                totalQty  += issueQty;
                totalCost += total;
            }

            issDoc.TotalQuantity = totalQty;
            issDoc.TotalCost     = totalCost;
            docs.Add(issDoc);
        }

        return (docs.ToArray(), lines.ToArray(), txns.ToArray(), modBal.ToArray());
    }

    // ────────────────────────────────────────────────────────────────────────
    // 15. INTER-WAREHOUSE TRANSFER (MAIN → RETAIL)
    // ────────────────────────────────────────────────────────────────────────

    private static (
        InventoryDocument[]    docs,
        InventoryDocumentLine[] lines,
        InventoryTransaction[]  txns,
        InventoryBalance[]      newBalances)
    SeedTransferMovements(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
        Item[] items,
        Warehouse[] warehouses,
        Bin[] bins,
        Unit[] units,
        InventoryBalance[] existingBalances)
    {
        var now    = DateTime.UtcNow;
        var year   = now.Year;
        var docs   = new List<InventoryDocument>();
        var lines  = new List<InventoryDocumentLine>();
        var txns   = new List<InventoryTransaction>();
        var newBal = new List<InventoryBalance>();

        var mainWh   = warehouses.First(w => w.Code == "MAIN-WH");
        var retailWh = warehouses.FirstOrDefault(w => w.Code == "RETAIL-WH") ?? warehouses[0];
        // Retail stock is held at warehouse level (no bin): the POS sells binless and deducts
        // against the bin-null balance row, so the seeded retail balance must also be bin-null
        // for the two to reconcile. (Binned retail stock would be invisible to POS deductions.)
        Guid? retailBin = null;

        var transferDate = new DateTime(year, now.Month, Math.Max(now.Day - 1, 1));

        var xferDoc = new InventoryDocument
        {
            Id              = Guid.NewGuid(),
            CompanyId       = T.companyId,
            BranchId        = T.branchId,
            BusinessUnitId  = T.businessUnitId,
            CreatedByUserId = T.userId,
            CreatedAt       = transferDate,
            DocumentNumber  = $"XFER-{year}-0001",
            DocumentType    = InventoryDocumentType.Transfer,
            DocumentDate    = transferDate,
            Status          = InventoryDocumentStatus.Posted,
            FromWarehouseId = mainWh.Id,
            ToWarehouseId   = retailWh.Id,
            PostingDate     = transferDate,
            PostedByUserId  = T.userId,
            Description     = "Initial stock replenishment to Retail warehouse",
        };

        // Transfer a sample of fast-moving items (beverages, snacks, personal care)
        // plus the made-to-order menu items & combo deal, so the POS retail store
        // has sellable stock of pizzas/burgers/fries/drinks from day one.
        var fastMovers = items
            .Where(i => i.Code.StartsWith("BEV-") || i.Code.StartsWith("SNK-") || i.Code.StartsWith("PC-")
                     || i.Code.StartsWith("FF-PIZZA-") || i.Code.StartsWith("FF-BURGER-")
                     || i.Code.StartsWith("FF-FRIES-") || i.Code.StartsWith("FF-DEAL-"))
            .Take(120)
            .ToArray();

        int lineNo = 1;
        decimal totalQty = 0, totalCost = 0;

        foreach (var item in fastMovers)
        {
            var srcBal = existingBalances.FirstOrDefault(b => b.ItemId == item.Id && b.WarehouseId == mainWh.Id);
            if (srcBal == null || srcBal.QuantityAvailable < 2) continue;

            var xferQty  = Math.Floor(srcBal.QuantityAvailable * 0.20m);
            if (xferQty <= 0) continue;

            var unitCost = srcBal.AverageCost;
            var total    = xferQty * unitCost;

            var line = new InventoryDocumentLine
            {
                Id              = Guid.NewGuid(),
                CompanyId       = T.companyId,
                BranchId        = T.branchId,
                BusinessUnitId  = T.businessUnitId,
                CreatedByUserId = T.userId,
                CreatedAt       = transferDate,
                DocumentId      = xferDoc.Id,
                ItemId          = item.Id,
                WarehouseId     = retailWh.Id,
                BinId           = retailBin,
                UnitId          = item.BaseUnitId,
                Quantity        = xferQty,
                UnitCost        = unitCost,
                TotalCost       = total,
                LineNumber      = lineNo++,
            };
            lines.Add(line);

            // Transfer OUT from main
            txns.Add(new InventoryTransaction
            {
                Id              = Guid.NewGuid(),
                CompanyId       = T.companyId,
                BranchId        = T.branchId,
                BusinessUnitId  = T.businessUnitId,
                CreatedByUserId = T.userId,
                CreatedAt       = transferDate,
                ItemId          = item.Id,
                WarehouseId     = mainWh.Id,
                TransactionType = InventoryTransactionType.Transfer,
                Quantity        = -xferQty,
                UnitId          = item.BaseUnitId,
                UnitCost        = unitCost,
                TotalCost       = total,
                DocumentId      = xferDoc.Id,
                DocumentLineId  = line.Id,
                TransactionDate = transferDate,
            });

            // Transfer IN to retail
            txns.Add(new InventoryTransaction
            {
                Id              = Guid.NewGuid(),
                CompanyId       = T.companyId,
                BranchId        = T.branchId,
                BusinessUnitId  = T.businessUnitId,
                CreatedByUserId = T.userId,
                CreatedAt       = transferDate,
                ItemId          = item.Id,
                WarehouseId     = retailWh.Id,
                BinId           = retailBin,
                TransactionType = InventoryTransactionType.Transfer,
                Quantity        = xferQty,
                UnitId          = item.BaseUnitId,
                UnitCost        = unitCost,
                TotalCost       = total,
                DocumentId      = xferDoc.Id,
                DocumentLineId  = line.Id,
                TransactionDate = transferDate,
            });

            // New balance at retail warehouse
            newBal.Add(new InventoryBalance
            {
                Id                   = Guid.NewGuid(),
                CompanyId            = T.companyId,
                BranchId             = T.branchId,
                BusinessUnitId       = T.businessUnitId,
                CreatedByUserId      = T.userId,
                CreatedAt            = transferDate,
                ItemId               = item.Id,
                WarehouseId          = retailWh.Id,
                BinId                = retailBin,
                QuantityOnHand       = xferQty,
                QuantityReserved     = 0,
                QuantityAvailable    = xferQty,
                AverageCost          = unitCost,
                TotalValue           = total,
                CostingMethod        = CostingMethod.MovingAverage,
                LastTransactionDate  = transferDate,
            });

            totalQty  += xferQty;
            totalCost += total;

            // Update source balance
            srcBal.QuantityOnHand    -= xferQty;
            srcBal.QuantityAvailable  = srcBal.QuantityOnHand - srcBal.QuantityReserved;
            srcBal.TotalValue         = srcBal.QuantityOnHand * srcBal.AverageCost;
            srcBal.LastTransactionDate= transferDate;
        }

        xferDoc.TotalQuantity = totalQty;
        xferDoc.TotalCost     = totalCost;
        docs.Add(xferDoc);

        return (docs.ToArray(), lines.ToArray(), txns.ToArray(), newBal.ToArray());
    }

    // ────────────────────────────────────────────────────────────────────────
    // 16. INVENTORY VALUATIONS  (monthly end-of-month snapshots)
    // ────────────────────────────────────────────────────────────────────────

    private static InventoryValuation[] SeedInventoryValuations(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
        Item[] items,
        Warehouse[] warehouses,
        InventoryBalance[] balances)
    {
        var now    = DateTime.UtcNow;
        var year   = now.Year;
        var list   = new List<InventoryValuation>();
        var mainWh = warehouses.First(w => w.Code == "MAIN-WH");

        // Create end-of-month valuations for the past 3 months
        var monthOffsets = new[] { -2, -1, 0 };

        foreach (var offset in monthOffsets)
        {
            var m   = now.Month + offset;
            var y   = year;
            if (m <= 0) { m += 12; y--; }
            var eom = new DateTime(y, m, DateTime.DaysInMonth(y, m), 23, 59, 59);
            var periodRef = $"{y}-{m:D2}";

            // Snap balances (use current balance as approximation)
            foreach (var bal in balances.Where(b => b.WarehouseId == mainWh.Id))
            {
                // Simulate slightly different quantities per month
                var factor = offset switch { -2 => 1.2m, -1 => 1.1m, _ => 1.0m };
                var qty    = Math.Round(bal.QuantityOnHand * factor, 4);
                if (qty <= 0) continue;

                list.Add(new InventoryValuation
                {
                    Id              = Guid.NewGuid(),
                    CompanyId       = T.companyId,
                    BranchId        = T.branchId,
                    BusinessUnitId  = T.businessUnitId,
                    CreatedByUserId = T.userId,
                    CreatedAt       = eom,
                    ItemId          = bal.ItemId,
                    WarehouseId     = mainWh.Id,
                    ValuationDate   = eom,
                    Quantity        = qty,
                    UnitCost        = bal.AverageCost,
                    TotalValue      = Math.Round(qty * bal.AverageCost, 2),
                    ValuationMethod = CostingMethod.MovingAverage,
                    PeriodReference = periodRef,
                });
            }
        }

        return list.ToArray();
    }
}
