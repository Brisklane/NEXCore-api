using Microsoft.AspNetCore.Http;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Accounting.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Accounting.Infrastructure.Repositories.Implementations;

/// <summary>
/// Ledger repository implementation using TenantAwareRepository
/// Automatically filters all queries by CompanyId, BranchId, BusinessUnitId from JWT context
/// </summary>
public class LedgerRepository : TenantAwareRepository<Ledger>, ILedgerRepository
{
    public LedgerRepository(AccountingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    /// <summary>
    /// Get default ledger for company
    /// Tenant context automatically applied by base class
    /// </summary>
    public async Task<Ledger?> GetDefaultLedgerAsync()
    {
        return await FirstOrDefaultAsync(l => l.IsDefault);
    }

    /// <summary>
    /// Get ledgers by company
    /// Tenant context automatically applied by base class
    /// </summary>
    public async Task<IEnumerable<Ledger>> GetByCompanyIdAsync()
    {
        return await GetAllAsync();
    }
}
