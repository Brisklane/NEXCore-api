using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface IEmployeeRepository : IRepository<Employee>
{
    Task<IEnumerable<Employee>> GetAllByTenantAsync();
    Task<IEnumerable<Employee>> GetByDepartmentAsync(Guid departmentId);
    Task<IEnumerable<Employee>> GetByDesignationAsync(Guid designationId);
    Task<IEnumerable<Employee>> GetDirectReportsAsync(Guid managerId);
}
