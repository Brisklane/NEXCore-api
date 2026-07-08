using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface IPayScaleRepository : IRepository<PayScale>
{
    Task<IEnumerable<PayScale>> GetAllByTenantAsync();
}
