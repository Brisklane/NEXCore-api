using Microsoft.AspNetCore.Http;
using Hr.Domain.Entities;
using Hr.Infrastructure.Persistence;
using Hr.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Implementations;

public class JobDetailRepository : TenantAwareRepository<JobDetail>, IJobDetailRepository
{
    public JobDetailRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<JobDetail>> GetAllByTenantAsync() => await GetAllAsync();

    public async Task<JobDetail?> GetByJobIdAsync(Guid jobId)
        => (await FindAsync(j => j.JobId == jobId)).FirstOrDefault();
}
