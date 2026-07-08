using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class DesignationService : IDesignationService
{
    private readonly IDesignationRepository _repo;
    private readonly ILogger<DesignationService> _logger;

    public DesignationService(IDesignationRepository repo, ILogger<DesignationService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<DesignationDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving designations"); throw; }
    }

    public async Task<DesignationDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving designation {Id}", id); throw; }
    }

    public async Task<DesignationDto> CreateAsync(CreateDesignationDto request, Guid userId)
    {
        try
        {
            var entity = new Designation
            {
                DesignationCode = request.DesignationCode, DesignationName = request.DesignationName,
                JobFamilyId = request.JobFamilyId, JobFunctionId = request.JobFunctionId,
                GradeId = request.GradeId, IsActive = true,
                CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
            };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            _logger.LogInformation("Designation created: {Name} ({Id})", entity.DesignationName, entity.Id);
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating designation"); throw; }
    }

    public async Task<DesignationDto> UpdateAsync(Guid id, UpdateDesignationDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Designation not found");
            if (!string.IsNullOrWhiteSpace(request.DesignationName)) entity.DesignationName = request.DesignationName;
            if (request.JobFamilyId.HasValue) entity.JobFamilyId = request.JobFamilyId;
            if (request.JobFunctionId.HasValue) entity.JobFunctionId = request.JobFunctionId;
            if (request.GradeId.HasValue) entity.GradeId = request.GradeId;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating designation {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Designation not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting designation {Id}", id); throw; }
    }

    public async Task<IEnumerable<HrLookupItemDto>> GetLookupListAsync()
    {
        try
        {
            return (await _repo.GetAllByTenantAsync())
                .Where(e => !e.IsDeleted && e.IsActive)
                .OrderBy(e => e.DesignationName)
                .Select(e => new HrLookupItemDto
                {
                    Value = e.Id.ToString(),
                    Label = e.DesignationName
                });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving designation lookup list"); throw; }
    }

    private static DesignationDto Map(Designation e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, DesignationCode = e.DesignationCode,
        DesignationName = e.DesignationName, JobFamilyId = e.JobFamilyId,
        JobFunctionId = e.JobFunctionId, GradeId = e.GradeId,
        IsActive = e.IsActive, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
