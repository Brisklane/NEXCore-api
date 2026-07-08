using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface IOnboardingTaskTemplateRepository : IRepository<OnboardingTaskTemplate>
{
    Task<IEnumerable<OnboardingTaskTemplate>> GetAllByTenantAsync();
}

public interface ISkillCategoryRepository : IRepository<SkillCategory>
{
    Task<IEnumerable<SkillCategory>> GetAllByTenantAsync();
}

public interface ISkillRepository : IRepository<Skill>
{
    Task<IEnumerable<Skill>> GetAllByTenantAsync();
    Task<IEnumerable<Skill>> GetByCategoryAsync(Guid skillCategoryId);
}

public interface ICompetencyFrameworkRepository : IRepository<CompetencyFramework>
{
    Task<IEnumerable<CompetencyFramework>> GetAllByTenantAsync();
}

public interface ICompetencyFrameworkItemRepository : IRepository<CompetencyFrameworkItem>
{
    Task<IEnumerable<CompetencyFrameworkItem>> GetAllByTenantAsync();
    Task<IEnumerable<CompetencyFrameworkItem>> GetByFrameworkIdAsync(Guid frameworkId);
}

public interface IWorkflowConfigRepository : IRepository<WorkflowConfig>
{
    Task<IEnumerable<WorkflowConfig>> GetAllByTenantAsync();
}

public interface IWorkflowConfigStepRepository : IRepository<WorkflowConfigStep>
{
    Task<IEnumerable<WorkflowConfigStep>> GetAllByTenantAsync();
    Task<IEnumerable<WorkflowConfigStep>> GetByWorkflowConfigIdAsync(Guid workflowConfigId);
}

public interface IWorkflowConditionRepository : IRepository<WorkflowCondition>
{
    Task<IEnumerable<WorkflowCondition>> GetAllByTenantAsync();
    Task<IEnumerable<WorkflowCondition>> GetByWorkflowConfigIdAsync(Guid workflowConfigId);
}

public interface IWorkflowEscalationRepository : IRepository<WorkflowEscalation>
{
    Task<IEnumerable<WorkflowEscalation>> GetAllByTenantAsync();
    Task<IEnumerable<WorkflowEscalation>> GetByStepIdAsync(Guid workflowConfigStepId);
}
