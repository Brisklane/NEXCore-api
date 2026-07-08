using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class CompetencyFrameworkService : ICompetencyFrameworkService
{
    private readonly ICompetencyFrameworkRepository _repo;
    private readonly ILogger<CompetencyFrameworkService> _logger;

    public CompetencyFrameworkService(ICompetencyFrameworkRepository repo, ILogger<CompetencyFrameworkService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<CompetencyFrameworkDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving competency frameworks"); throw; }
    }

    public async Task<CompetencyFrameworkDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving competency framework {Id}", id); throw; }
    }

    public async Task<CompetencyFrameworkDto> CreateAsync(CreateCompetencyFrameworkDto request, Guid userId)
    {
        try
        {
            var entity = new CompetencyFramework { Code = request.Code, Name = request.Name, Description = request.Description, JobFamilyId = request.JobFamilyId, VersionNumber = 1, IsActive = true, EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            _logger.LogInformation("CompetencyFramework created: {Name} ({Id})", entity.Name, entity.Id);
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating competency framework"); throw; }
    }

    public async Task<CompetencyFrameworkDto> UpdateAsync(Guid id, UpdateCompetencyFrameworkDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Competency framework not found");
            if (!string.IsNullOrWhiteSpace(request.Name)) entity.Name = request.Name;
            if (request.Description != null) entity.Description = request.Description;
            if (request.JobFamilyId.HasValue) entity.JobFamilyId = request.JobFamilyId;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            if (request.EffectiveFrom.HasValue) entity.EffectiveFrom = request.EffectiveFrom;
            if (request.EffectiveTo.HasValue) entity.EffectiveTo = request.EffectiveTo;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating competency framework {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Competency framework not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting competency framework {Id}", id); throw; }
    }

    private static CompetencyFrameworkDto Map(CompetencyFramework e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, Code = e.Code, Name = e.Name,
        Description = e.Description, JobFamilyId = e.JobFamilyId, VersionNumber = e.VersionNumber,
        IsActive = e.IsActive, EffectiveFrom = e.EffectiveFrom, EffectiveTo = e.EffectiveTo,
        CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}

public class CompetencyFrameworkItemService : ICompetencyFrameworkItemService
{
    private readonly ICompetencyFrameworkItemRepository _repo;
    private readonly ILogger<CompetencyFrameworkItemService> _logger;

    public CompetencyFrameworkItemService(ICompetencyFrameworkItemRepository repo, ILogger<CompetencyFrameworkItemService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<CompetencyFrameworkItemDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving competency framework items"); throw; }
    }

    public async Task<CompetencyFrameworkItemDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving competency framework item {Id}", id); throw; }
    }

    public async Task<IEnumerable<CompetencyFrameworkItemDto>> GetByFrameworkIdAsync(Guid frameworkId)
    {
        try { return (await _repo.GetByFrameworkIdAsync(frameworkId)).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving items for framework {FrameworkId}", frameworkId); throw; }
    }

    public async Task<CompetencyFrameworkItemDto> CreateAsync(CreateCompetencyFrameworkItemDto request, Guid userId)
    {
        try
        {
            var entity = new CompetencyFrameworkItem { CompetencyFrameworkId = request.CompetencyFrameworkId, SkillId = request.SkillId, WeightPercent = request.WeightPercent, MinimumRating = request.MinimumRating, IsMandatory = request.IsMandatory, SortOrder = request.SortOrder, Notes = request.Notes, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating competency framework item"); throw; }
    }

    public async Task<CompetencyFrameworkItemDto> UpdateAsync(Guid id, UpdateCompetencyFrameworkItemDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Competency framework item not found");
            if (request.SkillId.HasValue) entity.SkillId = request.SkillId.Value;
            if (request.WeightPercent.HasValue) entity.WeightPercent = request.WeightPercent.Value;
            if (request.MinimumRating.HasValue) entity.MinimumRating = request.MinimumRating.Value;
            if (request.IsMandatory.HasValue) entity.IsMandatory = request.IsMandatory.Value;
            if (request.SortOrder.HasValue) entity.SortOrder = request.SortOrder.Value;
            if (request.Notes != null) entity.Notes = request.Notes;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating competency framework item {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Competency framework item not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting competency framework item {Id}", id); throw; }
    }

    private static CompetencyFrameworkItemDto Map(CompetencyFrameworkItem e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, CompetencyFrameworkId = e.CompetencyFrameworkId,
        SkillId = e.SkillId, WeightPercent = e.WeightPercent, MinimumRating = e.MinimumRating,
        IsMandatory = e.IsMandatory, SortOrder = e.SortOrder, Notes = e.Notes,
        CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
