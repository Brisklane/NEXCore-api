using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;

namespace Procurement.Api.Controllers;

/// <summary>Vendor Categories — master classification for supplier segmentation.</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class VendorCategoryController : ControllerBase
{
    private readonly IVendorCategoryService _service;
    private readonly ILogger<VendorCategoryController> _logger;

    public VendorCategoryController(IVendorCategoryService service, ILogger<VendorCategoryController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    /// <summary>Get all vendor categories.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<VendorCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var list = await _service.GetAllAsync();
            return Ok(new ApiResponse<List<VendorCategoryDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving vendor categories"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving vendor categories" }); }
    }

    /// <summary>Get active vendor categories (for dropdown use).</summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(ApiResponse<List<VendorCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive()
    {
        try
        {
            var list = await _service.GetActiveAsync();
            return Ok(new ApiResponse<List<VendorCategoryDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving vendor categories"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving vendor categories" }); }
    }

    /// <summary>Get vendor category by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VendorCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var cat = await _service.GetByIdAsync(id);
            if (cat is null) return NotFound(new ApiErrorResponse { Message = "Vendor category not found" });
            return Ok(new ApiResponse<VendorCategoryDto> { Success = true, Data = cat });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving vendor category {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving vendor category" }); }
    }

    /// <summary>Create a new vendor category.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<VendorCategoryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateVendorCategoryDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var cat = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = cat.Id },
                new ApiResponse<VendorCategoryDto> { Success = true, Data = cat, Message = "Vendor category created" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating vendor category"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating vendor category" }); }
    }

    /// <summary>Update a vendor category.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VendorCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVendorCategoryDto dto)
    {
        try
        {
            var cat = await _service.UpdateAsync(id, dto);
            return Ok(new ApiResponse<VendorCategoryDto> { Success = true, Data = cat, Message = "Vendor category updated" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Vendor category not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating vendor category {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating vendor category" }); }
    }

    /// <summary>Delete a vendor category.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Vendor category deleted" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Vendor category not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting vendor category {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting vendor category" }); }
    }
}
