using Sales.Application.DTOs;

namespace Sales.Application.Services.Interfaces;

/// <summary>
/// Quota management. Owned by Sales because quota drives commission; CRM consumes it for
/// forecast attainment rather than keeping its own copy.
/// </summary>
public interface ISalesTargetService
{
    Task<SalesTargetDto> CreateAsync(CreateSalesTargetDto dto, Guid userId);
    Task<SalesTargetDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<SalesTargetDto>> GetBySalesRepAsync(Guid salesRepId, int? fiscalYear = null);
    Task<IEnumerable<SalesTargetDto>> GetByTeamAsync(Guid salesTeamId, int? fiscalYear = null);
    Task<IEnumerable<SalesTargetDto>> GetByTerritoryAsync(Guid salesTerritoryId, int? fiscalYear = null);
    Task<SalesTargetDto> UpdateAsync(Guid id, UpdateSalesTargetDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
