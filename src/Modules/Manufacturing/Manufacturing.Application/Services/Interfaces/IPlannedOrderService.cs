using Manufacturing.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Manufacturing.Application.Services.Interfaces;

public interface IPlannedOrderService
{
    Task<PlannedOrderDto> CreateAsync(CreatePlannedOrderDto request, Guid userId);
    Task<PlannedOrderDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<PlannedOrderDto>> GetAllAsync(PaginationParams pagination);
    Task<PaginatedResponse<PlannedOrderDto>> GetByProductAsync(Guid productId, PaginationParams pagination);
    Task<PlannedOrderDto> UpdateAsync(Guid id, UpdatePlannedOrderDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
