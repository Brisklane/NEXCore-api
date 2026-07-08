using Manufacturing.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Manufacturing.Application.Services.Interfaces;

public interface ISubContractOrderService
{
    Task<SubContractOrderDto> CreateAsync(CreateSubContractOrderDto request, Guid userId);
    Task<PaginatedResponse<SubContractOrderDto>> GetAllAsync(PaginationParams pagination);
    Task<SubContractOrderDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<SubContractOrderDto>> GetByProductionOrderAsync(Guid productionOrderId, PaginationParams pagination);
    Task<SubContractOrderDto> UpdateAsync(Guid id, UpdateSubContractOrderDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
