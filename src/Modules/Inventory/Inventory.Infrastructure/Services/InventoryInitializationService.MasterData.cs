using Inventory.Domain.Constants;
using Inventory.Domain.Entities;
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Constants;
using Nexcore.SharedKernel.Events;
using Nexcore.SharedKernel.Helpers;
using InvEnums = Inventory.Domain.Enums;

namespace Inventory.Infrastructure.Services;

/// <summary>
/// Seeds master-data lookups: Units, ItemCategories, Warehouses, Bins,
/// Brands, Colors, Sizes, AttributeDefinitions, TaxDefinitions,
/// and item-level detail records (ItemAttributes, ItemColors, ItemSizes).
/// </summary>
public partial class InventoryInitializationService
{
    // helper - stamps audit / tenant fields onto any BaseEntity

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

    // 1. Units of Measure

    private static Unit[] SeedUnits(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T) =>
    [
        E(new Unit { Code = "PCS",  Name = "Pieces",       Description = "pcs",  DisplayOrder = 1  }, T),
        E(new Unit { Code = "KG",   Name = "Kilogram",      Description = "kg",   DisplayOrder = 2  }, T),
        E(new Unit { Code = "G",    Name = "Gram",          Description = "g",    DisplayOrder = 3  }, T),
        E(new Unit { Code = "L",    Name = "Litre",         Description = "l",    DisplayOrder = 4  }, T),
        E(new Unit { Code = "ML",   Name = "Millilitre",    Description = "ml",   DisplayOrder = 5  }, T),
        E(new Unit { Code = "LTR",  Name = "Litre (alt)",   Description = "ltr",  DisplayOrder = 6  }, T),
        E(new Unit { Code = "MTR",  Name = "Metre",         Description = "mtr",  DisplayOrder = 7  }, T),
        E(new Unit { Code = "BOX",  Name = "Box",           Description = "box",  DisplayOrder = 8  }, T),
        E(new Unit { Code = "CTN",  Name = "Carton",        Description = "ctn",  DisplayOrder = 9  }, T),
        E(new Unit { Code = "DOZ",  Name = "Dozen",         Description = "doz",  DisplayOrder = 10 }, T),
        E(new Unit { Code = "PKT",  Name = "Packet",        Description = "pkt",  DisplayOrder = 11 }, T),
        E(new Unit { Code = "SET",  Name = "Set",           Description = "set",  DisplayOrder = 12 }, T),
        E(new Unit { Code = "PAIR", Name = "Pair",          Description = "pair", DisplayOrder = 13 }, T),
    ];

    // 2. Item Categories
    // Codes MUST match those used in SeedItems - any mismatch causes a
    // KeyNotFoundException at seed time.

