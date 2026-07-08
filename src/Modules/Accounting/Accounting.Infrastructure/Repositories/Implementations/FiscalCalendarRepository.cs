using Microsoft.AspNetCore.Http;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Accounting.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Accounting.Infrastructure.Repositories.Implementations;

/// <summary>
/// FiscalCalendar repository implementation using TenantAwareRepository
/// Automatically filters all queries by CompanyId, BranchId, BusinessUnitId from JWT context
/// </summary>
public class FiscalCalendarRepository : TenantAwareRepository<FiscalCalendar>, IFiscalCalendarRepository
{
    public FiscalCalendarRepository(AccountingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    /// <summary>
    /// Get active fiscal year
    /// Tenant context automatically applied by base class
    /// </summary>
    public async Task<FiscalCalendar?> GetActiveFiscalYearAsync()
    {
        return await FirstOrDefaultAsync(c => c.IsActive);
    }
}
