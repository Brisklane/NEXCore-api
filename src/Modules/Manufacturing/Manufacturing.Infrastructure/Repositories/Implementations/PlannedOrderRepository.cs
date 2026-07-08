using Microsoft.AspNetCore.Http;
using Manufacturing.Domain.Entities;
using Manufacturing.Infrastructure.Persistence;
using Manufacturing.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Manufacturing.Infrastructure.Repositories.Implementations;

public class PlannedOrderRepository : TenantAwareRepository<PlannedOrder>, IPlannedOrderRepository
{
    public PlannedOrderRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<PlannedOrder>> GetByProductAsync(Guid productId)
        => await FindAsync(p => p.ProductId == productId);

    public async Task<IEnumerable<PlannedOrder>> GetByStatusAsync(string status)
        => await FindAsync(p => p.Status == status);
}
