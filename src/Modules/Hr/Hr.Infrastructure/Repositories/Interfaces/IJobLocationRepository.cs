using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface IJobLocationRepository : IRepository<JobLocation>
{
    Task<IEnumerable<JobLocation>> GetAllByTenantAsync();
}
