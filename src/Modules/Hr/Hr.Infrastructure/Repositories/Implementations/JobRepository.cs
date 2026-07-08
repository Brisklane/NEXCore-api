using Microsoft.AspNetCore.Http;
using Hr.Domain.Entities;
using Hr.Infrastructure.Persistence;
using Hr.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Enums;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Implementations;

public class JobRepository : TenantAwareRepository<Job>, IJobRepository
{
    public JobRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<Job>> GetAllByTenantAsync() => await GetAllAsync();

    public async Task<IEnumerable<Job>> GetByRecordTypeAsync(JobRecordType recordType)
        => await FindAsync(j => j.RecordType == recordType && !j.IsDeleted);

    public async Task<IEnumerable<Job>> GetByStatusAsync(Guid statusLookupValueId)
        => await FindAsync(j => j.StatusLookupValueId == statusLookupValueId);
}
