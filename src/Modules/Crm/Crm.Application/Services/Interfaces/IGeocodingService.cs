namespace Crm.Application.Services.Interfaces;

public interface IGeocodingService
{
    /// <summary>
    /// Resolves GPS coordinates to a structured address.
    /// Returns null when the provider returns no result or an error occurs.
    /// </summary>
    Task<ReverseGeocodeResult?> ReverseGeocodeAsync(double latitude, double longitude);
}

public class ReverseGeocodeResult
{
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? CountryCode { get; set; }
    /// <summary>Full human-readable address string from the provider.</summary>
    public string? DisplayName { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
