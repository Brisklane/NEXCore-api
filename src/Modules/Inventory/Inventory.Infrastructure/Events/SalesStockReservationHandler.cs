using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Events;

namespace Inventory.Infrastructure.Events;

/// <summary>
/// Adjusts soft allocations (InventoryBalance.QuantityReserved) in response to a Sales order being
/// committed or released — published as <see cref="StockReservationChangedEvent"/>.
///
/// Each line carries a SIGNED quantity (positive = reserve, negative = release). This only moves
/// Reserved / Available; physical on-hand and cost are untouched (no ledger transaction, no GL).
/// </summary>
public class SalesStockReservationHandler : IEventHandler<StockReservationChangedEvent>
{
    private readonly InventoryDbContext _ctx;
    private readonly IInventoryBalanceService _balances;
    private readonly ILogger<SalesStockReservationHandler> _logger;

    public SalesStockReservationHandler(
        InventoryDbContext ctx,
        IInventoryBalanceService balances,
        ILogger<SalesStockReservationHandler> logger)
    {
        _ctx      = ctx;
        _balances = balances;
        _logger   = logger;
    }

    public async Task HandleAsync(StockReservationChangedEvent e, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Stock reservation change: {RefType} {RefId} — {LineCount} line(s)",
            e.ReferenceType, e.ReferenceId, e.Lines.Count);

        var changed = 0;
        foreach (var line in e.Lines)
        {
            if (line.Quantity == 0m) continue;

            var warehouseId = line.WarehouseId ?? e.WarehouseId;
            if (warehouseId == Guid.Empty)
            {
                _logger.LogWarning(
                    "Reservation skipped for Product {ProductId} on {RefType} {RefId}: no warehouse resolved.",
                    line.ProductId, e.ReferenceType, e.ReferenceId);
                continue;
            }

            // SignedQuantity 0 = no physical movement; only the reservation changes.
            await _balances.ApplyMovementAsync(new StockMovement(
                ItemId:          line.ProductId,
                WarehouseId:     warehouseId,
                BinId:           line.BinId,
                VariantId:       line.VariantId,
                SignedQuantity:  0m,
                UnitCost:        0m,
                CompanyId:       e.CompanyId,
                BranchId:        e.BranchId,
                BusinessUnitId:  e.BusinessUnitId,
                CreatedByUserId: e.CreatedByUserId,
                ReservationDelta: line.Quantity), cancellationToken);

            changed++;
        }

        await _ctx.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Stock reservation committed: {RefType} {RefId} — {Count}/{Total} line(s)",
            e.ReferenceType, e.ReferenceId, changed, e.Lines.Count);
    }
}
