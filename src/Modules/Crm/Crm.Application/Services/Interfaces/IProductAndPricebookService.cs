using Crm.Application.DTOs;

namespace Crm.Application.Services.Interfaces;

public interface IProductService
{
    Task<ProductDto> CreateAsync(CreateProductDto dto, Guid userId);
    Task<ProductDto?> GetByIdAsync(Guid id);
    Task<(IEnumerable<ProductDto> Items, int Total)> GetPagedAsync(int page, int pageSize, string? search = null);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}

public interface IPricebookService
{
    Task<PricebookDto> CreateAsync(CreatePricebookDto dto, Guid userId);
    Task<PricebookDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<PricebookDto>> GetAllAsync();
    Task<PricebookDto> UpdateAsync(Guid id, UpdatePricebookDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);

    // Entries
    Task<PricebookEntryDto> AddEntryAsync(Guid pricebookId, CreatePricebookEntryDto dto, Guid userId);
    Task<IEnumerable<PricebookEntryDto>> GetEntriesAsync(Guid pricebookId);
    Task<PricebookEntryDto> UpdateEntryAsync(Guid pricebookId, Guid entryId, UpdatePricebookEntryDto dto, Guid userId);
    Task RemoveEntryAsync(Guid pricebookId, Guid entryId, Guid userId);
}
