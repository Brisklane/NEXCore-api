namespace Hr.Application.DTOs;

public class CandidateTaskDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string TaskCode { get; set; } = string.Empty;
    public Guid ApplicationId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid JobId { get; set; }
    public Guid TaskTypeLookupValueId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string? ExternalProvider { get; set; }
    public string? ProviderReferenceId { get; set; }
    public DateTime AssignedDate { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid StatusLookupValueId { get; set; }
    public Guid AssignedByEmployeeId { get; set; }
    public string? Description { get; set; }
    public decimal? MaxScore { get; set; }
    public decimal? PassingScore { get; set; }
    public int AttemptAllowed { get; set; }
    public bool IsMandatory { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCandidateTaskDto
{
    public string TaskCode { get; set; } = string.Empty;
    public Guid ApplicationId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid JobId { get; set; }
    public Guid TaskTypeLookupValueId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string? ExternalProvider { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid StatusLookupValueId { get; set; }
    public Guid AssignedByEmployeeId { get; set; }
    public string? Description { get; set; }
    public decimal? MaxScore { get; set; }
    public decimal? PassingScore { get; set; }
    public int AttemptAllowed { get; set; } = 1;
    public bool IsMandatory { get; set; } = true;
}

public class UpdateCandidateTaskDto
{
    public DateTime? DueDate { get; set; }
    public Guid? StatusLookupValueId { get; set; }
    public string? Description { get; set; }
    public decimal? MaxScore { get; set; }
    public decimal? PassingScore { get; set; }
    public int? AttemptAllowed { get; set; }
    public bool? IsMandatory { get; set; }
    public string? CancelReason { get; set; }
}
