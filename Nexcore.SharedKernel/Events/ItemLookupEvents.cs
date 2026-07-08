namespace Nexcore.SharedKernel.Events;

/// <summary>
/// GL account IDs and unit cost resolved from an Inventory item.
/// Null fields mean the item has no override — the accounting handler will fall back to posting profiles.
/// </summary>
public record ItemGlData(
    Guid? InventoryAccountId,
    Guid? CogsAccountId,
    Guid? SalesAccountId,
    decimal UnitCost)
{
    public static readonly ItemGlData Empty = new(null, null, null, 0m);
}

/// <summary>
/// Published by any module that needs an item's GL account IDs and unit cost.
/// Inventory.Infrastructure handles this event and completes <see cref="Result"/>.
/// Uses the TCS request/response pattern — the caller awaits <see cref="Result.Task"/>
/// within the same request without polling.
/// </summary>
public class ItemGlLookupEvent
{
    public Guid ProductId { get; init; }

    /// <summary>Completed by the Inventory handler with the resolved GL data.</summary>
    public TaskCompletionSource<ItemGlData> Result { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}

/// <summary>
/// Catalog price + classification resolved from an Inventory item — used by the Sales
/// pricing engine to resolve a server-authoritative price and to match category-wide promotions.
/// Null/zero fields mean the item was not found or has no default price set.
/// </summary>
public record ItemPriceData(
    decimal BasePrice,
    Guid? CategoryId,
    string? ProductCode,
    string? ProductName,
    string? UnitOfMeasure)
{
    public static readonly ItemPriceData Empty = new(0m, null, null, null, null);
}

/// <summary>
/// Published by the Sales pricing engine to fetch an item's default selling price and category.
/// Inventory.Infrastructure handles this event and completes <see cref="Result"/>.
/// Same TCS request/response pattern as <see cref="ItemGlLookupEvent"/>.
/// </summary>
public class ItemPriceLookupEvent
{
    public Guid ProductId { get; init; }

    /// <summary>Completed by the Inventory handler with the resolved price/category data.</summary>
    public TaskCompletionSource<ItemPriceData> Result { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}

/// <summary>
/// Current physical on-hand for a product (and optional variant) in a warehouse, summed across bins.
/// <see cref="Resolved"/> is false when the lookup could not be answered (handler error / not loaded);
/// callers should fail-open on an unresolved result rather than block a sale on a transient failure.
/// </summary>
public record StockAvailabilityData(decimal QuantityOnHand, bool Resolved)
{
    /// <summary>Lookup could not be answered — treat as "unknown", do not enforce.</summary>
    public static readonly StockAvailabilityData Unresolved = new(0m, false);
}

/// <summary>
/// Published by Sales (POS checkout) to ask Inventory how much stock is on hand before settling a sale.
/// Inventory.Infrastructure handles this event and completes <see cref="Result"/>.
/// Same TCS request/response pattern as <see cref="ItemGlLookupEvent"/>.
/// </summary>
public class StockAvailabilityLookupEvent
{
    public Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public Guid WarehouseId { get; init; }
    public Guid CompanyId { get; init; }

    /// <summary>Completed by the Inventory handler with the current on-hand quantity.</summary>
    public TaskCompletionSource<StockAvailabilityData> Result { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
