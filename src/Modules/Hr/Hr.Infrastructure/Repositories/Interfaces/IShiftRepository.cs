using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface IShiftRepository : IRepository<Shift>
{
    Task<IEnumerable<Shift>> GetAllByTenantAsync();
}
