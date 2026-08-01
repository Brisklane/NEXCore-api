using Crm.Application.DTOs;

namespace Crm.Application.Services.Interfaces;

public interface IForecastService
{
    Task<ForecastDto> CreateAsync(CreateForecastDto dto, Guid userId);
    Task<ForecastDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<ForecastDto>> GetByUserAsync(Guid userId, int fiscalYear, int fiscalQuarter);
    Task<ForecastDto> UpdateAsync(Guid id, UpdateForecastDto dto, Guid userId);
    Task<ForecastDto> SubmitAsync(Guid id, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
