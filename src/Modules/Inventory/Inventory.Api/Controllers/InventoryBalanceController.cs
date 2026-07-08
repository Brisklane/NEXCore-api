using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.Application.DTOs;
using Inventory.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Api;

namespace Inventory.Api.Controllers;

/// <summary>
/// Inventory Balances - current stock levels
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryBalanceController : ControllerBase
{
    private readonly IInventoryBalanceRepository _balanceRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ILogger<InventoryBalanceController> _logger;

    public InventoryBalanceController(
        IInventoryBalanceRepository balanceRepository,
        IItemRepository itemRepository,
        IWarehouseRepository warehouseRepository,
        ILogger<InventoryBalanceController> logger)
    {
        _balanceRepository = balanceRepository;
        _itemRepository = itemRepository;
        _warehouseRepository = warehouseRepository;
        _logger = logger;
    }

    /// <summary>Get inventory balance by item and warehouse</summary>
    [HttpGet("by-item-warehouse")]
    [ProducesResponseType(typeof(ApiResponse<InventoryBalanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBalance([FromQuery] Guid itemId, [FromQuery] Guid warehouseId, [FromQuery] Guid? binId = null, [FromQuery] Guid? variantId = null)
    {
        try
        {
            var balance = await _balanceRepository.GetBalanceAsync(itemId, warehouseId, binId, variantId);
            if (balance == null)
                return NotFound(new ApiErrorResponse { Message = "Inventory balance not found" });

            return Ok(new ApiResponse<InventoryBalanceDto>
            {
                Success = true,
                Data = MapToDto(balance),
                Message = "Inventory balance retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory balance");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving inventory balance" });
        }
    }

    /// <summary>Get balances for an item across all warehouses</summary>
    [HttpGet("by-item/{itemId}")]
    [ProducesResponseType(typeof(PaginatedResponse<InventoryBalanceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalancesByItem(Guid itemId, [FromQuery] PaginationParams pagination)
    {
        try
        {
            var (balances, total) = await _balanceRepository.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: b => b.ItemId == itemId);
            return Ok(PaginatedResponse<InventoryBalanceDto>.Ok(balances.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving item balances");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving item balances" });
        }
    }

    /// <summary>Get all balances in a warehouse</summary>
    [HttpGet("by-warehouse/{warehouseId}")]
    [ProducesResponseType(typeof(PaginatedResponse<InventoryBalanceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalancesByWarehouse(Guid warehouseId, [FromQuery] PaginationParams pagination)
    {
        try
        {
            var (balances, total) = await _balanceRepository.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: b => b.WarehouseId == warehouseId);
            return Ok(PaginatedResponse<InventoryBalanceDto>.Ok(balances.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving warehouse balances");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving warehouse balances" });
        }
    }

    /// <summary>Get low stock items</summary>
    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(ApiResponse<List<InventoryBalanceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLowStock([FromQuery] decimal? threshold = null)
    {
        try
        {
            var balances = await _balanceRepository.GetLowStockAsync(threshold);

            return Ok(new ApiResponse<List<InventoryBalanceDto>>
            {
                Success = true,
                Data = balances.Select(MapToDto).ToList(),
                Message = "Low stock items retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving low stock items");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving low stock items" });
        }
    }

    /// <summary>Get total inventory value for a warehouse</summary>
    [HttpGet("total-value/{warehouseId}")]
    [ProducesResponseType(typeof(ApiResponse<decimal>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTotalValue(Guid warehouseId)
    {
        try
        {
            var totalValue = await _balanceRepository.GetTotalValueAsync(warehouseId);

            return Ok(new ApiResponse<decimal>
            {
                Success = true,
                Data = totalValue,
                Message = "Total inventory value retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving total inventory value");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving total inventory value" });
        }
    }

    private static InventoryBalanceDto MapToDto(Inventory.Domain.Entities.InventoryBalance b) => new()
    {
        Id = b.Id,
        ItemId = b.ItemId,
        WarehouseId = b.WarehouseId,
        BinId = b.BinId,
        VariantId = b.VariantId,
        QuantityOnHand = b.QuantityOnHand,
        QuantityReserved = b.QuantityReserved,
        QuantityAvailable = b.QuantityAvailable,
        AverageCost = b.AverageCost,
        TotalValue = b.TotalValue,
        LastTransactionDate = b.LastTransactionDate,
        CostingMethod = b.CostingMethod
    };
}
