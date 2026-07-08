using Inventory.Domain.Entities;
using Nexcore.SharedKernel.Events;
using Nexcore.SharedKernel.Helpers;

namespace Inventory.Infrastructure.Services;

/// <summary>
/// Seed data — Items, Barcodes, Images
/// Pakistan + International Cash &amp; Carry catalogue (~1,000+ products)
/// </summary>
public partial class InventoryInitializationService
{
    private static (Item[] items, ItemBarcode[] barcodes, ItemImage[] images) SeedItems(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
        Unit[] units,
        List<ItemCategory> categories,
        Brand[] brands,
        GlAccountSet gl)
    {
        var now      = DateTime.UtcNow;
        var itemList = new List<Item>();
        var bcList   = new List<ItemBarcode>();
        var imgList  = new List<ItemImage>();

        // ── Unit lookups ────────────────────────────────────────────────────
        Guid PCS = units.First(u => u.Code == "PCS").Id;
        Guid KG  = units.First(u => u.Code == "KG").Id;
        Guid G   = units.First(u => u.Code == "G").Id;
        Guid L   = units.First(u => u.Code == "L").Id;
        Guid ML  = units.First(u => u.Code == "ML").Id;
        Guid PKT = units.First(u => u.Code == "PKT").Id;
        Guid BOX = units.First(u => u.Code == "BOX").Id;
        Guid CTN = units.First(u => u.Code == "CTN").Id;

        // ── Helpers ─────────────────────────────────────────────────────────
        Guid CatId(string code) => categories.First(c => c.Code == code).Id;
        Guid? BrandId(string? code) => code == null ? null : brands.FirstOrDefault(b => b.Code == code)?.Id;

        // Seed images served from wwwroot/images/products/ (placeholder — remove after real images are uploaded)
        var seedImages = new[]
        {
            "/images/products/car-air-freshener-hanging.jpg",
            "/images/products/car-wash-shampoo-500ml.jpg",
            "/images/products/castrol-gtx-20w50-1l.jpg",
            "/images/products/castrol-gtx-20w50-4l.jpg",
            "/images/products/car-polish-wax-300g.jpg",
            "/images/products/wiper-blade-20-inch.jpg",
            "/images/products/nestle-cerelac-rice-250g.jpg",
            "/images/products/pampers-large-34-pcs.jpg",
            "/images/products/pampers-medium-38-pcs.jpg",
            "/images/products/johnsons-baby-lotion-200ml.jpg",
            "/images/products/nestle-nan-pro-1-400g.jpg",
            "/images/products/johnsons-baby-powder-200g.jpg",
            "/images/products/johnsons-baby-shampoo-200ml.jpg",
            "/images/products/huggies-wipes-80-pack.jpg",
            "/images/products/johnsons-wipes-80-pack.jpg",
            "/images/products/brown-bread-400g.jpg",
            "/images/products/wonder-white-bread-400g.jpg",
            "/images/products/larosh-cup-cake-6-pack.jpg",
            "/images/products/croissant-6-pack.jpg",
        };

        var itemImageMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["AUTO-AIRFRSH"]     = "/images/products/car-air-freshener-hanging.jpg",
            ["AUTO-CARWASH"]     = "/images/products/car-wash-shampoo-500ml.jpg",
            ["AUTO-OIL-CAS1L"]  = "/images/products/castrol-gtx-20w50-1l.jpg",
            ["AUTO-OIL-CAS4L"]  = "/images/products/castrol-gtx-20w50-4l.jpg",
            ["AUTO-POLWAX"]      = "/images/products/car-polish-wax-300g.jpg",
            ["AUTO-WIPER-20"]    = "/images/products/wiper-blade-20-inch.jpg",
            ["BABY-CERELAC-250"] = "/images/products/nestle-cerelac-rice-250g.jpg",
            ["BABY-DIAP-PAMP-L"] = "/images/products/pampers-large-34-pcs.jpg",
            ["BABY-DIAP-PAMP-M"] = "/images/products/pampers-medium-38-pcs.jpg",
            ["BABY-LOT-JNJ200"]  = "/images/products/johnsons-baby-lotion-200ml.jpg",
            ["BABY-NAN1-400"]    = "/images/products/nestle-nan-pro-1-400g.jpg",
            ["BABY-NAN2-400"]    = "/images/products/nestle-nan-pro-1-400g.jpg",
            ["BABY-PWD-JNJ200"]  = "/images/products/johnsons-baby-powder-200g.jpg",
            ["BABY-SHP-JNJ200"]  = "/images/products/johnsons-baby-shampoo-200ml.jpg",
            ["BABY-WIPES-HGS80"] = "/images/products/huggies-wipes-80-pack.jpg",
            ["BABY-WIPES-JNJ80"] = "/images/products/johnsons-wipes-80-pack.jpg",
            ["BAK-BREAD-BRN"]    = "/images/products/brown-bread-400g.jpg",
            ["BAK-BREAD-WH"]     = "/images/products/wonder-white-bread-400g.jpg",
            ["BAK-CAKE-CUP6"]    = "/images/products/larosh-cup-cake-6-pack.jpg",
            ["BAK-CROISSANT-6"]  = "/images/products/croissant-6-pack.jpg",

            // Fast-food (pizza shop) menu items — all sizes share the same dish photo
            ["FF-PIZZA-MARG-S"]  = "/images/products/margherita-pizza.jpg",
            ["FF-PIZZA-MARG-M"]  = "/images/products/margherita-pizza.jpg",
            ["FF-PIZZA-MARG-L"]  = "/images/products/margherita-pizza.jpg",
            ["FF-PIZZA-PEP-S"]   = "/images/products/pepperoni-pizza.jpg",
            ["FF-PIZZA-PEP-M"]   = "/images/products/pepperoni-pizza.jpg",
            ["FF-PIZZA-PEP-L"]   = "/images/products/pepperoni-pizza.jpg",
            ["FF-PIZZA-TIKKA-S"] = "/images/products/chicken-tikka-pizza.jpg",
            ["FF-PIZZA-TIKKA-M"] = "/images/products/chicken-tikka-pizza.jpg",
            ["FF-PIZZA-TIKKA-L"] = "/images/products/chicken-tikka-pizza.jpg",
            ["FF-BURGER-BEEF"]   = "/images/products/beef-burger.jpg",
            ["FF-BURGER-ZINGER"] = "/images/products/chicken-zinger-burger.jpg",
            ["FF-BURGER-CHEESE"] = "/images/products/cheese-burger.jpg",
            ["FF-FRIES-REG"]     = "/images/products/french-fries.jpg",
            ["FF-FRIES-LRG"]     = "/images/products/french-fries.jpg",
        };

        Item MkItem(string code, string name, string catCode, string? brandCode,
            Guid unitId, string type = "Inventory", string? shortDesc = null) =>
            new()
            {
                Id                  = Guid.NewGuid(),
                CompanyId           = T.companyId,
                BranchId            = T.branchId,
                BusinessUnitId      = T.businessUnitId,
                CreatedByUserId     = T.userId,
                CreatedAt           = now,
                Code                = code,
                Name                = name,
                ItemType            = type,
                ShortDescription    = shortDesc,
                CategoryId          = CatId(catCode),
                BrandId             = BrandId(brandCode),
                BaseUnitId          = unitId,
                CostingMethod       = "MovingAverage",
                IsActive            = true,
                IsPublished         = true,
                Condition           = "New",
                AlertOnLowStock     = true,
                ReorderLevel        = 10,
                // ── GL accounts from Accounting module ──
                InventoryAccountId  = gl.Inventory,
                CogsAccountId       = gl.Cogs,
                PurchaseAccountId   = gl.Purchase,
                SalesAccountId      = gl.Sales,
            };

        void AddBarcode(Guid itemId, string barcode, bool isPrimary = true) =>
            bcList.Add(new ItemBarcode
            {
                Id               = Guid.NewGuid(),
                CompanyId        = T.companyId,
                BranchId         = T.branchId,
                BusinessUnitId   = T.businessUnitId,
                CreatedByUserId  = T.userId,
                CreatedAt        = now,
                ItemId           = itemId,
                Barcode          = barcode,
                BarcodeType      = "EAN13",
                IsPrimary        = isPrimary,
                IsActive         = true
            });

        void AddImage(Guid itemId, string productCode, string altText)
        {
            var url = itemImageMap.TryGetValue(productCode, out var specific)
                ? specific
                : seedImages[Math.Abs(productCode.GetHashCode()) % seedImages.Length];

            imgList.Add(new ItemImage
            {
                Id               = Guid.NewGuid(),
                CompanyId        = T.companyId,
                BranchId         = T.branchId,
                BusinessUnitId   = T.businessUnitId,
                CreatedByUserId  = T.userId,
                CreatedAt        = now,
                ItemId           = itemId,
                Url              = url,
                Resolution       = "Original",
                AltText          = altText,
                IsPrimary        = true,
                DisplayOrder     = 1,
                ContentType      = "image/jpeg"
            });
        }

        void Add(Item item, string barcode, string? barcode2 = null)
        {
            itemList.Add(item);
            AddBarcode(item.Id, barcode);
            if (barcode2 != null) AddBarcode(item.Id, barcode2, false);
            AddImage(item.Id, item.Code, item.Name);
        }

