using Accounting.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Accounting.Infrastructure.Repositories.Interfaces;

/// <summary>
/// PostingProfile repository interface extending generic repository
/// Tenant context (CompanyId, BranchId, BusinessUnitId) is automatically extracted and applied in TenantAwareRepository
/// </summary>
public interface IPostingProfileRepository : IRepository<PostingProfile>
{
    /// <summary>
    /// Get profile by type for current tenant
    /// Tenant filtering is automatic
    /// </summary>
    Task<PostingProfile?> GetByTypeAsync(string moduleName, string transactionType);

    /// <summary>
    /// Get profiles by module for current tenant
    /// Tenant filtering is automatic
    /// </summary>
    Task<IEnumerable<PostingProfile>> GetByModuleAsync(string moduleName);
}
