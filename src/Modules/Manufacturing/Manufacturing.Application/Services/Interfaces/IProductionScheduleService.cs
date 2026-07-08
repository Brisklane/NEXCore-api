using Manufacturing.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Manufacturing.Application.Services.Interfaces;

public interface IProductionScheduleService
{
    Task<ProductionScheduleDto> CreateAsync(CreateProductionScheduleDto request, Guid userId);
    Task<PaginatedResponse<ProductionScheduleDto>> GetAllAsync(PaginationParams pagination);
    Task<ProductionScheduleDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<ProductionScheduleDto>> GetByProductionOrderAsync(Guid productionOrderId, PaginationParams pagination);
    Task<PaginatedResponse<ProductionScheduleDto>> GetByWorkCenterAsync(Guid workCenterId, PaginationParams pagination);
    Task<ProductionScheduleDto> UpdateAsync(Guid id, UpdateProductionScheduleDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
