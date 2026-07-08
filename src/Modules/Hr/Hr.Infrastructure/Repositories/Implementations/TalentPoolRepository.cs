using Microsoft.AspNetCore.Http;
using Hr.Domain.Entities;
using Hr.Infrastructure.Persistence;
using Hr.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Implementations;

public class TalentPoolRepository : TenantAwareRepository<TalentPool>, ITalentPoolRepository
{
    public TalentPoolRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<TalentPool>> GetAllByTenantAsync() => await GetAllAsync();
}
