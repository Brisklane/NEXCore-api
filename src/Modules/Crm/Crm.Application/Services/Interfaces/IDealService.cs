using Crm.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Crm.Application.Services.Interfaces;

public interface IDealService
{
    Task<DealDto> CreateAsync(CreateDealDto dto, Guid userId);
    Task<DealDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<DealDto>> GetPagedAsync(PaginationParams pagination, string? stage = null);
    Task<IEnumerable<DealDto>> GetByAccountIdAsync(Guid accountId);
    Task<DealDto> UpdateAsync(Guid id, UpdateDealDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);

    // Deal Products
    Task<DealProductDto> AddProductAsync(Guid dealId, CreateDealProductDto dto, Guid userId);
    Task<IEnumerable<DealProductDto>> GetProductsAsync(Guid dealId);
    Task<DealProductDto> UpdateProductAsync(Guid dealId, Guid productId, UpdateDealProductDto dto, Guid userId);
    Task RemoveProductAsync(Guid dealId, Guid dealProductId, Guid userId);

    // Deal Contacts (Contact Roles)
    Task<DealContactDto> AddContactAsync(Guid dealId, CreateDealContactDto dto, Guid userId);
    Task<IEnumerable<DealContactDto>> GetContactsAsync(Guid dealId);
    Task RemoveContactAsync(Guid dealId, Guid dealContactId, Guid userId);
}
