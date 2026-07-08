using Accounting.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Accounting.Infrastructure.Repositories.Interfaces;

/// <summary>
/// LedgerAccount repository interface extending generic repository
/// Tenant context (CompanyId, BranchId, BusinessUnitId) is automatically extracted and applied in TenantAwareRepository
/// </summary>
public interface ILedgerAccountRepository : IRepository<LedgerAccount>
{
    /// <summary>
    /// Get account by account number
    /// Tenant filtering is automatic
    /// </summary>
    Task<LedgerAccount?> GetByAccountNumberAsync(string accountNumber);

    /// <summary>
    /// Get accounts by ledger
    /// Tenant filtering is automatic
    /// </summary>
    Task<IEnumerable<LedgerAccount>> GetByLedgerIdAsync(Guid ledgerId);

    /// <summary>
    /// Get accounts by category
    /// Tenant filtering is automatic
    /// </summary>
    Task<IEnumerable<LedgerAccount>> GetByCategoryIdAsync(Guid categoryId);
}
