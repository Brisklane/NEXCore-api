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
    /// When <paramref name="includeSampleData"/> is false, only master/config data is
    /// seeded (units, categories, warehouses, bins, brands, colors, sizes, attributes,
    /// taxes) — the demo item catalog and its stock movements are skipped.
    /// </summary>
    Task<Result> InitializeInventoryForNewCompanyAsync(
        Guid companyId,
        Guid branchId,
        Guid businessUnitId,
        Guid userId,
        bool includeSampleData = false);

    /// <summary>
    /// Returns true if inventory master data already exists for the company.
    /// </summary>
    Task<bool> InventoryDataExistsAsync(Guid companyId);
}
