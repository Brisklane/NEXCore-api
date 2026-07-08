using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IOnboardingTaskService
{
    Task<IEnumerable<OnboardingTaskDto>> GetAllAsync();
    Task<OnboardingTaskDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<OnboardingTaskDto>> GetByApplicationIdAsync(Guid applicationId);
    Task<OnboardingTaskDto> CreateAsync(CreateOnboardingTaskDto request, Guid userId);
    Task<OnboardingTaskDto> UpdateAsync(Guid id, UpdateOnboardingTaskDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
