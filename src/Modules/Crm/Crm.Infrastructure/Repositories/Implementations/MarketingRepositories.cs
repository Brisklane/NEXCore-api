using System.Linq.Expressions;
using Crm.Domain.Entities;
using Crm.Infrastructure.Persistence;
using Crm.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Repository;

namespace Crm.Infrastructure.Repositories.Implementations;

// ??? Campaign ?????????????????????????????????????????????????????????????????
public class CampaignRepository : TenantAwareRepository<Campaign>, ICampaignRepository
{
    public CampaignRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<(IEnumerable<Campaign> Items, int Total)> GetPagedByStatusAsync(int page, int pageSize, string? status)
    {
        Expression<Func<Campaign, bool>>? predicate = !string.IsNullOrWhiteSpace(status)
            ? c => c.Status == status
            : null;
        return await GetPagedAsync(page, pageSize, predicate);
    }
}

// ??? CampaignMember ???????????????????????????????????????????????????????????
public class CampaignMemberRepository : TenantAwareRepository<CampaignMember>, ICampaignMemberRepository
{
    public CampaignMemberRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<CampaignMember>> GetByCampaignIdAsync(Guid campaignId)
        => await FindAsync(m => m.CampaignId == campaignId);
}

// ??? ContactList ??????????????????????????????????????????????????????????????
public class ContactListRepository : TenantAwareRepository<ContactList>, IContactListRepository
{
    public ContactListRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<ContactList?> GetByIdWithMembersAsync(Guid id)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet.AsNoTracking()
            .Include(cl => cl.Members)
            .Where(e => e.Id == id && e.CompanyId == companyId && e.BranchId == branchId
                     && e.BusinessUnitId == businessUnitId && !e.IsDeleted)
            .FirstOrDefaultAsync();
    }

    public async Task<(IEnumerable<ContactList> Items, int Total)> GetPagedAsync(int page, int pageSize)
        => await GetPagedAsync(page, pageSize, null);
}

// ??? ContactListMember ????????????????????????????????????????????????????????
public class ContactListMemberRepository : TenantAwareRepository<ContactListMember>, IContactListMemberRepository
{
    public ContactListMemberRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<ContactListMember>> GetByListIdAsync(Guid listId)
        => await FindAsync(m => m.ContactListId == listId);
}
