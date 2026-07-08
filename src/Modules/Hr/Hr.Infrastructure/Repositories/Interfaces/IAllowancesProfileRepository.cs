using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface IAllowancesProfileRepository : IRepository<AllowancesProfile>
{
    Task<IEnumerable<AllowancesProfile>> GetAllByTenantAsync();
}
