using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface IJobFamilyRepository : IRepository<JobFamily>
{
    Task<IEnumerable<JobFamily>> GetAllByTenantAsync();
}
