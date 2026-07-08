namespace Inventory.Application.DTOs;

/// <summary>
/// Inventory Balance DTO
/// </summary>
public class InventoryBalanceDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? BinId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public decimal QuantityAvailable { get; set; }
    public decimal AverageCost { get; set; }
    public decimal TotalValue { get; set; }
    public DateTime? LastTransactionDate { get; set; }
    public string CostingMethod { get; set; } = null!;
}

/// <summary>
/// Inventory Balance Report DTO
/// </summary>
public class InventoryBalanceReportDto
{
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = null!;
    public string ItemName { get; set; } = null!;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = null!;
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public decimal QuantityAvailable { get; set; }
    public decimal AverageCost { get; set; }
    public decimal TotalValue { get; set; }
    public DateTime? LastTransactionDate { get; set; }
}

/// <summary>
/// Per-item stock totals summed across all warehouses. Lightweight projection for the
/// items grid, which only needs on-hand/reserved/available per item (not per-warehouse rows).
/// Computed in a single GROUP BY query — no per-row item/warehouse lookups.
/// </summary>
public class ItemStockTotalDto
{
    public Guid ItemId { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public decimal QuantityAvailable { get; set; }
}

/// <summary>
/// Stock Valuation Report DTO
/// </summary>
public class StockValuationReportDto
{
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = null!;
    public string ItemName { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public decimal TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
    public decimal AverageCost { get; set; }
    public DateTime AsOfDate { get; set; }
}
