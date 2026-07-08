using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface ICostCenterService
{
    Task<IEnumerable<CostCenterDto>> GetAllAsync();
    Task<CostCenterDto?> GetByIdAsync(Guid id);
    Task<CostCenterDto> CreateAsync(CreateCostCenterDto request, Guid userId);
    Task<CostCenterDto> UpdateAsync(Guid id, UpdateCostCenterDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
