using Microsoft.AspNetCore.Http;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Accounting.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Accounting.Infrastructure.Repositories.Implementations;

/// <summary>
/// FiscalPeriod repository implementation using TenantAwareRepository
/// Automatically filters all queries by CompanyId, BranchId, BusinessUnitId from JWT context
/// </summary>
public class FiscalPeriodRepository : TenantAwareRepository<FiscalPeriod>, IFiscalPeriodRepository
{
    public FiscalPeriodRepository(AccountingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    /// <summary>
    /// Get periods by calendar
    /// Tenant context automatically applied by base class
    /// </summary>
    public async Task<IEnumerable<FiscalPeriod>> GetByCalendarIdAsync(Guid calendarId)
    {
        return await FindAsync(p => p.FiscalCalendarId == calendarId);
    }

    /// <summary>
    /// Get current period
    /// Tenant context automatically applied by base class
    /// </summary>
    public async Task<FiscalPeriod?> GetCurrentPeriodAsync(Guid calendarId)
    {
        var today = DateTime.UtcNow.Date;
        return await FirstOrDefaultAsync(p => p.FiscalCalendarId == calendarId 
            && p.StartDate <= today && today <= p.EndDate 
            && !p.IsClosed);
    }
}
