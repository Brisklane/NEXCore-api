using Manufacturing.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Manufacturing.Application.Services.Interfaces;

public interface IProductionVarianceService
{
    Task<ProductionVarianceDto> CreateAsync(CreateProductionVarianceDto request, Guid userId);
    Task<PaginatedResponse<ProductionVarianceDto>> GetAllAsync(PaginationParams pagination);
    Task<ProductionVarianceDto?> GetByIdAsync(Guid id);
    Task<ProductionVarianceDto?> GetByProductionOrderAsync(Guid productionOrderId);
    Task<ProductionVarianceDto> UpdateAsync(Guid id, UpdateProductionVarianceDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
