namespace Core.Domain.Entities;

/// <summary>Localized text for a <see cref="Subdivision"/>. Keyed by (SubdivisionCode, LanguageCode).</summary>
public class SubdivisionTranslation
{
    /// <summary>ISO 3166-2 code — FK to <see cref="Subdivision"/>.</summary>
    public required string SubdivisionCode { get; set; }

    /// <summary>BCP-47 language tag — FK to <see cref="Language"/>.</summary>
    public required string LanguageCode { get; set; }

    /// <summary>Subdivision name in the target language.</summary>
    public required string Name { get; set; }

    // ── Navigation ───────────────────────────────────────────────────────────
    public Subdivision? Subdivision { get; set; }
    public Language? Language { get; set; }
}
