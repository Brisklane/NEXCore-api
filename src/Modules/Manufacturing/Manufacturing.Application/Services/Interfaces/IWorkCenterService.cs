using Manufacturing.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Manufacturing.Application.Services.Interfaces;

public interface IWorkCenterService
{
    Task<WorkCenterDto> CreateAsync(CreateWorkCenterDto request, Guid userId);
    Task<WorkCenterDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<WorkCenterDto>> GetAllAsync(PaginationParams pagination);
    Task<WorkCenterDto> UpdateAsync(Guid id, UpdateWorkCenterDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);

    // Shifts
    Task<WorkCenterShiftDto> AddShiftAsync(CreateWorkCenterShiftDto request, Guid userId);
    Task<WorkCenterShiftDto?> GetShiftByIdAsync(Guid shiftId);
    Task<PaginatedResponse<WorkCenterShiftDto>> GetShiftsByWorkCenterAsync(Guid workCenterId, PaginationParams pagination);
    Task<WorkCenterShiftDto> UpdateShiftAsync(Guid shiftId, UpdateWorkCenterShiftDto request, Guid userId);
    Task DeleteShiftAsync(Guid shiftId, Guid userId);
}
