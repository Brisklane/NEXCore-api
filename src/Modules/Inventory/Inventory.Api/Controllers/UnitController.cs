using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.Application.DTOs;
using Inventory.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Inventory.Api.Controllers;

/// <summary>Units of Measure (UOM) management</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UnitController : ControllerBase
{
    private readonly IUnitRepository _unitRepository;
    private readonly IItemUomConversionRepository _conversionRepository;
    private readonly ILogger<UnitController> _logger;

    public UnitController(
        IUnitRepository unitRepository,
        IItemUomConversionRepository conversionRepository,
        ILogger<UnitController> logger)
    {
        _unitRepository = unitRepository;
        _conversionRepository = conversionRepository;
        _logger = logger;
    }

    // ?? Unit CRUD ?????????????????????????????????????????????????????????

    /// <summary>Get all units</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<UnitDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination, [FromQuery] bool? isActive)
    {
        try
        {
            var search = pagination.SearchTerm?.Trim().ToLower();
            var (units, total) = await _unitRepository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                predicate: u =>
                    (isActive == null || u.IsActive == isActive) &&
                    (string.IsNullOrEmpty(search) ||
                        (u.Code != null && u.Code.ToLower().Contains(search)) ||
                        (u.Name != null && u.Name.ToLower().Contains(search)) ||
                        (u.Description != null && u.Description.ToLower().Contains(search))),
                orderBy: q => q.ApplyOrderNewestFirst(pagination.SortBy, pagination.SortDirection, "Code"));
            return Ok(PaginatedResponse<UnitDto>.Ok(units.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving units");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving units" });
        }
    }

    /// <summary>Get unit by ID</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<UnitDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var unit = await _unitRepository.GetByIdAsync(id);
            if (unit == null) return NotFound(new ApiErrorResponse { Message = "Unit not found" });
            return Ok(new ApiResponse<UnitDto> { Success = true, Data = MapToDto(unit), Message = "Unit retrieved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unit {UnitId}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving unit" });
        }
    }

    /// <summary>Get active units only</summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(PaginatedResponse<UnitDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive([FromQuery] PaginationParams pagination)
    {
        try
        {
            var (units, total) = await _unitRepository.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: u => u.IsActive);
            return Ok(PaginatedResponse<UnitDto>.Ok(units.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active units");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving active units" });
        }
    }

    /// <summary>Create new unit</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UnitDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateUnitDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var existing = await _unitRepository.GetByCodeAsync(dto.Code);
            if (existing != null) return BadRequest(new ApiErrorResponse { Message = "Unit code already exists" });

            var unit = new Inventory.Domain.Entities.Unit
            {
                Code = dto.Code,
                Name = dto.Name,
                Description = dto.Description,
                DisplayOrder = dto.DisplayOrder,
                IsActive = true
            };
            await _unitRepository.AddAsync(unit);
            await _unitRepository.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = unit.Id },
                new ApiResponse<UnitDto> { Success = true, Data = MapToDto(unit), Message = "Unit created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating unit");
            return StatusCode(500, new ApiErrorResponse { Message = "Error creating unit" });
        }
    }

    /// <summary>Update unit</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<UnitDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUnitDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var unit = await _unitRepository.GetByIdAsync(id);
            if (unit == null) return NotFound(new ApiErrorResponse { Message = "Unit not found" });

            if (dto.Code != null && dto.Code != unit.Code)
            {
                var dup = await _unitRepository.GetByCodeAsync(dto.Code);
                if (dup != null) return BadRequest(new ApiErrorResponse { Message = "Unit code already exists" });
                unit.Code = dto.Code;
            }
            if (dto.Name        != null) unit.Name         = dto.Name;
            if (dto.Description != null) unit.Description  = dto.Description;
            if (dto.IsActive    != null) unit.IsActive      = dto.IsActive.Value;
            if (dto.DisplayOrder!= null) unit.DisplayOrder  = dto.DisplayOrder.Value;

            _unitRepository.Update(unit);
            await _unitRepository.SaveChangesAsync();
            return Ok(new ApiResponse<UnitDto> { Success = true, Data = MapToDto(unit), Message = "Unit updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating unit {UnitId}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error updating unit" });
        }
    }

    /// <summary>Delete (soft-delete) unit</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var unit = await _unitRepository.GetByIdAsync(id);
            if (unit == null) return NotFound(new ApiErrorResponse { Message = "Unit not found" });
            _unitRepository.SoftDelete(unit);
            await _unitRepository.SaveChangesAsync();
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Unit deleted successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting unit {UnitId}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting unit" }); }
    }

    // ?? UOM Conversions ???????????????????????????????????????????????????

    /// <summary>Get UOM conversions for an item</summary>
    [HttpGet("conversions/item/{itemId}")]
    [ProducesResponseType(typeof(ApiResponse<List<ItemUomConversionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversions(Guid itemId)
    {
        try
        {
            var conversions = await _conversionRepository.GetConversionsForItemAsync(itemId);
            return Ok(new ApiResponse<List<ItemUomConversionDto>>
            {
                Success = true,
                Data = conversions.Select(MapConversionToDto).ToList(),
                Message = "Conversions retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversions for item {ItemId}", itemId);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving conversions" });
        }
    }

    /// <summary>Create UOM conversion for an item</summary>
    [HttpPost("conversions/item/{itemId}")]
    [ProducesResponseType(typeof(ApiResponse<ItemUomConversionDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateConversion(Guid itemId, [FromBody] CreateItemUomConversionDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var existing = await _conversionRepository.GetConversionAsync(itemId, dto.FromUnitId, dto.ToUnitId);
            if (existing != null) return BadRequest(new ApiErrorResponse { Message = "Conversion already exists" });

            var conversion = new Inventory.Domain.Entities.ItemUomConversion
            {
                ItemId           = itemId,
                FromUnitId       = dto.FromUnitId,
                ToUnitId         = dto.ToUnitId,
                ConversionFactor = dto.ConversionFactor,
                IsActive         = true
            };
            await _conversionRepository.AddAsync(conversion);
            await _conversionRepository.SaveChangesAsync();
            return StatusCode(201, new ApiResponse<ItemUomConversionDto>
            {
                Success = true,
                Data = MapConversionToDto(conversion),
                Message = "Conversion created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating conversion for item {ItemId}", itemId);
            return StatusCode(500, new ApiErrorResponse { Message = "Error creating conversion" });
        }
    }

    /// <summary>Update UOM conversion</summary>
    [HttpPut("conversions/{id}")]
    [ProducesResponseType(typeof(ApiResponse<ItemUomConversionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateConversion(Guid id, [FromBody] UpdateItemUomConversionDto dto)
    {
        try
        {
            var conversion = await _conversionRepository.GetByIdAsync(id);
            if (conversion == null) return NotFound(new ApiErrorResponse { Message = "Conversion not found" });
            if (dto.ConversionFactor != null) conversion.ConversionFactor = dto.ConversionFactor.Value;
            if (dto.IsActive         != null) conversion.IsActive          = dto.IsActive.Value;
            _conversionRepository.Update(conversion);
            await _conversionRepository.SaveChangesAsync();
            return Ok(new ApiResponse<ItemUomConversionDto> { Success = true, Data = MapConversionToDto(conversion), Message = "Conversion updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating conversion {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error updating conversion" });
        }
    }

    /// <summary>Delete UOM conversion</summary>
    [HttpDelete("conversions/{id}")]
    public async Task<IActionResult> DeleteConversion(Guid id)
    {
        try
        {
            var conversion = await _conversionRepository.GetByIdAsync(id);
            if (conversion == null) return NotFound(new ApiErrorResponse { Message = "Conversion not found" });
            _conversionRepository.SoftDelete(conversion);
            await _conversionRepository.SaveChangesAsync();
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Conversion deleted successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting conversion {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting conversion" }); }
    }

    private static UnitDto MapToDto(Inventory.Domain.Entities.Unit u) => new()
    {
        Id = u.Id, Code = u.Code, Name = u.Name,
        Description = u.Description, IsActive = u.IsActive, DisplayOrder = u.DisplayOrder
    };

    private static ItemUomConversionDto MapConversionToDto(Inventory.Domain.Entities.ItemUomConversion c) => new()
    {
        Id = c.Id, ItemId = c.ItemId,
        FromUnitId = c.FromUnitId, FromUnitCode = c.FromUnit?.Code ?? "",
        ToUnitId   = c.ToUnitId,   ToUnitCode   = c.ToUnit?.Code ?? "",
        ConversionFactor = c.ConversionFactor, IsActive = c.IsActive
    };
}
