using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Accounting.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Accounting.Infrastructure.Repositories.Implementations;

/// <summary>
/// JournalAudit repository implementation using TenantAwareRepository
/// Automatically filters all queries by CompanyId, BranchId, BusinessUnitId from JWT context
/// </summary>
public class JournalAuditRepository : TenantAwareRepository<JournalAudit>, IJournalAuditRepository
{
    public JournalAuditRepository(AccountingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    /// <summary>
    /// Get audit trail by journal
    /// Tenant context automatically applied by base class
    /// </summary>
    public async Task<IEnumerable<JournalAudit>> GetByJournalIdAsync(Guid journalId)
    {
        return await FindAsync(a => a.JournalEntryId == journalId);
    }
}
