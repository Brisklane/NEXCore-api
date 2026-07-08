using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Hr.Domain.Entities;
using Hr.Infrastructure.Persistence;
using Hr.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Implementations;

public class ApprovalRequestRepository : TenantAwareRepository<ApprovalRequest>, IApprovalRequestRepository
{
    private readonly HrDbContext _hrContext;

    public ApprovalRequestRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor)
    {
        _hrContext = context;
    }

    public async Task<IEnumerable<ApprovalRequest>> GetAllByTenantAsync()
        => await GetAllAsync();

    public async Task<ApprovalRequest?> GetByIdWithStepsAsync(Guid id)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();

        return await _hrContext.ApprovalRequests
            .Include(a => a.Steps!.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(a =>
                a.Id == id &&
                !a.IsDeleted &&
                a.CompanyId == companyId &&
                a.BranchId == branchId &&
                a.BusinessUnitId == businessUnitId);
    }

    public async Task<IEnumerable<ApprovalRequest>> GetByEntityAsync(string entityType, Guid entityId)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();

        return await _hrContext.ApprovalRequests
            .Include(a => a.Steps!.Where(s => !s.IsDeleted))
            .Where(a =>
                a.EntityType == entityType &&
                a.EntityId == entityId &&
                !a.IsDeleted &&
                a.CompanyId == companyId &&
                a.BranchId == branchId &&
                a.BusinessUnitId == businessUnitId)
            .OrderByDescending(a => a.RequestedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<ApprovalRequest>> GetPendingForApproverAsync(Guid approverEmployeeId)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();

        // A request is pending for an employee if it has a step that is pending
        // and that step's ApproverEmployeeId matches (or was delegated to them)
        return await _hrContext.ApprovalRequests
            .Include(a => a.Steps!.Where(s => !s.IsDeleted))
            .Where(a =>
                !a.IsDeleted &&
                a.CompanyId == companyId &&
                a.BranchId == branchId &&
                a.BusinessUnitId == businessUnitId &&
                a.Steps!.Any(s =>
                    !s.IsDeleted &&
                    s.StepLevel == a.CurrentLevel &&
                    (s.ApproverEmployeeId == approverEmployeeId ||
                     s.DelegatedToEmployeeId == approverEmployeeId)))
            .OrderByDescending(a => a.RequestedAt)
            .ToListAsync();
    }
}

public class ApprovalRequestStepRepository : TenantAwareRepository<ApprovalRequestStep>, IApprovalRequestStepRepository
{
    private readonly HrDbContext _hrContext;

    public ApprovalRequestStepRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor)
    {
        _hrContext = context;
    }

    public async Task<IEnumerable<ApprovalRequestStep>> GetByApprovalRequestIdAsync(Guid approvalRequestId)
        => await _hrContext.ApprovalRequestSteps
            .Where(s => s.ApprovalRequestId == approvalRequestId && !s.IsDeleted)
            .OrderBy(s => s.StepLevel)
            .ToListAsync();

    public async Task<ApprovalRequestStep?> GetCurrentPendingStepAsync(Guid approvalRequestId)
    {
        var request = await _hrContext.ApprovalRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == approvalRequestId && !a.IsDeleted);

        if (request == null) return null;

        return await _hrContext.ApprovalRequestSteps
            .Where(s =>
                s.ApprovalRequestId == approvalRequestId &&
                s.StepLevel == request.CurrentLevel &&
                !s.IsDeleted)
            .FirstOrDefaultAsync();
    }
}
