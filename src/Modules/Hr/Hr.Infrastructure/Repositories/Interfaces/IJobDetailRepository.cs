using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface IJobDetailRepository : IRepository<JobDetail>
{
    Task<IEnumerable<JobDetail>> GetAllByTenantAsync();
    Task<JobDetail?> GetByJobIdAsync(Guid jobId);
}
