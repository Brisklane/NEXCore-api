using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Manufacturing.Domain.Entities;
using Manufacturing.Infrastructure.Persistence;
using Manufacturing.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Manufacturing.Infrastructure.Repositories.Implementations;

public class InspectionRepository : TenantAwareRepository<Inspection>, IInspectionRepository
{
    private readonly ManufacturingDbContext _dbContext;

    public InspectionRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor)
    {
        _dbContext = context;
    }

    public async Task<IEnumerable<Inspection>> GetByProductionOrderAsync(Guid productionOrderId)
        => await FindAsync(i => i.ProductionOrderId == productionOrderId);

    public async Task<Inspection?> GetWithCharacteristicsAsync(Guid id)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await _dbContext.Inspections
            .Include(i => i.Characteristics)
            .AsNoTracking()
            .FirstOrDefaultAsync(i =>
                i.Id == id &&
                i.CompanyId == companyId &&
                i.BranchId == branchId &&
                i.BusinessUnitId == businessUnitId &&
                !i.IsDeleted);
    }
}

public class InspectionCharacteristicRepository : TenantAwareRepository<InspectionCharacteristic>, IInspectionCharacteristicRepository
{
    public InspectionCharacteristicRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<InspectionCharacteristic>> GetByInspectionAsync(Guid inspectionId)
        => await FindAsync(c => c.InspectionId == inspectionId);
}

public class FinishedGoodsReceiptRepository : TenantAwareRepository<FinishedGoodsReceipt>, IFinishedGoodsReceiptRepository
{
    public FinishedGoodsReceiptRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<FinishedGoodsReceipt>> GetByProductionOrderAsync(Guid productionOrderId)
        => await FindAsync(f => f.ProductionOrderId == productionOrderId);
}

public class ProductionBatchRepository : TenantAwareRepository<ProductionBatch>, IProductionBatchRepository
{
    public ProductionBatchRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<ProductionBatch?> GetByBatchNumberAsync(string batchNumber)
        => await FirstOrDefaultAsync(b => b.BatchNumber == batchNumber);

    public async Task<IEnumerable<ProductionBatch>> GetByProductionOrderAsync(Guid productionOrderId)
        => await FindAsync(b => b.ProductionOrderId == productionOrderId);

    public async Task<IEnumerable<ProductionBatch>> GetByProductAsync(Guid productId)
        => await FindAsync(b => b.ProductId == productId);
}

public class CostEntryRepository : TenantAwareRepository<CostEntry>, ICostEntryRepository
{
    public CostEntryRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<CostEntry?> GetByProductionOrderAsync(Guid productionOrderId)
        => await FirstOrDefaultAsync(c => c.ProductionOrderId == productionOrderId);
}

public class ProductionVarianceRepository : TenantAwareRepository<ProductionVariance>, IProductionVarianceRepository
{
    public ProductionVarianceRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<ProductionVariance?> GetByProductionOrderAsync(Guid productionOrderId)
        => await FirstOrDefaultAsync(v => v.ProductionOrderId == productionOrderId);
}

public class MachineDowntimeRepository : TenantAwareRepository<MachineDowntime>, IMachineDowntimeRepository
{
    public MachineDowntimeRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<MachineDowntime>> GetByWorkCenterAsync(Guid workCenterId)
        => await FindAsync(d => d.WorkCenterId == workCenterId);

    public async Task<IEnumerable<MachineDowntime>> GetByProductionOrderAsync(Guid productionOrderId)
        => await FindAsync(d => d.ProductionOrderId == productionOrderId);
}
