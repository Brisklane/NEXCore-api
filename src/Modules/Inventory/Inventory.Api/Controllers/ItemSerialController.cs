using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.Application.DTOs;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Api;
using Inv = Inventory.Domain.Constants;

namespace Inventory.Api.Controllers;

/// <summary>
/// Serialized units (ItemSerial) — the per-physical-unit registry for serial/IMEI-tracked items.
/// Read the units of an item, scan-lookup a serial/IMEI globally, view a unit's history, bulk-generate
/// serials, and change a unit's lifecycle status.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ItemSerialController : ControllerBase
{
    private readonly IItemSerialRepository _serials;
    private readonly IItemRepository _items;
    private readonly ILogger<ItemSerialController> _logger;

    public ItemSerialController(IItemSerialRepository serials, IItemRepository items, ILogger<ItemSerialController> logger)
    {
        _serials = serials;
        _items = items;
        _logger = logger;
    }

    /// <summary>Serialized units for an item (paged; optional status + serial/IMEI search).</summary>
    [HttpGet("by-item/{itemId}")]
    [ProducesResponseType(typeof(PaginatedResponse<ItemSerialDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByItem(Guid itemId, [FromQuery] PaginationParams pagination,
        [FromQuery] string? status = null)
    {
        try
        {
            var (items, total) = await _serials.GetByItemPagedAsync(
                itemId, pagination.PageNumber, pagination.PageSize, status, pagination.SearchTerm);
            return Ok(PaginatedResponse<ItemSerialDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving serials for item {ItemId}", itemId); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving serials" }); }
    }

    /// <summary>Global scan lookup by serial number OR any IMEI. Returns the unit + its full history.</summary>
    [HttpGet("lookup/{value}")]
    [ProducesResponseType(typeof(ApiResponse<SerialLookupResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Lookup(string value)
    {
        try
        {
            var serial = await _serials.GetBySerialOrImeiAsync(value);
            if (serial == null)
                return Ok(new ApiResponse<SerialLookupResultDto> { Success = true, Data = new SerialLookupResultDto { Found = false }, Message = "No unit found" });

            var history = await _serials.GetHistoryAsync(serial.Id);
            return Ok(new ApiResponse<SerialLookupResultDto>
            {
                Success = true,
                Data = new SerialLookupResultDto
                {
                    Found = true,
                    Serial = MapToDto(serial),
                    History = history.Select(MapHistory).ToList(),
                },
                Message = "Unit found"
            });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error looking up serial {Value}", value); return StatusCode(500, new ApiErrorResponse { Message = "Error looking up serial" }); }
    }

    /// <summary>Get a single unit by ID.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ItemSerialDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var serial = await _serials.GetByIdAsync(id);
            if (serial == null) return NotFound(new ApiErrorResponse { Message = "Serial not found" });
            return Ok(new ApiResponse<ItemSerialDto> { Success = true, Data = MapToDto(serial), Message = "Serial retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving serial {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving serial" }); }
    }

    /// <summary>The audit trail (received → sold → returned → …) for a unit.</summary>
    [HttpGet("{id}/history")]
    [ProducesResponseType(typeof(ApiResponse<List<ItemSerialHistoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(Guid id)
    {
        try
        {
            var history = await _serials.GetHistoryAsync(id);
            return Ok(new ApiResponse<List<ItemSerialHistoryDto>> { Success = true, Data = history.Select(MapHistory).ToList(), Message = "History retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving history for serial {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving history" }); }
    }

    /// <summary>Manually register a single serialized unit (outside a receipt flow).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ItemSerialDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateItemSerialDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.SerialNumber))
                return BadRequest(new ApiErrorResponse { Message = "Serial number is required" });

            var existing = await _serials.GetBySerialNumberAsync(dto.ItemId, dto.SerialNumber.Trim());
            if (existing != null)
                return BadRequest(new ApiErrorResponse { Message = "A unit with this serial number already exists for the item" });

            var serial = new ItemSerial
            {
                ItemId = dto.ItemId,
                VariantId = dto.VariantId,
                SerialNumber = dto.SerialNumber.Trim(),
                Imei = dto.Imei,
                Imei2 = dto.Imei2,
                MacAddress = dto.MacAddress,
                Status = Inv.SerialStatus.InStock,
                WarehouseId = dto.WarehouseId,
                BinId = dto.BinId,
                UnitCost = dto.UnitCost,
                SupplierId = dto.SupplierId,
                WarrantyStartDate = dto.WarrantyStartDate,
                WarrantyEndDate = dto.WarrantyEndDate,
                Notes = dto.Notes,
                ReceiptDate = DateTime.UtcNow,
            };
            await _serials.AddAsync(serial);
            await _serials.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = serial.Id },
                new ApiResponse<ItemSerialDto> { Success = true, Data = MapToDto(serial), Message = "Serial registered" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating serial"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating serial" }); }
    }

    /// <summary>Bulk-generate a run of serials from prefix + start number (e.g. SN-00001 … SN-00020).</summary>
    [HttpPost("bulk-generate")]
    [ProducesResponseType(typeof(ApiResponse<List<ItemSerialDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkGenerate([FromBody] BulkGenerateSerialsDto dto)
    {
        try
        {
            if (dto.Count < 1 || dto.Count > 5000)
                return BadRequest(new ApiErrorResponse { Message = "Count must be between 1 and 5000" });

            var item = await _items.GetByIdAsync(dto.ItemId);
            if (item == null) return BadRequest(new ApiErrorResponse { Message = "Item not found" });

            var candidates = new List<string>(dto.Count);
            for (var i = 0; i < dto.Count; i++)
            {
                var num = (dto.StartNumber + i).ToString().PadLeft(Math.Max(0, dto.Padding), '0');
                candidates.Add($"{dto.Prefix}{num}");
            }

            var clash = await _serials.GetExistingSerialNumbersAsync(dto.ItemId, candidates);
            var toCreate = candidates.Where(c => !clash.Contains(c)).ToList();
            if (toCreate.Count == 0)
                return BadRequest(new ApiErrorResponse { Message = "All generated serial numbers already exist for this item" });

            var serials = toCreate.Select(sn => new ItemSerial
            {
                ItemId = dto.ItemId,
                VariantId = dto.VariantId,
                SerialNumber = sn,
                Status = Inv.SerialStatus.InStock,
                WarehouseId = dto.WarehouseId,
                UnitCost = dto.UnitCost,
                ReceiptDate = DateTime.UtcNow,
            }).ToList();

            await _serials.AddRangeAsync(serials);
            await _serials.SaveChangesAsync();

            return Ok(new ApiResponse<List<ItemSerialDto>>
            {
                Success = true,
                Data = serials.Select(MapToDto).ToList(),
                Message = $"Generated {serials.Count} serial(s)" + (clash.Count > 0 ? $" ({clash.Count} skipped as duplicates)" : "")
            });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error bulk-generating serials"); return StatusCode(500, new ApiErrorResponse { Message = "Error generating serials" }); }
    }

    /// <summary>Change a unit's lifecycle status (Defective, Returned, Scrapped, …).</summary>
    [HttpPut("{id}/status")]
    [ProducesResponseType(typeof(ApiResponse<ItemSerialDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateSerialStatusDto dto)
    {
        try
        {
            if (!Inv.SerialStatus.All.Contains(dto.Status))
                return BadRequest(new ApiErrorResponse { Message = "Invalid status" });

            var serial = await _serials.UpdateStatusAsync(id, dto.Status, dto.Notes);
            if (serial == null) return NotFound(new ApiErrorResponse { Message = "Serial not found" });
            await _serials.SaveChangesAsync();
            return Ok(new ApiResponse<ItemSerialDto> { Success = true, Data = MapToDto(serial), Message = "Status updated" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating status for serial {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating status" }); }
    }

    /// <summary>Units whose warranty ends within the next N days (default 30).</summary>
    [HttpGet("report/warranty-expiring")]
    [ProducesResponseType(typeof(ApiResponse<List<ItemSerialDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> WarrantyExpiring([FromQuery] int days = 30)
    {
        try
        {
            var units = await _serials.GetWarrantyExpiringAsync(days);
            return Ok(new ApiResponse<List<ItemSerialDto>> { Success = true, Data = units.Select(MapToDto).ToList(), Message = "Report generated" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error generating warranty-expiring report"); return StatusCode(500, new ApiErrorResponse { Message = "Error generating report" }); }
    }

    private static ItemSerialDto MapToDto(ItemSerial s) => new()
    {
        Id = s.Id,
        ItemId = s.ItemId,
        ItemCode = s.Item?.Code,
        ItemName = s.Item?.Name,
        VariantId = s.VariantId,
        SerialNumber = s.SerialNumber,
        Imei = s.Imei,
        Imei2 = s.Imei2,
        MacAddress = s.MacAddress,
        Status = s.Status,
        WarehouseId = s.WarehouseId,
        WarehouseName = s.Warehouse?.Name,
        BinId = s.BinId,
        UnitCost = s.UnitCost,
        ReceiptDocumentId = s.ReceiptDocumentId,
        ReceiptDate = s.ReceiptDate,
        SupplierId = s.SupplierId,
        WarrantyStartDate = s.WarrantyStartDate,
        WarrantyEndDate = s.WarrantyEndDate,
        SoldDocumentId = s.SoldDocumentId,
        SoldDate = s.SoldDate,
        SalesReference = s.SalesReference,
        Notes = s.Notes,
        CreatedAt = s.CreatedAt,
    };

    private static ItemSerialHistoryDto MapHistory(ItemSerialHistory h) => new()
    {
        Id = h.Id,
        ItemSerialId = h.ItemSerialId,
        EventType = h.EventType,
        FromStatus = h.FromStatus,
        ToStatus = h.ToStatus,
        WarehouseId = h.WarehouseId,
        DocumentId = h.DocumentId,
        EventDate = h.EventDate,
        Notes = h.Notes,
    };
}
