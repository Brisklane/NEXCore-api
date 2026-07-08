
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class CandidateContact : BaseEntity
{
    public Guid CandidateId { get; set; }
    public Guid ContactTypeLookupValueId { get; set; }
    public string ContactValue { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? Notes { get; set; }

    public Candidate? Candidate { get; set; }
    public LookupValue? ContactTypeLookupValue { get; set; }
}
