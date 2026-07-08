using Nexcore.SharedKernel;

namespace Crm.Application.Services.Interfaces;

/// <summary>
/// Initializes CRM master and sample data for a newly created company.
/// Called automatically via CompanyCreatedEvent.
/// </summary>
public interface ICrmInitializationService
{
    Task<Result> InitializeCrmForNewCompanyAsync(
        Guid companyId,
        Guid branchId,
        Guid businessUnitId,
        Guid userId);

    Task<bool> CrmDataExistsAsync(Guid companyId);

    /// <summary>
    /// Ensures every company that has CRM data has exactly one Walk-in Customer contact.
    /// Safe to call on every startup — no-op when the contact already exists.
    /// </summary>
    Task EnsureWalkInCustomersAsync();
}
