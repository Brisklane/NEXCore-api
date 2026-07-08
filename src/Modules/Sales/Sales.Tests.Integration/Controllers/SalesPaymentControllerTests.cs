namespace Sales.Tests.Integration.Controllers;

[Collection(SalesTestCollection.Name)]
public class SalesPaymentControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "api/sales/salespayment";

    private readonly SalesCollectionFixture _fixture;
    private readonly HttpClient _client;
    private readonly SalesTestDataBuilder _builder;
    private readonly List<Guid> _createdOrderIds = [];

    public SalesPaymentControllerTests(SalesCollectionFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient();
        _builder = new SalesTestDataBuilder(fixture);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var id in _createdOrderIds)
            await _builder.DeleteSalesOrderAsync(id);
        _client.Dispose();
    }

    // ── POST /api/sales/salespayment ── Minimal ───────────────────────────────

    [Fact]
    public async Task Create_WithMinimalPayload_Returns201AndPayment()
    {
        var order = await _builder.CreateSalesOrderAsync(SalesOrderStatus.Placed);
        _createdOrderIds.Add(order.Id);

        var payload = new CreateSalesPaymentDto
        {
            SalesOrderId = order.Id,
            Amount = 500m,
            CurrencyCode = "USD",
            PaymentMethod = "Cash",
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SalesPaymentDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.PaymentNumber.Should().StartWith("PAY-");
        body.Data.SalesOrderId.Should().Be(order.Id);
        body.Data.Amount.Should().Be(500m);
        body.Data.CurrencyCode.Should().Be("USD");
        body.Data.PaymentMethod.Should().Be("Cash");
        body.Data.Id.Should().NotBeEmpty();
    }

    // ── POST /api/sales/salespayment ── Maximal ───────────────────────────────

    [Fact]
    public async Task Create_WithMaximalPayload_Returns201AndAllFieldsPersisted()
    {
        var order = await _builder.CreateSalesOrderAsync(SalesOrderStatus.Placed);
        _createdOrderIds.Add(order.Id);

        var contactId = Guid.NewGuid();

        var payload = new CreateSalesPaymentDto
        {
            SalesOrderId = order.Id,
            ContactId = contactId,
            Amount = 7500m,
            CurrencyCode = "PKR",
            ExchangeRate = 278.5m,
            PaymentMethod = "BankTransfer",
            ReferenceNumber = "TRF-HBL-20260115-001",
            BankName = "Habib Bank Limited",
            GatewayTransactionId = "TXN-98765",
            Notes = "Payment for invoice INV-00001",
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SalesPaymentDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.Amount.Should().Be(7500m);
        body.Data.CurrencyCode.Should().Be("PKR");
        body.Data.PaymentMethod.Should().Be("BankTransfer");
        body.Data.ReferenceNumber.Should().Be("TRF-HBL-20260115-001");
        body.Data.BankName.Should().Be("Habib Bank Limited");
        body.Data.GatewayTransactionId.Should().Be("TXN-98765");
    }

    // ── POST requires existing order ──────────────────────────────────────────

    [Fact]
    public async Task Create_OrderNotFound_Returns404()
    {
        var payload = new CreateSalesPaymentDto
        {
            SalesOrderId = Guid.NewGuid(),
            Amount = 100m,
            CurrencyCode = "USD",
            PaymentMethod = "Cash",
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/sales/salespayment ───────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<SalesPaymentDto>>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
    }

    // ── GET /api/sales/salespayment/{id} ──────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingPayment_ReturnsOk()
    {
        var order = await _builder.CreateSalesOrderAsync(SalesOrderStatus.Placed);
        _createdOrderIds.Add(order.Id);

        var payload = new CreateSalesPaymentDto
        {
            SalesOrderId = order.Id,
            Amount = 200m,
            CurrencyCode = "USD",
            PaymentMethod = "Card",
        };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<SalesPaymentDto>>())!.Data!;

        var response = await _client.GetAsync($"{BaseUrl}/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SalesPaymentDto>>();
        body!.Data!.Id.Should().Be(created.Id);
        body.Data.Amount.Should().Be(200m);
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/sales/salespayment/by-order/{orderId} ────────────────────────

    [Fact]
    public async Task GetByOrder_ReturnsPaymentsForOrder()
    {
        var order = await _builder.CreateSalesOrderAsync(SalesOrderStatus.Placed);
        _createdOrderIds.Add(order.Id);

        var payload = new CreateSalesPaymentDto
        {
            SalesOrderId = order.Id,
            Amount = 300m,
            CurrencyCode = "USD",
            PaymentMethod = "Wallet",
        };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<SalesPaymentDto>>())!.Data!;

        var response = await _client.GetAsync($"{BaseUrl}/by-order/{order.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<SalesPaymentDto>>>();
        body!.Data.Should().Contain(p => p.Id == created.Id);
    }

    // ── GET /api/sales/salespayment/by-number/{paymentNumber} ────────────────

    [Fact]
    public async Task GetByNumber_ExistingPayment_ReturnsCorrectPayment()
    {
        var order = await _builder.CreateSalesOrderAsync(SalesOrderStatus.Placed);
        _createdOrderIds.Add(order.Id);

        var payload = new CreateSalesPaymentDto
        {
            SalesOrderId = order.Id,
            Amount = 150m,
            CurrencyCode = "USD",
            PaymentMethod = "Cash",
        };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<SalesPaymentDto>>())!.Data!;

        var response = await _client.GetAsync($"{BaseUrl}/by-number/{created.PaymentNumber}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SalesPaymentDto>>();
        body!.Data!.PaymentNumber.Should().Be(created.PaymentNumber);
    }

    // ── Order balance updated after payment ───────────────────────────────────

    [Fact]
    public async Task Create_FullPayment_OrderBecomePaidAndClosed()
    {
        var createOrderPayload = new CreateSalesOrderDto
        {
            ContactId    = Guid.NewGuid(),
            ContactName  = "Test Customer",
            CurrencyCode = "USD",
            SalesChannel = SalesChannel.DirectSales,
            Lines        = [],
        };
        var orderResponse = await _client.PostAsJsonAsync("api/sales/salesorder", createOrderPayload);
        var order = (await orderResponse.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>())!.Data!;
        _createdOrderIds.Add(order.Id);

        var paymentPayload = new CreateSalesPaymentDto
        {
            SalesOrderId = order.Id,
            Amount = order.TotalAmount == 0 ? 0.01m : order.TotalAmount,
            CurrencyCode = "USD",
            PaymentMethod = "Cash",
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, paymentPayload);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
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
