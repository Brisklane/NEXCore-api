using Manufacturing.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Manufacturing.Application.Services.Interfaces;

public interface ICostEntryService
{
    Task<CostEntryDto> CreateAsync(CreateCostEntryDto request, Guid userId);
    Task<PaginatedResponse<CostEntryDto>> GetAllAsync(PaginationParams pagination);
    Task<CostEntryDto?> GetByIdAsync(Guid id);
    Task<CostEntryDto?> GetByProductionOrderAsync(Guid productionOrderId);
    Task<CostEntryDto> UpdateAsync(Guid id, UpdateCostEntryDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
