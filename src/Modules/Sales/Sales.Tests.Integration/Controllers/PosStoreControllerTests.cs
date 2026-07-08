namespace Sales.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for GET/POST/PUT/DELETE /api/sales/posstore.
///
/// Each test method gets its own class instance (xUnit design), so:
///   - _createdIds tracks stores made during that one test
///   - DisposeAsync deletes them, keeping tests isolated
/// </summary>
[Collection(SalesTestCollection.Name)]
public class PosStoreControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "api/sales/posstore";

    private readonly SalesCollectionFixture _fixture;
    private readonly HttpClient _client;
    private readonly SalesTestDataBuilder _builder;
    private readonly List<Guid> _createdIds = [];

    public PosStoreControllerTests(SalesCollectionFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient();
        _builder = new SalesTestDataBuilder(fixture);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var id in _createdIds)
            await _builder.DeletePosStoreAsync(id);
        _client.Dispose();
    }

    // ── GET /api/sales/posstore ───────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<PosStoreDto>>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
    }

    // ── POST /api/sales/posstore ──────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidPayload_Returns201AndStore()
    {
        var payload = new CreatePosStoreDto
        {
            TradingName = $"Integration Store {Guid.NewGuid():N}"[..40],
            StoreType = PosStoreType.Retail,
            StoreFormat = PosStoreFormat.Physical,
            CountryCode = "PK",
            HasDelivery = true,
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PosStoreDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.TradingName.Should().Be(payload.TradingName);
        body.Data.IsActive.Should().BeTrue();
        body.Data.Id.Should().NotBeEmpty();

        _createdIds.Add(body.Data.Id);
    }

    [Fact]
    public async Task Create_EachStoreGetsUniqueIdAndCode()
    {
        var payload1 = new CreatePosStoreDto { TradingName = "Store Alpha", CountryCode = "PK" };
        var payload2 = new CreatePosStoreDto { TradingName = "Store Beta", CountryCode = "PK" };

        var r1 = await _client.PostAsJsonAsync(BaseUrl, payload1);
        var r2 = await _client.PostAsJsonAsync(BaseUrl, payload2);

        r1.StatusCode.Should().Be(HttpStatusCode.Created);
        r2.StatusCode.Should().Be(HttpStatusCode.Created);

        var s1 = (await r1.Content.ReadFromJsonAsync<ApiResponse<PosStoreDto>>())!.Data!;
        var s2 = (await r2.Content.ReadFromJsonAsync<ApiResponse<PosStoreDto>>())!.Data!;

        s1.Id.Should().NotBe(s2.Id);
        s1.Code.Should().StartWith("POS-");
        s2.Code.Should().StartWith("POS-");

        _createdIds.Add(s1.Id);
        _createdIds.Add(s2.Id);
    }

    // ── GET /api/sales/posstore/{id} ──────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingStore_ReturnsOk()
    {
        var store = await _builder.CreatePosStoreAsync("GetById Test Store");
        _createdIds.Add(store.Id);

        var response = await _client.GetAsync($"{BaseUrl}/{store.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PosStoreDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.Id.Should().Be(store.Id);
        body.Data.TradingName.Should().Be("GetById Test Store");
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/sales/posstore/active ────────────────────────────────────────

    [Fact]
    public async Task GetActive_ReturnsOnlyActiveStores()
    {
        var active = await _builder.CreatePosStoreAsync("Active Store", isActive: true);
        var inactive = await _builder.CreatePosStoreAsync("Inactive Store", isActive: false);
        _createdIds.Add(active.Id);
        _createdIds.Add(inactive.Id);

        var response = await _client.GetAsync($"{BaseUrl}/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<PosStoreDto>>>();
        body!.Data.Should().NotContain(s => s.Id == inactive.Id);
        body.Data.Should().Contain(s => s.Id == active.Id);
    }

    // ── PUT /api/sales/posstore/{id} ──────────────────────────────────────────

    [Fact]
    public async Task Update_ChangeTradingName_ReturnsOkWithUpdatedData()
    {
        var store = await _builder.CreatePosStoreAsync("Original Name");
        _createdIds.Add(store.Id);

        var update = new UpdatePosStoreDto { TradingName = "Updated Name" };

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{store.Id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PosStoreDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.TradingName.Should().Be("Updated Name");
    }

    [Fact]
    public async Task Update_SetOnlineStatus_PersistsChange()
    {
        var store = await _builder.CreatePosStoreAsync("Online Status Store");
        _createdIds.Add(store.Id);

        var update = new UpdatePosStoreDto { IsActive = false };

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{store.Id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PosStoreDto>>();
        body!.Data!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Update_NonExistentStore_Returns404()
    {
        var update = new UpdatePosStoreDto { TradingName = "Ghost Store" };

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{Guid.NewGuid()}", update);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /api/sales/posstore/{id} ───────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingStore_ReturnsOkAndStore404Afterwards()
    {
        var store = await _builder.CreatePosStoreAsync("To Be Deleted");
        // Do NOT add to _createdIds — we'll verify it's gone after deletion

        var deleteResponse = await _client.DeleteAsync($"{BaseUrl}/{store.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"{BaseUrl}/{store.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_NonExistentStore_Returns404()
    {
        var response = await _client.DeleteAsync($"{BaseUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/sales/posstore/nearby ────────────────────────────────────────

    [Fact]
    public async Task GetNearby_NoAuth_ReturnsOk()
    {
        // nearby is [AllowAnonymous] — use unauthenticated client
        var anonClient = _fixture.CreateClient();

        var response = await anonClient.GetAsync($"{BaseUrl}/nearby?lat=33.6&lng=73.0&radiusKm=50");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        anonClient.Dispose();
    }

    [Fact]
    public async Task GetNearby_InvalidRadius_Returns400()
    {
        var anonClient = _fixture.CreateClient();

        var response = await anonClient.GetAsync($"{BaseUrl}/nearby?lat=0&lng=0&radiusKm=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        anonClient.Dispose();
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
