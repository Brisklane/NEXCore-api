using Manufacturing.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Manufacturing.Infrastructure.Repositories.Interfaces;

public interface IBillOfMaterialRepository : IRepository<BillOfMaterial>
{
    Task<IEnumerable<BillOfMaterial>> GetByProductAsync(Guid productId);
    Task<BillOfMaterial?> GetWithItemsAsync(Guid id);
}

public interface IBOMItemRepository : IRepository<BOMItem>
{
    Task<IEnumerable<BOMItem>> GetByBOMAsync(Guid bomId);
}

public interface IBOMByProductRepository : IRepository<BOMByProduct>
{
    Task<IEnumerable<BOMByProduct>> GetByBOMAsync(Guid bomId);
}
