using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IPositionService
{
    Task<IEnumerable<PositionDto>> GetAllAsync();
    Task<PositionDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<PositionDto>> GetVacantAsync();
    Task<PositionDto> CreateAsync(CreatePositionDto request, Guid userId);
    Task<PositionDto> UpdateAsync(Guid id, UpdatePositionDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
