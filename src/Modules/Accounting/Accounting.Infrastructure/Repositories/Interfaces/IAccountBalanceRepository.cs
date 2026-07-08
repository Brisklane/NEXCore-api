using Accounting.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Accounting.Infrastructure.Repositories.Interfaces;

/// <summary>
/// AccountBalance repository interface extending generic repository
/// Tenant context (CompanyId, BranchId, BusinessUnitId) is automatically extracted and applied in TenantAwareRepository
/// </summary>
public interface IAccountBalanceRepository : IRepository<AccountBalance>
{
    /// <summary>
    /// Get balance by account and period
    /// Tenant filtering is automatic
    /// </summary>
    Task<AccountBalance?> GetByAccountAndPeriodAsync(Guid accountId, Guid periodId);

    /// <summary>
    /// Get balances by period
    /// Tenant filtering is automatic
    /// </summary>
    Task<IEnumerable<AccountBalance>> GetByPeriodAsync(Guid periodId);

    /// <summary>
    /// Get balances by account
    /// Tenant filtering is automatic
    /// </summary>
    Task<IEnumerable<AccountBalance>> GetByAccountAsync(Guid accountId);
}
