using Microsoft.AspNetCore.Http;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Accounting.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Accounting.Infrastructure.Repositories.Implementations;

/// <summary>
/// AccountBalance repository implementation using TenantAwareRepository
/// Automatically filters all queries by CompanyId, BranchId, BusinessUnitId from JWT context
/// </summary>
public class AccountBalanceRepository : TenantAwareRepository<AccountBalance>, IAccountBalanceRepository
{
    public AccountBalanceRepository(AccountingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    /// <summary>
    /// Get balance by account and period
    /// Tenant context automatically applied by base class
    /// </summary>
    public async Task<AccountBalance?> GetByAccountAndPeriodAsync(Guid accountId, Guid periodId)
    {
        return await FirstOrDefaultAsync(b => b.LedgerAccountId == accountId && b.FiscalPeriodId == periodId);
    }

    /// <summary>
    /// Get balances by period
    /// Tenant context automatically applied by base class
    /// </summary>
    public async Task<IEnumerable<AccountBalance>> GetByPeriodAsync(Guid periodId)
    {
        return await FindAsync(b => b.FiscalPeriodId == periodId);
    }

    /// <summary>
    /// Get balances by account
    /// Tenant context automatically applied by base class
    /// </summary>
    public async Task<IEnumerable<AccountBalance>> GetByAccountAsync(Guid accountId)
    {
        return await FindAsync(b => b.LedgerAccountId == accountId);
    }
}
