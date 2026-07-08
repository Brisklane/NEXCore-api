using Manufacturing.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Manufacturing.Application.Services.Interfaces;

public interface IMaterialIssueService
{
    Task<MaterialIssueDto> CreateAsync(CreateMaterialIssueDto request, Guid userId);
    Task<PaginatedResponse<MaterialIssueDto>> GetAllAsync(PaginationParams pagination);
    Task<MaterialIssueDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<MaterialIssueDto>> GetByProductionOrderAsync(Guid productionOrderId, PaginationParams pagination);
    Task<MaterialIssueDto> UpdateAsync(Guid id, UpdateMaterialIssueDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