        // ====================================================================
        // BEVERAGES
        // ====================================================================
        Add(MkItem("BEV-PEPSI-250",    "Pepsi 250ml Can",           "BEVERAGES", null,       ML),  "6281006710014");
        Add(MkItem("BEV-PEPSI-500",    "Pepsi 500ml Bottle",        "BEVERAGES", null,       ML),  "6281006710021");
        Add(MkItem("BEV-PEPSI-1L",     "Pepsi 1 Litre Bottle",      "BEVERAGES", null,       L),   "6281006710038");
        Add(MkItem("BEV-PEPSI-15L",    "Pepsi 1.5 Litre",           "BEVERAGES", null,       L),   "6281006710045");
        Add(MkItem("BEV-PEPSI-2L",     "Pepsi 2 Litre",             "BEVERAGES", null,       L),   "6281006710052");
        Add(MkItem("BEV-PEPSI-CTN24",  "Pepsi 250ml Cans 24pk",     "BEVERAGES", null,       CTN), "6281006710069");
        Add(MkItem("BEV-COKE-250",     "Coca-Cola 250ml Can",       "BEVERAGES", null,       ML),  "5449000000996");
        Add(MkItem("BEV-COKE-500",     "Coca-Cola 500ml Bottle",    "BEVERAGES", null,       ML),  "5449000054227");
        Add(MkItem("BEV-COKE-1L",      "Coca-Cola 1 Litre",         "BEVERAGES", null,       L),   "5449000131805");
        Add(MkItem("BEV-COKE-15L",     "Coca-Cola 1.5 Litre",       "BEVERAGES", null,       L),   "5449000214799");
        Add(MkItem("BEV-COKE-2L",      "Coca-Cola 2 Litre",         "BEVERAGES", null,       L),   "5449000054203");
        Add(MkItem("BEV-SPRITE-500",   "Sprite 500ml Bottle",       "BEVERAGES", null,       ML),  "5449000054210");
        Add(MkItem("BEV-SPRITE-15L",   "Sprite 1.5 Litre",          "BEVERAGES", null,       L),   "5449000054234");
        Add(MkItem("BEV-7UP-500",      "7UP 500ml Bottle",          "BEVERAGES", null,       ML),  "6281006710076");
        Add(MkItem("BEV-7UP-15L",      "7UP 1.5 Litre",             "BEVERAGES", null,       L),   "6281006710083");
        Add(MkItem("BEV-7UP-2L",       "7UP 2 Litre",               "BEVERAGES", null,       L),   "6281006710090");
        Add(MkItem("BEV-STING-250",    "Sting Energy 250ml",        "BEVERAGES", null,       ML),  "6281006710107");
        Add(MkItem("BEV-STING-500",    "Sting Energy 500ml",        "BEVERAGES", null,       ML),  "6281006710114");
        Add(MkItem("BEV-MTN-DEW-500",  "Mountain Dew 500ml",        "BEVERAGES", null,       ML),  "6281006710121");
        Add(MkItem("BEV-MTN-DEW-15L",  "Mountain Dew 1.5L",         "BEVERAGES", null,       L),   "6281006710138");
        Add(MkItem("BEV-FANTA-500",    "Fanta Orange 500ml",        "BEVERAGES", null,       ML),  "5449000133380");
        Add(MkItem("BEV-FANTA-15L",    "Fanta Orange 1.5L",         "BEVERAGES", null,       L),   "5449000133397");
        Add(MkItem("BEV-NWL-500",      "Nestle Pure Life 500ml",    "BEVERAGES", "NESTLE",   ML, shortDesc: "Mineral water"), "6281014030027");
        Add(MkItem("BEV-NWL-1L5",      "Nestle Pure Life 1.5L",     "BEVERAGES", "NESTLE",   L),   "6281014030034");
        Add(MkItem("BEV-NWL-5G",       "Nestle Pure Life 5 Gal",    "BEVERAGES", "NESTLE",   L),   "6281014030041");
        Add(MkItem("BEV-NWL-19L",      "Nestle Pure Life 19L",      "BEVERAGES", "NESTLE",   L),   "6281014030058");
        Add(MkItem("BEV-AQUAFINA-500", "Aquafina 500ml",             "BEVERAGES", null,       ML),  "6281006720013");
        Add(MkItem("BEV-AQUAFINA-1L",  "Aquafina 1L",               "BEVERAGES", null,       L),   "6281006720020");
        Add(MkItem("BEV-REDBULL-250",  "Red Bull 250ml",             "BEVERAGES", null,       ML),  "9002490100070");
        Add(MkItem("BEV-MONSTER-500",  "Monster Energy 500ml",      "BEVERAGES", null,       ML),  "5060517882118");
        Add(MkItem("BEV-FRTVIT-MNG",   "Fruita Vitals Mango 200ml", "BEVERAGES", "NESTLE",   ML),  "6281014040026");
        Add(MkItem("BEV-FRTVIT-GVA",   "Fruita Vitals Guava 200ml", "BEVERAGES", "NESTLE",   ML),  "6281014040033");
        Add(MkItem("BEV-FRTVIT-APL",   "Fruita Vitals Apple 200ml", "BEVERAGES", "NESTLE",   ML),  "6281014040040");
        Add(MkItem("BEV-NESFRUT-MNG",  "Nestle Fruitful Mango 1L",  "BEVERAGES", "NESTLE",   L),   "6281014040057");
        Add(MkItem("BEV-NESFRUT-ONG",  "Nestle Fruitful Orange 1L", "BEVERAGES", "NESTLE",   L),   "6281014040064");
        Add(MkItem("BEV-TAPAL-200",    "Tapal Danedar Tea 200g",    "BEVERAGES", null,       PKT), "6281006061018");
        Add(MkItem("BEV-TAPAL-450",    "Tapal Danedar Tea 450g",    "BEVERAGES", null,       PKT), "6281006061025");
        Add(MkItem("BEV-TAPAL-900",    "Tapal Danedar Tea 900g",    "BEVERAGES", null,       PKT), "6281006061032");
        Add(MkItem("BEV-LIPTON-100TB", "Lipton Yellow Label 100tb", "BEVERAGES", "UNILEVER", BOX), "8722700052791");
        Add(MkItem("BEV-LIPTON-200TB", "Lipton Yellow Label 200tb", "BEVERAGES", "UNILEVER", BOX), "8722700052807");
        Add(MkItem("BEV-NESCAF-200",   "Nescafe Classic 200g",      "BEVERAGES", "NESTLE",   PKT), "7613036113878");
        Add(MkItem("BEV-NESCAF-400",   "Nescafe Classic 400g",      "BEVERAGES", "NESTLE",   PKT), "7613036113885");
        Add(MkItem("BEV-NESCAF-GOLD",  "Nescafe Gold 100g",         "BEVERAGES", "NESTLE",   PKT), "7613036113892");
        Add(MkItem("BEV-MILO-400",     "Milo 400g Tin",             "BEVERAGES", "NESTLE",   PCS), "6281014050025");
        Add(MkItem("BEV-MILO-1KG",     "Milo 1kg Pack",             "BEVERAGES", "NESTLE",   PKT), "6281014050032");
        Add(MkItem("BEV-OVALTINE-400", "Ovaltine 400g",             "BEVERAGES", null,       PCS), "6281014050049");
        Add(MkItem("BEV-HORLICKS-500", "Horlicks 500g",             "BEVERAGES", null,       PCS), "8901030792427");

        // ====================================================================
        // DAIRY & EGGS
        // ====================================================================
        Add(MkItem("DAI-OLPERS-1L",    "Olpers Full Cream Milk 1L", "DAIRY", null,       L, shortDesc: "UHT Milk"), "6281006170006");
        Add(MkItem("DAI-OLPERS-500",   "Olpers Milk 500ml",         "DAIRY", null,       ML),  "6281006170013");
        Add(MkItem("DAI-OLPERS-250",   "Olpers Milk 250ml",         "DAIRY", null,       ML),  "6281006170020");
        Add(MkItem("DAI-OLPERS-6PK",   "Olpers Milk 1L x 6 Pack",  "DAIRY", null,       CTN), "6281006170037");
        Add(MkItem("DAI-MILKPAK-1L",   "Nestle Milkpak 1L",         "DAIRY", "NESTLE",   L),   "6281014060024");
        Add(MkItem("DAI-MILKPAK-500",  "Nestle Milkpak 500ml",      "DAIRY", "NESTLE",   ML),  "6281014060031");
        Add(MkItem("DAI-HALEEB-1L",    "Haleeb Full Cream 1L",      "DAIRY", null,       L),   "6281006180005");
        Add(MkItem("DAI-GOODMILK-1L",  "Good Milk 1L",              "DAIRY", null,       L),   "6281006190004");
        Add(MkItem("DAI-OLPERS-CREAM", "Olpers Cream 200ml",        "DAIRY", null,       ML),  "6281006170044");
        Add(MkItem("DAI-OLPERS-DAHI",  "Olpers Dahi 400g",          "DAIRY", null,       PCS), "6281006170051");
        Add(MkItem("DAI-OLPERS-DAHI1K","Olpers Dahi 1kg",           "DAIRY", null,       PCS), "6281006170068");
        Add(MkItem("DAI-NESTLE-YOG125","Nestle Yogurt 125g",        "DAIRY", "NESTLE",   PCS), "6281014060055");
        Add(MkItem("DAI-NESTLE-YOG400","Nestle Yogurt 400g",        "DAIRY", "NESTLE",   PCS), "6281014060062");
        Add(MkItem("DAI-LURPAK-100",   "Lurpak Butter 100g",        "DAIRY", null,       PCS), "5740900180017");
        Add(MkItem("DAI-LURPAK-200",   "Lurpak Butter 200g",        "DAIRY", null,       PCS), "5740900180024");
        Add(MkItem("DAI-EGGS-30",      "Farm Fresh Eggs 30 Tray",   "DAIRY", null,       PCS), "6281000300301");
        Add(MkItem("DAI-EGGS-12",      "Eggs 12 Pack",              "DAIRY", null,       PCS), "6281000300318");
        Add(MkItem("DAI-CHEESE-200",   "Kraft Cheddar Slice 200g",  "DAIRY", null,       PKT), "0021000015528");
        Add(MkItem("DAI-CHEDDAR-400",  "Adams Cheddar Block 400g",  "DAIRY", null,       PCS), "6281006200018");

