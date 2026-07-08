using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class SkillCategoryService : ISkillCategoryService
{
    private readonly ISkillCategoryRepository _repo;
    private readonly ILogger<SkillCategoryService> _logger;

    public SkillCategoryService(ISkillCategoryRepository repo, ILogger<SkillCategoryService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<SkillCategoryDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving skill categories"); throw; }
    }

    public async Task<SkillCategoryDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving skill category {Id}", id); throw; }
    }

    public async Task<SkillCategoryDto> CreateAsync(CreateSkillCategoryDto request, Guid userId)
    {
        try
        {
            var entity = new SkillCategory { Code = request.Code, Name = request.Name, Description = request.Description, IsActive = true, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            _logger.LogInformation("SkillCategory created: {Name} ({Id})", entity.Name, entity.Id);
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating skill category"); throw; }
    }

    public async Task<SkillCategoryDto> UpdateAsync(Guid id, UpdateSkillCategoryDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Skill category not found");
            if (!string.IsNullOrWhiteSpace(request.Name)) entity.Name = request.Name;
            if (request.Description != null) entity.Description = request.Description;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating skill category {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Skill category not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting skill category {Id}", id); throw; }
    }

    private static SkillCategoryDto Map(SkillCategory e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, Code = e.Code, Name = e.Name,
        Description = e.Description, IsActive = e.IsActive, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}

public class SkillService : ISkillService
{
    private readonly ISkillRepository _repo;
    private readonly ILogger<SkillService> _logger;

    public SkillService(ISkillRepository repo, ILogger<SkillService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<SkillDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving skills"); throw; }
    }

    public async Task<SkillDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving skill {Id}", id); throw; }
    }

    public async Task<IEnumerable<SkillDto>> GetByCategoryAsync(Guid skillCategoryId)
    {
        try { return (await _repo.GetByCategoryAsync(skillCategoryId)).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving skills for category {CategoryId}", skillCategoryId); throw; }
    }

    public async Task<SkillDto> CreateAsync(CreateSkillDto request, Guid userId)
    {
        try
        {
            var entity = new Skill { SkillCategoryId = request.SkillCategoryId, Code = request.Code, Name = request.Name, Description = request.Description, SkillTypeLookupValueId = request.SkillTypeLookupValueId, IsCoreSkill = request.IsCoreSkill, IsActive = true, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            _logger.LogInformation("Skill created: {Name} ({Id})", entity.Name, entity.Id);
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating skill"); throw; }
    }

    public async Task<SkillDto> UpdateAsync(Guid id, UpdateSkillDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Skill not found");
            if (request.SkillCategoryId.HasValue) entity.SkillCategoryId = request.SkillCategoryId.Value;
            if (!string.IsNullOrWhiteSpace(request.Name)) entity.Name = request.Name;
            if (request.Description != null) entity.Description = request.Description;
            if (request.SkillTypeLookupValueId.HasValue) entity.SkillTypeLookupValueId = request.SkillTypeLookupValueId;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            if (request.IsCoreSkill.HasValue) entity.IsCoreSkill = request.IsCoreSkill.Value;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating skill {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Skill not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting skill {Id}", id); throw; }
    }

    private static SkillDto Map(Skill e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, SkillCategoryId = e.SkillCategoryId,
        Code = e.Code, Name = e.Name, Description = e.Description,
        SkillTypeLookupValueId = e.SkillTypeLookupValueId, IsActive = e.IsActive,
        IsCoreSkill = e.IsCoreSkill, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
