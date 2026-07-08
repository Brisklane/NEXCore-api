using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface IApprovalRequestRepository : IRepository<ApprovalRequest>
{
    Task<IEnumerable<ApprovalRequest>> GetAllByTenantAsync();
    Task<ApprovalRequest?> GetByIdWithStepsAsync(Guid id);
    Task<IEnumerable<ApprovalRequest>> GetByEntityAsync(string entityType, Guid entityId);
    Task<IEnumerable<ApprovalRequest>> GetPendingForApproverAsync(Guid approverEmployeeId);
}

public interface IApprovalRequestStepRepository : IRepository<ApprovalRequestStep>
{
    Task<IEnumerable<ApprovalRequestStep>> GetByApprovalRequestIdAsync(Guid approvalRequestId);
    Task<ApprovalRequestStep?> GetCurrentPendingStepAsync(Guid approvalRequestId);
}
