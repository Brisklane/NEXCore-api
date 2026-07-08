using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Crm.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Crm.Infrastructure.Services;

/// <summary>
/// Reverse geocoding via OpenStreetMap Nominatim (free, no API key required).
/// Rate limit: 1 request/second per Nominatim policy.
/// Replace with Google Maps or HERE if higher volume or better accuracy is needed.
/// </summary>
public class NominatimGeocodingService : IGeocodingService
{
    private readonly HttpClient _http;
    private readonly ILogger<NominatimGeocodingService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public NominatimGeocodingService(HttpClient http, ILogger<NominatimGeocodingService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<ReverseGeocodeResult?> ReverseGeocodeAsync(double latitude, double longitude)
    {
        try
        {
            var url = $"https://nominatim.openstreetmap.org/reverse" +
                      $"?lat={latitude}&lon={longitude}&format=json&addressdetails=1";

            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Nominatim returned {Status} for ({Lat},{Lng})",
                    response.StatusCode, latitude, longitude);
                return null;
            }

            var nominatim = await response.Content
                .ReadFromJsonAsync<NominatimResponse>(JsonOptions);

            if (nominatim?.Address == null) return null;

            var addr = nominatim.Address;

            // Build best street string from available components
            var streetParts = new[] { addr.HouseNumber, addr.Road }
                .Where(p => !string.IsNullOrWhiteSpace(p));
            var street = string.Join(" ", streetParts).Trim();

            // City: prefer city, fall back to town → village → suburb
            var city = addr.City
                    ?? addr.Town
                    ?? addr.Village
                    ?? addr.Suburb
                    ?? addr.County;

            return new ReverseGeocodeResult
            {
                Street      = string.IsNullOrEmpty(street) ? null : street,
                City        = city,
                State       = addr.State,
                PostalCode  = addr.Postcode,
                Country     = addr.Country,
                CountryCode = addr.CountryCode?.ToUpperInvariant(),
                DisplayName = nominatim.DisplayName,
                Latitude    = latitude,
                Longitude   = longitude,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reverse geocoding failed for ({Lat},{Lng})", latitude, longitude);
            return null;
        }
    }

    // ── Nominatim JSON shape ──────────────────────────────────────────────────

    private class NominatimResponse
    {
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("address")]
        public NominatimAddress? Address { get; set; }
    }

    private class NominatimAddress
    {
        [JsonPropertyName("house_number")] public string? HouseNumber { get; set; }
        [JsonPropertyName("road")]         public string? Road        { get; set; }
        [JsonPropertyName("suburb")]       public string? Suburb      { get; set; }
        [JsonPropertyName("city")]         public string? City        { get; set; }
        [JsonPropertyName("town")]         public string? Town        { get; set; }
        [JsonPropertyName("village")]      public string? Village     { get; set; }
        [JsonPropertyName("county")]       public string? County      { get; set; }
        [JsonPropertyName("state")]        public string? State       { get; set; }
        [JsonPropertyName("postcode")]     public string? Postcode    { get; set; }
        [JsonPropertyName("country")]      public string? Country     { get; set; }
        [JsonPropertyName("country_code")] public string? CountryCode { get; set; }
    }
}
