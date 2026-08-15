using Core.Application.DTOs;
using Core.Application.Services.Interfaces;
using Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.Services;

/// <summary>
/// Read-only geographic reference data — countries, subdivisions, cities.
/// Results are cached in IMemoryCache (singleton, shared across all requests) so the
/// DB is only queried once per cache key per application lifetime.
/// </summary>
public class GeoReferenceService : IGeoReferenceService
{
    private readonly CoreDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GeoReferenceService> _logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(1);

    private const string KeyCountries  = "geo:countries";
    private const string KeyCurrencies = "geo:currencies";
    private const string KeyLanguages  = "geo:languages";
    private const string KeyPhoneCodes = "geo:phone-codes";

    public GeoReferenceService(
        CoreDbContext db,
        IMemoryCache cache,
        ILogger<GeoReferenceService> logger)
    {
        _db     = db;
        _cache  = cache;
        _logger = logger;
    }

    // ── Phone codes ───────────────────────────────────────────────────────────

    public async Task<List<PhoneCodeDto>> GetPhoneCodesAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(KeyPhoneCodes, out List<PhoneCodeDto>? cached) && cached is not null)
            return cached;

        var countries = await GetCountriesAsync(ct);

        var result = countries
            .Where(c => !string.IsNullOrEmpty(c.PhoneCode))
            .OrderBy(c => c.Name)
            .Select(c =>
            {
                var code = c.Code.ToLowerInvariant();
                return new PhoneCodeDto
                {
                    CountryCode = c.Code,
                    CountryName = c.Name,
                    PhoneCode   = c.PhoneCode!,
                    FlagEmoji   = c.FlagEmoji ?? string.Empty,
                    FlagPng20   = $"https://flagcdn.com/w20/{code}.png",
                    FlagPng40   = $"https://flagcdn.com/w40/{code}.png",
                    FlagSvgUrl  = $"https://flagcdn.com/{code}.svg",
                };
            })
            .ToList();

        _cache.Set(KeyPhoneCodes, result, CacheTtl);
        return result;
    }

    // ── Countries ─────────────────────────────────────────────────────────────

    public async Task<List<CountryDto>> GetCountriesAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(KeyCountries, out List<CountryDto>? cached) && cached is not null)
            return cached;

        var result = await _db.Countries
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => MapCountry(c))
            .ToListAsync(ct);

        _cache.Set(KeyCountries, result, CacheTtl);
        _logger.LogDebug("GeoReference: loaded {Count} countries into cache", result.Count);
        return result;
    }

    public async Task<CountryDto?> GetCountryAsync(string countryCode, CancellationToken ct = default)
    {
        var countries = await GetCountriesAsync(ct);
        return countries.FirstOrDefault(c =>
            c.Code.Equals(countryCode, StringComparison.OrdinalIgnoreCase));
    }

    // ── Subdivisions ──────────────────────────────────────────────────────────

    public async Task<List<SubdivisionDto>> GetSubdivisionsAsync(
        string countryCode, CancellationToken ct = default)
    {
        var key = $"geo:subdivisions:{countryCode.ToUpperInvariant()}";
        if (_cache.TryGetValue(key, out List<SubdivisionDto>? cached) && cached is not null)
            return cached;

        var result = await _db.Subdivisions
            .Where(s => s.CountryCode == countryCode.ToUpperInvariant() && s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new SubdivisionDto
            {
                Code            = s.Code,
                CountryCode     = s.CountryCode,
                Name            = s.Name,
                SubdivisionType = s.SubdivisionType,
                IsActive        = s.IsActive,
            })
            .ToListAsync(ct);

        _cache.Set(key, result, CacheTtl);
        return result;
    }

    // ── Cities ────────────────────────────────────────────────────────────────

    public async Task<List<CityDto>> GetCitiesAsync(
        string countryCode,
        string? subdivisionCode = null,
        int limit = 200,
        CancellationToken ct = default)
    {
        var key = $"geo:cities:{countryCode.ToUpperInvariant()}:{subdivisionCode?.ToUpperInvariant() ?? "*"}:{limit}";
        if (_cache.TryGetValue(key, out List<CityDto>? cached) && cached is not null)
            return cached;

        var query = _db.Cities
            .Where(c => c.CountryCode == countryCode.ToUpperInvariant() && c.IsActive);

        if (!string.IsNullOrWhiteSpace(subdivisionCode))
            query = query.Where(c => c.SubdivisionCode == subdivisionCode.ToUpperInvariant());

        var result = await query
            .OrderByDescending(c => c.Population)
            .ThenBy(c => c.Name)
            .Take(limit)
            .Select(c => MapCity(c))
            .ToListAsync(ct);

        _cache.Set(key, result, CacheTtl);
        return result;
    }

    public async Task<List<CityDto>> SearchCitiesAsync(
        string countryCode,
        string query,
        int limit = 20,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        // ILIKE, not StartsWith: LIKE is case-sensitive in PostgreSQL, so "kar" would
        // never match "Karachi". Matching anywhere in the name also lets people find
        // "New York" by typing "york", which is how users expect a city box to behave.
        var pattern = $"%{EscapeLike(query.Trim())}%";

        return await _db.Cities
            .Where(c => c.CountryCode == countryCode.ToUpperInvariant()
                     && c.IsActive
                     && EF.Functions.ILike(c.Name, pattern, "\\"))
            // Prefix matches first, then by size — "york" surfaces York before New York.
            .OrderByDescending(c => EF.Functions.ILike(c.Name, $"{EscapeLike(query.Trim())}%", "\\"))
            .ThenByDescending(c => c.Population)
            .ThenBy(c => c.Name)
            .Take(limit)
            .Select(c => MapCity(c))
            .ToListAsync(ct);
    }

    public async Task<CityDto?> GetCityByIdAsync(int cityId, CancellationToken ct = default)
    {
        var city = await _db.Cities.FirstOrDefaultAsync(c => c.Id == cityId && c.IsActive, ct);
        return city is null ? null : MapCity(city);
    }

    // ── Address Format ────────────────────────────────────────────────────────

    public async Task<AddressFormatDto> GetAddressFormatAsync(
        string countryCode, CancellationToken ct = default)
    {
        var country = await GetCountryAsync(countryCode, ct);
        var templateKey = country?.AddressFormat ?? "DEFAULT";

        return new AddressFormatDto
        {
            CountryCode = countryCode.ToUpperInvariant(),
            TemplateKey = templateKey,
            Fields      = BuildFieldTemplate(templateKey, country),
        };
    }

    // ── Currencies ────────────────────────────────────────────────────────────

    public async Task<List<CurrencyDto>> GetCurrenciesAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(KeyCurrencies, out List<CurrencyDto>? cached) && cached is not null)
            return cached;

        var result = await _db.Currencies
            .Where(c => c.IsActive)
            .OrderBy(c => c.Code)
            .Select(c => new CurrencyDto
            {
                Id            = c.Id,
                Code          = c.Code,
                Name          = c.Name,
                Symbol        = c.Symbol,
                DecimalPlaces = c.DecimalPlaces,
                IsActive      = c.IsActive,
            })
            .ToListAsync(ct);

        _cache.Set(KeyCurrencies, result, CacheTtl);
        _logger.LogDebug("GeoReference: loaded {Count} currencies into cache", result.Count);
        return result;
    }

    public async Task<CurrencyDto?> GetCurrencyAsync(string currencyCode, CancellationToken ct = default)
    {
        var currencies = await GetCurrenciesAsync(ct);
        return currencies.FirstOrDefault(c =>
            c.Code.Equals(currencyCode, StringComparison.OrdinalIgnoreCase));
    }

    // ── Languages ─────────────────────────────────────────────────────────────

    public async Task<List<LanguageDto>> GetLanguagesAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(KeyLanguages, out List<LanguageDto>? cached) && cached is not null)
            return cached;

        var result = await _db.Languages
            .Where(l => l.IsActive)
            .OrderBy(l => l.Name)
            .Select(l => new LanguageDto
            {
                Code       = l.Code,
                Name       = l.Name,
                NativeName = l.NativeName,
                IsRtl      = l.IsRtl,
                IsActive   = l.IsActive,
            })
            .ToListAsync(ct);

        _cache.Set(KeyLanguages, result, CacheTtl);
        _logger.LogDebug("GeoReference: loaded {Count} languages into cache", result.Count);
        return result;
    }

    public async Task<LanguageDto?> GetLanguageAsync(string languageCode, CancellationToken ct = default)
    {
        var languages = await GetLanguagesAsync(ct);
        return languages.FirstOrDefault(l =>
            l.Code.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CountryDto MapCountry(Core.Domain.Entities.Country c) => new()
    {
        Code              = c.Code,
        Code3             = c.Code3,
        NumericCode       = c.NumericCode,
        Name              = c.Name,
        OfficialName      = c.OfficialName,
        Region            = c.Region,
        SubRegion         = c.SubRegion,
        CurrencyCode      = c.CurrencyCode,
        PhoneCode         = c.PhoneCode,
        TimeZone          = c.TimeZone,
        FlagEmoji         = c.FlagEmoji,
        PostalCodePattern = c.PostalCodePattern,
        AddressFormat     = c.AddressFormat,
        StateLabel        = c.StateLabel,
        PostalCodeLabel   = c.PostalCodeLabel,
        IsActive          = c.IsActive,
    };

    /// <summary>
    /// Neutralises LIKE wildcards in user input so a query of "%" doesn't match every
    /// row. Paired with an explicit ESCAPE '\' on the ILike call.
    /// </summary>
    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private static CityDto MapCity(Core.Domain.Entities.City c) => new()
    {
        Id              = c.Id,
        Name            = c.Name,
        CountryCode     = c.CountryCode,
        SubdivisionCode = c.SubdivisionCode,
        PostalCode      = c.PostalCode,
        CityCode        = c.CityCode,
        Latitude        = c.Latitude,
        Longitude       = c.Longitude,
        Population      = c.Population,
    };

    private static List<AddressFieldDto> BuildFieldTemplate(
        string templateKey, CountryDto? country)
    {
        var stateLabel      = country?.StateLabel      ?? "State / Province";
        var postalCodeLabel = country?.PostalCodeLabel ?? "Postal Code";
        bool hasPostalCode  = postalCodeLabel is not null;

        return templateKey switch
        {
            // US / Canada
            "US" or "CA" =>
            [
                new() { FieldKey="Street1",    Label="Street Address",  IsRequired=true,  DisplayOrder=1 },
                new() { FieldKey="Street2",    Label="Apt / Suite",     IsRequired=false, DisplayOrder=2 },
                new() { FieldKey="City",       Label="City",            IsRequired=true,  DisplayOrder=3 },
                new() { FieldKey="State",      Label=stateLabel,        IsRequired=true,  DisplayOrder=4 },
                new() { FieldKey="PostalCode", Label=postalCodeLabel!,  IsRequired=true,  DisplayOrder=5 },
                new() { FieldKey="Country",    Label="Country",         IsRequired=true,  DisplayOrder=6 },
            ],

            // UK / Ireland
            "UK" =>
            [
                new() { FieldKey="Street1",    Label="Address Line 1",  IsRequired=true,  DisplayOrder=1 },
                new() { FieldKey="Street2",    Label="Address Line 2",  IsRequired=false, DisplayOrder=2 },
                new() { FieldKey="City",       Label="Town / City",     IsRequired=true,  DisplayOrder=3 },
                new() { FieldKey="State",      Label=stateLabel,        IsRequired=false, DisplayOrder=4 },
                new() { FieldKey="PostalCode", Label="Postcode",        IsRequired=true,  DisplayOrder=5 },
                new() { FieldKey="Country",    Label="Country",         IsRequired=true,  DisplayOrder=6 },
            ],

            // UAE / Gulf (no postal codes)
            "AE" =>
            [
                new() { FieldKey="Street1",    Label="Building / Street", IsRequired=true,  DisplayOrder=1 },
                new() { FieldKey="Street2",    Label="Area / District",   IsRequired=false, DisplayOrder=2 },
                new() { FieldKey="City",       Label="City",              IsRequired=true,  DisplayOrder=3 },
                new() { FieldKey="State",      Label=stateLabel,          IsRequired=true,  DisplayOrder=4 },
                new() { FieldKey="PostalCode", Label="Postal Code",       IsRequired=false, IsVisible=false, DisplayOrder=5 },
                new() { FieldKey="Country",    Label="Country",           IsRequired=true,  DisplayOrder=6 },
            ],

            // Pakistan
            "PAK" =>
            [
                new() { FieldKey="Street1",    Label="House / Plot No., Street", IsRequired=true,  DisplayOrder=1 },
                new() { FieldKey="Street2",    Label="Area / Sector",            IsRequired=false, DisplayOrder=2 },
                new() { FieldKey="City",       Label="City",                     IsRequired=true,  DisplayOrder=3 },
                new() { FieldKey="State",      Label="Province",                 IsRequired=true,  DisplayOrder=4 },
                new() { FieldKey="PostalCode", Label="Postal Code",              IsRequired=false, DisplayOrder=5 },
                new() { FieldKey="Country",    Label="Country",                  IsRequired=true,  DisplayOrder=6 },
            ],

            // India
            "IN" =>
            [
                new() { FieldKey="Street1",    Label="Flat / House No., Building",   IsRequired=true,  DisplayOrder=1 },
                new() { FieldKey="Street2",    Label="Colony / Area / Locality",     IsRequired=false, DisplayOrder=2 },
                new() { FieldKey="City",       Label="City / District",              IsRequired=true,  DisplayOrder=3 },
                new() { FieldKey="State",      Label="State",                        IsRequired=true,  DisplayOrder=4 },
                new() { FieldKey="PostalCode", Label="PIN Code",                     IsRequired=true,  DisplayOrder=5 },
                new() { FieldKey="Country",    Label="Country",                      IsRequired=true,  DisplayOrder=6 },
            ],

            // DEFAULT — covers all remaining countries
            _ =>
            [
                new() { FieldKey="Street1",    Label="Street Address",  IsRequired=true,  DisplayOrder=1 },
                new() { FieldKey="Street2",    Label="Address Line 2",  IsRequired=false, DisplayOrder=2 },
                new() { FieldKey="City",       Label="City",            IsRequired=true,  DisplayOrder=3 },
                new() { FieldKey="State",      Label=stateLabel,        IsRequired=false, DisplayOrder=4 },
                new() { FieldKey="PostalCode", Label=postalCodeLabel ?? "Postal Code",
                        IsRequired=false, IsVisible=hasPostalCode, DisplayOrder=5 },
                new() { FieldKey="Country",    Label="Country",         IsRequired=true,  DisplayOrder=6 },
            ],
        };
    }
}
