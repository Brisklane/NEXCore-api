using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;

namespace Procurement.Api.Controllers;

/// <summary>Landed Costs — freight/duty/insurance allocated across received goods.</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class LandedCostController : ControllerBase
{
    private readonly ILandedCostService _service;
    private readonly ILogger<LandedCostController> _logger;

    public LandedCostController(ILandedCostService service, ILogger<LandedCostController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<LandedCostDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        try { return Ok(await _service.GetAllAsync(pagination)); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving landed costs"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving landed costs" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LandedCostDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var entry = await _service.GetByIdAsync(id);
            if (entry is null) return NotFound(new ApiErrorResponse { Message = "Landed cost not found" });
            return Ok(new ApiResponse<LandedCostDto> { Success = true, Data = entry });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving landed cost {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving landed cost" }); }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LandedCostDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateLandedCostDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var entry = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = entry.Id }, new ApiResponse<LandedCostDto> { Success = true, Data = entry, Message = "Landed cost created" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating landed cost"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating landed cost" }); }
    }

    [HttpPost("{id:guid}/post")]
    [ProducesResponseType(typeof(ApiResponse<LandedCostDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Post(Guid id)
    {
        try { return Ok(new ApiResponse<LandedCostDto> { Success = true, Data = await _service.PostAsync(id, GetUserId()), Message = "Landed cost posted and allocated" }); }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Landed cost not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error posting landed cost {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error posting landed cost" }); }
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<LandedCostDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try { return Ok(new ApiResponse<LandedCostDto> { Success = true, Data = await _service.CancelAsync(id), Message = "Landed cost cancelled" }); }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Landed cost not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error cancelling landed cost {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error cancelling landed cost" }); }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try { await _service.DeleteAsync(id); return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Landed cost deleted" }); }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Landed cost not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting landed cost {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting landed cost" }); }
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }
}
