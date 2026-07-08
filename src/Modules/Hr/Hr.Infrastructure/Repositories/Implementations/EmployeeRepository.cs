using Microsoft.AspNetCore.Http;
using Hr.Domain.Entities;
using Hr.Infrastructure.Persistence;
using Hr.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Implementations;

public class EmployeeRepository : TenantAwareRepository<Employee>, IEmployeeRepository
{
    public EmployeeRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<Employee>> GetAllByTenantAsync() => await GetAllAsync();

    public async Task<IEnumerable<Employee>> GetByDepartmentAsync(Guid departmentId)
        => await FindAsync(e => e.DepartmentId == departmentId && !e.IsDeleted);

    public async Task<IEnumerable<Employee>> GetByDesignationAsync(Guid designationId)
        => await FindAsync(e => e.DesignationId == designationId && !e.IsDeleted);

    public async Task<IEnumerable<Employee>> GetDirectReportsAsync(Guid managerId)
        => await FindAsync(e => e.ReportingManagerId == managerId && !e.IsDeleted);
}
