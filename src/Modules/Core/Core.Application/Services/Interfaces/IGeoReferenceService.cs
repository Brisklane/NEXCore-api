using Core.Application.DTOs;

namespace Core.Application.Services.Interfaces;

/// <summary>
/// Read-only geographic reference data service.
/// Provides ISO 3166 countries, subdivisions, and cities.
/// All data is seeded at migration time - this service never writes.
/// </summary>
public interface IGeoReferenceService
{
    // ?? Phone codes (dial-code picker) ───────────────────────────────────────

    /// <summary>
    /// Lightweight list of all countries with their dial codes and flag URLs.
    /// Sorted by country name. Use for phone number input pickers.
    /// Countries that share a dial code (e.g. US/CA both use +1) appear as separate entries.
    /// </summary>
    Task<List<PhoneCodeDto>> GetPhoneCodesAsync(CancellationToken ct = default);

    // ?? Countries ?????????????????????????????????????????????????????????

    /// <summary>Returns all active countries, ordered by Name.</summary>
    Task<List<CountryDto>> GetCountriesAsync(CancellationToken ct = default);

    /// <summary>Returns a single country by its ISO 3166-1 alpha-2 code.</summary>
    Task<CountryDto?> GetCountryAsync(string countryCode, CancellationToken ct = default);

    // ?? Subdivisions ??????????????????????????????????????????????????????

    /// <summary>
    /// Returns all active subdivisions for a country, ordered by Name.
    /// Returns an empty list for countries with no seeded subdivisions.
    /// </summary>
    Task<List<SubdivisionDto>> GetSubdivisionsAsync(string countryCode, CancellationToken ct = default);

    // ?? Cities ????????????????????????????????????????????????????????????

    /// <summary>
    /// Returns cities for a country, optionally filtered by subdivision.
    /// Ordered by Population descending (largest cities first) then Name.
    /// </summary>
    Task<List<CityDto>> GetCitiesAsync(
        string countryCode,
        string? subdivisionCode = null,
        int limit = 200,
        CancellationToken ct = default);

    /// <summary>
    /// Searches cities by partial name within a country.
    /// Returns up to <paramref name="limit"/> results ordered by Population desc.
    /// </summary>
    Task<List<CityDto>> SearchCitiesAsync(
        string countryCode,
        string query,
        int limit = 20,
        CancellationToken ct = default);

    /// <summary>Returns a single city by its integer GeoNames Id.</summary>
    Task<CityDto?> GetCityByIdAsync(int cityId, CancellationToken ct = default);

    // ?? Address Format ????????????????????????????????????????????????????

    /// <summary>
    /// Returns the address form field template for a country.
    /// Falls back to "DEFAULT" template when no country-specific template exists.
    /// </summary>
    Task<AddressFormatDto> GetAddressFormatAsync(string countryCode, CancellationToken ct = default);

    // ?? Currencies ????????????????????????????????????????????????????????

    /// <summary>Returns all active ISO 4217 currencies, ordered by Code.</summary>
    Task<List<CurrencyDto>> GetCurrenciesAsync(CancellationToken ct = default);

    /// <summary>Returns a single currency by its ISO 4217 alpha-3 code (e.g. "USD").</summary>
    Task<CurrencyDto?> GetCurrencyAsync(string currencyCode, CancellationToken ct = default);

    // ?? Languages ?????????????????????????????????????????????????????????????

    /// <summary>Returns all active BCP-47 languages, ordered by Name.</summary>
    Task<List<LanguageDto>> GetLanguagesAsync(CancellationToken ct = default);

    /// <summary>Returns a single language by its BCP-47 code (e.g. "en", "ar").</summary>
    Task<LanguageDto?> GetLanguageAsync(string languageCode, CancellationToken ct = default);
}
