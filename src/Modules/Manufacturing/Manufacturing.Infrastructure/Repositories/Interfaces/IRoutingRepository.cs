using Manufacturing.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Manufacturing.Infrastructure.Repositories.Interfaces;

public interface IRoutingRepository : IRepository<Routing>
{
    Task<IEnumerable<Routing>> GetByProductAsync(Guid productId);
    Task<Routing?> GetWithOperationsAsync(Guid id);
}

public interface IRoutingOperationRepository : IRepository<RoutingOperation>
{
    Task<IEnumerable<RoutingOperation>> GetByRoutingAsync(Guid routingId);
}
