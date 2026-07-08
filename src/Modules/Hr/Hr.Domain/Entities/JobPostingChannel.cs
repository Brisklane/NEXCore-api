
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class JobPostingChannel : BaseEntity
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
    public int ShortlistedCount { get; set; }
    public int HiredCount { get; set; }
    public decimal? CostPerApplication { get; set; }
    public decimal? CostPerHire { get; set; }
    public decimal? QualityScore { get; set; }
    public string? ExternalPostingId { get; set; }
    public string? LegacySourceSystem { get; set; }
    public string? ErrorMessage { get; set; }

    public Job? Job { get; set; }
    public ChannelTemplate? ChannelTemplate { get; set; }
    public LookupValue? ChannelTypeLookupValue { get; set; }
    public LookupValue? StatusLookupValue { get; set; }
}
