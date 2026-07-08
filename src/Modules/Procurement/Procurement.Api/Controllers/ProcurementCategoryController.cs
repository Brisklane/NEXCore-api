using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;

namespace Procurement.Api.Controllers;

/// <summary>Procurement Categories — hierarchical spend classification for PO lines and budgeting.</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class ProcurementCategoryController : ControllerBase
{
    private readonly IProcurementCategoryService _service;
    private readonly ILogger<ProcurementCategoryController> _logger;

    public ProcurementCategoryController(IProcurementCategoryService service, ILogger<ProcurementCategoryController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    /// <summary>Get all procurement categories (flat list).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ProcurementCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var list = await _service.GetAllAsync();
            return Ok(new ApiResponse<List<ProcurementCategoryDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving procurement categories"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving categories" }); }
    }

    /// <summary>Get active categories (for dropdown use).</summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(ApiResponse<List<ProcurementCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive()
    {
        try
        {
            var list = await _service.GetActiveAsync();
            return Ok(new ApiResponse<List<ProcurementCategoryDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving procurement categories"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving categories" }); }
    }

    /// <summary>Get categories as a nested tree (parent → children).</summary>
    [HttpGet("tree")]
    [ProducesResponseType(typeof(ApiResponse<List<ProcurementCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTree()
    {
        try
        {
            var tree = await _service.GetTreeAsync();
            return Ok(new ApiResponse<List<ProcurementCategoryDto>> { Success = true, Data = tree });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving category tree"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving category tree" }); }
    }

    /// <summary>Get procurement category by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProcurementCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var cat = await _service.GetByIdAsync(id);
            if (cat is null) return NotFound(new ApiErrorResponse { Message = "Category not found" });
            return Ok(new ApiResponse<ProcurementCategoryDto> { Success = true, Data = cat });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving category {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving category" }); }
    }

    /// <summary>Create a new procurement category.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProcurementCategoryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProcurementCategoryDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var cat = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = cat.Id },
                new ApiResponse<ProcurementCategoryDto> { Success = true, Data = cat, Message = "Category created" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating category"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating category" }); }
    }

    /// <summary>Update a procurement category.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProcurementCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProcurementCategoryDto dto)
    {
        try
        {
            var cat = await _service.UpdateAsync(id, dto);
            return Ok(new ApiResponse<ProcurementCategoryDto> { Success = true, Data = cat, Message = "Category updated" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Category not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating category {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating category" }); }
    }

    /// <summary>Delete a procurement category.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Category deleted" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Category not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting category {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting category" }); }
    }
}
