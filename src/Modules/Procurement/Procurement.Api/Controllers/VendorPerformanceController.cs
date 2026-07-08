using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;

namespace Procurement.Api.Controllers;

/// <summary>Vendor Performance — periodic KPI scorecards per vendor (delivery, quality, price, etc.).</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class VendorPerformanceController : ControllerBase
{
    private readonly IVendorPerformanceService _service;
    private readonly ILogger<VendorPerformanceController> _logger;

    public VendorPerformanceController(IVendorPerformanceService service, ILogger<VendorPerformanceController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    /// <summary>Get all performance scorecards.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<VendorPerformanceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var list = await _service.GetAllAsync();
            return Ok(new ApiResponse<List<VendorPerformanceDto>> { Success = true, Data = list, Message = $"{list.Count} scorecard(s) retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving performance scorecards"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving vendor performance" }); }
    }

    /// <summary>Get scorecards for a vendor (most recent first).</summary>
    [HttpGet("by-vendor/{vendorId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<VendorPerformanceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByVendor(Guid vendorId)
    {
        try { return Ok(new ApiResponse<List<VendorPerformanceDto>> { Success = true, Data = await _service.GetByVendorAsync(vendorId) }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving performance by vendor"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving vendor performance" }); }
    }

    /// <summary>Get a scorecard by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VendorPerformanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var entry = await _service.GetByIdAsync(id);
            if (entry is null) return NotFound(new ApiErrorResponse { Message = "Scorecard not found" });
            return Ok(new ApiResponse<VendorPerformanceDto> { Success = true, Data = entry });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving scorecard {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving scorecard" }); }
    }

    /// <summary>Create a performance scorecard. Overall rating is computed from the weighted KPIs.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<VendorPerformanceDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateVendorPerformanceDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var entry = await _service.CreateAsync(dto, GetUserId());
            return CreatedAtAction(nameof(GetById), new { id = entry.Id },
                new ApiResponse<VendorPerformanceDto> { Success = true, Data = entry, Message = "Scorecard recorded" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating scorecard"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating scorecard" }); }
    }

    /// <summary>Update a performance scorecard.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VendorPerformanceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVendorPerformanceDto dto)
    {
        try { return Ok(new ApiResponse<VendorPerformanceDto> { Success = true, Data = await _service.UpdateAsync(id, dto), Message = "Scorecard updated" }); }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Scorecard not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating scorecard {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating scorecard" }); }
    }

    /// <summary>Delete a performance scorecard.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Scorecard deleted" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Scorecard not found" }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting scorecard {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting scorecard" }); }
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }
}
