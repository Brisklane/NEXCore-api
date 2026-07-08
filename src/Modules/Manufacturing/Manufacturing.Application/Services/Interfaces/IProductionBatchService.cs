using Manufacturing.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Manufacturing.Application.Services.Interfaces;

public interface IProductionBatchService
{
    Task<ProductionBatchDto> CreateAsync(CreateProductionBatchDto request, Guid userId);
    Task<PaginatedResponse<ProductionBatchDto>> GetAllAsync(PaginationParams pagination);
    Task<ProductionBatchDto?> GetByIdAsync(Guid id);
    Task<ProductionBatchDto?> GetByBatchNumberAsync(string batchNumber);
    Task<PaginatedResponse<ProductionBatchDto>> GetByProductionOrderAsync(Guid productionOrderId, PaginationParams pagination);
    Task<PaginatedResponse<ProductionBatchDto>> GetByProductAsync(Guid productId, PaginationParams pagination);
    Task<ProductionBatchDto> UpdateAsync(Guid id, UpdateProductionBatchDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
