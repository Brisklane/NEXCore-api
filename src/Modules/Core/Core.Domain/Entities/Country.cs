namespace Core.Domain.Entities;

/// <summary>
/// ISO country reference data plus the per-country address-form hints (labels, postal pattern,
/// layout key) the UI uses to render the right address fields. Seeded, read-mostly lookup.
/// </summary>
public class Country
{
    /// <summary>ISO 3166-1 alpha-2 code (e.g. "US", "GB", "IN").</summary>
    public required string Code { get; set; }

    /// <summary>ISO 3166-1 alpha-3 code (e.g. "USA", "GBR", "IND").</summary>
    public required string Code3 { get; set; }

    /// <summary>ISO 3166-1 numeric code.</summary>
    public int NumericCode { get; set; }

    public required string Name { get; set; }
    public string? OfficialName { get; set; }

    public string? Region { get; set; }
    public string? SubRegion { get; set; }

    public string? CurrencyCode { get; set; }

    /// <summary>FK to <see cref="Currency"/> — the country's default currency; null until currencies are seeded.</summary>
    public Guid? DefaultCurrencyId { get; set; }

    /// <summary>Telephone country code (e.g. "+1", "+44", "+91").</summary>
    public string? PhoneCode { get; set; }

    /// <summary>IANA timezone for the main/capital zone (e.g. "Asia/Karachi", "Europe/London").</summary>
    public string? TimeZone { get; set; }

    /// <summary>Unicode flag emoji (two regional-indicator letters).</summary>
    public string? FlagEmoji { get; set; }

    /// <summary>Postal-code regex for UI validation; null when the country has no postal codes (e.g. UAE).</summary>
    public string? PostalCodePattern { get; set; }

    /// <summary>Address-layout key telling the UI which fields to show: "US", "UK", "PAK", "AE", "IN", "CA", "DEFAULT".</summary>
    public string AddressFormat { get; set; } = "DEFAULT";

    /// <summary>Label for the state/province field in this country's address form.</summary>
    public string StateLabel { get; set; } = "State / Province";

    /// <summary>Label for the postal-code field; null hides the field.</summary>
    public string? PostalCodeLabel { get; set; } = "Postal Code";

    public bool IsActive { get; set; } = true;

    // ── Navigation ───────────────────────────────────────────────────────────
    public Currency? DefaultCurrency { get; set; }
    public ICollection<Subdivision> Subdivisions { get; set; } = new List<Subdivision>();
    public ICollection<City> Cities { get; set; } = new List<City>();
    public ICollection<CountryTranslation> Translations { get; set; } = new List<CountryTranslation>();
}
