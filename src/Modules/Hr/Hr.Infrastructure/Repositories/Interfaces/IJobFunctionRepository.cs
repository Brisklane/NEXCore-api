using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface IJobFunctionRepository : IRepository<JobFunction>
{
    Task<IEnumerable<JobFunction>> GetAllByTenantAsync();
}
