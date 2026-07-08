using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class JobDetailService : IJobDetailService
{
    private readonly IJobDetailRepository _repository;
    private readonly ILogger<JobDetailService> _logger;

    public JobDetailService(IJobDetailRepository repository, ILogger<JobDetailService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<JobDetailDto>> GetAllAsync()
    {
        var items = await _repository.GetAllByTenantAsync();
        return items.Where(i => !i.IsDeleted).Select(MapToDto);
    }

    public async Task<JobDetailDto?> GetByIdAsync(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? null : MapToDto(item);
    }

    public async Task<JobDetailDto> CreateAsync(CreateJobDetailDto request, Guid userId)
    {
        var entity = new JobDetail
        {
            JobId = request.JobId,
            JobTemplateId = request.JobTemplateId,
            JobLocationId = request.JobLocationId,
            PositionId = request.PositionId,
            Description = request.Description,
            Requirements = request.Requirements,
            MinExperienceYears = request.MinExperienceYears,
            MaxExperienceYears = request.MaxExperienceYears,
            PublishExternallyFlag = request.PublishExternallyFlag,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task<JobDetailDto> UpdateAsync(Guid id, UpdateJobDetailDto request, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted) throw new InvalidOperationException("JobDetail not found");

        if (request.JobTemplateId.HasValue) entity.JobTemplateId = request.JobTemplateId.Value;
        if (request.JobLocationId.HasValue) entity.JobLocationId = request.JobLocationId.Value;
        if (request.PositionId.HasValue) entity.PositionId = request.PositionId.Value;
        if (request.Description != null) entity.Description = request.Description;
        if (request.Requirements != null) entity.Requirements = request.Requirements;
        if (request.MinExperienceYears.HasValue) entity.MinExperienceYears = request.MinExperienceYears;
        if (request.MaxExperienceYears.HasValue) entity.MaxExperienceYears = request.MaxExperienceYears;
        if (request.PublishExternallyFlag.HasValue) entity.PublishExternallyFlag = request.PublishExternallyFlag.Value;

        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUserId = userId;

        _repository.Update(entity);
        await _repository.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted) throw new InvalidOperationException("JobDetail not found");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedByUserId = userId;

        _repository.Update(entity);
        await _repository.SaveChangesAsync();
    }

    private static JobDetailDto MapToDto(JobDetail j) => new()
    {
        Id = j.Id,
        CompanyId = j.CompanyId,
        JobId = j.JobId,
        JobTemplateId = j.JobTemplateId,
        JobLocationId = j.JobLocationId,
        PositionId = j.PositionId,
        Description = j.Description,
        Requirements = j.Requirements,
        MinExperienceYears = j.MinExperienceYears,
        MaxExperienceYears = j.MaxExperienceYears,
        PublishExternallyFlag = j.PublishExternallyFlag,
        CreatedAt = j.CreatedAt,
        UpdatedAt = j.UpdatedAt
    };
}