        // ====================================================================
        // FOOD STAPLES
        // ====================================================================
        Add(MkItem("GROC-ATTA-10",     "Sunridge Atta 10kg",        "FOOD",  null,       PKT), "6281030001001");
        Add(MkItem("GROC-ATTA-5",      "Sunridge Atta 5kg",         "FOOD",  null,       PKT), "6281030001018");
        Add(MkItem("GROC-ATTA-2",      "Sunridge Atta 2kg",         "FOOD",  null,       PKT), "6281030001025");
        Add(MkItem("GROC-GRDMLS-10",   "Grand Mills Atta 10kg",     "FOOD",  null,       PKT), "6281030003001");
        Add(MkItem("GROC-RICE-SK5",    "Super Kernel Rice 5kg",     "FOOD",  null,       PKT), "6281030010001");
        Add(MkItem("GROC-RICE-SK25",   "Super Kernel Rice 25kg",    "FOOD",  null,       PKT), "6281030010018");
        Add(MkItem("GROC-RICE-BAS5",   "Basmati Rice 5kg",          "FOOD",  null,       PKT), "6281030011001");
        Add(MkItem("GROC-RICE-BAS25",  "Basmati Rice 25kg",         "FOOD",  null,       PKT), "6281030011018");
        Add(MkItem("GROC-SUGAR-1",     "Crystal Sugar 1kg",         "FOOD",  null,       PKT), "6281030020001");
        Add(MkItem("GROC-SUGAR-5",     "Crystal Sugar 5kg",         "FOOD",  null,       PKT), "6281030020018");
        Add(MkItem("GROC-SUGAR-50",    "Crystal Sugar 50kg",        "FOOD",  null,       PKT), "6281030020025");
        Add(MkItem("GROC-SALT-800",    "Flavour Salt 800g",         "FOOD",  null,       PKT), "6281030030001");
        Add(MkItem("GROC-SALT-1KG",    "Mehran Salt 1kg",           "FOOD",  null,       PKT), "6281030031001");
        Add(MkItem("GROC-OIL-DALDA1",  "Dalda Cooking Oil 1L",      "FOOD",  null,       L),   "6281030040001");
        Add(MkItem("GROC-OIL-DALDA3",  "Dalda Cooking Oil 3L",      "FOOD",  null,       L),   "6281030040018");
        Add(MkItem("GROC-OIL-DALDA5",  "Dalda Cooking Oil 5L",      "FOOD",  null,       L),   "6281030040025");
        Add(MkItem("GROC-OIL-SUFI1",   "Sufi Cooking Oil 1L",       "FOOD",  null,       L),   "6281030041001");
        Add(MkItem("GROC-OIL-SUFI3",   "Sufi Cooking Oil 3L",       "FOOD",  null,       L),   "6281030041018");
        Add(MkItem("GROC-OIL-SUFI5",   "Sufi Cooking Oil 5L",       "FOOD",  null,       L),   "6281030041025");
        Add(MkItem("GROC-OIL-MZNL1",   "Mezan Cooking Oil 1L",      "FOOD",  null,       L),   "6281030043001");
        Add(MkItem("GROC-OIL-MZNL5",   "Mezan Cooking Oil 5L",      "FOOD",  null,       L),   "6281030043018");
        Add(MkItem("GROC-OIL-OLIV",    "Borges Olive Oil 500ml",    "FOOD",  null,       ML),  "8410179012245");
        Add(MkItem("GROC-GHEE-DALDA1", "Dalda Banaspati 1kg",       "FOOD",  null,       PCS), "6281030050001");
        Add(MkItem("GROC-GHEE-DALDA5", "Dalda Banaspati 5kg",       "FOOD",  null,       PCS), "6281030050018");
        Add(MkItem("GROC-PULSES-MASH", "Mehran Mash Daal 500g",     "FOOD",  null,       PKT), "6281030060001");
        Add(MkItem("GROC-PULSES-LNTL", "Mehran Masoor Daal 500g",   "FOOD",  null,       PKT), "6281030060018");
        Add(MkItem("GROC-PULSES-CHAN",  "Mehran Chana Daal 500g",    "FOOD",  null,       PKT), "6281030060025");
        Add(MkItem("GROC-PULSES-MOONG","Moong Daal 500g",           "FOOD",  null,       PKT), "6281030061001");

        // ====================================================================
        // SNACKS & CONFECTIONERY
        // ====================================================================
        Add(MkItem("SNK-LAYS-MK26",    "Lays Magic Masala 26g",     "SNACKS", null,      PKT), "0028400047593");
        Add(MkItem("SNK-LAYS-MK65",    "Lays Magic Masala 65g",     "SNACKS", null,      PKT), "0028400047609");
        Add(MkItem("SNK-LAYS-MK150",   "Lays Magic Masala 150g",    "SNACKS", null,      PKT), "0028400047616");
        Add(MkItem("SNK-LAYS-CH26",    "Lays Cheddar 26g",          "SNACKS", null,      PKT), "0028400047623");
        Add(MkItem("SNK-LAYS-CH65",    "Lays Cheddar 65g",          "SNACKS", null,      PKT), "0028400047630");
        Add(MkItem("SNK-LAYS-BBQ26",   "Lays BBQ 26g",              "SNACKS", null,      PKT), "0028400047654");
        Add(MkItem("SNK-LAYS-BBQ65",   "Lays BBQ 65g",              "SNACKS", null,      PKT), "0028400047661");
        Add(MkItem("SNK-KURKURE-30",   "Kurkure Masala Munch 30g",  "SNACKS", null,      PKT), "8901491001269");
        Add(MkItem("SNK-KURKURE-65",   "Kurkure Masala Munch 65g",  "SNACKS", null,      PKT), "8901491001276");
        Add(MkItem("SNK-KURKURE-125",  "Kurkure Masala Munch 125g", "SNACKS", null,      PKT), "8901491001283");
        Add(MkItem("SNK-PRINGLES-OR",  "Pringles Original 165g",    "SNACKS", null,      PCS), "5053990101507");
        Add(MkItem("SNK-PRINGLES-SC",  "Pringles Sour Cream 165g",  "SNACKS", null,      PCS), "5053990101514");
        Add(MkItem("SNK-PRINGLES-CH",  "Pringles Cheese 165g",      "SNACKS", null,      PCS), "5053990101521");
        Add(MkItem("SNK-PRINGLES-BB",  "Pringles BBQ 165g",         "SNACKS", null,      PCS), "5053990101538");
        Add(MkItem("SNK-PF-SOOPER60",  "Peek Freans Sooper 60g",    "SNACKS", null,      PKT), "6281022010001");
        Add(MkItem("SNK-PF-SOOPER150", "Peek Freans Sooper 150g",   "SNACKS", null,      PKT), "6281022010018");
        Add(MkItem("SNK-PF-MARIE100",  "Peek Freans Marie 100g",    "SNACKS", null,      PKT), "6281022011001");
        Add(MkItem("SNK-PF-SALTISH",   "Peek Freans Saltish 154g",  "SNACKS", null,      PKT), "6281022012001");
        Add(MkItem("SNK-PF-COCOMO",    "Peek Freans Cocomo 112g",   "SNACKS", null,      PKT), "6281022013001");
        Add(MkItem("SNK-LU-OZ80",      "LU Oreo 80g",               "SNACKS", null,      PKT), "7622210453600");
        Add(MkItem("SNK-EBM-BK100",    "EBM Bakeri 100g",           "SNACKS", null,      PKT), "6281017010001");
        Add(MkItem("SNK-EBM-ZF80",     "EBM Zeera Plus 80g",        "SNACKS", null,      PKT), "6281017011001");
        Add(MkItem("SNK-KITKAT-42",    "KitKat 42g",                "SNACKS", "NESTLE",  PCS), "6281014070023");
        Add(MkItem("SNK-KITKAT-170",   "KitKat 170g",               "SNACKS", "NESTLE",  PCS), "6281014070030");
        Add(MkItem("SNK-CADBURY-40",   "Cadbury Dairy Milk 40g",    "SNACKS", null,      PCS), "7622210358370");
        Add(MkItem("SNK-CADBURY-90",   "Cadbury Dairy Milk 90g",    "SNACKS", null,      PCS), "7622210358387");
        Add(MkItem("SNK-TWIX-50",      "Twix 50g",                  "SNACKS", null,      PCS), "5000159461122");
        Add(MkItem("SNK-SNICKERS-50",  "Snickers 50g",              "SNACKS", null,      PCS), "5000159461139");
        Add(MkItem("SNK-MARS-51",      "Mars Bar 51g",              "SNACKS", null,      PCS), "5000159000025");
        Add(MkItem("SNK-BOUNTY-57",    "Bounty 57g",                "SNACKS", null,      PCS), "5000159461146");
        Add(MkItem("SNK-MM-45",        "M&Ms Peanut 45g",           "SNACKS", null,      PKT), "5000159414136");
        Add(MkItem("SNK-DORITOS-OR",   "Doritos Nacho Cheese 40g",  "SNACKS", null,      PKT), "0028400590013");
        Add(MkItem("SNK-DORITOS-CP",   "Doritos Cool Ranch 40g",    "SNACKS", null,      PKT), "0028400590020");
        Add(MkItem("SNK-NUT-KAJU200",  "Cashews Roasted 200g",      "SNACKS", null,      PKT), "6281040001001");
        Add(MkItem("SNK-NUT-ALMD200",  "Almonds 200g",              "SNACKS", null,      PKT), "6281040001018");
        Add(MkItem("SNK-NUT-PISC200",  "Pistachios 200g",           "SNACKS", null,      PKT), "6281040001025");
        Add(MkItem("SNK-NUT-WALNT200", "Walnuts 200g",              "SNACKS", null,      PKT), "6281040001032");
        Add(MkItem("SNK-NUT-MIX200",   "Mixed Nuts 200g",           "SNACKS", null,      PKT), "6281040001049");

