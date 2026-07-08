using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Events;

namespace Inventory.Infrastructure.Events;

public class ItemGlLookupHandler : IEventHandler<ItemGlLookupEvent>
{
    private readonly InventoryDbContext _db;
    private readonly ILogger<ItemGlLookupHandler> _logger;

    public ItemGlLookupHandler(InventoryDbContext db, ILogger<ItemGlLookupHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task HandleAsync(ItemGlLookupEvent e, CancellationToken ct = default)
    {
        try
        {
            var item = await _db.Items
                .Include(i => i.Category)
                .Include(i => i.Prices.Where(p => p.PriceList == "Default"))
                .FirstOrDefaultAsync(i => i.Id == e.ProductId, ct);

            if (item is null)
            {
                e.Result.TrySetResult(ItemGlData.Empty);
                return;
            }

            e.Result.TrySetResult(new ItemGlData(
                InventoryAccountId: item.EffectiveInventoryAccountId,
                CogsAccountId: item.EffectiveCogsAccountId,
                SalesAccountId: item.EffectiveSalesAccountId,
                UnitCost: item.Prices.FirstOrDefault()?.PurchasePrice ?? 0m));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ItemGlLookupHandler failed for ProductId {ProductId}", e.ProductId);
            e.Result.TrySetResult(ItemGlData.Empty);
        }
    }
}
