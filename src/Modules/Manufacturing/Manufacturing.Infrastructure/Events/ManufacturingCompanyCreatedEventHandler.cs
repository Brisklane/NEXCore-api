using Nexcore.SharedKernel.Events;
using Manufacturing.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace Manufacturing.Infrastructure.Events;

/// <summary>
/// Handles CompanyCreatedEvent by initializing Manufacturing master data for the new company.
/// Follows the same pattern as Accounting and Inventory modules.
/// </summary>
public class ManufacturingCompanyCreatedEventHandler : IEventHandler<CompanyCreatedEvent>
{
    private readonly ManufacturingInitializationService _initService;
    private readonly ManufacturingSeedDataService _seedService;
    private readonly ILogger<ManufacturingCompanyCreatedEventHandler> _logger;

    public ManufacturingCompanyCreatedEventHandler(
        ManufacturingInitializationService initService,
        ManufacturingSeedDataService seedService,
        ILogger<ManufacturingCompanyCreatedEventHandler> logger)
    {
        _initService = initService;
        _seedService = seedService;
        _logger = logger;
    }

    public async Task HandleAsync(CompanyCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Handling CompanyCreatedEvent for Manufacturing module - Company: {CompanyId} ({CompanyName})",
                domainEvent.CompanyId, domainEvent.CompanyName);

            var initialized = await _initService.InitializeForNewCompanyAsync(
                domainEvent.CompanyId,
                domainEvent.BranchId,
                domainEvent.BusinessUnitId,
                domainEvent.CreatedByUserId);

            if (initialized)
                _logger.LogInformation("Manufacturing master data (WorkCenters, OverheadRules) initialized for Company: {CompanyId}", domainEvent.CompanyId);
            else
                _logger.LogWarning("Manufacturing master data already exists for Company: {CompanyId}", domainEvent.CompanyId);

            var seeded = await _seedService.SeedAsync(
                domainEvent.CompanyId,
                domainEvent.BranchId,
                domainEvent.BusinessUnitId,
                domainEvent.CreatedByUserId);

            if (seeded)
                _logger.LogInformation("Manufacturing sample data seeded for Company: {CompanyId}", domainEvent.CompanyId);
            else
                _logger.LogInformation("Manufacturing sample data already exists for Company: {CompanyId}", domainEvent.CompanyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Manufacturing data for Company: {CompanyId}", domainEvent.CompanyId);
            // Non-blocking - do not rethrow
        }
    }
}
