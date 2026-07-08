using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface IGradeRepository : IRepository<Grade>
{
    Task<IEnumerable<Grade>> GetAllByTenantAsync();
}
