namespace Inventory.Application.DTOs;

/// <summary>
/// Stock Ledger Report DTO
/// Shows all transactions for an item
/// </summary>
public class StockLedgerReportDto
{
    public Guid TransactionId { get; set; }
    public DateTime TransactionDate { get; set; }
    public string DocumentNumber { get; set; } = null!;
    public string DocumentType { get; set; } = null!;
    public string TransactionType { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal RunningBalance { get; set; }
    public decimal RunningValue { get; set; }
    public string? Reference { get; set; }
}

/// <summary>
/// Inventory Aging Report DTO
/// Shows age of inventory (FIFO layers)
/// </summary>
public class InventoryAgingReportDto
{
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = null!;
    public string ItemName { get; set; } = null!;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = null!;
    public DateTime ReceiptDate { get; set; }
    public decimal RemainingQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue { get; set; }
    public int DaysInInventory { get; set; }
    public string? Status { get; set; } // New, Aging, Old
}

/// <summary>
/// Low Stock Alert DTO
/// </summary>
public class LowStockAlertDto
{
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = null!;
    public string ItemName { get; set; } = null!;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = null!;
    public decimal CurrentQuantity { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal EconomicOrderQuantity { get; set; }
    public string Status { get; set; } = null!; // Critical, Low
}
