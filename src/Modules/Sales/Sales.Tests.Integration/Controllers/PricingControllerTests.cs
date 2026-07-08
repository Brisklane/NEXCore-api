using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sales.Domain.Entities;
using Sales.Infrastructure.Persistence;

namespace Sales.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for the central pricing &amp; promotion engine (POST api/sales/pricing/quote).
///
/// Prices are resolved from a seeded <see cref="PriceList"/> (Sales-owned), so these tests do not
/// depend on the Inventory item catalogue. Every test seeds against unique product GUIDs and cleans
/// up in DisposeAsync so the shared collection database stays isolated between tests.
/// </summary>
[Collection(SalesTestCollection.Name)]
public class PricingControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "api/sales/pricing/quote";

    private readonly SalesCollectionFixture _fixture;
    private readonly HttpClient _client;

    private readonly List<Guid> _priceListIds = [];
    private readonly List<Guid> _promotionIds = [];
    private readonly List<Guid> _couponIds    = [];
    private readonly List<Guid> _taxGroupIds  = [];
    private readonly List<Guid> _taxRuleIds   = [];
    private readonly List<Guid> _orderIds     = [];

    public PricingControllerTests(SalesCollectionFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        try
        {
            using var scope = _fixture.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

            db.TaxRules.RemoveRange(db.TaxRules.Where(r => _taxRuleIds.Contains(r.Id)));
            db.TaxGroupRates.RemoveRange(db.TaxGroupRates.Where(r => _taxGroupIds.Contains(r.TaxGroupId)));
            db.TaxGroups.RemoveRange(db.TaxGroups.Where(g => _taxGroupIds.Contains(g.Id)));
            db.PromotionItems.RemoveRange(db.PromotionItems.Where(i => _promotionIds.Contains(i.PromotionId)));
            db.Promotions.RemoveRange(db.Promotions.Where(p => _promotionIds.Contains(p.Id)));
            db.PriceListItems.RemoveRange(db.PriceListItems.Where(i => _priceListIds.Contains(i.PriceListId)));
            db.PriceLists.RemoveRange(db.PriceLists.Where(p => _priceListIds.Contains(p.Id)));
            db.Coupons.RemoveRange(db.Coupons.Where(c => _couponIds.Contains(c.Id)));
            db.SalesOrderLines.RemoveRange(db.SalesOrderLines.Where(l => _orderIds.Contains(l.SalesOrderId)));
            db.SalesOrders.RemoveRange(db.SalesOrders.Where(o => _orderIds.Contains(o.Id)));

            await db.SaveChangesAsync();
        }
        catch { /* best-effort cleanup */ }

        _client.Dispose();
    }

    // ── Price-list resolution ──────────────────────────────────────────────────

    [Fact]
    public async Task Quote_ResolvesUnitPriceFromPriceList()
    {
        var productId = Guid.NewGuid();
        var priceListId = await SeedPriceListAsync(productId, 250m);

        var result = await QuoteAsync(new PriceOrderRequestDto
        {
            PriceListId = priceListId,
            Lines = [Line(productId, qty: 2)],
        });

        result.Lines.Should().HaveCount(1);
        result.Lines[0].ListUnitPrice.Should().Be(250m);
        result.Lines[0].LineAmount.Should().Be(500m);
        result.SubtotalAmount.Should().Be(500m);
        result.TotalAmount.Should().Be(500m);
        result.DiscountAmount.Should().Be(0m);
    }

    // ── Auto-applied percentage promotion ──────────────────────────────────────

    [Fact]
    public async Task Quote_AppliesAutoPercentagePromotion()
    {
        var productId = Guid.NewGuid();
        var priceListId = await SeedPriceListAsync(productId, 100m);
        var promoId = await SeedPromotionAsync(productId, PromotionDiscountType.PercentageOff, value: 10m);

        var result = await QuoteAsync(new PriceOrderRequestDto
        {
            PriceListId = priceListId,
            Lines = [Line(productId, qty: 1)],
        });

        var line = result.Lines.Single();
        line.DiscountAmount.Should().Be(10m);
        line.NetUnitPrice.Should().Be(90m);
        line.LineAmount.Should().Be(90m);
        line.AppliedPromotions.Should().ContainSingle(p => p.PromotionId == promoId);
        result.LineDiscountAmount.Should().Be(10m);
        result.TotalAmount.Should().Be(90m);
    }

    // ── Buy 2 Get 1 Free (same item) ───────────────────────────────────────────

    [Fact]
    public async Task Quote_BuyTwoGetOneFree_SameItem_ZeroesOneUnit()
    {
        var productId = Guid.NewGuid();
        var priceListId = await SeedPriceListAsync(productId, 100m);
        await SeedPromotionAsync(productId, PromotionDiscountType.BuyXGetYFree, value: 0m,
            buyQty: 2, getQty: 1, freeItemId: productId);

        var result = await QuoteAsync(new PriceOrderRequestDto
        {
            PriceListId = priceListId,
            Lines = [Line(productId, qty: 3)],
        });

        var line = result.Lines.Single();
        line.DiscountAmount.Should().Be(100m);   // one of three units free
        line.LineAmount.Should().Be(200m);
        result.TotalAmount.Should().Be(200m);
    }

    // ── Coupon ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Quote_AppliesFixedCoupon()
    {
        var productId = Guid.NewGuid();
        var priceListId = await SeedPriceListAsync(productId, 100m);
        var code = $"SAVE{Guid.NewGuid():N}"[..10].ToUpper();
        await SeedCouponAsync(code, DiscountType.FixedAmount, value: 25m);

        var result = await QuoteAsync(new PriceOrderRequestDto
        {
            PriceListId = priceListId,
            CouponCode  = code,
            Lines = [Line(productId, qty: 1)],
        });

        result.CouponDiscountAmount.Should().Be(25m);
        result.CouponCode.Should().Be(code);
        result.TotalAmount.Should().Be(75m);
    }

    // ── Tax ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Quote_AppliesTaxFromRuleEngine()
    {
        var productId = Guid.NewGuid();
        var priceListId = await SeedPriceListAsync(productId, 100m);
        await SeedTaxRuleAsync(TaxCategory.Standard, ratePercent: 17m);

        var result = await QuoteAsync(new PriceOrderRequestDto
        {
            PriceListId         = priceListId,
            CustomerCountryCode = "PK",
            Lines = [Line(productId, qty: 1, taxCategory: TaxCategory.Standard)],
        });

        var line = result.Lines.Single();
        line.TaxRate.Should().Be(17m);
        line.TaxAmount.Should().Be(17m);
        result.TaxAmount.Should().Be(17m);
        result.TotalAmount.Should().Be(117m);
    }

    // ── Server-authoritative pricing on order creation ─────────────────────────

    [Fact]
    public async Task CreateOrder_IgnoresClientPrice_AndAppliesServerPriceAndPromotion()
    {
        var productId = Guid.NewGuid();
        var priceListId = await SeedPriceListAsync(productId, 100m);
        await SeedPromotionAsync(productId, PromotionDiscountType.PercentageOff, value: 10m);

        var payload = new CreateSalesOrderDto
        {
            ContactId    = Guid.NewGuid(),
            ContactName  = "Pricing Customer",
            SalesChannel = SalesChannel.DirectSales,
            CurrencyCode = "USD",
            PriceListId  = priceListId,
            Lines =
            [
                new CreateSalesOrderLineDto
                {
                    ProductId     = productId,
                    ProductCode   = "SKU",
                    ProductName   = "Test Product",
                    Quantity      = 1,
                    UnitPrice     = 999m,    // client-sent price — must be ignored
                    UnitOfMeasure = "PCS",
                },
            ],
        };

        var response = await _client.PostAsJsonAsync("api/sales/salesorder", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = (await response.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>())!.Data!;
        _orderIds.Add(order.Id);

        var line = order.Lines.Single();
        line.UnitPrice.Should().Be(100m);       // resolved from price list, not 999
        line.DiscountAmount.Should().Be(10m);   // auto 10% promotion
        line.NetUnitPrice.Should().Be(90m);
        order.TotalAmount.Should().Be(90m);
    }

    // ── Auth ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Quote_WithoutToken_Returns401()
    {
        var anon = _fixture.CreateClient();
        var response = await anon.PostAsJsonAsync(BaseUrl, new PriceOrderRequestDto
        {
            Lines = [Line(Guid.NewGuid(), 1)],
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        anon.Dispose();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static PriceOrderLineRequestDto Line(Guid productId, decimal qty,
        TaxCategory taxCategory = TaxCategory.Standard) => new()
    {
        ProductId     = productId,
        ProductCode   = "SKU",
        ProductName   = "Test Product",
        Quantity      = qty,
        UnitOfMeasure = "PCS",
        TaxCategory   = taxCategory,
    };

    private async Task<PricedOrderDto> QuoteAsync(PriceOrderRequestDto request)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PricedOrderDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        return body.Data!;
    }

    private async Task<Guid> SeedPriceListAsync(Guid productId, decimal unitPrice)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

        var priceList = new PriceList
        {
            Code         = $"PL-{Guid.NewGuid():N}"[..12],
            Name         = "Test Price List",
            CurrencyCode = "USD",
            ValidFrom    = DateTime.UtcNow.AddDays(-1),
            IsActive     = true,
            CompanyId    = TestJwtSettings.CompanyId,
            BranchId     = TestJwtSettings.BranchId,
            BusinessUnitId = TestJwtSettings.BusinessUnitId,
            Items =
            [
                new PriceListItem
                {
                    ProductId  = productId,
                    UnitPrice  = unitPrice,
                    IsActive   = true,
                    ValidFrom  = DateTime.UtcNow.AddDays(-1),
                    CompanyId  = TestJwtSettings.CompanyId,
                    BranchId   = TestJwtSettings.BranchId,
                    BusinessUnitId = TestJwtSettings.BusinessUnitId,
                },
            ],
        };

        db.PriceLists.Add(priceList);
        await db.SaveChangesAsync();
        _priceListIds.Add(priceList.Id);
        return priceList.Id;
    }

    private async Task<Guid> SeedPromotionAsync(Guid productId, PromotionDiscountType type, decimal value,
        decimal? buyQty = null, decimal? getQty = null, Guid? freeItemId = null)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var promo = new Promotion
        {
            Name          = "Test Promotion",
            IsAutoApplied = true,
            Status        = PromotionStatus.Active,
            Priority      = 10,
            StartDate     = today.AddDays(-1),
            EndDate       = today.AddDays(30),
            ScheduledDays = ScheduledDays.EveryDay,
            TargetType    = PromotionTargetType.AllCustomers,
            IsStackable   = false,
            CompanyId     = TestJwtSettings.CompanyId,
            BranchId      = TestJwtSettings.BranchId,
            BusinessUnitId = TestJwtSettings.BusinessUnitId,
            Items =
            [
                new PromotionItem
                {
                    ItemId       = productId,
                    DiscountType = type,
                    Value        = value,
                    BuyQuantity  = buyQty,
                    GetQuantity  = getQty,
                    FreeItemId   = freeItemId,
                    CompanyId    = TestJwtSettings.CompanyId,
                    BranchId     = TestJwtSettings.BranchId,
                    BusinessUnitId = TestJwtSettings.BusinessUnitId,
                },
            ],
        };

        db.Promotions.Add(promo);
        await db.SaveChangesAsync();
        _promotionIds.Add(promo.Id);
        return promo.Id;
    }

    private async Task SeedCouponAsync(string code, DiscountType type, decimal value)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

        var coupon = new Coupon
        {
            Code          = code,
            Status        = CouponStatus.Active,
            DiscountType  = type,
            DiscountValue = value,
            ValidFrom     = DateTime.UtcNow.AddDays(-1),
            CompanyId     = TestJwtSettings.CompanyId,
            BranchId      = TestJwtSettings.BranchId,
            BusinessUnitId = TestJwtSettings.BusinessUnitId,
        };

        db.Coupons.Add(coupon);
        await db.SaveChangesAsync();
        _couponIds.Add(coupon.Id);
    }

    private async Task SeedTaxRuleAsync(TaxCategory category, decimal ratePercent)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

        var group = new TaxGroup
        {
            Code      = $"TG-{Guid.NewGuid():N}"[..10],
            Name      = "Test Tax Group",
            IsActive  = true,
            CompanyId = TestJwtSettings.CompanyId,
            BranchId  = TestJwtSettings.BranchId,
            BusinessUnitId = TestJwtSettings.BusinessUnitId,
            Rates =
            [
                new TaxGroupRate
                {
                    TaxDefinitionId = Guid.NewGuid(),
                    SnapshotCode    = "VAT",
                    SnapshotRate    = ratePercent,
                    SnapshotTaxType = "VAT",
                    SnapshotInclusionType = "Exclusive",
                    Sequence        = 1,
                    IsCompound      = false,
                    CompanyId       = TestJwtSettings.CompanyId,
                    BranchId        = TestJwtSettings.BranchId,
                    BusinessUnitId  = TestJwtSettings.BusinessUnitId,
                },
            ],
        };
        db.TaxGroups.Add(group);
        await db.SaveChangesAsync();
        _taxGroupIds.Add(group.Id);

        var rule = new TaxRule
        {
            Name               = "Test Tax Rule",
            Priority           = 10,
            ProductTaxCategory = category,
            CustomerCountryCode = "PK",
            TaxGroupId         = group.Id,
            IsActive           = true,
            ValidFrom          = DateTime.UtcNow.AddDays(-1),
            CompanyId          = TestJwtSettings.CompanyId,
            BranchId           = TestJwtSettings.BranchId,
            BusinessUnitId     = TestJwtSettings.BusinessUnitId,
        };
        db.TaxRules.Add(rule);
        await db.SaveChangesAsync();
        _taxRuleIds.Add(rule.Id);
    }
}
