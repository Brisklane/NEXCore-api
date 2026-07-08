using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Events;

namespace Inventory.Infrastructure.Events;

/// <summary>
/// Resolves an item's default selling price and category for the Sales pricing engine.
/// Mirrors <see cref="ItemGlLookupHandler"/> — completes the request's TCS in-process.
/// </summary>
public class ItemPriceLookupHandler : IEventHandler<ItemPriceLookupEvent>
{
    private readonly InventoryDbContext _db;
    private readonly ILogger<ItemPriceLookupHandler> _logger;

    public ItemPriceLookupHandler(InventoryDbContext db, ILogger<ItemPriceLookupHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task HandleAsync(ItemPriceLookupEvent e, CancellationToken ct = default)
    {
        try
        {
            var item = await _db.Items
                .Include(i => i.Prices.Where(p => p.PriceList == "Default"))
                .FirstOrDefaultAsync(i => i.Id == e.ProductId, ct);

            if (item is null)
            {
                e.Result.TrySetResult(ItemPriceData.Empty);
                return;
            }

            var price = item.Prices.FirstOrDefault();
            // Prefer the net (tax-exclusive) sale price; fall back to the entered SalePrice.
            var basePrice = price is null
                ? 0m
                : price.SalePriceExcludingTax > 0 ? price.SalePriceExcludingTax : price.SalePrice;

            e.Result.TrySetResult(new ItemPriceData(
                BasePrice:     basePrice,
                CategoryId:    item.CategoryId,
                ProductCode:   item.Code,
                ProductName:   item.Name,
                UnitOfMeasure: null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ItemPriceLookupHandler failed for ProductId {ProductId}", e.ProductId);
            e.Result.TrySetResult(ItemPriceData.Empty);
        }
    }
}
