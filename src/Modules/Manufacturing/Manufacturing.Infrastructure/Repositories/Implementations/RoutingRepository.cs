using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Manufacturing.Domain.Entities;
using Manufacturing.Infrastructure.Persistence;
using Manufacturing.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Manufacturing.Infrastructure.Repositories.Implementations;

public class RoutingRepository : TenantAwareRepository<Routing>, IRoutingRepository
{
    private readonly ManufacturingDbContext _dbContext;

    public RoutingRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor)
    {
        _dbContext = context;
    }

    public async Task<IEnumerable<Routing>> GetByProductAsync(Guid productId)
        => await FindAsync(r => r.ProductId == productId);

    public async Task<Routing?> GetWithOperationsAsync(Guid id)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await _dbContext.Routings
            .Include(r => r.Operations)
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.Id == id &&
                r.CompanyId == companyId &&
                r.BranchId == branchId &&
                r.BusinessUnitId == businessUnitId &&
                !r.IsDeleted);
    }
}

public class RoutingOperationRepository : TenantAwareRepository<RoutingOperation>, IRoutingOperationRepository
{
    public RoutingOperationRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<RoutingOperation>> GetByRoutingAsync(Guid routingId)
        => await FindAsync(o => o.RoutingId == routingId);
}
