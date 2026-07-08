using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IJobFunctionService
{
    Task<IEnumerable<JobFunctionDto>> GetAllAsync();
    Task<JobFunctionDto?> GetByIdAsync(Guid id);
    Task<JobFunctionDto> CreateAsync(CreateJobFunctionDto request, Guid userId);
    Task<JobFunctionDto> UpdateAsync(Guid id, UpdateJobFunctionDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
