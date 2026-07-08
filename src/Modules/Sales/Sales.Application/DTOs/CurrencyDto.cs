using Sales.Domain.Enums;

namespace Sales.Application.DTOs;

// ── Currency master ───────────────────────────────────────────────────────────

public class CurrencyDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int DecimalPlaces { get; set; }
    public bool IsBaseCurrency { get; set; }
    public bool IsActive { get; set; }
    /// <summary>Most recent Official rate against the base currency, if available.</summary>
    public decimal? LatestOfficialRate { get; set; }
    public DateOnly? LatestRateDate { get; set; }
}

public class CreateCurrencyDto
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(3, MinimumLength = 3)]
    public string Code { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    public string Name { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    public string Symbol { get; set; } = string.Empty;

    public int DecimalPlaces { get; set; } = 2;
    public bool IsBaseCurrency { get; set; }
}

public class UpdateCurrencyDto
{
    public string? Name { get; set; }
    public string? Symbol { get; set; }
    public int? DecimalPlaces { get; set; }
    public bool? IsActive { get; set; }
}

// ── Exchange Rate ─────────────────────────────────────────────────────────────

public class CurrencyRateDto
{
    public Guid Id { get; set; }
    public Guid CurrencyId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string BaseCurrencyCode { get; set; } = string.Empty;
    /// <summary>1 unit of CurrencyCode = Rate units of BaseCurrencyCode.</summary>
    public decimal Rate { get; set; }
    public ExchangeRateType RateType { get; set; }
    /// <summary>
    /// Label distinguishing multiple rates of the same type on the same date.
    /// e.g. "State Bank", "Bank Al-Habib", "Contract - Acme Corp".
    /// Null = default rate for this type.
    /// </summary>
    public string? RateName { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public string? Source { get; set; }
    public string? Notes { get; set; }
}

public class CreateCurrencyRateDto
{
    [System.ComponentModel.DataAnnotations.Required]
    public Guid CurrencyId { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.Range(0.000001, double.MaxValue, ErrorMessage = "Rate must be positive")]
    public decimal Rate { get; set; }

    public ExchangeRateType RateType { get; set; } = ExchangeRateType.Official;

    /// <summary>
    /// Optional label to distinguish this rate from others of the same type on the same date.
    /// e.g. "State Bank", "Bank Al-Habib Buying", "Contract - Acme Corp".
    /// Leave null for the generic/default rate of this type.
    /// </summary>
    public string? RateName { get; set; }

    public DateOnly EffectiveDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public DateOnly? ValidUntil { get; set; }

    public string? Source { get; set; }

    public string? Notes { get; set; }
}

public class UpdateCurrencyRateDto
{
    public decimal? Rate { get; set; }
    public string? RateName { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public string? Source { get; set; }
    public string? Notes { get; set; }
}

// ── Currency conversion (for reporting) ───────────────────────────────────────

/// <summary>
/// Request body for POST /api/sales/currency/convert.
/// Used by reporting to convert amounts using a chosen rate type and date.
/// </summary>
public class CurrencyConversionRequestDto
{
    [System.ComponentModel.DataAnnotations.Required]
    public decimal Amount { get; set; }

    /// <summary>ISO code of the source currency, e.g. "USD".</summary>
    [System.ComponentModel.DataAnnotations.Required]
    public string FromCurrencyCode { get; set; } = string.Empty;

    /// <summary>ISO code of the target currency, e.g. "PKR". Defaults to base currency.</summary>
    public string? ToCurrencyCode { get; set; }

    /// <summary>Which rate type to use — Official, Buying, Selling, or Custom.</summary>
    public ExchangeRateType RateType { get; set; } = ExchangeRateType.Official;

    /// <summary>
    /// Optional: pick a specific named rate within the RateType.
    /// e.g. "Bank Al-Habib" to use that bank's buying rate specifically.
    /// When null, the most recent default (unnamed) rate for the type is used.
    /// </summary>
    public string? RateName { get; set; }

    /// <summary>Use the rate effective on this date. Defaults to today.</summary>
    public DateOnly? AsOfDate { get; set; }
}

public class CurrencyConversionResultDto
{
    public decimal OriginalAmount { get; set; }
    public string FromCurrencyCode { get; set; } = string.Empty;
    public string ToCurrencyCode { get; set; } = string.Empty;
    public decimal ConvertedAmount { get; set; }
    /// <summary>The actual rate used (1 unit of From = Rate units of To).</summary>
    public decimal RateUsed { get; set; }
    public ExchangeRateType RateType { get; set; }
    public DateOnly RateDate { get; set; }
    public string? RateSource { get; set; }
}

// ── Bulk document re-valuation (for reporting) ────────────────────────────────

/// <summary>
/// Revalue a list of amounts (from different orders/invoices) using a chosen rate type.
/// Used when generating consolidated reports across multiple currencies.
/// </summary>
public class BulkConversionRequestDto
{
    public List<BulkConversionLineDto> Lines { get; set; } = [];
    public string TargetCurrencyCode { get; set; } = string.Empty;
    public ExchangeRateType RateType { get; set; } = ExchangeRateType.Official;
    /// <summary>Optional: use a specific named rate (e.g. "State Bank") for all lines.</summary>
    public string? RateName { get; set; }
    /// <summary>Use a single date for all conversions (e.g., report end date).</summary>
    public DateOnly? AsOfDate { get; set; }
}

public class BulkConversionLineDto
{
    public string Reference { get; set; } = string.Empty;    // order/invoice number
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public DateOnly? DocumentDate { get; set; }               // use document's own date if AsOfDate null
}

public class BulkConversionResultDto
{
    public string TargetCurrencyCode { get; set; } = string.Empty;
    public ExchangeRateType RateType { get; set; }
    public decimal TotalConverted { get; set; }
    public List<BulkConversionLineResultDto> Lines { get; set; } = [];
}

public class BulkConversionLineResultDto
{
    public string Reference { get; set; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public string OriginalCurrencyCode { get; set; } = string.Empty;
    public decimal ConvertedAmount { get; set; }
    public decimal RateUsed { get; set; }
    public DateOnly RateDate { get; set; }
}