    private static List<ItemCategory> SeedItemCategories(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
        GlAccountSet gl)
    {
        // Inventory categories inherit the resolved GL accounts so items that
        // don't override them get correct posting accounts automatically.
        ItemCategory Cat(string code, string name, bool isInventory = true, string? desc = null)
        {
            var cat = E(new ItemCategory
            {
                Code        = code,
                Name        = name,
                Description = desc,
            }, T);

            if (isInventory)
            {
                cat.InventoryAccountId = gl.Inventory;
                cat.CogsAccountId      = gl.Cogs;
                cat.PurchaseAccountId  = gl.Purchase;
                cat.SalesAccountId     = gl.Sales;
            }
            return cat;
        }

        return
        [
            // FMCG / Food & Beverage
            Cat("BEVERAGES",  "Beverages",              desc: "Soft drinks, water, juices & hot drinks"),
            Cat("DAIRY",      "Dairy & Eggs",           desc: "Milk, yogurt, cheese & eggs"),
            Cat("FOOD",       "Food & Grocery",         desc: "Staples, oil, flour, spices & condiments"),
            Cat("SNACKS",     "Snacks & Confectionery", desc: "Crisps, biscuits, chocolates & nuts"),
            Cat("BAKERY",     "Bakery",                 desc: "Bread, toast & baked goods"),
            Cat("FROZEN",     "Frozen Foods",           desc: "Frozen meals, meats & vegetables"),

            // ── Fast-Food / Restaurant menu (GL-bearing, sellable at POS) ──────
            // Deterministic IDs so the Sales combo promotion can target the
            // FAST-FOOD category reliably (Inventory category IDs are otherwise
            // random Guids that Sales cannot reference). Seq 1400/1401.
            E(new ItemCategory
            {
                Id                 = CrossModuleGuid.Derive(T.companyId, 1400),
                Code               = "FAST-FOOD",
                Name               = "Fast Food Menu",
                Description        = "Pizzas, burgers, fries & sides — made-to-order menu items",
                InventoryAccountId = gl.Inventory,
                CogsAccountId      = gl.Cogs,
                PurchaseAccountId  = gl.Purchase,
                SalesAccountId     = gl.Sales,
            }, T),
            E(new ItemCategory
            {
                Id                 = CrossModuleGuid.Derive(T.companyId, 1401),
                Code               = "COMBO-DEALS",
                Name               = "Combo Deals",
                Description        = "Fixed-price combo meals (pizza + burger + fries + drink)",
                InventoryAccountId = gl.Inventory,
                CogsAccountId      = gl.Cogs,
                PurchaseAccountId  = gl.Purchase,
                SalesAccountId     = gl.Sales,
            }, T),

            // Health, Beauty & Baby
            Cat("HEALTH",     "Health & Personal Care", desc: "OTC medicines, hygiene & cosmetics"),

            // Electronics
            Cat("PHONES",     "Mobile Phones",          desc: "Smartphones & feature phones"),
            Cat("TABLETS",    "Tablets",                desc: "iPads & Android tablets"),
            Cat("LAPTOPS",    "Laptops",                desc: "Notebook computers"),
            Cat("TV",         "Televisions",            desc: "Smart TVs & monitors"),
            Cat("AUDIO",      "Audio & Headphones",     desc: "Speakers, earphones & headphones"),
            Cat("WEARABLES",  "Wearables",              desc: "Smartwatches & fitness bands"),
            Cat("ACCELS",     "Electronics Accessories",desc: "Cables, chargers & power banks"),

            // Apparel & Lifestyle
            Cat("CLOTHING",   "Clothing",               desc: "Apparel & garments"),
            Cat("FOOTWEAR",   "Footwear",               desc: "Shoes, sandals & boots"),
            Cat("ACCESSORIES","Accessories",            desc: "Bags, belts, watches & jewellery"),
            Cat("SPORTS",     "Sports & Fitness",       desc: "Sportswear, equipment & gym gear"),

            // Home & Living
            Cat("KITCHEN",    "Kitchen & Appliances",   desc: "Small appliances, cookware & utensils"),
            Cat("HOME-LIVING","Home & Living",          desc: "Furniture, décor & bed/bath"),

            // B2B / Trade
            Cat("TOOLS",      "Tools & Hardware",       desc: "Hand tools, power tools & fixings"),
            Cat("AUTO",       "Automotive",             desc: "Car accessories & lubricants"),
            Cat("OFFICE-EQ",  "Stationery & Office",    desc: "Pens, paper & office supplies"),
            Cat("TOYS",       "Toys & Games",           desc: "Toys, games & hobbies"),

            // Manufacturing / Non-stock (no GL accounts needed)
            Cat("RAW-MAT",      "Raw Materials",  isInventory: false, desc: "Production raw materials"),
            Cat("FINISHED-GOODS","Finished Goods",isInventory: false, desc: "Ready-to-sell manufactured goods"),
            Cat("SERVICES",     "Services",       isInventory: false, desc: "Non-inventory service items"),
        ];
    }

    // 3. Warehouses

    private static Warehouse[] SeedWarehouses(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T) =>
    [
        E(new Warehouse
        {
            // Deterministic per-company ID shared with Sales (POS) — see CrossModuleIds.
            Id            = CrossModuleIds.MainWarehouseId(T.companyId),
            Code          = "MAIN-WH",
            Name          = "Main Warehouse",
            Address       = "1 Warehouse Road",
            WarehouseType = WarehouseType.Main,
        }, T),
        E(new Warehouse
        {
            // Deterministic per-company ID shared with Sales (POS) — POS stock deduction
            // targets this exact warehouse, so the IDs MUST match. See CrossModuleIds.
            Id            = CrossModuleIds.RetailWarehouseId(T.companyId),
            Code          = "RETAIL-WH",
            Name          = "Retail Store",
            Address       = "1 Main Street",
            WarehouseType = WarehouseType.Retail,
        }, T),
        // Manufacturing warehouses — IDs are per-company derived (seq 2001/2002)
        // so they match what ManufacturingSeedDataService.cs resolves at runtime.
        E(new Warehouse
        {
            Id            = CrossModuleGuid.Derive(T.companyId, 2001),
            Code          = "RAW-MAT-WH",
            Name          = "Raw Materials Warehouse",
            Address       = "2 Industrial Zone",
            WarehouseType = WarehouseType.Raw,
        }, T),
        E(new Warehouse
        {
            Id            = CrossModuleGuid.Derive(T.companyId, 2002),
            Code          = "FG-WH",
            Name          = "Finished Goods Warehouse",
            Address       = "3 Industrial Zone",
            WarehouseType = WarehouseType.Finished,
        }, T),
    ];

