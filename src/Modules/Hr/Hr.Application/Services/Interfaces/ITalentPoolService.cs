using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface ITalentPoolService
{
    Task<IEnumerable<TalentPoolDto>> GetAllAsync();
    Task<TalentPoolDto?> GetByIdAsync(Guid id);
    Task<TalentPoolDto> CreateAsync(CreateTalentPoolDto request, Guid userId);
    Task<TalentPoolDto> UpdateAsync(Guid id, UpdateTalentPoolDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
