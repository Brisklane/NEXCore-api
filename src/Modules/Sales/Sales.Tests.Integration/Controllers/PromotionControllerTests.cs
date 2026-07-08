namespace Sales.Tests.Integration.Controllers;

[Collection(SalesTestCollection.Name)]
public class PromotionControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "api/sales/promotion";

    private readonly SalesCollectionFixture _fixture;
    private readonly HttpClient _client;
    private readonly List<Guid> _createdIds = [];

    public PromotionControllerTests(SalesCollectionFixture fixture)
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

    // ── POST /api/sales/promotion ── Minimal ──────────────────────────────────

    [Fact]
    public async Task Create_WithMinimalPayload_Returns201AndDraftPromotion()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var payload = new CreatePromotionDto
        {
            Name = "Simple 10% Off",
            StartDate = today,
            EndDate = today.AddDays(7),
            Items = [],
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PromotionDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.Name.Should().Be(payload.Name);
        body.Data.Status.Should().Be(PromotionStatus.Draft);
        body.Data.StartDate.Should().Be(today);
        body.Data.EndDate.Should().Be(today.AddDays(7));
        body.Data.Id.Should().NotBeEmpty();

        _createdIds.Add(body.Data.Id);
    }

    // ── POST /api/sales/promotion ── Maximal ──────────────────────────────────

    [Fact]
    public async Task Create_WithMaximalPayload_Returns201AndAllFieldsPersisted()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var promoCode = $"PROMO{Guid.NewGuid():N}"[..12].ToUpper();
        var productId = Guid.NewGuid();

        var payload = new CreatePromotionDto
        {
            Name = "Ramadan Mega Sale — Buy 2 Get 1 Free",
            Description = "Special Ramadan offer on selected items",
            PromotionCode = promoCode,
            IsAutoApplied = false,
            Priority = 10,
            StartDate = today,
            EndDate = today.AddDays(30),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(21, 0),
            ScheduledDays = ScheduledDays.EveryDay,
            MaxUsageCount = 500,
            MaxUsagePerCustomer = 1,
            MinOrderAmount = 2000m,
            TargetType = PromotionTargetType.AllCustomers,
            IsStackable = false,
            Notes = "Ramadan special — not stackable with other promos",
            Items =
            [
                new CreatePromotionItemDto
                {
                    ItemId = productId,
                    DiscountType = PromotionDiscountType.BuyXGetYFree,
                    PriceTarget = PromotionPriceTarget.AnyPrice,
                    Value = 0m,
                    IsConditional = true,
                    ConditionType = PromotionConditionType.MinQuantity,
                    BuyQuantity = 2,
                    GetQuantity = 1,
                    FreeItemId = productId,
                },
            ],
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PromotionDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.Name.Should().Be(payload.Name);
        body.Data.PromotionCode.Should().Be(promoCode);
        body.Data.IsAutoApplied.Should().BeFalse();
        body.Data.Priority.Should().Be(10);
        body.Data.MaxUsageCount.Should().Be(500);
        body.Data.MinOrderAmount.Should().Be(2000m);
        body.Data.Items.Should().HaveCount(1);

        _createdIds.Add(body.Data.Id);
    }

    // ── POST duplicate promo code ─────────────────────────────────────────────

    [Fact]
    public async Task Create_DuplicatePromotionCode_Returns409Conflict()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var promoCode = $"UNIQ{Guid.NewGuid():N}"[..12].ToUpper();

        var payload = new CreatePromotionDto
        {
            Name = "Unique Code Promo",
            PromotionCode = promoCode,
            StartDate = today,
            EndDate = today.AddDays(7),
            Items = [],
        };

        var r1 = await _client.PostAsJsonAsync(BaseUrl, payload);
        r1.StatusCode.Should().Be(HttpStatusCode.Created);
        var p1 = (await r1.Content.ReadFromJsonAsync<ApiResponse<PromotionDto>>())!.Data!;
        _createdIds.Add(p1.Id);

        payload.Name = "Duplicate Code Promo";
        var r2 = await _client.PostAsJsonAsync(BaseUrl, payload);
        r2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── GET /api/sales/promotion ───────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<PromotionDto>>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
    }

    // ── GET /api/sales/promotion/active ───────────────────────────────────────

    [Fact]
    public async Task GetActive_ReturnsOkWithList()
    {
        var response = await _client.GetAsync($"{BaseUrl}/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<PromotionDto>>>();
        body!.Success.Should().BeTrue();
    }

    // ── GET /api/sales/promotion/{id} ─────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingPromotion_ReturnsOkWithItems()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var productId = Guid.NewGuid();

        var payload = new CreatePromotionDto
        {
            Name = "GetById Promo",
            StartDate = today,
            EndDate = today.AddDays(7),
            Items =
            [
                new CreatePromotionItemDto
                {
                    ItemId = productId,
                    DiscountType = PromotionDiscountType.PercentageOff,
                    PriceTarget = PromotionPriceTarget.AnyPrice,
                    Value = 15m,
                    IsConditional = false,
                    ConditionType = PromotionConditionType.None,
                },
            ],
        };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<PromotionDto>>())!.Data!;
        _createdIds.Add(created.Id);

        var response = await _client.GetAsync($"{BaseUrl}/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PromotionDto>>();
        body!.Data!.Id.Should().Be(created.Id);
        body.Data.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PUT /api/sales/promotion/{id}/activate ────────────────────────────────

    [Fact]
    public async Task Activate_DraftPromotion_TransitionsToActive()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var payload = new CreatePromotionDto
        {
            Name = "Activation Test Promo",
            StartDate = today,
            EndDate = today.AddDays(7),
            Items = [],
        };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<PromotionDto>>())!.Data!;
        _createdIds.Add(created.Id);

        var activateResponse = await _client.PutAsJsonAsync($"{BaseUrl}/{created.Id}/activate", new { });

        activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await activateResponse.Content.ReadFromJsonAsync<ApiResponse<PromotionDto>>();
        body!.Data!.Status.Should().Be(PromotionStatus.Active);
    }

    // ── DELETE /api/sales/promotion/{id} ──────────────────────────────────────

    [Fact]
    public async Task Delete_DraftPromotion_ReturnsOk()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var payload = new CreatePromotionDto
        {
            Name = "To Delete Promo",
            StartDate = today,
            EndDate = today.AddDays(3),
            Items = [],
        };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<PromotionDto>>())!.Data!;

        var deleteResponse = await _client.DeleteAsync($"{BaseUrl}/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
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
