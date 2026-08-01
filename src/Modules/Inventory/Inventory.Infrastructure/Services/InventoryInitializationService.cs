using Inventory.Application.Services.Interfaces;
using Inventory.Domain.Constants;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel;

namespace Inventory.Infrastructure.Services;

/// <summary>
/// Seeds comprehensive inventory master data for a newly created company.
/// </summary>
public partial class InventoryInitializationService : IInventoryInitializationService
{
    private readonly InventoryDbContext             _ctx;
    private readonly InventoryGlAccountResolver     _glResolver;
    private readonly ILogger<InventoryInitializationService> _logger;

    public InventoryInitializationService(
        InventoryDbContext ctx,
        InventoryGlAccountResolver glResolver,
        ILogger<InventoryInitializationService> logger)
    {
        _ctx        = ctx;
        _glResolver = glResolver;
        _logger     = logger;
    }

    // ── public API ────────────────────────────────────────────────────────────

    public async Task<Result> InitializeInventoryForNewCompanyAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId,
        bool includeSampleData = false)
    {
        try
        {
            _logger.LogInformation(
                "Initializing inventory data for Company:{CompanyId} Branch:{BranchId} BU:{BusinessUnitId} IncludeSampleData:{Include}",
                companyId, branchId, businessUnitId, includeSampleData);

            if (await InventoryDataExistsAsync(companyId))
            {
                _logger.LogWarning("Inventory data already exists for Company:{CompanyId}", companyId);
                return Result.Ok("Inventory data already initialized for this company");
            }

            // Resolve GL accounts BEFORE the transaction (read-only, separate DbContext)
            var gl = await _glResolver.ResolveAsync(companyId, branchId, businessUnitId);

            await using var tx = await _ctx.Database.BeginTransactionAsync();
            try
            {
                var T = (companyId, branchId, businessUnitId, userId);

                // ── 1. Units of Measure ───────────────────────────────────────
                var units = SeedUnits(T);
                _ctx.Units.AddRange(units);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} units", units.Length);

                // ── 2. Item Categories ────────────────────────────────────────
                var categories = SeedItemCategories(T, gl);   // gl passed here
                _ctx.ItemCategories.AddRange(categories);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} item categories", categories.Count);

                // ── 3. Warehouses ─────────────────────────────────────────────
                var warehouses = SeedWarehouses(T);
                _ctx.Warehouses.AddRange(warehouses);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} warehouses", warehouses.Length);

                // ── 4. Bins ───────────────────────────────────────────────────
                var bins = SeedBins(T, warehouses);
                _ctx.Bins.AddRange(bins);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} bins", bins.Length);

                // ── 5. Brands ─────────────────────────────────────────────────
                var brands = SeedBrands(T);
                _ctx.Brands.AddRange(brands);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} brands", brands.Length);

                // ── 6. Colors ─────────────────────────────────────────────────
                var colors = SeedColors(T);
                _ctx.Colors.AddRange(colors);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} colors", colors.Length);

                // ── 7. Sizes ──────────────────────────────────────────────────
                var sizes = SeedSizes(T);
                _ctx.Sizes.AddRange(sizes);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} sizes", sizes.Length);

                // ── 8. Attribute Definitions ──────────────────────────────────
                var attrs = SeedAttributeDefinitions(T);
                _ctx.AttributeDefinitions.AddRange(attrs);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} attribute definitions", attrs.Length);

                // ── 9. Tax Definitions ────────────────────────────────────────
                var taxes = SeedTaxDefinitions(T);
                _ctx.TaxDefinitions.AddRange(taxes);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} tax definitions", taxes.Length);

                // ═══ SAMPLE DATA (opt-in) ═════════════════════════════════════
                // Everything below is the demo product catalog and its stock movements.
                // Skipped entirely when the user declined sample data at registration —
                // the master data above is all a real company needs to start trading.
                if (!includeSampleData)
                {
                    await tx.CommitAsync();
                    _logger.LogInformation(
                        "Inventory sample data skipped (IncludeSampleData=false) for Company:{CompanyId}", companyId);
                    return Result.Ok("Inventory master data initialized successfully");
                }

                // ── 10. Items (batched) — GL accounts injected ─────────────────
                var (items, barcodes, images) = SeedItems(T, units, categories, brands, gl);
                const int batchSize = 200;
                for (int i = 0; i < items.Length; i += batchSize)
                {
                    _ctx.Items.AddRange(items.Skip(i).Take(batchSize));
                    await _ctx.SaveChangesAsync();
                }
                _ctx.ItemBarcodes.AddRange(barcodes);
                await _ctx.SaveChangesAsync();
                _ctx.ItemImages.AddRange(images);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation(
                    "Seeded {N} items, {B} barcodes, {Img} images",
                    items.Length, barcodes.Length, images.Length);

                // ── 10b. Item Attributes ───────────────────────────────────────
                var itemAttrs = SeedItemAttributes(T, items, attrs);
                _ctx.ItemAttributes.AddRange(itemAttrs);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} item attributes", itemAttrs.Length);

                // ── 10c. Item Colors ───────────────────────────────────────────
                var itemColors = SeedItemColors(T, items, colors);
                _ctx.ItemColors.AddRange(itemColors);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} item colors", itemColors.Length);

                // ── 10d. Item Sizes ────────────────────────────────────────────
                var itemSizes = SeedItemSizes(T, items, sizes);
                _ctx.ItemSizes.AddRange(itemSizes);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} item sizes", itemSizes.Length);

                // ── 11. Item Prices ────────────────────────────────────────────
                var prices = SeedItemPrices(T, items, units, taxes);
                _ctx.ItemPrices.AddRange(prices);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} item prices", prices.Length);

                // ── 12. Item Taxes ─────────────────────────────────────────────
                var itemTaxes = SeedItemTaxes(T, items, taxes);
                _ctx.ItemTaxes.AddRange(itemTaxes);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} item taxes", itemTaxes.Length);

                // ── 13. GRN Documents + Lines + Transactions + Balances ────────
                var (grnDocs, grnLines, grnTxns, costLayers, balances) =
                    SeedGrnMovements(T, items, warehouses, bins, units);

                _ctx.InventoryDocuments.AddRange(grnDocs);
                await _ctx.SaveChangesAsync();

                _ctx.InventoryDocumentLines.AddRange(grnLines);
                await _ctx.SaveChangesAsync();

                _ctx.InventoryTransactions.AddRange(grnTxns);
                await _ctx.SaveChangesAsync();

                _ctx.InventoryCostLayers.AddRange(costLayers);
                await _ctx.SaveChangesAsync();

                _ctx.InventoryBalances.AddRange(balances);
                await _ctx.SaveChangesAsync();

                _logger.LogInformation(
                    "Seeded {D} GRN docs, {L} lines, {Tx} transactions, {Cl} cost layers, {Bl} balances",
                    grnDocs.Length, grnLines.Length, grnTxns.Length, costLayers.Length, balances.Length);

                // ── 14. Sales Issue Documents + Stock-Out Transactions ─────────
                var (issDocs, issLines, issTxns, updatedBalances) =
                    SeedIssueMovements(T, items, warehouses, bins, units, grnTxns, balances);

                _ctx.InventoryDocuments.AddRange(issDocs);
                await _ctx.SaveChangesAsync();

                _ctx.InventoryDocumentLines.AddRange(issLines);
                await _ctx.SaveChangesAsync();

                _ctx.InventoryTransactions.AddRange(issTxns);
                await _ctx.SaveChangesAsync();

                // Update balances for issued quantities
                _ctx.InventoryBalances.UpdateRange(updatedBalances);
                await _ctx.SaveChangesAsync();

                _logger.LogInformation(
                    "Seeded {D} issue docs, {L} lines, {Tx} transactions",
                    issDocs.Length, issLines.Length, issTxns.Length);

                // ── 15. Transfer Document ──────────────────────────────────────
                var (xferDocs, xferLines, xferTxns, xferBalances) =
                    SeedTransferMovements(T, items, warehouses, bins, units, balances);

                _ctx.InventoryDocuments.AddRange(xferDocs);
                await _ctx.SaveChangesAsync();
                _ctx.InventoryDocumentLines.AddRange(xferLines);
                await _ctx.SaveChangesAsync();
                _ctx.InventoryTransactions.AddRange(xferTxns);
                await _ctx.SaveChangesAsync();
                _ctx.InventoryBalances.AddRange(xferBalances);
                await _ctx.SaveChangesAsync();

                _logger.LogInformation(
                    "Seeded {D} transfer docs, {L} lines, {Tx} transactions",
                    xferDocs.Length, xferLines.Length, xferTxns.Length);

                // ── 16. Inventory Valuations (monthly snapshots) ───────────────
                var valuations = SeedInventoryValuations(T, items, warehouses, balances);
                _ctx.InventoryValuations.AddRange(valuations);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Seeded {N} inventory valuations", valuations.Length);

                await tx.CommitAsync();
                _logger.LogInformation(
                    "Inventory initialization complete for Company:{CompanyId}", companyId);
                return Result.Ok("Inventory data initialized successfully");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error during inventory initialization for Company:{CompanyId}", companyId);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize inventory for Company:{CompanyId}", companyId);
            return Result.Fail($"Failed to initialize inventory data: {ex.Message}");
        }
    }

    public async Task<bool> InventoryDataExistsAsync(Guid companyId) =>
        await _ctx.Units.AnyAsync(u => u.CompanyId == companyId);

    // ── private helpers ───────────────────────────────────────────────────────

    private static (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId)
        Unpack((Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) t) => t;

    private Unit Make<T>(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId) t,
        Func<(Guid companyId, Guid branchId, Guid businessUnitId, Guid userId), Unit> factory) =>
        factory(t);
}
