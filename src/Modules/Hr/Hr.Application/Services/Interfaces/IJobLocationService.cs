using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IJobLocationService
{
    Task<IEnumerable<JobLocationDto>> GetAllAsync();
    Task<JobLocationDto?> GetByIdAsync(Guid id);
    Task<JobLocationDto> CreateAsync(CreateJobLocationDto request, Guid userId);
    Task<JobLocationDto> UpdateAsync(Guid id, UpdateJobLocationDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
