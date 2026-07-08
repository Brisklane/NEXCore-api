using Microsoft.AspNetCore.Http;
using Hr.Domain.Entities;
using Hr.Infrastructure.Persistence;
using Hr.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Implementations;

public class PositionRepository : TenantAwareRepository<Position>, IPositionRepository
{
    public PositionRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<Position>> GetAllByTenantAsync() => await GetAllAsync();

    public async Task<IEnumerable<Position>> GetVacantAsync()
        => await FindAsync(p => p.IsVacant && !p.IsDeleted);
}