    // 4. Bins

    private static Bin[] SeedBins(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
        Warehouse[] warehouses)
    {
        var main   = warehouses.First(w => w.Code == "MAIN-WH").Id;
        var retail = warehouses.First(w => w.Code == "RETAIL-WH").Id;

        Bin B(string code, string name, Guid whId) =>
            E(new Bin { Code = code, Name = name, WarehouseId = whId }, T);

        return
        [
            B("A1-01", "Aisle A, Rack 1", main),
            B("A1-02", "Aisle A, Rack 2", main),
            B("B1-01", "Aisle B, Rack 1", main),
            B("B1-02", "Aisle B, Rack 2", main),
            B("RT-01", "Retail Shelf 1",  retail),
            B("RT-02", "Retail Shelf 2",  retail),
        ];
    }

    // 5. Brands

    private static Brand[] SeedBrands(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T)
    {
        Brand Br(string code, string name, string? website = null) =>
            E(new Brand { Code = code, Name = name, Website = website }, T);

        return
        [
            // Technology
            Br("SAMSUNG", "Samsung",        "https://samsung.com"),
            Br("APPLE",   "Apple",          "https://apple.com"),
            Br("SONY",    "Sony",           "https://sony.com"),
            Br("DELL",    "Dell",           "https://dell.com"),
            Br("HP",      "HP",             "https://hp.com"),
            Br("LENOVO",  "Lenovo",         "https://lenovo.com"),
            Br("LG",      "LG",             "https://lg.com"),
            // Apparel & Sports
            Br("NIKE",    "Nike",           "https://nike.com"),
            Br("ADIDAS",  "Adidas",         "https://adidas.com"),
            Br("PUMA",    "Puma",           "https://puma.com"),
            // FMCG
            Br("NESTLE",  "Nestlé",         "https://nestle.com"),
            Br("UNILEVER","Unilever",       "https://unilever.com"),
            Br("PG",      "Procter & Gamble","https://pg.com"),
            Br("KELLOGS", "Kellogg's",      "https://kelloggs.com"),
            // Industrial / Hardware
            Br("BOSCH",   "Bosch",          "https://bosch.com"),
            Br("3M",      "3M",             "https://3m.com"),
            // Private / Generic
            Br("GENERIC", "Generic"),
            Br("PRIVATE", "Private Label"),
        ];
    }

    // 6. Colors

    private static Color[] SeedColors(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T)
    {
        int order = 0;
        Color C(string code, string name, string hex, string family) =>
            E(new Color
            {
                Code        = code,
                Name        = name,
                HexCode     = hex,
                ColorFamily = family,
                DisplayOrder= ++order,
            }, T);

        return
        [
            C("BLK", "Black",     "#000000", "Neutral"),
            C("WHT", "White",     "#FFFFFF", "Neutral"),
            C("GRY", "Grey",      "#808080", "Neutral"),
            C("RED", "Red",       "#FF0000", "Warm"),
            C("BLU", "Blue",      "#0000FF", "Cool"),
            C("GRN", "Green",     "#008000", "Cool"),
            C("YLW", "Yellow",    "#FFFF00", "Warm"),
            C("ORG", "Orange",    "#FFA500", "Warm"),
            C("PNK", "Pink",      "#FFC0CB", "Warm"),
            C("PRP", "Purple",    "#800080", "Cool"),
            C("BRN", "Brown",     "#A52A2A", "Neutral"),
            C("GLD", "Gold",      "#FFD700", "Metallic"),
            C("SLV", "Silver",    "#C0C0C0", "Metallic"),
            C("NVY", "Navy Blue", "#000080", "Cool"),
            C("MGT", "Magenta",   "#FF00FF", "Warm"),
        ];
    }

    // 7. Sizes

