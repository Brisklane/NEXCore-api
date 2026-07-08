using Nexcore.SharedKernel;

namespace Crm.Domain.Entities;

/// <summary>
/// A saved address for a Contact - used for billing, shipping, and delivery.
/// Replaces Sales.CustomerAddress. All delivery addresses live here.
/// </summary>
public class ContactAddress : BaseEntity
{
    public Guid ContactId { get; set; }
    public Contact Contact { get; set; } = null!;

    /// <summary>Billing | Shipping | Both</summary>
    public string AddressType { get; set; } = "Shipping";
    public bool IsDefault { get; set; }

    public string Street { get; set; } = string.Empty;
    public string? Street2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? State { get; set; }
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;

    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string? DeliveryInstructions { get; set; }

    /// <summary>GPS coordinates — used for app delivery routing.</summary>
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>Human-readable label: "Home", "Office", "Warehouse".</summary>
    public string? Label { get; set; }
}
