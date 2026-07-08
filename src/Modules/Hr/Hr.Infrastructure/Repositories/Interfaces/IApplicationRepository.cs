using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface IApplicationRepository : IRepository<Hr.Domain.Entities.Application>
{
    Task<IEnumerable<Hr.Domain.Entities.Application>> GetAllByTenantAsync();
    Task<IEnumerable<Hr.Domain.Entities.Application>> GetByJobIdAsync(Guid jobId);
    Task<IEnumerable<Hr.Domain.Entities.Application>> GetByCandidateIdAsync(Guid candidateId);
}
