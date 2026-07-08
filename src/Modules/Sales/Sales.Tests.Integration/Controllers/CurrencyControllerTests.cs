namespace Sales.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for /api/sales/currency — multi-currency and exchange-rate management.
///
/// Covers:
///   • Currency CRUD
///   • Multiple named rates per currency (same type, same date, different RateName)
///   • Rate history
///   • Single-amount conversion with rate type + name selection
///   • Bulk conversion for reporting (many currencies → one target)
///   • Rate lookup (for pre-filling orders/invoices)
/// </summary>
[Collection(SalesTestCollection.Name)]
public class CurrencyControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "api/sales/currency";

    private readonly SalesCollectionFixture _fixture;
    private readonly HttpClient _client;

    // track created currency IDs for cleanup
    private readonly List<Guid> _createdCurrencyIds = [];

    public CurrencyControllerTests(SalesCollectionFixture fixture)
    {
        _fixture = fixture;
        _client  = fixture.CreateAuthenticatedClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // soft-delete by deactivating — no hard-delete endpoint on currencies
        foreach (var id in _createdCurrencyIds)
        {
            try
            {
                await _client.PutAsJsonAsync($"{BaseUrl}/{id}", new UpdateCurrencyDto { IsActive = false });
            }
            catch { /* ignore */ }
        }
        _client.Dispose();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Currency CRUD
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateCurrency_ValidPayload_Returns201WithCorrectFields()
    {
        var code = UniqueCurrencyCode();
        var dto  = new CreateCurrencyDto
        {
            Code           = code,
            Name           = "Test Dollar",
            Symbol         = "T$",
            DecimalPlaces  = 2,
            IsBaseCurrency = false,
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CurrencyDto>>();
        body!.Success.Should().BeTrue();
        var c = body.Data!;

        c.Code.Should().Be(code);
        c.Name.Should().Be("Test Dollar");
        c.Symbol.Should().Be("T$");
        c.DecimalPlaces.Should().Be(2);
        c.IsBaseCurrency.Should().BeFalse();
        c.IsActive.Should().BeTrue();
        c.Id.Should().NotBeEmpty();

        _createdCurrencyIds.Add(c.Id);
    }

    [Fact]
    public async Task CreateCurrency_DuplicateCode_Returns400()
    {
        var code = UniqueCurrencyCode();
        var dto  = new CreateCurrencyDto { Code = code, Name = "Dup A", Symbol = "A" };

        var r1 = await _client.PostAsJsonAsync(BaseUrl, dto);
        r1.StatusCode.Should().Be(HttpStatusCode.Created);
        _createdCurrencyIds.Add((await r1.Content.ReadFromJsonAsync<ApiResponse<CurrencyDto>>())!.Data!.Id);

        // second create with same code must fail
        var r2 = await _client.PostAsJsonAsync(BaseUrl, dto);
        r2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCurrencyByCode_Existing_ReturnsCorrectCurrency()
    {
        var c = await CreateCurrencyAsync();

        var response = await _client.GetAsync($"{BaseUrl}/{c.Code}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CurrencyDto>>();
        body!.Data!.Code.Should().Be(c.Code);
    }

    [Fact]
    public async Task GetCurrencyByCode_NonExistent_Returns404()
    {
        var response = await _client.GetAsync($"{BaseUrl}/ZZZ");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateCurrency_ChangeName_ReturnsUpdatedDto()
    {
        var c = await CreateCurrencyAsync();

        var dto      = new UpdateCurrencyDto { Name = "Updated Name", Symbol = "U$" };
        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{c.Id}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CurrencyDto>>();
        body!.Data!.Name.Should().Be("Updated Name");
        body.Data.Symbol.Should().Be("U$");
    }

    [Fact]
    public async Task GetAllCurrencies_ReturnsOkList()
    {
        await CreateCurrencyAsync();

        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<CurrencyDto>>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Exchange Rates — single rate per currency
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AddRate_ValidPayload_Returns201WithRate()
    {
        var base_ = await EnsureBaseCurrencyAsync();  // PKR
        var usd   = await CreateCurrencyAsync("USD-T");

        var rateDto = new CreateCurrencyRateDto
        {
            CurrencyId    = usd.Id,
            Rate          = 278.00m,
            RateType      = ExchangeRateType.Official,
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Source        = "State Bank of Pakistan",
        };

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/{usd.Id}/rates", rateDto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CurrencyRateDto>>();
        body!.Success.Should().BeTrue();
        var rate = body.Data!;

        rate.CurrencyId.Should().Be(usd.Id);
        rate.Rate.Should().Be(278.00m);
        rate.RateType.Should().Be(ExchangeRateType.Official);
        rate.RateName.Should().BeNull("unnamed = default rate for this type");
        rate.Source.Should().Be("State Bank of Pakistan");
    }

    [Fact]
    public async Task GetRatesForCurrency_AfterAdding_ReturnsAllRates()
    {
        var usd = await CreateCurrencyAsync("USD-G");

        await AddRateAsync(usd.Id, 278.00m, ExchangeRateType.Official, null);
        await AddRateAsync(usd.Id, 277.50m, ExchangeRateType.Buying,   null);
        await AddRateAsync(usd.Id, 279.00m, ExchangeRateType.Selling,  null);

        var response = await _client.GetAsync($"{BaseUrl}/{usd.Id}/rates");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<CurrencyRateDto>>>();
        body!.Data!.Should().HaveCount(3);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  MULTIPLE RATES PER CURRENCY — same type, same date, different RateName
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Core scenario: on the same day for USD, different banks publish different buying rates.
    /// We store all of them and can look up by name when reporting.
    /// </summary>
    [Fact]
    public async Task MultipleNamedRates_SameTypeAndDate_AllStoredAndRetrievable()
    {
        var usd  = await CreateCurrencyAsync("USD-M");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Three buying rates on the same day from different banks
        await AddRateAsync(usd.Id, 277.30m, ExchangeRateType.Buying, "Bank Al-Habib",  today);
        await AddRateAsync(usd.Id, 277.60m, ExchangeRateType.Buying, "Bank Al-Falah",  today);
        await AddRateAsync(usd.Id, 277.00m, ExchangeRateType.Buying, "HBL",            today);
        // One unnamed default buying rate
        await AddRateAsync(usd.Id, 277.50m, ExchangeRateType.Buying, null,             today);

        var ratesResponse = await _client.GetAsync($"{BaseUrl}/{usd.Id}/rates");
        var allRates = (await ratesResponse.Content.ReadFromJsonAsync<ApiResponse<List<CurrencyRateDto>>>())!.Data!;

        allRates.Should().HaveCount(4, "all four named variants must be stored");
        allRates.Should().Contain(r => r.RateName == "Bank Al-Habib" && r.Rate == 277.30m);
        allRates.Should().Contain(r => r.RateName == "Bank Al-Falah" && r.Rate == 277.60m);
        allRates.Should().Contain(r => r.RateName == "HBL"           && r.Rate == 277.00m);
        allRates.Should().Contain(r => r.RateName == null             && r.Rate == 277.50m);
    }

    [Fact]
    public async Task RateLookup_WithSpecificRateName_ReturnsCorrectNamedRate()
    {
        var usd  = await CreateCurrencyAsync("USD-N");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await AddRateAsync(usd.Id, 277.30m, ExchangeRateType.Buying, "Bank Al-Habib", today);
        await AddRateAsync(usd.Id, 277.60m, ExchangeRateType.Buying, "Bank Al-Falah", today);
        await AddRateAsync(usd.Id, 277.50m, ExchangeRateType.Buying, null,            today);

        // Request the Bank Al-Habib rate specifically
        var response = await _client.GetAsync(
            $"{BaseUrl}/rate-lookup?from={usd.Code}&rateType=Buying&rateName=Bank+Al-Habib&asOfDate={today:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<decimal>>();
        body!.Data.Should().Be(277.30m, "Bank Al-Habib rate was 277.30");
    }

    [Fact]
    public async Task RateLookup_WithoutRateName_ReturnsDefaultUnnamedRate()
    {
        var usd  = await CreateCurrencyAsync("USD-D");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Named rates + one default (null name)
        await AddRateAsync(usd.Id, 277.30m, ExchangeRateType.Buying, "Bank Al-Habib", today);
        await AddRateAsync(usd.Id, 277.50m, ExchangeRateType.Buying, null,            today);

        // No rateName → should return the default (unnamed) rate
        var response = await _client.GetAsync(
            $"{BaseUrl}/rate-lookup?from={usd.Code}&rateType=Buying&asOfDate={today:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<decimal>>();
        body!.Data.Should().Be(277.50m, "default (unnamed) buying rate is 277.50");
    }

    [Fact]
    public async Task RateLookup_NamedRateDoesNotExist_Returns404()
    {
        var usd  = await CreateCurrencyAsync("USD-X");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await AddRateAsync(usd.Id, 278.00m, ExchangeRateType.Official, null, today);

        var response = await _client.GetAsync(
            $"{BaseUrl}/rate-lookup?from={usd.Code}&rateType=Official&rateName=NonExistentBank&asOfDate={today:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Latest rates overview
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetLatestRates_ReturnsOneRowPerNamedVariant()
    {
        var eur  = await CreateCurrencyAsync("EUR-L");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await AddRateAsync(eur.Id, 305.00m, ExchangeRateType.Official, null,           today);
        await AddRateAsync(eur.Id, 304.50m, ExchangeRateType.Buying,   "HBL",          today);
        await AddRateAsync(eur.Id, 304.80m, ExchangeRateType.Buying,   "Bank Al-Falah",today);

        var response = await _client.GetAsync($"{BaseUrl}/rates/latest?asOfDate={today:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<CurrencyRateDto>>>();
        // Three EUR rows — Official(default) + Buying(HBL) + Buying(Al-Falah)
        var eurRates = body!.Data!.Where(r => r.CurrencyCode == eur.Code).ToList();
        eurRates.Should().HaveCount(3, "each (currency, rateType, rateName) combo is a separate row");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Rate history
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetRateHistory_ReturnsAllRatesInDateRange()
    {
        var usd  = await CreateCurrencyAsync("USD-H");
        var base_ = DateOnly.FromDateTime(DateTime.UtcNow);

        await AddRateAsync(usd.Id, 277.00m, ExchangeRateType.Official, null, base_.AddDays(-2));
        await AddRateAsync(usd.Id, 277.50m, ExchangeRateType.Official, null, base_.AddDays(-1));
        await AddRateAsync(usd.Id, 278.00m, ExchangeRateType.Official, null, base_);

        var from = base_.AddDays(-2);
        var to   = base_;

        var response = await _client.GetAsync(
            $"{BaseUrl}/{usd.Code}/rates/history?fromDate={from:yyyy-MM-dd}&toDate={to:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<CurrencyRateDto>>>();
        body!.Data!.Should().HaveCount(3, "three rates over 3 days");
        body.Data.Should().BeInDescendingOrder(r => r.EffectiveDate);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Single-amount conversion (reporting)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Convert_OfficialRate_ReturnsCorrectConvertedAmount()
    {
        await EnsureBaseCurrencyAsync();
        var usd  = await CreateCurrencyAsync("USD-C");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await AddRateAsync(usd.Id, 278.00m, ExchangeRateType.Official, null, today);

        var dto = new CurrencyConversionRequestDto
        {
            Amount           = 100m,
            FromCurrencyCode = usd.Code,
            RateType         = ExchangeRateType.Official,
            AsOfDate         = today,
        };

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/convert", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CurrencyConversionResultDto>>();
        var result = body!.Data!;

        result.OriginalAmount.Should().Be(100m);
        result.ConvertedAmount.Should().Be(27800m, "100 × 278 = 27,800");
        result.RateUsed.Should().Be(278.00m);
        result.RateType.Should().Be(ExchangeRateType.Official);
        result.FromCurrencyCode.Should().Be(usd.Code);
    }

    [Fact]
    public async Task Convert_WithSpecificRateName_UsesNamedRate()
    {
        await EnsureBaseCurrencyAsync();
        var usd  = await CreateCurrencyAsync("USD-CN");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await AddRateAsync(usd.Id, 277.50m, ExchangeRateType.Buying, null,           today);  // default
        await AddRateAsync(usd.Id, 277.30m, ExchangeRateType.Buying, "Bank Al-Habib",today);  // named

        var dto = new CurrencyConversionRequestDto
        {
            Amount           = 100m,
            FromCurrencyCode = usd.Code,
            RateType         = ExchangeRateType.Buying,
            RateName         = "Bank Al-Habib",
            AsOfDate         = today,
        };

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/convert", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = (await response.Content.ReadFromJsonAsync<ApiResponse<CurrencyConversionResultDto>>())!.Data!;

        result.RateUsed.Should().Be(277.30m, "Bank Al-Habib rate must be used, not the default 277.50");
        result.ConvertedAmount.Should().Be(27730m);
    }

    [Fact]
    public async Task Convert_SameCurrency_ReturnsSameAmount()
    {
        await EnsureBaseCurrencyAsync();
        var dto = new CurrencyConversionRequestDto
        {
            Amount           = 5000m,
            FromCurrencyCode = "PKR",
            ToCurrencyCode   = "PKR",
            RateType         = ExchangeRateType.Official,
        };

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/convert", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = (await response.Content.ReadFromJsonAsync<ApiResponse<CurrencyConversionResultDto>>())!.Data!;
        result.ConvertedAmount.Should().Be(5000m);
        result.RateUsed.Should().Be(1m);
    }

    [Fact]
    public async Task Convert_NoRateAvailable_Returns400()
    {
        await EnsureBaseCurrencyAsync();
        var cad = await CreateCurrencyAsync("CAD-T");   // no rate added

        var dto = new CurrencyConversionRequestDto
        {
            Amount           = 100m,
            FromCurrencyCode = cad.Code,
            RateType         = ExchangeRateType.Official,
            AsOfDate         = DateOnly.FromDateTime(DateTime.UtcNow),
        };

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/convert", dto);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Bulk conversion (month-end / report scenarios)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Month-end report: three invoices in different currencies
    /// all converted to PKR using the Official rate as of report end date.
    /// </summary>
    [Fact]
    public async Task BulkConvert_MixedCurrencies_ConvertsAllLinesToTarget()
    {
        await EnsureBaseCurrencyAsync();                 // PKR = base
        var usd = await CreateCurrencyAsync("USD-B");
        var eur = await CreateCurrencyAsync("EUR-B");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await AddRateAsync(usd.Id, 278.00m, ExchangeRateType.Official, null, today);
        await AddRateAsync(eur.Id, 305.00m, ExchangeRateType.Official, null, today);

        var dto = new BulkConversionRequestDto
        {
            TargetCurrencyCode = "PKR",
            RateType           = ExchangeRateType.Official,
            AsOfDate           = today,
            Lines =
            [
                new BulkConversionLineDto { Reference = "INV-001", Amount = 1000m, CurrencyCode = usd.Code },
                new BulkConversionLineDto { Reference = "INV-002", Amount = 500m,  CurrencyCode = eur.Code },
                new BulkConversionLineDto { Reference = "INV-003", Amount = 50000m,CurrencyCode = "PKR"   },
            ],
        };

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/bulk-convert", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = (await response.Content.ReadFromJsonAsync<ApiResponse<BulkConversionResultDto>>())!.Data!;

        result.Lines.Should().HaveCount(3);

        var inv001 = result.Lines.First(l => l.Reference == "INV-001");
        inv001.ConvertedAmount.Should().Be(278000m, "1000 USD × 278 = 278,000 PKR");
        inv001.RateUsed.Should().Be(278.00m);

        var inv002 = result.Lines.First(l => l.Reference == "INV-002");
        inv002.ConvertedAmount.Should().Be(152500m, "500 EUR × 305 = 152,500 PKR");
        inv002.RateUsed.Should().Be(305.00m);

        var inv003 = result.Lines.First(l => l.Reference == "INV-003");
        inv003.ConvertedAmount.Should().Be(50000m, "PKR → PKR passes through at rate 1");
        inv003.RateUsed.Should().Be(1m);

        result.TotalConverted.Should().Be(278000m + 152500m + 50000m);
    }

    /// <summary>
    /// Report using Bank Al-Habib buying rate specifically — all lines use the named rate.
    /// </summary>
    [Fact]
    public async Task BulkConvert_WithNamedRate_UsesNamedRateForAllLines()
    {
        await EnsureBaseCurrencyAsync();
        var usd  = await CreateCurrencyAsync("USD-BN");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await AddRateAsync(usd.Id, 277.50m, ExchangeRateType.Buying, null,            today);  // default
        await AddRateAsync(usd.Id, 277.30m, ExchangeRateType.Buying, "Bank Al-Habib", today);  // named

        var dto = new BulkConversionRequestDto
        {
            TargetCurrencyCode = "PKR",
            RateType           = ExchangeRateType.Buying,
            RateName           = "Bank Al-Habib",
            AsOfDate           = today,
            Lines =
            [
                new BulkConversionLineDto { Reference = "SO-001", Amount = 100m, CurrencyCode = usd.Code },
                new BulkConversionLineDto { Reference = "SO-002", Amount = 200m, CurrencyCode = usd.Code },
            ],
        };

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/bulk-convert", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = (await response.Content.ReadFromJsonAsync<ApiResponse<BulkConversionResultDto>>())!.Data!;

        result.Lines.Should().AllSatisfy(l => l.RateUsed.Should().Be(277.30m,
            "Bank Al-Habib rate should be used for all lines, not the default 277.50"));
        result.TotalConverted.Should().Be((100m + 200m) * 277.30m);
    }

    [Fact]
    public async Task BulkConvert_EmptyLines_Returns400()
    {
        var dto = new BulkConversionRequestDto
        {
            TargetCurrencyCode = "PKR",
            RateType           = ExchangeRateType.Official,
            Lines              = [],
        };

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/bulk-convert", dto);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Rate history for reporting on past transactions
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Reporting scenario: an invoice was created on 2026-05-01.
    /// We need the exact Official rate that was in effect on that date.
    /// Current rate (today) is different — the historical rate must be used.
    /// </summary>
    [Fact]
    public async Task Convert_HistoricalDate_UsesRateEffectiveOnThatDate()
    {
        await EnsureBaseCurrencyAsync();
        var usd      = await CreateCurrencyAsync("USD-HIST");
        var pastDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var today    = DateOnly.FromDateTime(DateTime.UtcNow);

        await AddRateAsync(usd.Id, 270.00m, ExchangeRateType.Official, null, pastDate);  // historical
        await AddRateAsync(usd.Id, 278.00m, ExchangeRateType.Official, null, today);      // current

        // Convert using the past date — should get 270, not 278
        var dto = new CurrencyConversionRequestDto
        {
            Amount           = 100m,
            FromCurrencyCode = usd.Code,
            RateType         = ExchangeRateType.Official,
            AsOfDate         = pastDate,
        };

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/convert", dto);
        var result   = (await response.Content.ReadFromJsonAsync<ApiResponse<CurrencyConversionResultDto>>())!.Data!;

        result.RateUsed.Should().Be(270.00m, "historical rate (270) must be used, not today's rate (278)");
        result.ConvertedAmount.Should().Be(27000m);
        result.RateDate.Should().Be(pastDate);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Rate update and delete
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateRate_ChangesRateValue()
    {
        var usd   = await CreateCurrencyAsync("USD-U");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rateId = await AddRateAndGetIdAsync(usd.Id, 278.00m, ExchangeRateType.Official, null, today);

        var updateDto = new UpdateCurrencyRateDto { Rate = 279.50m, Notes = "Corrected" };
        var response  = await _client.PutAsJsonAsync($"{BaseUrl}/rates/{rateId}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CurrencyRateDto>>();
        body!.Data!.Rate.Should().Be(279.50m);
        body.Data.Notes.Should().Be("Corrected");
    }

    [Fact]
    public async Task DeleteRate_RemovesRate()
    {
        var usd   = await CreateCurrencyAsync("USD-DEL");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rateId = await AddRateAndGetIdAsync(usd.Id, 278.00m, ExchangeRateType.Official, null, today);

        var deleteResponse = await _client.DeleteAsync($"{BaseUrl}/rates/{rateId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // rate should no longer appear in the list
        var ratesResponse = await _client.GetAsync($"{BaseUrl}/{usd.Id}/rates");
        var rates = (await ratesResponse.Content.ReadFromJsonAsync<ApiResponse<List<CurrencyRateDto>>>())!.Data!;
        rates.Should().NotContain(r => r.Id == rateId);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Auth
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var anon     = _fixture.CreateClient();
        var response = await anon.GetAsync(BaseUrl);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        anon.Dispose();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Private helpers
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Generates a unique 3-char ISO-style code for test isolation.</summary>
    private static string UniqueCurrencyCode()
        => ("T" + Guid.NewGuid().ToString("N")[..2]).ToUpperInvariant();

    /// <summary>Creates a currency with a guaranteed-unique 3-char code and tracks it for cleanup.</summary>
    private async Task<CurrencyDto> CreateCurrencyAsync(string? nameHint = null)
    {
        // Always generate a unique code — truncating a prefix like "USD-G" to "USD" would
        // cause all test methods to share the same currency and pollute each other's rates.
        var code = UniqueCurrencyCode();

        var dto = new CreateCurrencyDto
        {
            Code           = code,
            Name           = $"Test Currency {nameHint ?? code}",
            Symbol         = code[..2],
            DecimalPlaces  = 2,
            IsBaseCurrency = false,
        };

        var response = await _client.PostAsJsonAsync(BaseUrl, dto);
        var body     = (await response.Content.ReadFromJsonAsync<ApiResponse<CurrencyDto>>())!.Data!;
        _createdCurrencyIds.Add(body.Id);
        return body;
    }

    /// <summary>
    /// Ensures PKR (base currency) exists. Only creates it if not already present.
    /// </summary>
    private async Task<CurrencyDto> EnsureBaseCurrencyAsync()
    {
        var existing = await _client.GetAsync($"{BaseUrl}/PKR");
        if (existing.IsSuccessStatusCode)
            return (await existing.Content.ReadFromJsonAsync<ApiResponse<CurrencyDto>>())!.Data!;

        var dto = new CreateCurrencyDto
        {
            Code           = "PKR",
            Name           = "Pakistani Rupee",
            Symbol         = "Rs.",
            DecimalPlaces  = 2,
            IsBaseCurrency = true,
        };
        var resp = await _client.PostAsJsonAsync(BaseUrl, dto);
        var body = (await resp.Content.ReadFromJsonAsync<ApiResponse<CurrencyDto>>())!.Data!;
        _createdCurrencyIds.Add(body.Id);
        return body;
    }

    private async Task AddRateAsync(
        Guid currencyId,
        decimal rate,
        ExchangeRateType rateType,
        string? rateName,
        DateOnly? effectiveDate = null)
    {
        var dto = new CreateCurrencyRateDto
        {
            CurrencyId    = currencyId,
            Rate          = rate,
            RateType      = rateType,
            RateName      = rateName,
            EffectiveDate = effectiveDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
        };
        await _client.PostAsJsonAsync($"{BaseUrl}/{currencyId}/rates", dto);
    }

    private async Task<Guid> AddRateAndGetIdAsync(
        Guid currencyId,
        decimal rate,
        ExchangeRateType rateType,
        string? rateName,
        DateOnly effectiveDate)
    {
        var dto = new CreateCurrencyRateDto
        {
            CurrencyId    = currencyId,
            Rate          = rate,
            RateType      = rateType,
            RateName      = rateName,
            EffectiveDate = effectiveDate,
        };
        var resp = await _client.PostAsJsonAsync($"{BaseUrl}/{currencyId}/rates", dto);
        return (await resp.Content.ReadFromJsonAsync<ApiResponse<CurrencyRateDto>>())!.Data!.Id;
    }
}
