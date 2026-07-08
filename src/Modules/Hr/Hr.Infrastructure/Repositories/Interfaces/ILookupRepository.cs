using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface ILookupTypeRepository : IRepository<LookupType>
{
    Task<IEnumerable<LookupType>> GetAllByTenantAsync();
}

public interface ILookupValueRepository : IRepository<LookupValue>
{
    Task<IEnumerable<LookupValue>> GetByTypeIdAsync(Guid lookupTypeId);
}
