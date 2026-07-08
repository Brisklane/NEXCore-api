using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.Application.DTOs;
using Inventory.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Inventory.Api.Controllers;

/// <summary>Size master management</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SizeController : ControllerBase
{
    private readonly ISizeRepository _sizeRepository;
    private readonly ILogger<SizeController> _logger;

    public SizeController(ISizeRepository sizeRepository, ILogger<SizeController> logger)
    {
        _sizeRepository = sizeRepository;
        _logger = logger;
    }

    /// <summary>Get all sizes</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<SizeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination, [FromQuery] bool? isActive)
    {
        try
        {
            var search = pagination.SearchTerm?.Trim().ToLower();
            var (sizes, total) = await _sizeRepository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                predicate: s =>
                    (isActive == null || s.IsActive == isActive) &&
                    (string.IsNullOrEmpty(search) ||
                        (s.Code != null && s.Code.ToLower().Contains(search)) ||
                        (s.Name != null && s.Name.ToLower().Contains(search)) ||
                        (s.Description != null && s.Description.ToLower().Contains(search)) ||
                        (s.SizeChart != null && s.SizeChart.ToLower().Contains(search))),
                orderBy: q => q.ApplyOrderNewestFirst(pagination.SortBy, pagination.SortDirection, "Code"));
            return Ok(PaginatedResponse<SizeDto>.Ok(sizes.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving sizes"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving sizes" }); }
    }

    /// <summary>Get size by ID</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SizeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var size = await _sizeRepository.GetByIdAsync(id);
            if (size == null) return NotFound(new ApiErrorResponse { Message = "Size not found" });
            return Ok(new ApiResponse<SizeDto> { Success = true, Data = MapToDto(size), Message = "Size retrieved successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving size {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving size" }); }
    }

    /// <summary>Get sizes by chart (e.g., Apparel, Footwear, Ring)</summary>
    [HttpGet("chart/{sizeChart}")]
    [ProducesResponseType(typeof(PaginatedResponse<SizeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBySizeChart(string sizeChart, [FromQuery] PaginationParams pagination)
    {
        try
        {
            var (sizes, total) = await _sizeRepository.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: s => s.SizeChart == sizeChart);
            return Ok(PaginatedResponse<SizeDto>.Ok(sizes.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving sizes for chart {Chart}", sizeChart); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving sizes" }); }
    }

    /// <summary>Create size</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SizeDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSizeDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var existing = await _sizeRepository.GetByCodeAsync(dto.Code, dto.SizeChart);
            if (existing != null) return BadRequest(new ApiErrorResponse { Message = "Size code already exists in this size chart" });

            var size = new Inventory.Domain.Entities.Size
            {
                Code = dto.Code, Name = dto.Name,
                SizeChart = dto.SizeChart, SortOrder = dto.SortOrder, IsActive = true
            };
            await _sizeRepository.AddAsync(size);
            await _sizeRepository.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = size.Id },
                new ApiResponse<SizeDto> { Success = true, Data = MapToDto(size), Message = "Size created successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating size"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating size" }); }
    }

    /// <summary>Update size</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SizeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSizeDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var size = await _sizeRepository.GetByIdAsync(id);
            if (size == null) return NotFound(new ApiErrorResponse { Message = "Size not found" });

            if (dto.Code != null) size.Code = dto.Code;
            if (dto.Name != null) size.Name = dto.Name;
            if (dto.SizeChart  != null) size.SizeChart  = dto.SizeChart;
            if (dto.SortOrder  != null) size.SortOrder  = dto.SortOrder.Value;
            if (dto.IsActive   != null) size.IsActive   = dto.IsActive.Value;

            _sizeRepository.Update(size);
            await _sizeRepository.SaveChangesAsync();
            return Ok(new ApiResponse<SizeDto> { Success = true, Data = MapToDto(size), Message = "Size updated successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating size {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating size" }); }
    }

    /// <summary>Delete size (soft-delete)</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var size = await _sizeRepository.GetByIdAsync(id);
            if (size == null) return NotFound(new ApiErrorResponse { Message = "Size not found" });
            _sizeRepository.SoftDelete(size);
            await _sizeRepository.SaveChangesAsync();
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Size deleted successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting size {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting size" }); }
    }

    private static SizeDto MapToDto(Inventory.Domain.Entities.Size s) => new()
    {
        Id = s.Id, Code = s.Code, Name = s.Name,
        SizeChart = s.SizeChart, SortOrder = s.SortOrder, IsActive = s.IsActive
    };
}
