using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Accounting.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Accounting.Infrastructure.Repositories.Implementations;

/// <summary>
/// JournalEntry repository implementation using TenantAwareRepository
/// Automatically filters all queries by CompanyId, BranchId, BusinessUnitId from JWT context
/// </summary>
public class JournalEntryRepository : TenantAwareRepository<JournalEntry>, IJournalEntryRepository
{
    public JournalEntryRepository(AccountingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    /// <summary>
    /// Get journal entry by number
    /// Tenant context automatically applied by base class
    /// </summary>
    public async Task<JournalEntry?> GetByJournalNumberAsync(string journalNumber)
    {
        return await FirstOrDefaultAsync(j => j.JournalNumber == journalNumber);
    }

    /// <summary>
    /// Get a journal entry with lines loaded (tracked) and tenant-filtered.
    /// </summary>
    public async Task<JournalEntry?> GetByIdWithLinesAsync(Guid id)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e =>
                e.Id == id &&
                e.CompanyId == companyId &&
                e.BranchId == branchId &&
                e.BusinessUnitId == businessUnitId &&
                !e.IsDeleted);
    }

    /// <summary>
    /// Get entries by ledger
    /// Tenant context automatically applied by base class
    /// </summary>
    public async Task<IEnumerable<JournalEntry>> GetByLedgerIdAsync(Guid ledgerId)
    {
        return await FindAsync(j => j.LedgerId == ledgerId);
    }

    /// <summary>
    /// Get entries by date range
    /// Tenant context automatically applied by base class
    /// </summary>
    public async Task<IEnumerable<JournalEntry>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        return await FindAsync(j => j.PostingDate >= fromDate && j.PostingDate <= toDate);
    }

    /// <summary>
    /// Get entries by status
    /// Tenant context automatically applied by base class
    /// </summary>
    public async Task<IEnumerable<JournalEntry>> GetByStatusAsync(JournalEntryStatus status)
    {
        return await FindAsync(j => j.Status == status);
    }
}
