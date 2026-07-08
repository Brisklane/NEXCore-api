using Manufacturing.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Manufacturing.Application.Services.Interfaces;

public interface IProductionOrderService
{
    Task<ProductionOrderDto> CreateAsync(CreateProductionOrderDto request, Guid userId);

    /// <summary>
    /// Produce a BOM-backed item in one step: create a Completed production order, backflush the BOM
    /// components from inventory and receive the finished goods. Used by the POS "Produce" action.
    /// </summary>
    Task<ProductionOrderDto> ProduceExpressAsync(
        ProduceExpressDto request, Guid companyId, Guid branchId, Guid businessUnitId, Guid userId);
    Task<ProductionOrderDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<ProductionOrderDto>> GetAllAsync(PaginationParams pagination);
    Task<PaginatedResponse<ProductionOrderDto>> GetByProductAsync(Guid productId, PaginationParams pagination);
    Task<PaginatedResponse<ProductionOrderDto>> GetByStatusAsync(string status, PaginationParams pagination);
    Task<ProductionOrderDto> UpdateAsync(Guid id, UpdateProductionOrderDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);

    // Operations
    Task<ProductionOrderOperationDto> AddOperationAsync(CreateProductionOrderOperationDto request, Guid userId);
    Task<ProductionOrderOperationDto> UpdateOperationAsync(Guid operationId, UpdateProductionOrderOperationDto request, Guid userId);
    Task<PaginatedResponse<ProductionOrderOperationDto>> GetOperationsByOrderAsync(Guid productionOrderId, PaginationParams pagination);

    // Components
    Task<ProductionOrderComponentDto> AddComponentAsync(CreateProductionOrderComponentDto request, Guid userId);
    Task<ProductionOrderComponentDto> UpdateComponentAsync(Guid componentId, UpdateProductionOrderComponentDto request, Guid userId);
    Task<PaginatedResponse<ProductionOrderComponentDto>> GetComponentsByOrderAsync(Guid productionOrderId, PaginationParams pagination);
}
