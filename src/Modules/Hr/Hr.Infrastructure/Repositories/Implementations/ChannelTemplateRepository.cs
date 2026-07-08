using Microsoft.AspNetCore.Http;
using Hr.Domain.Entities;
using Hr.Infrastructure.Persistence;
using Hr.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Implementations;

public class ChannelTemplateRepository : TenantAwareRepository<ChannelTemplate>, IChannelTemplateRepository
{
    public ChannelTemplateRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<ChannelTemplate>> GetAllByTenantAsync() => await GetAllAsync();
}
