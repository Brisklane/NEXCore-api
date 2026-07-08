namespace Hr.Application.DTOs;

public class ApplicationDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string ApplicationCode { get; set; } = string.Empty;
    public Guid JobId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid JobPostingChannelId { get; set; }
    public DateTime AppliedDate { get; set; }
    public Guid CurrentStageLookupValueId { get; set; }
    public Guid StatusLookupValueId { get; set; }
    public Guid? PriorityLookupValueId { get; set; }
    public bool IsShortlisted { get; set; }
    public decimal? ScreeningScore { get; set; }
    public decimal? InternalScore { get; set; }
    public Guid? AssignedRecruiterEmployeeId { get; set; }
    public Guid? ConvertedToEmployeeId { get; set; }
    public Guid? OfferId { get; set; }
    public DateTime? HiredDate { get; set; }
    public DateTime? StageChangedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateApplicationDto
{
    public string ApplicationCode { get; set; } = string.Empty;
    public Guid JobId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid JobPostingChannelId { get; set; }
    public Guid CurrentStageLookupValueId { get; set; }
    public Guid StatusLookupValueId { get; set; }
    public Guid? PriorityLookupValueId { get; set; }
    public Guid? AssignedRecruiterEmployeeId { get; set; }
}

public class UpdateApplicationDto
{
    public Guid? CurrentStageLookupValueId { get; set; }
    public Guid? StatusLookupValueId { get; set; }
    public Guid? PriorityLookupValueId { get; set; }
    public bool? IsShortlisted { get; set; }
    public decimal? ScreeningScore { get; set; }
    public decimal? InternalScore { get; set; }
    public Guid? AssignedRecruiterEmployeeId { get; set; }
    public DateTime? StageChangedAt { get; set; }
}
