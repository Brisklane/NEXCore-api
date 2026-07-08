using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Manufacturing.Domain.Entities;
using Manufacturing.Infrastructure.Persistence;
using Manufacturing.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Manufacturing.Infrastructure.Repositories.Implementations;

public class WorkCenterRepository : TenantAwareRepository<WorkCenter>, IWorkCenterRepository
{
    public WorkCenterRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<WorkCenter?> GetByCodeAsync(string code)
        => await FirstOrDefaultAsync(w => w.Code == code);

    public async Task<IEnumerable<WorkCenter>> GetAllActiveAsync()
        => await FindAsync(w => w.IsActive);
}

public class WorkCenterShiftRepository : TenantAwareRepository<WorkCenterShift>, IWorkCenterShiftRepository
{
    public WorkCenterShiftRepository(ManufacturingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<WorkCenterShift>> GetByWorkCenterAsync(Guid workCenterId)
        => await FindAsync(s => s.WorkCenterId == workCenterId);
}
