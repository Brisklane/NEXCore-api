using Hr.Domain.Entities;
using Nexcore.SharedKernel.Enums;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface IJobRepository : IRepository<Job>
{
    Task<IEnumerable<Job>> GetAllByTenantAsync();
    Task<IEnumerable<Job>> GetByRecordTypeAsync(JobRecordType recordType);
    Task<IEnumerable<Job>> GetByStatusAsync(Guid statusLookupValueId);
}
