namespace Sales.Tests.Integration.Controllers;

[Collection(SalesTestCollection.Name)]
public class SalesLookupControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "api/sales/saleslookup";

    private readonly SalesCollectionFixture _fixture;
    private readonly HttpClient _client;

    public SalesLookupControllerTests(SalesCollectionFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() { _client.Dispose(); return Task.CompletedTask; }

    // ── Sales Order ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderStatuses_ReturnsOkWithValues()
    {
        var response = await _client.GetAsync($"{BaseUrl}/order-statuses");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<LookupItemDto>>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetSalesChannels_ReturnsOkWithValues()
    {
        var response = await _client.GetAsync($"{BaseUrl}/sales-channels");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<LookupItemDto>>>();
        body!.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPaymentTerms_ReturnsOkWithValues()
    {
        var response = await _client.GetAsync($"{BaseUrl}/payment-terms");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<LookupItemDto>>>();
        body!.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetFulfillmentTypes_ReturnsOkWithValues()
    {
        var response = await _client.GetAsync($"{BaseUrl}/fulfillment-types");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<LookupItemDto>>>();
        body!.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetDiscountTypes_ReturnsOkWithValues()
    {
        var response = await _client.GetAsync($"{BaseUrl}/discount-types");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<LookupItemDto>>>();
        body!.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetIncoterms_ReturnsOkWithValues()
    {
        var response = await _client.GetAsync($"{BaseUrl}/incoterms");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<LookupItemDto>>>();
        body!.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetTaxCategories_ReturnsOkWithValues()
    {
        var response = await _client.GetAsync($"{BaseUrl}/tax-categories");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<LookupItemDto>>>();
        body!.Data.Should().NotBeEmpty();
    }

    // ── Quotation / Invoice / Delivery ────────────────────────────────────────

    [Fact]
    public async Task GetQuotationStatuses_ReturnsOkWithValues()
    {
        var response = await _client.GetAsync($"{BaseUrl}/quotation-statuses");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<LookupItemDto>>>();
        body!.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetInvoiceStatuses_ReturnsOkWithValues()
    {
        var response = await _client.GetAsync($"{BaseUrl}/invoice-statuses");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<LookupItemDto>>>();
        body!.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetDeliveryStatuses_ReturnsOkWithValues()
    {
        var response = await _client.GetAsync($"{BaseUrl}/delivery-statuses");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<LookupItemDto>>>();
        body!.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPriceListTypes_ReturnsOkWithValues()
    {
        var response = await _client.GetAsync($"{BaseUrl}/price-list-types");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<LookupItemDto>>>();
        body!.Data.Should().NotBeEmpty();
    }

    // ── Rider ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRiderStatuses_ReturnsOkWithValues()
    {
        var response = await _client.GetAsync($"{BaseUrl}/rider-statuses");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<LookupItemDto>>>();
        body!.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetVehicleTypes_ReturnsOkWithValues()
    {
        var response = await _client.GetAsync($"{BaseUrl}/vehicle-types");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<LookupItemDto>>>();
        body!.Data.Should().NotBeEmpty();
    }

    // ── POS ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPosStoreTypes_ReturnsOkWithValues()
    {
        var response = await _client.GetAsync($"{BaseUrl}/pos-store-types");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<LookupItemDto>>>();
        body!.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPosSessionStatuses_ReturnsOkWithValues()
    {
        var response = await _client.GetAsync($"{BaseUrl}/pos-session-statuses");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<LookupItemDto>>>();
        body!.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetStoreOnlineStatuses_ReturnsOkWithValues()
    {
        var response = await _client.GetAsync($"{BaseUrl}/store-online-statuses");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<LookupItemDto>>>();
        body!.Data.Should().NotBeEmpty();
    }

    // ── Promotions ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPromotionStatuses_ReturnsOkWithValues()
    {
        var response = await _client.GetAsync($"{BaseUrl}/promotion-statuses");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<LookupItemDto>>>();
        body!.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPromotionDiscountTypes_ReturnsOkWithValues()
    {
        var response = await _client.GetAsync($"{BaseUrl}/promotion-discount-types");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<LookupItemDto>>>();
        body!.Data.Should().NotBeEmpty();
    }

    // ── Coupon / Loyalty / Commission ─────────────────────────────────────────

    [Fact]
    public async Task GetCouponStatuses_ReturnsOkWithValues()
    {
        var response = await _client.GetAsync($"{BaseUrl}/coupon-statuses");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<LookupItemDto>>>();
        body!.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetLoyaltyTiers_ReturnsOkWithValues()
    {
        var response = await _client.GetAsync($"{BaseUrl}/loyalty-tiers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<LookupItemDto>>>();
        body!.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetVendorOnboardingStatuses_ReturnsOkWithValues()
    {
        var response = await _client.GetAsync($"{BaseUrl}/vendor-onboarding-statuses");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<LookupItemDto>>>();
        body!.Data.Should().NotBeEmpty();
    }

    // ── Unauthenticated access ────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderStatuses_WithoutToken_Returns401()
    {
        var anonClient = _fixture.CreateClient();
        var response = await anonClient.GetAsync($"{BaseUrl}/order-statuses");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        anonClient.Dispose();
    }
}
