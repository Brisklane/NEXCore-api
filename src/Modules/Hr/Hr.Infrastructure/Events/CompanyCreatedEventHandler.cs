using Nexcore.SharedKernel.Events;
using Hr.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Events;

public class CompanyCreatedEventHandler : IEventHandler<CompanyCreatedEvent>
{
    private readonly IHrInitializationService _hrInitializationService;
    private readonly ILogger<CompanyCreatedEventHandler> _logger;

    public CompanyCreatedEventHandler(
        IHrInitializationService hrInitializationService,
        ILogger<CompanyCreatedEventHandler> logger)
    {
        _hrInitializationService = hrInitializationService;
        _logger = logger;
    }

    public async Task HandleAsync(CompanyCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Handling CompanyCreatedEvent for Company: {CompanyId} ({CompanyName})",
                domainEvent.CompanyId, domainEvent.CompanyName);

            var result = await _hrInitializationService.InitializeHrForNewCompanyAsync(
                domainEvent.CompanyId,
                domainEvent.BranchId,
                domainEvent.BusinessUnitId,
                domainEvent.CreatedByUserId,
                domainEvent.IncludeSampleData);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Successfully initialized HR data for Company: {CompanyId}",
                    domainEvent.CompanyId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to initialize HR data for Company: {CompanyId}. Message: {Message}",
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
