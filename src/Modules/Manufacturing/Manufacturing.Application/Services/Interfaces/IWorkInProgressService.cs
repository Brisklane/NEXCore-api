using Manufacturing.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Manufacturing.Application.Services.Interfaces;

public interface IWorkInProgressService
{
    Task<WorkInProgressDto> CreateAsync(CreateWorkInProgressDto request, Guid userId);
    Task<PaginatedResponse<WorkInProgressDto>> GetAllAsync(PaginationParams pagination);
    Task<WorkInProgressDto?> GetByIdAsync(Guid id);
    Task<WorkInProgressDto?> GetByProductionOrderAsync(Guid productionOrderId);
    Task<WorkInProgressDto> UpdateAsync(Guid id, UpdateWorkInProgressDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
