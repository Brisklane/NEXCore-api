using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Api;

namespace Accounting.Application.Services.Interfaces;

/// <summary>
/// Service for initializing accounting data when a new company is created
/// Handles seeding of chart of accounts, categories, ledgers, etc. for new companies
/// Only runs in non-production environments
/// </summary>
public interface IAccountingInitializationService
{
    /// <summary>
    /// Initialize accounting data for a newly created company
    /// Seeds chart of accounts, fiscal calendars, tax codes, and other master data
    /// </summary>
    /// <param name="companyId">The newly created company ID</param>
    /// <param name="branchId">The default branch ID</param>
    /// <param name="businessUnitId">The default business unit ID</param>
    /// <param name="userId">The user creating the company (for audit trail)</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> InitializeAccountingForNewCompanyAsync(
        Guid companyId,
        Guid branchId,
        Guid businessUnitId,
        Guid userId);

    /// <summary>
    /// Check if accounting data already exists for a company
    /// </summary>
    /// <param name="companyId">The company ID to check</param>
    /// <returns>True if accounting data exists, false otherwise</returns>
    Task<bool> AccountingDataExistsAsync(Guid companyId);
}
