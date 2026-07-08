using Microsoft.AspNetCore.Http;
using Manufacturing.Domain.Entities;
using Manufacturing.Infrastructure.Persistence;
using Manufacturing.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Manufacturing.Infrastructure.Repositories.Implementations;

public class ProductionOrderRepository : TenantAwareRepository<ProductionOrder>, IProductionOrderRepository
{
    public ProductionOrderRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<ProductionOrder?> GetByOrderNumberAsync(string orderNumber)
        => await FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

    public async Task<IEnumerable<ProductionOrder>> GetByProductAsync(Guid productId)
        => await FindAsync(o => o.ProductId == productId);

    public async Task<IEnumerable<ProductionOrder>> GetByStatusAsync(string status)
        => await FindAsync(o => o.Status == status);
}

public class ProductionOrderOperationRepository : TenantAwareRepository<ProductionOrderOperation>, IProductionOrderOperationRepository
{
    public ProductionOrderOperationRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<ProductionOrderOperation>> GetByProductionOrderAsync(Guid productionOrderId)
        => await FindAsync(o => o.ProductionOrderId == productionOrderId);
}

public class ProductionOrderComponentRepository : TenantAwareRepository<ProductionOrderComponent>, IProductionOrderComponentRepository
{
    public ProductionOrderComponentRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<ProductionOrderComponent>> GetByProductionOrderAsync(Guid productionOrderId)
        => await FindAsync(c => c.ProductionOrderId == productionOrderId);
}
