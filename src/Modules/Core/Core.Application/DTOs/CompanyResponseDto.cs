using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Application.DTOs;

public class CompanyResponseDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
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
}
