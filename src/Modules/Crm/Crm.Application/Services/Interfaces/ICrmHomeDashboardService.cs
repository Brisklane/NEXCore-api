using Crm.Application.DTOs;

namespace Crm.Application.Services.Interfaces;

public interface ICrmHomeDashboardService
{
    /// <summary>
    /// Get aggregated CRM home dashboard data
    /// </summary>
    Task<CrmHomeDashboardDto> GetDashboardAsync();
}
