namespace Core.Domain.Entities;

/// <summary>ISO 4217 currency reference data. Seeded idempotently at startup.</summary>
public class Currency
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>ISO 4217 alpha-3 code, e.g. "USD", "GBP", "PKR".</summary>
    public required string Code { get; set; }

    /// <summary>Full English name, e.g. "United States Dollar".</summary>
    public required string Name { get; set; }

    /// <summary>Display symbol (e.g. "$", "GBP"); null when there is no standard symbol.</summary>
    public string? Symbol { get; set; }

    /// <summary>Minor-unit precision (ISO 4217 decimal places).</summary>
    public int DecimalPlaces { get; set; } = 2;

    public bool IsActive { get; set; } = true;

    // ── Navigation ───────────────────────────────────────────────────────────
    public ICollection<Country> Countries { get; set; } = new List<Country>();
    public ICollection<CurrencyTranslation> Translations { get; set; } = new List<CurrencyTranslation>();
}
