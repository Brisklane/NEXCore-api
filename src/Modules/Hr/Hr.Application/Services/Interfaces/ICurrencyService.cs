using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface ICurrencyService
{
    Task<IEnumerable<CurrencyDto>> GetAllAsync();
    Task<CurrencyDto?> GetByIdAsync(Guid id);
    Task<CurrencyDto> CreateAsync(CreateCurrencyDto request, Guid userId);
    Task<CurrencyDto> UpdateAsync(Guid id, UpdateCurrencyDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
