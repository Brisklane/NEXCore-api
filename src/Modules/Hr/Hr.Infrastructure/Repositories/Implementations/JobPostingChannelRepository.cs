using Microsoft.AspNetCore.Http;
using Hr.Domain.Entities;
using Hr.Infrastructure.Persistence;
using Hr.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Implementations;

public class JobPostingChannelRepository : TenantAwareRepository<JobPostingChannel>, IJobPostingChannelRepository
{
    public JobPostingChannelRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<JobPostingChannel>> GetAllByTenantAsync() => await GetAllAsync();

    public async Task<IEnumerable<JobPostingChannel>> GetByJobIdAsync(Guid jobId)
        => await FindAsync(j => j.JobId == jobId);
}
