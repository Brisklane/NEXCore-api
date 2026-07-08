using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class ChannelTemplateService : IChannelTemplateService
{
    private readonly IChannelTemplateRepository _repository;
    private readonly ILogger<ChannelTemplateService> _logger;

    public ChannelTemplateService(IChannelTemplateRepository repository, ILogger<ChannelTemplateService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<ChannelTemplateDto>> GetAllAsync()
    {
        var items = await _repository.GetAllByTenantAsync();
        return items.Where(i => !i.IsDeleted).Select(MapToDto);
    }

    public async Task<ChannelTemplateDto?> GetByIdAsync(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? null : MapToDto(item);
    }

    public async Task<ChannelTemplateDto> CreateAsync(CreateChannelTemplateDto request, Guid userId)
    {
        var entity = new ChannelTemplate
        {
            ChannelCode = request.ChannelCode,
            ChannelName = request.ChannelName,
            ChannelTypeLookupValueId = request.ChannelTypeLookupValueId,
            SupportsAutoPosting = request.SupportsAutoPosting,
            RequiresApproval = request.RequiresApproval,
            ApiEndpoint = request.ApiEndpoint,
            AuthConfigJson = request.AuthConfigJson,
            TrackingPrefix = request.TrackingPrefix,
            DefaultStatusLookupValueId = request.DefaultStatusLookupValueId,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task<ChannelTemplateDto> UpdateAsync(Guid id, UpdateChannelTemplateDto request, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted) throw new InvalidOperationException("ChannelTemplate not found");

        if (request.ChannelName != null) entity.ChannelName = request.ChannelName;
        if (request.ChannelTypeLookupValueId.HasValue) entity.ChannelTypeLookupValueId = request.ChannelTypeLookupValueId.Value;
        if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
        if (request.SupportsAutoPosting.HasValue) entity.SupportsAutoPosting = request.SupportsAutoPosting.Value;
        if (request.RequiresApproval.HasValue) entity.RequiresApproval = request.RequiresApproval.Value;
        if (request.ApiEndpoint != null) entity.ApiEndpoint = request.ApiEndpoint;
        if (request.AuthConfigJson != null) entity.AuthConfigJson = request.AuthConfigJson;
        if (request.TrackingPrefix != null) entity.TrackingPrefix = request.TrackingPrefix;
        if (request.DefaultStatusLookupValueId.HasValue) entity.DefaultStatusLookupValueId = request.DefaultStatusLookupValueId.Value;

        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUserId = userId;

        _repository.Update(entity);
        await _repository.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted) throw new InvalidOperationException("ChannelTemplate not found");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedByUserId = userId;

        _repository.Update(entity);
        await _repository.SaveChangesAsync();
    }

    private static ChannelTemplateDto MapToDto(ChannelTemplate c) => new()
    {
        Id = c.Id,
        CompanyId = c.CompanyId,
        ChannelCode = c.ChannelCode,
        ChannelName = c.ChannelName,
        ChannelTypeLookupValueId = c.ChannelTypeLookupValueId,
        IsActive = c.IsActive,
        SupportsAutoPosting = c.SupportsAutoPosting,
        RequiresApproval = c.RequiresApproval,
        ApiEndpoint = c.ApiEndpoint,
        AuthConfigJson = c.AuthConfigJson,
        TrackingPrefix = c.TrackingPrefix,
        DefaultStatusLookupValueId = c.DefaultStatusLookupValueId,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };
}
