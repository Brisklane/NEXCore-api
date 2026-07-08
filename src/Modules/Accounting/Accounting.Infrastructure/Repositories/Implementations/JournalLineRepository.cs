using Microsoft.AspNetCore.Http;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Accounting.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Accounting.Infrastructure.Repositories.Implementations;

/// <summary>
/// JournalLine repository implementation using TenantAwareRepository
/// Automatically filters all queries by CompanyId, BranchId, BusinessUnitId from JWT context
/// </summary>
public class JournalLineRepository : TenantAwareRepository<JournalLine>, IJournalLineRepository
{
    public JournalLineRepository(AccountingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    /// <summary>
    /// Get lines by journal entry
    /// Tenant context automatically applied by base class
    /// </summary>
    public async Task<IEnumerable<JournalLine>> GetByJournalEntryIdAsync(Guid journalEntryId)
    {
        return await FindAsync(l => l.JournalEntryId == journalEntryId);
    }

    /// <summary>
    /// Get lines by account
    /// Tenant context automatically applied by base class
    /// </summary>
    public async Task<IEnumerable<JournalLine>> GetByAccountIdAsync(Guid accountId)
    {
        return await FindAsync(l => l.LedgerAccountId == accountId);
    }
}
