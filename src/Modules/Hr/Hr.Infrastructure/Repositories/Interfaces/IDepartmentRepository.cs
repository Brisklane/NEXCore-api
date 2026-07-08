using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface IDepartmentRepository : IRepository<Department>
{
    Task<IEnumerable<Department>> GetAllByTenantAsync();
    Task<IEnumerable<Department>> GetByParentAsync(Guid parentDepartmentId);
}
