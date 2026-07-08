using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface IJobPostingChannelRepository : IRepository<JobPostingChannel>
{
    Task<IEnumerable<JobPostingChannel>> GetAllByTenantAsync();
    Task<IEnumerable<JobPostingChannel>> GetByJobIdAsync(Guid jobId);
}
