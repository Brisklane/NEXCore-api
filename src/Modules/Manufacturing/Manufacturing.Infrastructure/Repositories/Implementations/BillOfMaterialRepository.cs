using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Manufacturing.Domain.Entities;
using Manufacturing.Infrastructure.Persistence;
using Manufacturing.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Manufacturing.Infrastructure.Repositories.Implementations;

public class BillOfMaterialRepository : TenantAwareRepository<BillOfMaterial>, IBillOfMaterialRepository
{
    private readonly ManufacturingDbContext _dbContext;

    public BillOfMaterialRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor)
    {
        _dbContext = context;
    }

    public async Task<IEnumerable<BillOfMaterial>> GetByProductAsync(Guid productId)
        => await FindAsync(b => b.FinishedProductId == productId);

    public async Task<BillOfMaterial?> GetWithItemsAsync(Guid id)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await _dbContext.BillsOfMaterial
            .Include(b => b.Items)
            .Include(b => b.ByProducts)
            .AsNoTracking()
            .FirstOrDefaultAsync(b =>
                b.Id == id &&
                b.CompanyId == companyId &&
                b.BranchId == branchId &&
                b.BusinessUnitId == businessUnitId &&
                !b.IsDeleted);
    }
}

public class BOMItemRepository : TenantAwareRepository<BOMItem>, IBOMItemRepository
{
    public BOMItemRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<BOMItem>> GetByBOMAsync(Guid bomId)
        => await FindAsync(i => i.BillOfMaterialId == bomId);
}

public class BOMByProductRepository : TenantAwareRepository<BOMByProduct>, IBOMByProductRepository
{
    public BOMByProductRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<BOMByProduct>> GetByBOMAsync(Guid bomId)
        => await FindAsync(b => b.BillOfMaterialId == bomId);
}
