using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface ICallLogService
{
    Task<IEnumerable<CallLogDto>> GetAllAsync();
    Task<CallLogDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<CallLogDto>> GetByCandidateIdAsync(Guid candidateId);
    Task<CallLogDto> CreateAsync(CreateCallLogDto request, Guid userId);
    Task<CallLogDto> UpdateAsync(Guid id, UpdateCallLogDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
