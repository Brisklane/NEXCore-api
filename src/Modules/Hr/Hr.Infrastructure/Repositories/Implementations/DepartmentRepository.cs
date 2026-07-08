using Microsoft.AspNetCore.Http;
using Hr.Domain.Entities;
using Hr.Infrastructure.Persistence;
using Hr.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Implementations;

public class DepartmentRepository : TenantAwareRepository<Department>, IDepartmentRepository
{
    public DepartmentRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<Department>> GetAllByTenantAsync() => await GetAllAsync();

    public async Task<IEnumerable<Department>> GetByParentAsync(Guid parentDepartmentId)
        => await FindAsync(d => d.ParentDepartmentId == parentDepartmentId);
}
