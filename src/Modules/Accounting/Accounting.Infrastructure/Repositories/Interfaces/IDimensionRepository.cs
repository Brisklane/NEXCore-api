using Accounting.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Accounting.Infrastructure.Repositories.Interfaces;

/// <summary>
/// Dimension repository interface extending generic repository
/// Tenant context (CompanyId, BranchId, BusinessUnitId) is automatically extracted and applied in TenantAwareRepository
/// </summary>
public interface IDimensionRepository : IRepository<Dimension>
{
    /// <summary>
    /// Get dimension by code for current tenant
    /// Tenant filtering is automatic
    /// </summary>
    Task<Dimension?> GetByCodeAsync(string code);
}
