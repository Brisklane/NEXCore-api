using Sales.Application.DTOs;

namespace Sales.Application.Services.Interfaces;

/// <summary>
/// Service layer for PriceList business logic.
/// Handles code uniqueness, active filtering, and item retrieval.
/// </summary>
public interface IPriceListService
{
    Task<List<PriceListDto>> GetAllAsync();
    Task<PriceListDto?> GetByIdAsync(Guid id);
    Task<PriceListDto?> GetByCodeAsync(string code);
    Task<List<PriceListDto>> GetActiveAsync();
    Task<PriceListDto> CreateAsync(CreatePriceListDto dto);
    Task<PriceListDto> UpdateAsync(Guid id, UpdatePriceListDto dto);
    Task DeleteAsync(Guid id);
}
