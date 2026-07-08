using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface IDesignationRepository : IRepository<Designation>
{
    Task<IEnumerable<Designation>> GetAllByTenantAsync();
}
