using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// Physical package / carton in a delivery.
/// </summary>
public class DeliveryPackage : BaseEntity
{
    public Guid DeliveryId { get; set; }
    public Delivery Delivery { get; set; } = null!;

    public string PackageNumber { get; set; } = string.Empty;
    public string? PackageType { get; set; }   // Box, Pallet, Envelope …
    public decimal? Weight { get; set; }
    public string? WeightUnit { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public string? DimensionUnit { get; set; }
    public string? TrackingNumber { get; set; }
    public string? SealNumber { get; set; }
}
