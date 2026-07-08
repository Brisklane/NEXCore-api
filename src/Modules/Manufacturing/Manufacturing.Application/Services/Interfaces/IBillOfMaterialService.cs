using Manufacturing.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Manufacturing.Application.Services.Interfaces;

public interface IBillOfMaterialService
{
    Task<BillOfMaterialDto> CreateAsync(CreateBillOfMaterialDto request, Guid userId);
    Task<BillOfMaterialDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<BillOfMaterialDto>> GetAllAsync(PaginationParams pagination);
    Task<PaginatedResponse<BillOfMaterialDto>> GetByProductAsync(Guid productId, PaginationParams pagination);
    Task<BillOfMaterialDto> UpdateAsync(Guid id, UpdateBillOfMaterialDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);

    // BOM Items
    Task<BOMItemDto> AddItemAsync(Guid bomId, CreateBOMItemDto request, Guid userId);
    Task<BOMItemDto> UpdateItemAsync(Guid itemId, UpdateBOMItemDto request, Guid userId);
    Task DeleteItemAsync(Guid itemId, Guid userId);

    // BOM ByProducts
    Task<BOMByProductDto> AddByProductAsync(Guid bomId, CreateBOMByProductDto request, Guid userId);
    Task<BOMByProductDto> UpdateByProductAsync(Guid byProductId, UpdateBOMByProductDto request, Guid userId);
    Task DeleteByProductAsync(Guid byProductId, Guid userId);
}
