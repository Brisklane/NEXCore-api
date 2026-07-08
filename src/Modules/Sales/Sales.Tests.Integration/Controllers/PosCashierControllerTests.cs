namespace Sales.Tests.Integration.Controllers;

[Collection(SalesTestCollection.Name)]
public class PosCashierControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "api/sales/poscashier";

    private readonly SalesCollectionFixture _fixture;
    private readonly HttpClient _client;
    private readonly SalesTestDataBuilder _builder;
    private readonly List<Guid> _createdCashierIds = [];
    private readonly List<Guid> _createdTerminalIds = [];

    public PosCashierControllerTests(SalesCollectionFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient();
        _builder = new SalesTestDataBuilder(fixture);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var id in _createdCashierIds)
            await _builder.DeletePosCashierAsync(id);
        foreach (var id in _createdTerminalIds)
            await _builder.DeletePosTerminalAsync(id);
        _client.Dispose();
    }

    // ── POST /api/sales/poscashier ── Minimal ─────────────────────────────────

    [Fact]
    public async Task Create_WithMinimalPayload_Returns201AndCashier()
    {
        var payload = new CreatePosCashierDto
        {
            EmployeeId = Guid.NewGuid(),
            DisplayName = "Jane Smith",
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PosCashierDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.EmployeeId.Should().Be(payload.EmployeeId);
        body.Data.DisplayName.Should().Be(payload.DisplayName);
        body.Data.IsActive.Should().BeTrue();
        body.Data.Id.Should().NotBeEmpty();

        _createdCashierIds.Add(body.Data.Id);
    }

    // ── POST /api/sales/poscashier ── Maximal ─────────────────────────────────

    [Fact]
    public async Task Create_WithMaximalPayload_Returns201AndAllPermissionsPersisted()
    {
        var payload = new CreatePosCashierDto
        {
            EmployeeId = Guid.NewGuid(),
            DisplayName = "Senior Manager Cashier",
            BadgeNumber = "BADGE-007",
            PosStoreId = _fixture.SharedStore.Id,
            CanApplyManualDiscount = true,
            MaxManualDiscountPercentage = 20m,
            CanVoidTransaction = true,
            CanIssueRefund = true,
            CanOpenDrawer = true,
            CanOverridePrices = true,
            CanApplyCoupons = true,
            CanAccessReports = true,
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PosCashierDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.DisplayName.Should().Be(payload.DisplayName);
        body.Data.BadgeNumber.Should().Be(payload.BadgeNumber);
        body.Data.CanApplyManualDiscount.Should().BeTrue();
        body.Data.MaxManualDiscountPercentage.Should().Be(20m);
        body.Data.CanVoidTransaction.Should().BeTrue();
        body.Data.CanIssueRefund.Should().BeTrue();
        body.Data.CanOpenDrawer.Should().BeTrue();
        body.Data.CanOverridePrices.Should().BeTrue();
        body.Data.CanApplyCoupons.Should().BeTrue();
        body.Data.CanAccessReports.Should().BeTrue();

        _createdCashierIds.Add(body.Data.Id);
    }

    // ── GET /api/sales/poscashier/{id} ────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingCashier_ReturnsOk()
    {
        var cashier = await _builder.CreatePosCashierAsync("GetById Cashier");
        _createdCashierIds.Add(cashier.Id);

        var response = await _client.GetAsync($"{BaseUrl}/{cashier.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PosCashierDto>>();
        body!.Data!.Id.Should().Be(cashier.Id);
        body.Data.DisplayName.Should().Be("GetById Cashier");
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PUT /api/sales/poscashier/{id} ────────────────────────────────────────

    [Fact]
    public async Task Update_DisplayName_Returns200WithUpdatedName()
    {
        var cashier = await _builder.CreatePosCashierAsync("Original Cashier");
        _createdCashierIds.Add(cashier.Id);

        var update = new UpdatePosCashierDto { DisplayName = "Updated Cashier Name" };
        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{cashier.Id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PosCashierDto>>();
        body!.Data!.DisplayName.Should().Be("Updated Cashier Name");
    }

    [Fact]
    public async Task Update_DeactivateCashier_PersistsChange()
    {
        var cashier = await _builder.CreatePosCashierAsync("Active Cashier");
        _createdCashierIds.Add(cashier.Id);

        var update = new UpdatePosCashierDto { IsActive = false };
        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{cashier.Id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PosCashierDto>>();
        body!.Data!.IsActive.Should().BeFalse();
    }

    // ── POST /api/sales/poscashier/check-in ───────────────────────────────────

    [Fact]
    public async Task CheckIn_WithMinimalPayload_Returns201AndOpenSession()
    {
        var store = await _builder.CreatePosStoreAsync("CheckIn Store");
        var cashier = await _builder.CreatePosCashierAsync("CheckIn Cashier");
        var terminal = await _builder.CreatePosTerminalAsync(store.Id, $"CI-T-{Guid.NewGuid():N}"[..10]);
        _createdCashierIds.Add(cashier.Id);
        _createdTerminalIds.Add(terminal.Id);

        var checkIn = new CashierCheckInDto
        {
            CashierId = cashier.Id,
            TerminalId = terminal.Id,
            OpeningFloat = 500m,
        };

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/check-in", checkIn);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PosSessionDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.PosCashierId.Should().Be(cashier.Id);
        body.Data.PosTerminalId.Should().Be(terminal.Id);
        body.Data.OpeningFloat.Should().Be(500m);
        body.Data.SessionNumber.Should().StartWith("POSS-");

        await _builder.DeletePosStoreAsync(store.Id);
    }

    [Fact]
    public async Task CheckIn_WithMaximalPayload_Returns201AndSessionWithDenominations()
    {
        var store = await _builder.CreatePosStoreAsync("CheckIn Max Store");
        var cashier = await _builder.CreatePosCashierAsync("CheckIn Max Cashier");
        var terminal = await _builder.CreatePosTerminalAsync(store.Id, $"CIM-T-{Guid.NewGuid():N}"[..10]);
        _createdCashierIds.Add(cashier.Id);
        _createdTerminalIds.Add(terminal.Id);

        var checkIn = new CashierCheckInDto
        {
            CashierId = cashier.Id,
            TerminalId = terminal.Id,
            OpeningFloat = 0m,
            OpeningNotes = "Morning opening float",
            Denominations =
            [
                new DenominationCountDto { Denomination = 100m, Count = 3 },
                new DenominationCountDto { Denomination = 50m, Count = 4 },
                new DenominationCountDto { Denomination = 10m, Count = 10 },
            ],
        };

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/check-in", checkIn);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PosSessionDto>>();
        body!.Data!.OpeningFloat.Should().Be(600m); // 3*100 + 4*50 + 10*10
        body.Data.OpeningNotes.Should().Be("Morning opening float");

        await _builder.DeletePosStoreAsync(store.Id);
    }

    [Fact]
    public async Task CheckIn_DuplicateOpenSession_Returns400()
    {
        var store = await _builder.CreatePosStoreAsync("DupCheckIn Store");
        var cashier = await _builder.CreatePosCashierAsync("DupCheckIn Cashier");
        var terminal = await _builder.CreatePosTerminalAsync(store.Id, $"DCI-T-{Guid.NewGuid():N}"[..10]);
        _createdCashierIds.Add(cashier.Id);
        _createdTerminalIds.Add(terminal.Id);

        var checkIn = new CashierCheckInDto
        {
            CashierId = cashier.Id,
            TerminalId = terminal.Id,
            OpeningFloat = 200m,
        };

        var r1 = await _client.PostAsJsonAsync($"{BaseUrl}/check-in", checkIn);
        r1.StatusCode.Should().Be(HttpStatusCode.Created);

        var r2 = await _client.PostAsJsonAsync($"{BaseUrl}/check-in", checkIn);
        r2.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await _builder.DeletePosStoreAsync(store.Id);
    }

    // ── DELETE /api/sales/poscashier/{id} ─────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingCashier_ReturnsOkAndCashier404Afterwards()
    {
        var cashier = await _builder.CreatePosCashierAsync("To Be Deleted Cashier");

        var deleteResponse = await _client.DeleteAsync($"{BaseUrl}/{cashier.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"{BaseUrl}/{cashier.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/sales/poscashier/store/{storeId} ─────────────────────────────

    [Fact]
    public async Task GetByStore_ExistingCashier_ReturnsListContainingCashier()
    {
        var cashier = await _builder.CreatePosCashierAsync("GetByStore Cashier");
        _createdCashierIds.Add(cashier.Id);

        var response = await _client.GetAsync($"{BaseUrl}/store/{TestJwtSettings.BranchId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<PosCashierDto>>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data.Should().Contain(c => c.Id == cashier.Id);
    }

    // ── PUT /api/sales/poscashier/{id}/set-pin ────────────────────────────────

    [Fact]
    public async Task SetPin_WithValidFourDigitPin_Returns200()
    {
        var cashier = await _builder.CreatePosCashierAsync("SetPin Cashier");
        _createdCashierIds.Add(cashier.Id);

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{cashier.Id}/set-pin",
            new SetCashierPinDto { Pin = "1234" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().BeTrue();
    }

    [Fact]
    public async Task SetPin_WithValidSixDigitPin_Returns200()
    {
        var cashier = await _builder.CreatePosCashierAsync("SetPin6 Cashier");
        _createdCashierIds.Add(cashier.Id);

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{cashier.Id}/set-pin",
            new SetCashierPinDto { Pin = "987654" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        body!.Data.Should().BeTrue();
    }

    [Fact]
    public async Task SetPin_TooShort_Returns400()
    {
        var cashier = await _builder.CreatePosCashierAsync("SetPin Short");
        _createdCashierIds.Add(cashier.Id);

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{cashier.Id}/set-pin",
            new SetCashierPinDto { Pin = "12" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetPin_NonNumericChars_Returns400()
    {
        var cashier = await _builder.CreatePosCashierAsync("SetPin Alpha");
        _createdCashierIds.Add(cashier.Id);

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{cashier.Id}/set-pin",
            new SetCashierPinDto { Pin = "ABCD" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── PUT /api/sales/poscashier/{id}/change-pin ─────────────────────────────

    [Fact]
    public async Task ChangePin_WithCorrectCurrentPin_Returns200()
    {
        var cashier = await _builder.CreatePosCashierAsync("ChangePin Cashier");
        _createdCashierIds.Add(cashier.Id);

        await _client.PutAsJsonAsync($"{BaseUrl}/{cashier.Id}/set-pin", new SetCashierPinDto { Pin = "1234" });

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{cashier.Id}/change-pin",
            new ChangeCashierPinDto { CurrentPin = "1234", NewPin = "5678" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().BeTrue();
    }

    [Fact]
    public async Task ChangePin_WithWrongCurrentPin_Returns400()
    {
        var cashier = await _builder.CreatePosCashierAsync("ChangePin Wrong");
        _createdCashierIds.Add(cashier.Id);

        await _client.PutAsJsonAsync($"{BaseUrl}/{cashier.Id}/set-pin", new SetCashierPinDto { Pin = "1234" });

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{cashier.Id}/change-pin",
            new ChangeCashierPinDto { CurrentPin = "9999", NewPin = "5678" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── POST /api/sales/poscashier/pin-login ──────────────────────────────────

    [Fact]
    public async Task PinLogin_WithCorrectPin_Returns200AndCashierProfile()
    {
        var cashier = await _builder.CreatePosCashierAsync("PinLogin Cashier");
        _createdCashierIds.Add(cashier.Id);

        await _client.PutAsJsonAsync($"{BaseUrl}/{cashier.Id}/set-pin", new SetCashierPinDto { Pin = "4321" });

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/pin-login",
            new CashierPinLoginDto { Pin = "4321", StoreId = _fixture.SharedStore.Id });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PosCashierDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.Id.Should().Be(cashier.Id);
        body.Data.DisplayName.Should().Be("PinLogin Cashier");
    }

    [Fact]
    public async Task PinLogin_WithWrongPin_Returns401()
    {
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/pin-login",
            new CashierPinLoginDto { Pin = "0000", StoreId = _fixture.SharedStore.Id });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST /api/sales/poscashier/check-out/{sessionId} ─────────────────────

    [Fact]
    public async Task CheckOut_WithMinimalPayload_Returns200AndClosedSession()
    {
        var store = await _builder.CreatePosStoreAsync("CheckOut Store");
        var cashier = await _builder.CreatePosCashierAsync("CheckOut Cashier");
        var terminal = await _builder.CreatePosTerminalAsync(store.Id, $"CO-T-{Guid.NewGuid():N}"[..10]);
        _createdCashierIds.Add(cashier.Id);
        _createdTerminalIds.Add(terminal.Id);

        var checkIn = new CashierCheckInDto { CashierId = cashier.Id, TerminalId = terminal.Id, OpeningFloat = 300m };
        var checkInResp = await _client.PostAsJsonAsync($"{BaseUrl}/check-in", checkIn);
        var session = (await checkInResp.Content.ReadFromJsonAsync<ApiResponse<PosSessionDto>>())!.Data!;

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/check-out/{session.Id}",
            new CashierCheckOutDto { ClosingFloat = 300m });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PosSessionDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.Status.Should().Be(PosSessionStatus.Closed);
        body.Data.ClosingFloat.Should().Be(300m);
        body.Data.FloatVariance.Should().Be(0m);
        body.Data.ClosedAt.Should().NotBeNull();

        await _builder.DeletePosStoreAsync(store.Id);
    }

    [Fact]
    public async Task CheckOut_WithDenominationsPayload_Returns200AndCalculatesFloat()
    {
        var store = await _builder.CreatePosStoreAsync("CheckOut Denom Store");
        var cashier = await _builder.CreatePosCashierAsync("CheckOut Denom Cashier");
        var terminal = await _builder.CreatePosTerminalAsync(store.Id, $"COD-T-{Guid.NewGuid():N}"[..10]);
        _createdCashierIds.Add(cashier.Id);
        _createdTerminalIds.Add(terminal.Id);

        var checkIn = new CashierCheckInDto { CashierId = cashier.Id, TerminalId = terminal.Id, OpeningFloat = 500m };
        var checkInResp = await _client.PostAsJsonAsync($"{BaseUrl}/check-in", checkIn);
        var session = (await checkInResp.Content.ReadFromJsonAsync<ApiResponse<PosSessionDto>>())!.Data!;

        var checkOut = new CashierCheckOutDto
        {
            Notes = "End of day — all cash counted",
            Denominations =
            [
                new DenominationCountDto { Denomination = 500m, Count = 1 },
                new DenominationCountDto { Denomination = 100m, Count = 3 },
                new DenominationCountDto { Denomination = 50m, Count = 4 },
                new DenominationCountDto { Denomination = 10m, Count = 10 },
                new DenominationCountDto { Denomination = 1m, Count = 5, IsCoin = true },
            ],
        };

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/check-out/{session.Id}", checkOut);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PosSessionDto>>();
        body!.Data!.ClosingFloat.Should().Be(1105m); // 500+(100×3)+(50×4)+(10×10)+(1×5)
        body.Data.ClosingNotes.Should().Be("End of day — all cash counted");
        body.Data.Status.Should().Be(PosSessionStatus.Closed);

        await _builder.DeletePosStoreAsync(store.Id);
    }

    [Fact]
    public async Task CheckOut_AlreadyClosedSession_Returns400()
    {
        var store = await _builder.CreatePosStoreAsync("CheckOut Closed Store");
        var cashier = await _builder.CreatePosCashierAsync("CheckOut Closed Cashier");
        var terminal = await _builder.CreatePosTerminalAsync(store.Id, $"COC-T-{Guid.NewGuid():N}"[..10]);
        _createdCashierIds.Add(cashier.Id);
        _createdTerminalIds.Add(terminal.Id);

        var checkIn = new CashierCheckInDto { CashierId = cashier.Id, TerminalId = terminal.Id, OpeningFloat = 100m };
        var checkInResp = await _client.PostAsJsonAsync($"{BaseUrl}/check-in", checkIn);
        var session = (await checkInResp.Content.ReadFromJsonAsync<ApiResponse<PosSessionDto>>())!.Data!;

        await _client.PostAsJsonAsync($"{BaseUrl}/check-out/{session.Id}", new CashierCheckOutDto { ClosingFloat = 100m });

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/check-out/{session.Id}",
            new CashierCheckOutDto { ClosingFloat = 100m });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await _builder.DeletePosStoreAsync(store.Id);
    }

    // ── GET /api/sales/poscashier/open-session ────────────────────────────────

    [Fact]
    public async Task GetOpenSession_AfterCheckIn_ReturnsOpenSession()
    {
        var store = await _builder.CreatePosStoreAsync("OpenSess Store");
        var cashier = await _builder.CreatePosCashierAsync("OpenSess Cashier");
        var terminal = await _builder.CreatePosTerminalAsync(store.Id, $"OS-T-{Guid.NewGuid():N}"[..10]);
        _createdCashierIds.Add(cashier.Id);
        _createdTerminalIds.Add(terminal.Id);

        await _client.PostAsJsonAsync($"{BaseUrl}/check-in",
            new CashierCheckInDto { CashierId = cashier.Id, TerminalId = terminal.Id, OpeningFloat = 200m });

        var response = await _client.GetAsync(
            $"{BaseUrl}/open-session?cashierId={cashier.Id}&terminalId={terminal.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PosSessionDto>>();
        body!.Data!.PosCashierId.Should().Be(cashier.Id);
        body.Data.PosTerminalId.Should().Be(terminal.Id);
        body.Data.Status.Should().Be(PosSessionStatus.Open);

        await _builder.DeletePosStoreAsync(store.Id);
    }

    [Fact]
    public async Task GetOpenSession_NoSessionExists_Returns404()
    {
        var response = await _client.GetAsync(
            $"{BaseUrl}/open-session?cashierId={Guid.NewGuid()}&terminalId={Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/sales/poscashier/sessions/by-terminal/{terminalId} ───────────

    [Fact]
    public async Task GetSessionsByTerminal_AfterCheckIn_ContainsSession()
    {
        var store = await _builder.CreatePosStoreAsync("ByTerminal Store");
        var cashier = await _builder.CreatePosCashierAsync("ByTerminal Cashier");
        var terminal = await _builder.CreatePosTerminalAsync(store.Id, $"BT-T-{Guid.NewGuid():N}"[..10]);
        _createdCashierIds.Add(cashier.Id);
        _createdTerminalIds.Add(terminal.Id);

        var checkInResp = await _client.PostAsJsonAsync($"{BaseUrl}/check-in",
            new CashierCheckInDto { CashierId = cashier.Id, TerminalId = terminal.Id, OpeningFloat = 150m });
        var session = (await checkInResp.Content.ReadFromJsonAsync<ApiResponse<PosSessionDto>>())!.Data!;

        var response = await _client.GetAsync($"{BaseUrl}/sessions/by-terminal/{terminal.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<PosSessionDto>>>();
        body!.Data.Should().Contain(s => s.Id == session.Id);

        await _builder.DeletePosStoreAsync(store.Id);
    }

    // ── GET /api/sales/poscashier/sessions/by-date ────────────────────────────

    [Fact]
    public async Task GetSessionsByDate_Today_ContainsTodaysSession()
    {
        var store = await _builder.CreatePosStoreAsync("ByDate Store");
        var cashier = await _builder.CreatePosCashierAsync("ByDate Cashier");
        var terminal = await _builder.CreatePosTerminalAsync(store.Id, $"BD-T-{Guid.NewGuid():N}"[..10]);
        _createdCashierIds.Add(cashier.Id);
        _createdTerminalIds.Add(terminal.Id);

        var checkInResp = await _client.PostAsJsonAsync($"{BaseUrl}/check-in",
            new CashierCheckInDto { CashierId = cashier.Id, TerminalId = terminal.Id, OpeningFloat = 100m });
        var session = (await checkInResp.Content.ReadFromJsonAsync<ApiResponse<PosSessionDto>>())!.Data!;

        var today = DateTime.UtcNow.Date;
        var response = await _client.GetAsync(
            $"{BaseUrl}/sessions/by-date?date={today:yyyy-MM-dd}&storeId={TestJwtSettings.BranchId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<PosSessionDto>>>();
        body!.Data.Should().Contain(s => s.Id == session.Id);

        await _builder.DeletePosStoreAsync(store.Id);
    }

    // ── POST /api/sales/poscashier/sessions/{sessionId}/cash-in ──────────────

    [Fact]
    public async Task CashIn_WithFullPayload_Returns201AndCashInMovement()
    {
        var store = await _builder.CreatePosStoreAsync("CashIn Store");
        var cashier = await _builder.CreatePosCashierAsync("CashIn Cashier");
        var terminal = await _builder.CreatePosTerminalAsync(store.Id, $"CI2-T-{Guid.NewGuid():N}"[..10]);
        _createdCashierIds.Add(cashier.Id);
        _createdTerminalIds.Add(terminal.Id);

        var checkInResp = await _client.PostAsJsonAsync($"{BaseUrl}/check-in",
            new CashierCheckInDto { CashierId = cashier.Id, TerminalId = terminal.Id, OpeningFloat = 200m });
        var session = (await checkInResp.Content.ReadFromJsonAsync<ApiResponse<PosSessionDto>>())!.Data!;

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/sessions/{session.Id}/cash-in",
            new CashMovementDto
            {
                CashierId = cashier.Id,
                Amount = 500m,
                Reason = "Change float top-up from safe",
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PosCashMovementDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.Amount.Should().Be(500m);
        body.Data.Reason.Should().Be("Change float top-up from safe");
        body.Data.MovementType.Should().Be(PosCashMovementType.CashIn);
        body.Data.PosSessionId.Should().Be(session.Id);

        await _builder.DeletePosStoreAsync(store.Id);
    }

    [Fact]
    public async Task CashIn_ZeroAmount_Returns400()
    {
        var store = await _builder.CreatePosStoreAsync("CashIn Zero Store");
        var cashier = await _builder.CreatePosCashierAsync("CashIn Zero Cashier");
        var terminal = await _builder.CreatePosTerminalAsync(store.Id, $"CIZ-T-{Guid.NewGuid():N}"[..10]);
        _createdCashierIds.Add(cashier.Id);
        _createdTerminalIds.Add(terminal.Id);

        var checkInResp = await _client.PostAsJsonAsync($"{BaseUrl}/check-in",
            new CashierCheckInDto { CashierId = cashier.Id, TerminalId = terminal.Id, OpeningFloat = 100m });
        var session = (await checkInResp.Content.ReadFromJsonAsync<ApiResponse<PosSessionDto>>())!.Data!;

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/sessions/{session.Id}/cash-in",
            new CashMovementDto { CashierId = cashier.Id, Amount = 0m, Reason = "Invalid zero" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await _builder.DeletePosStoreAsync(store.Id);
    }

    // ── POST /api/sales/poscashier/sessions/{sessionId}/cash-out ─────────────

    [Fact]
    public async Task CashOut_WithFullPayload_Returns201AndCashOutMovement()
    {
        var store = await _builder.CreatePosStoreAsync("CashOut Store");
        var cashier = await _builder.CreatePosCashierAsync("CashOut Cashier");
        var terminal = await _builder.CreatePosTerminalAsync(store.Id, $"CO3-T-{Guid.NewGuid():N}"[..10]);
        _createdCashierIds.Add(cashier.Id);
        _createdTerminalIds.Add(terminal.Id);

        var checkInResp = await _client.PostAsJsonAsync($"{BaseUrl}/check-in",
            new CashierCheckInDto { CashierId = cashier.Id, TerminalId = terminal.Id, OpeningFloat = 1000m });
        var session = (await checkInResp.Content.ReadFromJsonAsync<ApiResponse<PosSessionDto>>())!.Data!;

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/sessions/{session.Id}/cash-out",
            new CashMovementDto
            {
                CashierId = cashier.Id,
                Amount = 400m,
                Reason = "Safe drop — mid-day",
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PosCashMovementDto>>();
        body!.Data!.Amount.Should().Be(400m);
        body.Data.MovementType.Should().Be(PosCashMovementType.CashOut);
        body.Data.Reason.Should().Be("Safe drop — mid-day");

        await _builder.DeletePosStoreAsync(store.Id);
    }

    // ── GET /api/sales/poscashier/sessions/{sessionId}/cash-movements ─────────

    [Fact]
    public async Task GetCashMovements_AfterCashInAndCashOut_ReturnsBothMovements()
    {
        var store = await _builder.CreatePosStoreAsync("Movements Store");
        var cashier = await _builder.CreatePosCashierAsync("Movements Cashier");
        var terminal = await _builder.CreatePosTerminalAsync(store.Id, $"MV-T-{Guid.NewGuid():N}"[..10]);
        _createdCashierIds.Add(cashier.Id);
        _createdTerminalIds.Add(terminal.Id);

        var checkInResp = await _client.PostAsJsonAsync($"{BaseUrl}/check-in",
            new CashierCheckInDto { CashierId = cashier.Id, TerminalId = terminal.Id, OpeningFloat = 500m });
        var session = (await checkInResp.Content.ReadFromJsonAsync<ApiResponse<PosSessionDto>>())!.Data!;

        await _client.PostAsJsonAsync($"{BaseUrl}/sessions/{session.Id}/cash-in",
            new CashMovementDto { CashierId = cashier.Id, Amount = 200m, Reason = "Petty cash in" });
        await _client.PostAsJsonAsync($"{BaseUrl}/sessions/{session.Id}/cash-out",
            new CashMovementDto { CashierId = cashier.Id, Amount = 100m, Reason = "Safe drop" });

        var response = await _client.GetAsync($"{BaseUrl}/sessions/{session.Id}/cash-movements");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<PosCashMovementDto>>>();
        body!.Data.Should().HaveCount(2);
        body.Data.Should().Contain(m => m.MovementType == PosCashMovementType.CashIn && m.Amount == 200m);
        body.Data.Should().Contain(m => m.MovementType == PosCashMovementType.CashOut && m.Amount == 100m);

        await _builder.DeletePosStoreAsync(store.Id);
    }

    [Fact]
    public async Task GetCashMovements_ClosedSessionWithNoMovements_ReturnsEmptyList()
    {
        var store = await _builder.CreatePosStoreAsync("NoMovements Store");
        var cashier = await _builder.CreatePosCashierAsync("NoMovements Cashier");
        var terminal = await _builder.CreatePosTerminalAsync(store.Id, $"NM-T-{Guid.NewGuid():N}"[..10]);
        _createdCashierIds.Add(cashier.Id);
        _createdTerminalIds.Add(terminal.Id);

        var checkInResp = await _client.PostAsJsonAsync($"{BaseUrl}/check-in",
            new CashierCheckInDto { CashierId = cashier.Id, TerminalId = terminal.Id, OpeningFloat = 300m });
        var session = (await checkInResp.Content.ReadFromJsonAsync<ApiResponse<PosSessionDto>>())!.Data!;

        await _client.PostAsJsonAsync($"{BaseUrl}/check-out/{session.Id}",
            new CashierCheckOutDto { ClosingFloat = 300m });

        var response = await _client.GetAsync($"{BaseUrl}/sessions/{session.Id}/cash-movements");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<PosCashMovementDto>>>();
        body!.Data.Should().BeEmpty();

        await _builder.DeletePosStoreAsync(store.Id);
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