        // ====================================================================
        // BAKERY
        // ====================================================================
        Add(MkItem("BAK-BREAD-WH",     "Wonder White Bread 400g",   "BAKERY", null,      PCS), "6281050001001");
        Add(MkItem("BAK-BREAD-BRN",    "Brown Bread 400g",          "BAKERY", null,      PCS), "6281050001018");
        Add(MkItem("BAK-TOAST-WH",     "White Toast 500g",          "BAKERY", null,      PCS), "6281050002001");
        Add(MkItem("BAK-TOAST-BRN",    "Brown Toast 500g",          "BAKERY", null,      PCS), "6281050002018");
        Add(MkItem("BAK-MCVITIES-250", "McVities Digestive 250g",   "BAKERY", null,      PKT), "5000168002981");
        Add(MkItem("BAK-CAKE-CUP6",    "Larosh Cup Cake 6 Pack",    "BAKERY", null,      PKT), "6281050010001");
        Add(MkItem("BAK-CROISSANT-6",  "Croissant 6 Pack",          "BAKERY", null,      PKT), "6281050011001");

        // ====================================================================
        // FROZEN FOODS
        // ====================================================================
        Add(MkItem("FRZ-KNS-CHUNKS",   "KNs Chicken Chunks 400g",   "FROZEN", null,      PKT), "6281060001001");
        Add(MkItem("FRZ-KNS-NUGG",     "KNs Nuggets 400g",          "FROZEN", null,      PKT), "6281060001018");
        Add(MkItem("FRZ-KNS-FNGR",     "KNs Chicken Fingers 400g",  "FROZEN", null,      PKT), "6281060001025");
        Add(MkItem("FRZ-KNS-BRGR",     "KNs Burger Patty 400g",     "FROZEN", null,      PKT), "6281060001032");
        Add(MkItem("FRZ-PIZZA-MARG",   "McCain Margherita Pizza",   "FROZEN", null,      PCS), "6281060010001");
        Add(MkItem("FRZ-FRIES-400",    "McCain French Fries 400g",  "FROZEN", null,      PKT), "6281060011001");
        Add(MkItem("FRZ-FRIES-1KG",    "McCain French Fries 1kg",   "FROZEN", null,      PKT), "6281060011018");
        Add(MkItem("FRZ-PEAS-450",     "Frozen Green Peas 450g",    "FROZEN", null,      PKT), "6281060020001");
        Add(MkItem("FRZ-CORN-450",     "Frozen Sweet Corn 450g",    "FROZEN", null,      PKT), "6281060020018");
        Add(MkItem("FRZ-VEGMIX-450",   "Frozen Veg Mix 450g",       "FROZEN", null,      PKT), "6281060020025");

        // ====================================================================
        // INSTANT / PACKAGED FOOD
        // ====================================================================
        Add(MkItem("INST-MAGGI-80",    "Maggi Noodles Masala 80g",  "FOOD",  "NESTLE",   PKT), "6281014080022");
        Add(MkItem("INST-MAGGI-250",   "Maggi Noodles Family 250g", "FOOD",  "NESTLE",   PKT), "6281014080039");
        Add(MkItem("INST-INDOMIE-80",  "Indomie Goreng 80g",        "FOOD",  null,       PKT), "8886388000173");
        Add(MkItem("INST-2MIN-CKN",    "2 Minute Noodles Chicken",  "FOOD",  null,       PKT), "6281030070001");
        Add(MkItem("INST-OATMEAL-500", "Quaker Oats 500g",          "FOOD",  null,       PKT), "3033710064503");
        Add(MkItem("INST-OATMEAL-1KG", "Quaker Oats 1kg",           "FOOD",  null,       PKT), "3033710064510");
        Add(MkItem("INST-CORNFLK-500", "Kelloggs Cornflakes 500g",  "FOOD",  "KELLOGS",  PKT), "5059319002068");
        Add(MkItem("INST-CORNFLK-1KG", "Kelloggs Cornflakes 1kg",   "FOOD",  "KELLOGS",  PKT), "5059319002075");
        Add(MkItem("INST-COCO-POPS",   "Kelloggs Coco Pops 350g",   "FOOD",  "KELLOGS",  PKT), "5059319002082");
        Add(MkItem("INST-FROSTED-350", "Kelloggs Frosted Flakes 350g","FOOD","KELLOGS",  PKT), "5059319002099");

        // ====================================================================
        // CONDIMENTS / SPICES
        // ====================================================================
        Add(MkItem("COND-HNZ-KTCHP500","Heinz Ketchup 500g",        "FOOD",  null,       PCS), "0000087157050");
        Add(MkItem("COND-HNZ-KTCHP1K", "Heinz Ketchup 1kg",         "FOOD",  null,       PCS), "0000087157067");
        Add(MkItem("COND-NATL-KTCHP",  "National Ketchup 500g",     "FOOD",  null,       PCS), "6281015010001");
        Add(MkItem("COND-MAYO-BEST400","Best Foods Mayo 400g",       "FOOD",  "UNILEVER", PCS), "8712100832277");
        Add(MkItem("COND-MAYO-KFT400", "Kraft Mayo 400g",            "FOOD",  null,       PCS), "0021000029692");
        Add(MkItem("COND-SOY-KIK640",  "Kikkoman Soy Sauce 640ml",  "FOOD",  null,       ML),  "0041390030040");
        Add(MkItem("SPICE-MHRN-BIRYNI","Mehran Biryani Masala 100g","FOOD",  null,       PKT), "6281015020018");
        Add(MkItem("SPICE-MHRN-CKN",   "Mehran Chicken Masala 100g","FOOD",  null,       PKT), "6281015020025");
        Add(MkItem("SPICE-NATL-BIRYNI","National Biryani Masala 100g","FOOD",null,       PKT), "6281015021001");
        Add(MkItem("SPICE-SHAN-BIRYNI","Shan Biryani Mix 60g",      "FOOD",  null,       PKT), "6281015023001");
        Add(MkItem("SPICE-SHAN-SEEKH", "Shan Seekh Kabab Mix 50g",  "FOOD",  null,       PKT), "6281015023018");

        // ====================================================================
        // PERSONAL CARE / HEALTH
        // ====================================================================
        Add(MkItem("PC-SHP-SNSL180",   "Sunsilk Shampoo 180ml",     "HEALTH","UNILEVER", ML),  "6281006050012");
        Add(MkItem("PC-SHP-SNSL380",   "Sunsilk Shampoo 380ml",     "HEALTH","UNILEVER", ML),  "6281006050029");
        Add(MkItem("PC-SHP-PANT180",   "Pantene Shampoo 180ml",     "HEALTH","PG",       ML),  "8001090430632");
        Add(MkItem("PC-SHP-PANT380",   "Pantene Shampoo 380ml",     "HEALTH","PG",       ML),  "8001090430649");
        Add(MkItem("PC-SHP-HNS200",    "Head & Shoulders 200ml",    "HEALTH","PG",       ML),  "8001090674029");
        Add(MkItem("PC-SHP-DOVE200",   "Dove Shampoo 200ml",        "HEALTH","UNILEVER", ML),  "6281006054010");
        Add(MkItem("PC-SHP-DOVE400",   "Dove Shampoo 400ml",        "HEALTH","UNILEVER", ML),  "6281006054027");
        Add(MkItem("PC-SOAP-DOVE100",  "Dove Soap 100g",            "HEALTH","UNILEVER", PCS), "6281006055010");
        Add(MkItem("PC-SOAP-LBY100",   "Lifebuoy Soap 100g",        "HEALTH","UNILEVER", PCS), "6281006056010");
        Add(MkItem("PC-SOAP-SFGRD100", "Safeguard Soap 100g",       "HEALTH","PG",       PCS), "8001090668004");
        Add(MkItem("PC-SOAP-LUX80",    "Lux Soap 80g",              "HEALTH","UNILEVER", PCS), "6281006057010");
        Add(MkItem("PC-HWSH-DTTL250",  "Dettol Handwash 250ml",     "HEALTH",null,       ML),  "6281000250001");
        Add(MkItem("PC-HWSH-DOVE250",  "Dove Handwash 250ml",       "HEALTH","UNILEVER", ML),  "6281006055034");
        Add(MkItem("PC-TOOTHP-CLG50",  "Colgate Toothpaste 50ml",   "HEALTH",null,       PCS), "8714789710297");
        Add(MkItem("PC-TOOTHP-CLG100", "Colgate Toothpaste 100ml",  "HEALTH",null,       PCS), "8714789710303");
        Add(MkItem("PC-TOOTHP-CLG200", "Colgate Toothpaste 200ml",  "HEALTH",null,       PCS), "8714789710310");
        Add(MkItem("PC-TOOTHP-CLOSE",  "Close Up Toothpaste 80g",   "HEALTH","UNILEVER", PCS), "6281006058010");
        Add(MkItem("PC-MWSH-LSTR250",  "Listerine 250ml",           "HEALTH",null,       ML),  "3574661385426");
        Add(MkItem("PC-DEO-AXE150",    "Axe Body Spray 150ml",      "HEALTH","UNILEVER", ML),  "6281006060010");
        Add(MkItem("PC-DEO-RXN150",    "Rexona Deo 150ml",          "HEALTH","UNILEVER", ML),  "6281006060027");
        Add(MkItem("PC-DEO-DOVE150",   "Dove Deodorant 150ml",      "HEALTH","UNILEVER", ML),  "6281006055041");
        Add(MkItem("PC-SKINCR-VAS200", "Vaseline Lotion 200ml",     "HEALTH","UNILEVER", ML),  "8717163528716");
        Add(MkItem("PC-SKINCR-VAS400", "Vaseline Lotion 400ml",     "HEALTH","UNILEVER", ML),  "8717163528723");
        Add(MkItem("PC-SKINCR-NIVEA",  "Nivea Cream 50ml",          "HEALTH",null,       PCS), "4005900036360");
        Add(MkItem("PC-RAZOR-MACH3",   "Gillette Mach3 Razor",      "HEALTH","PG",       PCS), "7702018263677");
        Add(MkItem("PC-SHAVF-GILLT",   "Gillette Shave Foam 200g",  "HEALTH","PG",       PCS), "7702018263691");
        Add(MkItem("PC-FEMHGN-8PK",    "Always Ultra Pads 8pk",     "HEALTH","PG",       PKT), "8700216004268");
        Add(MkItem("PC-FEMHGN-20PK",   "Always Ultra Pads 20pk",    "HEALTH","PG",       PKT), "8700216004275");
        Add(MkItem("PC-COTTON-100",    "Cotton Pads 100 Pack",      "HEALTH",null,       PKT), "6281070001001");

