namespace Core.Domain.Entities;

/// <summary>
/// ISO 3166-2 subdivision — state, province, region, emirate, territory, etc. The primary key is
/// the ISO 3166-2 code (e.g. "US-CA", "PK-PB", "AE-DU").
/// </summary>
public class Subdivision
{
    /// <summary>ISO 3166-2 code, e.g. "US-CA", "PK-PB", "AE-DU".</summary>
    public required string Code { get; set; }

    /// <summary>Parent country's ISO 3166-1 alpha-2 code (FK to <see cref="Country"/>).</summary>
    public required string CountryCode { get; set; }

    /// <summary>English name, e.g. "California", "Punjab", "Dubai".</summary>
    public required string Name { get; set; }

    /// <summary>UI label for the subdivision kind: "State", "Province", "Emirate", "Region", "District", …</summary>
    public string SubdivisionType { get; set; } = "State";

    public bool IsActive { get; set; } = true;

    // ── Navigation ───────────────────────────────────────────────────────────
    public Country? Country { get; set; }
    public ICollection<City> Cities { get; set; } = new List<City>();
    public ICollection<SubdivisionTranslation> Translations { get; set; } = new List<SubdivisionTranslation>();
}
