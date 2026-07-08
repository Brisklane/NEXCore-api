using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface ICostCenterRepository : IRepository<CostCenter>
{
    Task<IEnumerable<CostCenter>> GetAllByTenantAsync();
}
