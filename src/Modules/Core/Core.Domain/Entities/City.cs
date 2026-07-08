namespace Core.Domain.Entities;

/// <summary>
/// City / locality lookup, seeded from GeoNames for the busiest countries (others added on demand).
/// Other modules reference cities by name, not by FK.
/// </summary>
public class City
{
    public int Id { get; set; }

    public required string Name { get; set; }

    /// <summary>
    /// Short 2–4 letter code used when auto-generating entity codes (e.g. "ISB" for Islamabad,
    /// "LHE" for Lahore). Admin-configured; falls back to the first three letters of <see cref="Name"/>.
    /// </summary>
    public string? CityCode { get; set; }

    /// <summary>ISO 3166-1 alpha-2 country code — always set.</summary>
    public required string CountryCode { get; set; }

    /// <summary>ISO 3166-2 subdivision code — null for countries without seeded subdivisions.</summary>
    public string? SubdivisionCode { get; set; }

    /// <summary>Representative postal/ZIP code for the city centre; null where not applicable.</summary>
    public string? PostalCode { get; set; }

    /// <summary>GeoNames latitude (distance calculations / map pins).</summary>
    public decimal? Latitude { get; set; }

    /// <summary>GeoNames longitude.</summary>
    public decimal? Longitude { get; set; }

    /// <summary>GeoNames population — used to sort results largest-first.</summary>
    public int Population { get; set; }

    public bool IsActive { get; set; } = true;

    // ── Navigation ───────────────────────────────────────────────────────────
    public Country? Country { get; set; }
    public Subdivision? Subdivision { get; set; }
    public ICollection<CityTranslation> Translations { get; set; } = new List<CityTranslation>();
}
