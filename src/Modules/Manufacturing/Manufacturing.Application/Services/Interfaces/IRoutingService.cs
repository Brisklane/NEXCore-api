using Manufacturing.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Manufacturing.Application.Services.Interfaces;

public interface IRoutingService
{
    Task<RoutingDto> CreateAsync(CreateRoutingDto request, Guid userId);
    Task<RoutingDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<RoutingDto>> GetAllAsync(PaginationParams pagination);
    Task<PaginatedResponse<RoutingDto>> GetByProductAsync(Guid productId, PaginationParams pagination);
    Task<RoutingDto> UpdateAsync(Guid id, UpdateRoutingDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);

    // Operations
    Task<RoutingOperationDto> AddOperationAsync(Guid routingId, CreateRoutingOperationDto request, Guid userId);
    Task<RoutingOperationDto> UpdateOperationAsync(Guid operationId, UpdateRoutingOperationDto request, Guid userId);
    Task DeleteOperationAsync(Guid operationId, Guid userId);
}
