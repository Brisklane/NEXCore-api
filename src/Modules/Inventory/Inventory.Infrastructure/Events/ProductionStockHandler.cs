using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Events;

namespace Inventory.Infrastructure.Events;

/// <summary>
/// Applies the inventory impact of a completed production run published by the Manufacturing module
/// (<see cref="ProductionCompletedEvent"/>) — the counterpart of <see cref="SalesStockDeductionHandler"/>
/// (outbound) and <see cref="PurchaseStockReceiptHandler"/> (inbound), combined into one posting:
///
///   ConsumedMaterials → "MFGISS" Issue document   → InventoryTransaction "OUT" (negative qty), balance −
///   ProducedGoods     → "MFGRCP" Receipt document  → InventoryTransaction "IN"  (positive qty), balance +
///
/// All movements go through the shared <see cref="IInventoryBalanceService"/> so keying and
/// moving-average costing match every other stock path, and everything commits in one SaveChanges.
/// </summary>
public class ProductionStockHandler : IEventHandler<ProductionCompletedEvent>
{
    private readonly InventoryDbContext _ctx;
    private readonly IInventoryBalanceService _balances;
    private readonly ILogger<ProductionStockHandler> _logger;

    public ProductionStockHandler(
        InventoryDbContext ctx,
        IInventoryBalanceService balances,
        ILogger<ProductionStockHandler> logger)
    {
        _ctx      = ctx;
        _balances = balances;
        _logger   = logger;
    }

    public async Task HandleAsync(ProductionCompletedEvent e, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Production stock: Order {OrderNumber} ({OrderId}) — {ConsumeCount} consumed, {ProduceCount} produced",
            e.OrderNumber, e.ProductionOrderId, e.ConsumedMaterials.Count, e.ProducedGoods.Count);

        if (e.ConsumedMaterials.Count == 0 && e.ProducedGoods.Count == 0) return;

        var defaultUnit = await _ctx.Units
            .FirstOrDefaultAsync(u => u.CompanyId == e.CompanyId && (u.Code == "EA" || u.Code == "PCS"), ct)
            ?? await _ctx.Units.FirstOrDefaultAsync(u => u.CompanyId == e.CompanyId, ct);

        // ── Backflush: consume raw materials (OUT), valued at current moving-average cost ──
        var consumedCost = 0m;
        if (e.ConsumedMaterials.Count > 0)
        {
            var issueDoc = NewDocument(e, "MFGISS", $"MFG-ISS-{e.ProductionOrderId:N}", fromWarehouse: true);
            _ctx.InventoryDocuments.Add(issueDoc);
            consumedCost = await PostLinesAsync(e, issueDoc, e.ConsumedMaterials, outbound: true, overrideUnitCost: null, defaultUnit, ct);
        }

        // ── Receive finished goods (IN), valued at the rolled-up component cost ──────────
        // The finished good's manufacturing cost = material cost (components consumed) + non-material
        // conversion cost (labour/overhead/other, from the product's Standard Cost), spread over the
        // quantity produced. Passing it as the receipt unit cost makes the finished item's moving-
        // average cost correct so a later sale books a real COGS instead of zero.
        if (e.ProducedGoods.Count > 0)
        {
            var producedQty = e.ProducedGoods.Sum(p => p.Quantity);
            var totalCost = consumedCost + (e.ConversionUnitCost * producedQty);
            var finishedUnitCost = producedQty > 0m ? totalCost / producedQty : 0m;
            var receiptDoc = NewDocument(e, "MFGRCP", $"MFG-RCP-{e.ProductionOrderId:N}", fromWarehouse: false);
            _ctx.InventoryDocuments.Add(receiptDoc);
            await PostLinesAsync(e, receiptDoc, e.ProducedGoods, outbound: false, overrideUnitCost: finishedUnitCost, defaultUnit, ct);
        }

