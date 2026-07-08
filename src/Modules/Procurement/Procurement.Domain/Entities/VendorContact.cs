using Nexcore.SharedKernel;

namespace Procurement.Domain.Entities;

/// <summary>
/// Individual contact person at a vendor.
/// </summary>
public class VendorContact : BaseEntity
{
    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    public required string FirstName { get; set; }
    public string? LastName { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }

    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }

    public bool IsPrimary { get; set; }

    public string? Notes { get; set; }
}
