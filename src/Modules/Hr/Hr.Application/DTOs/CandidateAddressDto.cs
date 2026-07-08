namespace Hr.Application.DTOs;

public class CandidateAddressDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid AddressTypeLookupValueId { get; set; }
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsVerified { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCandidateAddressDto
{
    public Guid CandidateId { get; set; }
    public Guid AddressTypeLookupValueId { get; set; }
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsPrimary { get; set; }
    public string? Notes { get; set; }
}

public class UpdateCandidateAddressDto
{
    public Guid? AddressTypeLookupValueId { get; set; }
    public string? Line1 { get; set; }
    public string? Line2 { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool? IsPrimary { get; set; }
    public bool? IsVerified { get; set; }
    public string? Notes { get; set; }
}
