using Accounting.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Accounting.Infrastructure.Repositories.Interfaces;

/// <summary>
/// FiscalCalendar repository interface extending generic repository
/// Tenant context (CompanyId, BranchId, BusinessUnitId) is automatically extracted and applied in TenantAwareRepository
/// </summary>
public interface IFiscalCalendarRepository : IRepository<FiscalCalendar>
{
    /// <summary>
    /// Get active fiscal year for current tenant
    /// Tenant filtering is automatic
    /// </summary>
    Task<FiscalCalendar?> GetActiveFiscalYearAsync();
}
