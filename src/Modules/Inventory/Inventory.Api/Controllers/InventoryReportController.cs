using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.Application.DTOs;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Api;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryReportController : ControllerBase
{
    private readonly IInventoryTransactionRepository _transactionRepository;
    private readonly IInventoryBalanceRepository _balanceRepository;
    private readonly IInventoryCostLayerRepository _costLayerRepository;
    private readonly IInventoryValuationRepository _valuationRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ILogger<InventoryReportController> _logger;

    public InventoryReportController(
        IInventoryTransactionRepository transactionRepository,
        IInventoryBalanceRepository balanceRepository,
        IInventoryCostLayerRepository costLayerRepository,
        IInventoryValuationRepository valuationRepository,
        IItemRepository itemRepository,
        IWarehouseRepository warehouseRepository,
        ILogger<InventoryReportController> logger)
    {
        _transactionRepository = transactionRepository;
        _balanceRepository = balanceRepository;
        _costLayerRepository = costLayerRepository;
        _valuationRepository = valuationRepository;
        _itemRepository = itemRepository;
        _warehouseRepository = warehouseRepository;
        _logger = logger;
    }

    /// <summary>Stock Ledger Report - All transactions for an item in a warehouse</summary>
    [HttpGet("stock-ledger")]
    [ProducesResponseType(typeof(ApiResponse<List<StockLedgerReportDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockLedger(
        [FromQuery] Guid itemId,
        [FromQuery] Guid warehouseId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var transactions = await _transactionRepository.GetStockLedgerAsync(itemId, warehouseId, fromDate, toDate);

            var ledgerDtos = new List<StockLedgerReportDto>();
            decimal runningBalance = 0;
            decimal runningValue = 0;

            foreach (var tx in transactions)
            {
                runningBalance += tx.Quantity;
                runningValue += tx.TotalCost;

                ledgerDtos.Add(new StockLedgerReportDto
                {
                    TransactionId = tx.Id,
                    TransactionDate = tx.TransactionDate,
                    DocumentNumber = tx.Document?.DocumentNumber ?? "N/A",
                    DocumentType = tx.Document?.DocumentType ?? "N/A",
                    TransactionType = tx.TransactionType,
                    Quantity = tx.Quantity,
                    UnitCost = tx.UnitCost,
                    TotalCost = tx.TotalCost,
                    RunningBalance = runningBalance,
                    RunningValue = runningValue
                });
            }

            return Ok(new ApiResponse<List<StockLedgerReportDto>>
            {
                Success = true,
                Data = ledgerDtos,
                Message = "Stock ledger retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating stock ledger report");
            return StatusCode(500, new ApiErrorResponse { Message = "Error generating report" });
        }
    }

    /// <summary>Stock Summary Report - Current balances by warehouse</summary>
    [HttpGet("stock-summary")]
    [ProducesResponseType(typeof(PaginatedResponse<InventoryBalanceReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockSummary([FromQuery] PaginationParams pagination, [FromQuery] Guid? warehouseId = null)
    {
        try
        {
            // Honor both the warehouse filter and the free-text search (item code/name) server-side
            // so they work together with pagination.
            var term    = pagination.SearchTerm?.Trim();
            var hasTerm  = !string.IsNullOrEmpty(term);
            var hasWh    = warehouseId.HasValue;
            var whId     = warehouseId ?? Guid.Empty;

            var desc = string.Equals(pagination.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            Func<IQueryable<Inventory.Domain.Entities.InventoryBalance>, IOrderedQueryable<Inventory.Domain.Entities.InventoryBalance>> orderBy =
                (pagination.SortBy ?? string.Empty).Trim().ToLower() switch
                {
                    "itemname"                                   => q => desc ? q.OrderByDescending(b => b.Item!.Name) : q.OrderBy(b => b.Item!.Name),
                    "quantityonhand" or "onhand"                 => q => desc ? q.OrderByDescending(b => b.QuantityOnHand) : q.OrderBy(b => b.QuantityOnHand),
                    "quantityreserved" or "reserved"             => q => desc ? q.OrderByDescending(b => b.QuantityReserved) : q.OrderBy(b => b.QuantityReserved),
                    "quantityavailable" or "available"           => q => desc ? q.OrderByDescending(b => b.QuantityAvailable) : q.OrderBy(b => b.QuantityAvailable),
                    "averagecost" or "avgcost"                   => q => desc ? q.OrderByDescending(b => b.AverageCost) : q.OrderBy(b => b.AverageCost),
                    "totalvalue"                                 => q => desc ? q.OrderByDescending(b => b.TotalValue) : q.OrderBy(b => b.TotalValue),
                    "lasttransactiondate" or "lasttransaction"   => q => desc ? q.OrderByDescending(b => b.LastTransactionDate) : q.OrderBy(b => b.LastTransactionDate),
                    _                                            => q => desc ? q.OrderByDescending(b => b.Item!.Code) : q.OrderBy(b => b.Item!.Code),
                };

            var (balances, total) = await _balanceRepository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                predicate: b =>
                    (!hasWh   || b.WarehouseId == whId)
                 && (!hasTerm || b.Item!.Code.Contains(term!) || b.Item!.Name.Contains(term!)),
                orderBy: orderBy);

            var reportDtos = new List<InventoryBalanceReportDto>();

            foreach (var balance in balances)
            {
                var item = await _itemRepository.GetByIdAsync(balance.ItemId);
                var warehouse = await _warehouseRepository.GetByIdAsync(balance.WarehouseId);

                if (item != null && warehouse != null)
                {
                    reportDtos.Add(new InventoryBalanceReportDto
                    {
                        ItemId = balance.ItemId,
                        ItemCode = item.Code,
                        ItemName = item.Name,
                        WarehouseId = balance.WarehouseId,
                        WarehouseName = warehouse.Name,
                        QuantityOnHand = balance.QuantityOnHand,
                        QuantityReserved = balance.QuantityReserved,
                        QuantityAvailable = balance.QuantityAvailable,
                        AverageCost = balance.AverageCost,
                        TotalValue = balance.TotalValue,
                        LastTransactionDate = balance.LastTransactionDate
                    });
                }
            }

            return Ok(PaginatedResponse<InventoryBalanceReportDto>.Ok(reportDtos, total, pagination.PageNumber, pagination.PageSize, "Stock summary retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating stock summary report");
            return StatusCode(500, new ApiErrorResponse { Message = "Error generating report" });
        }
    }

    /// <summary>
    /// Per-item stock totals (on-hand/reserved/available summed across all warehouses) in a single
    /// query. Backs the items grid so its stock columns populate in one fast call instead of paging
    /// through the full per-warehouse stock-summary report.
    /// </summary>
    [HttpGet("stock-by-item")]
    [ProducesResponseType(typeof(ApiResponse<List<ItemStockTotalDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockByItem([FromQuery] Guid? warehouseId = null)
    {
        try
        {
            var totals = await _balanceRepository.GetStockTotalsByItemAsync(warehouseId);
            return Ok(new ApiResponse<List<ItemStockTotalDto>>
            {
                Success = true,
                Data = totals,
                Message = "Per-item stock totals retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating per-item stock totals");
            return StatusCode(500, new ApiErrorResponse { Message = "Error generating report" });
        }
    }

    /// <summary>Stock Valuation Report - Total value by item</summary>
    [HttpGet("stock-valuation")]
    [ProducesResponseType(typeof(ApiResponse<List<StockValuationReportDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockValuation([FromQuery] DateTime? asOfDate = null)
    {
        try
        {
            var reportDate = asOfDate ?? DateTime.Now;
            var balances = await _balanceRepository.GetAllAsync();
            var reportDtos = new List<StockValuationReportDto>();

            foreach (var balance in balances)
            {
                var item = await _itemRepository.GetByIdAsync(balance.ItemId);
                if (item?.Category != null)
                {
                    reportDtos.Add(new StockValuationReportDto
                    {
                        ItemId = balance.ItemId,
                        ItemCode = item.Code,
                        ItemName = item.Name,
                        CategoryName = item.Category.Name,
                        TotalQuantity = balance.QuantityOnHand,
                        TotalValue = balance.TotalValue,
                        AverageCost = balance.AverageCost,
                        AsOfDate = reportDate
                    });
                }
            }

            return Ok(new ApiResponse<List<StockValuationReportDto>>
            {
                Success = true,
                Data = reportDtos,
                Message = "Stock valuation report retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating stock valuation report");
            return StatusCode(500, new ApiErrorResponse { Message = "Error generating report" });
        }
    }

    /// <summary>Inventory Aging Report - Age of inventory using FIFO cost layers</summary>
    [HttpGet("inventory-aging")]
    [ProducesResponseType(typeof(ApiResponse<List<InventoryAgingReportDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventoryAging([FromQuery] Guid? warehouseId = null)
    {
        try
        {
            var items = await _itemRepository.GetActiveItemsAsync();
            var warehouses = warehouseId.HasValue
                ? new[] { await _warehouseRepository.GetByIdAsync(warehouseId.Value) }.Where(w => w != null).Select(w => w!)
                : await _warehouseRepository.GetActiveWarehousesAsync();

            var reportDtos = new List<InventoryAgingReportDto>();

            foreach (var item in items)
            {
                foreach (var warehouse in warehouses)
                {
                    var costLayers = await _costLayerRepository.GetCostLayersForItemAsync(item.Id, warehouse.Id);

                    foreach (var layer in costLayers)
                    {
                        var daysInInventory = (int)(DateTime.Now - layer.ReceiptDate).TotalDays;

                        reportDtos.Add(new InventoryAgingReportDto
                        {
                            ItemId = item.Id,
                            ItemCode = item.Code,
                            ItemName = item.Name,
                            WarehouseId = warehouse.Id,
                            WarehouseName = warehouse.Name,
                            ReceiptDate = layer.ReceiptDate,
                            RemainingQuantity = layer.RemainingQuantity,
                            UnitCost = layer.UnitCost,
                            TotalValue = layer.RemainingQuantity * layer.UnitCost,
                            DaysInInventory = daysInInventory,
                            Status = daysInInventory switch { <= 30 => "New", <= 90 => "Aging", _ => "Old" }
                        });
                    }
                }
            }

            return Ok(new ApiResponse<List<InventoryAgingReportDto>>
            {
                Success = true,
                Data = reportDtos,
                Message = "Inventory aging report retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating inventory aging report");
            return StatusCode(500, new ApiErrorResponse { Message = "Error generating report" });
        }
    }

    /// <summary>Low Stock Alert Report</summary>
    [HttpGet("low-stock-alert")]
    [ProducesResponseType(typeof(ApiResponse<List<LowStockAlertDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLowStockAlert()
    {
        try
        {
            var balances = await _balanceRepository.GetAllAsync();
            var reportDtos = new List<LowStockAlertDto>();

            foreach (var balance in balances)
            {
                var item = await _itemRepository.GetByIdAsync(balance.ItemId);
                var warehouse = await _warehouseRepository.GetByIdAsync(balance.WarehouseId);

                if (item != null && warehouse != null && item.ReorderLevel.HasValue
                    && balance.QuantityOnHand <= item.ReorderLevel.Value)
                {
                    reportDtos.Add(new LowStockAlertDto
                    {
                        ItemId = item.Id,
                        ItemCode = item.Code,
                        ItemName = item.Name,
                        WarehouseId = warehouse.Id,
                        WarehouseName = warehouse.Name,
                        CurrentQuantity = balance.QuantityOnHand,
                        ReorderLevel = item.ReorderLevel.Value,
                        EconomicOrderQuantity = item.EconomicOrderQuantity ?? 0,
                        Status = balance.QuantityOnHand == 0 ? "Critical" : "Low"
                    });
                }
            }

            return Ok(new ApiResponse<List<LowStockAlertDto>>
            {
                Success = true,
                Data = reportDtos,
                Message = "Low stock alert retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating low stock alert");
            return StatusCode(500, new ApiErrorResponse { Message = "Error generating report" });
        }
    }
}
