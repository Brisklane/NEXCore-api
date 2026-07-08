using Manufacturing.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Manufacturing.Infrastructure.Repositories.Interfaces;

public interface IProductionOrderRepository : IRepository<ProductionOrder>
{
    Task<ProductionOrder?> GetByOrderNumberAsync(string orderNumber);
    Task<IEnumerable<ProductionOrder>> GetByProductAsync(Guid productId);
    Task<IEnumerable<ProductionOrder>> GetByStatusAsync(string status);
}

public interface IProductionOrderOperationRepository : IRepository<ProductionOrderOperation>
{
    Task<IEnumerable<ProductionOrderOperation>> GetByProductionOrderAsync(Guid productionOrderId);
}

public interface IProductionOrderComponentRepository : IRepository<ProductionOrderComponent>
{
    Task<IEnumerable<ProductionOrderComponent>> GetByProductionOrderAsync(Guid productionOrderId);
}
