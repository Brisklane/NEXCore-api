using Accounting.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Accounting.Infrastructure.Repositories.Interfaces;

/// <summary>
/// DimensionValue repository interface extending generic repository
/// Tenant context (CompanyId, BranchId, BusinessUnitId) is automatically extracted and applied in TenantAwareRepository
/// </summary>
public interface IDimensionValueRepository : IRepository<DimensionValue>
{
    /// <summary>
    /// Get values by dimension
    /// Tenant filtering is automatic
    /// </summary>
    Task<IEnumerable<DimensionValue>> GetByDimensionIdAsync(Guid dimensionId);
}
