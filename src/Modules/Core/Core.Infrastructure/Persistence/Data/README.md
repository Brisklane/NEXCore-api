# Geo reference data

## `geonames-cities.tsv.gz`

Every populated place GeoNames lists with a population of 1,000 or more, plus all
seats of administration — **156,625 distinct cities across 246 countries**. Imported by
`CitySeedImporter` so the country → city pickers cover every country, not just the
nine hand-curated in `GeoReferenceSeed`.

**Source:** [GeoNames](https://www.geonames.org/) `cities1000.zip`
**Licence:** [Creative Commons Attribution 4.0](https://creativecommons.org/licenses/by/4.0/)
— attribution is required wherever this data is surfaced to end users.

### Format

Tab-separated, gzipped, sorted by country then population (largest first):

| # | Column       | Example      |
|---|--------------|--------------|
| 0 | Country code | `PK`         |
| 1 | Name         | `Karachi`    |
| 2 | Latitude     | `24.8608`    |
| 3 | Longitude    | `67.0104`    |
| 4 | Population   | `11624219`   |

Two deliberate choices:

- **Subdivision is omitted.** GeoNames `admin1` codes are not ISO 3166-2, so imported
  cities leave `SubdivisionCode` null rather than carry a code that would not match
  the seeded subdivisions.
- **Names are unique per country.** GeoNames has ~14,000 same-name duplicates within a
  country (30-odd `Springfield`s in the US alone). Without a state to disambiguate
  them, repeats are noise in a picker, so only the most populous of each name is kept.

### Refreshing

```bash
curl -O https://download.geonames.org/export/dump/cities1000.zip
unzip cities1000.zip

# columns → country, name, lat, lon, population; sorted; one row per country+name
awk -F'\t' 'BEGIN{OFS="\t"} $9!="" && $2!="" { print $9, $2, $5, $6, ($15+0) }' cities1000.txt \
  | sort -t$'\t' -k1,1 -k5,5nr \
  | awk -F'\t' 'BEGIN{OFS="\t"} { k = $1 "|" tolower($2); if (!(k in seen)) { seen[k]=1; print } }' \
  | gzip -9 > geonames-cities.tsv.gz
```

Then update `CitySeedImporter.DatasetRowCount` to the new line count — it is the guard
that stops the importer re-scanning on every startup.

The importer is idempotent: it skips any city whose (country, name) pair already
exists, so hand-curated entries — which carry `CityCode` and `PostalCode` — always win.