        await _ctx.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Production stock committed for Order {OrderNumber}", e.OrderNumber);
    }

    private static InventoryDocument NewDocument(
        ProductionCompletedEvent e, string type, string number, bool fromWarehouse) => new()
    {
        Id              = Guid.NewGuid(),
        CompanyId       = e.CompanyId,
        BranchId        = e.BranchId,
        BusinessUnitId  = e.BusinessUnitId,
        CreatedByUserId = e.CreatedByUserId,
        DocumentNumber  = number,
        DocumentType    = type,
        DocumentDate    = DateTime.UtcNow,
        Status          = "Posted",
        ReferenceId     = e.ProductionOrderId,
        ReferenceType   = "ProductionOrder",
        FromWarehouseId = fromWarehouse ? e.WarehouseId : null,
        ToWarehouseId   = fromWarehouse ? null : e.WarehouseId,
    };

    /// <summary>
    /// Posts one direction (issue or receipt) and returns the total cost moved. For an issue,
    /// each line is valued at the material's current moving-average cost; for a receipt, the caller
    /// supplies the rolled-up finished-goods cost via <paramref name="overrideUnitCost"/>.
    /// </summary>
    private async Task<decimal> PostLinesAsync(
        ProductionCompletedEvent e,
        InventoryDocument document,
        IReadOnlyList<StockDeductionLine> lines,
        bool outbound,
        decimal? overrideUnitCost,
        Unit? defaultUnit,
        CancellationToken ct)
    {
        var totalCost = 0m;
        var lineNumber = 0;
        foreach (var line in lines)
        {
            lineNumber++;
            if (line.Quantity <= 0) continue;

            var warehouseId = line.WarehouseId ?? e.WarehouseId ?? Guid.Empty;
            if (warehouseId == Guid.Empty)
                warehouseId = await ResolveFallbackWarehouseAsync(e.CompanyId, line.ProductId, line.VariantId, outbound, ct);

            if (warehouseId == Guid.Empty)
            {
                _logger.LogError(
                    "Production stock skipped for Product {ProductId} on Order {OrderId}: no warehouse resolved.",
                    line.ProductId, e.ProductionOrderId);
                continue;
            }

            // Resolve unit: line UoM → item base unit → company default.
            var unit = string.IsNullOrEmpty(line.UnitOfMeasure)
                ? null
                : await _ctx.Units.FirstOrDefaultAsync(u => u.CompanyId == e.CompanyId && u.Code == line.UnitOfMeasure, ct);
            if (unit is null)
            {
                var baseUnitId = await _ctx.Items
                    .Where(i => i.Id == line.ProductId)
                    .Select(i => i.BaseUnitId)
                    .FirstOrDefaultAsync(ct);
                if (baseUnitId != Guid.Empty)
                    unit = await _ctx.Units.FirstOrDefaultAsync(u => u.Id == baseUnitId, ct);
            }
            unit ??= defaultUnit;

            if (unit is null)
            {
                _logger.LogWarning(
                    "No unit resolved for Product {ProductId} (CompanyId:{CompanyId}). Skipping production line.",
                    line.ProductId, e.CompanyId);
                continue;
            }

            var signedQty = outbound ? -line.Quantity : line.Quantity;

            // Cost: a receipt uses the rolled-up cost the caller computed; an issue is valued at the
            // material's current moving-average cost. Any explicit cost on the line is the last resort.
            decimal unitCost;
            if (overrideUnitCost.HasValue) unitCost = overrideUnitCost.Value;
            else if (outbound) unitCost = await ResolveUnitCostAsync(e.CompanyId, line.ProductId, line.VariantId, warehouseId, ct);
            else unitCost = line.UnitCost;
            if (unitCost <= 0m && line.UnitCost > 0m) unitCost = line.UnitCost;

            var lineTotal = line.Quantity * unitCost;
            totalCost += lineTotal;

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
                BinId           = line.BinId,
                VariantId       = line.VariantId,
                UnitId          = unit.Id,
                Quantity        = line.Quantity,
                UnitCost        = unitCost,
                TotalCost       = lineTotal,
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
                BinId           = line.BinId,
                VariantId       = line.VariantId,
                UnitId          = unit.Id,
                TransactionType = outbound ? "OUT" : "IN",
                Quantity        = signedQty,
                UnitCost        = unitCost,
                TotalCost       = lineTotal,
                DocumentId      = document.Id,
                DocumentLineId  = docLine.Id,
                TransactionDate = DateTime.UtcNow,
            };
            _ctx.InventoryTransactions.Add(transaction);

            await _balances.ApplyMovementAsync(new StockMovement(
                ItemId:          line.ProductId,
                WarehouseId:     warehouseId,
                BinId:           line.BinId,
                VariantId:       line.VariantId,
                SignedQuantity:  signedQty,
                UnitCost:        unitCost,
                CompanyId:       e.CompanyId,
                BranchId:        e.BranchId,
                BusinessUnitId:  e.BusinessUnitId,
                CreatedByUserId: e.CreatedByUserId), ct);
        }

        return totalCost;
    }

    /// <summary>
    /// Current moving-average cost of a material: the balance in the issuing warehouse first, then the
    /// richest-stock balance in any warehouse, then the item's Default-price-list purchase price.
    /// Returns 0 when nothing is known (the finished good is then valued by whatever components did cost).
    /// </summary>
    private async Task<decimal> ResolveUnitCostAsync(
        Guid companyId, Guid itemId, Guid? variantId, Guid warehouseId, CancellationToken ct)
    {
        var cost = await _ctx.InventoryBalances
            .Where(b => b.CompanyId == companyId && b.ItemId == itemId && b.VariantId == variantId
                     && b.WarehouseId == warehouseId && b.AverageCost > 0m)
            .Select(b => (decimal?)b.AverageCost)
            .FirstOrDefaultAsync(ct);

        cost ??= await _ctx.InventoryBalances
            .Where(b => b.CompanyId == companyId && b.ItemId == itemId && b.VariantId == variantId
                     && b.AverageCost > 0m)
            .OrderByDescending(b => b.QuantityOnHand)
            .Select(b => (decimal?)b.AverageCost)
            .FirstOrDefaultAsync(ct);

        if (cost is null or 0m)
        {
            cost = await _ctx.Items
                .Where(i => i.Id == itemId)
                .SelectMany(i => i.Prices.Where(p => p.PriceList == "Default"))
                .Select(p => (decimal?)p.PurchasePrice)
                .FirstOrDefaultAsync(ct);
        }

        return cost ?? 0m;
    }

    /// <summary>
    /// Warehouse fallback when a line carries none. For an issue (outbound) prefer where the material
    /// already holds stock; for a receipt (inbound) prefer the Main/Retail warehouse.
    /// </summary>
    private async Task<Guid> ResolveFallbackWarehouseAsync(
        Guid companyId, Guid itemId, Guid? variantId, bool outbound, CancellationToken ct)
    {
        if (outbound)
        {
            var stockedWarehouseId = await _ctx.InventoryBalances
                .Where(b => b.CompanyId == companyId && b.ItemId == itemId && b.VariantId == variantId)
                .OrderByDescending(b => b.QuantityOnHand)
                .Select(b => b.WarehouseId)
                .FirstOrDefaultAsync(ct);
            if (stockedWarehouseId != Guid.Empty) return stockedWarehouseId;
        }

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
