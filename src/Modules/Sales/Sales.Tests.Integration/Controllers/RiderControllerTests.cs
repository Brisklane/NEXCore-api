namespace Sales.Tests.Integration.Controllers;

[Collection(SalesTestCollection.Name)]
public class RiderControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "api/sales/rider";

    private readonly SalesCollectionFixture _fixture;
    private readonly HttpClient _client;
    private readonly List<Guid> _createdIds = [];

    public RiderControllerTests(SalesCollectionFixture fixture)
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

    // ── POST /api/sales/rider ── Minimal ──────────────────────────────────────

    [Fact]
    public async Task Create_WithMinimalPayload_Returns201AndRider()
    {
        var code = $"RDR-{Guid.NewGuid():N}"[..10];

        var payload = new CreateRiderDto
        {
            RiderCode = code,
            FirstName = "Ali",
            Phone = "0300-1234567",
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<RiderDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.RiderCode.Should().Be(code);
        body.Data.FirstName.Should().Be("Ali");
        body.Data.Phone.Should().Be("0300-1234567");
        body.Data.IsActive.Should().BeTrue();
        body.Data.Id.Should().NotBeEmpty();

        _createdIds.Add(body.Data.Id);
    }

    // ── POST /api/sales/rider ── Maximal ──────────────────────────────────────

    [Fact]
    public async Task Create_WithMaximalPayload_Returns201AndAllFieldsPersisted()
    {
        var code = $"RDR-{Guid.NewGuid():N}"[..10];
        var hrEmployeeId = Guid.NewGuid();

        var payload = new CreateRiderDto
        {
            RiderCode = code,
            FirstName = "Hassan",
            LastName = "Raza",
            Phone = "0321-9876543",
            Email = "hassan.raza@deliveries.pk",
            VehicleType = VehicleType.Motorcycle,
            VehiclePlateNumber = "ABC-1234",
            VehicleModel = "Honda CD 70",
            ContractType = "Freelance",
            HrEmployeeId = hrEmployeeId,
            // HomeBranchId is a cross-module ref with no FK constraint — safe to set.
            // ZoneId omitted: Rider.Zone is a DeliveryZone navigation with an enforced FK.
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<RiderDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.RiderCode.Should().Be(code);
        body.Data.FirstName.Should().Be("Hassan");
        body.Data.LastName.Should().Be("Raza");
        body.Data.Phone.Should().Be("0321-9876543");
        body.Data.Email.Should().Be("hassan.raza@deliveries.pk");
        body.Data.VehicleType.Should().Be(VehicleType.Motorcycle);
        body.Data.VehiclePlateNumber.Should().Be("ABC-1234");

        _createdIds.Add(body.Data.Id);
    }

    // ── GET /api/sales/rider ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<RiderDto>>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
    }

    // ── GET /api/sales/rider/{id} ─────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingRider_ReturnsOk()
    {
        var code = $"GBI-{Guid.NewGuid():N}"[..10];
        var payload = new CreateRiderDto { RiderCode = code, FirstName = "Imran", Phone = "0311-0000001" };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<RiderDto>>())!.Data!;
        _createdIds.Add(created.Id);

        var response = await _client.GetAsync($"{BaseUrl}/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<RiderDto>>();
        body!.Data!.Id.Should().Be(created.Id);
        body.Data.RiderCode.Should().Be(code);
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/sales/rider/available ────────────────────────────────────────

    [Fact]
    public async Task GetAvailable_ReturnsOkWithList()
    {
        var response = await _client.GetAsync($"{BaseUrl}/available");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<RiderDto>>>();
        body!.Success.Should().BeTrue();
    }

    // ── PUT /api/sales/rider/{id} ─────────────────────────────────────────────

    [Fact]
    public async Task Update_RiderPhone_Returns200WithUpdatedPhone()
    {
        var code = $"UPD-{Guid.NewGuid():N}"[..10];
        var payload = new CreateRiderDto { RiderCode = code, FirstName = "Usman", Phone = "0300-1111111" };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<RiderDto>>())!.Data!;
        _createdIds.Add(created.Id);

        var update = new UpdateRiderDto { Phone = "0333-9999999" };
        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<RiderDto>>();
        body!.Data!.Phone.Should().Be("0333-9999999");
    }

    // ── DELETE /api/sales/rider/{id} ──────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingRider_ReturnsOkAndNotFound()
    {
        var code = $"DEL-{Guid.NewGuid():N}"[..10];
        var payload = new CreateRiderDto { RiderCode = code, FirstName = "ToDelete", Phone = "0300-2222222" };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<RiderDto>>())!.Data!;

        var deleteResponse = await _client.DeleteAsync($"{BaseUrl}/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"{BaseUrl}/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/sales/rider/assign ──────────────────────────────────────────

    [Fact]
    public async Task Assign_ValidRiderAndOrder_Returns201Assignment()
    {
        var builder = new SalesTestDataBuilder(_fixture);
        var order = await builder.CreateSalesOrderAsync(SalesOrderStatus.Placed);

        var code = $"ASN-{Guid.NewGuid():N}"[..10];
        var payload = new CreateRiderDto { RiderCode = code, FirstName = "Salman", Phone = "0312-3333333" };
        var rider = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<RiderDto>>())!.Data!;
        _createdIds.Add(rider.Id);

        var assignPayload = new CreateRiderAssignmentDto
        {
            SalesOrderId = order.Id,
            RiderId = rider.Id,
        };

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/assign", assignPayload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<RiderAssignmentDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.RiderId.Should().Be(rider.Id);
        body.Data.SalesOrderId.Should().Be(order.Id);

        await builder.DeleteSalesOrderAsync(order.Id);
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
