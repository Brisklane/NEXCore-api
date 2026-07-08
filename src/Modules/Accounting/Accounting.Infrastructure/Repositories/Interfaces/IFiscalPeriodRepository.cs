using Accounting.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Accounting.Infrastructure.Repositories.Interfaces;

/// <summary>
/// FiscalPeriod repository interface extending generic repository
/// Tenant context (CompanyId, BranchId, BusinessUnitId) is automatically extracted and applied in TenantAwareRepository
/// </summary>
public interface IFiscalPeriodRepository : IRepository<FiscalPeriod>
{
    /// <summary>
    /// Get periods by calendar
    /// Tenant filtering is automatic
    /// </summary>
    Task<IEnumerable<FiscalPeriod>> GetByCalendarIdAsync(Guid calendarId);

    /// <summary>
    /// Get current period
    /// Tenant filtering is automatic
    /// </summary>
    Task<FiscalPeriod?> GetCurrentPeriodAsync(Guid calendarId);
}
