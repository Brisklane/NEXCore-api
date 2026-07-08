using Inventory.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Events;

namespace Inventory.Infrastructure.Events;

/// <summary>
/// Answers <see cref="StockAvailabilityLookupEvent"/> from the Sales POS checkout with the current
/// on-hand quantity (summed across bins) for a product/variant in a warehouse. Always completes the
/// TCS — on any failure it returns <see cref="StockAvailabilityData.Unresolved"/> so the caller can
/// fail-open instead of blocking a sale on a transient error.
/// </summary>
public class StockAvailabilityLookupHandler : IEventHandler<StockAvailabilityLookupEvent>
{
    private readonly IInventoryBalanceService _balances;
    private readonly ILogger<StockAvailabilityLookupHandler> _logger;

    public StockAvailabilityLookupHandler(
        IInventoryBalanceService balances,
        ILogger<StockAvailabilityLookupHandler> logger)
    {
        _balances = balances;
        _logger = logger;
    }

    public async Task HandleAsync(StockAvailabilityLookupEvent e, CancellationToken ct = default)
    {
        try
        {
            var available = await _balances.GetAvailableAsync(e.ProductId, e.WarehouseId, e.VariantId, e.CompanyId, ct);
            // No balance row = item not stocked/tracked here (service line, never received) → Unresolved,
            // so the POS check fails open and does not block. A real zero available returns a resolved 0.
            e.Result.TrySetResult(available.HasValue
                ? new StockAvailabilityData(available.Value, Resolved: true)
                : StockAvailabilityData.Unresolved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "StockAvailabilityLookupHandler failed for Product {ProductId} Warehouse {WarehouseId}",
                e.ProductId, e.WarehouseId);
            e.Result.TrySetResult(StockAvailabilityData.Unresolved);
        }
    }
}
