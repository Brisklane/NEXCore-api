namespace Sales.Tests.Integration.Controllers;

[Collection(SalesTestCollection.Name)]
public class QuotationControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "api/sales/quotation";

    private readonly SalesCollectionFixture _fixture;
    private readonly HttpClient _client;
    private readonly List<Guid> _createdIds = [];

    public QuotationControllerTests(SalesCollectionFixture fixture)
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

    // ── POST /api/sales/quotation ── Minimal ──────────────────────────────────

    [Fact]
    public async Task Create_WithMinimalPayload_Returns201AndDraftQuotation()
    {
        var payload = new CreateQuotationDto
        {
            ContactId   = Guid.NewGuid(),
            ContactName = "Test Customer",
            CurrencyCode = "USD",
            Lines = [],
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.QuotationNumber.Should().StartWith("QT-");
        body.Data.Status.Should().Be(QuotationStatus.Draft);
        body.Data.CurrencyCode.Should().Be("USD");
        body.Data.ContactId.Should().Be(payload.ContactId);
        body.Data.Id.Should().NotBeEmpty();

        _createdIds.Add(body.Data.Id);
    }

    // ── POST — missing customer returns 400 ──────────────────────────────────

    [Fact]
    public async Task Create_WithoutCustomer_Returns400()
    {
        var payload = new CreateQuotationDto
        {
            CurrencyCode = "USD",
            Lines = [],
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── POST /api/sales/quotation ── Maximal ──────────────────────────────────

    [Fact]
    public async Task Create_WithMaximalPayload_Returns201AndAllFieldsPersisted()
    {
        var contactId  = Guid.NewGuid();
        var salesRepId = Guid.NewGuid();
        var payload = new CreateQuotationDto
        {
            QuotationName         = "Enterprise Software Proposal Q4 2026",
            ContactId             = contactId,
            ContactName           = "Acme Corp",
            ValidUntil            = DateTime.UtcNow.AddDays(30),
            CurrencyCode          = "PKR",
            ExchangeRate          = 278.5m,
            PaymentTerms          = PaymentTerms.Net30,
            Incoterm              = Incoterm.DAP,
            IncotermLocation      = "Karachi Port",
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(14),
            ShippingMethod        = "Standard",
            SalesRepId            = salesRepId,
            CustomerPONumber      = "PO-ACM-2026-001",
            Notes                 = "Priority customer — fast turnaround required",
            TermsAndConditions    = "Standard terms apply. 30-day payment.",
            Lines =
            [
                new CreateQuotationLineDto
                {
                    ProductId          = Guid.NewGuid(),
                    ProductCode        = "SW-ENT-001",
                    ProductName        = "Enterprise License Pack",
                    ProductDescription = "Annual enterprise license with support",
                    Quantity           = 5,
                    UnitOfMeasure      = "SEAT",
                    UnitPrice          = 50000m,
                    DiscountPercentage = 10m,
                    TaxCategory        = TaxCategory.Standard,
                    Notes              = "5-seat license bundle",
                },
                new CreateQuotationLineDto
                {
                    ProductId    = Guid.NewGuid(),
                    ProductCode  = "SVC-IMPL-001",
                    ProductName  = "Implementation Services",
                    Quantity     = 40,
                    UnitOfMeasure = "HRS",
                    UnitPrice    = 5000m,
                },
            ],
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.QuotationName.Should().Be(payload.QuotationName);
        body.Data.ContactId.Should().Be(contactId);
        body.Data.ContactName.Should().Be("Acme Corp");
        body.Data.CurrencyCode.Should().Be("PKR");
        body.Data.CustomerPONumber.Should().Be("PO-ACM-2026-001");
        body.Data.Lines.Should().HaveCount(2);
        body.Data.Lines[0].ProductCode.Should().Be("SW-ENT-001");
        body.Data.Lines[0].Quantity.Should().Be(5);

        _createdIds.Add(body.Data.Id);
    }

    // ── GET /api/sales/quotation ───────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<QuotationDto>>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
    }

    // ── GET /api/sales/quotation/{id} ─────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingQuotation_ReturnsOkWithLines()
    {
        var payload = new CreateQuotationDto
        {
            ContactId   = Guid.NewGuid(),
            ContactName = "Test Customer",
            CurrencyCode = "USD",
            Lines =
            [
                new CreateQuotationLineDto
                {
                    ProductId     = Guid.NewGuid(),
                    ProductCode   = "P-001",
                    ProductName   = "Test Product",
                    Quantity      = 2,
                    UnitOfMeasure = "PCS",
                    UnitPrice     = 100m,
                },
            ],
        };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>())!.Data!;
        _createdIds.Add(created.Id);

        var response = await _client.GetAsync($"{BaseUrl}/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>();
        body!.Data!.Id.Should().Be(created.Id);
        body.Data.Lines.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/sales/quotation/by-number/{number} ───────────────────────────

    [Fact]
    public async Task GetByNumber_ExistingQuotation_ReturnsCorrectQuotation()
    {
        var payload = new CreateQuotationDto
        {
            ContactId   = Guid.NewGuid(),
            ContactName = "Test Customer",
            CurrencyCode = "USD",
            Lines = [],
        };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>())!.Data!;
        _createdIds.Add(created.Id);

        var response = await _client.GetAsync($"{BaseUrl}/by-number/{created.QuotationNumber}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>();
        body!.Data!.QuotationNumber.Should().Be(created.QuotationNumber);
    }

    // ── POST /api/sales/quotation/{id}/send ───────────────────────────────────

    [Fact]
    public async Task Send_DraftQuotation_TransitionsToSent()
    {
        var payload = new CreateQuotationDto
        {
            ContactId   = Guid.NewGuid(),
            ContactName = "Test Customer",
            CurrencyCode = "USD",
            Lines = [],
        };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>())!.Data!;
        _createdIds.Add(created.Id);

        var sendResponse = await _client.PostAsJsonAsync($"{BaseUrl}/{created.Id}/send", new { });

        sendResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await sendResponse.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>();
        body!.Data!.Status.Should().Be(QuotationStatus.Sent);
    }

    // ── DELETE /api/sales/quotation/{id} ──────────────────────────────────────

    [Fact]
    public async Task Delete_DraftQuotation_ReturnsOkAndNotFound()
    {
        var payload = new CreateQuotationDto
        {
            ContactId   = Guid.NewGuid(),
            ContactName = "Test Customer",
            CurrencyCode = "USD",
            Lines = [],
        };
        var created = (await (await _client.PostAsJsonAsync(BaseUrl, payload))
            .Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>())!.Data!;

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
