namespace Hr.Application.DTOs;

public class CandidateSkillDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid SkillId { get; set; }
    public Guid ProficiencyLookupValueId { get; set; }
    public decimal? YearsExperience { get; set; }
    public bool IsVerified { get; set; }
    public Guid? VerifiedByEmployeeId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? Source { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCandidateSkillDto
{
    public Guid CandidateId { get; set; }
    public Guid SkillId { get; set; }
    public Guid ProficiencyLookupValueId { get; set; }
    public decimal? YearsExperience { get; set; }
    public string? Source { get; set; }
    public string? Notes { get; set; }
}

public class UpdateCandidateSkillDto
{
    public Guid? SkillId { get; set; }
    public Guid? ProficiencyLookupValueId { get; set; }
    public decimal? YearsExperience { get; set; }
    public bool? IsVerified { get; set; }
    public Guid? VerifiedByEmployeeId { get; set; }
    public string? Source { get; set; }
    public string? Notes { get; set; }
}
