using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface IInterviewRepository : IRepository<Interview>
{
    Task<IEnumerable<Interview>> GetAllByTenantAsync();
    Task<IEnumerable<Interview>> GetByApplicationIdAsync(Guid applicationId);
}
