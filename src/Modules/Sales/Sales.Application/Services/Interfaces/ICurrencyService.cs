using Sales.Application.DTOs;
using Sales.Domain.Enums;

namespace Sales.Application.Services.Interfaces;

/// <summary>
/// Currency and exchange-rate service.
/// Supports multiple rate types per currency pair per date so that
/// reporting can choose which rate (Official, Buying, Selling, Custom) to apply.
/// </summary>
public interface ICurrencyService
{
    // ── Currency master ───────────────────────────────────────────────────────
    Task<List<CurrencyDto>> GetAllCurrenciesAsync();
    Task<CurrencyDto?> GetCurrencyByCodeAsync(string code);
    Task<CurrencyDto?> GetBaseCurrencyAsync();
    Task<CurrencyDto> CreateCurrencyAsync(CreateCurrencyDto dto);
    Task<CurrencyDto> UpdateCurrencyAsync(Guid id, UpdateCurrencyDto dto);

    // ── Exchange rates ────────────────────────────────────────────────────────
    Task<List<CurrencyRateDto>> GetRatesForCurrencyAsync(Guid currencyId);
    Task<CurrencyRateDto> AddRateAsync(CreateCurrencyRateDto dto);
    Task<CurrencyRateDto> UpdateRateAsync(Guid rateId, UpdateCurrencyRateDto dto);
    Task DeleteRateAsync(Guid rateId);

    /// <summary>
    /// Current rates for every active currency as of today, for every rate type.
    /// Used for the rate dashboard / exchange rate overview screen.
    /// </summary>
    Task<List<CurrencyRateDto>> GetLatestAllRatesAsync(DateOnly? asOfDate = null);

    /// <summary>
    /// Rate history for a specific currency pair between two dates.
    /// All rate types are included so the caller can filter.
    /// </summary>
    Task<List<CurrencyRateDto>> GetRateHistoryAsync(
        string currencyCode,
        DateOnly fromDate,
        DateOnly toDate);

    // ── Conversion (core reporting method) ────────────────────────────────────

    /// <summary>
    /// Convert an amount using the best available rate for the requested type and date.
    /// Returns the converted amount, the exact rate used, and its source.
    /// Throws <see cref="InvalidOperationException"/> when no rate is found.
    /// </summary>
    Task<CurrencyConversionResultDto> ConvertAsync(CurrencyConversionRequestDto request);

    /// <summary>
    /// Bulk-convert a list of document amounts (e.g. all invoices in a report)
    /// to a single target currency using the chosen rate type.
    /// Lines whose currency already matches the target are passed through with rate = 1.
    /// </summary>
    Task<BulkConversionResultDto> BulkConvertAsync(BulkConversionRequestDto request);

    /// <summary>
    /// Raw rate lookup — returns null when no rate is available.
    /// Use this when you need the rate value only (e.g. to stamp on a document).
    /// </summary>
    Task<decimal?> GetRateValueAsync(
        string fromCurrencyCode,
        string toCurrencyCode,
        ExchangeRateType rateType,
        DateOnly asOfDate,
        string? rateName = null);
}
