using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Events;

namespace Inventory.Infrastructure.Services;

/// <summary>
/// Resolves Accounting GL Account IDs needed for inventory item posting by
/// publishing a <see cref="GlAccountsRequestedEvent"/> on the in-process event
/// bus and awaiting the reply from the Accounting module handler.
///
/// The caller controls which COA account numbers to look up — defaults match
/// the standard chart of accounts seeded by AccountingInitializationService.
/// Pass a custom <see cref="GlAccountNumbers"/> to support localised or
/// company-specific numbering schemes.
///
/// Inventory.Infrastructure has zero compile-time reference to
/// Accounting.Infrastructure — communication is purely through SharedKernel events.
/// </summary>
public class InventoryGlAccountResolver
{
    private readonly IEventPublisher _publisher;
    private readonly ILogger<InventoryGlAccountResolver> _logger;

    public InventoryGlAccountResolver(
        IEventPublisher publisher,
        ILogger<InventoryGlAccountResolver> logger)
    {
        _publisher = publisher;
        _logger    = logger;
    }

    /// <summary>
    /// Asks the Accounting module for the four GL account IDs for the given
    /// company / branch / business-unit via the in-process event bus.
    ///
    /// Optionally supply <paramref name="accountNumbers"/> to override the
    /// default COA numbers (useful for multi-locale deployments).
    ///
    /// Returns <see cref="GlAccountSet.Empty"/> if Accounting has not been
    /// initialised yet or if no handler is registered.
    /// </summary>
    public async Task<GlAccountSet> ResolveAsync(
        Guid  companyId,
        Guid  branchId,
        Guid? businessUnitId    = null,
        GlAccountNumbers? accountNumbers = null)
    {
        try
        {
            var numbers = accountNumbers ?? GlAccountNumbers.Default;

            var request = new GlAccountsRequestedEvent
            {
                CompanyId          = companyId,
                BranchId           = branchId,
                BusinessUnitId     = businessUnitId,
                InventoryAccountNo = numbers.Inventory,
                CogsAccountNo      = numbers.Cogs,
                PurchaseAccountNo  = numbers.Purchase,
                SalesAccountNo     = numbers.Sales,
            };

            await _publisher.PublishAsync(request);

            if (request.Result.Task.IsCompleted)
            {
                var set = await request.Result.Task;
                _logger.LogInformation(
                    "GL accounts resolved for Company:{CompanyId} Branch:{BranchId} — " +
                    "Inventory:{Inv} COGS:{Cogs} Purchase:{Pur} Sales:{Sal}",
                    companyId, branchId,
                    set.Inventory, set.Cogs, set.Purchase, set.Sales);
                return set;
            }

            _logger.LogWarning(
                "GL account resolution for Company:{CompanyId} Branch:{BranchId} returned no result. " +
                "Items will be saved without GL links.", companyId, branchId);
            return GlAccountSet.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to resolve GL accounts for Company:{CompanyId} Branch:{BranchId}. " +
                "Items will be saved without GL links.", companyId, branchId);
            return GlAccountSet.Empty;
        }
    }
}