        // ====================================================================
        // CLEANING & HOUSEHOLD
        // ====================================================================
        Add(MkItem("CLN-SURF-1KG",     "Surf Excel 1kg",            "FOOD",  "UNILEVER", PKT), "6281006050019");
        Add(MkItem("CLN-SURF-3KG",     "Surf Excel 3kg",            "FOOD",  "UNILEVER", PKT), "6281006050026");
        Add(MkItem("CLN-ARIEL-1KG",    "Ariel 1kg",                 "FOOD",  "PG",       PKT), "8001841004532");
        Add(MkItem("CLN-BONUS-1KG",    "Bonus Washing Powder 1kg",  "FOOD",  null,       PKT), "6281010001001");
        Add(MkItem("CLN-VIM-1KG",      "Vim Dishwash 1kg",          "FOOD",  "UNILEVER", PCS), "6281006050040");
        Add(MkItem("CLN-DAWN-475",     "Dawn Dishwash 475ml",       "FOOD",  "PG",       ML),  "8001841004563");
        Add(MkItem("CLN-DOMEX-1L",     "Domex Floor Cleaner 1L",    "FOOD",  "UNILEVER", L),   "6281006050064");
        Add(MkItem("CLN-HARPIC-500",   "Harpic Toilet Cleaner 500ml","FOOD", null,       ML),  "5000204065824");
        Add(MkItem("CLN-TISU-KL100",   "Kleenex Tissue 100 Sheets", "FOOD",  null,       PKT), "5901478001001");
        Add(MkItem("CLN-SFTX-200",     "Softex Tissue 200 Pull",    "FOOD",  null,       PCS), "6281020001001");
        Add(MkItem("CLN-TOILROLL-6",   "Toilet Roll 6 Pack",        "FOOD",  null,       PKT), "6281020002018");
        Add(MkItem("CLN-TOILROLL-12",  "Toilet Roll 12 Pack",       "FOOD",  null,       PKT), "6281020002025");
        Add(MkItem("CLN-KTCHTOW-1",    "Kitchen Towel Roll 1 Pack", "FOOD",  null,       PCS), "6281020003001");
        Add(MkItem("CLN-INSECT-MORT",  "Mortein Spray 300ml",       "FOOD",  null,       ML),  "4002448069097");
        Add(MkItem("CLN-INSECT-COIL",  "Mortein Coils 10 Pack",     "FOOD",  null,       PKT), "4002448069103");
        Add(MkItem("CLN-BAGS-30L",     "Trash Bags 30L 20 pcs",     "FOOD",  null,       PKT), "6281020010001");
        Add(MkItem("CLN-ALUMFOIL-10",  "Aluminum Foil 10m",         "FOOD",  null,       PCS), "6281020011001");
        Add(MkItem("CLN-CLWRP-20M",    "Cling Wrap 20m",            "FOOD",  null,       PCS), "6281020012001");

        // ====================================================================
        // ELECTRONICS
        // ====================================================================
        Add(MkItem("ELEC-SAM-A15",     "Samsung Galaxy A15 128GB",  "PHONES","SAMSUNG",  PCS), "8806095076133");
        Add(MkItem("ELEC-SAM-A35",     "Samsung Galaxy A35 256GB",  "PHONES","SAMSUNG",  PCS), "8806095076140");
        Add(MkItem("ELEC-SAM-A55",     "Samsung Galaxy A55 256GB",  "PHONES","SAMSUNG",  PCS), "8806095076157");
        Add(MkItem("ELEC-SAM-S24",     "Samsung Galaxy S24 256GB",  "PHONES","SAMSUNG",  PCS), "8806095076164");
        Add(MkItem("ELEC-IPHONE15",    "Apple iPhone 15 128GB",     "PHONES","APPLE",    PCS), "0195949047428");
        Add(MkItem("ELEC-IPHONE15P",   "Apple iPhone 15 Pro 256GB", "PHONES","APPLE",    PCS), "0195949047435");
        Add(MkItem("ELEC-BUDS-SAM",    "Samsung Galaxy Buds FE",    "AUDIO", "SAMSUNG",  PCS), "8806095076171");
        Add(MkItem("ELEC-AIRPODS2",    "Apple AirPods 2nd Gen",     "AUDIO", "APPLE",    PCS), "0194252969960");
        Add(MkItem("ELEC-WATCH-SAM4",  "Samsung Galaxy Watch 4",    "WEARABLES","SAMSUNG",PCS),"8806095076188");
        Add(MkItem("ELEC-WATCH-AW9",   "Apple Watch Series 9",      "WEARABLES","APPLE", PCS), "0194253410553");
        Add(MkItem("ELEC-TAB-SAMA8",   "Samsung Galaxy Tab A8",     "TABLETS","SAMSUNG", PCS), "8806094215915");
        Add(MkItem("ELEC-IPAD10",      "Apple iPad 10th Gen",       "TABLETS","APPLE",   PCS), "0194253381120");
        Add(MkItem("ELEC-DELL-INS15",  "Dell Inspiron 15 Core i5",  "LAPTOPS","DELL",    PCS), "5397184753957");
        Add(MkItem("ELEC-HP-15S",      "HP 15s Core i5 512GB",      "LAPTOPS","HP",      PCS), "0196548718048");
        Add(MkItem("ELEC-LNV-SLIM3",   "Lenovo IdeaPad Slim 3",     "LAPTOPS","LENOVO",  PCS), "0196379756249");
        Add(MkItem("ELEC-LG-TV43",     "LG 43 inch 4K Smart TV",    "TV",    "LG",       PCS), "8806091574046");
        Add(MkItem("ELEC-SAM-TV50",    "Samsung 50 inch Crystal UHD","TV",   "SAMSUNG",  PCS), "8806092781726");
        Add(MkItem("ELEC-CHARGER-65W", "USB-C 65W Fast Charger",    "ACCELS",null,       PCS), "6281080001001");
        Add(MkItem("ELEC-CBL-USBC2M",  "USB-C Cable 2m",            "ACCELS",null,       PCS), "6281080001018");
        Add(MkItem("ELEC-CBL-LGHT2M",  "Lightning Cable 2m",        "ACCELS",null,       PCS), "6281080001025");
        Add(MkItem("ELEC-PWRBK-20K",   "Anker 20000mAh Power Bank", "ACCELS",null,       PCS), "0848061080156");
        Add(MkItem("ELEC-PWRBK-10K",   "Anker 10000mAh Power Bank", "ACCELS",null,       PCS), "0848061080163");
        Add(MkItem("ELEC-BATT-AA4",    "Duracell AA x4 Pack",       "ACCELS",null,       PKT), "5000394052390");
        Add(MkItem("ELEC-BATT-AAA4",   "Duracell AAA x4 Pack",      "ACCELS",null,       PKT), "5000394052406");
        Add(MkItem("ELEC-JBL-FLIP6",   "JBL Flip 6 Speaker",        "AUDIO", null,       PCS), "0050036388619");
        Add(MkItem("ELEC-SONY-WH520",  "Sony WH-CH520 Headphones",  "AUDIO", "SONY",     PCS), "4548736135536");

