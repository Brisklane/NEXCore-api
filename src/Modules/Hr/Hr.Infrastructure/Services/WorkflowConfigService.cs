using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class WorkflowConfigService : IWorkflowConfigService
{
    private readonly IWorkflowConfigRepository _repo;
    private readonly ILogger<WorkflowConfigService> _logger;

    public WorkflowConfigService(IWorkflowConfigRepository repo, ILogger<WorkflowConfigService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<WorkflowConfigDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving workflow configs"); throw; }
    }

    public async Task<WorkflowConfigDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving workflow config {Id}", id); throw; }
    }

    public async Task<WorkflowConfigDto> CreateAsync(CreateWorkflowConfigDto request, Guid userId)
    {
        try
        {
            var entity = new WorkflowConfig { WorkflowCode = request.WorkflowCode, Module = request.Module, TransactionType = request.TransactionType, WorkflowName = request.WorkflowName, TotalLevels = request.TotalLevels, EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo, Description = request.Description, IsActive = true, VersionNo = 1, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            _logger.LogInformation("WorkflowConfig created: {Name} ({Id})", entity.WorkflowName, entity.Id);
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating workflow config"); throw; }
    }

    public async Task<WorkflowConfigDto> UpdateAsync(Guid id, UpdateWorkflowConfigDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Workflow config not found");
            if (!string.IsNullOrWhiteSpace(request.WorkflowName)) entity.WorkflowName = request.WorkflowName;
            if (request.Module != null) entity.Module = request.Module;
            if (request.TransactionType != null) entity.TransactionType = request.TransactionType;
            if (request.TotalLevels.HasValue) entity.TotalLevels = request.TotalLevels.Value;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            if (request.EffectiveFrom.HasValue) entity.EffectiveFrom = request.EffectiveFrom;
            if (request.EffectiveTo.HasValue) entity.EffectiveTo = request.EffectiveTo;
            if (request.Description != null) entity.Description = request.Description;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating workflow config {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Workflow config not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting workflow config {Id}", id); throw; }
    }

    private static WorkflowConfigDto Map(WorkflowConfig e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, WorkflowCode = e.WorkflowCode, Module = e.Module,
        TransactionType = e.TransactionType, WorkflowName = e.WorkflowName, IsActive = e.IsActive,
        TotalLevels = e.TotalLevels, EffectiveFrom = e.EffectiveFrom, EffectiveTo = e.EffectiveTo,
        Description = e.Description, VersionNo = e.VersionNo, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
