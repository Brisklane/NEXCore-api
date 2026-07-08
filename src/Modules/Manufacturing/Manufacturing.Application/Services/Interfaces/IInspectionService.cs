using Manufacturing.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Manufacturing.Application.Services.Interfaces;

public interface IInspectionService
{
    Task<InspectionDto> CreateAsync(CreateInspectionDto request, Guid userId);
    Task<PaginatedResponse<InspectionDto>> GetAllAsync(PaginationParams pagination);
    Task<InspectionDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<InspectionDto>> GetByProductionOrderAsync(Guid productionOrderId, PaginationParams pagination);
    Task<InspectionDto> UpdateAsync(Guid id, UpdateInspectionDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);

    // Characteristics
    Task<InspectionCharacteristicDto> AddCharacteristicAsync(Guid inspectionId, CreateInspectionCharacteristicDto request, Guid userId);
    Task<InspectionCharacteristicDto> UpdateCharacteristicAsync(Guid characteristicId, UpdateInspectionCharacteristicDto request, Guid userId);
    Task DeleteCharacteristicAsync(Guid characteristicId, Guid userId);
}
