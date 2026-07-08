using Manufacturing.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Manufacturing.Infrastructure.Repositories.Interfaces;

public interface IMaterialIssueRepository : IRepository<MaterialIssue>
{
    Task<IEnumerable<MaterialIssue>> GetByProductionOrderAsync(Guid productionOrderId);
}

public interface IWorkInProgressRepository : IRepository<WorkInProgress>
{
    Task<WorkInProgress?> GetByProductionOrderAsync(Guid productionOrderId);
}

public interface ISubContractOrderRepository : IRepository<SubContractOrder>
{
    Task<IEnumerable<SubContractOrder>> GetByProductionOrderAsync(Guid productionOrderId);
}

public interface IProductionScheduleRepository : IRepository<ProductionSchedule>
{
    Task<IEnumerable<ProductionSchedule>> GetByProductionOrderAsync(Guid productionOrderId);
    Task<IEnumerable<ProductionSchedule>> GetByWorkCenterAsync(Guid workCenterId);
}
