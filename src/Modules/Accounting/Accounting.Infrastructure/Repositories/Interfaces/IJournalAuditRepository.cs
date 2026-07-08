using Accounting.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Accounting.Infrastructure.Repositories.Interfaces;

/// <summary>
/// JournalAudit repository interface extending generic repository
/// Tenant context (CompanyId, BranchId, BusinessUnitId) is automatically extracted and applied in TenantAwareRepository
/// </summary>
public interface IJournalAuditRepository : IRepository<JournalAudit>
{
    /// <summary>
    /// Get audits by journal
    /// Tenant filtering is automatic
    /// </summary>
    Task<IEnumerable<JournalAudit>> GetByJournalIdAsync(Guid journalId);
}
