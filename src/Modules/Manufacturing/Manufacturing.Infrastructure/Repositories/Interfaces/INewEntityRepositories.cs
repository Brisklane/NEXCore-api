using Manufacturing.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Manufacturing.Infrastructure.Repositories.Interfaces;

public interface IReworkOrderRepository : IRepository<ReworkOrder>
{
    Task<IEnumerable<ReworkOrder>> GetByProductionOrderAsync(Guid productionOrderId);
}

public interface IMaterialPlanningDataRepository : IRepository<MaterialPlanningData>
{
    Task<MaterialPlanningData?> GetByProductAsync(Guid productId);
}

public interface IStandardCostRepository : IRepository<StandardCost>
{
    Task<StandardCost?> GetActiveByProductAsync(Guid productId);
    Task<IEnumerable<StandardCost>> GetByProductAsync(Guid productId);
}

public interface IOverheadRuleRepository : IRepository<OverheadRule>
{
    Task<IEnumerable<OverheadRule>> GetByWorkCenterAsync(Guid workCenterId);
    Task<IEnumerable<OverheadRule>> GetGlobalRulesAsync();
}

public interface ICapacityLoadRepository : IRepository<CapacityLoad>
{
    Task<IEnumerable<CapacityLoad>> GetByWorkCenterAsync(Guid workCenterId);
    Task<IEnumerable<CapacityLoad>> GetByDateRangeAsync(Guid workCenterId, DateTime from, DateTime to);
}

public interface IDemandRepository : IRepository<Demand>
{
    Task<IEnumerable<Demand>> GetByProductAsync(Guid productId);
    Task<IEnumerable<Demand>> GetByStatusAsync(string status);
}

public interface IInventoryTransactionRepository : IRepository<InventoryTransaction>
{
    Task<IEnumerable<InventoryTransaction>> GetByProductAsync(Guid productId);
    Task<IEnumerable<InventoryTransaction>> GetByReferenceAsync(Guid referenceId);
}
