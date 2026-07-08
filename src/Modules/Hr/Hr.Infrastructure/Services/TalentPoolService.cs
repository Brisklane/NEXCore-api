using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class TalentPoolService : ITalentPoolService
{
    private readonly ITalentPoolRepository _repo;
    private readonly ILogger<TalentPoolService> _logger;

    public TalentPoolService(ITalentPoolRepository repo, ILogger<TalentPoolService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<TalentPoolDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving talent pools"); throw; }
    }

    public async Task<TalentPoolDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving talent pool {Id}", id); throw; }
    }

    public async Task<TalentPoolDto> CreateAsync(CreateTalentPoolDto request, Guid userId)
    {
        try
        {
            var entity = new TalentPool
            {
                TalentPoolCode = request.TalentPoolCode, TalentPoolName = request.TalentPoolName,
                CriteriaJson = request.CriteriaJson, IsActive = true,
                CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
            };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            _logger.LogInformation("Talent pool created: {Name} ({Id})", entity.TalentPoolName, entity.Id);
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating talent pool"); throw; }
    }

    public async Task<TalentPoolDto> UpdateAsync(Guid id, UpdateTalentPoolDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Talent pool not found");
            if (!string.IsNullOrWhiteSpace(request.TalentPoolName)) entity.TalentPoolName = request.TalentPoolName;
            if (request.CriteriaJson != null) entity.CriteriaJson = request.CriteriaJson;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating talent pool {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Talent pool not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting talent pool {Id}", id); throw; }
    }

    private static TalentPoolDto Map(TalentPool e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, TalentPoolCode = e.TalentPoolCode,
        TalentPoolName = e.TalentPoolName, CriteriaJson = e.CriteriaJson,
        IsActive = e.IsActive, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
