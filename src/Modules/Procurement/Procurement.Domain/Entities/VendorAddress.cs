using Nexcore.SharedKernel;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Physical/mailing address for a vendor.
/// </summary>
public class VendorAddress : BaseEntity
{
    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    public VendorAddressType AddressType { get; set; } = VendorAddressType.Both;

    public required string Street { get; set; }
    public string? Street2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public required string Country { get; set; }
    public string? CountryCode { get; set; }

    /// <summary>GPS coordinates for delivery routing.</summary>
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public bool IsPrimary { get; set; }
    public string? Notes { get; set; }
}
