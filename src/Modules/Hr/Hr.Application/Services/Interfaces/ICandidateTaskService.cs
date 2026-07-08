using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface ICandidateTaskService
{
    Task<IEnumerable<CandidateTaskDto>> GetAllAsync();
    Task<CandidateTaskDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<CandidateTaskDto>> GetByApplicationIdAsync(Guid applicationId);
    Task<IEnumerable<CandidateTaskDto>> GetByCandidateIdAsync(Guid candidateId);
    Task<CandidateTaskDto> CreateAsync(CreateCandidateTaskDto request, Guid userId);
    Task<CandidateTaskDto> UpdateAsync(Guid id, UpdateCandidateTaskDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
