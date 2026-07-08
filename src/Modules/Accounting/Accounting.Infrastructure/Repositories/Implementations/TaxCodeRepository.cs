using Microsoft.AspNetCore.Http;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Accounting.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Accounting.Infrastructure.Repositories.Implementations;

/// <summary>
/// TaxCode repository implementation using TenantAwareRepository
/// Automatically filters all queries by CompanyId, BranchId, BusinessUnitId from JWT context
/// </summary>
public class TaxCodeRepository : TenantAwareRepository<TaxCode>, ITaxCodeRepository
{
    public TaxCodeRepository(AccountingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    /// <summary>
    /// Get tax code by code
    /// Tenant context automatically applied by base class
    /// </summary>
    public async Task<TaxCode?> GetByCodeAsync(string code)
    {
        return await FirstOrDefaultAsync(t => t.Code == code);
    }

    /// <summary>
    /// Get active tax codes
    /// Tenant context automatically applied by base class
    /// </summary>
    public async Task<IEnumerable<TaxCode>> GetActiveAsync()
    {
        return await FindAsync(t => t.IsActive);
    }
}
