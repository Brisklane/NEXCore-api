namespace Hr.Application.DTOs;

public class JobPostingChannelDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid JobId { get; set; }
    public Guid ChannelTemplateId { get; set; }
    public string ChannelNameSnapshot { get; set; } = string.Empty;
    public Guid ChannelTypeLookupValueId { get; set; }
    public string SourceTrackingCode { get; set; } = string.Empty;
    public string? PostingUrl { get; set; }
    public DateTime OpenDate { get; set; }
    public DateTime? CloseDate { get; set; }
    public Guid StatusLookupValueId { get; set; }
    public int ApplicationsReceived { get; set; }
    public bool IsSponsored { get; set; }
    public decimal? SponsoredBudget { get; set; }
    public DateTime? SponsoredStartDate { get; set; }
    public DateTime? SponsoredEndDate { get; set; }
    public Guid? AgencyId { get; set; }
    public decimal? AgencyFeePercent { get; set; }
    public int ViewCount { get; set; }
    public int ClickCount { get; set; }
    public decimal? ConversionRate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateJobPostingChannelDto
{
    public Guid JobId { get; set; }
    public Guid ChannelTemplateId { get; set; }
    public string ChannelNameSnapshot { get; set; } = string.Empty;
    public Guid ChannelTypeLookupValueId { get; set; }
    public string SourceTrackingCode { get; set; } = string.Empty;
    public string? PostingUrl { get; set; }
    public DateTime OpenDate { get; set; }
    public DateTime? CloseDate { get; set; }
    public Guid StatusLookupValueId { get; set; }
}

public class UpdateJobPostingChannelDto
{
    public Guid? ChannelTemplateId { get; set; }
    public string? ChannelNameSnapshot { get; set; }
    public string? PostingUrl { get; set; }
    public DateTime? CloseDate { get; set; }
    public Guid? StatusLookupValueId { get; set; }
    public bool? IsSponsored { get; set; }
    public decimal? SponsoredBudget { get; set; }
}
