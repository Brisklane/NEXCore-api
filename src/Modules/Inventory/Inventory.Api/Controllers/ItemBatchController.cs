using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.Application.DTOs;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Api;
using Inv = Inventory.Domain.Constants;

namespace Inventory.Api.Controllers;

/// <summary>
/// Batches / lots (ItemBatch) — for lot-tracked items (pharma, food, chemicals).
/// Read an item's batches, a near-expiry report, and change a batch's status.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ItemBatchController : ControllerBase
{
    private readonly IItemBatchRepository _batches;
    private readonly ILogger<ItemBatchController> _logger;

    public ItemBatchController(IItemBatchRepository batches, ILogger<ItemBatchController> logger)
    {
        _batches = batches;
        _logger = logger;
    }

    /// <summary>All batches for an item (soonest expiry first).</summary>
    [HttpGet("by-item/{itemId}")]
    [ProducesResponseType(typeof(ApiResponse<List<ItemBatchDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByItem(Guid itemId)
    {
        try
        {
            var batches = await _batches.GetByItemAsync(itemId);
            return Ok(new ApiResponse<List<ItemBatchDto>> { Success = true, Data = batches.Select(MapToDto).ToList(), Message = "Batches retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving batches for item {ItemId}", itemId); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving batches" }); }
    }

    /// <summary>Get a single batch by ID.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ItemBatchDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var batch = await _batches.GetByIdAsync(id);
            if (batch == null) return NotFound(new ApiErrorResponse { Message = "Batch not found" });
            return Ok(new ApiResponse<ItemBatchDto> { Success = true, Data = MapToDto(batch), Message = "Batch retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving batch {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving batch" }); }
    }

    /// <summary>Active batches expiring within the next N days (default 30).</summary>
    [HttpGet("report/expiring")]
    [ProducesResponseType(typeof(ApiResponse<List<ItemBatchDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Expiring([FromQuery] int days = 30)
    {
        try
        {
            var batches = await _batches.GetExpiringAsync(days);
            return Ok(new ApiResponse<List<ItemBatchDto>> { Success = true, Data = batches.Select(MapToDto).ToList(), Message = "Report generated" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error generating expiring-batches report"); return StatusCode(500, new ApiErrorResponse { Message = "Error generating report" }); }
    }

    /// <summary>Change a batch's status (Quarantine → Active, Recalled, …).</summary>
    [HttpPut("{id}/status")]
    [ProducesResponseType(typeof(ApiResponse<ItemBatchDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateBatchStatusDto dto)
    {
        try
        {
            if (!Inv.BatchStatus.All.Contains(dto.Status))
                return BadRequest(new ApiErrorResponse { Message = "Invalid status" });

            var batch = await _batches.GetByIdAsync(id);
            if (batch == null) return NotFound(new ApiErrorResponse { Message = "Batch not found" });

            batch.Status = dto.Status;
            _batches.Update(batch);
            await _batches.SaveChangesAsync();
            return Ok(new ApiResponse<ItemBatchDto> { Success = true, Data = MapToDto(batch), Message = "Status updated" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating status for batch {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating status" }); }
    }

    private static ItemBatchDto MapToDto(ItemBatch b) => new()
    {
        Id = b.Id,
        ItemId = b.ItemId,
        ItemCode = b.Item?.Code,
        ItemName = b.Item?.Name,
        VariantId = b.VariantId,
        BatchNumber = b.BatchNumber,
        ManufactureDate = b.ManufactureDate,
        ExpiryDate = b.ExpiryDate,
        SupplierId = b.SupplierId,
        ReceivedQuantity = b.ReceivedQuantity,
        RemainingQuantity = b.RemainingQuantity,
        ReservedQuantity = b.ReservedQuantity,
        UnitCost = b.UnitCost,
        Status = b.Status,
        DaysToExpiry = b.ExpiryDate.HasValue ? (int)Math.Floor((b.ExpiryDate.Value - DateTime.UtcNow).TotalDays) : null,
        CreatedAt = b.CreatedAt,
    };
}
