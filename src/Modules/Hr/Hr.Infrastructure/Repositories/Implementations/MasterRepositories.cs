using Microsoft.AspNetCore.Http;
using Hr.Domain.Entities;
using Hr.Infrastructure.Persistence;
using Hr.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Implementations;

public class OnboardingTaskTemplateRepository : TenantAwareRepository<OnboardingTaskTemplate>, IOnboardingTaskTemplateRepository
{
    public OnboardingTaskTemplateRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
    public async Task<IEnumerable<OnboardingTaskTemplate>> GetAllByTenantAsync() => await GetAllAsync();
}

public class SkillCategoryRepository : TenantAwareRepository<SkillCategory>, ISkillCategoryRepository
{
    public SkillCategoryRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
    public async Task<IEnumerable<SkillCategory>> GetAllByTenantAsync() => await GetAllAsync();
}

public class SkillRepository : TenantAwareRepository<Skill>, ISkillRepository
{
    public SkillRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
    public async Task<IEnumerable<Skill>> GetAllByTenantAsync() => await GetAllAsync();
    public async Task<IEnumerable<Skill>> GetByCategoryAsync(Guid skillCategoryId)
        => await FindAsync(s => s.SkillCategoryId == skillCategoryId && !s.IsDeleted);
}

public class CompetencyFrameworkRepository : TenantAwareRepository<CompetencyFramework>, ICompetencyFrameworkRepository
{
    public CompetencyFrameworkRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
    public async Task<IEnumerable<CompetencyFramework>> GetAllByTenantAsync() => await GetAllAsync();
}

public class CompetencyFrameworkItemRepository : TenantAwareRepository<CompetencyFrameworkItem>, ICompetencyFrameworkItemRepository
{
    public CompetencyFrameworkItemRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
    public async Task<IEnumerable<CompetencyFrameworkItem>> GetAllByTenantAsync() => await GetAllAsync();
    public async Task<IEnumerable<CompetencyFrameworkItem>> GetByFrameworkIdAsync(Guid frameworkId)
        => await FindAsync(i => i.CompetencyFrameworkId == frameworkId && !i.IsDeleted);
}

public class WorkflowConfigRepository : TenantAwareRepository<WorkflowConfig>, IWorkflowConfigRepository
{
    public WorkflowConfigRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
    public async Task<IEnumerable<WorkflowConfig>> GetAllByTenantAsync() => await GetAllAsync();
}

public class WorkflowConfigStepRepository : TenantAwareRepository<WorkflowConfigStep>, IWorkflowConfigStepRepository
{
    public WorkflowConfigStepRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
    public async Task<IEnumerable<WorkflowConfigStep>> GetAllByTenantAsync() => await GetAllAsync();
    public async Task<IEnumerable<WorkflowConfigStep>> GetByWorkflowConfigIdAsync(Guid workflowConfigId)
        => await FindAsync(s => s.WorkflowConfigId == workflowConfigId && !s.IsDeleted);
}

public class WorkflowConditionRepository : TenantAwareRepository<WorkflowCondition>, IWorkflowConditionRepository
{
    public WorkflowConditionRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
    public async Task<IEnumerable<WorkflowCondition>> GetAllByTenantAsync() => await GetAllAsync();
    public async Task<IEnumerable<WorkflowCondition>> GetByWorkflowConfigIdAsync(Guid workflowConfigId)
        => await FindAsync(c => c.WorkflowConfigId == workflowConfigId && !c.IsDeleted);
}

public class WorkflowEscalationRepository : TenantAwareRepository<WorkflowEscalation>, IWorkflowEscalationRepository
{
    public WorkflowEscalationRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
    public async Task<IEnumerable<WorkflowEscalation>> GetAllByTenantAsync() => await GetAllAsync();
    public async Task<IEnumerable<WorkflowEscalation>> GetByStepIdAsync(Guid workflowConfigStepId)
        => await FindAsync(e => e.WorkflowConfigStepId == workflowConfigStepId && !e.IsDeleted);
}
