using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Events;
using Procurement.Application.Services.Interfaces;
using Procurement.Infrastructure.Services;

namespace Procurement.Infrastructure.Events;

/// <summary>
/// Handles <see cref="CompanyCreatedEvent"/> by initializing Procurement data for the new company:
///   1. Master data (categories, document sequences, approval workflows, settings) via
///      <see cref="IProcurementInitializationService"/>.
///   2. Comprehensive interconnected sample data (vendors, requisitions, RFQs, POs, receipts,
///      invoices, payments, returns, debit notes, landed costs) via <see cref="ProcurementSeedDataService"/>.
///
/// Follows the same pattern as the Inventory, Accounting and Manufacturing modules — non-blocking,
/// loosely coupled, subscribing only to Core events.
/// </summary>
public class ProcurementCompanyCreatedEventHandler : IEventHandler<CompanyCreatedEvent>
{
    private readonly IProcurementInitializationService _initService;
    private readonly ProcurementSeedDataService _seedService;
    private readonly ILogger<ProcurementCompanyCreatedEventHandler> _logger;

    public ProcurementCompanyCreatedEventHandler(
        IProcurementInitializationService initService,
        ProcurementSeedDataService seedService,
        ILogger<ProcurementCompanyCreatedEventHandler> logger)
    {
        _initService = initService;
        _seedService = seedService;
        _logger      = logger;
    }

    public async Task HandleAsync(CompanyCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Handling CompanyCreatedEvent for Procurement module — Company: {CompanyId} ({CompanyName})",
                domainEvent.CompanyId, domainEvent.CompanyName);

            // 1. Master data first (sample data references categories / workflows it seeds).
            await _initService.InitializeAsync(domainEvent.CompanyId);

            // 2. Sample transactional data.
            var seeded = await _seedService.SeedAsync(
                domainEvent.CompanyId,
                domainEvent.BranchId,
                domainEvent.BusinessUnitId,
                domainEvent.CreatedByUserId);

            if (seeded)
                _logger.LogInformation("Procurement sample data seeded for Company: {CompanyId}", domainEvent.CompanyId);
            else
                _logger.LogInformation("Procurement sample data already existed for Company: {CompanyId}", domainEvent.CompanyId);
        }
        catch (Exception ex)
        {
            // Non-blocking — do not rethrow; log and continue so company creation still succeeds.
            _logger.LogError(ex, "Error initializing Procurement data for Company: {CompanyId}", domainEvent.CompanyId);
        }
    }
}
