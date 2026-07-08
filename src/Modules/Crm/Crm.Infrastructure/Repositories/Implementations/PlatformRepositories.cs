using Crm.Domain.Entities;
using Crm.Infrastructure.Persistence;
using Crm.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Repository;

namespace Crm.Infrastructure.Repositories.Implementations;

// ??? Pipeline ?????????????????????????????????????????????????????????????????
public class PipelineRepository : TenantAwareRepository<Pipeline>, IPipelineRepository
{
    public PipelineRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<Pipeline?> GetByIdWithStagesAsync(Guid id)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet.AsNoTracking()
            .Include(p => p.Stages)
            .Where(e => e.Id == id && e.CompanyId == companyId && e.BranchId == branchId
                     && e.BusinessUnitId == businessUnitId && !e.IsDeleted)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Pipeline>> GetAllWithStagesAsync()
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet.AsNoTracking()
            .Include(p => p.Stages)
            .Where(e => e.CompanyId == companyId && e.BranchId == branchId
                     && e.BusinessUnitId == businessUnitId && !e.IsDeleted)
            .ToListAsync();
    }
}

// ??? PipelineStage ????????????????????????????????????????????????????????????
public class PipelineStageRepository : TenantAwareRepository<PipelineStage>, IPipelineStageRepository
{
    public PipelineStageRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<PipelineStage>> GetByPipelineIdAsync(Guid pipelineId)
        => await FindAsync(s => s.PipelineId == pipelineId);
}

// ??? Tag ??????????????????????????????????????????????????????????????????????
public class TagRepository : TenantAwareRepository<Tag>, ITagRepository
{
    public TagRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<Tag>> GetAllActiveAsync()
        => await GetAllAsync();
}

// ??? EntityTag ????????????????????????????????????????????????????????????????
public class EntityTagRepository : TenantAwareRepository<EntityTag>, IEntityTagRepository
{
    public EntityTagRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<EntityTag>> GetByEntityAsync(Guid entityId, string entityType)
        => await FindAsync(et => et.EntityId == entityId && et.EntityType == entityType);
}

// ??? EmailMessage ?????????????????????????????????????????????????????????????
public class EmailMessageRepository : TenantAwareRepository<EmailMessage>, IEmailMessageRepository
{
    public EmailMessageRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<EmailMessage>> GetByRelatedEntityAsync(Guid relatedToId, string relatedToType)
        => await FindAsync(e => e.RelatedToId == relatedToId && e.RelatedToType == relatedToType);

    public async Task<(IEnumerable<EmailMessage> Items, int Total)> GetPagedAsync(int page, int pageSize)
        => await GetPagedAsync(page, pageSize, null);
}

// ??? Territory ????????????????????????????????????????????????????????????????
public class TerritoryRepository : TenantAwareRepository<Territory>, ITerritoryRepository
{
    public TerritoryRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<Territory>> GetAllActiveAsync()
        => await GetAllAsync();
}

// ??? TerritoryAccount ?????????????????????????????????????????????????????????
public class TerritoryAccountRepository : TenantAwareRepository<TerritoryAccount>, ITerritoryAccountRepository
{
    public TerritoryAccountRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<bool> ExistsAsync(Guid territoryId, Guid accountId)
    {
        var match = await FirstOrDefaultAsync(ta => ta.TerritoryId == territoryId && ta.AccountId == accountId);
        return match != null;
    }

    public async Task<TerritoryAccount?> FindAssignmentAsync(Guid territoryId, Guid accountId)
        => await FirstOrDefaultAsync(ta => ta.TerritoryId == territoryId && ta.AccountId == accountId);
}

// ??? Team ?????????????????????????????????????????????????????????????????????
public class TeamRepository : TenantAwareRepository<Team>, ITeamRepository
{
    public TeamRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<Team?> GetByIdWithMembersAsync(Guid id)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet.AsNoTracking()
            .Include(t => t.Members)
            .Where(e => e.Id == id && e.CompanyId == companyId && e.BranchId == branchId
                     && e.BusinessUnitId == businessUnitId && !e.IsDeleted)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Team>> GetAllActiveAsync()
        => await GetAllAsync();
}

// ??? TeamMember ???????????????????????????????????????????????????????????????
public class TeamMemberRepository : TenantAwareRepository<TeamMember>, ITeamMemberRepository
{
    public TeamMemberRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<TeamMember>> GetByTeamIdAsync(Guid teamId)
        => await FindAsync(m => m.TeamId == teamId);
}

// ??? SalesTarget ??????????????????????????????????????????????????????????????
public class SalesTargetRepository : TenantAwareRepository<SalesTarget>, ISalesTargetRepository
{
    public SalesTargetRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<SalesTarget>> GetByUserIdAsync(Guid userId, int? fiscalYear)
    {
        if (fiscalYear.HasValue)
            return await FindAsync(t => t.UserId == userId && t.FiscalYear == fiscalYear.Value);
        return await FindAsync(t => t.UserId == userId);
    }

    public async Task<IEnumerable<SalesTarget>> GetByTeamIdAsync(Guid teamId, int? fiscalYear)
    {
        if (fiscalYear.HasValue)
            return await FindAsync(t => t.TeamId == teamId && t.FiscalYear == fiscalYear.Value);
        return await FindAsync(t => t.TeamId == teamId);
    }
}

// ??? Forecast ?????????????????????????????????????????????????????????????????
public class ForecastRepository : TenantAwareRepository<Forecast>, IForecastRepository
{
    public ForecastRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<Forecast>> GetByUserAsync(Guid userId, int fiscalYear, int fiscalQuarter)
        => await FindAsync(f => f.UserId == userId && f.FiscalYear == fiscalYear && f.FiscalQuarter == fiscalQuarter);
}
