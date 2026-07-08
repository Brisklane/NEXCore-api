using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.Application.DTOs;
using Inventory.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Inventory.Api.Controllers;

/// <summary>Warehouse management (warehouses + bins)</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WarehouseController : ControllerBase
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IBinRepository _binRepository;
    private readonly ILogger<WarehouseController> _logger;

    public WarehouseController(IWarehouseRepository warehouseRepository, IBinRepository binRepository, ILogger<WarehouseController> logger)
    {
        _warehouseRepository = warehouseRepository;
        _binRepository = binRepository;
        _logger = logger;
    }

    // ── Warehouse CRUD ────────────────────────────────────────────────────

    /// <summary>Get all warehouses</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<WarehouseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination, [FromQuery] bool? isActive)
    {
        try
        {
            var search = pagination.SearchTerm?.Trim().ToLower();
            var (warehouses, total) = await _warehouseRepository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                predicate: w =>
                    (isActive == null || w.IsActive == isActive) &&
                    (string.IsNullOrEmpty(search) ||
                        (w.Code != null && w.Code.ToLower().Contains(search)) ||
                        (w.Name != null && w.Name.ToLower().Contains(search)) ||
                        (w.Description != null && w.Description.ToLower().Contains(search)) ||
                        (w.City != null && w.City.ToLower().Contains(search)) ||
                        (w.Country != null && w.Country.ToLower().Contains(search)) ||
                        (w.WarehouseType != null && w.WarehouseType.ToLower().Contains(search))),
                orderBy: q => q.ApplyOrderNewestFirst(pagination.SortBy, pagination.SortDirection, "Code"));
            return Ok(PaginatedResponse<WarehouseDto>.Ok(warehouses.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving warehouses"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving warehouses" }); }
    }

    /// <summary>Get warehouse by ID</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var warehouse = await _warehouseRepository.GetByIdAsync(id);
            if (warehouse == null) return NotFound(new ApiErrorResponse { Message = "Warehouse not found" });
            return Ok(new ApiResponse<WarehouseDto> { Success = true, Data = MapToDto(warehouse), Message = "Warehouse retrieved successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving warehouse {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving warehouse" }); }
    }

    /// <summary>Get active warehouses</summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(PaginatedResponse<WarehouseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive([FromQuery] PaginationParams pagination)
    {
        try
        {
            var (warehouses, total) = await _warehouseRepository.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: w => w.IsActive);
            return Ok(PaginatedResponse<WarehouseDto>.Ok(warehouses.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving active warehouses"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving active warehouses" }); }
    }

    /// <summary>Get warehouse with all bins</summary>
    [HttpGet("{id}/with-bins")]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWithBins(Guid id)
    {
        try
        {
            var warehouse = await _warehouseRepository.GetWithBinsAsync(id);
            if (warehouse == null) return NotFound(new ApiErrorResponse { Message = "Warehouse not found" });
            return Ok(new ApiResponse<WarehouseDto> { Success = true, Data = MapToDto(warehouse), Message = "Warehouse retrieved successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving warehouse with bins {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving warehouse" }); }
    }

    /// <summary>Create warehouse</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateWarehouseDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var existing = await _warehouseRepository.GetByCodeAsync(dto.Code);
            if (existing != null) return BadRequest(new ApiErrorResponse { Message = "Warehouse code already exists" });

            var warehouse = new Inventory.Domain.Entities.Warehouse
            {
                Code = dto.Code, Name = dto.Name, Address = dto.Address, City = dto.City,
                Region = dto.Region, PostalCode = dto.PostalCode, Country = dto.Country,
                WarehouseType = dto.WarehouseType, IsActive = true
            };
            await _warehouseRepository.AddAsync(warehouse);
            await _warehouseRepository.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = warehouse.Id },
                new ApiResponse<WarehouseDto> { Success = true, Data = MapToDto(warehouse), Message = "Warehouse created successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating warehouse"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating warehouse" }); }
    }

    /// <summary>Update warehouse</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWarehouseDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var warehouse = await _warehouseRepository.GetByIdAsync(id);
            if (warehouse == null) return NotFound(new ApiErrorResponse { Message = "Warehouse not found" });

            if (dto.Code != null && dto.Code != warehouse.Code)
            {
                var dup = await _warehouseRepository.GetByCodeAsync(dto.Code);
                if (dup != null) return BadRequest(new ApiErrorResponse { Message = "Warehouse code already exists" });
                warehouse.Code = dto.Code;
            }
            if (dto.Name          != null) warehouse.Name          = dto.Name;
            if (dto.Address       != null) warehouse.Address       = dto.Address;
            if (dto.City          != null) warehouse.City          = dto.City;
            if (dto.Region        != null) warehouse.Region        = dto.Region;
            if (dto.PostalCode    != null) warehouse.PostalCode    = dto.PostalCode;
            if (dto.Country       != null) warehouse.Country       = dto.Country;
            if (dto.WarehouseType != null) warehouse.WarehouseType = dto.WarehouseType;
            if (dto.IsActive      != null) warehouse.IsActive      = dto.IsActive.Value;

            _warehouseRepository.Update(warehouse);
            await _warehouseRepository.SaveChangesAsync();
            return Ok(new ApiResponse<WarehouseDto> { Success = true, Data = MapToDto(warehouse), Message = "Warehouse updated successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating warehouse {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating warehouse" }); }
    }

    /// <summary>Delete warehouse (soft-delete)</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var warehouse = await _warehouseRepository.GetByIdAsync(id);
            if (warehouse == null) return NotFound(new ApiErrorResponse { Message = "Warehouse not found" });
            _warehouseRepository.SoftDelete(warehouse);
            await _warehouseRepository.SaveChangesAsync();
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Warehouse deleted successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting warehouse {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting warehouse" }); }
    }

    // ── Bin sub-resource ──────────────────────────────────────────────────

    /// <summary>Get all bins for a warehouse</summary>
    [HttpGet("{warehouseId}/bins")]
    [ProducesResponseType(typeof(PaginatedResponse<BinDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBins(Guid warehouseId, [FromQuery] PaginationParams pagination)
    {
        try
        {
            var (bins, total) = await _binRepository.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: b => b.WarehouseId == warehouseId);
            return Ok(PaginatedResponse<BinDto>.Ok(bins.Select(MapBinToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving bins for warehouse {Id}", warehouseId); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving bins" }); }
    }

    /// <summary>Get active bins for a warehouse</summary>
    [HttpGet("{warehouseId}/bins/active")]
    [ProducesResponseType(typeof(PaginatedResponse<BinDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveBins(Guid warehouseId, [FromQuery] PaginationParams pagination)
    {
        try
        {
            var (bins, total) = await _binRepository.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: b => b.WarehouseId == warehouseId && b.IsActive);
            return Ok(PaginatedResponse<BinDto>.Ok(bins.Select(MapBinToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving active bins for warehouse {Id}", warehouseId); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving bins" }); }
    }

    /// <summary>Get bin by ID</summary>
    [HttpGet("bins/{id}")]
    [ProducesResponseType(typeof(ApiResponse<BinDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBinById(Guid id)
    {
        try
        {
            var bin = await _binRepository.GetByIdAsync(id);
            if (bin == null) return NotFound(new ApiErrorResponse { Message = "Bin not found" });
            return Ok(new ApiResponse<BinDto> { Success = true, Data = MapBinToDto(bin), Message = "Bin retrieved successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving bin {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving bin" }); }
    }

    /// <summary>Create bin</summary>
    [HttpPost("{warehouseId}/bins")]
    [ProducesResponseType(typeof(ApiResponse<BinDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateBin(Guid warehouseId, [FromBody] CreateBinDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var warehouse = await _warehouseRepository.GetByIdAsync(warehouseId);
            if (warehouse == null) return NotFound(new ApiErrorResponse { Message = "Warehouse not found" });

            var existing = await _binRepository.GetByCodeAsync(dto.Code, warehouseId);
            if (existing != null) return BadRequest(new ApiErrorResponse { Message = "Bin code already exists in this warehouse" });

            var bin = new Inventory.Domain.Entities.Bin
            {
                WarehouseId = warehouseId, Code = dto.Code, Name = dto.Name,
                Aisle = dto.Aisle, Rack = dto.Rack, Level = dto.Level,
                Position = dto.Position, Capacity = dto.Capacity, IsActive = true
            };
            await _binRepository.AddAsync(bin);
            await _binRepository.SaveChangesAsync();
            return StatusCode(201, new ApiResponse<BinDto> { Success = true, Data = MapBinToDto(bin), Message = "Bin created successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating bin for warehouse {Id}", warehouseId); return StatusCode(500, new ApiErrorResponse { Message = "Error creating bin" }); }
    }

    /// <summary>Update bin</summary>
    [HttpPut("bins/{id}")]
    [ProducesResponseType(typeof(ApiResponse<BinDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateBin(Guid id, [FromBody] UpdateBinDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var bin = await _binRepository.GetByIdAsync(id);
            if (bin == null) return NotFound(new ApiErrorResponse { Message = "Bin not found" });

            if (dto.Code     != null) bin.Code     = dto.Code;
            if (dto.Name     != null) bin.Name     = dto.Name;
            if (dto.Aisle    != null) bin.Aisle    = dto.Aisle;
            if (dto.Rack     != null) bin.Rack     = dto.Rack;
            if (dto.Level    != null) bin.Level    = dto.Level;
            if (dto.Position != null) bin.Position = dto.Position;
            if (dto.Capacity != null) bin.Capacity = dto.Capacity;
            if (dto.IsActive != null) bin.IsActive = dto.IsActive.Value;

            _binRepository.Update(bin);
            await _binRepository.SaveChangesAsync();
            return Ok(new ApiResponse<BinDto> { Success = true, Data = MapBinToDto(bin), Message = "Bin updated successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating bin {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating bin" }); }
    }

    /// <summary>Delete bin (soft-delete)</summary>
    [HttpDelete("bins/{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteBin(Guid id)
    {
        try
        {
            var bin = await _binRepository.GetByIdAsync(id);
            if (bin == null) return NotFound(new ApiErrorResponse { Message = "Bin not found" });
            _binRepository.SoftDelete(bin);
            await _binRepository.SaveChangesAsync();
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Bin deleted successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting bin {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting bin" }); }
    }

    private static WarehouseDto MapToDto(Inventory.Domain.Entities.Warehouse w) => new()
    {
        Id = w.Id, Code = w.Code, Name = w.Name, Address = w.Address,
        City = w.City, Region = w.Region, PostalCode = w.PostalCode,
        Country = w.Country, IsActive = w.IsActive, WarehouseType = w.WarehouseType
    };

    private static BinDto MapBinToDto(Inventory.Domain.Entities.Bin b) => new()
    {
        Id = b.Id, WarehouseId = b.WarehouseId, Code = b.Code, Name = b.Name,
        Aisle = b.Aisle, Rack = b.Rack, Level = b.Level, Position = b.Position,
        Capacity = b.Capacity, IsActive = b.IsActive
    };
}
