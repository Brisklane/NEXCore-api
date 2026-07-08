namespace Core.Domain.Entities;

/// <summary>Localized text for a <see cref="Currency"/>. Keyed by (CurrencyId, LanguageCode).</summary>
public class CurrencyTranslation
{
    /// <summary>FK to <see cref="Currency"/>.</summary>
    public Guid CurrencyId { get; set; }

    /// <summary>BCP-47 language tag — FK to <see cref="Language"/>.</summary>
    public required string LanguageCode { get; set; }

    /// <summary>Currency name in the target language.</summary>
    public required string Name { get; set; }

    // ── Navigation ───────────────────────────────────────────────────────────
    public Currency? Currency { get; set; }
    public Language? Language { get; set; }
}
