using Nexcore.SharedKernel;

namespace Inventory.Application.Services.Interfaces;

/// <summary>
/// Service for initializing inventory master data when a new company is created.
/// Seeds units of measure, item categories, warehouses, bins, brands, colors,
/// sizes, attribute definitions, and tax definitions.
/// </summary>
public interface IInventoryInitializationService
{
    /// <summary>
    /// Initialize inventory master data for a newly created company.
    /// </summary>
    Task<Result> InitializeInventoryForNewCompanyAsync(
        Guid companyId,
        Guid branchId,
        Guid businessUnitId,
        Guid userId);

    /// <summary>
    /// Returns true if inventory master data already exists for the company.
    /// </summary>
    Task<bool> InventoryDataExistsAsync(Guid companyId);
}