        // ====================================================================
        // HEALTH / OTC MEDICINES
        // ====================================================================
        Add(MkItem("MED-PANADOL-10",   "Panadol Tablets 10 Pack",   "HEALTH",null,       PCS), "6281000100101");
        Add(MkItem("MED-PANADOL-30",   "Panadol Tablets 30 Pack",   "HEALTH",null,       PCS), "6281000100118");
        Add(MkItem("MED-BRUFEN-10",    "Brufen 400mg 10 Tabs",      "HEALTH",null,       PCS), "6281000100125");
        Add(MkItem("MED-DISPRIN-10",   "Disprin 300mg 10 Tabs",     "HEALTH",null,       PCS), "6281000100149");
        Add(MkItem("MED-ORS-SACHET",   "ORS Sachet Lemon",          "HEALTH",null,       PCS), "6281000100156");
        Add(MkItem("MED-BANDAID-20",   "Band-Aid 20 Strip Pack",    "HEALTH",null,       PKT), "0381370044534");
        Add(MkItem("MED-ANTISEP-100",  "Dettol Antiseptic 100ml",   "HEALTH",null,       ML),  "6281000250018");
        Add(MkItem("MED-THERMO",       "Digital Thermometer",       "HEALTH",null,       PCS), "6281000300001");
        Add(MkItem("MED-BPMON",        "Digital BP Monitor",        "HEALTH",null,       PCS), "6281000300018");
        Add(MkItem("MED-MASK-3PLY50",  "3-Ply Surgical Mask 50pk",  "HEALTH",null,       PKT), "6281000300032");
        Add(MkItem("MED-VITC-20TB",    "Vitamin C 500mg 20 Tabs",   "HEALTH",null,       PCS), "6281000300049");

        // ====================================================================
        // BABY CARE
        // ====================================================================
        Add(MkItem("BABY-DIAP-PAMP-S", "Pampers Small 42 Pcs",      "HEALTH","PG",       PKT), "8001090797155");
        Add(MkItem("BABY-DIAP-PAMP-M", "Pampers Medium 38 Pcs",     "HEALTH","PG",       PKT), "8001090797162");
        Add(MkItem("BABY-DIAP-PAMP-L", "Pampers Large 34 Pcs",      "HEALTH","PG",       PKT), "8001090797179");
        Add(MkItem("BABY-WIPES-JNJ80", "Johnsons Wipes 80 Pack",    "HEALTH",null,       PKT), "3574661001001");
        Add(MkItem("BABY-WIPES-HGS80", "Huggies Wipes 80 Pack",     "HEALTH",null,       PKT), "5029053568782");
        Add(MkItem("BABY-SHP-JNJ200",  "Johnsons Baby Shampoo 200ml","HEALTH",null,      ML),  "3574661002001");
        Add(MkItem("BABY-LOT-JNJ200",  "Johnsons Baby Lotion 200ml","HEALTH",null,       ML),  "3574661003001");
        Add(MkItem("BABY-PWD-JNJ200",  "Johnsons Baby Powder 200g", "HEALTH",null,       PCS), "3574661004001");
        Add(MkItem("BABY-NAN1-400",    "Nestle NAN Pro 1 400g",     "HEALTH","NESTLE",   PCS), "6281014090021");
        Add(MkItem("BABY-NAN2-400",    "Nestle NAN Pro 2 400g",     "HEALTH","NESTLE",   PCS), "6281014090038");
        Add(MkItem("BABY-CERELAC-250", "Nestle Cerelac Rice 250g",  "HEALTH","NESTLE",   PKT), "6281014091001");

        // ====================================================================
        // TOOLS & HARDWARE
        // ====================================================================
        Add(MkItem("TOOL-SCRWDRVR",    "Screwdriver Set 12 pcs",    "TOOLS", null,       PCS), "6281100001001");
        Add(MkItem("TOOL-HAMMER",      "Hammer 300g",               "TOOLS", null,       PCS), "6281100002001");
        Add(MkItem("TOOL-WRENCH",      "Adjustable Wrench 10 inch", "TOOLS", null,       PCS), "6281100003001");
        Add(MkItem("TOOL-PLIERS",      "Pliers Set 3 Piece",        "TOOLS", null,       PCS), "6281100004001");
        Add(MkItem("TOOL-TAPEMEAS",    "Tape Measure 5m",           "TOOLS", null,       PCS), "6281100005001");
        Add(MkItem("TOOL-DRILL-BSCH",  "Bosch Drill GSB 13RE",      "TOOLS", "BOSCH",    PCS), "3165140351881");
        Add(MkItem("TOOL-GLUEGUN",     "Glue Gun 100W",             "TOOLS", null,       PCS), "6281100006001");
        Add(MkItem("TOOL-PADLOCK",     "Padlock 40mm",              "TOOLS", null,       PCS), "6281100009001");
        Add(MkItem("TOOL-TORCH",       "LED Torch 500 Lumens",      "TOOLS", null,       PCS), "6281100010001");
        Add(MkItem("TOOL-EXTNCORD3M",  "Extension Cord 3m 4 Socket","TOOLS", null,       PCS), "6281100011001");
        Add(MkItem("TOOL-3M-TAPE",     "3M Double Sided Tape",      "TOOLS", "3M",       PCS), "0051131975408");

        // ====================================================================
        // STATIONERY / OFFICE
        // ====================================================================
        Add(MkItem("STAT-PEN-BIC-BK",  "BIC Ballpoint Pen Black",   "OFFICE-EQ",null,   PCS), "0070330304015");
        Add(MkItem("STAT-PEN-BIC-BL",  "BIC Ballpoint Pen Blue",    "OFFICE-EQ",null,   PCS), "0070330304022");
        Add(MkItem("STAT-PEN-BOX12",   "BIC Pens 12 Box",           "OFFICE-EQ",null,   BOX), "0070330304039");
        Add(MkItem("STAT-PENCIL-12",   "Pencils HB 12 Pack",        "OFFICE-EQ",null,   PKT), "6281095001001");
        Add(MkItem("STAT-NBOOK-A4",    "A4 Notebook 200 Pages",     "OFFICE-EQ",null,   PCS), "6281095002001");
        Add(MkItem("STAT-PRNPPR-A4",   "A4 Copier Paper 500 Sheets","OFFICE-EQ",null,   PKT), "6281095004001");
        Add(MkItem("STAT-STICKYNOTE",  "Post-It Sticky Notes 100",  "OFFICE-EQ",null,   PKT), "0051141380215");
        Add(MkItem("STAT-TAPE-12MM",   "Scotch Tape 12mm x 33m",    "OFFICE-EQ",null,   PCS), "0051141046119");
        Add(MkItem("STAT-SCISSORS",    "Scissors 7 inch",           "OFFICE-EQ",null,   PCS), "6281095007001");
        Add(MkItem("STAT-ARCHFILE",    "Arch Lever File A4",        "OFFICE-EQ",null,   PCS), "6281095008001");
        Add(MkItem("STAT-CALC-CASIO",  "Casio Calculator 12 Digit", "OFFICE-EQ",null,   PCS), "4549526612589");

        // ====================================================================
        // SPORTS & FITNESS
        // ====================================================================
        Add(MkItem("SPT-PROTEIN-1KG",  "Optimum Nutrition Whey 1kg","SPORTS",null,       PCS), "0748927025224");
        Add(MkItem("SPT-YOGA-MAT",     "Yoga Mat 6mm",              "SPORTS",null,       PCS), "6281098010001");
        Add(MkItem("SPT-JUMP-ROPE",    "Jump Rope Adjustable",      "SPORTS",null,       PCS), "6281098011001");
        Add(MkItem("SPT-DUMBBELL-5",   "Dumbbells 5kg Pair",        "SPORTS",null,       PCS), "6281098012001");
        Add(MkItem("SPT-DUMBBELL-10",  "Dumbbells 10kg Pair",       "SPORTS",null,       PCS), "6281098012018");
        Add(MkItem("SPT-FOOTBALL",     "Football Size 5",           "SPORTS","ADIDAS",   PCS), "4060509157649");
        Add(MkItem("SPT-CRICBAT-JR",   "Cricket Bat Junior",        "SPORTS",null,       PCS), "6281098002001");
        Add(MkItem("SPT-NIKE-42",      "Nike Air Max Size 42",      "FOOTWEAR","NIKE",   PCS), "0885178932274");
        Add(MkItem("SPT-ADIDAS-42",    "Adidas Ultraboost Size 42", "FOOTWEAR","ADIDAS", PCS), "4065432655521");
        Add(MkItem("SPT-PUMA-42",      "Puma RS-X Size 42",         "FOOTWEAR","PUMA",   PCS), "4060981726476");

        // ====================================================================
        // KITCHEN
        // ====================================================================
        Add(MkItem("KIT-KETTLE-1L7",   "Electric Kettle 1.7L",      "KITCHEN",null,      PCS), "6281110005001");
        Add(MkItem("KIT-TOASTER-2SLC", "Toaster 2 Slice",           "KITCHEN",null,      PCS), "6281110006001");
        Add(MkItem("KIT-BLENDER",      "Blender 1.5L",              "KITCHEN",null,      PCS), "6281110007001");
        Add(MkItem("KIT-PRESSCOOK-6L", "Pressure Cooker 6L",        "KITCHEN",null,      PCS), "6281110008001");
        Add(MkItem("KIT-KNIFE-SET5",   "Kitchen Knife Set 5 Pcs",   "KITCHEN",null,      PCS), "6281110009001");
        Add(MkItem("KIT-FRYPAN-28",    "Non-Stick Frying Pan 28cm", "KITCHEN",null,      PCS), "6281110004001");
        Add(MkItem("KIT-POT-3L",       "Stainless Steel Pot 3L",    "KITCHEN",null,      PCS), "6281110003001");
        Add(MkItem("KIT-STORAGE-3",    "Food Storage Box Set 3",    "KITCHEN",null,      PCS), "6281110011001");

