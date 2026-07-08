namespace Nexcore.SharedKernel.ValueObjects;

/// <summary>
/// GPS coordinate value object for location-aware entities.
/// </summary>
public class GeoCoordinate
{
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public GeoCoordinate() { }

    public GeoCoordinate(decimal? latitude, decimal? longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public bool HasValue => Latitude.HasValue && Longitude.HasValue;

    public override string ToString() =>
        HasValue ? $"{Latitude},{Longitude}" : string.Empty;

    public override bool Equals(object? obj)
    {
        if (obj is not GeoCoordinate other) return false;
        return Latitude == other.Latitude && Longitude == other.Longitude;
    }

    public override int GetHashCode() => HashCode.Combine(Latitude, Longitude);
}
