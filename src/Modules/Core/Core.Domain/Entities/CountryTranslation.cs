namespace Core.Domain.Entities;

/// <summary>Localized text for a <see cref="Country"/>. Keyed by (CountryCode, LanguageCode).</summary>
public class CountryTranslation
{
    /// <summary>ISO 3166-1 alpha-2 code — FK to <see cref="Country"/>.</summary>
    public required string CountryCode { get; set; }

    /// <summary>BCP-47 language tag — FK to <see cref="Language"/>.</summary>
    public required string LanguageCode { get; set; }

    /// <summary>Country name in the target language.</summary>
    public required string Name { get; set; }

    /// <summary>Official name in the target language; null means same as <see cref="Name"/>.</summary>
    public string? OfficialName { get; set; }

    // ── Navigation ───────────────────────────────────────────────────────────
    public Country? Country { get; set; }
    public Language? Language { get; set; }
}
