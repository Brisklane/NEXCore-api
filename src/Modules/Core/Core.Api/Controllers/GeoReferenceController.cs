using Core.Application.DTOs;
using Core.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;

namespace Core.Api.Controllers;

/// <summary>
/// Read-only geographic reference data.
/// Returns ISO 3166 countries, subdivisions (states/provinces/emirates/etc.) and cities.
/// No authentication required - these are public lookup endpoints.
/// </summary>
[ApiController]
[Route("api/core/geo")]
[AllowAnonymous]
[Produces("application/json")]
public class GeoReferenceController : ControllerBase
{
    private readonly IGeoReferenceService _geo;
    private readonly ILogger<GeoReferenceController> _logger;

    public GeoReferenceController(
        IGeoReferenceService geo,
        ILogger<GeoReferenceController> logger)
    {
        _geo    = geo;
        _logger = logger;
    }

    // ?? Phone codes ??????????????????????????????????????????????????????

    /// <summary>
    /// All countries with their dial codes and flag images — optimised for phone-number pickers.
    ///
    /// Each entry includes:
    ///   • PhoneCode   — dial prefix, e.g. "+92"
    ///   • FlagEmoji   — Unicode flag, e.g. "🇵🇰"  (no HTTP request, works everywhere)
    ///   • FlagPng20   — https://flagcdn.com/w20/pk.png  (20 px, for small inline icons)
    ///   • FlagPng40   — https://flagcdn.com/w40/pk.png  (40 px, for retina displays)
    ///   • FlagSvgUrl  — https://flagcdn.com/pk.svg      (scalable, any size)
    ///
    /// Countries that share a code (e.g. US and CA both use +1) appear as separate entries.
    /// Results are sorted by CountryName.
    /// </summary>
    [HttpGet("phone-codes")]
    [ProducesResponseType(typeof(ApiResponse<List<PhoneCodeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPhoneCodes(CancellationToken ct)
    {
        try
        {
            var list = await _geo.GetPhoneCodesAsync(ct);
            return Ok(new ApiResponse<List<PhoneCodeDto>>
            {
                Success = true,
                Data    = list,
                Message = $"{list.Count} countries with dial codes",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching phone codes");
            return StatusCode(500, new ApiErrorResponse { Message = "Error fetching phone codes" });
        }
    }

    // ?? Countries ????????????????????????????????????????????????????????

    /// <summary>
    /// Get all active ISO 3166-1 countries ordered by name.
    /// Includes phone codes, currency codes, time zones and address-format keys.
    /// </summary>
    [HttpGet("countries")]
    [ProducesResponseType(typeof(ApiResponse<List<CountryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCountries(CancellationToken ct)
    {
        try
        {
            var countries = await _geo.GetCountriesAsync(ct);
            return Ok(new ApiResponse<List<CountryDto>>
            {
                Success = true,
                Data    = countries,
                Message = $"{countries.Count} countries"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching countries");
            return StatusCode(500, new ApiErrorResponse { Message = "Error fetching countries" });
        }
    }

    /// <summary>
    /// Get a single country by its ISO 3166-1 alpha-2 code (e.g. "PK", "AE", "US").
    /// </summary>
    [HttpGet("countries/{countryCode}")]
    [ProducesResponseType(typeof(ApiResponse<CountryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCountry(string countryCode, CancellationToken ct)
    {
        try
        {
            var country = await _geo.GetCountryAsync(countryCode, ct);
            if (country is null)
                return NotFound(new ApiErrorResponse { Message = $"Country '{countryCode}' not found" });

            return Ok(new ApiResponse<CountryDto> { Success = true, Data = country });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching country {Code}", countryCode);
            return StatusCode(500, new ApiErrorResponse { Message = "Error fetching country" });
        }
    }

    // ?? Subdivisions ??????????????????????????????????????????????????????

    /// <summary>
    /// Get all subdivisions (states / provinces / emirates / regions) for a country.
    /// Returns an empty list for countries with no seeded subdivisions.
    /// </summary>
    [HttpGet("countries/{countryCode}/subdivisions")]
    [ProducesResponseType(typeof(ApiResponse<List<SubdivisionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubdivisions(string countryCode, CancellationToken ct)
    {
        try
        {
            var subdivisions = await _geo.GetSubdivisionsAsync(countryCode, ct);
            return Ok(new ApiResponse<List<SubdivisionDto>>
            {
                Success = true,
                Data    = subdivisions,
                Message = $"{subdivisions.Count} subdivisions for {countryCode.ToUpperInvariant()}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching subdivisions for {Code}", countryCode);
            return StatusCode(500, new ApiErrorResponse { Message = "Error fetching subdivisions" });
        }
    }

    // ?? Cities ????????????????????????????????????????????????????????????

    /// <summary>
    /// Get cities for a country, optionally filtered by subdivision code.
    /// Results are ordered by population (largest first) then name.
    /// </summary>
    /// <param name="countryCode">ISO 3166-1 alpha-2 (e.g. "PK")</param>
    /// <param name="subdivisionCode">ISO 3166-2 code (e.g. "PK-PB"). Optional.</param>
    /// <param name="limit">Max results (1–500, default 200).</param>
    [HttpGet("countries/{countryCode}/cities")]
    [ProducesResponseType(typeof(ApiResponse<List<CityDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCities(
        string countryCode,
        [FromQuery] string? subdivisionCode,
        [FromQuery] int limit = 200,
        CancellationToken ct = default)
    {
        try
        {
            limit = Math.Clamp(limit, 1, 500);
            var cities = await _geo.GetCitiesAsync(countryCode, subdivisionCode, limit, ct);
            return Ok(new ApiResponse<List<CityDto>>
            {
                Success = true,
                Data    = cities,
                Message = $"{cities.Count} cities"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cities for {Code}", countryCode);
            return StatusCode(500, new ApiErrorResponse { Message = "Error fetching cities" });
        }
    }

    /// <summary>
    /// Search cities by partial name within a country.
    /// Returns up to <c>limit</c> results (default 20), ordered by population.
    /// </summary>
    /// <param name="countryCode">ISO 3166-1 alpha-2 (e.g. "PK")</param>
    /// <param name="q">Search term — at least 1 character (e.g. "Kar" → Karachi)</param>
    /// <param name="limit">Max results (1–100, default 20)</param>
    [HttpGet("countries/{countryCode}/cities/search")]
    [ProducesResponseType(typeof(ApiResponse<List<CityDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchCities(
        string countryCode,
        [FromQuery] string q,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new ApiErrorResponse { Message = "Query parameter 'q' is required" });

            limit = Math.Clamp(limit, 1, 100);
            var cities = await _geo.SearchCitiesAsync(countryCode, q, limit, ct);
            return Ok(new ApiResponse<List<CityDto>> { Success = true, Data = cities });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching cities for {Code}", countryCode);
            return StatusCode(500, new ApiErrorResponse { Message = "Error searching cities" });
        }
    }

    // ?? Address Format ????????????????????????????????????????????????????

    /// <summary>
    /// Returns the address form field template for a country - which fields to show,
    /// their labels, required flags and display order.
    /// Use this to dynamically render address forms in the UI per country.
    /// </summary>
    [HttpGet("countries/{countryCode}/address-format")]
    [ProducesResponseType(typeof(ApiResponse<AddressFormatDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAddressFormat(string countryCode, CancellationToken ct)
    {
        try
        {
            var format = await _geo.GetAddressFormatAsync(countryCode, ct);
            return Ok(new ApiResponse<AddressFormatDto> { Success = true, Data = format });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching address format for {Code}", countryCode);
            return StatusCode(500, new ApiErrorResponse { Message = "Error fetching address format" });
        }
    }

    // ?? Currencies ????????????????????????????????????????????????????????

    /// <summary>
    /// Get all active ISO 4217 currencies, ordered by code.
    /// </summary>
    [HttpGet("currencies")]
    [ProducesResponseType(typeof(ApiResponse<List<CurrencyDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrencies(CancellationToken ct)
    {
        try
        {
            var currencies = await _geo.GetCurrenciesAsync(ct);
            return Ok(new ApiResponse<List<CurrencyDto>>
            {
                Success = true,
                Data    = currencies,
                Message = $"{currencies.Count} currencies"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching currencies");
            return StatusCode(500, new ApiErrorResponse { Message = "Error fetching currencies" });
        }
    }

    /// <summary>
    /// Get a single currency by its ISO 4217 alpha-3 code (e.g. "USD", "PKR", "AED").
    /// </summary>
    [HttpGet("currencies/{currencyCode}")]
    [ProducesResponseType(typeof(ApiResponse<CurrencyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrency(string currencyCode, CancellationToken ct)
    {
        try
        {
            var currency = await _geo.GetCurrencyAsync(currencyCode, ct);
            if (currency is null)
                return NotFound(new ApiErrorResponse { Message = $"Currency '{currencyCode}' not found" });

            return Ok(new ApiResponse<CurrencyDto> { Success = true, Data = currency });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching currency {Code}", currencyCode);
            return StatusCode(500, new ApiErrorResponse { Message = "Error fetching currency" });
        }
    }

    // ?? Languages ?????????????????????????????????????????????????????????????

    /// <summary>
    /// Get all active BCP-47 languages ordered by name.
    /// </summary>
    [HttpGet("languages")]
    [ProducesResponseType(typeof(ApiResponse<List<LanguageDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLanguages(CancellationToken ct)
    {
        try
        {
            var languages = await _geo.GetLanguagesAsync(ct);
            return Ok(new ApiResponse<List<LanguageDto>>
            {
                Success = true,
                Data    = languages,
                Message = $"{languages.Count} languages"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching languages");
            return StatusCode(500, new ApiErrorResponse { Message = "Error fetching languages" });
        }
    }

    /// <summary>
    /// Get a single language by its BCP-47 code (e.g. "en", "ar", "fr").
    /// </summary>
    [HttpGet("languages/{languageCode}")]
    [ProducesResponseType(typeof(ApiResponse<LanguageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLanguage(string languageCode, CancellationToken ct)
    {
        try
        {
            var language = await _geo.GetLanguageAsync(languageCode, ct);
            if (language is null)
                return NotFound(new ApiErrorResponse { Message = $"Language '{languageCode}' not found" });

            return Ok(new ApiResponse<LanguageDto> { Success = true, Data = language });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching language {Code}", languageCode);
            return StatusCode(500, new ApiErrorResponse { Message = "Error fetching language" });
        }
    }
}
