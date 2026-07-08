using Manufacturing.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Manufacturing.Infrastructure.Repositories.Interfaces;

public interface IPlannedOrderRepository : IRepository<PlannedOrder>
{
    Task<IEnumerable<PlannedOrder>> GetByProductAsync(Guid productId);
    Task<IEnumerable<PlannedOrder>> GetByStatusAsync(string status);
}