        // ====================================================================
        // AUTOMOTIVE
        // ====================================================================
        Add(MkItem("AUTO-OIL-CAS1L",   "Castrol GTX 20W50 1L",      "AUTO",  null,       L),   "6281090001001");
        Add(MkItem("AUTO-OIL-CAS4L",   "Castrol GTX 20W50 4L",      "AUTO",  null,       L),   "6281090001018");
        Add(MkItem("AUTO-AIRFRSH",     "Car Air Freshener Hanging",  "AUTO",  null,       PCS), "6281090002001");
        Add(MkItem("AUTO-CARWASH",     "Car Wash Shampoo 500ml",     "AUTO",  null,       ML),  "6281090003001");
        Add(MkItem("AUTO-WIPER-20",    "Wiper Blade 20 inch",        "AUTO",  null,       PCS), "6281090004001");
        Add(MkItem("AUTO-POLWAX",      "Car Polish Wax 300g",        "AUTO",  null,       PCS), "6281090006001");

        // ====================================================================
        // TOYS
        // ====================================================================
        Add(MkItem("TOY-PLAYDOH-6",    "Play-Doh 6 Colour Set",     "TOYS",  null,       PCS), "5010994971564");
        Add(MkItem("TOY-PUZZLE-100",   "100 Piece Puzzle",          "TOYS",  null,       PCS), "6281098001001");
        Add(MkItem("TOY-UNO-CARD",     "UNO Card Game",             "TOYS",  null,       PCS), "0746775310622");
        Add(MkItem("TOY-COLRPNCL-24",  "Crayola Pencils 24 Colours","TOYS",  null,       BOX), "0071662030247");

        // ====================================================================
        // PET SUPPLIES
        // ====================================================================
        Add(MkItem("PET-PEDIGREE-1KG", "Pedigree Dog Food 1kg",     "FOOD",  null,       PKT), "9310036157413");
        Add(MkItem("PET-PEDIGREE-3KG", "Pedigree Dog Food 3kg",     "FOOD",  null,       PKT), "9310036157420");
        Add(MkItem("PET-WHISKAS-400",  "Whiskas Cat Food 400g",     "FOOD",  null,       PCS), "9310036160498");
        Add(MkItem("PET-LITTER-5KG",   "Cat Litter 5kg",            "FOOD",  null,       PKT), "6281120002001");

        // ====================================================================
        // MANUFACTURING — Finished Goods & Raw Materials
        // IDs are derived per-company using CrossModuleGuid.Derive so that
        // every company gets unique IDs (multi-tenant safe) while Manufacturing
        // seed independently computes the same IDs without querying Inventory.
        // Sequence map must stay in sync with ManufacturingSeedDataService.cs.
        // ====================================================================
        Guid MTR = units.First(u => u.Code == "MTR").Id;
        Guid LTR = units.First(u => u.Code == "LTR").Id;

        Item MkMfgItem(int seq, string code, string name, string catCode,
            Guid unitId, string itemType, string shortDesc) =>
            new()
            {
                Id                  = CrossModuleGuid.Derive(T.companyId, seq),
                CompanyId           = T.companyId,
                BranchId            = T.branchId,
                BusinessUnitId      = T.businessUnitId,
                CreatedByUserId     = T.userId,
                CreatedAt           = now,
                Code                = code,
                Name                = name,
                ItemType            = itemType,
                ShortDescription    = shortDesc,
                CategoryId          = CatId(catCode),
                BaseUnitId          = unitId,
                CostingMethod       = "Standard",
                IsActive            = true,
                IsPublished         = false,
                Condition           = "New",
                AlertOnLowStock     = true,
                ReorderLevel        = 5,
            };

        void AddMfg(Item item, string barcode)
        {
            itemList.Add(item);
            AddBarcode(item.Id, barcode);
        }

        // Finished Goods (seq 1001–1005)
        AddMfg(MkMfgItem(1001, "MFG-FG-CHAIR",     "Executive Ergonomic Chair",        "FINISHED-GOODS", PCS, "FinishedGood", "Adjustable lumbar support, mesh back, 5-caster base"),        "6281099000001");
        AddMfg(MkMfgItem(1002, "MFG-FG-DESK",      "Height-Adjustable Standing Desk",  "FINISHED-GOODS", PCS, "FinishedGood", "Electric height adjustment, solid wood top, steel frame"),    "6281099000002");
        AddMfg(MkMfgItem(1003, "MFG-FG-CABINET",   "4-Drawer Steel Filing Cabinet",    "FINISHED-GOODS", PCS, "FinishedGood", "A4 lateral filing, central lock, powder-coated finish"),      "6281099000003");
        AddMfg(MkMfgItem(1004, "MFG-FG-BOOKSHELF", "3-Tier Steel Bookshelf",           "FINISHED-GOODS", PCS, "FinishedGood", "Heavy-duty, adjustable shelves, 120kg load capacity"),        "6281099000004");
        AddMfg(MkMfgItem(1005, "MFG-FG-TABLE",     "6-Seater Meeting Room Table",      "FINISHED-GOODS", PCS, "FinishedGood", "Solid wood top, powder-coated steel legs, cable management"), "6281099000005");

        // Raw Materials (seq 1101–1112)
        AddMfg(MkMfgItem(1101, "MFG-RM-STEEL-TUBE",     "Steel Structural Tube 40×40mm",      "RAW-MAT", PCS, "RawMaterial", "Cold-formed hollow section, 6m length, 2mm wall thickness"),      "6281099001001");
        AddMfg(MkMfgItem(1102, "MFG-RM-FABRIC",         "Upholstery Mesh Fabric",             "RAW-MAT", MTR, "RawMaterial", "Breathable mesh, 1500mm wide, 250g/m², black/grey"),               "6281099001002");
        AddMfg(MkMfgItem(1103, "MFG-RM-FOAM",           "High-Density Seat Foam",             "RAW-MAT", KG,  "RawMaterial", "HR45 density foam, 50mm thickness, cut-to-size"),                  "6281099001003");
        AddMfg(MkMfgItem(1104, "MFG-RM-WOOD-PANEL",     "Solid Rubberwood Panel 1200×600",    "RAW-MAT", PCS, "RawMaterial", "18mm solid rubberwood, sanded, ready for lacquer"),                "6281099001004");
        AddMfg(MkMfgItem(1105, "MFG-RM-SCREWS",         "Screws & Fasteners Pack",            "RAW-MAT", PKT, "RawMaterial", "M6×16 hex bolts + M8 barrel nuts, 50-piece assorted"),             "6281099001005");
        AddMfg(MkMfgItem(1106, "MFG-RM-PAINT",          "Powder Coat Paint (RAL 9005)",       "RAW-MAT", LTR, "RawMaterial", "Epoxy-polyester, textured black, electrostatic application"),       "6281099001006");
        AddMfg(MkMfgItem(1107, "MFG-RM-MDF",            "MDF Board 18mm",                     "RAW-MAT", PCS, "RawMaterial", "2440×1220mm, E1 emission class, moisture-resistant"),              "6281099001007");
        AddMfg(MkMfgItem(1108, "MFG-RM-HANDLES",        "Drawer Handle Set",                  "RAW-MAT", PCS, "RawMaterial", "Brushed stainless, 128mm hole centres, includes M4 screws"),        "6281099001008");
        AddMfg(MkMfgItem(1109, "MFG-RM-STEEL-SHEET",    "Cold-Rolled Steel Sheet 1.5mm",      "RAW-MAT", PCS, "RawMaterial", "2000×1000mm CR4 grade, oiled surface, decoiled"),                  "6281099001009");
        AddMfg(MkMfgItem(1110, "MFG-RM-GAS-LIFT",       "Pneumatic Gas Lift Cylinder Class 4","RAW-MAT", PCS, "RawMaterial", "100mm stroke, 135kg rated, SGS certified"),                         "6281099001010");
        AddMfg(MkMfgItem(1111, "MFG-RM-CASTERS",        "Twin-Wheel Caster Set (5-pack)",     "RAW-MAT", PCS, "RawMaterial", "65mm PU wheels, 11mm stem, 50kg each, floor-safe"),                 "6281099001011");
        AddMfg(MkMfgItem(1112, "MFG-RM-PARTICLE-BOARD", "Particle Board 16mm (Shelving)",     "RAW-MAT", PCS, "RawMaterial", "2440×1220mm, melamine-faced white, V313 moisture class"),           "6281099001012");

        // ====================================================================
        // FAST-FOOD (Pizza shop) — Raw Materials + Made-to-order Menu
        // IDs are derived per-company via CrossModuleGuid.Derive so Manufacturing
        // can build BOMs/production orders against the same item IDs without
        // querying Inventory, and so the Sales combo promotion can target them.
        // Seq map (must stay in sync with ManufacturingSeedDataService.cs):
        //   Food raw materials : 1201–1230
        //   Menu finished goods: 1301–1340
        //   Family Deal SKU    : 1350
        // ====================================================================

