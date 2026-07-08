using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Manufacturing.Domain.Entities;
using Manufacturing.Infrastructure.Persistence;
using Manufacturing.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Manufacturing.Infrastructure.Repositories.Implementations;

public class MaterialIssueRepository : TenantAwareRepository<MaterialIssue>, IMaterialIssueRepository
{
    public MaterialIssueRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<MaterialIssue>> GetByProductionOrderAsync(Guid productionOrderId)
        => await FindAsync(m => m.ProductionOrderId == productionOrderId);
}

public class WorkInProgressRepository : TenantAwareRepository<WorkInProgress>, IWorkInProgressRepository
{
    private readonly ManufacturingDbContext _manufacturingDbContext;

    public WorkInProgressRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) 
    {
        _manufacturingDbContext = context;
    }

    public async Task<WorkInProgress?> GetByProductionOrderAsync(Guid productionOrderId)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();

        return await _manufacturingDbContext.WorkInProgress
            .Include(w => w.ProductionOrder)
            .AsNoTracking()
            .FirstOrDefaultAsync(w => 
                w.ProductionOrderId == productionOrderId &&
                w.CompanyId == companyId && 
                w.BranchId == branchId &&
                w.BusinessUnitId == businessUnitId &&
                !w.IsDeleted);
    }

    public override async Task<IEnumerable<WorkInProgress>> GetAllAsync()
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();

        return await _manufacturingDbContext.WorkInProgress
            .Include(w => w.ProductionOrder)
            .AsNoTracking()
            .Where(w => 
                w.CompanyId == companyId && 
                w.BranchId == branchId &&
                w.BusinessUnitId == businessUnitId &&
                !w.IsDeleted)
            .ToListAsync();
    }

    public override async Task<WorkInProgress?> GetByIdAsync(Guid id)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();

        return await _manufacturingDbContext.WorkInProgress
            .Include(w => w.ProductionOrder)
            .AsNoTracking()
            .FirstOrDefaultAsync(w => 
                w.Id == id && 
                w.CompanyId == companyId && 
                w.BranchId == branchId &&
                w.BusinessUnitId == businessUnitId &&
                !w.IsDeleted);
    }
}

public class SubContractOrderRepository : TenantAwareRepository<SubContractOrder>, ISubContractOrderRepository
{
    public SubContractOrderRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<SubContractOrder>> GetByProductionOrderAsync(Guid productionOrderId)
        => await FindAsync(s => s.ProductionOrderId == productionOrderId);
}

public class ProductionScheduleRepository : TenantAwareRepository<ProductionSchedule>, IProductionScheduleRepository
{
    public ProductionScheduleRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<ProductionSchedule>> GetByProductionOrderAsync(Guid productionOrderId)
        => await FindAsync(s => s.ProductionOrderId == productionOrderId);

    public async Task<IEnumerable<ProductionSchedule>> GetByWorkCenterAsync(Guid workCenterId)
        => await FindAsync(s => s.WorkCenterId == workCenterId);
}
