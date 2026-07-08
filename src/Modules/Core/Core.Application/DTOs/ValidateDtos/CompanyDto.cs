using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Application.DTOs.ValidateDtos;

public class CompanyDto
{
    /// <summary>Company id — populated on GET responses; null when creating.</summary>
    public Guid? CompanyId { get; set; }
    public string? Code { get; set; }
    [Required]
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// URL-safe identifier unique to this company (e.g. "acme-corp").
    /// Used as the per-company tenancy URL: acme-corp.nexcore.app
    /// Auto-generated from CompanyName if not provided.
    /// Only lowercase letters, numbers, and hyphens. 3–50 characters.
    /// </summary>
    [MaxLength(50)]
    public string? CompanySlug { get; set; }

    public string LegalName { get; set; } = string.Empty;
    public string? RegistrationNumber { get; set; }
    public string BaseCurrencyCode { get; set; } = "USD";

    public string PhoneNumber { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;
    public byte[] CompanyLogo { get; set; } = Array.Empty<byte>();
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int RadiusInMeters { get; set; }

    public string StreetAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;

    public ICollection<CreateBranchDto> Branches { get; set; } = new List<CreateBranchDto>();
}
