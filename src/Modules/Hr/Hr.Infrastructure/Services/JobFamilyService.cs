using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class JobFamilyService : IJobFamilyService
{
    private readonly IJobFamilyRepository _repo;
    private readonly ILogger<JobFamilyService> _logger;

    public JobFamilyService(IJobFamilyRepository repo, ILogger<JobFamilyService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<JobFamilyDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving job families"); throw; }
    }

    public async Task<JobFamilyDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving job family {Id}", id); throw; }
    }

    public async Task<JobFamilyDto> CreateAsync(CreateJobFamilyDto request, Guid userId)
    {
        try
        {
            var entity = new JobFamily
            {
                JobFamilyCode = request.JobFamilyCode, JobFamilyName = request.JobFamilyName,
                Description = request.Description, IsActive = true,
                CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
            };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            _logger.LogInformation("Job family created: {Name} ({Id})", entity.JobFamilyName, entity.Id);
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating job family"); throw; }
    }

    public async Task<JobFamilyDto> UpdateAsync(Guid id, UpdateJobFamilyDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Job family not found");
            if (!string.IsNullOrWhiteSpace(request.JobFamilyName)) entity.JobFamilyName = request.JobFamilyName;
            if (request.Description != null) entity.Description = request.Description;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating job family {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Job family not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting job family {Id}", id); throw; }
    }

    private static JobFamilyDto Map(JobFamily e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, JobFamilyCode = e.JobFamilyCode,
        JobFamilyName = e.JobFamilyName, Description = e.Description,
        IsActive = e.IsActive, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
