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

    // ── Lines ─────────────────────────────────────────────────────────────
    // The PriceListItem entity has always existed; these are the first endpoints that
    // let anyone actually populate a list.

    Task<List<PriceListItemDto>> GetItemsAsync(Guid priceListId);
    Task<PriceListItemDto> AddItemAsync(Guid priceListId, CreatePriceListItemDto dto);
    Task<PriceListItemDto> UpdateItemAsync(Guid itemId, UpdatePriceListItemDto dto);
    Task DeleteItemAsync(Guid itemId);
}
