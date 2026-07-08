namespace Sales.Tests.Integration.Controllers;

[Collection(SalesTestCollection.Name)]
public class StoreOfferControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "api/sales/storeoffer";

    private readonly SalesCollectionFixture _fixture;
    private readonly HttpClient _client;
    private readonly SalesTestDataBuilder _builder;
    private readonly List<Guid> _createdOfferIds = [];
    private readonly List<Guid> _createdStoreIds = [];

    public StoreOfferControllerTests(SalesCollectionFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient();
        _builder = new SalesTestDataBuilder(fixture);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // Offers deleted via HTTP soft-delete; store cleanup cascade-handles the rest
        foreach (var id in _createdOfferIds)
        {
            try { await _client.DeleteAsync($"{BaseUrl}/{id}"); } catch { }
        }
        foreach (var id in _createdStoreIds)
            await _builder.DeletePosStoreAsync(id);
        _client.Dispose();
    }

    // ── POST /api/sales/storeoffer ── Minimal ─────────────────────────────────

    [Fact]
    public async Task Create_WithMinimalPayload_Returns201AndOffer()
    {
        var store = await _builder.CreatePosStoreAsync("Offer Store Minimal");
        _createdStoreIds.Add(store.Id);

        var payload = new CreateStoreOfferDto
        {
            StoreId = store.Id,
            OfferType = StoreOfferType.Featured,
            Title = "Weekend Special",
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StoreOfferDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.Title.Should().Be(payload.Title);
        body.Data.StoreId.Should().Be(store.Id);
        body.Data.IsActive.Should().BeTrue();
        body.Data.Id.Should().NotBeEmpty();

        _createdOfferIds.Add(body.Data.Id);
    }

    // ── POST /api/sales/storeoffer ── Maximal ─────────────────────────────────

    [Fact]
    public async Task Create_WithMaximalPayload_Returns201AndAllFieldsPersisted()
    {
        var store = await _builder.CreatePosStoreAsync("Offer Store Maximal");
        _createdStoreIds.Add(store.Id);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var payload = new CreateStoreOfferDto
        {
            StoreId = store.Id,
            OfferType = StoreOfferType.Combo,
            Title = "Ramadan Mega Sale — Up to 50% Off",
            Subtitle = "On all categories",
            Description = "Special Ramadan discounts across all product categories",
            ImageUrl = "https://cdn.example.com/offers/ramadan.jpg",
            BannerUrl = "https://cdn.example.com/banners/ramadan-wide.jpg",
            BadgeText = "50% OFF",
            BadgeColor = "#FF5722",
            CallToAction = "Shop Now",
            DeepLinkUrl = "app://offers/ramadan-sale",
            DisplayOrder = 1,
            StartDate = today,
            EndDate = today.AddDays(30),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(23, 0),
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StoreOfferDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.Title.Should().Be(payload.Title);
        body.Data.Subtitle.Should().Be(payload.Subtitle);
        body.Data.Description.Should().Be(payload.Description);
        body.Data.ImageUrl.Should().Be(payload.ImageUrl);
        body.Data.BannerUrl.Should().Be(payload.BannerUrl);
        body.Data.BadgeText.Should().Be(payload.BadgeText);
        body.Data.BadgeColor.Should().Be(payload.BadgeColor);
        body.Data.CallToAction.Should().Be(payload.CallToAction);
        body.Data.DeepLinkUrl.Should().Be(payload.DeepLinkUrl);
        body.Data.DisplayOrder.Should().Be(payload.DisplayOrder);
        body.Data.StartDate.Should().Be(payload.StartDate);
        body.Data.EndDate.Should().Be(payload.EndDate);
        body.Data.StartTime.Should().Be(payload.StartTime);
        body.Data.EndTime.Should().Be(payload.EndTime);
        body.Data.OfferType.Should().Be(StoreOfferType.Combo);
        body.Data.IsActive.Should().BeTrue();

        _createdOfferIds.Add(body.Data.Id);
    }

    // ── POST — store not found ────────────────────────────────────────────────

    [Fact]
    public async Task Create_StoreNotFound_Returns400()
    {
        var payload = new CreateStoreOfferDto
        {
            StoreId = Guid.NewGuid(),
            OfferType = StoreOfferType.Featured,
            Title = "Ghost Offer",
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /api/sales/storeoffer ─────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<StoreOfferDto>>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
    }

    // ── GET /api/sales/storeoffer/store/{storeId} (AllowAnonymous) ────────────

    [Fact]
    public async Task GetPublicOffers_NoAuth_ReturnsOk()
    {
        var anonClient = _fixture.CreateClient();

        var response = await anonClient.GetAsync($"{BaseUrl}/store/{_fixture.SharedStore.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<StoreOfferDto>>>();
        body!.Success.Should().BeTrue();
        anonClient.Dispose();
    }

    // ── GET /api/sales/storeoffer/store/{storeId}/all ─────────────────────────

    [Fact]
    public async Task GetByStore_ReturnsOffersForStore()
    {
        var store = await _builder.CreatePosStoreAsync("GetByStore Offer Store");
        _createdStoreIds.Add(store.Id);

        var payload = new CreateStoreOfferDto
        {
            StoreId = store.Id,
            OfferType = StoreOfferType.Featured,
            Title = "GetByStore Offer",
        };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<StoreOfferDto>>())!.Data!;
        _createdOfferIds.Add(created.Id);

        var response = await _client.GetAsync($"{BaseUrl}/store/{store.Id}/all");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<StoreOfferDto>>>();
        body!.Data.Should().Contain(o => o.Id == created.Id);
    }

    // ── GET /api/sales/storeoffer/{id} ────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingOffer_ReturnsOk()
    {
        var store = await _builder.CreatePosStoreAsync("GetById Offer Store");
        _createdStoreIds.Add(store.Id);

        var payload = new CreateStoreOfferDto
        {
            StoreId = store.Id,
            OfferType = StoreOfferType.Featured,
            Title = "GetById Offer",
        };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<StoreOfferDto>>())!.Data!;
        _createdOfferIds.Add(created.Id);

        var response = await _client.GetAsync($"{BaseUrl}/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StoreOfferDto>>();
        body!.Data!.Id.Should().Be(created.Id);
        body.Data.Title.Should().Be("GetById Offer");
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PUT /api/sales/storeoffer/{id} ────────────────────────────────────────

    [Fact]
    public async Task Update_Title_Returns200WithUpdatedTitle()
    {
        var store = await _builder.CreatePosStoreAsync("Update Offer Store");
        _createdStoreIds.Add(store.Id);

        var payload = new CreateStoreOfferDto
        {
            StoreId = store.Id,
            OfferType = StoreOfferType.Featured,
            Title = "Original Title",
        };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<StoreOfferDto>>())!.Data!;
        _createdOfferIds.Add(created.Id);

        var update = new UpdateStoreOfferDto { Title = "Updated Offer Title" };
        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StoreOfferDto>>();
        body!.Data!.Title.Should().Be("Updated Offer Title");
    }

    // ── PUT /api/sales/storeoffer/{id}/toggle ─────────────────────────────────

    [Fact]
    public async Task Toggle_ActiveOffer_DeactivatesIt()
    {
        var store = await _builder.CreatePosStoreAsync("Toggle Offer Store");
        _createdStoreIds.Add(store.Id);

        var payload = new CreateStoreOfferDto
        {
            StoreId = store.Id,
            OfferType = StoreOfferType.Featured,
            Title = "Toggle Offer",
        };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<StoreOfferDto>>())!.Data!;
        _createdOfferIds.Add(created.Id);

        created.IsActive.Should().BeTrue();

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{created.Id}/toggle", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StoreOfferDto>>();
        body!.Data!.IsActive.Should().BeFalse();
    }

    // ── DELETE /api/sales/storeoffer/{id} ─────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingOffer_ReturnsOkAndNotFound()
    {
        var store = await _builder.CreatePosStoreAsync("Delete Offer Store");
        _createdStoreIds.Add(store.Id);

        var payload = new CreateStoreOfferDto
        {
            StoreId = store.Id,
            OfferType = StoreOfferType.Featured,
            Title = "To Be Deleted Offer",
        };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<StoreOfferDto>>())!.Data!;

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
