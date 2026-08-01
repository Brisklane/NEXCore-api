using Nexcore.SharedKernel;

namespace Hr.Application.Services.Interfaces;

public interface IHrInitializationService
{
    /// <summary>
    /// When <paramref name="includeSampleData"/> is false, only master/config data is
    /// seeded (lookups, currencies, grades, departments, designations, pay scales, shifts,
    /// skills, workflows, templates, positions) — demo employees, jobs, candidates,
    /// applications, requisitions, interviews and offer letters are skipped.
    /// </summary>
    Task<Result> InitializeHrForNewCompanyAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId,
        bool includeSampleData = false);

    Task<Result> EnsureWorkflowConfigsAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId);

    Task<bool> HrDataExistsAsync(Guid companyId);
}
