using Accounting.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Accounting.Infrastructure.Repositories.Interfaces;

/// <summary>
/// JournalLine repository interface extending generic repository
/// Tenant context (CompanyId, BranchId, BusinessUnitId) is automatically extracted and applied in TenantAwareRepository
/// </summary>
public interface IJournalLineRepository : IRepository<JournalLine>
{
    /// <summary>
    /// Get lines by journal entry
    /// Tenant filtering is automatic
    /// </summary>
    Task<IEnumerable<JournalLine>> GetByJournalEntryIdAsync(Guid journalEntryId);

    /// <summary>
    /// Get lines by account
    /// Tenant filtering is automatic
    /// </summary>
    Task<IEnumerable<JournalLine>> GetByAccountIdAsync(Guid accountId);
}
