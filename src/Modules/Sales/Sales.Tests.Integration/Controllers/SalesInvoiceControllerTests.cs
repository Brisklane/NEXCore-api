namespace Sales.Tests.Integration.Controllers;

[Collection(SalesTestCollection.Name)]
public class SalesInvoiceControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "api/sales/salesinvoice";

    private readonly SalesCollectionFixture _fixture;
    private readonly HttpClient _client;
    private readonly SalesTestDataBuilder _builder;
    private readonly List<Guid> _createdInvoiceIds = [];
    private readonly List<Guid> _createdOrderIds = [];

    public SalesInvoiceControllerTests(SalesCollectionFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient();
        _builder = new SalesTestDataBuilder(fixture);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var id in _createdInvoiceIds)
        {
            try
            {
                await _client.PostAsJsonAsync($"{BaseUrl}/{id}/cancel", new CancelInvoiceDto { Reason = "Test cleanup" });
            }
            catch { /* ignore */ }
        }
        foreach (var id in _createdOrderIds)
            await _builder.DeleteSalesOrderAsync(id);
        _client.Dispose();
    }

    // ── POST /api/sales/salesinvoice ── Minimal ───────────────────────────────

    [Fact]
    public async Task Create_WithMinimalPayload_Returns201AndDraftInvoice()
    {
        var order = await _builder.CreateSalesOrderAsync(SalesOrderStatus.Confirmed);
        _createdOrderIds.Add(order.Id);

        var payload = new CreateSalesInvoiceDto
        {
            SalesOrderId = order.Id,
            DueDate = DateTime.UtcNow.AddDays(30),
            CurrencyCode = "USD",
            Lines = [],
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.InvoiceNumber.Should().StartWith("INV-");
        body.Data.SalesOrderId.Should().Be(order.Id);
        body.Data.Status.Should().Be(InvoiceStatus.Draft);
        body.Data.CurrencyCode.Should().Be("USD");
        body.Data.Id.Should().NotBeEmpty();

        _createdInvoiceIds.Add(body.Data.Id);
    }

    // ── POST /api/sales/salesinvoice ── Maximal ───────────────────────────────

    [Fact]
    public async Task Create_WithMaximalPayload_Returns201AndAllFieldsPersisted()
    {
        var order = await _builder.CreateSalesOrderAsync(SalesOrderStatus.Confirmed);
        _createdOrderIds.Add(order.Id);

        var orderLine = await _builder.CreateSalesOrderLineAsync(order.Id);
        var contactId = Guid.NewGuid();

        var payload = new CreateSalesInvoiceDto
        {
            SalesOrderId = order.Id,
            ContactId = contactId,
            DueDate = DateTime.UtcNow.AddDays(30),
            CurrencyCode = "PKR",
            ExchangeRate = 278.5m,
            PaymentTerms = PaymentTerms.Net30,
            BillToName = "Zara Khan",
            BillToStreet = "45 Commercial Area",
            BillToCity = "Karachi",
            BillToState = "Sindh",
            BillToPostalCode = "75600",
            BillToCountry = "PK",
            CustomerReference = "PO-ZK-2026-001",
            Notes = "Invoice for Q2 software delivery",
            Lines =
            [
                new CreateSalesInvoiceLineDto
                {
                    SalesOrderLineId = orderLine.Id,
                    ProductId = orderLine.ProductId,
                    ProductCode = "SW-LIC-001",
                    ProductName = "Software License Annual",
                    Quantity = 3,
                    UnitOfMeasure = "SEAT",
                    UnitPrice = 50000m,
                    DiscountAmount = 5000m,
                    TaxCategory = TaxCategory.Standard,
                    TaxRate = 13m,
                },
            ],
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.SalesOrderId.Should().Be(order.Id);
        body.Data.CurrencyCode.Should().Be("PKR");
        body.Data.BillToName.Should().Be("Zara Khan");
        body.Data.BillToCity.Should().Be("Karachi");
        body.Data.CustomerReference.Should().Be("PO-ZK-2026-001");
        body.Data.Lines.Should().HaveCount(1);
        body.Data.Lines[0].ProductCode.Should().Be("SW-LIC-001");
        body.Data.Lines[0].Quantity.Should().Be(3);

        _createdInvoiceIds.Add(body.Data.Id);
    }

    // ── GET /api/sales/salesinvoice ───────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<SalesInvoiceDto>>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
    }

    // ── GET /api/sales/salesinvoice/{id} ──────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingInvoice_ReturnsOkWithLines()
    {
        var order = await _builder.CreateSalesOrderAsync(SalesOrderStatus.Confirmed);
        _createdOrderIds.Add(order.Id);

        var payload = new CreateSalesInvoiceDto
        {
            SalesOrderId = order.Id,
            DueDate = DateTime.UtcNow.AddDays(30),
            CurrencyCode = "USD",
            Lines = [],
        };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>())!.Data!;
        _createdInvoiceIds.Add(created.Id);

        var response = await _client.GetAsync($"{BaseUrl}/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>();
        body!.Data!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/sales/salesinvoice/by-order/{salesOrderId} ──────────────────

    [Fact]
    public async Task GetByOrder_ReturnsInvoicesForOrder()
    {
        var order = await _builder.CreateSalesOrderAsync(SalesOrderStatus.Confirmed);
        _createdOrderIds.Add(order.Id);

        var payload = new CreateSalesInvoiceDto
        {
            SalesOrderId = order.Id,
            DueDate = DateTime.UtcNow.AddDays(30),
            CurrencyCode = "USD",
            Lines = [],
        };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>())!.Data!;
        _createdInvoiceIds.Add(created.Id);

        var response = await _client.GetAsync($"{BaseUrl}/by-order/{order.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<SalesInvoiceDto>>>();
        body!.Data.Should().Contain(i => i.Id == created.Id);
    }

    // ── POST /api/sales/salesinvoice/{id}/confirm ─────────────────────────────

    [Fact]
    public async Task Confirm_DraftInvoice_TransitionsToIssued()
    {
        var order = await _builder.CreateSalesOrderAsync(SalesOrderStatus.Confirmed);
        _createdOrderIds.Add(order.Id);

        var payload = new CreateSalesInvoiceDto
        {
            SalesOrderId = order.Id,
            DueDate = DateTime.UtcNow.AddDays(30),
            CurrencyCode = "USD",
            Lines = [],
        };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>())!.Data!;
        _createdInvoiceIds.Add(created.Id);

        var confirmResponse = await _client.PostAsJsonAsync($"{BaseUrl}/{created.Id}/confirm", new { });

        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await confirmResponse.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>();
        body!.Data!.Status.Should().Be(InvoiceStatus.Issued);
    }

    // ── POST /api/sales/salesinvoice/{id}/cancel ──────────────────────────────

    [Fact]
    public async Task Cancel_DraftInvoice_TransitionsToCancelled()
    {
        var order = await _builder.CreateSalesOrderAsync(SalesOrderStatus.Confirmed);
        _createdOrderIds.Add(order.Id);

        var payload = new CreateSalesInvoiceDto
        {
            SalesOrderId = order.Id,
            DueDate = DateTime.UtcNow.AddDays(30),
            CurrencyCode = "USD",
            Lines = [],
        };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>())!.Data!;

        var cancelResponse = await _client.PostAsJsonAsync(
            $"{BaseUrl}/{created.Id}/cancel",
            new CancelInvoiceDto { Reason = "Test cancellation" });

        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await cancelResponse.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>();
        body!.Data!.Status.Should().Be(InvoiceStatus.Cancelled);
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
