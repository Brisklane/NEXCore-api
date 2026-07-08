using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface IJobTemplateRepository : IRepository<JobTemplate>
{
    Task<IEnumerable<JobTemplate>> GetAllByTenantAsync();
}
