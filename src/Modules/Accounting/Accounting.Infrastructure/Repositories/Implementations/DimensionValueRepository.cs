using Microsoft.AspNetCore.Http;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Accounting.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Accounting.Infrastructure.Repositories.Implementations;

/// <summary>
/// DimensionValue repository implementation using TenantAwareRepository
/// Automatically filters all queries by CompanyId, BranchId, BusinessUnitId from JWT context
/// </summary>
public class DimensionValueRepository : TenantAwareRepository<DimensionValue>, IDimensionValueRepository
{
    public DimensionValueRepository(AccountingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    /// <summary>
    /// Get values by dimension
    /// Tenant context automatically applied by base class
    /// </summary>
    public async Task<IEnumerable<DimensionValue>> GetByDimensionIdAsync(Guid dimensionId)
    {
        return await FindAsync(v => v.DimensionId == dimensionId && v.IsActive);
    }
}
