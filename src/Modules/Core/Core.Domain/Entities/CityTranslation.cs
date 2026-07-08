namespace Core.Domain.Entities;

/// <summary>Localized text for a <see cref="City"/>. Keyed by (CityId, LanguageCode).</summary>
public class CityTranslation
{
    /// <summary>FK to <see cref="City"/>.</summary>
    public int CityId { get; set; }

    /// <summary>BCP-47 language tag — FK to <see cref="Language"/>.</summary>
    public required string LanguageCode { get; set; }

    /// <summary>City name in the target language.</summary>
    public required string Name { get; set; }

    // ── Navigation ───────────────────────────────────────────────────────────
    public City? City { get; set; }
    public Language? Language { get; set; }
}
