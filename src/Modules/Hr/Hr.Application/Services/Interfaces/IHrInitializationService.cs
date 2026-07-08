using Nexcore.SharedKernel;

namespace Hr.Application.Services.Interfaces;

public interface IHrInitializationService
{
    Task<Result> InitializeHrForNewCompanyAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId);

    Task<Result> EnsureWorkflowConfigsAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId);

    Task<bool> HrDataExistsAsync(Guid companyId);
}
