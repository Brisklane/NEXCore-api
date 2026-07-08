using Accounting.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Accounting.Infrastructure.Repositories.Interfaces;

/// <summary>
/// JournalEntry repository interface extending generic repository
/// Tenant context (CompanyId, BranchId, BusinessUnitId) is automatically extracted and applied in TenantAwareRepository
/// </summary>
public interface IJournalEntryRepository : IRepository<JournalEntry>
{
    /// <summary>
    /// Get entry by journal number
    /// Tenant filtering is automatic
    /// </summary>
    Task<JournalEntry?> GetByJournalNumberAsync(string journalNumber);

    /// <summary>
    /// Get a journal entry with its lines loaded (tracked), tenant-filtered.
    /// Used for detail view and Draft editing where lines must be read/replaced.
    /// </summary>
    Task<JournalEntry?> GetByIdWithLinesAsync(Guid id);

    /// <summary>
    /// Get entries by ledger
    /// Tenant filtering is automatic
    /// </summary>
    Task<IEnumerable<JournalEntry>> GetByLedgerIdAsync(Guid ledgerId);

    /// <summary>
    /// Get entries by date range
    /// Tenant filtering is automatic
    /// </summary>
    Task<IEnumerable<JournalEntry>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Get entries by status
    /// Tenant filtering is automatic
    /// </summary>
    Task<IEnumerable<JournalEntry>> GetByStatusAsync(JournalEntryStatus status);
}
