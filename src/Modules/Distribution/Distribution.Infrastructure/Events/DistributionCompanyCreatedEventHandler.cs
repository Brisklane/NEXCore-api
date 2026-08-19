using Distribution.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Events;

namespace Distribution.Infrastructure.Events;

/// <summary>
/// Stands the Distribution app up when a company is created, the same way every other module does.
///
/// Failures are logged and swallowed, not rethrown: a problem seeding a demonstration network
/// must never stop a company from being created. The app is still installable and the business
/// can build its own network from the Setup screens.
/// </summary>
public class DistributionCompanyCreatedEventHandler(
    DistributionInitializationService initService,
    ILogger<DistributionCompanyCreatedEventHandler> logger) : IEventHandler<CompanyCreatedEvent>
{
    public async Task HandleAsync(CompanyCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "Handling CompanyCreatedEvent for Distribution module — Company: {CompanyId} ({CompanyName})",
                domainEvent.CompanyId, domainEvent.CompanyName);

            var initialised = await initService.InitializeForNewCompanyAsync(
                domainEvent.CompanyId,
                domainEvent.BranchId,
                domainEvent.BusinessUnitId,
                domainEvent.CreatedByUserId,
                domainEvent.IncludeSampleData);

            if (initialised)
                logger.LogInformation(
                    "Distribution settings, reason codes and network initialised for company {CompanyId}",
                    domainEvent.CompanyId);
            else
                logger.LogInformation(
                    "Distribution data already exists for company {CompanyId}", domainEvent.CompanyId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initialising Distribution data for company {CompanyId}",
                domainEvent.CompanyId);
        }
    }
}
