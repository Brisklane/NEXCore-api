using Microsoft.AspNetCore.Http;
using Manufacturing.Domain.Entities;
using Manufacturing.Infrastructure.Persistence;
using Manufacturing.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Manufacturing.Infrastructure.Repositories.Implementations;

public class ReworkOrderRepository : TenantAwareRepository<ReworkOrder>, IReworkOrderRepository
{
    public ReworkOrderRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<ReworkOrder>> GetByProductionOrderAsync(Guid productionOrderId)
        => await FindAsync(r => r.ProductionOrderId == productionOrderId);
}

public class MaterialPlanningDataRepository : TenantAwareRepository<MaterialPlanningData>, IMaterialPlanningDataRepository
{
    public MaterialPlanningDataRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<MaterialPlanningData?> GetByProductAsync(Guid productId)
        => await FirstOrDefaultAsync(m => m.ProductId == productId && m.IsActive);
}

public class StandardCostRepository : TenantAwareRepository<StandardCost>, IStandardCostRepository
{
    public StandardCostRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<StandardCost?> GetActiveByProductAsync(Guid productId)
        => await FirstOrDefaultAsync(s => s.ProductId == productId && s.IsActive);

    public async Task<IEnumerable<StandardCost>> GetByProductAsync(Guid productId)
        => await FindAsync(s => s.ProductId == productId);
}

public class OverheadRuleRepository : TenantAwareRepository<OverheadRule>, IOverheadRuleRepository
{
    public OverheadRuleRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<OverheadRule>> GetByWorkCenterAsync(Guid workCenterId)
        => await FindAsync(r => r.WorkCenterId == workCenterId);

    public async Task<IEnumerable<OverheadRule>> GetGlobalRulesAsync()
        => await FindAsync(r => r.WorkCenterId == null && r.IsActive);
}

public class CapacityLoadRepository : TenantAwareRepository<CapacityLoad>, ICapacityLoadRepository
{
    public CapacityLoadRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<CapacityLoad>> GetByWorkCenterAsync(Guid workCenterId)
        => await FindAsync(c => c.WorkCenterId == workCenterId);

    public async Task<IEnumerable<CapacityLoad>> GetByDateRangeAsync(Guid workCenterId, DateTime from, DateTime to)
        => await FindAsync(c => c.WorkCenterId == workCenterId && c.Date >= from && c.Date <= to);
}

public class DemandRepository : TenantAwareRepository<Demand>, IDemandRepository
{
    public DemandRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<Demand>> GetByProductAsync(Guid productId)
        => await FindAsync(d => d.ProductId == productId);

    public async Task<IEnumerable<Demand>> GetByStatusAsync(string status)
        => await FindAsync(d => d.Status == status);
}

public class InventoryTransactionRepository : TenantAwareRepository<InventoryTransaction>, IInventoryTransactionRepository
{
    public InventoryTransactionRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<InventoryTransaction>> GetByProductAsync(Guid productId)
        => await FindAsync(t => t.ProductId == productId);

    public async Task<IEnumerable<InventoryTransaction>> GetByReferenceAsync(Guid referenceId)
        => await FindAsync(t => t.ReferenceId == referenceId);
}
