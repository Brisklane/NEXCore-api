using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IApplicationService
{
    Task<IEnumerable<ApplicationDto>> GetAllAsync();
    Task<ApplicationDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<ApplicationDto>> GetByJobIdAsync(Guid jobId);
    Task<IEnumerable<ApplicationDto>> GetByCandidateIdAsync(Guid candidateId);
    Task<ApplicationDto> CreateAsync(CreateApplicationDto request, Guid userId);
    Task<ApplicationDto> UpdateAsync(Guid id, UpdateApplicationDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
