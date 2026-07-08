using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.Application.DTOs;
using Inventory.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Inventory.Api.Controllers;

/// <summary>Brand management</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BrandController : ControllerBase
{
    private readonly IBrandRepository _brandRepository;
    private readonly ILogger<BrandController> _logger;

    public BrandController(IBrandRepository brandRepository, ILogger<BrandController> logger)
    {
        _brandRepository = brandRepository;
        _logger = logger;
    }

    /// <summary>Get all brands</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<BrandDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination, [FromQuery] bool? isActive)
    {
        try
        {
            var search = pagination.SearchTerm?.Trim().ToLower();
            var (brands, total) = await _brandRepository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                predicate: b =>
                    (isActive == null || b.IsActive == isActive) &&
                    (string.IsNullOrEmpty(search) ||
                        (b.Code != null && b.Code.ToLower().Contains(search)) ||
                        (b.Name != null && b.Name.ToLower().Contains(search)) ||
                        (b.Description != null && b.Description.ToLower().Contains(search))),
                orderBy: q => q.ApplyOrderNewestFirst(pagination.SortBy, pagination.SortDirection, nameof(Inventory.Domain.Entities.Brand.Code)));
            return Ok(PaginatedResponse<BrandDto>.Ok(brands.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving brands"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving brands" }); }
    }

    /// <summary>Get brand by ID</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<BrandDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var brand = await _brandRepository.GetByIdAsync(id);
            if (brand == null) return NotFound(new ApiErrorResponse { Message = "Brand not found" });
            return Ok(new ApiResponse<BrandDto> { Success = true, Data = MapToDto(brand), Message = "Brand retrieved successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving brand {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving brand" }); }
    }

    /// <summary>Get active brands</summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(PaginatedResponse<BrandDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive([FromQuery] PaginationParams pagination)
    {
        try
        {
            var (brands, total) = await _brandRepository.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: b => b.IsActive);
            return Ok(PaginatedResponse<BrandDto>.Ok(brands.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving active brands"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving active brands" }); }
    }

    /// <summary>Create brand</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BrandDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateBrandDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var existing = await _brandRepository.GetByCodeAsync(dto.Code);
            if (existing != null) return BadRequest(new ApiErrorResponse { Message = "Brand code already exists" });

            var brand = new Inventory.Domain.Entities.Brand
            {
                Code = dto.Code, Name = dto.Name, Description = dto.Description,
                LogoUrl = dto.LogoUrl, Website = dto.Website, IsActive = true
            };
            await _brandRepository.AddAsync(brand);
            await _brandRepository.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = brand.Id },
                new ApiResponse<BrandDto> { Success = true, Data = MapToDto(brand), Message = "Brand created successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating brand"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating brand" }); }
    }

    /// <summary>Update brand</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<BrandDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBrandDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var brand = await _brandRepository.GetByIdAsync(id);
            if (brand == null) return NotFound(new ApiErrorResponse { Message = "Brand not found" });

            if (dto.Code != null && dto.Code != brand.Code)
            {
                var dup = await _brandRepository.GetByCodeAsync(dto.Code);
                if (dup != null) return BadRequest(new ApiErrorResponse { Message = "Brand code already exists" });
                brand.Code = dto.Code;
            }
            if (dto.Name        != null) brand.Name        = dto.Name;
            if (dto.Description != null) brand.Description = dto.Description;
            if (dto.LogoUrl     != null) brand.LogoUrl     = dto.LogoUrl;
            if (dto.Website     != null) brand.Website     = dto.Website;
            if (dto.IsActive    != null) brand.IsActive    = dto.IsActive.Value;

            _brandRepository.Update(brand);
            await _brandRepository.SaveChangesAsync();
            return Ok(new ApiResponse<BrandDto> { Success = true, Data = MapToDto(brand), Message = "Brand updated successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating brand {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating brand" }); }
    }

    /// <summary>Delete brand (soft-delete)</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var brand = await _brandRepository.GetByIdAsync(id);
            if (brand == null) return NotFound(new ApiErrorResponse { Message = "Brand not found" });
            _brandRepository.SoftDelete(brand);
            await _brandRepository.SaveChangesAsync();
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Brand deleted successfully" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting brand {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting brand" }); }
    }

    private static BrandDto MapToDto(Inventory.Domain.Entities.Brand b) => new()
    {
        Id = b.Id, Code = b.Code, Name = b.Name, Description = b.Description,
        LogoUrl = b.LogoUrl, Website = b.Website, IsActive = b.IsActive
    };
}
