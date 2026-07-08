using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IJobFamilyService
{
    Task<IEnumerable<JobFamilyDto>> GetAllAsync();
    Task<JobFamilyDto?> GetByIdAsync(Guid id);
    Task<JobFamilyDto> CreateAsync(CreateJobFamilyDto request, Guid userId);
    Task<JobFamilyDto> UpdateAsync(Guid id, UpdateJobFamilyDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
