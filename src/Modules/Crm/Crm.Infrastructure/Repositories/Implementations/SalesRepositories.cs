using System.Linq.Expressions;
using Crm.Domain.Entities;
using Crm.Infrastructure.Persistence;
using Crm.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Repository;

namespace Crm.Infrastructure.Repositories.Implementations;

// ??? Product ??????????????????????????????????????????????????????????????????
public class ProductRepository : TenantAwareRepository<Product>, IProductRepository
{
    public ProductRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<(IEnumerable<Product> Items, int Total)> SearchPagedAsync(int page, int pageSize, string? search)
    {
        Expression<Func<Product, bool>>? predicate = !string.IsNullOrWhiteSpace(search)
            ? p => p.ProductName.Contains(search) || (p.ProductCode != null && p.ProductCode.Contains(search))
            : null;
        return await GetPagedAsync(page, pageSize, predicate);
    }
}

// ??? Pricebook ????????????????????????????????????????????????????????????????
public class PricebookRepository : TenantAwareRepository<Pricebook>, IPricebookRepository
{
    public PricebookRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<Pricebook>> GetAllActiveAsync()
        => await FindAsync(p => p.IsActive);
}

// ??? PricebookEntry ???????????????????????????????????????????????????????????
public class PricebookEntryRepository : TenantAwareRepository<PricebookEntry>, IPricebookEntryRepository
{
    public PricebookEntryRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<PricebookEntry>> GetByPricebookIdAsync(Guid pricebookId)
        => await FindAsync(e => e.PricebookId == pricebookId);
}

// ??? DealProduct ??????????????????????????????????????????????????????????????
public class DealProductRepository : TenantAwareRepository<DealProduct>, IDealProductRepository
{
    public DealProductRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<DealProduct>> GetByDealIdAsync(Guid dealId)
        => await FindAsync(dp => dp.DealId == dealId);
}

// ??? DealContact ??????????????????????????????????????????????????????????????
public class DealContactRepository : TenantAwareRepository<DealContact>, IDealContactRepository
{
    public DealContactRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<DealContact>> GetByDealIdAsync(Guid dealId)
        => await FindAsync(dc => dc.DealId == dealId);
}

// ??? Quote ????????????????????????????????????????????????????????????????????
public class QuoteRepository : TenantAwareRepository<Quote>, IQuoteRepository
{
    public QuoteRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<Quote?> GetByIdWithLineItemsAsync(Guid id)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet.AsNoTracking()
            .Include(q => q.LineItems)
            .Where(e => e.Id == id && e.CompanyId == companyId && e.BranchId == branchId
                     && e.BusinessUnitId == businessUnitId && !e.IsDeleted)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Quote>> GetByDealIdAsync(Guid dealId)
        => await FindAsync(q => q.DealId == dealId);

    public async Task<(IEnumerable<Quote> Items, int Total)> GetPagedAsync(int page, int pageSize)
        => await GetPagedAsync(page, pageSize, null);
}

// ??? Order ????????????????????????????????????????????????????????????????????
public class OrderRepository : TenantAwareRepository<Order>, IOrderRepository
{
    public OrderRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<Order?> GetByIdWithDetailsAsync(Guid id)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet.AsNoTracking()
            .Include(o => o.LineItems)
            .Where(e => e.Id == id && e.CompanyId == companyId && e.BranchId == branchId
                     && e.BusinessUnitId == businessUnitId && !e.IsDeleted)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Order>> GetByAccountIdAsync(Guid accountId)
        => await FindAsync(o => o.AccountId == accountId);

    public async Task<(IEnumerable<Order> Items, int Total)> GetPagedAsync(int page, int pageSize)
        => await GetPagedAsync(page, pageSize, null);
}

// ??? Contract ?????????????????????????????????????????????????????????????????
public class ContractRepository : TenantAwareRepository<Contract>, IContractRepository
{
    public ContractRepository(CrmDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<Contract?> GetByIdWithDetailsAsync(Guid id)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet.AsNoTracking()
            .Include(c => c.Account)
            .Where(e => e.Id == id && e.CompanyId == companyId && e.BranchId == branchId
                     && e.BusinessUnitId == businessUnitId && !e.IsDeleted)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Contract>> GetByAccountIdAsync(Guid accountId)
        => await FindAsync(c => c.AccountId == accountId);

    public async Task<(IEnumerable<Contract> Items, int Total)> GetPagedAsync(int page, int pageSize)
        => await GetPagedAsync(page, pageSize, null);
}
