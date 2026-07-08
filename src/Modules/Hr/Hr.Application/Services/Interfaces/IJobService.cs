using Hr.Application.DTOs;
using Nexcore.SharedKernel.Enums;

namespace Hr.Application.Services.Interfaces;

public interface IJobService
{
    Task<IEnumerable<JobDto>> GetAllAsync(JobRecordType? recordType = null);
    Task<JobDto?> GetByIdAsync(Guid id);
    Task<JobDto> CreateAsync(CreateJobDto request, Guid userId);
    Task<JobDto> UpdateAsync(Guid id, UpdateJobDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
