using Accounting.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Accounting.Infrastructure.Repositories.Interfaces;

/// <summary>
/// TaxCode repository interface extending generic repository
/// Tenant context (CompanyId, BranchId, BusinessUnitId) is automatically extracted and applied in TenantAwareRepository
/// </summary>
public interface ITaxCodeRepository : IRepository<TaxCode>
{
    /// <summary>
    /// Get tax code by code for current tenant
    /// Tenant filtering is automatic
    /// </summary>
    Task<TaxCode?> GetByCodeAsync(string code);

    /// <summary>
    /// Get active tax codes for current tenant
    /// Tenant filtering is automatic
    /// </summary>
    Task<IEnumerable<TaxCode>> GetActiveAsync();
}
