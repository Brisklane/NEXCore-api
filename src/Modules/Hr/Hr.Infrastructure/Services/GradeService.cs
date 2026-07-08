using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class GradeService : IGradeService
{
    private readonly IGradeRepository _repo;
    private readonly ILogger<GradeService> _logger;

    public GradeService(IGradeRepository repo, ILogger<GradeService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<GradeDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving grades"); throw; }
    }

    public async Task<GradeDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving grade {Id}", id); throw; }
    }

    public async Task<GradeDto> CreateAsync(CreateGradeDto request, Guid userId)
    {
        try
        {
            var entity = new Grade
            {
                GradeCode = request.GradeCode, GradeName = request.GradeName, LevelNo = request.LevelNo,
                IsActive = true, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
            };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            _logger.LogInformation("Grade created: {Name} ({Id})", entity.GradeName, entity.Id);
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating grade"); throw; }
    }

    public async Task<GradeDto> UpdateAsync(Guid id, UpdateGradeDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Grade not found");
            if (!string.IsNullOrWhiteSpace(request.GradeName)) entity.GradeName = request.GradeName;
            if (request.LevelNo.HasValue) entity.LevelNo = request.LevelNo.Value;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating grade {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Grade not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting grade {Id}", id); throw; }
    }

    private static GradeDto Map(Grade e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, GradeCode = e.GradeCode,
        GradeName = e.GradeName, LevelNo = e.LevelNo,
        IsActive = e.IsActive, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
