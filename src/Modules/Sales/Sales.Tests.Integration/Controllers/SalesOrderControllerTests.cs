namespace Sales.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for /api/sales/salesorder endpoints.
/// Customer (ContactId + ContactName) is mandatory on every order.
/// </summary>
[Collection(SalesTestCollection.Name)]
public class SalesOrderControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "api/sales/salesorder";

    private readonly SalesCollectionFixture _fixture;
    private readonly HttpClient _client;
    private readonly SalesTestDataBuilder _builder;
    private readonly List<Guid> _createdIds = [];

    public SalesOrderControllerTests(SalesCollectionFixture fixture)
    {
        _fixture = fixture;
        _client  = fixture.CreateAuthenticatedClient();
        _builder = new SalesTestDataBuilder(fixture);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var id in _createdIds)
            await _builder.DeleteSalesOrderAsync(id);
        _client.Dispose();
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    /// <summary>Minimal valid order payload — customer always required.</summary>
    private static CreateSalesOrderDto MinimalOrder(Guid? contactId = null) => new()
    {
        ContactId       = contactId ?? Guid.NewGuid(),
        ContactName     = "Test Customer",
        SalesChannel    = SalesChannel.DirectSales,
        FulfillmentType = FulfillmentType.Immediate,
        CurrencyCode    = "USD",
        Lines           = [],
    };

    // ── GET /api/sales/salesorder ─────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<SalesOrderDto>>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
    }

    // ── POST /api/sales/salesorder ────────────────────────────────────────────

    [Fact]
    public async Task Create_WithMinimalPayload_Returns201AndDraftOrder()
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, MinimalOrder());

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.Status.Should().Be(SalesOrderStatus.Draft);
        body.Data.OrderNumber.Should().StartWith("SO-");
        body.Data.CurrencyCode.Should().Be("USD");
        body.Data.ContactId.Should().NotBeEmpty();
        body.Data.Id.Should().NotBeEmpty();

        _createdIds.Add(body.Data.Id);
    }

    [Fact]
    public async Task Create_WithoutCustomer_Returns400()
    {
        var payload = new CreateSalesOrderDto
        {
            CurrencyCode = "USD",
            Lines = [],
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithOrderLines_PersistsLines()
    {
        var contactId = Guid.NewGuid();
        var payload = new CreateSalesOrderDto
        {
            ContactId    = contactId,
            ContactName  = "Zara Khan",
            SalesChannel = SalesChannel.DirectSales,
            CurrencyCode = "PKR",
            Lines =
            [
                new CreateSalesOrderLineDto
                {
                    ProductId     = Guid.NewGuid(),
                    ProductCode   = "SKU-001",
                    ProductName   = "Test Product",
                    Quantity      = 2,
                    UnitPrice     = 100m,
                    UnitOfMeasure = "PCS",
                },
            ],
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>();
        body!.Data!.ContactId.Should().Be(contactId);
        body.Data.Lines.Should().HaveCount(1);
        body.Data.Lines[0].ProductCode.Should().Be("SKU-001");
        body.Data.Lines[0].Quantity.Should().Be(2m);

        _createdIds.Add(body.Data.Id);
    }

    [Fact]
    public async Task Create_EachOrderGetsUniqueOrderNumber()
    {
        var r1 = await _client.PostAsJsonAsync(BaseUrl, MinimalOrder());
        var r2 = await _client.PostAsJsonAsync(BaseUrl, MinimalOrder());

        r1.StatusCode.Should().Be(HttpStatusCode.Created);
        r2.StatusCode.Should().Be(HttpStatusCode.Created);

        var o1 = (await r1.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>())!.Data!;
        var o2 = (await r2.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>())!.Data!;

        o1.OrderNumber.Should().NotBe(o2.OrderNumber);

        _createdIds.Add(o1.Id);
        _createdIds.Add(o2.Id);
    }

    // ── GET /api/sales/salesorder/{id} ────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingOrder_ReturnsOk()
    {
        var order = await _builder.CreateSalesOrderAsync();
        _createdIds.Add(order.Id);

        var response = await _client.GetAsync($"{BaseUrl}/{order.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>();
        body!.Data!.Id.Should().Be(order.Id);
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/sales/salesorder/{id}/full ───────────────────────────────────

    [Fact]
    public async Task GetFullDetails_ExistingOrder_ReturnsOkWithDetails()
    {
        var order = await _builder.CreateSalesOrderAsync();
        _createdIds.Add(order.Id);

        var response = await _client.GetAsync($"{BaseUrl}/{order.Id}/full");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.Id.Should().Be(order.Id);
    }

    // ── GET /api/sales/salesorder/by-status/{status} ─────────────────────────

    [Fact]
    public async Task GetByStatus_Draft_IncludesNewOrders()
    {
        var order = await _builder.CreateSalesOrderAsync(status: SalesOrderStatus.Draft);
        _createdIds.Add(order.Id);

        var response = await _client.GetAsync($"{BaseUrl}/by-status/{SalesOrderStatus.Draft}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<SalesOrderDto>>>();
        body!.Data.Should().Contain(o => o.Id == order.Id);
    }

    // ── GET /api/sales/salesorder/by-channel/{channel} ───────────────────────

    [Fact]
    public async Task GetByChannel_DirectSales_ReturnsMatchingOrders()
    {
        var order = await _builder.CreateSalesOrderAsync(channel: SalesChannel.DirectSales);
        _createdIds.Add(order.Id);

        var response = await _client.GetAsync($"{BaseUrl}/by-channel/{SalesChannel.DirectSales}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<SalesOrderDto>>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().Contain(o => o.Id == order.Id);
    }

    // ── POST /api/sales/salesorder/{id}/place ─────────────────────────────────

    [Fact]
    public async Task PlaceOrder_DraftOrder_TransitionsToPlaced()
    {
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, MinimalOrder()))
            .Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>())!.Data!;
        _createdIds.Add(created.Id);

        var placeResponse = await _client.PostAsJsonAsync($"{BaseUrl}/{created.Id}/place", new { });

        placeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await placeResponse.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>();
        body!.Data!.Status.Should().Be(SalesOrderStatus.Placed);
        body.Data.PlacedAt.Should().NotBeNull();
    }

    // ── PATCH /api/sales/salesorder/{id}/status ───────────────────────────────

    [Fact]
    public async Task UpdateStatus_PlacedToConfirmed_TransitionsStatus()
    {
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, MinimalOrder()))
            .Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>())!.Data!;
        _createdIds.Add(created.Id);

        await _client.PostAsJsonAsync($"{BaseUrl}/{created.Id}/place", new { });

        var statusDto = new UpdateSalesOrderStatusDto { Status = SalesOrderStatus.Confirmed };
        var response = await _client.PatchAsJsonAsync($"{BaseUrl}/{created.Id}/status", statusDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>();
        body!.Data!.Status.Should().Be(SalesOrderStatus.Confirmed);
        body.Data.ApprovedDate.Should().NotBeNull();
    }

    // ── DELETE /api/sales/salesorder/{id} ─────────────────────────────────────

    [Fact]
    public async Task Delete_DraftOrder_ReturnsOkAndOrderNotFound()
    {
        var order = await _builder.CreateSalesOrderAsync(status: SalesOrderStatus.Draft);

        var deleteResponse = await _client.DeleteAsync($"{BaseUrl}/{order.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"{BaseUrl}/{order.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/sales/salesorder/by-number/{orderNumber} ────────────────────

    [Fact]
    public async Task GetByNumber_ExistingOrder_ReturnsCorrectOrder()
    {
        var order = await _builder.CreateSalesOrderAsync();
        _createdIds.Add(order.Id);

        var response = await _client.GetAsync($"{BaseUrl}/by-number/{order.OrderNumber}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>();
        body!.Data!.OrderNumber.Should().Be(order.OrderNumber);
    }

    [Fact]
    public async Task GetByNumber_NonExistentNumber_Returns404()
    {
        var response = await _client.GetAsync($"{BaseUrl}/by-number/SO-DOES-NOT-EXIST");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
