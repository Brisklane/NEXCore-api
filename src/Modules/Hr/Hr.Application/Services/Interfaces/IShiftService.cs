using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IShiftService
{
    Task<IEnumerable<ShiftDto>> GetAllAsync();
    Task<ShiftDto?> GetByIdAsync(Guid id);
    Task<ShiftDto> CreateAsync(CreateShiftDto request, Guid userId);
    Task<ShiftDto> UpdateAsync(Guid id, UpdateShiftDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
