using Microsoft.AspNetCore.Http;
using Hr.Domain.Entities;
using Hr.Infrastructure.Persistence;
using Hr.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Implementations;

public class LookupTypeRepository : TenantAwareRepository<LookupType>, ILookupTypeRepository
{
    public LookupTypeRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<LookupType>> GetAllByTenantAsync() => await GetAllAsync();
}

public class LookupValueRepository : TenantAwareRepository<LookupValue>, ILookupValueRepository
{
    public LookupValueRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<LookupValue>> GetByTypeIdAsync(Guid lookupTypeId)
        => await FindAsync(v => v.LookupTypeId == lookupTypeId && v.IsActive);
}
