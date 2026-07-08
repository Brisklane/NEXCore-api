
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class CandidateAddress : BaseEntity
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
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? Notes { get; set; }

    public Candidate? Candidate { get; set; }
    public LookupValue? AddressTypeLookupValue { get; set; }
}
