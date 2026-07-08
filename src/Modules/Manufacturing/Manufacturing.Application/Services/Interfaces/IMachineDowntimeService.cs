using Manufacturing.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Manufacturing.Application.Services.Interfaces;

public interface IMachineDowntimeService
{
    Task<MachineDowntimeDto> CreateAsync(CreateMachineDowntimeDto request, Guid userId);
    Task<PaginatedResponse<MachineDowntimeDto>> GetAllAsync(PaginationParams pagination);
    Task<MachineDowntimeDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<MachineDowntimeDto>> GetByWorkCenterAsync(Guid workCenterId, PaginationParams pagination);
    Task<PaginatedResponse<MachineDowntimeDto>> GetByProductionOrderAsync(Guid productionOrderId, PaginationParams pagination);
    Task<MachineDowntimeDto> UpdateAsync(Guid id, UpdateMachineDowntimeDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
