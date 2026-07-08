using System.Linq.Expressions;
using Crm.Domain.Entities;
using Crm.Infrastructure.Persistence;
using Crm.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Repository;

namespace Crm.Infrastructure.Repositories.Implementations;

// ??? Case ?????????????????????????????????????????????????????????????????????
public class CaseRepository : TenantAwareRepository<Case>, ICaseRepository
{
    public CaseRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<Case?> GetByIdWithDetailsAsync(Guid id)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet.AsNoTracking()
            .Include(c => c.Contact)
            .Include(c => c.Account)
            .Where(e => e.Id == id && e.CompanyId == companyId && e.BranchId == branchId
                     && e.BusinessUnitId == businessUnitId && !e.IsDeleted)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Case>> GetByAccountIdAsync(Guid accountId)
        => await FindAsync(c => c.AccountId == accountId);

    public async Task<(IEnumerable<Case> Items, int Total)> GetPagedByStatusAsync(int page, int pageSize, string? status)
    {
        Expression<Func<Case, bool>>? predicate = !string.IsNullOrWhiteSpace(status)
            ? c => c.Status == status
            : null;
        return await GetPagedAsync(page, pageSize, predicate);
    }
}

// ??? CaseComment ??????????????????????????????????????????????????????????????
public class CaseCommentRepository : TenantAwareRepository<CaseComment>, ICaseCommentRepository
{
    public CaseCommentRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<CaseComment>> GetByCaseIdAsync(Guid caseId)
        => await FindAsync(c => c.CaseId == caseId);
}

// ??? KnowledgeArticle ?????????????????????????????????????????????????????????
public class KnowledgeArticleRepository : TenantAwareRepository<KnowledgeArticle>, IKnowledgeArticleRepository
{
    public KnowledgeArticleRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<(IEnumerable<KnowledgeArticle> Items, int Total)> SearchPagedAsync(int page, int pageSize, string? search)
    {
        Expression<Func<KnowledgeArticle, bool>>? predicate = !string.IsNullOrWhiteSpace(search)
            ? a => a.Title.Contains(search)
            : null;
        return await GetPagedAsync(page, pageSize, predicate);
    }
}

// ??? Entitlement ??????????????????????????????????????????????????????????????
public class EntitlementRepository : TenantAwareRepository<Entitlement>, IEntitlementRepository
{
    public EntitlementRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<Entitlement?> GetByIdWithDetailsAsync(Guid id)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet.AsNoTracking()
            .Include(e => e.Account)
            .Where(e => e.Id == id && e.CompanyId == companyId && e.BranchId == branchId
                     && e.BusinessUnitId == businessUnitId && !e.IsDeleted)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Entitlement>> GetByAccountIdAsync(Guid accountId)
        => await FindAsync(e => e.AccountId == accountId);
}
