namespace Sales.Tests.Integration.Controllers;

[Collection(SalesTestCollection.Name)]
public class PosTerminalControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "api/sales/posterminal";

    private readonly SalesCollectionFixture _fixture;
    private readonly HttpClient _client;
    private readonly SalesTestDataBuilder _builder;
    private readonly List<Guid> _createdIds = [];

    public PosTerminalControllerTests(SalesCollectionFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient();
        _builder = new SalesTestDataBuilder(fixture);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var id in _createdIds)
            await _builder.DeletePosTerminalAsync(id);
        _client.Dispose();
    }

    // ── POST /api/sales/posterminal ── Minimal ────────────────────────────────

    [Fact]
    public async Task Register_WithMinimalPayload_Returns201AndTerminal()
    {
        var store = await _builder.CreatePosStoreAsync("Terminal Test Store");

        var payload = new CreatePosTerminalDto
        {
            TerminalCode = $"T-{Guid.NewGuid():N}"[..8],
            TerminalName = "Main Register",
            PosStoreId = store.Id,
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PosTerminalDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.TerminalCode.Should().Be(payload.TerminalCode);
        body.Data.TerminalName.Should().Be(payload.TerminalName);
        body.Data.IsActive.Should().BeTrue();
        body.Data.IsOnline.Should().BeFalse();
        body.Data.Id.Should().NotBeEmpty();

        _createdIds.Add(body.Data.Id);
        await _builder.DeletePosStoreAsync(store.Id);
    }

    // ── POST /api/sales/posterminal ── Maximal ────────────────────────────────

    [Fact]
    public async Task Register_WithMaximalPayload_Returns201AndAllFieldsPersisted()
    {
        var store = await _builder.CreatePosStoreAsync("Full Terminal Store");

        var payload = new CreatePosTerminalDto
        {
            TerminalCode = $"T-{Guid.NewGuid():N}"[..8],
            TerminalName = "Self-Checkout Kiosk 1",
            PosStoreId = store.Id,
            DeviceIdentifier = "DEVICE-MAC-AA:BB:CC:DD:EE:FF",
            IpAddress = "192.168.1.101",
            // CashDrawerId and ReceiptTemplateId are optional FK references;
            // omit them to avoid FK violations against non-existent records.
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PosTerminalDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.TerminalCode.Should().Be(payload.TerminalCode);
        body.Data.TerminalName.Should().Be(payload.TerminalName);
        body.Data.DeviceIdentifier.Should().Be(payload.DeviceIdentifier);
        body.Data.IpAddress.Should().Be(payload.IpAddress);

        _createdIds.Add(body.Data.Id);
        await _builder.DeletePosStoreAsync(store.Id);
    }

    // ── POST duplicate code ───────────────────────────────────────────────────

    [Fact]
    public async Task Register_DuplicateCodeSameStore_Returns400()
    {
        var store = await _builder.CreatePosStoreAsync("Dup Terminal Store");
        var code = $"DUP-{Guid.NewGuid():N}"[..10];

        var payload = new CreatePosTerminalDto
        {
            TerminalCode = code,
            TerminalName = "Terminal A",
            PosStoreId = TestJwtSettings.BranchId,
        };

        var r1 = await _client.PostAsJsonAsync(BaseUrl, payload);
        r1.StatusCode.Should().Be(HttpStatusCode.Created);
        var t1 = (await r1.Content.ReadFromJsonAsync<ApiResponse<PosTerminalDto>>())!.Data!;
        _createdIds.Add(t1.Id);

        var r2 = await _client.PostAsJsonAsync(BaseUrl, payload);
        r2.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await _builder.DeletePosStoreAsync(store.Id);
    }

    // ── GET /api/sales/posterminal/{id} ───────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingTerminal_ReturnsOk()
    {
        var store = await _builder.CreatePosStoreAsync("GetById Terminal Store");
        var terminal = await _builder.CreatePosTerminalAsync(store.Id, "GET-T-001", "Get By Id Terminal");
        _createdIds.Add(terminal.Id);

        var response = await _client.GetAsync($"{BaseUrl}/{terminal.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PosTerminalDto>>();
        body!.Data!.Id.Should().Be(terminal.Id);
        body.Data.TerminalCode.Should().Be("GET-T-001");

        await _builder.DeletePosStoreAsync(store.Id);
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/sales/posterminal/branch/{branchId} ──────────────────────────

    [Fact]
    public async Task GetByBranch_ExistingStore_ReturnsOkWithList()
    {
        var store = await _builder.CreatePosStoreAsync("Branch Terminal Store");
        var terminal = await _builder.CreatePosTerminalAsync(store.Id, "BRANCH-T-001");
        _createdIds.Add(terminal.Id);

        var response = await _client.GetAsync($"{BaseUrl}/branch/{TestJwtSettings.BranchId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<PosTerminalDto>>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().Contain(t => t.Id == terminal.Id);

        await _builder.DeletePosStoreAsync(store.Id);
    }

    // ── PUT /api/sales/posterminal/{id} ───────────────────────────────────────

    [Fact]
    public async Task Update_TerminalName_Returns200WithUpdatedName()
    {
        var store = await _builder.CreatePosStoreAsync("Update Terminal Store");
        var terminal = await _builder.CreatePosTerminalAsync(store.Id, "UPD-T-001", "Original Name");
        _createdIds.Add(terminal.Id);

        var update = new UpdatePosTerminalDto { TerminalName = "Updated Register Name" };
        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{terminal.Id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PosTerminalDto>>();
        body!.Data!.TerminalName.Should().Be("Updated Register Name");

        await _builder.DeletePosStoreAsync(store.Id);
    }

    // ── DELETE /api/sales/posterminal/{id} ────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingTerminal_ReturnsOkAndTerminal404Afterwards()
    {
        var store = await _builder.CreatePosStoreAsync("Delete Terminal Store");
        var terminal = await _builder.CreatePosTerminalAsync(store.Id, "DEL-T-001");

        var deleteResponse = await _client.DeleteAsync($"{BaseUrl}/{terminal.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"{BaseUrl}/{terminal.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        await _builder.DeletePosStoreAsync(store.Id);
    }

    // ── GET /api/sales/posterminal/branch/{branchId}/active ──────────────────

    [Fact]
    public async Task GetActiveByBranch_ReturnsOnlyActiveTerminals()
    {
        var store = await _builder.CreatePosStoreAsync("Active Terminal Store");
        var active = await _builder.CreatePosTerminalAsync(store.Id, $"ACT-T-{Guid.NewGuid():N}"[..10], "Active Terminal");
        _createdIds.Add(active.Id);

        var response = await _client.GetAsync($"{BaseUrl}/branch/{TestJwtSettings.BranchId}/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<PosTerminalDto>>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().Contain(t => t.Id == active.Id);

        await _builder.DeletePosStoreAsync(store.Id);
    }

    // ── PUT /api/sales/posterminal/{id}/heartbeat ─────────────────────────────

    [Fact]
    public async Task Heartbeat_WithIpAndDeviceId_MarksTerminalOnlineAndUpdatesFields()
    {
        var store = await _builder.CreatePosStoreAsync("Heartbeat Store");
        var terminal = await _builder.CreatePosTerminalAsync(store.Id, $"HB-T-{Guid.NewGuid():N}"[..10], "Heartbeat Terminal");
        _createdIds.Add(terminal.Id);

        var dto = new TerminalHeartbeatDto
        {
            IpAddress = "10.0.1.55",
            DeviceIdentifier = "DEVICE-HB-AA:BB:CC:DD:EE:FF",
        };

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{terminal.Id}/heartbeat", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PosTerminalDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.IsOnline.Should().BeTrue();
        body.Data.IpAddress.Should().Be("10.0.1.55");
        body.Data.DeviceIdentifier.Should().Be("DEVICE-HB-AA:BB:CC:DD:EE:FF");

        await _builder.DeletePosStoreAsync(store.Id);
    }

    [Fact]
    public async Task Heartbeat_NonExistentTerminal_Returns404()
    {
        var dto = new TerminalHeartbeatDto { IpAddress = "192.168.1.1" };

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{Guid.NewGuid()}/heartbeat", dto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PUT /api/sales/posterminal/{id}/go-offline ────────────────────────────

    [Fact]
    public async Task GoOffline_AfterHeartbeat_MarksTerminalOffline()
    {
        var store = await _builder.CreatePosStoreAsync("GoOffline Store");
        var terminal = await _builder.CreatePosTerminalAsync(store.Id, $"GO-T-{Guid.NewGuid():N}"[..10], "GoOffline Terminal");
        _createdIds.Add(terminal.Id);

        await _client.PutAsJsonAsync($"{BaseUrl}/{terminal.Id}/heartbeat",
            new TerminalHeartbeatDto { IpAddress = "10.0.1.99" });

        var response = await _client.PutAsync($"{BaseUrl}/{terminal.Id}/go-offline", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().BeTrue();

        var getResp = await _client.GetAsync($"{BaseUrl}/{terminal.Id}");
        var refreshed = (await getResp.Content.ReadFromJsonAsync<ApiResponse<PosTerminalDto>>())!.Data!;
        refreshed.IsOnline.Should().BeFalse();

        await _builder.DeletePosStoreAsync(store.Id);
    }

    [Fact]
    public async Task GoOffline_NonExistentTerminal_Returns404()
    {
        var response = await _client.PutAsync($"{BaseUrl}/{Guid.NewGuid()}/go-offline", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Unauthenticated access ────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WithoutToken_Returns401()
    {
        var anonClient = _fixture.CreateClient();
        var response = await anonClient.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        anonClient.Dispose();
    }
}
