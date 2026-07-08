using Microsoft.AspNetCore.Http;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Accounting.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Accounting.Infrastructure.Repositories.Implementations;

/// <summary>
/// Dimension repository implementation using TenantAwareRepository
/// Automatically filters all queries by CompanyId, BranchId, BusinessUnitId from JWT context
/// </summary>
public class DimensionRepository : TenantAwareRepository<Dimension>, IDimensionRepository
{
    public DimensionRepository(AccountingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    /// <summary>
    /// Get dimension by code
    /// Tenant context automatically applied by base class
    /// </summary>
    public async Task<Dimension?> GetByCodeAsync(string code)
    {
        return await FirstOrDefaultAsync(d => d.Code == code);
    }
}
