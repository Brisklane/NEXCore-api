using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IJobTemplateService
{
    Task<IEnumerable<JobTemplateDto>> GetAllAsync();
    Task<JobTemplateDto?> GetByIdAsync(Guid id);
    Task<JobTemplateDto> CreateAsync(CreateJobTemplateDto request, Guid userId);
    Task<JobTemplateDto> UpdateAsync(Guid id, UpdateJobTemplateDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
