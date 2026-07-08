using Microsoft.Extensions.Logging;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;
using Sales.Domain.Entities;
using Sales.Domain.Enums;
using Sales.Infrastructure.Repositories.Interfaces;

namespace Sales.Infrastructure.Services;

/// <summary>
/// Implements multi-currency exchange-rate lookup and conversion.
///
/// Rate resolution order for ConvertAsync / GetRateValueAsync:
///   1. Find the most recent CurrencyRate where EffectiveDate &lt;= asOfDate for the
///      requested RateType.
///   2. If the source currency IS the base currency, return rate = 1.
///   3. If no rate is found for the requested type, throw InvalidOperationException
///      so the caller knows to either use a fallback type or surface the error.
/// </summary>
public class CurrencyService : ICurrencyService
{
    private readonly ICurrencyRepository     _currencies;
    private readonly ICurrencyRateRepository _rates;
    private readonly ILogger<CurrencyService> _logger;

    public CurrencyService(
        ICurrencyRepository currencies,
        ICurrencyRateRepository rates,
        ILogger<CurrencyService> logger)
    {
        _currencies = currencies;
        _rates      = rates;
        _logger     = logger;
    }

    // ── Currency master ───────────────────────────────────────────────────────

    public async Task<List<CurrencyDto>> GetAllCurrenciesAsync()
    {
        var all  = await _currencies.GetAllAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var result = new List<CurrencyDto>();
        foreach (var c in all.OrderBy(c => c.Code))
        {
            var latestRate = await _rates.GetRateAsync(c.Code, await GetBaseCurrencyCodeAsync(), ExchangeRateType.Official, today);
            result.Add(MapCurrencyToDto(c, latestRate));
        }
        return result;
    }

    public async Task<CurrencyDto?> GetCurrencyByCodeAsync(string code)
    {
        var c = await _currencies.GetByCodeAsync(code);
        if (c == null) return null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var latestRate = await _rates.GetRateAsync(c.Code, await GetBaseCurrencyCodeAsync(), ExchangeRateType.Official, today);
        return MapCurrencyToDto(c, latestRate);
    }

    public async Task<CurrencyDto?> GetBaseCurrencyAsync()
    {
        var c = await _currencies.GetBaseCurrencyAsync();
        return c == null ? null : MapCurrencyToDto(c, null);
    }

    public async Task<CurrencyDto> CreateCurrencyAsync(CreateCurrencyDto dto)
    {
        var existing = await _currencies.GetByCodeAsync(dto.Code);
        if (existing != null)
            throw new InvalidOperationException($"Currency '{dto.Code}' already exists");

        if (dto.IsBaseCurrency)
        {
            // Un-flag any existing base currency first
            var current = await _currencies.GetBaseCurrencyAsync();
            if (current != null)
            {
                current.IsBaseCurrency = false;
                _currencies.Update(current);
            }
        }

        var currency = new Currency
        {
            Code           = dto.Code.ToUpperInvariant(),
            Name           = dto.Name,
            Symbol         = dto.Symbol,
            DecimalPlaces  = dto.DecimalPlaces,
            IsBaseCurrency = dto.IsBaseCurrency,
            IsActive       = true,
        };

        await _currencies.AddAsync(currency);
        await _currencies.SaveChangesAsync();

        _logger.LogInformation("Currency created: {Code} — {Name}", currency.Code, currency.Name);
        return MapCurrencyToDto(currency, null);
    }

    public async Task<CurrencyDto> UpdateCurrencyAsync(Guid id, UpdateCurrencyDto dto)
    {
        var currency = await _currencies.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Currency not found");

        if (dto.Name          != null) currency.Name          = dto.Name;
        if (dto.Symbol        != null) currency.Symbol        = dto.Symbol;
        if (dto.DecimalPlaces != null) currency.DecimalPlaces = dto.DecimalPlaces.Value;
        if (dto.IsActive      != null) currency.IsActive      = dto.IsActive.Value;

        _currencies.Update(currency);
        await _currencies.SaveChangesAsync();
        return MapCurrencyToDto(currency, null);
    }

    // ── Exchange rates ────────────────────────────────────────────────────────

    public async Task<List<CurrencyRateDto>> GetRatesForCurrencyAsync(Guid currencyId)
    {
        var rates = await _rates.GetByCurrencyAsync(currencyId);
        return rates.Select(MapRateToDto).ToList();
    }

