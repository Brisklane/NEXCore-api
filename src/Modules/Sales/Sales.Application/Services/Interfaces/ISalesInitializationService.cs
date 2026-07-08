using Nexcore.SharedKernel;

namespace Sales.Application.Services.Interfaces;

/// <summary>
/// Seeds all Sales master + transactional demo data for a newly created company.
/// Consumes inventory items (cross-module Guid references) and CRM contacts
/// (cross-module Guid references) that were seeded by their respective modules first.
/// </summary>
public interface ISalesInitializationService
{
    Task<Result> InitializeSalesForNewCompanyAsync(
        Guid companyId,
        Guid branchId,
        Guid businessUnitId,
        Guid userId,
        string? companyName = null,
        bool includeSampleData = false);

    Task<bool> SalesDataExistsAsync(Guid companyId);
}
