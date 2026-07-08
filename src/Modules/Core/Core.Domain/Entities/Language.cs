namespace Core.Domain.Entities;

/// <summary>
/// ISO 639-1 / BCP-47 language reference data, seeded idempotently at startup. The primary key is
/// the BCP-47 tag (e.g. "en", "ar", "fr").
/// </summary>
public class Language
{
    /// <summary>BCP-47 language tag, e.g. "en", "ar", "fr", "ur".</summary>
    public required string Code { get; set; }

    /// <summary>English name, e.g. "English", "Arabic", "French".</summary>
    public required string Name { get; set; }

    /// <summary>The language's own name, written in its native script.</summary>
    public required string NativeName { get; set; }

    /// <summary>True for right-to-left scripts (Arabic, Urdu, Hebrew, …).</summary>
    public bool IsRtl { get; set; }

    public bool IsActive { get; set; } = true;

    // ── Navigation ───────────────────────────────────────────────────────────
    public ICollection<CountryTranslation> CountryTranslations { get; set; } = new List<CountryTranslation>();
    public ICollection<SubdivisionTranslation> SubdivisionTranslations { get; set; } = new List<SubdivisionTranslation>();
    public ICollection<CityTranslation> CityTranslations { get; set; } = new List<CityTranslation>();
    public ICollection<CurrencyTranslation> CurrencyTranslations { get; set; } = new List<CurrencyTranslation>();
}
