using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Events;
using RealEstate.Infrastructure.Services;

namespace RealEstate.Infrastructure.Events;

/// <summary>
/// Stands the Real Estate app up when a company is created, the same way every other module does.
///
/// Failures are logged and swallowed, not rethrown: a problem seeding a demonstration project must
/// never stop a company from being created. The app is still installable and the manager can build
/// their own scheme from the setup screens.
/// </summary>
public class RealEstateCompanyCreatedEventHandler(
    RealEstateInitializationService initService,
    ILogger<RealEstateCompanyCreatedEventHandler> logger) : IEventHandler<CompanyCreatedEvent>
{
    public async Task HandleAsync(CompanyCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "Handling CompanyCreatedEvent for Real Estate module — Company: {CompanyId} ({CompanyName})",
                domainEvent.CompanyId, domainEvent.CompanyName);

            var initialised = await initService.InitializeForNewCompanyAsync(
                domainEvent.CompanyId,
                domainEvent.BranchId,
                domainEvent.BusinessUnitId,
                domainEvent.CreatedByUserId,
                domainEvent.IncludeSampleData);

            if (initialised)
                logger.LogInformation(
                    "Real Estate offices, reason codes, approval bands and policies initialised for company {CompanyId}",
                    domainEvent.CompanyId);
            else
                logger.LogInformation("Real Estate data already exists for company {CompanyId}", domainEvent.CompanyId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initialising Real Estate data for company {CompanyId}", domainEvent.CompanyId);
        }
    }
}
