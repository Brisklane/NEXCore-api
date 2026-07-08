namespace Hr.Application.DTOs;

public class CandidateContactDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid ContactTypeLookupValueId { get; set; }
    public string ContactValue { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCandidateContactDto
{
    public Guid CandidateId { get; set; }
    public Guid ContactTypeLookupValueId { get; set; }
    public string ContactValue { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public string? Notes { get; set; }
}

public class UpdateCandidateContactDto
{
    public Guid? ContactTypeLookupValueId { get; set; }
    public string? ContactValue { get; set; }
    public bool? IsPrimary { get; set; }
    public bool? IsVerified { get; set; }
    public string? Notes { get; set; }
}
