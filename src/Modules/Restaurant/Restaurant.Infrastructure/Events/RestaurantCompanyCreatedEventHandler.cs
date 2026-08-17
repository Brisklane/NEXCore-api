using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Events;
using Restaurant.Infrastructure.Services;

namespace Restaurant.Infrastructure.Events;

/// <summary>
/// Stands the Restaurant app up when a company is created, the same way every other module does.
///
/// Failures are logged and swallowed, not rethrown: a problem seeding a demo menu must never
/// stop a company from being created. The app is still installable and the manager can build
/// their own venue from the Setup screens.
/// </summary>
public class RestaurantCompanyCreatedEventHandler(
    RestaurantInitializationService initService,
    ILogger<RestaurantCompanyCreatedEventHandler> logger) : IEventHandler<CompanyCreatedEvent>
{
    public async Task HandleAsync(CompanyCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "Handling CompanyCreatedEvent for Restaurant module — Company: {CompanyId} ({CompanyName})",
                domainEvent.CompanyId, domainEvent.CompanyName);

            var initialised = await initService.InitializeForNewCompanyAsync(
                domainEvent.CompanyId,
                domainEvent.BranchId,
                domainEvent.BusinessUnitId,
                domainEvent.CreatedByUserId,
                domainEvent.IncludeSampleData);

            if (initialised)
                logger.LogInformation("Restaurant venue, menu and kitchen initialised for company {CompanyId}",
                    domainEvent.CompanyId);
            else
                logger.LogInformation("Restaurant data already exists for company {CompanyId}", domainEvent.CompanyId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initialising Restaurant data for company {CompanyId}", domainEvent.CompanyId);
        }
    }
}
