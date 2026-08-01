using Nexcore.SharedKernel;

namespace Crm.Application.Services.Interfaces;

/// <summary>
/// Initializes CRM master and sample data for a newly created company.
/// Called automatically via CompanyCreatedEvent.
/// </summary>
public interface ICrmInitializationService
{
    /// <summary>
    /// When <paramref name="includeSampleData"/> is false, only master/config data is
    /// seeded (pipelines, stages, pricebooks, tags, territories, teams and the anonymous
    /// Walk-in Customer that POS requires) — demo accounts, contacts, leads, deals,
    /// campaigns, cases and activities are skipped.
    /// </summary>
    Task<Result> InitializeCrmForNewCompanyAsync(
        Guid companyId,
        Guid branchId,
        Guid businessUnitId,
        Guid userId,
        bool includeSampleData = false);

    Task<bool> CrmDataExistsAsync(Guid companyId);

    /// <summary>
    /// Ensures every company that has CRM data has exactly one Walk-in Customer contact.
    /// Safe to call on every startup — no-op when the contact already exists.
    /// </summary>
    Task EnsureWalkInCustomersAsync();
}
