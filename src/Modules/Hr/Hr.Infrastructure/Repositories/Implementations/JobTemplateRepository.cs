using Microsoft.AspNetCore.Http;
using Hr.Domain.Entities;
using Hr.Infrastructure.Persistence;
using Hr.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Implementations;

public class JobTemplateRepository : TenantAwareRepository<JobTemplate>, IJobTemplateRepository
{
    public JobTemplateRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<JobTemplate>> GetAllByTenantAsync() => await GetAllAsync();
}
