using System.Linq.Expressions;
using Crm.Domain.Entities;
using Crm.Infrastructure.Persistence;
using Crm.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Repository;

namespace Crm.Infrastructure.Repositories.Implementations;

// ??? Account ??????????????????????????????????????????????????????????????????
public class AccountRepository : TenantAwareRepository<Account>, IAccountRepository
{
    public AccountRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<Account?> GetByIdWithDetailsAsync(Guid id)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet.AsNoTracking()
            .Where(e => e.Id == id && e.CompanyId == companyId && e.BranchId == branchId
                     && e.BusinessUnitId == businessUnitId && !e.IsDeleted)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Account>> GetByParentAccountIdAsync(Guid parentId)
        => await FindAsync(a => a.ParentAccountId == parentId);

    public async Task<(IEnumerable<Account> Items, int Total)> SearchPagedAsync(int page, int pageSize, string? search)
    {
        Expression<Func<Account, bool>>? predicate = null;
        if (!string.IsNullOrWhiteSpace(search))
            predicate = a => a.AccountName.Contains(search);

        var (items, total) = await GetPagedAsync(page, pageSize, predicate);
        return (items.OrderBy(a => a.AccountName), total);
    }
}

// ??? Contact ??????????????????????????????????????????????????????????????????
public class ContactRepository : TenantAwareRepository<Contact>, IContactRepository
{
    public ContactRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<Contact?> GetByIdWithDetailsAsync(Guid id)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet.AsNoTracking()
            .Include(c => c.Account)
            .Where(e => e.Id == id && e.CompanyId == companyId && e.BranchId == branchId
                     && e.BusinessUnitId == businessUnitId && !e.IsDeleted)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Contact>> GetByAccountIdAsync(Guid accountId)
        => await FindAsync(c => c.AccountId == accountId);

    public async Task<(IEnumerable<Contact> Items, int Total)> SearchPagedAsync(int page, int pageSize, string? search)
    {
        Expression<Func<Contact, bool>>? predicate = null;
        if (!string.IsNullOrWhiteSpace(search))
        {
            // ILike, not Contains: on PostgreSQL `Contains` is a case-sensitive LIKE, so
            // "khan" would miss "Khan". Phone is included because at a till that is how a
            // customer is looked up far more often than by spelling out a name.
            var needle = $"%{search.Trim()}%";
            predicate = c =>
                   EF.Functions.ILike(c.FirstName ?? "", needle)
                || EF.Functions.ILike(c.LastName, needle)
                || EF.Functions.ILike(c.Email ?? "", needle)
                || EF.Functions.ILike(c.Phone ?? "", needle);
        }

        // Ordering belongs in the query: sorting the page after it has been fetched only
        // shuffles the rows that already came back, which makes paging non-deterministic.
        return await GetPagedAsync(page, pageSize, predicate,
            orderBy: q => q.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ThenBy(c => c.Id));
    }
}

// ??? Lead ?????????????????????????????????????????????????????????????????????
public class LeadRepository : TenantAwareRepository<Lead>, ILeadRepository
{
    public LeadRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<(IEnumerable<Lead> Items, int Total)> SearchPagedAsync(int page, int pageSize, string? search, string? status)
    {
        Expression<Func<Lead, bool>>? predicate = null;
        if (!string.IsNullOrWhiteSpace(search) && !string.IsNullOrWhiteSpace(status))
            predicate = l => (l.FirstName!.Contains(search) || l.LastName.Contains(search) || l.Company.Contains(search)) && l.Status == status;
        else if (!string.IsNullOrWhiteSpace(search))
            predicate = l => l.FirstName!.Contains(search) || l.LastName.Contains(search) || l.Company.Contains(search);
        else if (!string.IsNullOrWhiteSpace(status))
            predicate = l => l.Status == status;
        return await GetPagedAsync(page, pageSize, predicate);
    }
}

// ??? Deal ?????????????????????????????????????????????????????????????????????
public class DealRepository : TenantAwareRepository<Deal>, IDealRepository
{
    public DealRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<Deal?> GetByIdWithDetailsAsync(Guid id)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet.AsNoTracking()
            .Include(d => d.Account)
            .Include(d => d.DealProducts)
            .Include(d => d.DealContacts)
            .Where(e => e.Id == id && e.CompanyId == companyId && e.BranchId == branchId
                     && e.BusinessUnitId == businessUnitId && !e.IsDeleted)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Deal>> GetByAccountIdAsync(Guid accountId)
        => await FindAsync(d => d.AccountId == accountId);

    public async Task<(IEnumerable<Deal> Items, int Total)> SearchPagedAsync(int page, int pageSize, string? search, string? stage)
    {
        Expression<Func<Deal, bool>>? predicate = null;
        if (!string.IsNullOrWhiteSpace(search) && !string.IsNullOrWhiteSpace(stage))
            predicate = d => d.OpportunityName.Contains(search) && d.Stage == stage;
        else if (!string.IsNullOrWhiteSpace(search))
            predicate = d => d.OpportunityName.Contains(search);
        else if (!string.IsNullOrWhiteSpace(stage))
            predicate = d => d.Stage == stage;
        return await GetPagedAsync(page, pageSize, predicate);
    }
}

// ??? Activity ?????????????????????????????????????????????????????????????????
public class ActivityRepository : TenantAwareRepository<Activity>, IActivityRepository
{
    public ActivityRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<Activity>> GetByRelatedEntityAsync(Guid relatedToId, string relatedToType)
        => await FindAsync(a => a.RelatedToId == relatedToId && a.RelatedToType == relatedToType);

    public async Task<(IEnumerable<Activity> Items, int Total)> GetPagedByTypeAsync(int page, int pageSize, string? type)
    {
        Expression<Func<Activity, bool>>? predicate = !string.IsNullOrWhiteSpace(type)
            ? a => a.Type == type
            : null;
        return await GetPagedAsync(page, pageSize, predicate);
    }
}

// ??? Note ?????????????????????????????????????????????????????????????????????
public class NoteRepository : TenantAwareRepository<Note>, INoteRepository
{
    public NoteRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<Note>> GetByParentAsync(Guid parentId, string parentType)
        => await FindAsync(n => n.ParentId == parentId && n.ParentType == parentType);
}

// ??? Attachment ???????????????????????????????????????????????????????????????
public class AttachmentRepository : TenantAwareRepository<Attachment>, IAttachmentRepository
{
    public AttachmentRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<Attachment>> GetByParentAsync(Guid parentId, string parentType)
        => await FindAsync(a => a.ParentId == parentId && a.ParentType == parentType);
}

// ??? Contact Address ???????????????????????????????????????????????????????????
public class ContactAddressRepository : TenantAwareRepository<ContactAddress>, IContactAddressRepository
{
    public ContactAddressRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<List<ContactAddress>> GetByContactAsync(Guid contactId)
        => (await FindAsync(a => a.ContactId == contactId && a.IsActive && !a.IsDeleted)).ToList();

    public async Task<ContactAddress?> GetDefaultAsync(Guid contactId)
        => await FirstOrDefaultAsync(a => a.ContactId == contactId && a.IsDefault && a.IsActive && !a.IsDeleted);
}
