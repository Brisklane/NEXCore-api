using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class JobPostingChannelService : IJobPostingChannelService
{
    private readonly IJobPostingChannelRepository _repository;
    private readonly ILogger<JobPostingChannelService> _logger;

    public JobPostingChannelService(IJobPostingChannelRepository repository, ILogger<JobPostingChannelService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<JobPostingChannelDto>> GetAllAsync()
    {
        var items = await _repository.GetAllByTenantAsync();
        return items.Where(i => !i.IsDeleted).Select(MapToDto);
    }

    public async Task<JobPostingChannelDto?> GetByIdAsync(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? null : MapToDto(item);
    }

    public async Task<JobPostingChannelDto> CreateAsync(CreateJobPostingChannelDto request, Guid userId)
    {
        var entity = new JobPostingChannel
        {
            JobId = request.JobId,
            ChannelTemplateId = request.ChannelTemplateId,
            ChannelNameSnapshot = request.ChannelNameSnapshot,
            ChannelTypeLookupValueId = request.ChannelTypeLookupValueId,
            SourceTrackingCode = request.SourceTrackingCode,
            PostingUrl = request.PostingUrl,
            OpenDate = request.OpenDate,
            CloseDate = request.CloseDate,
            StatusLookupValueId = request.StatusLookupValueId,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task<JobPostingChannelDto> UpdateAsync(Guid id, UpdateJobPostingChannelDto request, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted) throw new InvalidOperationException("JobPostingChannel not found");

        if (request.ChannelTemplateId.HasValue) entity.ChannelTemplateId = request.ChannelTemplateId.Value;
        if (request.ChannelNameSnapshot != null) entity.ChannelNameSnapshot = request.ChannelNameSnapshot;
        if (request.PostingUrl != null) entity.PostingUrl = request.PostingUrl;
        if (request.CloseDate.HasValue) entity.CloseDate = request.CloseDate.Value;
        if (request.StatusLookupValueId.HasValue) entity.StatusLookupValueId = request.StatusLookupValueId.Value;
        if (request.IsSponsored.HasValue) entity.IsSponsored = request.IsSponsored.Value;
        if (request.SponsoredBudget.HasValue) entity.SponsoredBudget = request.SponsoredBudget.Value;

        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUserId = userId;

        _repository.Update(entity);
        await _repository.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted) throw new InvalidOperationException("JobPostingChannel not found");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedByUserId = userId;

        _repository.Update(entity);
        await _repository.SaveChangesAsync();
    }

    private static JobPostingChannelDto MapToDto(JobPostingChannel j) => new()
    {
        Id = j.Id,
        CompanyId = j.CompanyId,
        JobId = j.JobId,
        ChannelTemplateId = j.ChannelTemplateId,
        ChannelNameSnapshot = j.ChannelNameSnapshot,
        ChannelTypeLookupValueId = j.ChannelTypeLookupValueId,
        SourceTrackingCode = j.SourceTrackingCode,
        PostingUrl = j.PostingUrl,
        OpenDate = j.OpenDate,
        CloseDate = j.CloseDate,
        StatusLookupValueId = j.StatusLookupValueId,
        ApplicationsReceived = j.ApplicationsReceived,
        IsSponsored = j.IsSponsored,
        SponsoredBudget = j.SponsoredBudget,
        SponsoredStartDate = j.SponsoredStartDate,
        SponsoredEndDate = j.SponsoredEndDate,
        AgencyId = j.AgencyId,
        AgencyFeePercent = j.AgencyFeePercent,
        ViewCount = j.ViewCount,
        ClickCount = j.ClickCount,
        ConversionRate = j.ConversionRate,
        CreatedAt = j.CreatedAt,
        UpdatedAt = j.UpdatedAt
    };
}
