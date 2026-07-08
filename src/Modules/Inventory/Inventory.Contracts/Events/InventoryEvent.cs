using Nexcore.SharedKernel.Events;

namespace Inventory.Contracts.Events;

/// <summary>
/// Base event for Inventory domain
/// </summary>
public abstract class InventoryEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Guid CompanyId { get; set; }
}

/// <summary>
/// Event raised when inventory document is posted
/// </summary>
public class InventoryDocumentPostedEvent : InventoryEvent
{
    public Guid DocumentId { get; set; }
    public string DocumentNumber { get; set; } = null!;
    public string DocumentType { get; set; } = null!;
    public DateTime PostingDate { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalCost { get; set; }
}

/// <summary>
/// Event raised when inventory balance changes
/// </summary>
public class InventoryBalanceChangedEvent : InventoryEvent
{
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal QuantityBefore { get; set; }
    public decimal QuantityAfter { get; set; }
    public decimal ValueBefore { get; set; }
    public decimal ValueAfter { get; set; }
}

/// <summary>
/// Event raised when stock falls below reorder level
/// </summary>
public class LowStockAlertEvent : InventoryEvent
{
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = null!;
    public Guid WarehouseId { get; set; }
    public decimal CurrentQuantity { get; set; }
    public decimal ReorderLevel { get; set; }
}

/// <summary>
/// Event raised when item is created
/// </summary>
public class ItemCreatedEvent : InventoryEvent
{
    public Guid ItemId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
}

/// <summary>
/// Event raised when warehouse is created
/// </summary>
public class WarehouseCreatedEvent : InventoryEvent
{
    public Guid WarehouseId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
}
