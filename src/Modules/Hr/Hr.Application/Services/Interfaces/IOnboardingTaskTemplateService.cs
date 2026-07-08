using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IOnboardingTaskTemplateService
{
    Task<IEnumerable<OnboardingTaskTemplateDto>> GetAllAsync();
    Task<OnboardingTaskTemplateDto?> GetByIdAsync(Guid id);
    Task<OnboardingTaskTemplateDto> CreateAsync(CreateOnboardingTaskTemplateDto request, Guid userId);
    Task<OnboardingTaskTemplateDto> UpdateAsync(Guid id, UpdateOnboardingTaskTemplateDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