    private static Size[] SeedSizes(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T)
    {
        int order = 0;
        Size S(string code, string name, string chart) =>
            E(new Size { Code = code, Name = name, SizeChart = chart, SortOrder = ++order }, T);

        return
        [
            // Apparel alpha
            S("XS",  "Extra Small", SizeChart.Apparel),
            S("S",   "Small",       SizeChart.Apparel),
            S("M",   "Medium",      SizeChart.Apparel),
            S("L",   "Large",       SizeChart.Apparel),
            S("XL",  "Extra Large", SizeChart.Apparel),
            S("XXL", "Double XL",   SizeChart.Apparel),
            // Footwear EU numeric
            S("38",  "EU 38",       SizeChart.FootwearEU),
            S("39",  "EU 39",       SizeChart.FootwearEU),
            S("40",  "EU 40",       SizeChart.FootwearEU),
            S("41",  "EU 41",       SizeChart.FootwearEU),
            S("42",  "EU 42",       SizeChart.FootwearEU),
            S("43",  "EU 43",       SizeChart.FootwearEU),
            S("44",  "EU 44",       SizeChart.FootwearEU),
            // One-size
            S("OS",  "One Size",    SizeChart.Apparel),
        ];
    }

    // 8. Attribute Definitions

    private static AttributeDefinition[] SeedAttributeDefinitions(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T)
    {
        int order = 0;
        AttributeDefinition A(string code, string name, string dataType,
            bool isVariant = false, string? unit = null, string? allowed = null) =>
            E(new AttributeDefinition
            {
                Code         = code,
                Name         = name,
                DataType     = dataType,
                IsVariant    = isVariant,
                Unit         = unit,
                AllowedValues= allowed,
                IsRequired   = false,
                DisplayOrder = ++order,
            }, T);

        return
        [
            A("MATERIAL",  "Material",          AttributeDataType.Text),
            A("WEIGHT-KG", "Weight (kg)",       AttributeDataType.Decimal, unit: "kg"),
            A("WARRANTY",  "Warranty (months)", AttributeDataType.Integer, unit: "months"),
            A("COUNTRY",   "Country of Origin", AttributeDataType.Text),
            A("GENDER",    "Gender",            AttributeDataType.List,
                allowed: "Mens,Womens,Unisex,Kids"),
            A("COLOR-VAR", "Color Variant",     AttributeDataType.Text,    isVariant: true),
            A("SIZE-VAR",  "Size Variant",      AttributeDataType.Text,    isVariant: true),
            A("VOLTAGE",   "Voltage",           AttributeDataType.Text,    unit: "V"),
            A("CAPACITY",  "Capacity",          AttributeDataType.Text),
            A("MODEL-NO",  "Model Number",      AttributeDataType.Text),
        ];
    }

    // 9. Tax Definitions

    private static TaxDefinition[] SeedTaxDefinitions(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T)
    {
        TaxDefinition Tx(string code, string name, InvEnums.TaxType taxType, decimal rate,
            bool isPercentage = true,
            bool sales = true, bool purchases = true,
            InvEnums.TaxInclusionType inclusionType = InvEnums.TaxInclusionType.Exclusive) =>
            E(new TaxDefinition
            {
                Code             = code,
                Name             = name,
                TaxType          = taxType,
                IsPercentage     = isPercentage,
                Rate             = rate,
                InclusionType    = inclusionType,
                ApplyOnSales     = sales,
                ApplyOnPurchases = purchases,
                ValidFrom        = DateTime.UtcNow,
            }, T);

        return
        [
            Tx("VAT-STD",   "Standard VAT",     InvEnums.TaxType.VAT,       15m),
            Tx("VAT-ZERO",  "Zero-Rated VAT",   InvEnums.TaxType.VAT,        0m),
            Tx("GST-5",     "GST 5%",           InvEnums.TaxType.GST,        5m),
            Tx("EXEMPT",    "Tax Exempt",        InvEnums.TaxType.VAT,        0m,
               inclusionType: InvEnums.TaxInclusionType.Inclusive),
            Tx("EXCISE-10", "Excise Duty 10%",  InvEnums.TaxType.Excise,    10m, sales: false),
            Tx("FIXED-1",   "Fixed Levy $1",    InvEnums.TaxType.Custom,     1m, isPercentage: false),
        ];
    }

    // 10b. Item Attributes
    // Assign a representative attribute to each item based on its category.

