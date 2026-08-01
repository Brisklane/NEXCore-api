using Inventory.Application.Services.Interfaces;
using Nexcore.SharedKernel.Events;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Events;

/// <summary>
/// Handles CompanyCreatedEvent by seeding inventory master data
/// (units, categories, warehouses, brands, colors, sizes, attributes, taxes)
/// for the newly created company.
/// Maintains loose coupling � Inventory module subscribes to Core events only.
/// </summary>
public class InventoryCompanyCreatedEventHandler : IEventHandler<CompanyCreatedEvent>
{
    private readonly IInventoryInitializationService _inventoryInitService;
    private readonly ILogger<InventoryCompanyCreatedEventHandler> _logger;

    public InventoryCompanyCreatedEventHandler(
        IInventoryInitializationService inventoryInitService,
        ILogger<InventoryCompanyCreatedEventHandler> logger)
    {
        _inventoryInitService = inventoryInitService;
        _logger               = logger;
    }

    public async Task HandleAsync(CompanyCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Handling CompanyCreatedEvent � initializing inventory for Company:{CompanyId} ({Name})",
                domainEvent.CompanyId, domainEvent.CompanyName);

            var result = await _inventoryInitService.InitializeInventoryForNewCompanyAsync(
                domainEvent.CompanyId,
                domainEvent.BranchId,
                domainEvent.BusinessUnitId,
                domainEvent.CreatedByUserId,
                domainEvent.IncludeSampleData);

            if (result.Success)
                _logger.LogInformation(
                    "Inventory data initialized successfully for Company:{CompanyId}", domainEvent.CompanyId);
            else
                _logger.LogWarning(
                    "Inventory initialization failed for Company:{CompanyId} � {Message}",
                    domainEvent.CompanyId, result.Message);
        }
        catch (Exception ex)
        {
            // Non-blocking � do not rethrow; log and continue
            _logger.LogError(ex,
                "Error in InventoryCompanyCreatedEventHandler for Company:{CompanyId}", domainEvent.CompanyId);
        }
    }
}
