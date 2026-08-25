using Fitness.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Events;

namespace Fitness.Infrastructure.Events;

/// <summary>
/// Stands the Fitness app up when a company is created, the same way every other module does.
///
/// Failures are logged and swallowed, not rethrown: a problem seeding a demo club must never stop
/// a company from being created. The app is still installable and the manager can build their own
/// club from the Setup screens.
/// </summary>
public class FitnessCompanyCreatedEventHandler(
    FitnessInitializationService initService,
    ILogger<FitnessCompanyCreatedEventHandler> logger) : IEventHandler<CompanyCreatedEvent>
{
    public async Task HandleAsync(CompanyCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "Handling CompanyCreatedEvent for Fitness module — Company: {CompanyId} ({CompanyName})",
                domainEvent.CompanyId, domainEvent.CompanyName);

            var initialised = await initService.InitializeForNewCompanyAsync(
                domainEvent.CompanyId,
                domainEvent.BranchId,
                domainEvent.BusinessUnitId,
                domainEvent.CreatedByUserId,
                domainEvent.IncludeSampleData);

            if (initialised)
                logger.LogInformation("Fitness club, plans and timetable initialised for company {CompanyId}",
                    domainEvent.CompanyId);
            else
                logger.LogInformation("Fitness data already exists for company {CompanyId}", domainEvent.CompanyId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initialising Fitness data for company {CompanyId}", domainEvent.CompanyId);
        }
    }
}
