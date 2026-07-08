namespace Sales.Tests.Integration.Controllers;

[Collection(SalesTestCollection.Name)]
public class PosDashboardControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "api/sales/pos/dashboard";

    private readonly SalesCollectionFixture _fixture;
    private readonly HttpClient _client;

    public PosDashboardControllerTests(SalesCollectionFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() { _client.Dispose(); return Task.CompletedTask; }

    // ── GET /api/sales/pos/dashboard/branch/{branchId} ────────────────────────

    [Fact]
    public async Task GetBranchStatus_ReturnsOkWithStatus()
    {
        var response = await _client.GetAsync($"{BaseUrl}/branch/{TestJwtSettings.BranchId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PosBranchStatusDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.BranchId.Should().Be(TestJwtSettings.BranchId);
    }

    [Fact]
    public async Task GetBranchStatus_WithNoOpenSessions_ReturnsNoSessionStatus()
    {
        // Using a random branch that has no sessions
        var emptyBranchId = Guid.NewGuid();

        var response = await _client.GetAsync($"{BaseUrl}/branch/{emptyBranchId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PosBranchStatusDto>>();
        body!.Data!.Status.Should().Be(PosBranchSessionStatus.NoSession);
        body.Data.OpenSessionCount.Should().Be(0);
    }

    [Fact]
    public async Task GetBranchStatus_DataContainsTodayStats()
    {
        var response = await _client.GetAsync($"{BaseUrl}/branch/{TestJwtSettings.BranchId}");

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PosBranchStatusDto>>();
        body!.Data!.TodayTotalSales.Should().BeGreaterThanOrEqualTo(0);
        body.Data.TodayTransactionCount.Should().BeGreaterThanOrEqualTo(0);
        body.Data.RecentDailySales.Should().NotBeNull();
    }

    // ── POST /api/sales/pos/dashboard/branches ────────────────────────────────

    [Fact]
    public async Task GetBranchesStatus_SingleBranch_ReturnsOkWithResult()
    {
        var dto = new PosDashboardBulkRequestDto
        {
            BranchIds = [TestJwtSettings.BranchId],
        };

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/branches", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<PosBranchStatusDto>>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().HaveCount(1);
        body.Data![0].BranchId.Should().Be(TestJwtSettings.BranchId);
    }

    [Fact]
    public async Task GetBranchesStatus_MultipleBranches_ReturnsAllResults()
    {
        var branch1 = TestJwtSettings.BranchId;
        var branch2 = Guid.NewGuid();

        var dto = new PosDashboardBulkRequestDto
        {
            BranchIds = [branch1, branch2],
        };

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/branches", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<PosBranchStatusDto>>>();
        body!.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetBranchesStatus_EmptyList_Returns400()
    {
        var dto = new PosDashboardBulkRequestDto { BranchIds = [] };

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/branches", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Unauthenticated access ────────────────────────────────────────────────

    [Fact]
    public async Task GetBranchStatus_WithoutToken_Returns401()
    {
        var anonClient = _fixture.CreateClient();
        var response = await anonClient.GetAsync($"{BaseUrl}/branch/{TestJwtSettings.BranchId}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        anonClient.Dispose();
    }

    [Fact]
    public async Task GetBranchesStatus_WithoutToken_Returns401()
    {
        var anonClient = _fixture.CreateClient();
        var response = await anonClient.PostAsJsonAsync($"{BaseUrl}/branches",
            new PosDashboardBulkRequestDto { BranchIds = [TestJwtSettings.BranchId] });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        anonClient.Dispose();
    }
}
