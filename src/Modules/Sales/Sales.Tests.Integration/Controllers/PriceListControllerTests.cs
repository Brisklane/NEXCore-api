namespace Sales.Tests.Integration.Controllers;

[Collection(SalesTestCollection.Name)]
public class PriceListControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "api/sales/pricelist";

    private readonly SalesCollectionFixture _fixture;
    private readonly HttpClient _client;
    private readonly List<Guid> _createdIds = [];

    public PriceListControllerTests(SalesCollectionFixture fixture)
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

    // ── POST /api/sales/pricelist ── Minimal ──────────────────────────────────

    [Fact]
    public async Task Create_WithMinimalPayload_Returns201AndPriceList()
    {
        var code = $"PL-{Guid.NewGuid():N}"[..12];

        var payload = new CreatePriceListDto
        {
            Code = code,
            Name = "Standard Retail List",
            CurrencyCode = "USD",
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PriceListDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.Code.Should().Be(code);
        body.Data.Name.Should().Be(payload.Name);
        body.Data.CurrencyCode.Should().Be("USD");
        body.Data.Id.Should().NotBeEmpty();

        _createdIds.Add(body.Data.Id);
    }

    // ── POST /api/sales/pricelist ── Maximal ──────────────────────────────────

    [Fact]
    public async Task Create_WithMaximalPayload_Returns201AndAllFieldsPersisted()
    {
        var code = $"WS-{Guid.NewGuid():N}"[..12];
        var validFrom = DateTime.UtcNow;
        var validTo = DateTime.UtcNow.AddMonths(6);

        var payload = new CreatePriceListDto
        {
            Code = code,
            Name = "Wholesale Q3 2026 — PKR",
            ListType = PriceListType.Wholesale,
            CurrencyCode = "PKR",
            ExchangeRate = 278.5m,
            ValidFrom = validFrom,
            ValidTo = validTo,
            Notes = "Seasonal wholesale pricing for Q3 partners",
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PriceListDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.Code.Should().Be(code);
        body.Data.Name.Should().Be(payload.Name);
        body.Data.ListType.Should().Be(PriceListType.Wholesale);
        body.Data.CurrencyCode.Should().Be("PKR");

        _createdIds.Add(body.Data.Id);
    }

    // ── GET /api/sales/pricelist ───────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<PriceListDto>>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
    }

    // ── GET /api/sales/pricelist/{id} ─────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingPriceList_ReturnsOk()
    {
        var code = $"GB-{Guid.NewGuid():N}"[..12];
        var payload = new CreatePriceListDto { Code = code, Name = "GetById List", CurrencyCode = "USD" };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<PriceListDto>>())!.Data!;
        _createdIds.Add(created.Id);

        var response = await _client.GetAsync($"{BaseUrl}/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PriceListDto>>();
        body!.Data!.Id.Should().Be(created.Id);
        body.Data.Code.Should().Be(code);
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/sales/pricelist/by-code/{code} ───────────────────────────────

    [Fact]
    public async Task GetByCode_ExistingPriceList_ReturnsCorrectList()
    {
        var code = $"BC-{Guid.NewGuid():N}"[..12];
        var payload = new CreatePriceListDto { Code = code, Name = "ByCode List", CurrencyCode = "USD" };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<PriceListDto>>())!.Data!;
        _createdIds.Add(created.Id);

        var response = await _client.GetAsync($"{BaseUrl}/by-code/{code}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PriceListDto>>();
        body!.Data!.Code.Should().Be(code);
    }

    // ── GET /api/sales/pricelist/active ───────────────────────────────────────

    [Fact]
    public async Task GetActive_ReturnsOkWithList()
    {
        var response = await _client.GetAsync($"{BaseUrl}/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<PriceListDto>>>();
        body!.Success.Should().BeTrue();
    }

    // ── PUT /api/sales/pricelist/{id} ─────────────────────────────────────────

    [Fact]
    public async Task Update_PriceListName_Returns200WithUpdatedName()
    {
        var code = $"UPD-{Guid.NewGuid():N}"[..12];
        var payload = new CreatePriceListDto { Code = code, Name = "Original Name", CurrencyCode = "USD" };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<PriceListDto>>())!.Data!;
        _createdIds.Add(created.Id);

        var update = new UpdatePriceListDto { Name = "Updated Price List Name" };
        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PriceListDto>>();
        body!.Data!.Name.Should().Be("Updated Price List Name");
    }

    // ── DELETE /api/sales/pricelist/{id} ──────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingPriceList_ReturnsOkAndNotFound()
    {
        var code = $"DEL-{Guid.NewGuid():N}"[..12];
        var payload = new CreatePriceListDto { Code = code, Name = "To Delete List", CurrencyCode = "USD" };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<PriceListDto>>())!.Data!;

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
