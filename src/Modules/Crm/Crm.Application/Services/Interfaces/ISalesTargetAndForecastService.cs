using Crm.Application.DTOs;

namespace Crm.Application.Services.Interfaces;

public interface ISalesTargetService
{
    Task<SalesTargetDto> CreateAsync(CreateSalesTargetDto dto, Guid userId);
    Task<SalesTargetDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<SalesTargetDto>> GetByUserIdAsync(Guid userId, int? fiscalYear = null);
    Task<IEnumerable<SalesTargetDto>> GetByTeamIdAsync(Guid teamId, int? fiscalYear = null);
    Task<SalesTargetDto> UpdateAsync(Guid id, UpdateSalesTargetDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}

public interface IForecastService
{
    Task<ForecastDto> CreateAsync(CreateForecastDto dto, Guid userId);
    Task<ForecastDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<ForecastDto>> GetByUserAsync(Guid userId, int fiscalYear, int fiscalQuarter);
    Task<ForecastDto> UpdateAsync(Guid id, UpdateForecastDto dto, Guid userId);
    Task<ForecastDto> SubmitAsync(Guid id, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
