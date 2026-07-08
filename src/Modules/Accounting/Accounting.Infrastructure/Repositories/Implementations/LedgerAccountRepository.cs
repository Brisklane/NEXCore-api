using Microsoft.AspNetCore.Http;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Accounting.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Accounting.Infrastructure.Repositories.Implementations;

/// <summary>
/// LedgerAccount repository implementation using TenantAwareRepository
/// Automatically filters all queries by CompanyId, BranchId, BusinessUnitId from JWT context
/// </summary>
public class LedgerAccountRepository : TenantAwareRepository<LedgerAccount>, ILedgerAccountRepository
{
    public LedgerAccountRepository(AccountingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    /// <summary>
    /// Get account by account number
    /// Tenant context automatically applied by base class
    /// </summary>
    public async Task<LedgerAccount?> GetByAccountNumberAsync(string accountNumber)
    {
        return await FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
    }

    /// <summary>
    /// Get accounts by ledger
    /// Tenant context automatically applied by base class
    /// </summary>
    public async Task<IEnumerable<LedgerAccount>> GetByLedgerIdAsync(Guid ledgerId)
    {
        var accounts = await FindAsync(a => a.LedgerId == ledgerId);
        return accounts.OrderBy(a => a.AccountNumber);
    }

    /// <summary>
    /// Get accounts by category
    /// Tenant context automatically applied by base class
    /// </summary>
    public async Task<IEnumerable<LedgerAccount>> GetByCategoryIdAsync(Guid categoryId)
    {
        return await FindAsync(a => a.CategoryId == categoryId);
    }

    /// <summary>
    /// Get chart of accounts for ledger
    /// </summary>
    public async Task<IEnumerable<LedgerAccount>> GetChartOfAccountsAsync(Guid companyId, Guid ledgerId)
    {
        var accounts = await FindAsync(a => a.LedgerId == ledgerId && a.IsActive);
        return accounts.OrderBy(a => a.AccountNumber);
    }
}
