using Accounting.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Accounting.Infrastructure.Repositories.Interfaces;

/// <summary>
/// Ledger repository interface extending generic repository
/// Tenant context (CompanyId, BranchId, BusinessUnitId) is automatically extracted and applied in TenantAwareRepository
/// </summary>
public interface ILedgerRepository : IRepository<Ledger>
{
    /// <summary>
    /// Get default ledger for current tenant
    /// Tenant filtering is automatic
    /// </summary>
    Task<Ledger?> GetDefaultLedgerAsync();

    /// <summary>
    /// Get ledgers by current tenant
    /// Tenant filtering is automatic
    /// </summary>
    Task<IEnumerable<Ledger>> GetByCompanyIdAsync();
}
