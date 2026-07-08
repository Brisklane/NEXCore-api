using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Hr.Domain.Entities;
using Hr.Infrastructure.Persistence;
using Hr.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Implementations;

public class CommunicationTemplateRepository : TenantAwareRepository<CommunicationTemplate>, ICommunicationTemplateRepository
{
    private readonly HrDbContext _dbContext;

    public CommunicationTemplateRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) => _dbContext = context;

    public async Task<IEnumerable<CommunicationTemplate>> GetAllByTenantAsync() => await GetAllAsync();

    public async Task<CommunicationTemplate?> GetByCodeAsync(string templateCode)
        => await _dbContext.CommunicationTemplates
            .FirstOrDefaultAsync(t => t.TemplateCode == templateCode && !t.IsDeleted);
}
