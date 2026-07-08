
using Nexcore.SharedKernel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hr.Domain.Entities;

public class ChannelTemplate : BaseEntity
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

    public LookupValue? ChannelTypeLookupValue { get; set; }
    public LookupValue? DefaultStatusLookupValue { get; set; }
    public ICollection<JobPostingChannel>? JobPostingChannels { get; set; }
}
