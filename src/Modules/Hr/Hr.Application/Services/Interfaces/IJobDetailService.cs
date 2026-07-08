using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IJobDetailService
{
    Task<IEnumerable<JobDetailDto>> GetAllAsync();
    Task<JobDetailDto?> GetByIdAsync(Guid id);
    Task<JobDetailDto> CreateAsync(CreateJobDetailDto request, Guid userId);
    Task<JobDetailDto> UpdateAsync(Guid id, UpdateJobDetailDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
