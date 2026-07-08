using Microsoft.AspNetCore.Http;
using Hr.Domain.Entities;
using Hr.Infrastructure.Persistence;
using Hr.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Implementations;

public class OfferLetterRepository : TenantAwareRepository<OfferLetter>, IOfferLetterRepository
{
    public OfferLetterRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<OfferLetter>> GetAllByTenantAsync() => await GetAllAsync();

    public async Task<IEnumerable<OfferLetter>> GetByApplicationIdAsync(Guid applicationId)
        => await FindAsync(o => o.ApplicationId == applicationId);
}
