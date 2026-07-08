using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IWorkflowConfigService
{
    Task<IEnumerable<WorkflowConfigDto>> GetAllAsync();
    Task<WorkflowConfigDto?> GetByIdAsync(Guid id);
    Task<WorkflowConfigDto> CreateAsync(CreateWorkflowConfigDto request, Guid userId);
    Task<WorkflowConfigDto> UpdateAsync(Guid id, UpdateWorkflowConfigDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}

public interface IWorkflowConfigStepService
{
    Task<IEnumerable<WorkflowConfigStepDto>> GetAllAsync();
    Task<WorkflowConfigStepDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<WorkflowConfigStepDto>> GetByWorkflowConfigIdAsync(Guid workflowConfigId);
    Task<WorkflowConfigStepDto> CreateAsync(CreateWorkflowConfigStepDto request, Guid userId);
    Task<WorkflowConfigStepDto> UpdateAsync(Guid id, UpdateWorkflowConfigStepDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}

public interface IWorkflowConditionService
{
    Task<IEnumerable<WorkflowConditionDto>> GetAllAsync();
    Task<WorkflowConditionDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<WorkflowConditionDto>> GetByWorkflowConfigIdAsync(Guid workflowConfigId);
    Task<WorkflowConditionDto> CreateAsync(CreateWorkflowConditionDto request, Guid userId);
    Task<WorkflowConditionDto> UpdateAsync(Guid id, UpdateWorkflowConditionDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}

public interface IWorkflowEscalationService
{
    Task<IEnumerable<WorkflowEscalationDto>> GetAllAsync();
    Task<WorkflowEscalationDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<WorkflowEscalationDto>> GetByStepIdAsync(Guid workflowConfigStepId);
    Task<WorkflowEscalationDto> CreateAsync(CreateWorkflowEscalationDto request, Guid userId);
    Task<WorkflowEscalationDto> UpdateAsync(Guid id, UpdateWorkflowEscalationDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
