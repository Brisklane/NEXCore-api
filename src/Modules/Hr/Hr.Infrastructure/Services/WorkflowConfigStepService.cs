using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class WorkflowConfigStepService : IWorkflowConfigStepService
{
    private readonly IWorkflowConfigStepRepository _repo;
    private readonly ILogger<WorkflowConfigStepService> _logger;

    public WorkflowConfigStepService(IWorkflowConfigStepRepository repo, ILogger<WorkflowConfigStepService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<WorkflowConfigStepDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving workflow config steps"); throw; }
    }

    public async Task<WorkflowConfigStepDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving workflow config step {Id}", id); throw; }
    }

    public async Task<IEnumerable<WorkflowConfigStepDto>> GetByWorkflowConfigIdAsync(Guid workflowConfigId)
    {
        try { return (await _repo.GetByWorkflowConfigIdAsync(workflowConfigId)).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving steps for workflow {Id}", workflowConfigId); throw; }
    }

    public async Task<WorkflowConfigStepDto> CreateAsync(CreateWorkflowConfigStepDto request, Guid userId)
    {
        try
        {
            var entity = new WorkflowConfigStep { WorkflowConfigId = request.WorkflowConfigId, LevelNo = request.LevelNo, ApproverType = request.ApproverType, ApproverValue = request.ApproverValue, Mandatory = request.Mandatory, SLAHours = request.SLAHours, ExecutionType = request.ExecutionType, SortOrder = request.SortOrder, IsConditional = request.IsConditional, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating workflow config step"); throw; }
    }

    public async Task<WorkflowConfigStepDto> UpdateAsync(Guid id, UpdateWorkflowConfigStepDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Workflow config step not found");
            if (request.LevelNo.HasValue) entity.LevelNo = request.LevelNo.Value;
            if (request.ApproverType != null) entity.ApproverType = request.ApproverType;
            if (request.ApproverValue != null) entity.ApproverValue = request.ApproverValue;
            if (request.Mandatory.HasValue) entity.Mandatory = request.Mandatory.Value;
            if (request.SLAHours.HasValue) entity.SLAHours = request.SLAHours.Value;
            if (request.ExecutionType != null) entity.ExecutionType = request.ExecutionType;
            if (request.SortOrder.HasValue) entity.SortOrder = request.SortOrder.Value;
            if (request.IsConditional.HasValue) entity.IsConditional = request.IsConditional.Value;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating workflow config step {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Workflow config step not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting workflow config step {Id}", id); throw; }
    }

    private static WorkflowConfigStepDto Map(WorkflowConfigStep e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, WorkflowConfigId = e.WorkflowConfigId,
        LevelNo = e.LevelNo, ApproverType = e.ApproverType, ApproverValue = e.ApproverValue,
        Mandatory = e.Mandatory, SLAHours = e.SLAHours, ExecutionType = e.ExecutionType,
        SortOrder = e.SortOrder, IsConditional = e.IsConditional, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
