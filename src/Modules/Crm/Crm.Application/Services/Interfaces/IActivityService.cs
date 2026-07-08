using Crm.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Crm.Application.Services.Interfaces;

public interface IActivityService
{
    Task<ActivityDto> CreateAsync(CreateActivityDto dto, Guid userId);
    Task<ActivityDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<ActivityDto>> GetPagedAsync(PaginationParams pagination, string? type = null);
    Task<IEnumerable<ActivityDto>> GetByRelatedEntityAsync(Guid relatedToId, string relatedToType);
    Task<ActivityDto> UpdateAsync(Guid id, UpdateActivityDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
