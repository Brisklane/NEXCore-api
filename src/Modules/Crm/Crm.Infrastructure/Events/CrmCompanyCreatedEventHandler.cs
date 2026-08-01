using Crm.Application.Services.Interfaces;
using Nexcore.SharedKernel.Events;
using Microsoft.Extensions.Logging;

namespace Crm.Infrastructure.Events;

/// <summary>
/// Handles CompanyCreatedEvent by seeding CRM master and sample data
/// (pipelines, products, pricebooks, accounts, contacts, leads, deals,
///  campaigns, cases, knowledge articles, activities, tags, territories, teams)
/// for the newly created company.
/// Maintains loose coupling - CRM module subscribes to Core events only.
/// </summary>
public class CrmCompanyCreatedEventHandler : IEventHandler<CompanyCreatedEvent>
{
    private readonly ICrmInitializationService _initService;
    private readonly ILogger<CrmCompanyCreatedEventHandler> _logger;

    public CrmCompanyCreatedEventHandler(
        ICrmInitializationService initService,
        ILogger<CrmCompanyCreatedEventHandler> logger)
    {
        _initService = initService;
        _logger      = logger;
    }

    public async Task HandleAsync(CompanyCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "CRM: Handling CompanyCreatedEvent for Company:{CompanyId} ({CompanyName})",
                domainEvent.CompanyId, domainEvent.CompanyName);

            var result = await _initService.InitializeCrmForNewCompanyAsync(
                domainEvent.CompanyId,
                domainEvent.BranchId,
                domainEvent.BusinessUnitId,
                domainEvent.CreatedByUserId,
                domainEvent.IncludeSampleData);

            if (result.Success)
                _logger.LogInformation(
                    "CRM: Data initialized successfully for Company:{CompanyId}", domainEvent.CompanyId);
            else
                _logger.LogWarning(
                    "CRM: Initialization returned non-success for Company:{CompanyId} - {Message}",
                    domainEvent.CompanyId, result.Message);
        }
        catch (Exception ex)
        {
            // Non-blocking - never prevent company creation from completing
            _logger.LogError(ex,
                "CRM: Error in CrmCompanyCreatedEventHandler for Company:{CompanyId}", domainEvent.CompanyId);
        }
    }
}
