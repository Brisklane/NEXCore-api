using Manufacturing.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Manufacturing.Application.Services.Interfaces;

public interface IReworkOrderService
{
    Task<ReworkOrderDto> CreateAsync(CreateReworkOrderDto request, Guid userId);
    Task<ReworkOrderDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<ReworkOrderDto>> GetByProductionOrderAsync(Guid productionOrderId, PaginationParams pagination);
    Task<PaginatedResponse<ReworkOrderDto>> GetAllAsync(PaginationParams pagination);
    Task<ReworkOrderDto> UpdateAsync(Guid id, UpdateReworkOrderDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}

public interface IMaterialPlanningDataService
{
    Task<MaterialPlanningDataDto> CreateAsync(CreateMaterialPlanningDataDto request, Guid userId);
    Task<MaterialPlanningDataDto?> GetByIdAsync(Guid id);
    Task<MaterialPlanningDataDto?> GetByProductAsync(Guid productId);
    Task<PaginatedResponse<MaterialPlanningDataDto>> GetAllAsync(PaginationParams pagination);
    Task<MaterialPlanningDataDto> UpdateAsync(Guid id, UpdateMaterialPlanningDataDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}

public interface IStandardCostService
{
    Task<StandardCostDto> CreateAsync(CreateStandardCostDto request, Guid userId);
    Task<StandardCostDto?> GetByIdAsync(Guid id);
    Task<StandardCostDto?> GetActiveByProductAsync(Guid productId);
    Task<PaginatedResponse<StandardCostDto>> GetByProductAsync(Guid productId, PaginationParams pagination);
    Task<PaginatedResponse<StandardCostDto>> GetAllAsync(PaginationParams pagination);
    Task<StandardCostDto> UpdateAsync(Guid id, UpdateStandardCostDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}

public interface IOverheadRuleService
{
    Task<OverheadRuleDto> CreateAsync(CreateOverheadRuleDto request, Guid userId);
    Task<OverheadRuleDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<OverheadRuleDto>> GetAllAsync(PaginationParams pagination);
    Task<PaginatedResponse<OverheadRuleDto>> GetByWorkCenterAsync(Guid workCenterId, PaginationParams pagination);
    Task<OverheadRuleDto> UpdateAsync(Guid id, UpdateOverheadRuleDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}

public interface ICapacityLoadService
{
    Task<CapacityLoadDto> CreateAsync(CreateCapacityLoadDto request, Guid userId);
    Task<PaginatedResponse<CapacityLoadDto>> GetAllAsync(PaginationParams pagination);
    Task<CapacityLoadDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<CapacityLoadDto>> GetByWorkCenterAsync(Guid workCenterId, PaginationParams pagination);
    Task<PaginatedResponse<CapacityLoadDto>> GetByDateRangeAsync(Guid workCenterId, DateTime from, DateTime to, PaginationParams pagination);
    Task<CapacityLoadDto> UpdateAsync(Guid id, UpdateCapacityLoadDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}

public interface IDemandService
{
    Task<DemandDto> CreateAsync(CreateDemandDto request, Guid userId);
    Task<DemandDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<DemandDto>> GetAllAsync(PaginationParams pagination);
    Task<PaginatedResponse<DemandDto>> GetByProductAsync(Guid productId, PaginationParams pagination);
    Task<PaginatedResponse<DemandDto>> GetOpenAsync(PaginationParams pagination);
    Task<DemandDto> UpdateAsync(Guid id, UpdateDemandDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}

public interface IInventoryTransactionService
{
    Task<InventoryTransactionDto> CreateAsync(CreateInventoryTransactionDto request, Guid userId);
    Task<PaginatedResponse<InventoryTransactionDto>> GetAllAsync(PaginationParams pagination);
    Task<InventoryTransactionDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<InventoryTransactionDto>> GetByProductAsync(Guid productId, PaginationParams pagination);
    Task<PaginatedResponse<InventoryTransactionDto>> GetByReferenceAsync(Guid referenceId, PaginationParams pagination);
    Task<InventoryTransactionDto> UpdateAsync(Guid id, UpdateInventoryTransactionDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
