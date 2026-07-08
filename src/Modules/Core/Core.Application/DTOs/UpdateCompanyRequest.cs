using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Core.Application.DTOs.ValidateDtos;

namespace Core.Application.DTOs;

/// <summary>
/// Request DTO for updating company details including branches and business units
/// </summary>
public class UpdateCompanyRequest
{
    public string? Code { get; set; } 
    [Required]
    public string CompanyName { get; set; } = string.Empty;
    [Required]
    public string LegalName { get; set; } = string.Empty;
    [Required]
    public string? RegistrationNumber { get; set; }
    [Required]
    public string BaseCurrencyCode { get; set; } = "USD";

    [Required]
    public string PhoneNumber { get; set; } = string.Empty;
    [Required]
    public string MobileNumber { get; set; } = string.Empty;
    [Required]
    public string ContactPerson { get; set; } = string.Empty;
    [Required]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string WebsiteUrl { get; set; } = string.Empty;
    public byte[] CompanyLogo { get; set; } = Array.Empty<byte>();
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int RadiusInMeters { get; set; }

    [Required]
    public string StreetAddress { get; set; } = string.Empty;
    [Required]
    public string City { get; set; } = string.Empty;
    [Required]
    public string State { get; set; } = string.Empty;
    [Required]
    public string PostalCode { get; set; } = string.Empty;

    [Required] 
    public ICollection<UpdateBranchRequestDto> Branches { get; set; } = new List<UpdateBranchRequestDto>();
}
