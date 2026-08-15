using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence;

/// <summary>
/// Imports the full GeoNames city dataset — 156,625 distinct places across 246
/// countries — so the country → city pickers cover every country, not just the nine
/// hand-curated in <see cref="GeoReferenceSeed"/>.
///
/// Idempotent in both directions: it short-circuits once the import has run, and any
/// city whose (country, name) pair already exists is skipped — so the curated rows,
/// which carry <c>CityCode</c> and <c>PostalCode</c>, are never overwritten.
///
/// Data: GeoNames, CC BY 4.0. See Persistence/Data/README.md.
/// </summary>
public static class CitySeedImporter
{
    private const string ResourceName =
        "Core.Infrastructure.Persistence.Data.geonames-cities.tsv.gz";

    /// <summary>
    /// Rows in the shipped dataset. Once the table holds at least this many cities the
    /// import is considered done and the (relatively costly) dedupe scan is skipped.
    /// </summary>
    private const int DatasetRowCount = 156_625;

    /// <summary>Rows per SaveChanges call — keeps the change tracker from ballooning.</summary>
    private const int BatchSize = 5_000;

    public static async Task ImportAsync(CoreDbContext db, CancellationToken ct = default)
    {
        // Cheap guard: nothing to do once the dataset is in.
        if (await db.Cities.CountAsync(ct) >= DatasetRowCount) return;

        await using var stream = OpenDataset();
        if (stream is null) return;   // resource missing — leave the curated seed as-is

        // (country, name) pairs already present, so re-runs and curated rows are respected.
        var existing = await db.Cities
            .Select(c => new { c.CountryCode, c.Name })
            .ToListAsync(ct);

        var seen = new HashSet<string>(
            existing.Select(e => Key(e.CountryCode, e.Name)),
            StringComparer.OrdinalIgnoreCase);

        // Only import cities for countries we actually know about, so the FK stays valid.
        var knownCountries = await db.Countries
            .Select(c => c.Code)
            .ToListAsync(ct);
        var known = new HashSet<string>(knownCountries, StringComparer.OrdinalIgnoreCase);

        await using var gzip = new GZipStream(stream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);

        var batch = new List<City>(BatchSize);

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (line.Length == 0) continue;

            var parts = line.Split('\t');
            if (parts.Length < 5) continue;

            var countryCode = parts[0].Trim().ToUpperInvariant();
            var name = parts[1].Trim();

            if (countryCode.Length != 2 || name.Length == 0) continue;
            if (!known.Contains(countryCode)) continue;
            if (!seen.Add(Key(countryCode, name))) continue;   // duplicate or curated

            batch.Add(new City
            {
                Name = name,
                CountryCode = countryCode,
                SubdivisionCode = null,   // GeoNames admin1 is not ISO 3166-2 — see README
                Latitude = ParseCoordinate(parts[2]),
                Longitude = ParseCoordinate(parts[3]),
                Population = ParsePopulation(parts[4]),
                IsActive = true,
            });

            if (batch.Count >= BatchSize)
                await FlushAsync(db, batch, ct);
        }

        if (batch.Count > 0)
            await FlushAsync(db, batch, ct);
    }

    private static async Task FlushAsync(CoreDbContext db, List<City> batch, CancellationToken ct)
    {
        db.Cities.AddRange(batch);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        batch.Clear();
    }

    private static Stream? OpenDataset() =>
        Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);

    private static string Key(string? countryCode, string? name) =>
        $"{countryCode}|{name}";

    private static decimal? ParseCoordinate(string value) =>
        decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
            ? decimal.Round(d, 6)
            : null;

    private static int ParsePopulation(string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var p) && p > 0
            ? p
            : 0;
}
