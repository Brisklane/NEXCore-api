using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IAllowancesProfileService
{
    Task<IEnumerable<AllowancesProfileDto>> GetAllAsync();
    Task<AllowancesProfileDto?> GetByIdAsync(Guid id);
    Task<AllowancesProfileDto> CreateAsync(CreateAllowancesProfileDto request, Guid userId);
    Task<AllowancesProfileDto> UpdateAsync(Guid id, UpdateAllowancesProfileDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
