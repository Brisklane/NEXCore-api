
using Nexcore.SharedKernel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hr.Domain.Entities;

public class CandidateMediaLink : BaseEntity
{
    public Guid CandidateId { get; set; }
    public Guid MediaTypeLookupValueId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
    public bool IsVerified { get; set; }
    public string? Notes { get; set; }

    public Candidate? Candidate { get; set; }
    public LookupValue? MediaTypeLookupValue { get; set; }
}