        // Sellable, GL-bearing, published finished menu item (deterministic Id).
        Item MkMenuItem(int seq, string code, string name, string catCode,
            Guid unitId, string? shortDesc = null) =>
            new()
            {
                Id                  = CrossModuleGuid.Derive(T.companyId, seq),
                CompanyId           = T.companyId,
                BranchId            = T.branchId,
                BusinessUnitId      = T.businessUnitId,
                CreatedByUserId     = T.userId,
                CreatedAt           = now,
                Code                = code,
                Name                = name,
                ItemType            = "FinishedGood",
                ShortDescription    = shortDesc,
                CategoryId          = CatId(catCode),
                BaseUnitId          = unitId,
                CostingMethod       = "MovingAverage",
                IsActive            = true,
                IsPublished         = true,
                IsFeatured          = true,
                Condition           = "New",
                AlertOnLowStock     = true,
                ReorderLevel        = 10,
                InventoryAccountId  = gl.Inventory,
                CogsAccountId       = gl.Cogs,
                PurchaseAccountId   = gl.Purchase,
                SalesAccountId      = gl.Sales,
            };

        // ── Food Raw Materials (RAW-MAT, RawMaterial) — seq 1201–1230 ────────
        AddMfg(MkMfgItem(1201, "FF-RM-DOUGH",      "Pizza Dough Ball 250g",       "RAW-MAT", PCS, "RawMaterial", "Fresh proofed dough ball, hand-stretched to base"), "6281099002001");
        AddMfg(MkMfgItem(1202, "FF-RM-MOZZ",       "Mozzarella Cheese",           "RAW-MAT", KG,  "RawMaterial", "Shredded mozzarella for pizza topping"),            "6281099002002");
        AddMfg(MkMfgItem(1203, "FF-RM-PIZZA-SAUCE","Pizza Tomato Sauce",          "RAW-MAT", L,   "RawMaterial", "Seasoned tomato pizza base sauce"),                 "6281099002003");
        AddMfg(MkMfgItem(1204, "FF-RM-PEPPERONI",  "Pepperoni Topping",           "RAW-MAT", KG,  "RawMaterial", "Sliced beef pepperoni"),                            "6281099002004");
        AddMfg(MkMfgItem(1205, "FF-RM-CHK-TIKKA",  "Chicken Tikka Topping",       "RAW-MAT", KG,  "RawMaterial", "Marinated grilled chicken tikka chunks"),           "6281099002005");
        AddMfg(MkMfgItem(1206, "FF-RM-VEG-MIX",    "Veg Topping Mix",             "RAW-MAT", KG,  "RawMaterial", "Onion, capsicum, olives & sweetcorn mix"),          "6281099002006");
        AddMfg(MkMfgItem(1207, "FF-RM-BEEF-PATTY", "Beef Patty 150g",             "RAW-MAT", PCS, "RawMaterial", "Seasoned beef burger patty"),                       "6281099002007");
        AddMfg(MkMfgItem(1208, "FF-RM-CHK-FILLET", "Chicken Fillet Patty 150g",   "RAW-MAT", PCS, "RawMaterial", "Breaded crispy chicken fillet"),                    "6281099002008");
        AddMfg(MkMfgItem(1209, "FF-RM-BUN",        "Sesame Burger Bun",           "RAW-MAT", PCS, "RawMaterial", "Toasted sesame-seed burger bun"),                   "6281099002009");
        AddMfg(MkMfgItem(1210, "FF-RM-CHEDDAR",    "Cheddar Cheese Slice",        "RAW-MAT", PCS, "RawMaterial", "Processed cheddar slice"),                          "6281099002010");
        AddMfg(MkMfgItem(1211, "FF-RM-LETTUCE",    "Iceberg Lettuce",             "RAW-MAT", KG,  "RawMaterial", "Fresh shredded iceberg lettuce"),                   "6281099002011");
        AddMfg(MkMfgItem(1212, "FF-RM-TOMATO",     "Fresh Tomato",                "RAW-MAT", KG,  "RawMaterial", "Sliced fresh tomato"),                              "6281099002012");
        AddMfg(MkMfgItem(1213, "FF-RM-MAYO",       "Burger Sauce / Mayo",         "RAW-MAT", L,   "RawMaterial", "Signature burger mayo sauce"),                      "6281099002013");
        AddMfg(MkMfgItem(1214, "FF-RM-POTATO",     "Potato (Fry Cut)",            "RAW-MAT", KG,  "RawMaterial", "Par-cut fries-grade potato"),                       "6281099002014");
        AddMfg(MkMfgItem(1215, "FF-RM-FRY-OIL",    "Frying Oil",                  "RAW-MAT", L,   "RawMaterial", "Vegetable frying oil"),                             "6281099002015");
        AddMfg(MkMfgItem(1216, "FF-RM-PIZZA-BOX",  "Pizza Box",                   "RAW-MAT", PCS, "RawMaterial", "Corrugated pizza takeaway box"),                    "6281099002016");
        AddMfg(MkMfgItem(1217, "FF-RM-BURGER-WRAP","Burger Wrap / Box",           "RAW-MAT", PCS, "RawMaterial", "Burger foil wrap / box"),                           "6281099002017");
        AddMfg(MkMfgItem(1218, "FF-RM-FRIES-CUP",  "Fries Cup",                   "RAW-MAT", PCS, "RawMaterial", "Takeaway fries cup"),                               "6281099002018");

        // ── Pizzas (FAST-FOOD, FinishedGood) — seq 1301–1320 ─────────────────
        AddMfg(MkMenuItem(1301, "FF-PIZZA-MARG-S",  "Margherita Pizza (Small)",     "FAST-FOOD", PCS, "Classic cheese & tomato — 7 inch"),   "6281099003001");
        AddMfg(MkMenuItem(1302, "FF-PIZZA-MARG-M",  "Margherita Pizza (Medium)",    "FAST-FOOD", PCS, "Classic cheese & tomato — 9 inch"),   "6281099003002");
        AddMfg(MkMenuItem(1303, "FF-PIZZA-MARG-L",  "Margherita Pizza (Large)",     "FAST-FOOD", PCS, "Classic cheese & tomato — 12 inch"),  "6281099003003");
        AddMfg(MkMenuItem(1304, "FF-PIZZA-PEP-S",   "Pepperoni Pizza (Small)",      "FAST-FOOD", PCS, "Loaded beef pepperoni — 7 inch"),     "6281099003004");
        AddMfg(MkMenuItem(1305, "FF-PIZZA-PEP-M",   "Pepperoni Pizza (Medium)",     "FAST-FOOD", PCS, "Loaded beef pepperoni — 9 inch"),     "6281099003005");
        AddMfg(MkMenuItem(1306, "FF-PIZZA-PEP-L",   "Pepperoni Pizza (Large)",      "FAST-FOOD", PCS, "Loaded beef pepperoni — 12 inch"),    "6281099003006");
        AddMfg(MkMenuItem(1307, "FF-PIZZA-TIKKA-S", "Chicken Tikka Pizza (Small)",  "FAST-FOOD", PCS, "Spicy chicken tikka — 7 inch"),       "6281099003007");
        AddMfg(MkMenuItem(1308, "FF-PIZZA-TIKKA-M", "Chicken Tikka Pizza (Medium)", "FAST-FOOD", PCS, "Spicy chicken tikka — 9 inch"),       "6281099003008");
        AddMfg(MkMenuItem(1309, "FF-PIZZA-TIKKA-L", "Chicken Tikka Pizza (Large)",  "FAST-FOOD", PCS, "Spicy chicken tikka — 12 inch"),      "6281099003009");

        // ── Burgers (FAST-FOOD, FinishedGood) — seq 1321–1330 ────────────────
        AddMfg(MkMenuItem(1321, "FF-BURGER-BEEF",   "Beef Burger",                  "FAST-FOOD", PCS, "Beef patty, cheese, lettuce & tomato"),  "6281099003021");
        AddMfg(MkMenuItem(1322, "FF-BURGER-ZINGER", "Chicken Zinger Burger",        "FAST-FOOD", PCS, "Crispy chicken fillet, lettuce & mayo"), "6281099003022");
        AddMfg(MkMenuItem(1323, "FF-BURGER-CHEESE", "Cheese Burger",                "FAST-FOOD", PCS, "Double cheese beef burger"),             "6281099003023");

        // ── Fries / Sides (FAST-FOOD, FinishedGood) — seq 1331–1340 ──────────
        AddMfg(MkMenuItem(1331, "FF-FRIES-REG",     "French Fries (Regular)",       "FAST-FOOD", PCS, "Crispy salted fries — regular"),  "6281099003031");
        AddMfg(MkMenuItem(1332, "FF-FRIES-LRG",     "French Fries (Large)",         "FAST-FOOD", PCS, "Crispy salted fries — large"),    "6281099003032");

        // ── Combo Deal SKU (COMBO-DEALS, sold as one fixed-price line) ───────
        AddMfg(MkMenuItem(1350, "FF-DEAL-FAMILY",   "Family Deal (Pizza+Burger+Fries+Drink)", "COMBO-DEALS", PCS, "Medium pizza + burger + large fries + 1.5L drink"), "6281099003050");

        // ── Fast-food menu photos ────────────────────────────────────────────
        // Standalone pass: attach product images to the seeded pizza-shop menu
        // items only. The shared Add/AddMfg helpers are deliberately left untouched.
        foreach (var ffItem in itemList.Where(i =>
                     i.Code.StartsWith("FF-", StringComparison.Ordinal) &&
                     itemImageMap.ContainsKey(i.Code)))
        {
            AddImage(ffItem.Id, ffItem.Code, ffItem.Name);
        }

        return (itemList.ToArray(), bcList.ToArray(), imgList.ToArray());
    }
}
