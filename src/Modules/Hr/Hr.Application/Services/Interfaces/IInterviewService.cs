using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IInterviewService
{
    Task<IEnumerable<InterviewDto>> GetAllAsync();
    Task<InterviewDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<InterviewDto>> GetByApplicationIdAsync(Guid applicationId);
    Task<InterviewDto> CreateAsync(CreateInterviewDto request, Guid userId);
    Task<InterviewDto> UpdateAsync(Guid id, UpdateInterviewDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
