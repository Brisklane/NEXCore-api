using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Accounting.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Accounting.Infrastructure.Repositories.Implementations;

/// <summary>
/// AccountCategory repository.
/// Categories are company-wide reference data — reads filter by CompanyId only,
/// ignoring BranchId/BusinessUnitId so every branch/unit sees the same set.
/// </summary>
public class AccountCategoryRepository : TenantAwareRepository<AccountCategory>, IAccountCategoryRepository
{
    public AccountCategoryRepository(AccountingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    /// <inheritdoc />
    public async Task<IEnumerable<AccountCategory>> GetActiveAsync()
    {
        var (companyId, _, _) = GetTenantContext();
        return await DbSet.AsNoTracking()
            .Where(c => c.CompanyId == companyId && c.IsActive && !c.IsDeleted)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task AddCompanyWideRangeAsync(IEnumerable<AccountCategory> entities)
    {
        var (companyId, _, _) = GetTenantContext();
        foreach (var entity in entities)
        {
            entity.CompanyId = companyId;
            entity.BranchId = Guid.Empty;
            entity.BusinessUnitId = Guid.Empty;
        }
        await DbSet.AddRangeAsync(entities);
    }
}
