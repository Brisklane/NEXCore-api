using Accounting.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Accounting.Infrastructure.Repositories.Interfaces;

/// <summary>
/// AccountCategory repository interface extending generic repository
/// Tenant context (CompanyId, BranchId, BusinessUnitId) is automatically extracted and applied in TenantAwareRepository
/// </summary>
public interface IAccountCategoryRepository : IRepository<AccountCategory>
{
    /// <summary>
    /// Get active categories scoped to the company only (ignores BranchId/BusinessUnitId).
    /// Account categories are company-wide reference data shared across all branches/units.
    /// </summary>
    Task<IEnumerable<AccountCategory>> GetActiveAsync();

    /// <summary>
    /// Add categories as company-wide data: stamps CompanyId from JWT but stores Guid.Empty for
    /// BranchId and BusinessUnitId so the records are visible across all branches/units.
    /// </summary>
    Task AddCompanyWideRangeAsync(IEnumerable<AccountCategory> entities);
}
