using Manufacturing.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Manufacturing.Infrastructure.Repositories.Interfaces;

public interface IWorkCenterRepository : IRepository<WorkCenter>
{
    Task<WorkCenter?> GetByCodeAsync(string code);
    Task<IEnumerable<WorkCenter>> GetAllActiveAsync();
}

public interface IWorkCenterShiftRepository : IRepository<WorkCenterShift>
{
    Task<IEnumerable<WorkCenterShift>> GetByWorkCenterAsync(Guid workCenterId);
}
