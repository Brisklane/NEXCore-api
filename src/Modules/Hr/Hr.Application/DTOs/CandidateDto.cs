using Hr.Application.Enums;

namespace Hr.Application.DTOs;

public class CandidateDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CandidateCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? ResumeUrl { get; set; }
    public string? CurrentCompany { get; set; }
    public CandidateSource? Source { get; set; }
    public decimal? TotalExperienceYears { get; set; }
    public decimal? CurrentSalary { get; set; }
    public decimal? ExpectedSalary { get; set; }
    public CandidateRating? CandidateRating { get; set; }
    public bool IsBlacklisted { get; set; }
    public Guid? BlacklistReasonLookupValueId { get; set; }
    public DateTime? BlacklistedAt { get; set; }
    public Guid? BlacklistedByEmployeeId { get; set; }
    public bool ConsentGiven { get; set; }
    public DateTime? DataRetentionExpiryDate { get; set; }
    public Guid? DuplicateOfCandidateId { get; set; }
    public Guid? MergedIntoCandidateId { get; set; }
    public string? LegacyCandidateId { get; set; }
    public string? LegacySourceSystem { get; set; }
    public Guid? CurrentDesignationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCandidateDto
{
    public string CandidateCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? ResumeUrl { get; set; }
    public CandidateSource? Source { get; set; }
    public decimal? TotalExperienceYears { get; set; }
    public decimal? CurrentSalary { get; set; }
    public decimal? ExpectedSalary { get; set; }
    public string? CurrentCompany { get; set; }
    public Guid? CurrentDesignationId { get; set; }
    public bool ConsentGiven { get; set; }
}

public class UpdateCandidateDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? ResumeUrl { get; set; }
    public CandidateSource? Source { get; set; }
    public decimal? TotalExperienceYears { get; set; }
    public decimal? CurrentSalary { get; set; }
    public decimal? ExpectedSalary { get; set; }
    public CandidateRating? CandidateRating { get; set; }
    public string? CurrentCompany { get; set; }
    public Guid? CurrentDesignationId { get; set; }
    public bool? ConsentGiven { get; set; }
    public bool? IsBlacklisted { get; set; }
    public Guid? BlacklistReasonLookupValueId { get; set; }
}
