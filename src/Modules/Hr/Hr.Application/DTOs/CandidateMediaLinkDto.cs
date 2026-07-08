namespace Hr.Application.DTOs;

public class CandidateMediaLinkDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid MediaTypeLookupValueId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
    public bool IsVerified { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCandidateMediaLinkDto
{
    public Guid CandidateId { get; set; }
    public Guid MediaTypeLookupValueId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
    public string? Notes { get; set; }
}

public class UpdateCandidateMediaLinkDto
{
    public Guid? MediaTypeLookupValueId { get; set; }
    public string? Url { get; set; }
    public string? Title { get; set; }
    public bool? IsPrimary { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsVerified { get; set; }
    public string? Notes { get; set; }
}
