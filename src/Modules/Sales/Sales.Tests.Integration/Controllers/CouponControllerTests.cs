namespace Sales.Tests.Integration.Controllers;

[Collection(SalesTestCollection.Name)]
public class CouponControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "api/sales/coupon";

    private readonly SalesCollectionFixture _fixture;
    private readonly HttpClient _client;
    private readonly List<Guid> _createdIds = [];

    public CouponControllerTests(SalesCollectionFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var id in _createdIds)
        {
            try { await _client.DeleteAsync($"{BaseUrl}/{id}"); } catch { /* ignore */ }
        }
        _client.Dispose();
    }

    // ── POST /api/sales/coupon ── Minimal ─────────────────────────────────────

    [Fact]
    public async Task Create_WithMinimalPayload_Returns201AndCoupon()
    {
        var code = $"SAVE{Guid.NewGuid():N}"[..12].ToUpper();

        var payload = new CreateCouponDto
        {
            Code = code,
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10m,
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CouponDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.Code.Should().Be(code);
        body.Data.DiscountType.Should().Be(DiscountType.Percentage);
        body.Data.DiscountValue.Should().Be(10m);
        body.Data.Id.Should().NotBeEmpty();

        _createdIds.Add(body.Data.Id);
    }

    // ── POST /api/sales/coupon ── Maximal ─────────────────────────────────────

    [Fact]
    public async Task Create_WithMaximalPayload_Returns201AndAllFieldsPersisted()
    {
        var code = $"MAX{Guid.NewGuid():N}"[..12].ToUpper();
        var validFrom = DateTime.UtcNow;
        var validTo = DateTime.UtcNow.AddDays(30);

        var payload = new CreateCouponDto
        {
            Code = code,
            Description = "Summer sale flat discount — all orders over PKR 1000",
            DiscountType = DiscountType.FixedAmount,
            DiscountValue = 250m,
            MinOrderAmount = 1000m,
            MaxDiscountAmount = 500m,
            UsageLimit = 100,
            PerCustomerLimit = 2,
            ValidFrom = validFrom,
            ValidTo = validTo,
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CouponDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.Code.Should().Be(code);
        body.Data.Description.Should().Be(payload.Description);
        body.Data.DiscountType.Should().Be(DiscountType.FixedAmount);
        body.Data.DiscountValue.Should().Be(250m);
        body.Data.MinOrderAmount.Should().Be(1000m);
        body.Data.MaxDiscountAmount.Should().Be(500m);
        body.Data.UsageLimit.Should().Be(100);

        _createdIds.Add(body.Data.Id);
    }

    // ── GET /api/sales/coupon ──────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<CouponDto>>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
    }

    // ── GET /api/sales/coupon/{id} ────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingCoupon_ReturnsOk()
    {
        var code = $"GBI{Guid.NewGuid():N}"[..12].ToUpper();
        var payload = new CreateCouponDto { Code = code, DiscountType = DiscountType.Percentage, DiscountValue = 5m };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<CouponDto>>())!.Data!;
        _createdIds.Add(created.Id);

        var response = await _client.GetAsync($"{BaseUrl}/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CouponDto>>();
        body!.Data!.Id.Should().Be(created.Id);
        body.Data.Code.Should().Be(code);
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/sales/coupon/active ──────────────────────────────────────────

    [Fact]
    public async Task GetActive_ReturnsOkWithList()
    {
        var response = await _client.GetAsync($"{BaseUrl}/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<CouponDto>>>();
        body!.Success.Should().BeTrue();
    }

    // ── GET /api/sales/coupon/validate/{code} — AllowAnonymous ───────────────

    [Fact]
    public async Task Validate_ValidCoupon_ReturnsOk()
    {
        var code = $"VLD{Guid.NewGuid():N}"[..12].ToUpper();
        var payload = new CreateCouponDto { Code = code, DiscountType = DiscountType.Percentage, DiscountValue = 15m };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<CouponDto>>())!.Data!;
        _createdIds.Add(created.Id);

        var anonClient = _fixture.CreateClient();
        var response = await anonClient.GetAsync($"{BaseUrl}/validate/{code}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        anonClient.Dispose();
    }

    [Fact]
    public async Task Validate_NonExistentCode_Returns400()
    {
        var anonClient = _fixture.CreateClient();
        var response = await anonClient.GetAsync($"{BaseUrl}/validate/NOSUCHCOUPON");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        anonClient.Dispose();
    }

    // ── DELETE /api/sales/coupon/{id} ─────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingCoupon_ReturnsOkAndNotFound()
    {
        var code = $"DEL{Guid.NewGuid():N}"[..12].ToUpper();
        var payload = new CreateCouponDto { Code = code, DiscountType = DiscountType.Percentage, DiscountValue = 5m };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<CouponDto>>())!.Data!;

        var deleteResponse = await _client.DeleteAsync($"{BaseUrl}/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"{BaseUrl}/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Unauthenticated access ────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var anonClient = _fixture.CreateClient();
        var response = await anonClient.GetAsync(BaseUrl);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        anonClient.Dispose();
    }
}