    public async Task<CurrencyRateDto> AddRateAsync(CreateCurrencyRateDto dto)
    {
        var currency = await _currencies.GetByIdAsync(dto.CurrencyId)
            ?? throw new InvalidOperationException("Currency not found");

        var baseCurrencyCode = await GetBaseCurrencyCodeAsync();

        var rate = new CurrencyRate
        {
            CurrencyId       = dto.CurrencyId,
            CurrencyCode     = currency.Code,
            BaseCurrencyCode = baseCurrencyCode,
            Rate             = dto.Rate,
            RateType         = dto.RateType,
            RateName         = string.IsNullOrWhiteSpace(dto.RateName) ? null : dto.RateName.Trim(),
            EffectiveDate    = dto.EffectiveDate,
            ValidUntil       = dto.ValidUntil,
            Source           = dto.Source,
            Notes            = dto.Notes,
        };

        await _rates.AddAsync(rate);
        await _rates.SaveChangesAsync();

        _logger.LogInformation(
            "Rate added: {Code} {RateType} {Date} = {Rate} {Base}",
            currency.Code, dto.RateType, dto.EffectiveDate, dto.Rate, baseCurrencyCode);

        return MapRateToDto(rate);
    }

    public async Task<CurrencyRateDto> UpdateRateAsync(Guid rateId, UpdateCurrencyRateDto dto)
    {
        var rate = await _rates.GetByIdAsync(rateId)
            ?? throw new InvalidOperationException("Exchange rate not found");

        if (dto.Rate       != null) rate.Rate     = dto.Rate.Value;
        if (dto.RateName   != null) rate.RateName = string.IsNullOrWhiteSpace(dto.RateName) ? null : dto.RateName.Trim();
        if (dto.ValidUntil != null) rate.ValidUntil = dto.ValidUntil;
        if (dto.Source     != null) rate.Source   = dto.Source;
        if (dto.Notes      != null) rate.Notes    = dto.Notes;

        _rates.Update(rate);
        await _rates.SaveChangesAsync();
        return MapRateToDto(rate);
    }

    public async Task DeleteRateAsync(Guid rateId)
    {
        var rate = await _rates.GetByIdAsync(rateId)
            ?? throw new InvalidOperationException("Exchange rate not found");
        _rates.Delete(rate);
        await _rates.SaveChangesAsync();
    }

    public async Task<List<CurrencyRateDto>> GetLatestAllRatesAsync(DateOnly? asOfDate = null)
    {
        var date  = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var rates = await _rates.GetLatestAllAsync(date);
        return rates.Select(MapRateToDto).ToList();
    }

    public async Task<List<CurrencyRateDto>> GetRateHistoryAsync(
        string currencyCode,
        DateOnly fromDate,
        DateOnly toDate)
    {
        var baseCurrencyCode = await GetBaseCurrencyCodeAsync();
        var history = await _rates.GetHistoryAsync(currencyCode, baseCurrencyCode, fromDate, toDate);
        return history.Select(MapRateToDto).ToList();
    }

    // ── Conversion ────────────────────────────────────────────────────────────

    public async Task<CurrencyConversionResultDto> ConvertAsync(CurrencyConversionRequestDto request)
    {
        var baseCurrencyCode = await GetBaseCurrencyCodeAsync();
        var toCode           = request.ToCurrencyCode ?? baseCurrencyCode;
        var asOfDate         = request.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var (rate, rateEntry) = await ResolveRateAsync(
            request.FromCurrencyCode, toCode, request.RateType, asOfDate, baseCurrencyCode, request.RateName);

        return new CurrencyConversionResultDto
        {
            OriginalAmount   = request.Amount,
            FromCurrencyCode = request.FromCurrencyCode,
            ToCurrencyCode   = toCode,
            ConvertedAmount  = Math.Round(request.Amount * rate, 4),
            RateUsed         = rate,
            RateType         = request.RateType,
            RateDate         = rateEntry?.EffectiveDate ?? asOfDate,
            RateSource       = rateEntry?.Source,
        };
    }

    public async Task<BulkConversionResultDto> BulkConvertAsync(BulkConversionRequestDto request)
    {
        var baseCurrencyCode = await GetBaseCurrencyCodeAsync();
        var targetCode       = string.IsNullOrWhiteSpace(request.TargetCurrencyCode)
                               ? baseCurrencyCode
                               : request.TargetCurrencyCode;
        var globalDate       = request.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var resultLines = new List<BulkConversionLineResultDto>();
        decimal total   = 0m;

        foreach (var line in request.Lines)
        {
            var asOfDate = request.AsOfDate ?? line.DocumentDate ?? globalDate;

            decimal rate;
            DateOnly rateDate;

            if (line.CurrencyCode.Equals(targetCode, StringComparison.OrdinalIgnoreCase))
            {
                rate     = 1m;
                rateDate = asOfDate;
            }
            else
            {
                var (r, entry) = await ResolveRateAsync(
                    line.CurrencyCode, targetCode, request.RateType, asOfDate, baseCurrencyCode, request.RateName);
                rate     = r;
                rateDate = entry?.EffectiveDate ?? asOfDate;
            }

            var converted = Math.Round(line.Amount * rate, 4);
            total += converted;

            resultLines.Add(new BulkConversionLineResultDto
            {
                Reference            = line.Reference,
                OriginalAmount       = line.Amount,
                OriginalCurrencyCode = line.CurrencyCode,
                ConvertedAmount      = converted,
                RateUsed             = rate,
                RateDate             = rateDate,
            });
        }

        return new BulkConversionResultDto
        {
            TargetCurrencyCode = targetCode,
            RateType           = request.RateType,
            TotalConverted     = total,
            Lines              = resultLines,
        };
    }

