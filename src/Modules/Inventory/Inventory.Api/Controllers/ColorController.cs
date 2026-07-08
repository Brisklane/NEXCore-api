using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.Application.DTOs;
using Inventory.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Inventory.Api.Controllers;

/// <summary>Color master management</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ColorController : ControllerBase
{
    private readonly IColorRepository _colorRepository;
    private readonly ILogger<ColorController> _logger;

    public ColorController(IColorRepository colorRepository, ILogger<ColorController> logger)
    {
        _colorRepository = colorRepository;
        _logger = logger;
    }

    /// <summary>Get all colors</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<ColorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination, [FromQuery] bool? isActive)
    {
        try
        {
            var search = pagination.SearchTerm?.Trim().ToLower();
            var (colors, total) = await _colorRepository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                predicate: c =>
                    (isActive == null || c.IsActive == isActive) &&
                    (string.IsNullOrEmpty(search) ||
                        (c.Code != null && c.Code.ToLower().Contains(search)) ||
                        (c.Name != null && c.Name.ToLower().Contains(search)) ||
                        (c.Description != null && c.Description.ToLower().Contains(search)) ||
                        (c.HexCode != null && c.HexCode.ToLower().Contains(search)) ||
                        (c.ColorFamily != null && c.ColorFamily.ToLower().Contains(search))),
                orderBy: q => q.ApplyOrderNewestFirst(pagination.SortBy, pagination.SortDirection, "Code"));
            return Ok(PaginatedResponse<ColorDto>.Ok(colors.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving colors"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving colors" }); }
    }

    /// <summary>Get color by ID</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ColorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var color = await _colorRepository.GetByIdAsync(id);
            if (color == null) return NotFound(new ApiErrorResponse { Message = "Color not found" });
            return Ok(new ApiResponse<ColorDto> { Success = true, Data = MapToDto(color), Message = "Color retrieved successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving color {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving color" }); }
    }

    /// <summary>Get active colors</summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(PaginatedResponse<ColorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive([FromQuery] PaginationParams pagination)
    {
        try
        {
            var (colors, total) = await _colorRepository.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: c => c.IsActive);
            return Ok(PaginatedResponse<ColorDto>.Ok(colors.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving active colors"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving active colors" }); }
    }

    /// <summary>Get colors by family (e.g., Red, Blue, Neutral)</summary>
    [HttpGet("family/{family}")]
    [ProducesResponseType(typeof(PaginatedResponse<ColorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByFamily(string family, [FromQuery] PaginationParams pagination)
    {
        try
        {
            var (colors, total) = await _colorRepository.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: c => c.ColorFamily == family);
            return Ok(PaginatedResponse<ColorDto>.Ok(colors.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving colors by family {Family}", family); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving colors" }); }
    }

    /// <summary>Create color</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ColorDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateColorDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var existing = await _colorRepository.GetByCodeAsync(dto.Code);
            if (existing != null) return BadRequest(new ApiErrorResponse { Message = "Color code already exists" });

            var color = new Inventory.Domain.Entities.Color
            {
                Code = dto.Code, Name = dto.Name, HexCode = dto.HexCode,
                R = dto.R, G = dto.G, B = dto.B,
                ColorFamily = dto.ColorFamily, SwatchImageUrl = dto.SwatchImageUrl,
                DisplayOrder = dto.DisplayOrder, IsActive = true
            };
            await _colorRepository.AddAsync(color);
            await _colorRepository.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = color.Id },
                new ApiResponse<ColorDto> { Success = true, Data = MapToDto(color), Message = "Color created successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating color"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating color" }); }
    }

    /// <summary>Update color</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ColorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateColorDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var color = await _colorRepository.GetByIdAsync(id);
            if (color == null) return NotFound(new ApiErrorResponse { Message = "Color not found" });

            if (dto.Code != null && dto.Code != color.Code)
            {
                var dup = await _colorRepository.GetByCodeAsync(dto.Code);
                if (dup != null) return BadRequest(new ApiErrorResponse { Message = "Color code already exists" });
                color.Code = dto.Code;
            }
            if (dto.Name          != null) color.Name          = dto.Name;
            if (dto.HexCode       != null) color.HexCode       = dto.HexCode;
            if (dto.R             != null) color.R             = dto.R;
            if (dto.G             != null) color.G             = dto.G;
            if (dto.B             != null) color.B             = dto.B;
            if (dto.ColorFamily   != null) color.ColorFamily   = dto.ColorFamily;
            if (dto.SwatchImageUrl!= null) color.SwatchImageUrl= dto.SwatchImageUrl;
            if (dto.DisplayOrder  != null) color.DisplayOrder  = dto.DisplayOrder.Value;
            if (dto.IsActive      != null) color.IsActive      = dto.IsActive.Value;

            _colorRepository.Update(color);
            await _colorRepository.SaveChangesAsync();
            return Ok(new ApiResponse<ColorDto> { Success = true, Data = MapToDto(color), Message = "Color updated successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating color {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating color" }); }
    }

    /// <summary>Delete color (soft-delete)</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var color = await _colorRepository.GetByIdAsync(id);
            if (color == null) return NotFound(new ApiErrorResponse { Message = "Color not found" });
            _colorRepository.SoftDelete(color);
            await _colorRepository.SaveChangesAsync();
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Color deleted successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting color {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting color" }); }
    }

    private static ColorDto MapToDto(Inventory.Domain.Entities.Color c) => new()
    {
        Id = c.Id, Code = c.Code, Name = c.Name, HexCode = c.HexCode,
        R = c.R, G = c.G, B = c.B, ColorFamily = c.ColorFamily,
        SwatchImageUrl = c.SwatchImageUrl, DisplayOrder = c.DisplayOrder, IsActive = c.IsActive
    };
}
