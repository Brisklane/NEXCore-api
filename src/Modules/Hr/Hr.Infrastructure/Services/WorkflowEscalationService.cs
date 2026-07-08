using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class WorkflowEscalationService : IWorkflowEscalationService
{
    private readonly IWorkflowEscalationRepository _repo;
    private readonly ILogger<WorkflowEscalationService> _logger;

    public WorkflowEscalationService(IWorkflowEscalationRepository repo, ILogger<WorkflowEscalationService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<WorkflowEscalationDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving workflow escalations"); throw; }
    }

    public async Task<WorkflowEscalationDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving workflow escalation {Id}", id); throw; }
    }

    public async Task<IEnumerable<WorkflowEscalationDto>> GetByStepIdAsync(Guid workflowConfigStepId)
    {
        try { return (await _repo.GetByStepIdAsync(workflowConfigStepId)).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving escalations for step {Id}", workflowConfigStepId); throw; }
    }

    public async Task<WorkflowEscalationDto> CreateAsync(CreateWorkflowEscalationDto request, Guid userId)
    {
        try
        {
            var entity = new WorkflowEscalation { WorkflowConfigStepId = request.WorkflowConfigStepId, AfterHours = request.AfterHours, ActionType = request.ActionType, ActionTarget = request.ActionTarget, ReminderCount = request.ReminderCount, AutoApproveFlag = request.AutoApproveFlag, IsActive = true, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating workflow escalation"); throw; }
    }

    public async Task<WorkflowEscalationDto> UpdateAsync(Guid id, UpdateWorkflowEscalationDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Workflow escalation not found");
            if (request.AfterHours.HasValue) entity.AfterHours = request.AfterHours.Value;
            if (request.ActionType != null) entity.ActionType = request.ActionType;
            if (request.ActionTarget != null) entity.ActionTarget = request.ActionTarget;
            if (request.ReminderCount.HasValue) entity.ReminderCount = request.ReminderCount.Value;
            if (request.AutoApproveFlag.HasValue) entity.AutoApproveFlag = request.AutoApproveFlag.Value;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating workflow escalation {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Workflow escalation not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting workflow escalation {Id}", id); throw; }
    }

    private static WorkflowEscalationDto Map(WorkflowEscalation e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, WorkflowConfigStepId = e.WorkflowConfigStepId,
        AfterHours = e.AfterHours, ActionType = e.ActionType, ActionTarget = e.ActionTarget,
        ReminderCount = e.ReminderCount, AutoApproveFlag = e.AutoApproveFlag, IsActive = e.IsActive,
        CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
