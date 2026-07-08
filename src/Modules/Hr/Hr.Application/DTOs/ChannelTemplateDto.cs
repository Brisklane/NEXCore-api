namespace Hr.Application.DTOs;

public class ChannelTemplateDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string ChannelCode { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public Guid ChannelTypeLookupValueId { get; set; }
    public bool IsActive { get; set; }
    public bool SupportsAutoPosting { get; set; }
    public bool RequiresApproval { get; set; }
    public string? ApiEndpoint { get; set; }
    public string? AuthConfigJson { get; set; }
    public string? TrackingPrefix { get; set; }
    public Guid? DefaultStatusLookupValueId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateChannelTemplateDto
{
    public string ChannelCode { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public Guid ChannelTypeLookupValueId { get; set; }
    public bool SupportsAutoPosting { get; set; }
    public bool RequiresApproval { get; set; }
    public string? ApiEndpoint { get; set; }
    public string? AuthConfigJson { get; set; }
    public string? TrackingPrefix { get; set; }
    public Guid? DefaultStatusLookupValueId { get; set; }
}

public class UpdateChannelTemplateDto
{
    public string? ChannelName { get; set; }
    public Guid? ChannelTypeLookupValueId { get; set; }
    public bool? IsActive { get; set; }
    public bool? SupportsAutoPosting { get; set; }
    public bool? RequiresApproval { get; set; }
    public string? ApiEndpoint { get; set; }
    public string? AuthConfigJson { get; set; }
    public string? TrackingPrefix { get; set; }
    public Guid? DefaultStatusLookupValueId { get; set; }
}
