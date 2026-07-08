using Manufacturing.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Manufacturing.Infrastructure.Repositories.Interfaces;

public interface IInspectionRepository : IRepository<Inspection>
{
    Task<IEnumerable<Inspection>> GetByProductionOrderAsync(Guid productionOrderId);
    Task<Inspection?> GetWithCharacteristicsAsync(Guid id);
}

public interface IInspectionCharacteristicRepository : IRepository<InspectionCharacteristic>
{
    Task<IEnumerable<InspectionCharacteristic>> GetByInspectionAsync(Guid inspectionId);
}

public interface IFinishedGoodsReceiptRepository : IRepository<FinishedGoodsReceipt>
{
    Task<IEnumerable<FinishedGoodsReceipt>> GetByProductionOrderAsync(Guid productionOrderId);
}

public interface IProductionBatchRepository : IRepository<ProductionBatch>
{
    Task<ProductionBatch?> GetByBatchNumberAsync(string batchNumber);
    Task<IEnumerable<ProductionBatch>> GetByProductionOrderAsync(Guid productionOrderId);
    Task<IEnumerable<ProductionBatch>> GetByProductAsync(Guid productId);
}

public interface ICostEntryRepository : IRepository<CostEntry>
{
    Task<CostEntry?> GetByProductionOrderAsync(Guid productionOrderId);
}

public interface IProductionVarianceRepository : IRepository<ProductionVariance>
{
    Task<ProductionVariance?> GetByProductionOrderAsync(Guid productionOrderId);
}

public interface IMachineDowntimeRepository : IRepository<MachineDowntime>
{
    Task<IEnumerable<MachineDowntime>> GetByWorkCenterAsync(Guid workCenterId);
    Task<IEnumerable<MachineDowntime>> GetByProductionOrderAsync(Guid productionOrderId);
}
