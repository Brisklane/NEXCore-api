using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IJobPostingChannelService
{
    Task<IEnumerable<JobPostingChannelDto>> GetAllAsync();
    Task<JobPostingChannelDto?> GetByIdAsync(Guid id);
    Task<JobPostingChannelDto> CreateAsync(CreateJobPostingChannelDto request, Guid userId);
    Task<JobPostingChannelDto> UpdateAsync(Guid id, UpdateJobPostingChannelDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
