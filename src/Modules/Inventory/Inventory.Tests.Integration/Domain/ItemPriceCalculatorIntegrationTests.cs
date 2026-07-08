using Xunit;
using Microsoft.EntityFrameworkCore;
using Inventory.Infrastructure.Persistence;
using Inventory.Domain.Entities;
using Inventory.Domain.Services;

namespace Inventory.Tests.Integration.Domain;

/// <summary>
/// Integration tests for ItemPriceCalculator working against real ItemPrice
/// and ItemTax entities saved to the in-memory database.
/// Verifies that RecalculateEntity produces correct stored values
/// and that those values survive a round-trip through EF.
/// </summary>
public class ItemPriceCalculatorIntegrationTests
{
    private InventoryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase($"PriceCalc_{Guid.NewGuid()}")
            .Options;
        return new InventoryDbContext(options);
    }

    // ?? Seed helpers ??????????????????????????????????????????????????????

    private static Unit SeedUnit(InventoryDbContext ctx)
    {
        var unit = new Unit { Code = "PCS", Name = "Pieces", DisplayOrder = 1, IsActive = true };
        ctx.Units.Add(unit);
        return unit;
    }

    private static Item SeedItem(InventoryDbContext ctx, Unit unit)
    {
        var item = new Item
        {
            Code = "ITEM001", Name = "Test Item",
            BaseUnitId = unit.Id, ItemType = "Inventory", IsActive = true
        };
        ctx.Items.Add(item);
        return item;
    }

    private static TaxDefinition SeedTaxDef(InventoryDbContext ctx,
        string code, decimal rate, bool isInclusive = false)
    {
        var td = new TaxDefinition
        {
            Code = code, Name = code,
            TaxType = Inventory.Domain.Enums.TaxType.VAT,
            IsPercentage = true,
            Rate = rate,
            InclusionType = isInclusive
                ? Inventory.Domain.Enums.TaxInclusionType.Inclusive
                : Inventory.Domain.Enums.TaxInclusionType.Exclusive,
            ApplyOnSales = true, ApplyOnPurchases = true, IsActive = true,
            ValidFrom = DateTime.UtcNow,
        };
        ctx.TaxDefinitions.Add(td);
        return td;
    }

    private static ItemTax SeedItemTax(InventoryDbContext ctx,
        Item item, TaxDefinition td, decimal? overrideRate = null)
    {
        var it = new ItemTax
        {
            ItemId = item.Id,
            TaxDefinitionId = td.Id,
            OverrideRate = overrideRate,
            EffectiveRate = overrideRate ?? td.Rate,
            IsActive = true
        };
        ctx.ItemTaxes.Add(it);
        return it;
    }

    // ?????????????????????????????????????????????????????????????????????

    [Fact]
    public async Task ExclusiveEntry_15PctVAT_CalculatesAndPersistsCorrectly()
    {
        await using var ctx = CreateContext();
        var unit = SeedUnit(ctx);
        var item = SeedItem(ctx, unit);
        var td   = SeedTaxDef(ctx, "VAT15", 15m);
        await ctx.SaveChangesAsync();

        var itemTax = SeedItemTax(ctx, item, td);
        await ctx.SaveChangesAsync();

        var price = new ItemPrice
        {
            ItemId = item.Id, UnitId = unit.Id, PriceList = "Default",
            SalePrice = 100m, PurchasePrice = 80m,
            IsTaxInclusive = false, CurrencyCode = "USD", IsActive = true
        };
        ctx.ItemPrices.Add(price);
        await ctx.SaveChangesAsync();

        // Act
        var salesTaxes    = new[] { (itemTax.EffectiveRate, td.IsPercentage, td.InclusionType == Inventory.Domain.Enums.TaxInclusionType.Inclusive) };
        var purchaseTaxes = salesTaxes;
        ItemPriceCalculator.RecalculateEntity(price, salesTaxes, purchaseTaxes);
        await ctx.SaveChangesAsync();

        // Assert ? reload from DB
        var saved = await ctx.ItemPrices.AsNoTracking().FirstAsync(p => p.Id == price.Id);

        Assert.Equal(100m, saved.SalePriceExcludingTax);
        Assert.Equal(15m,  saved.SaleTaxAmount);
        Assert.Equal(115m, saved.SalePriceIncludingTax);
        Assert.Equal(80m,  saved.PurchasePriceExcludingTax);
        Assert.Equal(12m,  saved.PurchaseTaxAmount);
        Assert.Equal(92m,  saved.PurchasePriceIncludingTax);
        Assert.Equal(15m,  saved.EffectiveTaxRate);
    }

    [Fact]
    public async Task InclusiveEntry_15PctVAT_ExtractsTaxAndPersistsCorrectly()
    {
        await using var ctx = CreateContext();
        var unit = SeedUnit(ctx);
        var item = SeedItem(ctx, unit);
        var td   = SeedTaxDef(ctx, "VAT15", 15m, isInclusive: true);
        await ctx.SaveChangesAsync();

        var itemTax = SeedItemTax(ctx, item, td);
        await ctx.SaveChangesAsync();

        var price = new ItemPrice
        {
            ItemId = item.Id, UnitId = unit.Id, PriceList = "Default",
            SalePrice = 115m, PurchasePrice = 92m,
            IsTaxInclusive = true, CurrencyCode = "USD", IsActive = true
        };
        ctx.ItemPrices.Add(price);
        await ctx.SaveChangesAsync();

        // Act
        var taxes = new[] { (itemTax.EffectiveRate, td.IsPercentage, td.InclusionType == Inventory.Domain.Enums.TaxInclusionType.Inclusive) };
        ItemPriceCalculator.RecalculateEntity(price, taxes, taxes);
        await ctx.SaveChangesAsync();

        // Assert
        var saved = await ctx.ItemPrices.AsNoTracking().FirstAsync(p => p.Id == price.Id);

        Assert.Equal(100m, saved.SalePriceExcludingTax);
        Assert.Equal(15m,  saved.SaleTaxAmount);
        Assert.Equal(115m, saved.SalePriceIncludingTax);  // same as entered
        Assert.Equal(80m,  saved.PurchasePriceExcludingTax);
        Assert.Equal(12m,  saved.PurchaseTaxAmount);
        Assert.Equal(92m,  saved.PurchasePriceIncludingTax);
    }

    [Fact]
    public async Task CombinedTaxes_VatPlusExcise_15Plus5_TotalIs20Pct()
    {
        await using var ctx = CreateContext();
        var unit = SeedUnit(ctx);
        var item = SeedItem(ctx, unit);
        var vat     = SeedTaxDef(ctx, "VAT15",    15m);
        var excise  = SeedTaxDef(ctx, "EXCISE5",  5m);
        await ctx.SaveChangesAsync();

        var vatTax    = SeedItemTax(ctx, item, vat);
        var exciseTax = SeedItemTax(ctx, item, excise);
        await ctx.SaveChangesAsync();

        var price = new ItemPrice
        {
            ItemId = item.Id, UnitId = unit.Id, PriceList = "Default",
            SalePrice = 100m, PurchasePrice = 80m,
            IsTaxInclusive = false, CurrencyCode = "USD", IsActive = true
        };
        ctx.ItemPrices.Add(price);
        await ctx.SaveChangesAsync();

        // Act
        var taxes = new[]
        {
            (vatTax.EffectiveRate,    vat.IsPercentage,    vat.InclusionType    == Inventory.Domain.Enums.TaxInclusionType.Inclusive),
            (exciseTax.EffectiveRate, excise.IsPercentage, excise.InclusionType == Inventory.Domain.Enums.TaxInclusionType.Inclusive)
        };
        ItemPriceCalculator.RecalculateEntity(price, taxes, taxes);
        await ctx.SaveChangesAsync();

        // Assert
        var saved = await ctx.ItemPrices.AsNoTracking().FirstAsync(p => p.Id == price.Id);

        Assert.Equal(100m, saved.SalePriceExcludingTax);
        Assert.Equal(20m,  saved.SaleTaxAmount);       // 15 + 5
        Assert.Equal(120m, saved.SalePriceIncludingTax);
        Assert.Equal(20m,  saved.EffectiveTaxRate);    // stored as percentage
    }

    [Fact]
    public async Task OverrideRate_OnItemTax_UsedInsteadOfDefinitionRate()
    {
        await using var ctx = CreateContext();
        var unit = SeedUnit(ctx);
        var item = SeedItem(ctx, unit);
        var td   = SeedTaxDef(ctx, "VAT15", 15m);  // definition says 15%
        await ctx.SaveChangesAsync();

        // override to 10% for this specific item
        var itemTax = SeedItemTax(ctx, item, td, overrideRate: 10m);
        await ctx.SaveChangesAsync();

        var price = new ItemPrice
        {
            ItemId = item.Id, UnitId = unit.Id, PriceList = "Default",
            SalePrice = 100m, PurchasePrice = 80m,
            IsTaxInclusive = false, CurrencyCode = "USD", IsActive = true
        };
        ctx.ItemPrices.Add(price);
        await ctx.SaveChangesAsync();

        // Act ? EffectiveRate from ItemTax (overridden to 10)
        var taxes = new[] { (itemTax.EffectiveRate, td.IsPercentage, td.InclusionType == Inventory.Domain.Enums.TaxInclusionType.Inclusive) };
        ItemPriceCalculator.RecalculateEntity(price, taxes, taxes);
        await ctx.SaveChangesAsync();

        var saved = await ctx.ItemPrices.AsNoTracking().FirstAsync(p => p.Id == price.Id);

        Assert.Equal(10m,  saved.SaleTaxAmount);       // 10%, NOT 15%
        Assert.Equal(110m, saved.SalePriceIncludingTax);
        Assert.Equal(10m,  saved.EffectiveTaxRate);
    }

    [Fact]
    public async Task ZeroTaxes_ComputedFieldsEqualEnteredPrices()
    {
        await using var ctx = CreateContext();
        var unit = SeedUnit(ctx);
        var item = SeedItem(ctx, unit);
        await ctx.SaveChangesAsync();

        var price = new ItemPrice
        {
            ItemId = item.Id, UnitId = unit.Id, PriceList = "Default",
            SalePrice = 500m, PurchasePrice = 400m,
            IsTaxInclusive = false, CurrencyCode = "USD", IsActive = true
        };
        ctx.ItemPrices.Add(price);
        await ctx.SaveChangesAsync();

        // Act — no taxes at all
        ItemPriceCalculator.RecalculateEntity(price, [], []);
        await ctx.SaveChangesAsync();

        var saved = await ctx.ItemPrices.AsNoTracking().FirstAsync(p => p.Id == price.Id);

        Assert.Equal(500m, saved.SalePriceExcludingTax);
        Assert.Equal(0m,   saved.SaleTaxAmount);
        Assert.Equal(500m, saved.SalePriceIncludingTax);
        Assert.Equal(0m,   saved.EffectiveTaxRate);
    }

    [Fact]
    public async Task MultiplePriceLists_EachCalculatedIndependently()
    {
        await using var ctx = CreateContext();
        var unit = SeedUnit(ctx);
        var item = SeedItem(ctx, unit);
        var td   = SeedTaxDef(ctx, "VAT15", 15m);
        await ctx.SaveChangesAsync();

        var itemTax = SeedItemTax(ctx, item, td);
        await ctx.SaveChangesAsync();

        var retail = new ItemPrice
        {
            ItemId = item.Id, UnitId = unit.Id, PriceList = "Retail",
            SalePrice = 100m, PurchasePrice = 70m,
            IsTaxInclusive = false, CurrencyCode = "USD", IsActive = true
        };
        var wholesale = new ItemPrice
        {
            ItemId = item.Id, UnitId = unit.Id, PriceList = "Wholesale",
            SalePrice = 80m, PurchasePrice = 70m,
            IsTaxInclusive = false, CurrencyCode = "USD", IsActive = true
        };
        ctx.ItemPrices.AddRange(retail, wholesale);
        await ctx.SaveChangesAsync();

        var taxes = new[] { (itemTax.EffectiveRate, td.IsPercentage, td.InclusionType == Inventory.Domain.Enums.TaxInclusionType.Inclusive) };

        ItemPriceCalculator.RecalculateEntity(retail,    taxes, taxes);
        ItemPriceCalculator.RecalculateEntity(wholesale, taxes, taxes);
        await ctx.SaveChangesAsync();

        var savedRetail    = await ctx.ItemPrices.AsNoTracking().FirstAsync(p => p.Id == retail.Id);
        var savedWholesale = await ctx.ItemPrices.AsNoTracking().FirstAsync(p => p.Id == wholesale.Id);

        Assert.Equal(115m, savedRetail.SalePriceIncludingTax);
        Assert.Equal(92m,  savedWholesale.SalePriceIncludingTax);
    }
}
