using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class WorkflowConditionService : IWorkflowConditionService
{
    private readonly IWorkflowConditionRepository _repo;
    private readonly ILogger<WorkflowConditionService> _logger;

    public WorkflowConditionService(IWorkflowConditionRepository repo, ILogger<WorkflowConditionService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<WorkflowConditionDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving workflow conditions"); throw; }
    }

    public async Task<WorkflowConditionDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving workflow condition {Id}", id); throw; }
    }

    public async Task<IEnumerable<WorkflowConditionDto>> GetByWorkflowConfigIdAsync(Guid workflowConfigId)
    {
        try { return (await _repo.GetByWorkflowConfigIdAsync(workflowConfigId)).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving conditions for workflow {Id}", workflowConfigId); throw; }
    }

    public async Task<WorkflowConditionDto> CreateAsync(CreateWorkflowConditionDto request, Guid userId)
    {
        try
        {
            var entity = new WorkflowCondition { WorkflowConfigId = request.WorkflowConfigId, FieldName = request.FieldName, Operator = request.Operator, FieldValue = request.FieldValue, ActionType = request.ActionType, ActionValue = request.ActionValue, LogicalGroup = request.LogicalGroup, JoinOperator = request.JoinOperator, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating workflow condition"); throw; }
    }

    public async Task<WorkflowConditionDto> UpdateAsync(Guid id, UpdateWorkflowConditionDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Workflow condition not found");
            if (request.FieldName != null) entity.FieldName = request.FieldName;
            if (request.Operator != null) entity.Operator = request.Operator;
            if (request.FieldValue != null) entity.FieldValue = request.FieldValue;
            if (request.ActionType != null) entity.ActionType = request.ActionType;
            if (request.ActionValue != null) entity.ActionValue = request.ActionValue;
            if (request.LogicalGroup.HasValue) entity.LogicalGroup = request.LogicalGroup.Value;
            if (request.JoinOperator != null) entity.JoinOperator = request.JoinOperator;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating workflow condition {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Workflow condition not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting workflow condition {Id}", id); throw; }
    }

    private static WorkflowConditionDto Map(WorkflowCondition e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, WorkflowConfigId = e.WorkflowConfigId,
        FieldName = e.FieldName, Operator = e.Operator, FieldValue = e.FieldValue,
        ActionType = e.ActionType, ActionValue = e.ActionValue, LogicalGroup = e.LogicalGroup,
        JoinOperator = e.JoinOperator, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