    public async Task<decimal?> GetRateValueAsync(
        string fromCurrencyCode,
        string toCurrencyCode,
        ExchangeRateType rateType,
        DateOnly asOfDate,
        string? rateName = null)
    {
        var baseCurrencyCode = await GetBaseCurrencyCodeAsync();
        try
        {
            var (rate, _) = await ResolveRateAsync(fromCurrencyCode, toCurrencyCode, rateType, asOfDate, baseCurrencyCode, rateName);
            return rate;
        }
        catch
        {
            return null;
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<string> GetBaseCurrencyCodeAsync()
    {
        var base_ = await _currencies.GetBaseCurrencyAsync();
        return base_?.Code ?? "PKR";  // fallback so the system doesn't crash during setup
    }

    /// <summary>
    /// Resolves the exchange rate between two currencies.
    /// Handles:
    ///   • Same currency → rate = 1
    ///   • Direct pair   → fromCode → toCode
    ///   • Cross-rate    → fromCode → base → toCode (if neither is the base)
    /// </summary>
    private async Task<(decimal Rate, CurrencyRate? Entry)> ResolveRateAsync(
        string fromCode,
        string toCode,
        ExchangeRateType rateType,
        DateOnly asOfDate,
        string baseCurrencyCode,
        string? rateName = null)
    {
        if (fromCode.Equals(toCode, StringComparison.OrdinalIgnoreCase))
            return (1m, null);

        // Direct: foreign → base
        if (toCode.Equals(baseCurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            var entry = await _rates.GetRateAsync(fromCode, baseCurrencyCode, rateType, asOfDate, rateName)
                        ?? throw new InvalidOperationException(
                            BuildNotFoundMessage(fromCode, baseCurrencyCode, rateType, asOfDate, rateName));
            return (entry.Rate, entry);
        }

        // Reverse: base → foreign  (1 base = 1/rate foreign)
        if (fromCode.Equals(baseCurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            var entry = await _rates.GetRateAsync(toCode, baseCurrencyCode, rateType, asOfDate, rateName)
                        ?? throw new InvalidOperationException(
                            BuildNotFoundMessage(toCode, baseCurrencyCode, rateType, asOfDate, rateName));
            return (Math.Round(1m / entry.Rate, 8), entry);
        }

        // Cross-rate: fromCode → base → toCode
        var fromEntry = await _rates.GetRateAsync(fromCode, baseCurrencyCode, rateType, asOfDate, rateName)
                        ?? throw new InvalidOperationException(
                            BuildNotFoundMessage(fromCode, baseCurrencyCode, rateType, asOfDate, rateName));
        var toEntry   = await _rates.GetRateAsync(toCode,   baseCurrencyCode, rateType, asOfDate, rateName)
                        ?? throw new InvalidOperationException(
                            BuildNotFoundMessage(toCode, baseCurrencyCode, rateType, asOfDate, rateName));

        var crossRate = Math.Round(fromEntry.Rate / toEntry.Rate, 8);
        return (crossRate, fromEntry);
    }

    private static string BuildNotFoundMessage(
        string currencyCode, string baseCode, ExchangeRateType rateType, DateOnly date, string? rateName)
    {
        var nameHint = rateName != null ? $" (name: '{rateName}')" : string.Empty;
        return $"No {rateType} rate{nameHint} found for {currencyCode}/{baseCode} on or before {date}";
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static CurrencyDto MapCurrencyToDto(Currency c, CurrencyRate? latestRate) => new()
    {
        Id               = c.Id,
        Code             = c.Code,
        Name             = c.Name,
        Symbol           = c.Symbol,
        DecimalPlaces    = c.DecimalPlaces,
        IsBaseCurrency   = c.IsBaseCurrency,
        IsActive         = c.IsActive,
        LatestOfficialRate = latestRate?.Rate,
        LatestRateDate     = latestRate?.EffectiveDate,
    };

    private static CurrencyRateDto MapRateToDto(CurrencyRate r) => new()
    {
        Id               = r.Id,
        CurrencyId       = r.CurrencyId,
        CurrencyCode     = r.CurrencyCode,
        BaseCurrencyCode = r.BaseCurrencyCode,
        Rate             = r.Rate,
        RateType         = r.RateType,
        RateName         = r.RateName,
        EffectiveDate    = r.EffectiveDate,
        ValidUntil       = r.ValidUntil,
        Source           = r.Source,
        Notes            = r.Notes,
    };
}
