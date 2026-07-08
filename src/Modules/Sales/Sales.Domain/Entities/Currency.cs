using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// Currency master record — one row per ISO 4217 currency the company transacts in.
/// The company's functional / base currency has IsBaseCurrency = true.
/// All exchange rates on sales documents are expressed as: 1 unit of this currency = Rate units of base.
/// </summary>
public class Currency : BaseEntity
{
    /// <summary>ISO 4217 code, e.g. "USD", "PKR", "EUR". Unique per tenant.</summary>
    public new string Code { get; set; } = string.Empty;

    /// <summary>Full name, e.g. "Pakistani Rupee", "US Dollar".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Display symbol printed on documents, e.g. "Rs.", "$", "€".</summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>Number of decimal places used when displaying amounts (2 for most, 0 for JPY).</summary>
    public int DecimalPlaces { get; set; } = 2;

    /// <summary>
    /// True for the company's functional/reporting currency (e.g., PKR).
    /// Exactly one currency per tenant should have this set.
    /// </summary>
    public bool IsBaseCurrency { get; set; }

    /// <summary>Historical exchange rates recorded for this currency against the base.</summary>
    public ICollection<CurrencyRate> Rates { get; set; } = new List<CurrencyRate>();
}

/// <summary>
/// One exchange rate entry for a currency on a specific date.
/// Multiple rows per currency are allowed — one per RateType (Official, Buying, Selling, Custom).
/// The rate is expressed as: 1 unit of the foreign currency = Rate units of the base currency.
///
/// Example: USD on 2026-05-30
///   Official  → 278.00 PKR
///   Buying    → 277.50 PKR  (bank buys USD from customer)
///   Selling   → 278.75 PKR  (bank sells USD to customer)
///
/// When reporting, the user chooses which RateType to apply.
/// </summary>
public class CurrencyRate : BaseEntity
{
    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;

    /// <summary>ISO code of the foreign currency this rate applies to (e.g., "USD").</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>ISO code of the base/target currency (e.g., "PKR").</summary>
    public string BaseCurrencyCode { get; set; } = string.Empty;

    /// <summary>1 unit of CurrencyCode = Rate units of BaseCurrencyCode.</summary>
    public decimal Rate { get; set; }

    public ExchangeRateType RateType { get; set; } = ExchangeRateType.Official;

    /// <summary>Date from which this rate is effective (inclusive).</summary>
    public DateOnly EffectiveDate { get; set; }

    /// <summary>Optional end date — null means "still in effect".</summary>
    public DateOnly? ValidUntil { get; set; }

    /// <summary>
    /// Human-readable label that distinguishes multiple rates of the same type on the same date.
    /// Examples: "State Bank", "Bank Al-Habib", "Bank Al-Falah", "Contract - Acme Corp".
    /// Null = the default rate for this RateType (used when no specific name is requested).
    /// </summary>
    public string? RateName { get; set; }

    /// <summary>Who provided this rate, e.g. "State Bank of Pakistan", "OANDA", "Manual".</summary>
    public string? Source { get; set; }

    public string? Notes { get; set; }
}
