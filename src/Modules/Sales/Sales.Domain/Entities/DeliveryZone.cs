using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// Delivery Zone - geographic area used for rider assignment and delivery fee calculation.
/// Zones can overlap; assignment priority is determined by sort order.
/// </summary>
public class DeliveryZone : BaseEntity
{
    public string ZoneCode { get; set; } = string.Empty;
    public string ZoneName { get; set; } = string.Empty;
    /// <summary>GeoJSON polygon string defining the zone boundary.</summary>
    public string? GeoPolygon { get; set; }

    /// <summary>Center point latitude for approximate matching.</summary>
    public double? CenterLatitude { get; set; }
    public double? CenterLongitude { get; set; }

    /// <summary>Radius in km from center (used if no polygon defined).</summary>
    public double? RadiusKm { get; set; }

    public decimal DeliveryFee { get; set; }
    public decimal? FreeDeliveryAboveAmount { get; set; }

    /// <summary>Estimated delivery time in minutes.</summary>
    public int? EstimatedDeliveryMinutes { get; set; }

    public int SortOrder { get; set; }

    // ? Navigation
    public ICollection<Rider> Riders { get; set; } = new List<Rider>();
    public ICollection<PosStore> Stores { get; set; } = new List<PosStore>();
}