    private static ItemAttribute[] SeedItemAttributes(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
        Item[] items,
        AttributeDefinition[] attrs)
    {
        var now  = DateTime.UtcNow;
        var list = new List<ItemAttribute>();

        var attrByCode = attrs.ToDictionary(a => a.Code);

        ItemAttribute? Attr(Guid itemId, string attrCode, string value)
        {
            if (!attrByCode.TryGetValue(attrCode, out var def)) return null;
            return E(new ItemAttribute
            {
                Id                    = Guid.NewGuid(),
                ItemId                = itemId,
                AttributeDefinitionId = def.Id,
                Value                 = value,
            }, T);
        }

        void Add(ItemAttribute? a) { if (a != null) list.Add(a); }

        foreach (var item in items)
        {
            // Country of origin for all items
            Add(Attr(item.Id, "COUNTRY", "PK"));

            // Category-specific attributes
            if (item.Code.StartsWith("ELEC-SAM-") || item.Code.StartsWith("ELEC-IPHONE"))
            {
                Add(Attr(item.Id, "MODEL-NO",  item.Code));
                Add(Attr(item.Id, "WARRANTY",  "12"));
                Add(Attr(item.Id, "VOLTAGE",   "5V"));
            }
            else if (item.Code.StartsWith("ELEC-"))
            {
                Add(Attr(item.Id, "MODEL-NO", item.Code));
                Add(Attr(item.Id, "VOLTAGE",  "5V"));
            }
            else if (item.Code.StartsWith("CLOTHING") || item.Code.StartsWith("SPT-NIKE")
                  || item.Code.StartsWith("SPT-ADIDAS") || item.Code.StartsWith("SPT-PUMA"))
            {
                Add(Attr(item.Id, "MATERIAL", "Cotton"));
                Add(Attr(item.Id, "GENDER",   "Unisex"));
            }
            else if (item.Code.StartsWith("KIT-") || item.Code.StartsWith("TOOL-"))
            {
                Add(Attr(item.Id, "MATERIAL", "Stainless Steel"));
                Add(Attr(item.Id, "WARRANTY", "6"));
            }
            else if (item.Code.StartsWith("GROC-") || item.Code.StartsWith("BEV-")
                  || item.Code.StartsWith("DAI-") || item.Code.StartsWith("SNK-")
                  || item.Code.StartsWith("BAK-") || item.Code.StartsWith("FRZ-")
                  || item.Code.StartsWith("INST-") || item.Code.StartsWith("COND-")
                  || item.Code.StartsWith("SPICE-"))
            {
                Add(Attr(item.Id, "WEIGHT-KG", "0.5"));
            }
        }

        return list.ToArray();
    }

    // 10c. Item Colors
    // Assign display colors to apparel, footwear and electronics.

    private static ItemColor[] SeedItemColors(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
        Item[] items,
        Color[] colors)
    {
        var now       = DateTime.UtcNow;
        var list      = new List<ItemColor>();
        var colorMap  = colors.ToDictionary(c => c.Code);

        void Assign(Guid itemId, string colorCode, bool isDefault = true)
        {
            if (!colorMap.TryGetValue(colorCode, out var color)) return;
            list.Add(E(new ItemColor
            {
                Id        = Guid.NewGuid(),
                ItemId    = itemId,
                ColorId   = color.Id,
                IsDefault = isDefault,
            }, T));
        }

        foreach (var item in items)
        {
            if (item.Code.StartsWith("ELEC-SAM-") || item.Code.StartsWith("ELEC-IPHONE"))
                Assign(item.Id, "BLK");
            else if (item.Code.StartsWith("SPT-NIKE"))
                Assign(item.Id, "WHT");
            else if (item.Code.StartsWith("SPT-ADIDAS"))
                Assign(item.Id, "BLK");
            else if (item.Code.StartsWith("SPT-PUMA"))
                Assign(item.Id, "RED");
            else if (item.Code.StartsWith("TOOL-") || item.Code.StartsWith("KIT-"))
                Assign(item.Id, "SLV");
        }

        return list.ToArray();
    }

    // 10d. Item Sizes
    // Assign sizes to footwear and apparel items.

    private static ItemSize[] SeedItemSizes(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) T,
        Item[] items,
        Size[] sizes)
    {
        var list     = new List<ItemSize>();
        var sizeMap  = sizes.ToDictionary(s => s.Code);

        void Assign(Guid itemId, string sizeCode, bool isDefault = false)
        {
            if (!sizeMap.TryGetValue(sizeCode, out var size)) return;
            list.Add(E(new ItemSize
            {
                Id        = Guid.NewGuid(),
                ItemId    = itemId,
                SizeId    = size.Id,
                IsDefault = isDefault,
            }, T));
        }

        foreach (var item in items)
        {
            if (item.Code.StartsWith("SPT-NIKE-")   ||
                item.Code.StartsWith("SPT-ADIDAS-") ||
                item.Code.StartsWith("SPT-PUMA-"))
            {
                // Footwear - seed a selection of EU sizes; 42 is default
                Assign(item.Id, "40");
                Assign(item.Id, "41");
                Assign(item.Id, "42", isDefault: true);
                Assign(item.Id, "43");
                Assign(item.Id, "44");
            }
        }

        return list.ToArray();
    }
}
