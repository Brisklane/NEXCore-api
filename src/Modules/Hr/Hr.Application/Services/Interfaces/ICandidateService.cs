using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface ICandidateService
{
    Task<IEnumerable<CandidateDto>> GetAllAsync();
    Task<CandidateDto?> GetByIdAsync(Guid id);
    Task<CandidateDto> CreateAsync(CreateCandidateDto request, Guid userId);
    Task<CandidateDto> UpdateAsync(Guid id, UpdateCandidateDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
