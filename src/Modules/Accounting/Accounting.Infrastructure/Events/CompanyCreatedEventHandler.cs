using Nexcore.SharedKernel.Events;
using Accounting.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Accounting.Infrastructure.Events;

/// <summary>
/// Event handler for CompanyCreatedEvent
/// When a new company is created, this handler initializes accounting data for that company
/// This maintains loose coupling - Accounting module subscribes to Core module events
/// </summary>
public class CompanyCreatedEventHandler : IEventHandler<CompanyCreatedEvent>
{
    private readonly IAccountingInitializationService _accountingInitializationService;
    private readonly ILogger<CompanyCreatedEventHandler> _logger;

    public CompanyCreatedEventHandler(
        IAccountingInitializationService accountingInitializationService,
        ILogger<CompanyCreatedEventHandler> logger)
    {
        _accountingInitializationService = accountingInitializationService;
        _logger = logger;
    }

    /// <summary>
    /// Handle the CompanyCreatedEvent by initializing accounting data
    /// </summary>
    public async Task HandleAsync(CompanyCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Handling CompanyCreatedEvent for Company: {CompanyId} ({CompanyName})",
                domainEvent.CompanyId, domainEvent.CompanyName);

            // Initialize accounting data for the newly created company
            var result = await _accountingInitializationService.InitializeAccountingForNewCompanyAsync(
                domainEvent.CompanyId,
                domainEvent.BranchId,
                domainEvent.BusinessUnitId,
                domainEvent.CreatedByUserId);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Successfully initialized accounting data for Company: {CompanyId}",
                    domainEvent.CompanyId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to initialize accounting data for Company: {CompanyId}. Message: {Message}",
                    domainEvent.CompanyId, result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling CompanyCreatedEvent for Company: {CompanyId}",
                domainEvent.CompanyId);
            // Non-blocking - don't throw, let event publishing continue
        }
    }
}
