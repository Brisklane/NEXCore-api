namespace Sales.Tests.Integration.Controllers;

[Collection(SalesTestCollection.Name)]
public class DeliveryControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "api/sales/delivery";

    private readonly SalesCollectionFixture _fixture;
    private readonly HttpClient _client;
    private readonly SalesTestDataBuilder _builder;
    private readonly List<Guid> _createdDeliveryIds = [];
    private readonly List<Guid> _createdOrderIds = [];

    public DeliveryControllerTests(SalesCollectionFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient();
        _builder = new SalesTestDataBuilder(fixture);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var id in _createdDeliveryIds)
        {
            try { await _client.DeleteAsync($"{BaseUrl}/{id}"); } catch { /* ignore */ }
        }
        foreach (var id in _createdOrderIds)
            await _builder.DeleteSalesOrderAsync(id);
        _client.Dispose();
    }

    // ── POST /api/sales/delivery ── Minimal ───────────────────────────────────

    [Fact]
    public async Task Create_WithMinimalPayload_Returns201AndDraftDelivery()
    {
        var order = await _builder.CreateSalesOrderAsync(SalesOrderStatus.Confirmed);
        _createdOrderIds.Add(order.Id);

        var payload = new CreateDeliveryDto
        {
            SalesOrderId = order.Id,
            PlannedDeliveryDate = DateTime.UtcNow.AddDays(3),
            Lines = [],
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<DeliveryDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.DeliveryNumber.Should().StartWith("DLV-");
        body.Data.SalesOrderId.Should().Be(order.Id);
        body.Data.Status.Should().Be(DeliveryStatus.Draft);
        body.Data.Id.Should().NotBeEmpty();

        _createdDeliveryIds.Add(body.Data.Id);
    }

    // ── POST /api/sales/delivery ── Maximal ───────────────────────────────────

    [Fact]
    public async Task Create_WithMaximalPayload_Returns201AndAllFieldsPersisted()
    {
        var order = await _builder.CreateSalesOrderAsync(SalesOrderStatus.Confirmed);
        _createdOrderIds.Add(order.Id);

        var orderLine = await _builder.CreateSalesOrderLineAsync(order.Id);
        var productId = orderLine.ProductId;
        var payload = new CreateDeliveryDto
        {
            SalesOrderId = order.Id,
            ContactId = Guid.NewGuid(),
            PlannedDeliveryDate = DateTime.UtcNow.AddDays(5),
            WarehouseId = Guid.NewGuid(),
            Carrier = "TCS Courier",
            ShippingMethod = "Express",
            Incoterm = Incoterm.DAP,
            IncotermLocation = "Lahore",
            RecipientName = "Ahmed Ali",
            Street = "12 Main Boulevard",
            City = "Lahore",
            State = "Punjab",
            PostalCode = "54000",
            Country = "PK",
            Lines =
            [
                new CreateDeliveryLineDto
                {
                    SalesOrderLineId = orderLine.Id,
                    DeliveredQuantity = 10,
                    BinLocation = "BIN-A3",
                    LotNumber = "LOT-2026-001",
                },
            ],
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<DeliveryDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.Carrier.Should().Be("TCS Courier");
        body.Data.RecipientName.Should().Be("Ahmed Ali");
        body.Data.City.Should().Be("Lahore");
        body.Data.Lines.Should().HaveCount(1);
        body.Data.Lines[0].BinLocation.Should().Be("BIN-A3");

        _createdDeliveryIds.Add(body.Data.Id);
    }

    // ── GET /api/sales/delivery ───────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<DeliveryDto>>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
    }

    // ── GET /api/sales/delivery/{id} ──────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingDelivery_ReturnsOkWithLines()
    {
        var order = await _builder.CreateSalesOrderAsync(SalesOrderStatus.Confirmed);
        _createdOrderIds.Add(order.Id);

        var payload = new CreateDeliveryDto
        {
            SalesOrderId = order.Id,
            PlannedDeliveryDate = DateTime.UtcNow.AddDays(2),
            Lines = [],
        };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<DeliveryDto>>())!.Data!;
        _createdDeliveryIds.Add(created.Id);

        var response = await _client.GetAsync($"{BaseUrl}/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<DeliveryDto>>();
        body!.Data!.Id.Should().Be(created.Id);
        body.Data.DeliveryNumber.Should().StartWith("DLV-");
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/sales/delivery/by-order/{salesOrderId} ──────────────────────

    [Fact]
    public async Task GetByOrder_ReturnsDeliveriesForOrder()
    {
        var order = await _builder.CreateSalesOrderAsync(SalesOrderStatus.Confirmed);
        _createdOrderIds.Add(order.Id);

        var payload = new CreateDeliveryDto
        {
            SalesOrderId = order.Id,
            PlannedDeliveryDate = DateTime.UtcNow.AddDays(2),
            Lines = [],
        };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<DeliveryDto>>())!.Data!;
        _createdDeliveryIds.Add(created.Id);

        var response = await _client.GetAsync($"{BaseUrl}/by-order/{order.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<DeliveryDto>>>();
        body!.Data.Should().Contain(d => d.Id == created.Id);
    }

    // ── POST /api/sales/delivery/{id}/ship ────────────────────────────────────

    [Fact]
    public async Task Ship_DraftDelivery_TransitionsToShipped()
    {
        var order = await _builder.CreateSalesOrderAsync(SalesOrderStatus.Confirmed);
        _createdOrderIds.Add(order.Id);

        var payload = new CreateDeliveryDto
        {
            SalesOrderId = order.Id,
            PlannedDeliveryDate = DateTime.UtcNow.AddDays(1),
            Lines = [],
        };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<DeliveryDto>>())!.Data!;
        _createdDeliveryIds.Add(created.Id);

        var shipPayload = new ShipDeliveryDto
        {
            TrackingNumber = "TCS-123456",
            Carrier = "TCS Courier",
        };

        var shipResponse = await _client.PostAsJsonAsync($"{BaseUrl}/{created.Id}/ship", shipPayload);

        shipResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await shipResponse.Content.ReadFromJsonAsync<ApiResponse<DeliveryDto>>();
        body!.Data!.Status.Should().Be(DeliveryStatus.Shipped);
        body.Data.TrackingNumber.Should().Be("TCS-123456");
    }

    // ── DELETE /api/sales/delivery/{id} ───────────────────────────────────────

    [Fact]
    public async Task Delete_DraftDelivery_ReturnsOkAndNotFound()
    {
        var order = await _builder.CreateSalesOrderAsync(SalesOrderStatus.Confirmed);
        _createdOrderIds.Add(order.Id);

        var payload = new CreateDeliveryDto
        {
            SalesOrderId = order.Id,
            PlannedDeliveryDate = DateTime.UtcNow.AddDays(1),
            Lines = [],
        };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<DeliveryDto>>())!.Data!;

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
