using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Events;

namespace Inventory.Infrastructure.Events;

/// <summary>
/// Adds accepted stock to inventory when Procurement posts a Goods Receipt.
///
/// Inbound counterpart of <see cref="SalesStockDeductionHandler"/>: for each line it
///   1. Creates one posted "GRN" InventoryDocument for the receipt
///   2. Creates an InventoryDocumentLine + InventoryTransaction (TransactionType = "IN", positive qty)
///   3. Updates InventoryBalance via the shared <see cref="IInventoryBalanceService"/> (QuantityOnHand
///      += accepted qty, moving-average cost) — the same keying/costing the manual GRN posting uses.
/// </summary>
public class PurchaseStockReceiptHandler : IEventHandler<GoodsReceiptPostedEvent>
{
    private readonly InventoryDbContext _ctx;
    private readonly IInventoryBalanceService _balances;
    private readonly ILogger<PurchaseStockReceiptHandler> _logger;

    public PurchaseStockReceiptHandler(
        InventoryDbContext ctx,
        IInventoryBalanceService balances,
        ILogger<PurchaseStockReceiptHandler> logger)
    {
        _ctx      = ctx;
        _balances = balances;
        _logger   = logger;
    }

    public async Task HandleAsync(GoodsReceiptPostedEvent e, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Stock receipt: Goods Receipt {ReceiptNumber} ({ReceiptId}) — {LineCount} line(s)",
            e.ReceiptNumber, e.GoodsReceiptId, e.Lines.Count);

        if (e.Lines.Count == 0) return;

        // Company-wide fallback unit (seeded each-unit is "PCS"; there is no "EA").
        var defaultUnit = await _ctx.Units
            .FirstOrDefaultAsync(u => u.CompanyId == e.CompanyId && (u.Code == "EA" || u.Code == "PCS"), cancellationToken)
            ?? await _ctx.Units.FirstOrDefaultAsync(u => u.CompanyId == e.CompanyId, cancellationToken);

        var document = new InventoryDocument
        {
            Id              = Guid.NewGuid(),
            CompanyId       = e.CompanyId,
            BranchId        = e.BranchId,
            BusinessUnitId  = e.BusinessUnitId,
            CreatedByUserId = e.CreatedByUserId,
            DocumentNumber  = string.IsNullOrWhiteSpace(e.ReceiptNumber) ? $"GRN-{e.GoodsReceiptId:N}" : e.ReceiptNumber,
            DocumentType    = "GRN",
            DocumentDate    = DateTime.UtcNow,
            Status          = "Posted",
            ReferenceId     = e.GoodsReceiptId,
            ReferenceType   = "GoodsReceipt",
            ToWarehouseId   = e.WarehouseId,   // inbound → goods land in the To warehouse
        };
        _ctx.InventoryDocuments.Add(document);

        var lineNumber = 0;
        var received = 0;
        foreach (var line in e.Lines)
        {
            lineNumber++;
            if (line.Quantity <= 0) continue;   // nothing accepted on this line

            var warehouseId = line.WarehouseId ?? e.WarehouseId;
            if (warehouseId == Guid.Empty)
                warehouseId = await ResolveFallbackWarehouseAsync(e.CompanyId, cancellationToken);

            if (warehouseId == Guid.Empty)
            {
                _logger.LogError(
                    "Stock receipt skipped for Product {ProductId} on GoodsReceipt {ReceiptId}: no warehouse " +
                    "resolved (receipt has none and the company has no active warehouse).",
                    line.ProductId, e.GoodsReceiptId);
                continue;
            }

            // Resolve unit by the line's UnitId, else the item's own base unit, else the company default.
            var unit = line.UnitId.HasValue
                ? await _ctx.Units.FirstOrDefaultAsync(u => u.Id == line.UnitId.Value, cancellationToken)
                : null;
            if (unit is null)
            {
                var baseUnitId = await _ctx.Items
                    .Where(i => i.Id == line.ProductId)
                    .Select(i => i.BaseUnitId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (baseUnitId != Guid.Empty)
                    unit = await _ctx.Units.FirstOrDefaultAsync(u => u.Id == baseUnitId, cancellationToken);
            }
            unit ??= defaultUnit;

            if (unit is null)
            {
                _logger.LogWarning(
                    "No unit resolved for Product {ProductId} (CompanyId:{CompanyId}). Skipping receipt line.",
                    line.ProductId, e.CompanyId);
                continue;
            }

            var docLine = new InventoryDocumentLine
            {
                Id              = Guid.NewGuid(),
                CompanyId       = e.CompanyId,
                BranchId        = e.BranchId,
                BusinessUnitId  = e.BusinessUnitId,
                CreatedByUserId = e.CreatedByUserId,
                DocumentId      = document.Id,
                LineNumber      = lineNumber,
                ItemId          = line.ProductId,
                WarehouseId     = warehouseId,
                VariantId       = line.VariantId,
                UnitId          = unit.Id,
                Quantity        = line.Quantity,
                UnitCost        = line.UnitCost,
                TotalCost       = line.Quantity * line.UnitCost,
            };
            _ctx.InventoryDocumentLines.Add(docLine);

            var transaction = new InventoryTransaction
            {
                Id              = Guid.NewGuid(),
                CompanyId       = e.CompanyId,
                BranchId        = e.BranchId,
                BusinessUnitId  = e.BusinessUnitId,
                CreatedByUserId = e.CreatedByUserId,
                ItemId          = line.ProductId,
                WarehouseId     = warehouseId,
                VariantId       = line.VariantId,
                UnitId          = unit.Id,
                TransactionType = "IN",
                Quantity        = line.Quantity,   // positive = inbound
                UnitCost        = line.UnitCost,
                TotalCost       = line.Quantity * line.UnitCost,
                DocumentId      = document.Id,
                DocumentLineId  = docLine.Id,
                TransactionDate = DateTime.UtcNow,
            };
            _ctx.InventoryTransactions.Add(transaction);

            // QuantityOnHand += qty with moving-average costing (same shared path as the manual GRN posting).
            // Receipts do not touch reservations (ReservationDelta defaults to 0).
            await _balances.ApplyMovementAsync(new StockMovement(
                ItemId:          line.ProductId,
                WarehouseId:     warehouseId,
                BinId:           null,
                VariantId:       line.VariantId,
                SignedQuantity:  line.Quantity,    // positive = receipt
                UnitCost:        line.UnitCost,
                CompanyId:       e.CompanyId,
                BranchId:        e.BranchId,
                BusinessUnitId:  e.BusinessUnitId,
                CreatedByUserId: e.CreatedByUserId), cancellationToken);

            received++;
        }

        await _ctx.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Stock receipt committed: Document {DocNumber} — {Count}/{Total} line(s) added",
            document.DocumentNumber, received, e.Lines.Count);
    }

    /// <summary>Pick a real warehouse for the company when the receipt carried none (Main → Retail → any).</summary>
    private async Task<Guid> ResolveFallbackWarehouseAsync(Guid companyId, CancellationToken ct)
    {
        var warehouses = await _ctx.Warehouses
            .Where(w => w.CompanyId == companyId && w.IsActive)
            .Select(w => new { w.Id, w.WarehouseType })
            .ToListAsync(ct);

        var chosen = warehouses.FirstOrDefault(w => w.WarehouseType == "Main")
                  ?? warehouses.FirstOrDefault(w => w.WarehouseType == "Retail")
                  ?? warehouses.FirstOrDefault();

        return chosen?.Id ?? Guid.Empty;
    }
}
