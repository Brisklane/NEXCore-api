using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class JobFunctionService : IJobFunctionService
{
    private readonly IJobFunctionRepository _repo;
    private readonly ILogger<JobFunctionService> _logger;

    public JobFunctionService(IJobFunctionRepository repo, ILogger<JobFunctionService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<JobFunctionDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving job functions"); throw; }
    }

    public async Task<JobFunctionDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving job function {Id}", id); throw; }
    }

    public async Task<JobFunctionDto> CreateAsync(CreateJobFunctionDto request, Guid userId)
    {
        try
        {
            var entity = new JobFunction
            {
                JobFunctionCode = request.JobFunctionCode, JobFunctionName = request.JobFunctionName,
                Description = request.Description, IsActive = true,
                CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
            };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            _logger.LogInformation("Job function created: {Name} ({Id})", entity.JobFunctionName, entity.Id);
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating job function"); throw; }
    }

    public async Task<JobFunctionDto> UpdateAsync(Guid id, UpdateJobFunctionDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Job function not found");
            if (!string.IsNullOrWhiteSpace(request.JobFunctionName)) entity.JobFunctionName = request.JobFunctionName;
            if (request.Description != null) entity.Description = request.Description;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating job function {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Job function not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting job function {Id}", id); throw; }
    }

    private static JobFunctionDto Map(JobFunction e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, JobFunctionCode = e.JobFunctionCode,
        JobFunctionName = e.JobFunctionName, Description = e.Description,
        IsActive = e.IsActive, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
