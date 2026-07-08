using Microsoft.AspNetCore.Http;
using Hr.Infrastructure.Persistence;
using Hr.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Implementations;

public class ApplicationRepository : TenantAwareRepository<Hr.Domain.Entities.Application>, IApplicationRepository
{
    public ApplicationRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<Hr.Domain.Entities.Application>> GetAllByTenantAsync() => await GetAllAsync();

    public async Task<IEnumerable<Hr.Domain.Entities.Application>> GetByJobIdAsync(Guid jobId)
        => await FindAsync(a => a.JobId == jobId);

    public async Task<IEnumerable<Hr.Domain.Entities.Application>> GetByCandidateIdAsync(Guid candidateId)
        => await FindAsync(a => a.CandidateId == candidateId);
}
