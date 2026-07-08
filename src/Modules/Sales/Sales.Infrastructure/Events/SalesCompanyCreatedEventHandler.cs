using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Events;
using Sales.Application.Services.Interfaces;

namespace Sales.Infrastructure.Events;

/// <summary>
/// Handles CompanyCreatedEvent by seeding comprehensive Sales master + demo data
/// (price lists, POS store/terminals/cashiers, quotations, sales orders, invoices,
///  payments, deliveries, returns, riders, loyalty, coupons, gift cards, reviews -)
/// for the newly created company.
///
/// Execution order: Accounting ? Inventory ? CRM ? HR ? Sales
/// (enforced by DI registration order in each module's ServiceCollectionExtensions).
/// Sales seeder references Inventory items and CRM contacts by their well-known
/// deterministic GUIDs, so all cross-module FK references resolve correctly.
/// </summary>
public class SalesCompanyCreatedEventHandler : IEventHandler<CompanyCreatedEvent>
{
    private readonly ISalesInitializationService _salesInitService;
    private readonly ILogger<SalesCompanyCreatedEventHandler> _logger;

    public SalesCompanyCreatedEventHandler(
        ISalesInitializationService salesInitService,
        ILogger<SalesCompanyCreatedEventHandler> logger)
    {
        _salesInitService = salesInitService;
        _logger           = logger;
    }

    public async Task HandleAsync(CompanyCreatedEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Sales: Handling CompanyCreatedEvent for Company:{CompanyId} ({CompanyName})",
                domainEvent.CompanyId, domainEvent.CompanyName);

            var result = await _salesInitService.InitializeSalesForNewCompanyAsync(
                domainEvent.CompanyId,
                domainEvent.BranchId,
                domainEvent.BusinessUnitId,
                domainEvent.CreatedByUserId,
                domainEvent.CompanyName,
                domainEvent.IncludeSampleData);

            if (result.Success)
                _logger.LogInformation(
                    "Sales: Data initialized successfully for Company:{CompanyId}",
                    domainEvent.CompanyId);
            else
                _logger.LogWarning(
                    "Sales: Initialization returned non-success for Company:{CompanyId} - {Message}",
                    domainEvent.CompanyId, result.Message);
        }
        catch (Exception ex)
        {
            // Non-blocking - never prevent company creation from completing
            _logger.LogError(ex,
                "Sales: Error in SalesCompanyCreatedEventHandler for Company:{CompanyId}",
                domainEvent.CompanyId);
        }
    }
}
